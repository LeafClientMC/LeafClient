package com.leafclient.modules;

import com.leafclient.ModSettingsFileManager;
import com.leafclient.macro.ChatMacro;
import net.minecraft.client.MinecraftClient;

import java.util.*;

public class ChatMacrosMod implements ILeafMod {
    private boolean enabled = false;
    private List<ChatMacro> macros = new ArrayList<>();
    
    public ChatMacrosMod() {
        loadMacros();
    }

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] ChatMacros " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "ChatMacros";
    }
    
    public List<ChatMacro> getMacros() {
        return macros;
    }
    
    public void addMacro(ChatMacro macro) {
        macros.add(macro);
        saveMacros();
    }
    
    public void removeMacro(ChatMacro macro) {
        macros.remove(macro);
        saveMacros();
    }
    
    public void updateMacro(int index, ChatMacro macro) {
        if (index >= 0 && index < macros.size()) {
            macros.set(index, macro);
            saveMacros();
        }
    }
    
    public void clearMacros() {
        macros.clear();
        saveMacros();
    }
    
    @SuppressWarnings("unchecked")
    public void loadMacros() {
        Map<String, Object> settings = ModSettingsFileManager.readModSettings();
        
        if (settings.containsKey("CHATMACROS")) {
            Map<String, Object> macroSettings = (Map<String, Object>) settings.get("CHATMACROS");
            
            if (macroSettings.containsKey("enabled")) {
                this.enabled = (Boolean) macroSettings.get("enabled");
            }
            
            if (macroSettings.containsKey("macros")) {
                List<Map<String, Object>> macroList = (List<Map<String, Object>>) macroSettings.get("macros");
                macros.clear();
                
                for (Map<String, Object> macroData : macroList) {
                    String name = (String) macroData.get("name");
                    String message = (String) macroData.get("message");
                    int key = ((Number) macroData.get("key")).intValue();
                    boolean macroEnabled = (Boolean) macroData.getOrDefault("enabled", true);
                    boolean allowRepeat = (Boolean) macroData.getOrDefault("allowRepeat", false);
                    int delayMs = ((Number) macroData.getOrDefault("delayMs", 0)).intValue();
                    
                    macros.add(new ChatMacro(name, message, key, macroEnabled, allowRepeat, delayMs));
                }
            }
        }
    }
    
    public void saveMacros() {
        Map<String, Object> settings = ModSettingsFileManager.readModSettings();
        
        Map<String, Object> macroSettings = new LinkedHashMap<>();
        macroSettings.put("enabled", enabled);
        
        List<Map<String, Object>> macroList = new ArrayList<>();
        for (ChatMacro macro : macros) {
            Map<String, Object> macroData = new LinkedHashMap<>();
            macroData.put("name", macro.getName());
            macroData.put("message", macro.getMessage());
            macroData.put("key", macro.getKey());
            macroData.put("enabled", macro.isEnabled());
            macroData.put("allowRepeat", macro.isAllowRepeat());
            macroData.put("delayMs", macro.getDelayMs());
            macroList.add(macroData);
        }
        
        macroSettings.put("macros", macroList);
        settings.put("CHATMACROS", macroSettings);
        
        ModSettingsFileManager.writeModSettings(settings);
    }
    
    public String applyPlaceholders(String message) {
        MinecraftClient client = MinecraftClient.getInstance();
        if (client.player == null) return message;
        
        String result = message;
        
        // %player%
        result = result.replace("%player%", client.player.getName().getString());
        
        // %x% %y% %z%
        result = result.replace("%x%", String.valueOf((int) client.player.getX()));
        result = result.replace("%y%", String.valueOf((int) client.player.getY()));
        result = result.replace("%z%", String.valueOf((int) client.player.getZ()));
        
        // %health%
        result = result.replace("%health%", String.format("%.1f", client.player.getHealth()));
        
        // %item%
        if (!client.player.getMainHandStack().isEmpty()) {
            result = result.replace("%item%", client.player.getMainHandStack().getName().getString());
        } else {
            result = result.replace("%item%", "Empty");
        }
        
        return result;
    }
}
