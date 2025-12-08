package com.leafclient.modules;

import net.minecraft.client.MinecraftClient;
import com.leafclient.LeafClient;

public class MinimapMod implements ILeafMod {
    private boolean userEnabled = true;
    private boolean currentlyActive = false;

    private int mapSize = 256;
    private int displaySize = 180;
    private float zoom = 1.0f;
    private boolean showWaypoints = true;
    private boolean showCoordinates = true;
    private int x = -1; // -1 means auto-position
    private int y = -1; // -1 means auto-position

    @Override
    public boolean isEnabled() {
        MinecraftClient client = MinecraftClient.getInstance();
        boolean worldReady = (client.world != null && client.player != null);
        
        // Get replay mod status
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        boolean isReplaying = (replayMod != null && replayMod.isPlaying());

        boolean shouldBeActive = userEnabled && worldReady && !isReplaying;

        // If active state changes, perform cleanup/init
        if (shouldBeActive != currentlyActive) {
            currentlyActive = shouldBeActive;
            if (currentlyActive) {
                // Minimap is becoming active, ensure texture is ready
                com.leafclient.util.render.MinimapRenderer.cleanup();
                System.out.println("[LeafClient] Minimap: Becoming active.");
            } else {
                // Minimap is becoming inactive, perform cleanup
                com.leafclient.util.render.MinimapRenderer.cleanup();
                System.out.println("[LeafClient] Minimap: Becoming inactive, performing cleanup.");
            }
        }
        
        return currentlyActive; // Return the actual active state
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.userEnabled = enabled; // Only update user preference
        System.out.println("[LeafClient] Minimap user preference: " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "Minimap";
    }

    // --- Getters and Setters for Minimap Settings ---
    public int getMapSize() { return mapSize; }
    public void setMapSize(int mapSize) { this.mapSize = mapSize; }

    public int getDisplaySize() { return displaySize; }
    public void setDisplaySize(int displaySize) { this.displaySize = displaySize; }

    public float getZoom() { return zoom; }
    public void setZoom(float zoom) { this.zoom = zoom; }

    public boolean isShowWaypoints() { return showWaypoints; }
    public void setShowWaypoints(boolean showWaypoints) { this.showWaypoints = showWaypoints; }

    public boolean isShowCoordinates() { return showCoordinates; }
    public void setShowCoordinates(boolean showCoordinates) { this.showCoordinates = showCoordinates; }

    public int getX() { return x; }
    public void setX(int x) { this.x = x; }

    public int getY() { return y; }
    public void setY(int y) { this.y = y; }
}
