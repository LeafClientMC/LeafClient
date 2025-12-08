package com.leafclient.ui;

import net.minecraft.item.Item;

public class ItemCounterEntry {
    private Item item;
    private int count;
    private int previousCount;
    private float animationProgress;
    private long lastUpdateTime;
    
    public ItemCounterEntry(Item item, int count) {
        this.item = item;
        this.count = count;
        this.previousCount = count;
        this.animationProgress = 0.0f;
        this.lastUpdateTime = System.currentTimeMillis();
    }
    
    public Item getItem() {
        return item;
    }
    
    public int getCount() {
        return count;
    }
    
    public void setCount(int count) {
        if (this.count != count) {
            this.previousCount = this.count;
            this.count = count;
            this.animationProgress = 1.0f;
            this.lastUpdateTime = System.currentTimeMillis();
        }
    }
    
    public int getPreviousCount() {
        return previousCount;
    }
    
    public float getAnimationProgress() {
        return animationProgress;
    }
    
    public void updateAnimation() {
        if (animationProgress > 0) {
            animationProgress = Math.max(0, animationProgress - 0.1f);
        }
    }
    
    public long getLastUpdateTime() {
        return lastUpdateTime;
    }
}