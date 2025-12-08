package com.leafclient.ui;

public class KeyState {
    private boolean pressed;
    private long lastPressTime;
    private long lastReleaseTime;
    private float animationProgress;
    
    public KeyState() {
        this.pressed = false;
        this.lastPressTime = 0;
        this.lastReleaseTime = 0;
        this.animationProgress = 0.0f;
    }
    
    public boolean isPressed() {
        return pressed;
    }
    
    public void setPressed(boolean pressed) {
        if (pressed && !this.pressed) {
            this.lastPressTime = System.currentTimeMillis();
        } else if (!pressed && this.pressed) {
            this.lastReleaseTime = System.currentTimeMillis();
        }
        this.pressed = pressed;
    }
    
    public long getLastPressTime() {
        return lastPressTime;
    }
    
    public long getLastReleaseTime() {
        return lastReleaseTime;
    }
    
    public float getAnimationProgress() {
        return animationProgress;
    }
    
    public void setAnimationProgress(float progress) {
        this.animationProgress = Math.max(0.0f, Math.min(1.0f, progress));
    }
    
    public void updateAnimation() {
        float animationSpeed = 0.15f;
        
        if (pressed) {
            animationProgress = Math.min(1.0f, animationProgress + animationSpeed);
        } else {
            animationProgress = Math.max(0.0f, animationProgress - animationSpeed);
        }
    }
}
