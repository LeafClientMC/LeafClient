package com.leafclient.util.render;

import com.leafclient.LeafClient;
import com.leafclient.modules.KeystrokesMod;
import com.leafclient.ui.KeystrokeBox;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.util.InputUtil;
import org.lwjgl.glfw.GLFW;

import java.util.Map;

public class KeystrokesRenderer {
    
    private static final MinecraftClient client = MinecraftClient.getInstance();
    
    public static void updateKeyStates() {
        KeystrokesMod mod = LeafClient.modManager.getMod(KeystrokesMod.class);
        if (mod == null || !mod.isEnabled() || client.player == null) return;
        
        long windowHandle = client.getWindow().getHandle();
        updateKeyState(mod, "W", GLFW.GLFW_KEY_W, windowHandle);
        updateKeyState(mod, "A", GLFW.GLFW_KEY_A, windowHandle);
        updateKeyState(mod, "S", GLFW.GLFW_KEY_S, windowHandle);
        updateKeyState(mod, "D", GLFW.GLFW_KEY_D, windowHandle);
        updateKeyState(mod, "SPACE", GLFW.GLFW_KEY_SPACE, windowHandle);
        updateMouseState(mod, "LMB", GLFW.GLFW_MOUSE_BUTTON_LEFT, windowHandle);
        updateMouseState(mod, "RMB", GLFW.GLFW_MOUSE_BUTTON_RIGHT, windowHandle);
        
        if (mod.isShowAnimations()) {
            mod.updateAllAnimations();
        }
    }
    
    private static void updateKeyState(KeystrokesMod mod, String key, int glfwKey, long windowHandle) {
        KeystrokeBox box = mod.getBox(key);
        if (box != null) {
            boolean pressed = InputUtil.isKeyPressed(windowHandle, glfwKey);
            box.setPressed(pressed);
        }
    }
    
    private static void updateMouseState(KeystrokesMod mod, String key, int mouseButton, long windowHandle) {
        KeystrokeBox box = mod.getBox(key);
        if (box != null) {
            boolean pressed = GLFW.glfwGetMouseButton(windowHandle, mouseButton) == GLFW.GLFW_PRESS;
            box.setPressed(pressed);
        }
    }
    
    public static void render(DrawContext context) {
        KeystrokesMod mod = LeafClient.modManager.getMod(KeystrokesMod.class);
        if (mod == null || !mod.isEnabled() || client.player == null) return;
        
        // FIXED: Removed screen check so it renders in Editor
        
        // FIXED: Set base to 0. HudRenderer already handles the translation.
        // If we fetch coordinates again here, we double them (100 + 100 = 200).
        float baseX = 0;
        float baseY = 0;
        
        float scale = mod.getScale();
        Map<String, KeystrokeBox> boxes = mod.getKeystrokeBoxes();
        
        for (KeystrokeBox box : boxes.values()) {
            renderKeystrokeBox(context, mod, box, baseX, baseY, scale);
        }
    }
    
    private static void renderKeystrokeBox(DrawContext context, KeystrokesMod mod, KeystrokeBox box, float baseX, float baseY, float scale) {
        float x = baseX + (box.getX() * scale);
        float y = baseY + (box.getY() * scale);
        float width = box.getWidth() * scale;
        float height = box.getHeight() * scale;
        
        int backgroundColor = mod.getBackgroundColor();
        int pressedColor = mod.getPressedColor();
        int textColor = mod.getTextColor();
        int borderColor = mod.getBorderColor();
        
        if (mod.isChromaMode()) {
            textColor = getChromaColor(System.currentTimeMillis(), 0);
            borderColor = getChromaColor(System.currentTimeMillis(), 500);
        }
        
        float opacity = mod.getOpacity();
        backgroundColor = applyOpacity(backgroundColor, opacity);
        pressedColor = applyOpacity(pressedColor, opacity);
        
        float animProgress = mod.isShowAnimations() ? box.getKeyState().getAnimationProgress() : (box.isPressed() ? 1.0f : 0.0f);
        int currentColor = interpolateColor(backgroundColor, pressedColor, animProgress);
        
        if (mod.isShowAnimations() && animProgress > 0) {
            float scaleEffect = 1.0f + (animProgress * 0.05f);
            float centerX = x + width / 2;
            float centerY = y + height / 2;
            
            width *= scaleEffect;
            height *= scaleEffect;
            x = centerX - width / 2;
            y = centerY - height / 2;
        }
        
        context.fill((int)x, (int)y, (int)(x + width), (int)(y + height), currentColor);
        
        if (mod.isShowBorder()) {
            drawBorder(context, (int)x, (int)y, (int)width, (int)height, borderColor);
        }
        
        String label = box.getLabel();
        int labelWidth = client.textRenderer.getWidth(label);
        int labelX = (int)(x + (width - labelWidth) / 2);
        int labelY = (int)(y + (height - client.textRenderer.fontHeight) / 2);
        
        context.drawTextWithShadow(client.textRenderer, label, labelX, labelY, textColor);
    }
    
    private static void drawBorder(DrawContext context, int x, int y, int width, int height, int color) {
        context.fill(x, y, x + width, y + 1, color);
        context.fill(x, y + height - 1, x + width, y + height, color);
        context.fill(x, y, x + 1, y + height, color);
        context.fill(x + width - 1, y, x + width, y + height, color);
    }
    
    private static int interpolateColor(int color1, int color2, float progress) {
        int a1 = (color1 >> 24) & 0xFF;
        int r1 = (color1 >> 16) & 0xFF;
        int g1 = (color1 >> 8) & 0xFF;
        int b1 = color1 & 0xFF;
        
        int a2 = (color2 >> 24) & 0xFF;
        int r2 = (color2 >> 16) & 0xFF;
        int g2 = (color2 >> 8) & 0xFF;
        int b2 = color2 & 0xFF;
        
        int a = (int)(a1 + (a2 - a1) * progress);
        int r = (int)(r1 + (r2 - r1) * progress);
        int g = (int)(g1 + (g2 - g1) * progress);
        int b = (int)(b1 + (b2 - b1) * progress);
        
        return (a << 24) | (r << 16) | (g << 8) | b;
    }
    
    private static int applyOpacity(int color, float opacity) {
        int alpha = (int)(((color >> 24) & 0xFF) * opacity);
        return (alpha << 24) | (color & 0x00FFFFFF);
    }
    
    private static int getChromaColor(long time, int offset) {
        float hue = ((time + offset) % 3000) / 3000.0f;
        int rgb = java.awt.Color.HSBtoRGB(hue, 1.0f, 1.0f);
        return 0xFF000000 | rgb;
    }
}
