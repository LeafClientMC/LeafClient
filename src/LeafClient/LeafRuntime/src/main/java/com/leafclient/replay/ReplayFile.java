package com.leafclient.replay;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import net.minecraft.util.math.Vec3d;

import java.io.*;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.zip.GZIPInputStream;
import java.util.zip.GZIPOutputStream;

public class ReplayFile {
    private ReplayHeader header;
    private List<TickEntry> timeline;
    private transient ReplayPacketTimeline packetTimeline; // NEW: Field to hold packet data

    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();

    public ReplayFile() {
        this.timeline = new ArrayList<>();
    }

    public void setHeader(ReplayHeader header) {
        this.header = header;
    }

    public ReplayHeader getHeader() {
        return header;
    }

    public void addTickEntry(TickEntry entry) {
        timeline.add(entry);
    }

    public List<TickEntry> getTimeline() {
        return timeline;
    }

    // NEW: Getter for the packet timeline
    public ReplayPacketTimeline getPacketTimeline() {
        return packetTimeline;
    }

    public void save(File file) throws IOException {
        try (FileOutputStream fos = new FileOutputStream(file);
             GZIPOutputStream gzos = new GZIPOutputStream(fos);
             OutputStreamWriter writer = new OutputStreamWriter(gzos)) {
            GSON.toJson(this, writer);
        }
    }

    public static ReplayFile load(File file) throws IOException {
        ReplayFile loadedReplay;
        try (FileInputStream fis = new FileInputStream(file);
             GZIPInputStream gzis = new GZIPInputStream(fis);
             InputStreamReader reader = new InputStreamReader(gzis)) {
            loadedReplay = GSON.fromJson(reader, ReplayFile.class);
        }

        if (loadedReplay != null) {
            File packetFile = new File(file.getParentFile(), file.getName() + ".packets.json.gz");
            loadedReplay.packetTimeline = ReplayPacketTimeline.load(packetFile);
        }

        return loadedReplay;
    }

    public static class ReplayHeader {
        public String version = "1.0";
        public String mcVersion;
        public long startTime;
        public int totalTicks;

        public ReplayHeader(String mcVersion, long startTime) {
            this.mcVersion = mcVersion;
            this.startTime = startTime;
        }
    }

    public static class TickEntry {
        public int tickIndex;
        public PlayerSnapshot playerSnapshot;
        public List<EntitySnapshot> entitySnapshots;
        public List<WorldEvent> worldEvents;
        public List<PacketLog> packetLogs;
        public List<PlayerAction> playerActions;

        public TickEntry(int tickIndex) {
            this.tickIndex = tickIndex;
            this.entitySnapshots = new ArrayList<>();
            this.worldEvents = new ArrayList<>();
            this.packetLogs = new ArrayList<>();
            this.playerActions = new ArrayList<>();
        }
    }

    public static class PlayerSnapshot {
        public double x, y, z;
        public float yaw, pitch;
        public double velocityX, velocityY, velocityZ;
        public String heldItem;
        public boolean sneaking;
        public boolean sprinting;
        public boolean isSwingingArm;
        public float handSwingProgress;
        public boolean isUsingItem;
        public int itemUseTime;
        public float health;
        public int hurtTime;

        public PlayerSnapshot(double x, double y, double z, float yaw, float pitch,
                            double vx, double vy, double vz, String heldItem,
                            boolean sneaking, boolean sprinting,
                            boolean isSwingingArm, float handSwingProgress,
                            boolean isUsingItem, int itemUseTime,
                            float health, int hurtTime) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.yaw = yaw;
            this.pitch = pitch;
            this.velocityX = vx;
            this.velocityY = vy;
            this.velocityZ = vz;
            this.heldItem = heldItem;
            this.sneaking = sneaking;
            this.sprinting = sprinting;
            this.isSwingingArm = isSwingingArm;
            this.handSwingProgress = handSwingProgress;
            this.isUsingItem = isUsingItem;
            this.itemUseTime = itemUseTime;
            this.health = health;
            this.hurtTime = hurtTime;
        }
    }

    public static class EntitySnapshot {
        public String uuid;
        public String type;
        public double x, y, z;
        public float yaw, pitch;
        public double velocityX, velocityY, velocityZ;
        public float health;
        public int hurtTime;

        public EntitySnapshot(UUID uuid, String type, double x, double y, double z,
                            float yaw, float pitch, double vx, double vy, double vz,
                            float health, int hurtTime) {
            this.uuid = uuid.toString();
            this.type = type;
            this.x = x;
            this.y = y;
            this.z = z;
            this.yaw = yaw;
            this.pitch = pitch;
            this.velocityX = vx;
            this.velocityY = vy;
            this.velocityZ = vz;
            this.health = health;
            this.hurtTime = hurtTime;
        }
    }

    public static class WorldEvent {
        public String type;
        public double x, y, z;
        public String data;

        public WorldEvent(String type, double x, double y, double z, String data) {
            this.type = type;
            this.x = x;
            this.y = y;
            this.z = z;
            this.data = data;
        }
    }

    public static class PacketLog {
        public String packetType;
        public String data;
        public long timestamp;

        public PacketLog(String packetType, String data) {
            this.packetType = packetType;
            this.data = data;
            this.timestamp = System.currentTimeMillis();
        }
    }

    public static class PlayerAction {
        public String actionType; // "ATTACK", "BREAK_BLOCK", "USE_ITEM", "DAMAGE_TAKEN"
        public String targetEntity;
        public int blockX, blockY, blockZ;
        public float breakProgress;
        public String itemUsed;
        public float damageAmount;
        public long timestamp;

        public PlayerAction(String actionType) {
            this.actionType = actionType;
            this.timestamp = System.currentTimeMillis();
        }

        public static PlayerAction attack(String targetEntityUuid, float damage) {
            PlayerAction action = new PlayerAction("ATTACK");
            action.targetEntity = targetEntityUuid;
            action.damageAmount = damage;
            return action;
        }

        public static PlayerAction breakBlock(int x, int y, int z, float progress) {
            PlayerAction action = new PlayerAction("BREAK_BLOCK");
            action.blockX = x;
            action.blockY = y;
            action.blockZ = z;
            action.breakProgress = progress;
            return action;
        }

        public static PlayerAction useItem(String item) {
            PlayerAction action = new PlayerAction("USE_ITEM");
            action.itemUsed = item;
            return action;
        }

        public static PlayerAction damageTaken(float damage) {
            PlayerAction action = new PlayerAction("DAMAGE_TAKEN");
            action.damageAmount = damage;
            return action;
        }
    }
}