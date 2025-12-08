package com.leafclient.config;

import com.leafclient.ModSettingsFileManager;
import java.util.Map;

/**
 * ConfigManager - Handles loading/saving all configurations
 * 
 * This is a NEW class that wraps ModSettingsFileManager for cleaner architecture.
 */
public class ConfigManager {
    
    public void loadAll() {
        System.out.println("[ConfigManager] Loading all configurations...");
        
        // Load mod settings
        Map<String, Object> settings = ModSettingsFileManager.readModSettings();
        
        System.out.println("[ConfigManager] Loaded " + settings.size() + " configuration entries");
    }
    
    public void saveAll() {
        System.out.println("[ConfigManager] Saving all configurations...");
        // Configuration is auto-saved by ModSettingsFileManager when mods change
    }
    
    public Map<String, Object> getSettings() {
        return ModSettingsFileManager.readModSettings();
    }
    
    public void setSettings(Map<String, Object> settings) {
        ModSettingsFileManager.writeModSettings(settings);
    }
}
