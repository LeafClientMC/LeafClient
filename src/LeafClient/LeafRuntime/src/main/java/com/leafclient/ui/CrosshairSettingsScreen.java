package com.leafclient.ui;

import com.leafclient.modules.CrosshairSettings;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.text.Text;

import java.awt.Color;

public class CrosshairSettingsScreen extends Screen {
    private final CrosshairSettings settings;
    private final Screen parent;

    public CrosshairSettingsScreen(CrosshairSettings settings) {
        super(Text.of("Crosshair Settings"));
        this.settings = settings;
        this.parent = null;
    }

    @Override
    protected void init() {
        super.init();

        int buttonWidth = 200;
        int buttonHeight = 20;
        int centerX = this.width / 2 - buttonWidth / 2;
        int startY = this.height / 4;

        // Color button
        this.addDrawableChild(CustomButtonWidget.customBuilder(
            Text.of("Color: " + settings.color.getRed() + ", " + 
                   settings.color.getGreen() + ", " + settings.color.getBlue()), 
            (button) -> {
                // Cycle through preset colors
                if (settings.color.equals(Color.WHITE)) {
                    settings.color = Color.RED;
                } else if (settings.color.equals(Color.RED)) {
                    settings.color = Color.GREEN;
                } else if (settings.color.equals(Color.GREEN)) {
                    settings.color = Color.BLUE;
                } else if (settings.color.equals(Color.BLUE)) {
                    settings.color = Color.YELLOW;
                } else if (settings.color.equals(Color.YELLOW)) {
                    settings.color = Color.CYAN;
                } else if (settings.color.equals(Color.CYAN)) {
                    settings.color = Color.MAGENTA;
                } else {
                    settings.color = Color.WHITE;
                }
                button.setMessage(Text.of("Color: " + settings.color.getRed() + ", " + 
                                         settings.color.getGreen() + ", " + 
                                         settings.color.getBlue()));
            }
        ).dimensions(centerX, startY, buttonWidth, buttonHeight).build());

        // Size button
        this.addDrawableChild(CustomButtonWidget.customBuilder(
            Text.of("Size: " + settings.size), 
            (button) -> {
                settings.size = (settings.size + 1) % 16;
                button.setMessage(Text.of("Size: " + settings.size));
            }
        ).dimensions(centerX, startY + 24, buttonWidth, buttonHeight).build());

        // Gap button
        this.addDrawableChild(CustomButtonWidget.customBuilder(
            Text.of("Gap: " + settings.gap), 
            (button) -> {
                settings.gap = (settings.gap + 1) % 11;
                button.setMessage(Text.of("Gap: " + settings.gap));
            }
        ).dimensions(centerX, startY + 48, buttonWidth, buttonHeight).build());

        // Dot toggle button
        this.addDrawableChild(CustomButtonWidget.customBuilder(
            Text.of("Dot: " + (settings.dot ? "Enabled" : "Disabled")), 
            (button) -> {
                settings.dot = !settings.dot;
                button.setMessage(Text.of("Dot: " + (settings.dot ? "Enabled" : "Disabled")));
            }
        ).dimensions(centerX, startY + 72, buttonWidth, buttonHeight).build());

        // Save and Close button
        this.addDrawableChild(CustomButtonWidget.customBuilder(
            Text.of("Save and Close"), 
            (button) -> {
                settings.save();
                this.close();
            }
        ).dimensions(centerX, startY + 96, buttonWidth, buttonHeight).build());
    }

    @Override
    public void render(DrawContext context, int mouseX, int mouseY, float delta) {
        renderBackground(context);
        
        context.drawCenteredTextWithShadow(this.textRenderer, this.title, 
                                          this.width / 2, 15, 0xFFFFFF);
        
        // Draw preview crosshair
        renderPreviewCrosshair(context);
        
        super.render(context, mouseX, mouseY, delta);
    }
    
    private void renderPreviewCrosshair(DrawContext context) {
        int centerX = this.width / 2;
        int centerY = this.height / 4 + 140;
        
        // Convert AWT Color to ARGB int
        int color = 0xFF000000 | 
                   (settings.color.getRed() << 16) | 
                   (settings.color.getGreen() << 8) | 
                   settings.color.getBlue();
        
        int size = settings.size;
        int gap = settings.gap;
        int thickness = 1;
        
        // Draw background box
        context.fill(centerX - 30, centerY - 30, centerX + 30, centerY + 30, 0x80000000);
        
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
        
        // Draw center dot if enabled
        if (settings.dot) {
            context.fill(centerX, centerY, centerX + 1, centerY + 1, color);
        }
        
        // Label
        context.drawCenteredTextWithShadow(this.textRenderer, 
                                          Text.of("Preview"), 
                                          centerX, centerY + 35, 0xAAAAAA);
    }

    @Override
    public void close() {
        if (this.client != null) {
            this.client.setScreen(parent);
        }
    }
}