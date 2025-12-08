package com.leafclient.ui;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.text.Text;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.util.Identifier;
import com.leafclient.ui.TextureUtil;

import java.awt.*;

public class CustomButton extends ButtonWidget {
    private static final int CORNER_RADIUS = 6;
    private Identifier buttonTexture;
    private Identifier hoveredTexture;

    public CustomButton(int x, int y, int width, int height, Text message, PressAction onPress) {
        super(x, y, width, height, message, onPress, DEFAULT_NARRATION_SUPPLIER);
        generateTextures();
    }

    private void generateTextures() {
        // Generate normal button texture
        this.buttonTexture = TextureUtil.createRoundedRectTexture(
            this.getWidth(), 
            this.getHeight(), 
            new Color(0x004433), // Dark green background
            CORNER_RADIUS
        );
        
        // Generate hovered button texture  
        this.hoveredTexture = TextureUtil.createRoundedRectTexture(
            this.getWidth(),
            this.getHeight(), 
            new Color(0x006644), // Lighter green when hovered
            CORNER_RADIUS
        );
    }

    @Override
    public void renderButton(DrawContext context, int mouseX, int mouseY, float delta) {
        // Use texture-based rendering for better visual quality
        Identifier textureToUse = this.hovered ? hoveredTexture : buttonTexture;
        
        if (textureToUse != null) {
            // Draw the rounded button texture
            context.drawTexture(textureToUse, this.getX(), this.getY(), 0, 0, 
                this.getWidth(), this.getHeight(), this.getWidth(), this.getHeight());
            
            // Draw border
            int borderColor = 0xFF00FF88; // Bright green border
            drawRoundedBorder(context, borderColor);
        } else {
            // Fallback to fill-based rendering if texture generation fails
            renderFallbackButton(context);
        }
        
        // Draw centered text with custom font styling
        int textColor = this.hovered ? 0xFFFFFFFF : 0xFFCCCCCC;
        MinecraftClient client = MinecraftClient.getInstance();
        int textWidth = client.textRenderer.getWidth(this.getMessage());
        int textX = this.getX() + (this.getWidth() - textWidth) / 2;
        int textY = this.getY() + (this.getHeight() - 8) / 2;
        context.drawTextWithShadow(client.textRenderer, this.getMessage(), textX, textY, textColor);
    }

    private void drawRoundedBorder(DrawContext context, int borderColor) {
        // Draw border around the rounded rectangle
        int x = this.getX();
        int y = this.getY();
        int width = this.getWidth();
        int height = this.getHeight();
        
        // Top and bottom borders
        context.fill(x + CORNER_RADIUS, y, x + width - CORNER_RADIUS, y + 1, borderColor);
        context.fill(x + CORNER_RADIUS, y + height - 1, x + width - CORNER_RADIUS, y + height, borderColor);
        
        // Left and right borders  
        context.fill(x, y + CORNER_RADIUS, x + 1, y + height - CORNER_RADIUS, borderColor);
        context.fill(x + width - 1, y + CORNER_RADIUS, x + width, y + height - CORNER_RADIUS, borderColor);
        
        // Corner pixels
        context.fill(x + 1, y + 1, x + CORNER_RADIUS, y + CORNER_RADIUS, borderColor);
        context.fill(x + width - CORNER_RADIUS, y + 1, x + width - 1, y + CORNER_RADIUS, borderColor);
        context.fill(x + 1, y + height - CORNER_RADIUS, x + CORNER_RADIUS, y + height - 1, borderColor);
        context.fill(x + width - CORNER_RADIUS, y + height - CORNER_RADIUS, x + width - 1, y + height, borderColor);
    }

    private void renderFallbackButton(DrawContext context) {
        // Fallback rendering using fills (simulated rounded corners)
        int borderColor = 0xFF00FF88;
        int bgColor = this.hovered ? 0xFF006644 : 0xFF004433;
        
        // Background
        context.fill(this.getX() + 2, this.getY() + 2, 
            this.getX() + this.getWidth() - 2, this.getY() + this.getHeight() - 2, bgColor);
        
        // Border (simulated rounded)
        drawRoundedBorder(context, borderColor);
    }
}