package com.leafclient.util;

import com.mojang.blaze3d.systems.RenderSystem;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.texture.NativeImage;
import net.minecraft.client.texture.NativeImageBackedTexture;
import net.minecraft.util.Identifier;

import java.awt.*;
import java.awt.image.BufferedImage;
import java.io.InputStream;
import java.util.HashMap;
import java.util.Map;

public class CustomFontRenderer {

    private Font font;
    private final Map<Character, GlyphData> glyphCache = new HashMap<>();
    private Identifier textureId;
    private boolean ready = false;

    private static final int ATLAS_SIZE = 1024;
    private BufferedImage textureImage;
    private int currentX = 0;
    private int currentY = 0;
    private int rowHeight = 0;
    private int fontHeight;
    private int fontAscent;

    public CustomFontRenderer(String fontPath, float size) {
        loadFont(fontPath, size);
    }

    private void loadFont(String fontPath, float size) {
        try {
            System.out.println("[FontRenderer] Loading font from: " + fontPath);
            
            // Parse the identifier first
            final Identifier fontId;
            if (fontPath.contains(":")) {
                String[] parts = fontPath.split(":", 2);
                fontId = new Identifier(parts[0], parts[1]);
            } else {
                fontId = new Identifier("leafclient", fontPath);
            }
            
            System.out.println("[FontRenderer] Font ID: " + fontId);
            
            // Get the resource
            try (InputStream stream = MinecraftClient.getInstance()
                    .getResourceManager()
                    .getResource(fontId)
                    .orElseThrow(() -> new RuntimeException("Resource not found: " + fontId))
                    .getInputStream()) {
                
                System.out.println("[FontRenderer] Font stream obtained successfully");
                
                // Load the font
                Font baseFont = Font.createFont(Font.TRUETYPE_FONT, stream);
                this.font = baseFont.deriveFont(Font.PLAIN, size);
                
                // Get font metrics
                BufferedImage tempImg = new BufferedImage(1, 1, BufferedImage.TYPE_INT_ARGB);
                Graphics2D tempG = tempImg.createGraphics();
                tempG.setFont(font);
                FontMetrics metrics = tempG.getFontMetrics();
                
                fontHeight = metrics.getHeight();
                fontAscent = metrics.getAscent();
                
                System.out.println(String.format(
                    "[FontRenderer] Font metrics: height=%d, ascent=%d, descent=%d",
                    fontHeight, fontAscent, metrics.getDescent()
                ));
                
                tempG.dispose();
                
                // Initialize texture atlas
                initializeTextureAtlas(metrics);
                
                ready = true;
                System.out.println("[FontRenderer] Font loaded and ready");
                
            }
        } catch (Exception e) {
            System.err.println("[FontRenderer] Failed to load font: " + fontPath);
            e.printStackTrace();
        }
    }

    private void initializeTextureAtlas(FontMetrics metrics) {
        // Create texture image
        textureImage = new BufferedImage(ATLAS_SIZE, ATLAS_SIZE, BufferedImage.TYPE_INT_ARGB);
        Graphics2D g = textureImage.createGraphics();
        
        // Set rendering hints for better quality
        g.setRenderingHint(RenderingHints.KEY_TEXT_ANTIALIASING, RenderingHints.VALUE_TEXT_ANTIALIAS_ON);
        g.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
        g.setRenderingHint(RenderingHints.KEY_RENDERING, RenderingHints.VALUE_RENDER_QUALITY);
        g.setFont(font);
        g.setColor(Color.WHITE);
        
        // Cache basic ASCII characters first
        for (char c = 32; c < 128; c++) {
            cacheGlyph(g, c, metrics);
        }
        
        // Also cache common symbols
        String symbols = "[]{}()<>:;,!?.\"\'/\\|@#$%^&*-_=+`~";
        for (char c : symbols.toCharArray()) {
            if (!glyphCache.containsKey(c)) {
                cacheGlyph(g, c, metrics);
            }
        }
        
        g.dispose();
        
        // Upload texture to GPU
        uploadTexture();
    }

    private void cacheGlyph(Graphics2D g, char c, FontMetrics metrics) {
        String charStr = String.valueOf(c);
        int charWidth = metrics.charWidth(c);
        int charHeight = metrics.getHeight();
        
        // Skip empty or invalid glyphs
        if (charWidth <= 0 || charHeight <= 0) {
            return;
        }
        
        // Add 1px padding around each character
        int padding = 1;
        int width = charWidth + padding * 2;
        int height = charHeight + padding * 2;
        
        // Check if we need to move to next row
        if (currentX + width > ATLAS_SIZE) {
            currentX = 0;
            currentY += rowHeight + padding;
            rowHeight = 0;
        }
        
        // Check if we've run out of space
        if (currentY + height > ATLAS_SIZE) {
            System.err.println("[FontRenderer] Texture atlas full! Could not cache character: " + c);
            return;
        }
        
        // Draw the character
        g.drawString(charStr, currentX + padding, currentY + padding + metrics.getAscent());
        
        // Store glyph data
        glyphCache.put(c, new GlyphData(
            currentX + padding,
            currentY + padding,
            width - padding * 2,
            height - padding * 2,
            charWidth
        ));
        
        // Update position
        currentX += width;
        rowHeight = Math.max(rowHeight, height);
    }

    private void uploadTexture() {
        if (textureImage == null) {
            return;
        }
        
        try {
            int width = textureImage.getWidth();
            int height = textureImage.getHeight();
            
            NativeImage nativeImage = new NativeImage(width, height, false);
            
            // Copy pixel data from BufferedImage to NativeImage
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int argb = textureImage.getRGB(x, y);
                    
                    // Extract components
                    int a = (argb >> 24) & 0xFF;
                    int r = (argb >> 16) & 0xFF;
                    int g = (argb >> 8) & 0xFF;
                    int b = argb & 0xFF;
                    
                    // Convert to ABGR (Minecraft's format)
                    int abgr = (a << 24) | (b << 16) | (g << 8) | r;
                    nativeImage.setColor(x, y, abgr);
                }
            }
            
            // Create and register texture
            NativeImageBackedTexture texture = new NativeImageBackedTexture(nativeImage);
            textureId = MinecraftClient.getInstance().getTextureManager().registerDynamicTexture("leaf_font", texture);
            
            System.out.println("[FontRenderer] Texture uploaded with ID: " + textureId);
            
        } catch (Exception e) {
            System.err.println("[FontRenderer] Failed to upload texture");
            e.printStackTrace();
        }
    }

    public void drawString(DrawContext context, String text, float x, float y, int color) {
        if (text == null || text.isEmpty() || !ready || textureId == null) {
            // Fallback to default renderer
            context.drawText(MinecraftClient.getInstance().textRenderer, text, (int) x, (int) y, color, false);
            return;
        }
        
        float drawX = x;
        float drawY = y;
        
        // Enable blending for transparency
        RenderSystem.enableBlend();
        RenderSystem.defaultBlendFunc();
        
        // Set color
        float alpha = (float) ((color >> 24) & 0xFF) / 255.0F;
        float red = (float) ((color >> 16) & 0xFF) / 255.0F;
        float green = (float) ((color >> 8) & 0xFF) / 255.0F;
        float blue = (float) (color & 0xFF) / 255.0F;
        
        RenderSystem.setShaderColor(red, green, blue, alpha);
        
        for (char c : text.toCharArray()) {
            GlyphData glyph = glyphCache.get(c);
            
            if (glyph == null) {
                // If character not cached, try to use space or skip
                glyph = glyphCache.get(' ');
                if (glyph == null) {
                    continue;
                }
            }
            
            // Draw the glyph
            context.drawTexture(
                textureId,
                (int) drawX,
                (int) drawY,
                0,
                glyph.u,
                glyph.v,
                glyph.width,
                glyph.height,
                ATLAS_SIZE,
                ATLAS_SIZE
            );
            
            drawX += glyph.advance;
        }
        
        // Reset color and disable blending
        RenderSystem.setShaderColor(1.0F, 1.0F, 1.0F, 1.0F);
        RenderSystem.disableBlend();
    }

    public int getStringWidth(String text) {
        if (text == null || !ready) {
            return MinecraftClient.getInstance().textRenderer.getWidth(text);
        }
        
        int width = 0;
        for (char c : text.toCharArray()) {
            GlyphData glyph = glyphCache.get(c);
            if (glyph != null) {
                width += glyph.advance;
            } else {
                // Add width of space for missing characters
                GlyphData space = glyphCache.get(' ');
                if (space != null) {
                    width += space.advance;
                }
            }
        }
        return width;
    }

    public int getFontHeight() {
        return fontHeight;
    }

    public boolean isReady() {
        return ready;
    }

    private static class GlyphData {
        final int u, v;
        final int width, height;
        final int advance;
        
        GlyphData(int u, int v, int width, int height, int advance) {
            this.u = u;
            this.v = v;
            this.width = width;
            this.height = height;
            this.advance = advance;
        }
    }
}