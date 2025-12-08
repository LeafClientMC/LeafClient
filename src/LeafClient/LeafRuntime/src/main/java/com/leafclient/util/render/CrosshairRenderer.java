package com.leafclient.util.render;

import com.leafclient.LeafClient;
import com.leafclient.modules.CrosshairMod;
import com.leafclient.modules.CrosshairSettings;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;

public class CrosshairRenderer {
    
    private static final MinecraftClient client = MinecraftClient.getInstance();
    
    public static void render(DrawContext context) {
        CrosshairMod mod = LeafClient.modManager.getMod(CrosshairMod.class);
        if (mod == null || !mod.isEnabled() || client.player == null) return;
        
        // Don't render in GUIs
        if (client.currentScreen != null) return;
        
        CrosshairSettings settings = mod.getSettings();
        
        int screenWidth = client.getWindow().getScaledWidth();
        int screenHeight = client.getWindow().getScaledHeight();
        int centerX = screenWidth / 2;
        int centerY = screenHeight / 2;
        
        // Convert AWT Color to ARGB int
        int color = 0xFF000000 | 
                   (settings.color.getRed() << 16) | 
                   (settings.color.getGreen() << 8) | 
                   settings.color.getBlue();
        
        int size = settings.size;
        int gap = settings.gap;
        int thickness = 1;
        
        // Draw horizontal line (left)
        context.fill(centerX - gap - size, centerY - thickness / 2, 
                    centerX - gap, centerY + thickness / 2 + 1, color);
        
        // Draw horizontal line (right)
        context.fill(centerX + gap + 1, centerY - thickness / 2, 
                    centerX + gap + size + 1, centerY + thickness / 2 + 1, color);
        
        // Draw vertical line (top)
        context.fill(centerX - thickness / 2, centerY - gap - size, 
                    centerX + thickness / 2 + 1, centerY - gap, color);
        
        // Draw vertical line (bottom)
        context.fill(centerX - thickness / 2, centerY + gap + 1, 
                    centerX + thickness / 2 + 1, centerY + gap + size + 1, color);
        
        // Draw center dot if enabled (perfectly centered)
        if (settings.dot) {
            context.fill(centerX, centerY, centerX + 1, centerY + 1, color);
        }
    }
}