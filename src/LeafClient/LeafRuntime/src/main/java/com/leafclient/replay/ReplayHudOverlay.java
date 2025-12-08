package com.leafclient.replay;

import com.leafclient.LeafClient;
import com.leafclient.modules.ReplayMod;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;

public class ReplayHudOverlay {

    public static void render(DrawContext context, int screenWidth, int screenHeight) {
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod == null || !replayMod.isEnabled()) return;

        MinecraftClient client = MinecraftClient.getInstance();
        
        // Recording indicator
        if (replayMod.isRecording()) {
            renderRecordingIndicator(context, client, screenWidth);
        }
        
        // Playback controls
        if (replayMod.isPlaying()) {
            renderPlaybackControls(context, client, screenWidth, screenHeight, replayMod);
        }
    }

    private static void renderRecordingIndicator(DrawContext context, MinecraftClient client, int screenWidth) {
        String recordText = "(REC)"; // Changed from "● REC"
        int textWidth = client.textRenderer.getWidth(recordText);
        int x = screenWidth / 2 - textWidth / 2;
        int y = 10;
        
        // Flashing red dot
        long time = System.currentTimeMillis();
        int alpha = (int)(Math.sin(time / 500.0) * 127 + 128);
        int color = (alpha << 24) | 0xFF0000;
        
        context.drawTextWithShadow(client.textRenderer, recordText, x, y, color);
    }

    private static void renderPlaybackControls(DrawContext context, MinecraftClient client, 
                                               int screenWidth, int screenHeight, ReplayMod replayMod) {
        ReplayPlayer player = replayMod.getPlayer();
        if (player == null) return;

        int barWidth = 400;
        int barHeight = 4;
        int barX = screenWidth / 2 - barWidth / 2;
        int barY = screenHeight - 60;

        // Background bar
        context.fill(barX, barY, barX + barWidth, barY + barHeight, 0x80000000);

        // Progress bar
        float progress = (float) player.getCurrentTick() / player.getTotalTicks();
        int progressWidth = (int) (barWidth * progress);
        context.fill(barX, barY, barX + progressWidth, barY + barHeight, 0xFFFFFFFF);

        // Time display
        String timeText = formatTime(player.getCurrentTick()) + " / " + formatTime(player.getTotalTicks());
        int timeWidth = client.textRenderer.getWidth(timeText);
        context.drawTextWithShadow(client.textRenderer, timeText, 
            screenWidth / 2 - timeWidth / 2, barY - 15, 0xFFFFFF);

        // Status text
        String statusText = player.isPaused() ? "[PAUSED]" : "[PLAYING]"; // Changed from "⏸ PAUSED" : "▶ PLAYING"
        if (player.isUsingKeyframes()) {
            statusText += " | [KEYFRAMES]"; // Changed from "🎬 KEYFRAMES"
        }
        if (player.isFreecamMode()) {
            statusText += " | [FREECAM]"; // Changed from "📷 FREECAM"
        }
        
        int statusWidth = client.textRenderer.getWidth(statusText);
        context.drawTextWithShadow(client.textRenderer, statusText, 
            screenWidth / 2 - statusWidth / 2, barY + 10, 0xFFFFFF);

        // Camera info
        if (player.isFreecamMode()) {
            String camInfo = String.format("Cam: %.1f, %.1f, %.1f | Yaw: %.1f | Pitch: %.1f | FOV: %.0f",
                player.getCameraPos().x, player.getCameraPos().y, player.getCameraPos().z,
                player.getCameraYaw(), player.getCameraPitch(), player.getCameraFov());
            
            context.drawTextWithShadow(client.textRenderer, camInfo, 10, screenHeight - 30, 0xFFFFFF);
        }

        // Action indicators
        ReplayFile.TickEntry currentEntry = player.getCurrentTickEntry();
        if (currentEntry != null && currentEntry.playerActions != null && !currentEntry.playerActions.isEmpty()) {
            int actionCount = currentEntry.playerActions.size();
            String actionsText = "Actions: " + actionCount;
            context.drawTextWithShadow(client.textRenderer, actionsText, 10, screenHeight - 50, 0xFFFF00);
        }

        // Show current action being played (TEXT ONLY)
        if (player.isPlayingAttackAnimation()) {
            context.drawTextWithShadow(client.textRenderer, "[ATTACK]", screenWidth / 2 - 30, screenHeight / 2 + 30, 0xFF0000); // Changed from "⚔ ATTACK"
        }

        if (player.isPlayingDamageAnimation()) {
            context.drawTextWithShadow(client.textRenderer, "[DAMAGE]", screenWidth / 2 - 35, screenHeight / 2 + 50, 0xFF0000); // Changed from "❤ DAMAGE"
        }

        // Keyframe indicators on timeline
        if (player.isUsingKeyframes()) {
            for (ReplayKeyframe kf : player.getKeyframeManager().getKeyframes()) {
                float kfProgress = (float) kf.tick / player.getTotalTicks();
                int kfX = barX + (int) (barWidth * kfProgress);
                
                // Draw keyframe marker
                context.fill(kfX - 1, barY - 4, kfX + 1, barY + barHeight + 4, 0xFFFFFF00);
            }
        }
    }

    private static String formatTime(int ticks) {
        int seconds = ticks / 20;
        int minutes = seconds / 60;
        seconds = seconds % 60;
        return String.format("%d:%02d", minutes, seconds);
    }
}
