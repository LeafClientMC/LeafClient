package com.leafclient.modules;

import com.leafclient.replay.ReplayRecorder;
import com.leafclient.replay.ReplayPlayer;
import com.leafclient.replay.ReplayFile;
import com.leafclient.replay.ReplayExporter;
import net.minecraft.client.MinecraftClient;
import net.minecraft.text.Text;
import java.io.File;

public class ReplayMod implements ILeafMod {
    private boolean enabled = false;
    private ReplayRecorder recorder;
    private ReplayPlayer player;
    private ReplayExporter exporter;
    private boolean isRecording = false;
    private boolean isPlaying = false;
    private File pendingReplayFile = null;
    private boolean pendingReplay = false;

    private static final File REPLAY_DIR = new File(
            System.getenv("APPDATA") + File.separator + "LeafClient" + File.separator + "replays");

    public ReplayMod() {
        REPLAY_DIR.mkdirs();
    }

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        if (!enabled) {
            stopRecording();
            stopPlayback();
            stopExport();
        }
        System.out.println("[LeafClient] Replay " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "Replay";
    }

    public void tickPendingReplay() {
        if (!pendingReplay || pendingReplayFile == null) {
            return;
        }
    
        MinecraftClient client = MinecraftClient.getInstance();
        if (client.world == null || client.player == null) {
            // Still no world; wait one more tick
            return;
        }
    
        // We now have a world; start playback
        File file = this.pendingReplayFile;
        this.pendingReplay = false;
        this.pendingReplayFile = null;
    
        System.out.println("[Replay] World detected, starting pending replay: " + file.getAbsolutePath());
        boolean started = startPlayback(file);
        System.out.println("[Replay] Pending replay startPlayback() returned: " + started);
    }

    public void queueReplay(File replayFile) {
        this.pendingReplayFile = replayFile;
        this.pendingReplay = true;
        System.out.println("[Replay] Queued replay for playback once a world is loaded: " + replayFile.getName());
    }

    public void startRecording() {
        if (!enabled || isRecording)
            return;

        MinecraftClient client = MinecraftClient.getInstance();
        if (client.world == null || client.player == null) {
            if (client.player != null) {
                client.player.sendMessage(Text.literal("§c[Replay] Cannot record - not in world"), false);
            }
            return;
        }

        String timestamp = String.valueOf(System.currentTimeMillis());
        File replayFile = new File(REPLAY_DIR, "replay_" + timestamp + ".lfreplay");

        // Main replay recorder (snapshots, actions, entities, etc.)
        recorder = new ReplayRecorder(replayFile);
        recorder.startRecording();
        recorder.setShouldLogActions(true);

        // Packet recording sidecar
        try {
            com.leafclient.replay.PacketCaptureManager.startRecording(replayFile);
        } catch (Throwable t) {
            System.err.println("[Leaf Replay] Failed to start packet capture");
            t.printStackTrace();
            if (client.player != null) {
                client.player.sendMessage(Text.literal("§c[Replay] Failed to start packet capture"), false);
            }
        }

        isRecording = true;

        client.player.sendMessage(Text.literal("§a[Replay] Recording started"), false);
    }

    public boolean startPlayback(File replayFile) {
        System.out.println("[Replay] Loading replay: " + replayFile.getAbsolutePath());

        MinecraftClient client = MinecraftClient.getInstance();

        if (!replayFile.exists()) {
            System.out.println("[Replay] File does NOT exist");
            return false;
        }

        try {
            // Load the replay file (snapshots + packet timeline)
            ReplayFile replay = ReplayFile.load(replayFile);

            if (replay == null || replay.getTimeline().isEmpty()) {
                System.out.println("[Replay] Replay is empty or failed to load");
                return false;
            }

            // If we're connected to a server, disconnect so live packets don't interfere
            if (client.getNetworkHandler() != null) {
                System.out.println("[Replay] Disconnecting from current server before starting replay.");
                client.getNetworkHandler().getConnection().disconnect(Text.literal("[Replay] Starting replay"));
            }

            // Create the ReplayPlayer with both snapshots and packet timeline
            this.player = new ReplayPlayer(replay, replay.getPacketTimeline());
            this.player.startPlayback();
            this.isPlaying = true;

            if (isRecording) {
                stopRecording();
            }

            if (client.player != null) {
                client.player.sendMessage(
                        Text.literal("§a[Replay] Playing: §f" + replayFile.getName()),
                        false);
            }

            System.out.println("[Replay] Playback started: " + replayFile.getName());
            return true;

        } catch (Exception e) {
            System.err.println("[Replay] Failed to load replay");
            e.printStackTrace();
            return false;
        }
    }

    public void stopRecording() {
        if (!isRecording || recorder == null)
            return;

        // Stop the tick/world snapshot recorder
        recorder.stopRecording();
        isRecording = false;

        // Stop packet recording and flush to disk
        try {
            com.leafclient.replay.PacketCaptureManager.stopAndSave();
        } catch (Throwable t) {
            System.err.println("[Leaf Replay] Failed to stop/save packet capture");
            t.printStackTrace();
        }

        MinecraftClient client = MinecraftClient.getInstance();
        if (client.player != null) {
            client.player.sendMessage(
                    Text.literal("§a[Replay] Recording saved to " + recorder.getReplayFile().getName()),
                    false);
        }

        recorder = null;
    }

    public void stopPlayback() {
        if (!isPlaying || player == null)
            return;

        player.stopPlayback();
        isPlaying = false;
        player = null;

        MinecraftClient client = MinecraftClient.getInstance();
        if (client.player != null) {
            client.player.sendMessage(Text.literal("§a[Replay] Playback stopped"), false);
        }
    }

    public void startExport(File outputDir) {
        if (!isPlaying || player == null)
            return;

        exporter = new ReplayExporter(player, outputDir);
        exporter.startExport();

        MinecraftClient client = MinecraftClient.getInstance();
        if (client.player != null) {
            client.player.sendMessage(Text.literal("§a[Replay] Export started"), false);
        }
    }

    public void stopExport() {
        if (exporter != null && exporter.isExporting()) {
            exporter.stopExport();
            exporter = null;
        }
    }

    public void tickExport() {
        if (exporter != null && exporter.isExporting()) {
            exporter.captureFrame();
        }
    }

    public void tick() {
        if (!enabled)
            return;

        if (isRecording && recorder != null) {
            recorder.tick();
        }

        if (isPlaying && player != null) {
            player.tick();
            tickExport(); // Capture frame if exporting
        }
    }

    public boolean isRecording() {
        return isRecording;
    }

    public boolean isPlaying() {
        return isPlaying;
    }

    public boolean isExporting() {
        return exporter != null && exporter.isExporting();
    }

    public ReplayRecorder getRecorder() {
        return recorder;
    }

    public ReplayPlayer getPlayer() {
        return player;
    }

    public ReplayExporter getExporter() {
        return exporter;
    }

    public File getReplayDirectory() {
        return REPLAY_DIR;
    }

    public File[] getReplayFiles() {
        File[] files = REPLAY_DIR.listFiles((dir, name) -> name.endsWith(".lfreplay"));
        return files != null ? files : new File[0];
    }
}
