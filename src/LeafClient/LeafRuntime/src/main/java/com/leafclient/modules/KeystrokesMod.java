package com.leafclient.modules;

import com.leafclient.ui.KeystrokeBox;
import java.util.HashMap;
import java.util.Map;

public class KeystrokesMod implements ILeafMod {
    private boolean enabled = false;
    private Map<String, KeystrokeBox> keystrokeBoxes = new HashMap<>();
    
    // Customization settings
    private int textColor = 0xFFFFFF;
    private int backgroundColor = 0x90000000;
    private int pressedColor = 0x90FFFFFF;
    private int borderColor = 0xFFFFFFFF;
    private float opacity = 0.9f;
    private float scale = 1.0f;
    private boolean showAnimations = true;
    private boolean chromaMode = false;
    private boolean showBorder = true;
    
    // Position settings
    private float baseX = 10;
    private float baseY = 10;
    
    public KeystrokesMod() {
        initializeKeystrokeBoxes();
    }
    
    private void initializeKeystrokeBoxes() {
        float boxSize = 30;
        float spacing = 2;
        float largeWidth = (boxSize * 3) + (spacing * 2);
        
        // W key (top center)
        keystrokeBoxes.put("W", new KeystrokeBox("W", boxSize + spacing, 0, boxSize, boxSize));
        
        // A, S, D keys (middle row)
        keystrokeBoxes.put("A", new KeystrokeBox("A", 0, boxSize + spacing, boxSize, boxSize));
        keystrokeBoxes.put("S", new KeystrokeBox("S", boxSize + spacing, boxSize + spacing, boxSize, boxSize));
        keystrokeBoxes.put("D", new KeystrokeBox("D", (boxSize + spacing) * 2, boxSize + spacing, boxSize, boxSize));
        
        // Spacebar (bottom, full width)
        keystrokeBoxes.put("SPACE", new KeystrokeBox("---", 0, (boxSize + spacing) * 2, largeWidth, boxSize));
        
        // Mouse buttons (below spacebar)
        float mouseButtonWidth = (largeWidth - spacing) / 2;
        keystrokeBoxes.put("LMB", new KeystrokeBox("LMB", 0, (boxSize + spacing) * 3, mouseButtonWidth, boxSize));
        keystrokeBoxes.put("RMB", new KeystrokeBox("RMB", mouseButtonWidth + spacing, (boxSize + spacing) * 3, mouseButtonWidth, boxSize));
    }
    
    @Override
    public boolean isEnabled() {
        return enabled;
    }
    
    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] Keystrokes " + (enabled ? "enabled" : "disabled"));
    }
    
    @Override
    public String getName() {
        return "Keystrokes";
    }
    
    public Map<String, KeystrokeBox> getKeystrokeBoxes() {
        return keystrokeBoxes;
    }
    
    public KeystrokeBox getBox(String key) {
        return keystrokeBoxes.get(key);
    }
    
    public void updateAllAnimations() {
        for (KeystrokeBox box : keystrokeBoxes.values()) {
            box.updateAnimation();
        }
    }
    
    // Getters and setters for customization
    public int getTextColor() {
        return textColor;
    }
    
    public void setTextColor(int textColor) {
        this.textColor = textColor;
    }
    
    public int getBackgroundColor() {
        return backgroundColor;
    }
    
    public void setBackgroundColor(int backgroundColor) {
        this.backgroundColor = backgroundColor;
    }
    
    public int getPressedColor() {
        return pressedColor;
    }
    
    public void setPressedColor(int pressedColor) {
        this.pressedColor = pressedColor;
    }
    
    public int getBorderColor() {
        return borderColor;
    }
    
    public void setBorderColor(int borderColor) {
        this.borderColor = borderColor;
    }
    
    public float getOpacity() {
        return opacity;
    }
    
    public void setOpacity(float opacity) {
        this.opacity = Math.max(0.0f, Math.min(1.0f, opacity));
    }
    
    public float getScale() {
        return scale;
    }
    
    public void setScale(float scale) {
        this.scale = Math.max(0.5f, Math.min(3.0f, scale));
    }
    
    public boolean isShowAnimations() {
        return showAnimations;
    }
    
    public void setShowAnimations(boolean showAnimations) {
        this.showAnimations = showAnimations;
    }
    
    public boolean isChromaMode() {
        return chromaMode;
    }
    
    public void setChromaMode(boolean chromaMode) {
        this.chromaMode = chromaMode;
    }
    
    public boolean isShowBorder() {
        return showBorder;
    }
    
    public void setShowBorder(boolean showBorder) {
        this.showBorder = showBorder;
    }
    
    public float getBaseX() {
        return baseX;
    }
    
    public void setBaseX(float baseX) {
        this.baseX = baseX;
    }
    
    public float getBaseY() {
        return baseY;
    }
    
    public void setBaseY(float baseY) {
        this.baseY = baseY;
    }
}
