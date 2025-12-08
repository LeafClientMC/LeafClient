package com.leafclient.replay;

import net.minecraft.client.MinecraftClient;
import net.minecraft.util.math.Vec3d;

import java.util.ArrayList;
import java.util.List;

public class ReplayPlayer {
    private final ReplayFile replay;
    private final ReplayPacketTimeline packetTimeline; // NEW: Field to hold packet data
    private int currentTick = 0;
    private boolean playing = false;
    private boolean paused = false;
    
    private Vec3d cameraPos;
    private float cameraYaw;
    private float cameraPitch;
    private float cameraFov = 70.0f;
    
    private boolean freecamMode = false;

    private ReplayKeyframe.KeyframeManager keyframeManager;
    private boolean useKeyframes = false;
    
    // Action animation states
    private boolean isPlayingAttackAnimation = false;
    private int attackAnimationTick = 0;
    private boolean isPlayingDamageAnimation = false;
    private int damageAnimationTick = 0;

    // NEW: Constructor now accepts ReplayPacketTimeline
    public ReplayPlayer(ReplayFile replay, ReplayPacketTimeline packetTimeline) {
        this.replay = replay;
        this.packetTimeline = packetTimeline; // Store the packet timeline
        this.keyframeManager = new ReplayKeyframe.KeyframeManager();
    }

    public void startPlayback() {
        playing = true;
        currentTick = 0;
        paused = false;
        freecamMode = false;
        
        // Reset animation states on start
        isPlayingAttackAnimation = false;
        attackAnimationTick = 0;
        isPlayingDamageAnimation = false;
        damageAnimationTick = 0;
        
        if (!replay.getTimeline().isEmpty()) {
            ReplayFile.PlayerSnapshot firstSnapshot = replay.getTimeline().get(0).playerSnapshot;
            if (firstSnapshot != null) {
                cameraPos = new Vec3d(firstSnapshot.x, firstSnapshot.y + 1.62, firstSnapshot.z);
                cameraYaw = firstSnapshot.yaw;
                cameraPitch = firstSnapshot.pitch;
                
                System.out.println("[ReplayPlayer] Camera initialized at: " + cameraPos);
                System.out.println("[ReplayPlayer] Yaw: " + cameraYaw + ", Pitch: " + cameraPitch);
            } else {
                System.err.println("[ReplayPlayer] WARNING: First snapshot is null!");
            }
        } else {
            System.err.println("[ReplayPlayer] WARNING: Timeline is empty!");
        }
        
        System.out.println("[ReplayPlayer] Started playback (" + replay.getHeader().totalTicks + " ticks)");
        
        // NEW: Log if packet timeline is available
        if (packetTimeline != null) {
            System.out.println("[ReplayPlayer] Packet timeline loaded with " + packetTimeline.getPackets().size() + " packets.");
        } else {
            System.out.println("[ReplayPlayer] No packet timeline available for this replay.");
        }
    }

    public void tick() {
        if (!playing || paused) return;
        
        if (currentTick >= replay.getTimeline().size()) {
            stopPlayback();
            return;
        }

        ReplayFile.TickEntry entry = replay.getTimeline().get(currentTick);
        
        if (useKeyframes && !keyframeManager.getKeyframes().isEmpty()) {
            ReplayKeyframe interpolated = keyframeManager.smoothInterpolate(currentTick);
            if (interpolated != null) {
                cameraPos = interpolated.position;
                cameraYaw = interpolated.yaw;
                cameraPitch = interpolated.pitch;
                cameraFov = interpolated.fov;
            }
        } else if (entry.playerSnapshot != null) {
            if (!freecamMode) {
                cameraPos = new Vec3d(entry.playerSnapshot.x, entry.playerSnapshot.y + 1.62, entry.playerSnapshot.z);
                cameraYaw = entry.playerSnapshot.yaw;
                cameraPitch = entry.playerSnapshot.pitch;
                
                if (currentTick % 20 == 0) {
                    System.out.println("[ReplayPlayer] Tick " + currentTick + "/" + replay.getTimeline().size() + 
                        " - Camera: " + String.format("%.1f, %.1f, %.1f", cameraPos.x, cameraPos.y, cameraPos.z) +
                        " | Yaw: " + String.format("%.1f", cameraYaw) +
                        " | Health: " + entry.playerSnapshot.health +
                        " | Swinging: " + entry.playerSnapshot.isSwingingArm);
                }
            }
        } else {
            if (currentTick % 20 == 0) {
                System.err.println("[ReplayPlayer] WARNING: Tick " + currentTick + " has null player snapshot!");
            }
        }
        
        // Play actions
        playActions(entry);
        
        // Update animation states
        // These flags are now primarily for the HUD overlay, as the mixin directly controls the animation.
        // We still manage them to ensure proper reset and for potential future use.
        if (isPlayingAttackAnimation) {
            attackAnimationTick++;
            if (attackAnimationTick > 6) { // Attack animation lasts ~6 ticks
                isPlayingAttackAnimation = false;
            }
        }
        
        if (isPlayingDamageAnimation) {
            damageAnimationTick++;
            if (damageAnimationTick > 10) { // Damage animation lasts ~10 ticks
                isPlayingDamageAnimation = false;
            }
        }
        
        currentTick++;
        applyCamera();
    }

    private void applyCamera() {
        MinecraftClient client = MinecraftClient.getInstance();
    
        if (client.player == null || client.world == null || cameraPos == null) return;
    
        // HARD OVERRIDE PLAYER CAMERA POSITION
        client.player.setPos(cameraPos.x, cameraPos.y, cameraPos.z);
    
        // FORCE ROTATION
        client.player.setYaw(cameraYaw);
        client.player.setPitch(cameraPitch);
    }
    

    private void playActions(ReplayFile.TickEntry entry) {
        if (entry.playerActions == null || entry.playerActions.isEmpty()) return;
        
        for (ReplayFile.PlayerAction action : entry.playerActions) {
            switch (action.actionType) {
                case "ATTACK":
                    System.out.println("[ReplayPlayer] Attack action at tick " + currentTick);
                    isPlayingAttackAnimation = true;
                    attackAnimationTick = 0;
                    break;
                case "DAMAGE_TAKEN":
                    System.out.println("[ReplayPlayer] Damage taken: " + action.damageAmount + " at tick " + currentTick);
                    isPlayingDamageAnimation = true;
                    damageAnimationTick = 0;
                    break;
                case "BREAK_BLOCK":
                    System.out.println("[ReplayPlayer] Breaking block at " + action.blockX + ", " + action.blockY + ", " + action.blockZ + 
                        " - Progress: " + (action.breakProgress * 100) + "%");
                    break;
                case "USE_ITEM":
                    System.out.println("[ReplayPlayer] Using item: " + action.itemUsed);
                    break;
            }
        }
    }

    public void stopPlayback() {
        playing = false;
        paused = false;
        currentTick = 0;
        freecamMode = false;
        isPlayingAttackAnimation = false; // Reset on stop
        isPlayingDamageAnimation = false; // Reset on stop
        System.out.println("[ReplayPlayer] Stopped playback");
    }

    public void pause() {
        paused = true;
        System.out.println("[ReplayPlayer] Paused at tick " + currentTick);
    }

    public void resume() {
        paused = false;
        System.out.println("[ReplayPlayer] Resumed from tick " + currentTick);
    }

    public void seekTo(int tick) {
        if (tick >= 0 && tick < replay.getTimeline().size()) {
            currentTick = tick;
            
            if (!freecamMode) {
                ReplayFile.TickEntry entry = replay.getTimeline().get(currentTick);
                if (entry.playerSnapshot != null) {
                    cameraPos = new Vec3d(entry.playerSnapshot.x, entry.playerSnapshot.y + 1.62, entry.playerSnapshot.z);
                    cameraYaw = entry.playerSnapshot.yaw;
                    cameraPitch = entry.playerSnapshot.pitch;
                }
            }
            
            // Reset animation states on seek
            isPlayingAttackAnimation = false;
            attackAnimationTick = 0;
            isPlayingDamageAnimation = false;
            damageAnimationTick = 0;
            
            System.out.println("[ReplayPlayer] Seeked to tick " + tick);
        }
    }

    public void skipForward(int ticks) {
        seekTo(currentTick + ticks);
    }

    public void skipBackward(int ticks) {
        seekTo(currentTick - ticks);
    }

    public void setPlaybackSpeed(float speed) {
        // Implementation would modify tick rate
    }

    public void updateCameraPosition(float forward, float strafe, float up) {
        if (!freecamMode) return;
        
        float speed = ReplaySettings.getInstance().getCameraSpeed();
        
        double radYaw = Math.toRadians(cameraYaw);
        double dx = -Math.sin(radYaw) * forward + Math.cos(radYaw) * strafe;
        double dz = Math.cos(radYaw) * forward + Math.sin(radYaw) * strafe;
        
        cameraPos = cameraPos.add(dx * speed, up * speed, dz * speed);
    }

    public void updateCameraRotation(float deltaYaw, float deltaPitch) {
        float sensitivity = ReplaySettings.getInstance().getCameraSensitivity();
        cameraYaw += deltaYaw * sensitivity;
        cameraPitch = Math.max(-90, Math.min(90, cameraPitch + deltaPitch * sensitivity));
    }

    public boolean isPlaying() {
        return playing;
    }

    public boolean isPaused() {
        return paused;
    }

    public int getCurrentTick() {
        return currentTick;
    }

    public int getTotalTicks() {
        return replay.getHeader().totalTicks;
    }

    public Vec3d getCameraPos() {
        return cameraPos;
    }

    public float getCameraYaw() {
        return cameraYaw;
    }

    public float getCameraPitch() {
        return cameraPitch;
    }

    public float getCameraFov() {
        return cameraFov;
    }

    public void setCameraFov(float fov) {
        this.cameraFov = fov;
    }

    public boolean isFreecamMode() {
        return freecamMode;
    }

    public void setFreecamMode(boolean freecam) {
        this.freecamMode = freecam;
        System.out.println("[ReplayPlayer] Freecam mode: " + (freecam ? "ON" : "OFF"));
    }

    public void toggleFreecam() {
        setFreecamMode(!freecamMode);
    }

    public ReplayFile.TickEntry getCurrentTickEntry() {
        if (currentTick >= 0 && currentTick < replay.getTimeline().size()) {
            return replay.getTimeline().get(currentTick);
        }
        return null;
    }

    public void addKeyframe() {
        ReplayKeyframe keyframe = new ReplayKeyframe(
            currentTick,
            cameraPos,
            cameraYaw,
            cameraPitch,
            cameraFov
        );
        keyframeManager.addKeyframe(keyframe);
        System.out.println("[ReplayPlayer] Keyframe added at tick " + currentTick);
    }
    
    public void removeKeyframe(int index) {
        keyframeManager.removeKeyframe(index);
        System.out.println("[ReplayPlayer] Keyframe " + index + " removed");
    }
    
    public void clearKeyframes() {
        keyframeManager.clearKeyframes();
        System.out.println("[ReplayPlayer] All keyframes cleared");
    }
    
    public ReplayKeyframe.KeyframeManager getKeyframeManager() {
        return keyframeManager;
    }
    
    public void setUseKeyframes(boolean use) {
        this.useKeyframes = use;
        System.out.println("[ReplayPlayer] Use keyframes: " + use);
    }
    
    public boolean isUsingKeyframes() {
        return useKeyframes;
    }
    
    public ReplayFile getReplay() {
        return replay;
    }

    public boolean isPlayingAttackAnimation() {
        return isPlayingAttackAnimation;
    }

    public boolean isPlayingDamageAnimation() {
        return isPlayingDamageAnimation;
    }
    
    // NEW: Getter for the packet timeline
    public ReplayPacketTimeline getPacketTimeline() {
        return packetTimeline;
    }
}