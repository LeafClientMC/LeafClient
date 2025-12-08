package com.leafclient.ui;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.text.Text;

public class CustomButtonWidget extends ButtonWidget {
    
    private boolean isHovered = false;
    private static final int CORNER_RADIUS = 3;
    
    private CustomButtonWidget(int x, int y, int width, int height, Text message, PressAction onPress) {
        super(x, y, width, height, message, onPress, DEFAULT_NARRATION_SUPPLIER);
    }
    
    public static CustomBuilder customBuilder(Text message, PressAction onPress) {
        return new CustomBuilder(message, onPress);
    }
    
    @Override
    public void renderButton(DrawContext context, int mouseX, int mouseY, float delta) {
        this.isHovered = mouseX >= this.getX() && mouseY >= this.getY() 
                      && mouseX < this.getX() + this.width 
                      && mouseY < this.getY() + this.height;
        
        int x = this.getX();
        int y = this.getY();
        int width = this.width;
        int height = this.height;
        
        // Draw rounded rectangle with borders
        drawRoundedRect(context, x, y, width, height);
        
        // Draw text
        int textColor = this.active ? (isHovered ? 0xFFFFFFFF : 0xFFAAAAAA) : 0xFF555555;
        context.drawCenteredTextWithShadow(
            MinecraftClient.getInstance().textRenderer, 
            this.getMessage(), 
            x + width / 2, 
            y + (height - 8) / 2, 
            textColor
        );
    }
    
    private void drawRoundedRect(DrawContext context, int x, int y, int width, int height) {
        // Determine colors based on state
        int outerBorderColor = 0xAA5A5A5A; // Semi-transparent outer border
        int innerBorderColor = 0xAA2A2A2A; // Semi-transparent inner border
        int backgroundColor;
        
        if (isHovered && this.active) {
            backgroundColor = 0xAA3A3A3A; // Semi-transparent hover
        } else if (!this.active) {
            backgroundColor = 0xAA1A1A1A; // Semi-transparent disabled
        } else {
            backgroundColor = 0xAA2A2A2A; // Semi-transparent normal
        }
        
        // Draw main background with rounded corners
        fillRoundedRect(context, x + 2, y + 2, width - 4, height - 4, CORNER_RADIUS, backgroundColor);
        
        // Draw outer border
        drawRoundedBorder(context, x, y, width, height, CORNER_RADIUS, outerBorderColor, 1);
        
        // Draw inner border
        drawRoundedBorder(context, x + 1, y + 1, width - 2, height - 2, CORNER_RADIUS - 1, innerBorderColor, 1);
    }
    
    private void fillRoundedRect(DrawContext context, int x, int y, int width, int height, int radius, int color) {
        // Fill center rectangle
        context.fill(x + radius, y, x + width - radius, y + height, color);
        context.fill(x, y + radius, x + radius, y + height - radius, color);
        context.fill(x + width - radius, y + radius, x + width, y + height - radius, color);
        
        // Draw rounded corners
        drawRoundedCorner(context, x, y, radius, color, 0); // Top-left
        drawRoundedCorner(context, x + width - radius, y, radius, color, 1); // Top-right
        drawRoundedCorner(context, x, y + height - radius, radius, color, 2); // Bottom-left
        drawRoundedCorner(context, x + width - radius, y + height - radius, radius, color, 3); // Bottom-right
    }
    
    private void drawRoundedCorner(DrawContext context, int x, int y, int radius, int color, int corner) {
        for (int dx = 0; dx < radius; dx++) {
            for (int dy = 0; dy < radius; dy++) {
                double distance = Math.sqrt(dx * dx + dy * dy);
                if (distance <= radius) {
                    int px = x, py = y;
                    
                    switch (corner) {
                        case 0: // Top-left
                            px = x + (radius - dx);
                            py = y + (radius - dy);
                            break;
                        case 1: // Top-right
                            px = x + dx;
                            py = y + (radius - dy);
                            break;
                        case 2: // Bottom-left
                            px = x + (radius - dx);
                            py = y + dy;
                            break;
                        case 3: // Bottom-right
                            px = x + dx;
                            py = y + dy;
                            break;
                    }
                    
                    context.fill(px, py, px + 1, py + 1, color);
                }
            }
        }
    }
    
    private void drawRoundedBorder(DrawContext context, int x, int y, int width, int height, int radius, int color, int thickness) {
        // Top border
        context.fill(x + radius, y, x + width - radius, y + thickness, color);
        // Bottom border
        context.fill(x + radius, y + height - thickness, x + width - radius, y + height, color);
        // Left border
        context.fill(x, y + radius, x + thickness, y + height - radius, color);
        // Right border
        context.fill(x + width - thickness, y + radius, x + width, y + height - radius, color);
        
        // Draw border corners (simplified)
        for (int i = 0; i < thickness; i++) {
            drawRoundedCorner(context, x + i, y + i, radius - i, color, 0);
            drawRoundedCorner(context, x + width - radius - i, y + i, radius - i, color, 1);
            drawRoundedCorner(context, x + i, y + height - radius - i, radius - i, color, 2);
            drawRoundedCorner(context, x + width - radius - i, y + height - radius - i, radius - i, color, 3);
        }
    }
    
    public static class CustomBuilder {
        private final Text message;
        private final PressAction onPress;
        private int x;
        private int y;
        private int width = 150;
        private int height = 20;
        
        public CustomBuilder(Text message, PressAction onPress) {
            this.message = message;
            this.onPress = onPress;
        }
        
        public CustomBuilder dimensions(int x, int y, int width, int height) {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            return this;
        }
        
        public CustomButtonWidget build() {
            return new CustomButtonWidget(x, y, width, height, message, onPress);
        }
    }
}