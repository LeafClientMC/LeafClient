package com.leafclient.replay;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import io.netty.buffer.ByteBuf;
import io.netty.buffer.Unpooled;
import net.minecraft.network.packet.Packet;
import net.minecraft.network.PacketByteBuf;

import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStreamWriter;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Base64;
import java.util.List;
import java.util.zip.GZIPOutputStream;


public class PacketCaptureManager {

    private static final Gson GSON = new GsonBuilder().setPrettyPrinting().create();

    public static class RecordedPacket {
        public long timestamp; // System.currentTimeMillis() at capture time (or tick index if you prefer)
        public String packetClass; // fully-qualified class name of the packet
        public String payloadBase64; // base64 of the serialized packet bytes

        public RecordedPacket(long timestamp, String packetClass, byte[] payload) {
            this.timestamp = timestamp;
            this.packetClass = packetClass;
            this.payloadBase64 = Base64.getEncoder().encodeToString(payload);
        }
    }

    public static class PacketCaptureFile {
        public long recordedAt;
        public List<RecordedPacket> packets = new ArrayList<>();
    }

    private static boolean recording = false;
    private static PacketCaptureFile current = null;
    private static File outFile = null;

    public static synchronized void startRecording(File outputFileBase) {
        if (recording) return;
    
        recording = true;
        packetCount = 0;
    
        current = new PacketCaptureFile();
        current.recordedAt = System.currentTimeMillis();
    
        // We want the packet file to live next to the .lfreplay:
        // replay_<ts>.lfreplay   (world snapshots)
        // replay_<ts>.lfreplay.packets.json.gz (packets)
        File parent = outputFileBase.getParentFile();
        String baseName = outputFileBase.getName(); // e.g. "replay_12345.lfreplay"
        outFile = new File(parent, baseName + ".packets.json.gz");
    
        if (parent != null && !parent.exists()) {
            parent.mkdirs();
        }
    
        System.out.println("[PacketCapture] Started recording to " + outFile.getAbsolutePath());
    }

    public static synchronized void recordPacket(Packet<?> packet) {
        if (!recording || packet == null)
            return;

        packetCount++;

        try {
            // Serialize packet into PacketByteBuf using vanilla serialization
            ByteBuf buf = Unpooled.buffer();
            PacketByteBuf pb = new PacketByteBuf(buf);

            // Many vanilla packets implement write(PacketByteBuf)
            // The Packet<?> interface implementation in vanilla provides a write method
            try {
                // calling packet.write(pb)
                packet.write(pb);
            } catch (NoSuchMethodError | AbstractMethodError melee) {
                // In the unlikely event the mapping is different, fallback - but typically this
                // will not happen.
                System.err.println("[PacketCapture] Warning: failed to write packet via packet.write(pb): "
                        + packet.getClass().getName());
            }

            int len = pb.readableBytes();
            byte[] bytes = new byte[len];
            pb.getBytes(pb.readerIndex(), bytes);

            RecordedPacket rp = new RecordedPacket(System.currentTimeMillis(), packet.getClass().getName(), bytes);
            current.packets.add(rp);

            // release buffers - Netty's Unpooled.buffer doesn't require explicit release
            // here,
            // but we will clear reference to allow GC
            buf.release();
        } catch (Throwable t) {
            t.printStackTrace();
        }
    }

    public static synchronized void stopAndSave() {
        System.out.println("=======================================");
        System.out.println("[LEAF REPLAY] SAVING REPLAY FILE...");
        System.out.println("=======================================");
        if (!recording || current == null)
            return;
        try {
            // Ensure parent directory exists
            if (outFile != null)
                outFile.getParentFile().mkdirs();

            // Write to GZIP JSON
            try (FileOutputStream fos = new FileOutputStream(outFile);
                    GZIPOutputStream gzos = new GZIPOutputStream(fos);
                    OutputStreamWriter writer = new OutputStreamWriter(gzos, StandardCharsets.UTF_8)) {
                GSON.toJson(current, writer);

                System.out.println("=======================================");
                System.out.println("[LEAF REPLAY] FILE SAVED SUCCESSFULLY!");
                System.out.println("Location: " + outFile.getAbsolutePath());
                System.out.println("=======================================");

            }

            System.out.println(
                    "[PacketCapture] Saved " + current.packets.size() + " packets to " + outFile.getAbsolutePath());
        } catch (Exception e) {
            e.printStackTrace();
        } finally {
            recording = false;
            current = null;
            outFile = null;
        }
    }

    private static int packetCount = 0;

    public static int getPacketCount() {
        return packetCount;
    }

    public static boolean isRecording() {
        return recording;
    }
}
