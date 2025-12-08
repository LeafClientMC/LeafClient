package com.leafclient.util.render;

import com.leafclient.LeafClient;
import com.leafclient.modules.ItemCounterMod;
import com.leafclient.ui.ItemCounterEntry;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.item.Item;
import net.minecraft.item.ItemStack;
import net.minecraft.entity.player.PlayerInventory;

import java.util.*;

public class ItemCounterRenderer {
    
    private static final MinecraftClient client = MinecraftClient.getInstance();
    private static Map<Item, ItemCounterEntry> entries = new LinkedHashMap<>();
    
    public static void updateItemCounts() {
        ItemCounterMod mod = LeafClient.modManager.getMod(ItemCounterMod.class);
        if (mod == null || !mod.isEnabled() || client.player == null) return;
        
        Map<Item, Integer> counts = countItems(client.player.getInventory(), mod);
        Set<Item> currentItems = new HashSet<>(counts.keySet());
        
        for (Map.Entry<Item, Integer> entry : counts.entrySet()) {
            Item item = entry.getKey();
            int count = entry.getValue();
            
            if (entries.containsKey(item)) {
                entries.get(item).setCount(count);
            } else {
                entries.put(item, new ItemCounterEntry(item, count));
            }
        }
        
        entries.keySet().removeIf(item -> !currentItems.contains(item));
        
        if (mod.isShowAnimations()) {
            for (ItemCounterEntry entry : entries.values()) {
                entry.updateAnimation();
            }
        }
        
        mod.setItemCounts(counts);
    }
    
    private static Map<Item, Integer> countItems(PlayerInventory inventory, ItemCounterMod mod) {
        Map<Item, Integer> counts = new LinkedHashMap<>();
        for (int i = 0; i < inventory.size(); i++) {
            ItemStack stack = inventory.getStack(i);
            if (!stack.isEmpty()) {
                Item item = stack.getItem();
                if (mod.isItemTracked(item)) {
                    counts.put(item, counts.getOrDefault(item, 0) + stack.getCount());
                }
            }
        }
        return counts;
    }
    
    public static void render(DrawContext context) {
        ItemCounterMod mod = LeafClient.modManager.getMod(ItemCounterMod.class);
        if (mod == null || !mod.isEnabled() || client.player == null) return;
        
        // FIXED: Removed screen check
        
        // FIXED: Set base to 0 because HudRenderer handles translation
        float baseX = 0; 
        float baseY = 0;
        
        float scale = mod.getScale();
        int iconSize = (int)(mod.getIconSize() * scale);
        int spacing = (int)(mod.getSpacing() * scale);
        boolean horizontal = mod.isHorizontalLayout();
        
        float currentX = baseX;
        float currentY = baseY;
        
        for (ItemCounterEntry entry : entries.values()) {
            if (entry.getCount() > 0) {
                renderItemEntry(context, mod, entry, currentX, currentY, iconSize, scale);
                
                if (horizontal) {
                    currentX += iconSize + spacing + (client.textRenderer.getWidth(String.valueOf(entry.getCount())) * scale) + spacing;
                } else {
                    currentY += iconSize + spacing;
                }
            }
        }
    }
    
    private static void renderItemEntry(DrawContext context, ItemCounterMod mod, ItemCounterEntry entry, float x, float y, int iconSize, float scale) {
        Item item = entry.getItem();
        int count = entry.getCount();
        
        String countText = String.valueOf(count);
        int textWidth = client.textRenderer.getWidth(countText);
        int totalWidth = iconSize + 4 + (int)(textWidth * scale);
        int totalHeight = iconSize;
        
        float animProgress = mod.isShowAnimations() ? entry.getAnimationProgress() : 0;
        float animScale = 1.0f + (animProgress * 0.2f);
        
        int bgColor = mod.getBackgroundColor();
        int alpha = (int)(mod.getOpacity() * 255);
        bgColor = (bgColor & 0x00FFFFFF) | (alpha << 24);
        
        context.fill((int)x, (int)y, (int)(x + totalWidth), (int)(y + totalHeight), bgColor);
        
        if (mod.isShowIcons()) {
            ItemStack stack = new ItemStack(item);
            if (animProgress > 0) {
                context.getMatrices().push();
                context.getMatrices().translate(x + iconSize / 2.0f, y + iconSize / 2.0f, 0);
                context.getMatrices().scale(animScale, animScale, 1.0f);
                context.getMatrices().translate(-(x + iconSize / 2.0f), -(y + iconSize / 2.0f), 0);
            }
            
            context.drawItem(stack, (int)x + 2, (int)y + (iconSize - 16) / 2);
            
            if (animProgress > 0) {
                context.getMatrices().pop();
            }
        }
        
        int textX = (int)(x + iconSize + 4);
        int textY = (int)(y + (iconSize - client.textRenderer.fontHeight) / 2);
        
        int textColor = mod.getTextColor();
        if (animProgress > 0) {
            int flashAlpha = (int)(animProgress * 128);
            textColor = interpolateColor(textColor, 0xFFFFFF, animProgress * 0.5f);
        }
        
        context.drawTextWithShadow(client.textRenderer, countText, textX, textY, textColor);
    }
    
    private static int interpolateColor(int color1, int color2, float progress) {
        int r1 = (color1 >> 16) & 0xFF;
        int g1 = (color1 >> 8) & 0xFF;
        int b1 = color1 & 0xFF;
        
        int r2 = (color2 >> 16) & 0xFF;
        int g2 = (color2 >> 8) & 0xFF;
        int b2 = color2 & 0xFF;
        
        int r = (int)(r1 + (r2 - r1) * progress);
        int g = (int)(g1 + (g2 - g1) * progress);
        int b = (int)(b1 + (b2 - b1) * progress);
        
        return 0xFF000000 | (r << 16) | (g << 8) | b;
    }
}
