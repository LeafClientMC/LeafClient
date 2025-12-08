package com.leafclient.modules;

import com.leafclient.ModSettingsFileManager;
import net.minecraft.item.Item;
import net.minecraft.registry.Registries;
import net.minecraft.util.Identifier;

import java.util.*;

public class ItemCounterMod implements ILeafMod {
    private boolean enabled = false;
    
    // Configuration
    private boolean autoDetect = false;
    private boolean showIcons = true;
    private boolean showAnimations = true;
    private boolean horizontalLayout = true;
    private float scale = 1.0f;
    private float opacity = 0.9f;
    private int textColor = 0xFFFFFF;
    private int backgroundColor = 0x90000000;
    private int iconSize = 16;
    private int spacing = 4;
    
    // Tracked items (whitelist mode)
    private Set<String> trackedItemIds = new LinkedHashSet<>();
    
    // Current item counts
    private Map<Item, Integer> itemCounts = new LinkedHashMap<>();
    
    public ItemCounterMod() {
        // Default tracked items
        trackedItemIds.add("minecraft:ender_pearl");
        trackedItemIds.add("minecraft:golden_apple");
        trackedItemIds.add("minecraft:enchanted_golden_apple");
        trackedItemIds.add("minecraft:cobblestone");
        trackedItemIds.add("minecraft:snowball");
        trackedItemIds.add("minecraft:arrow");
        trackedItemIds.add("minecraft:oak_planks");
        
        // Load settings from file
        loadSettings();
    }

    @SuppressWarnings("unchecked")
    public void loadSettings() {
        Map<String, Object> settings = ModSettingsFileManager.readModSettings();
        
        if (settings.containsKey("ITEMCOUNTER")) {
            Map<String, Object> itemCounterSettings = (Map<String, Object>) settings.get("ITEMCOUNTER");
            
            if (itemCounterSettings.containsKey("enabled")) {
                this.enabled = (Boolean) itemCounterSettings.get("enabled");
            }
            
            if (itemCounterSettings.containsKey("autoDetect")) {
                this.autoDetect = (Boolean) itemCounterSettings.get("autoDetect");
            }
            
            if (itemCounterSettings.containsKey("showIcons")) {
                this.showIcons = (Boolean) itemCounterSettings.get("showIcons");
            }
            
            if (itemCounterSettings.containsKey("showAnimations")) {
                this.showAnimations = (Boolean) itemCounterSettings.get("showAnimations");
            }
            
            if (itemCounterSettings.containsKey("horizontalLayout")) {
                this.horizontalLayout = (Boolean) itemCounterSettings.get("horizontalLayout");
            }
            
            if (itemCounterSettings.containsKey("scale")) {
                Object scaleObj = itemCounterSettings.get("scale");
                this.scale = ((Number) scaleObj).floatValue();
            }
            
            if (itemCounterSettings.containsKey("opacity")) {
                Object opacityObj = itemCounterSettings.get("opacity");
                this.opacity = ((Number) opacityObj).floatValue();
            }
            
            if (itemCounterSettings.containsKey("textColor")) {
                Object colorObj = itemCounterSettings.get("textColor");
                this.textColor = ((Number) colorObj).intValue();
            }
            
            if (itemCounterSettings.containsKey("backgroundColor")) {
                Object bgColorObj = itemCounterSettings.get("backgroundColor");
                this.backgroundColor = ((Number) bgColorObj).intValue();
            }
            
            if (itemCounterSettings.containsKey("iconSize")) {
                Object iconSizeObj = itemCounterSettings.get("iconSize");
                this.iconSize = ((Number) iconSizeObj).intValue();
            }
            
            if (itemCounterSettings.containsKey("spacing")) {
                Object spacingObj = itemCounterSettings.get("spacing");
                this.spacing = ((Number) spacingObj).intValue();
            }
            
            if (itemCounterSettings.containsKey("trackedItems")) {
                List<String> items = (List<String>) itemCounterSettings.get("trackedItems");
                this.trackedItemIds.clear();
                this.trackedItemIds.addAll(items);
            }
        }
    }

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        if (!enabled) {
            itemCounts.clear();
        }
        System.out.println("[LeafClient] ItemCounter " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "ItemCounter";
    }
    
    public boolean isAutoDetect() {
        return autoDetect;
    }
    
    public void setAutoDetect(boolean autoDetect) {
        this.autoDetect = autoDetect;
    }
    
    public boolean isShowIcons() {
        return showIcons;
    }
    
    public void setShowIcons(boolean showIcons) {
        this.showIcons = showIcons;
    }
    
    public boolean isShowAnimations() {
        return showAnimations;
    }
    
    public void setShowAnimations(boolean showAnimations) {
        this.showAnimations = showAnimations;
    }
    
    public boolean isHorizontalLayout() {
        return horizontalLayout;
    }
    
    public void setHorizontalLayout(boolean horizontalLayout) {
        this.horizontalLayout = horizontalLayout;
    }
    
    public float getScale() {
        return scale;
    }
    
    public void setScale(float scale) {
        this.scale = Math.max(0.5f, Math.min(3.0f, scale));
    }
    
    public float getOpacity() {
        return opacity;
    }
    
    public void setOpacity(float opacity) {
        this.opacity = Math.max(0.0f, Math.min(1.0f, opacity));
    }
    
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
    
    public int getIconSize() {
        return iconSize;
    }
    
    public void setIconSize(int iconSize) {
        this.iconSize = Math.max(8, Math.min(32, iconSize));
    }
    
    public int getSpacing() {
        return spacing;
    }
    
    public void setSpacing(int spacing) {
        this.spacing = Math.max(0, Math.min(20, spacing));
    }
    
    public Set<String> getTrackedItemIds() {
        return trackedItemIds;
    }
    
    public void addTrackedItem(String itemId) {
        trackedItemIds.add(itemId);
    }
    
    public void removeTrackedItem(String itemId) {
        trackedItemIds.remove(itemId);
    }
    
    public void clearTrackedItems() {
        trackedItemIds.clear();
    }
    
    public boolean isItemTracked(Item item) {
        if (autoDetect) {
            return true;
        }
        Identifier id = Registries.ITEM.getId(item);
        return trackedItemIds.contains(id.toString());
    }
    
    public Map<Item, Integer> getItemCounts() {
        return itemCounts;
    }
    
    public void setItemCounts(Map<Item, Integer> counts) {
        this.itemCounts = counts;
    }
}