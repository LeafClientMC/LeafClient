package com.leafclient.replay;

public class ReplaySettings {
    
    // Recording settings
    private boolean recordEntities = true;
    private boolean recordParticles = false; // Changed default to false
    private boolean recordSounds = false; // Changed default to false
    private boolean recordBlockUpdates = true;
    private boolean recordPackets = false; // Changed default to false
    
    // Performance settings
    private int recordInterval = 2; // Record every N ticks (2 = 10fps, 1 = 20fps)
    private double entityCullDistance = 64.0; // Only record entities within this distance
    private int maxEntitiesPerTick = 50; // Maximum entities to record per tick
    
    // Playback settings
    private float playbackSpeed = 1.0f;
    private boolean smoothCamera = true;
    private float cameraSpeed = 0.5f;
    private float cameraSensitivity = 0.15f;
    
    // Export settings
    private int exportFps = 60;
    private int exportWidth = 1920;
    private int exportHeight = 1080;
    
    private static ReplaySettings instance;
    
    private ReplaySettings() {}
    
    public static ReplaySettings getInstance() {
        if (instance == null) {
            instance = new ReplaySettings();
        }
        return instance;
    }
    
    // Performance presets
    public void setPerformancePreset(PerformancePreset preset) {
        switch (preset) {
            case MAXIMUM_PERFORMANCE:
                recordEntities = false;
                recordParticles = false;
                recordSounds = false;
                recordBlockUpdates = false;
                recordPackets = false;
                recordInterval = 4; // 5 fps
                break;
            case BALANCED:
                recordEntities = true;
                recordParticles = false;
                recordSounds = false;
                recordBlockUpdates = true;
                recordPackets = false;
                recordInterval = 2; // 10 fps
                entityCullDistance = 64.0;
                maxEntitiesPerTick = 50;
                break;
            case MAXIMUM_QUALITY:
                recordEntities = true;
                recordParticles = true;
                recordSounds = true;
                recordBlockUpdates = true;
                recordPackets = true;
                recordInterval = 1; // 20 fps
                entityCullDistance = 128.0;
                maxEntitiesPerTick = 200;
                break;
        }
    }
    
    public enum PerformancePreset {
        MAXIMUM_PERFORMANCE,
        BALANCED,
        MAXIMUM_QUALITY
    }
    
    // Getters and setters
    public boolean isRecordEntities() {
        return recordEntities;
    }
    
    public void setRecordEntities(boolean recordEntities) {
        this.recordEntities = recordEntities;
    }
    
    public boolean isRecordParticles() {
        return recordParticles;
    }
    
    public void setRecordParticles(boolean recordParticles) {
        this.recordParticles = recordParticles;
    }
    
    public boolean isRecordSounds() {
        return recordSounds;
    }
    
    public void setRecordSounds(boolean recordSounds) {
        this.recordSounds = recordSounds;
    }
    
    public boolean isRecordBlockUpdates() {
        return recordBlockUpdates;
    }
    
    public void setRecordBlockUpdates(boolean recordBlockUpdates) {
        this.recordBlockUpdates = recordBlockUpdates;
    }
    
    public boolean isRecordPackets() {
        return recordPackets;
    }
    
    public void setRecordPackets(boolean recordPackets) {
        this.recordPackets = recordPackets;
    }
    
    public int getRecordInterval() {
        return recordInterval;
    }
    
    public void setRecordInterval(int interval) {
        this.recordInterval = Math.max(1, Math.min(10, interval));
    }
    
    public double getEntityCullDistance() {
        return entityCullDistance;
    }
    
    public void setEntityCullDistance(double distance) {
        this.entityCullDistance = Math.max(16.0, Math.min(256.0, distance));
    }
    
    public int getMaxEntitiesPerTick() {
        return maxEntitiesPerTick;
    }
    
    public void setMaxEntitiesPerTick(int max) {
        this.maxEntitiesPerTick = Math.max(10, Math.min(500, max));
    }
    
    public float getPlaybackSpeed() {
        return playbackSpeed;
    }
    
    public void setPlaybackSpeed(float speed) {
        this.playbackSpeed = Math.max(0.1f, Math.min(10.0f, speed));
    }
    
    public boolean isSmoothCamera() {
        return smoothCamera;
    }
    
    public void setSmoothCamera(boolean smooth) {
        this.smoothCamera = smooth;
    }
    
    public float getCameraSpeed() {
        return cameraSpeed;
    }
    
    public void setCameraSpeed(float speed) {
        this.cameraSpeed = Math.max(0.1f, Math.min(5.0f, speed));
    }
    
    public float getCameraSensitivity() {
        return cameraSensitivity;
    }
    
    public void setCameraSensitivity(float sensitivity) {
        this.cameraSensitivity = Math.max(0.01f, Math.min(1.0f, sensitivity));
    }
    
    public int getExportFps() {
        return exportFps;
    }
    
    public void setExportFps(int fps) {
        this.exportFps = Math.max(20, Math.min(120, fps));
    }
    
    public int getExportWidth() {
        return exportWidth;
    }
    
    public void setExportWidth(int width) {
        this.exportWidth = Math.max(640, Math.min(7680, width));
    }
    
    public int getExportHeight() {
        return exportHeight;
    }
    
    public void setExportHeight(int height) {
        this.exportHeight = Math.max(480, Math.min(4320, height));
    }
}
