package com.leafclient.replay;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.network.ClientPlayerEntity;
import net.minecraft.entity.Entity;
import net.minecraft.entity.LivingEntity;
import net.minecraft.item.ItemStack;
import net.minecraft.util.hit.BlockHitResult;
import net.minecraft.util.hit.HitResult;
import net.minecraft.util.math.BlockPos;
import net.minecraft.util.math.Vec3d;

import java.io.File;
import java.io.IOException;

public class ReplayRecorder {
    private final File replayFile;
    private final ReplayFile replay;
    private int currentTick = 0;
    private boolean recording = false;
    private long startTime;
    
    private int tickCounter = 0;
    private static final int RECORD_INTERVAL = 2; // Record every 2 ticks (10 times per second)
    
    // Track previous state for detecting changes
    private float lastPlayerHealth = 20.0f;
    private boolean wasSwinging = false;
    private boolean wasUsingItem = false;
    private BlockPos lastBreakingBlock = null;

    // Flag to control if actions/packets should be logged
    private boolean shouldLogActions = false; 

    public ReplayRecorder(File replayFile) {
        this.replayFile = replayFile;
        this.replay = new ReplayFile();
    }

    public void startRecording() {
        MinecraftClient client = MinecraftClient.getInstance();
        
        startTime = System.currentTimeMillis();
        String mcVersion = client.getGameVersion();
        
        replay.setHeader(new ReplayFile.ReplayHeader(mcVersion, startTime));
        recording = true;
        currentTick = 0;
        tickCounter = 0;
        
        if (client.player != null) {
            lastPlayerHealth = client.player.getHealth();
            wasSwinging = client.player.handSwinging;
            wasUsingItem = client.player.isUsingItem();
            lastBreakingBlock = null;
        }
        
        System.out.println("[ReplayRecorder] Started recording to: " + replayFile.getName());
    }

    public void tick() {
        if (!recording) return;
        
        MinecraftClient client = MinecraftClient.getInstance();
        if (client.world == null || client.player == null) {
            stopRecording();
            return;
        }
    
        tickCounter++;
        // Respect ReplaySettings recordInterval instead of fixed constant
        int interval = ReplaySettings.getInstance().getRecordInterval();
        if (tickCounter < interval) {
            return;
        }
        tickCounter = 0;
    
        ReplayFile.TickEntry entry = new ReplayFile.TickEntry(currentTick);
    
        // Capture player snapshot with actions
        entry.playerSnapshot = capturePlayerSnapshot(client.player);
    
        // Detect player actions (only if logging is enabled)
        if (shouldLogActions) {
            detectPlayerActions(client, entry);
        }
    
        // Capture entities with health
        if (ReplaySettings.getInstance().isRecordEntities()) {
            captureNearbyEntities(client, entry);
        }
    
        // NOTE: world events and packets are appended via the logX methods
        // during the same tick; here we only create the TickEntry and add it
        // to the timeline.
        replay.addTickEntry(entry);
        currentTick++;
    }

    private void captureNearbyEntities(MinecraftClient client, ReplayFile.TickEntry entry) {
        Vec3d playerPos = client.player.getPos();
        double maxDistance = 64.0;
        int maxEntities = 50;
        int count = 0;
        
        for (Entity entity : client.world.getEntities()) {
            if (entity != client.player) {
                if (entity.getPos().distanceTo(playerPos) <= maxDistance) {
                    entry.entitySnapshots.add(captureEntitySnapshot(entity));
                    count++;
                    if (count >= maxEntities) break;
                }
            }
        }
    }

    private ReplayFile.PlayerSnapshot capturePlayerSnapshot(ClientPlayerEntity player) {
        Vec3d pos = player.getPos();
        Vec3d velocity = player.getVelocity();
        ItemStack heldItem = player.getMainHandStack();
        String itemName = heldItem.isEmpty() ? "none" : heldItem.getItem().toString();

        return new ReplayFile.PlayerSnapshot(
            pos.x, pos.y, pos.z,
            player.getYaw(), player.getPitch(),
            velocity.x, velocity.y, velocity.z,
            itemName,
            player.isSneaking(),
            player.isSprinting(),
            player.handSwinging,
            player.getHandSwingProgress(0),
            player.isUsingItem(),
            player.getItemUseTime(),
            player.getHealth(),
            player.hurtTime
        );
    }

    private ReplayFile.EntitySnapshot captureEntitySnapshot(Entity entity) {
        Vec3d pos = entity.getPos();
        Vec3d velocity = entity.getVelocity();
        
        float health = 0;
        int hurtTime = 0;
        if (entity instanceof LivingEntity) {
            LivingEntity living = (LivingEntity) entity;
            health = living.getHealth();
            hurtTime = living.hurtTime;
        }

        return new ReplayFile.EntitySnapshot(
            entity.getUuid(),
            entity.getType().toString(),
            pos.x, pos.y, pos.z,
            entity.getYaw(), entity.getPitch(),
            velocity.x, velocity.y, velocity.z,
            health,
            hurtTime
        );
    }

    private void detectPlayerActions(MinecraftClient client, ReplayFile.TickEntry entry) {
        ClientPlayerEntity player = client.player;
        if (player == null || client.interactionManager == null) return;

        // --- Attack (Hand Swing) ---
        if (player.handSwinging && !wasSwinging) {
            entry.playerActions.add(ReplayFile.PlayerAction.attack("player_swing", 0));
        }
        wasSwinging = player.handSwinging;

        // --- Damage Taken ---
        float currentHealth = player.getHealth();
        if (currentHealth < lastPlayerHealth) {
            float damage = lastPlayerHealth - currentHealth;
            entry.playerActions.add(ReplayFile.PlayerAction.damageTaken(damage));
        }
        lastPlayerHealth = currentHealth;

        // --- Item Use ---
        if (player.isUsingItem() && !wasUsingItem) {
            ItemStack item = player.getActiveItem();
            if (!item.isEmpty()) {
                entry.playerActions.add(ReplayFile.PlayerAction.useItem(item.getItem().toString()));
            }
        }
        wasUsingItem = player.isUsingItem();

        // --- Block Breaking (Simplified and Corrected) ---
        if (client.options.attackKey.isPressed() && client.crosshairTarget != null && client.crosshairTarget.getType() == HitResult.Type.BLOCK) {
            BlockHitResult blockHit = (BlockHitResult) client.crosshairTarget;
            BlockPos currentTarget = blockHit.getBlockPos();
            
            if (currentTarget != null && !currentTarget.equals(lastBreakingBlock)) {
                entry.playerActions.add(ReplayFile.PlayerAction.breakBlock(currentTarget.getX(), currentTarget.getY(), currentTarget.getZ(), 0.0f));
            }
            lastBreakingBlock = currentTarget;
        } else {
            lastBreakingBlock = null;
        }
    }


    public void logBlockBreak(int x, int y, int z, float progress) {
        if (!recording || replay.getTimeline().isEmpty() || !shouldLogActions) return;
        
        ReplayFile.TickEntry currentEntry = replay.getTimeline().get(replay.getTimeline().size() - 1);
        currentEntry.playerActions.add(ReplayFile.PlayerAction.breakBlock(x, y, z, progress));
    }

    public void logAttack(String targetUuid, float damage) {
        if (!recording || replay.getTimeline().isEmpty() || !shouldLogActions) return;
        
        ReplayFile.TickEntry currentEntry = replay.getTimeline().get(replay.getTimeline().size() - 1);
        currentEntry.playerActions.add(ReplayFile.PlayerAction.attack(targetUuid, damage));
    }

    public void stopRecording() {
        if (!recording) return;
        
        recording = false;
        replay.getHeader().totalTicks = currentTick;
        
        new Thread(() -> {
            try {
                replay.save(replayFile);
                System.out.println("[ReplayRecorder] Saved replay: " + replayFile.getName() + " (" + currentTick + " ticks)");
            } catch (IOException e) {
                System.err.println("[ReplayRecorder] Failed to save replay: " + e.getMessage());
                e.printStackTrace();
            }
        }).start();
    }

    public void logWorldEvent(String type, double x, double y, double z, String data) {
        if (!recording || replay.getTimeline().isEmpty() || !shouldLogActions) return;
        
        ReplaySettings settings = ReplaySettings.getInstance();
        
        if (type.equals("particle") && !settings.isRecordParticles()) return;
        if (type.equals("sound") && !settings.isRecordSounds()) return;
        if (type.equals("block_update") && !settings.isRecordBlockUpdates()) return;
        
        ReplayFile.TickEntry currentEntry = replay.getTimeline().get(replay.getTimeline().size() - 1);
        currentEntry.worldEvents.add(new ReplayFile.WorldEvent(type, x, y, z, data));
    }

    public void logPacket(String packetType, String data) {
        if (!recording || replay.getTimeline().isEmpty() || !shouldLogActions) return;
        if (!ReplaySettings.getInstance().isRecordPackets()) return;
        
        if (data.length() > 500) {
            data = data.substring(0, 500) + "...";
        }
        
        ReplayFile.TickEntry currentEntry = replay.getTimeline().get(replay.getTimeline().size() - 1);
        currentEntry.packetLogs.add(new ReplayFile.PacketLog(packetType, data));
    }

    public boolean isRecording() {
        return recording;
    }

    public File getReplayFile() {
        return replayFile;
    }

    public int getCurrentTick() {
        return currentTick;
    }

    public void setShouldLogActions(boolean shouldLogActions) {
        this.shouldLogActions = shouldLogActions;
        System.out.println("[ReplayRecorder] Action logging " + (shouldLogActions ? "ENABLED" : "DISABLED"));
    }
}
