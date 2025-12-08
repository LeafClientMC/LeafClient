// src/main/java/com/leafclient/macro/ChatMacro.java
package com.leafclient.macro;

public class ChatMacro {
    private String name;
    private String message;
    private int key;
    private boolean enabled;
    private boolean allowRepeat;
    private int delayMs;
    private long lastSentTimestamp;
    private boolean wasPressed; // For debouncing
    
    public ChatMacro(String name, String message, int key, boolean enabled, boolean allowRepeat, int delayMs) {
        this.name = name;
        this.message = message;
        this.key = key;
        this.enabled = enabled;
        this.allowRepeat = allowRepeat;
        this.delayMs = delayMs;
        this.lastSentTimestamp = 0;
        this.wasPressed = false;
    }
    
    public String getName() {
        return name;
    }
    
    public void setName(String name) {
        this.name = name;
    }
    
    public String getMessage() {
        return message;
    }
    
    public void setMessage(String message) {
        this.message = message;
    }
    
    public int getKey() {
        return key;
    }
    
    public void setKey(int key) {
        this.key = key;
    }
    
    public boolean isEnabled() {
        return enabled;
    }
    
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
    }
    
    public boolean isAllowRepeat() {
        return allowRepeat;
    }
    
    public void setAllowRepeat(boolean allowRepeat) {
        this.allowRepeat = allowRepeat;
    }
    
    public int getDelayMs() {
        return delayMs;
    }
    
    public void setDelayMs(int delayMs) {
        this.delayMs = delayMs;
    }
    
    public long getLastSentTimestamp() {
        return lastSentTimestamp;
    }
    
    public void setLastSentTimestamp(long lastSentTimestamp) {
        this.lastSentTimestamp = lastSentTimestamp;
    }
    
    public boolean wasPressed() {
        return wasPressed;
    }
    
    public void setWasPressed(boolean wasPressed) {
        this.wasPressed = wasPressed;
    }
    
    public boolean canSend() {
        if (!enabled) return false;
        
        long currentTime = System.currentTimeMillis();
        return (currentTime - lastSentTimestamp) >= delayMs;
    }
}
