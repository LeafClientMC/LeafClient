package com.leafclient.replay;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.zip.GZIPInputStream;

/**
 * Represents the timeline of captured network packets for a replay.
 * Loads data from the .packets.json.gz file.
 */
public class ReplayPacketTimeline {

    private static final Gson GSON = new GsonBuilder().create(); // Don't need pretty printing for loading

    private final List<PacketCaptureManager.RecordedPacket> packets;
    private final long recordedAt; // Timestamp when packet recording started

    private ReplayPacketTimeline(List<PacketCaptureManager.RecordedPacket> packets, long recordedAt) {
        this.packets = packets;
        this.recordedAt = recordedAt;
    }

    /**
     * Loads packet data from a GZIP-compressed JSON file.
     * The file is expected to be in the format of PacketCaptureManager.PacketCaptureFile.
     *
     * @param packetFile The .packets.json.gz file to load.
     * @return A ReplayPacketTimeline instance, or null if loading fails.
     */
    public static ReplayPacketTimeline load(File packetFile) {
        if (!packetFile.exists()) {
            System.err.println("[ReplayPacketTimeline] Packet file does not exist: " + packetFile.getAbsolutePath());
            return null;
        }

        try (FileInputStream fis = new FileInputStream(packetFile);
             GZIPInputStream gzis = new GZIPInputStream(fis);
             InputStreamReader reader = new InputStreamReader(gzis, StandardCharsets.UTF_8)) {

            PacketCaptureManager.PacketCaptureFile captureFile = GSON.fromJson(reader, PacketCaptureManager.PacketCaptureFile.class);

            if (captureFile != null && captureFile.packets != null) {
                System.out.println("[ReplayPacketTimeline] Loaded " + captureFile.packets.size() + " packets from " + packetFile.getName());
                return new ReplayPacketTimeline(captureFile.packets, captureFile.recordedAt);
            } else {
                System.err.println("[ReplayPacketTimeline] Loaded packet file is empty or malformed: " + packetFile.getName());
                return null;
            }
        } catch (IOException e) {
            System.err.println("[ReplayPacketTimeline] Failed to load packet file: " + packetFile.getName());
            e.printStackTrace();
            return null;
        }
    }

    public List<PacketCaptureManager.RecordedPacket> getPackets() {
        return packets;
    }

    public long getRecordedAt() {
        return recordedAt;
    }
}
