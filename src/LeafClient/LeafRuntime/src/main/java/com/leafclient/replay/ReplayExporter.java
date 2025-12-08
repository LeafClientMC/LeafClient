package com.leafclient.replay;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.texture.NativeImage;
import net.minecraft.client.util.ScreenshotRecorder;

import java.io.File;
import java.io.IOException;
import java.nio.file.Path;

public class ReplayExporter {
    
    private final ReplayPlayer player;
    private final File outputDir;
    private boolean exporting = false;
    private int frameCount = 0;

    public ReplayExporter(ReplayPlayer player, File outputDir) {
        this.player = player;
        this.outputDir = outputDir;
        outputDir.mkdirs();
    }

    public void startExport() {
        exporting = true;
        frameCount = 0;
        player.seekTo(0);
        System.out.println("[ReplayExporter] Starting export to: " + outputDir.getAbsolutePath());
    }

    public void captureFrame() {
        if (!exporting) return;

        MinecraftClient client = MinecraftClient.getInstance();
        
        try {
            File frameFile = new File(outputDir, String.format("frame_%06d.png", frameCount));
            ScreenshotRecorder.saveScreenshot(
                outputDir,
                String.format("frame_%06d.png", frameCount),
                client.getFramebuffer(),
                (text) -> {}
            );
            frameCount++;
        } catch (Exception e) {
            e.printStackTrace();
        }

        // Check if export is complete
        if (player.getCurrentTick() >= player.getTotalTicks()) {
            stopExport();
        }
    }

    public void stopExport() {
        exporting = false;
        System.out.println("[ReplayExporter] Export complete. " + frameCount + " frames saved.");
        System.out.println("[ReplayExporter] Use FFmpeg to convert frames to video:");
        System.out.println("ffmpeg -framerate 20 -i frame_%06d.png -c:v libx264 -pix_fmt yuv420p output.mp4");
    }

    public boolean isExporting() {
        return exporting;
    }

    public int getFrameCount() {
        return frameCount;
    }
}
