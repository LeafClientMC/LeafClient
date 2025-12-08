package com.leafclient.ui;

public class KeystrokeBox {
    private String label;
    private float x;
    private float y;
    private float width;
    private float height;
    private KeyState keyState;
    
    public KeystrokeBox(String label, float x, float y, float width, float height) {
        this.label = label;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        this.keyState = new KeyState();
    }
    
    public String getLabel() {
        return label;
    }
    
    public float getX() {
        return x;
    }
    
    public void setX(float x) {
        this.x = x;
    }
    
    public float getY() {
        return y;
    }
    
    public void setY(float y) {
        this.y = y;
    }
    
    public float getWidth() {
        return width;
    }
    
    public void setWidth(float width) {
        this.width = width;
    }
    
    public float getHeight() {
        return height;
    }
    
    public void setHeight(float height) {
        this.height = height;
    }
    
    public KeyState getKeyState() {
        return keyState;
    }
    
    public boolean isPressed() {
        return keyState.isPressed();
    }
    
    public void setPressed(boolean pressed) {
        keyState.setPressed(pressed);
    }
    
    public void updateAnimation() {
        keyState.updateAnimation();
    }
    
    public boolean isMouseOver(double mouseX, double mouseY) {
        return mouseX >= x && mouseX <= x + width && mouseY >= y && mouseY <= y + height;
    }
}
