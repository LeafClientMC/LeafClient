package com.leafclient.modules;

public class ZoomMod implements ILeafMod {
    private boolean enabled = false;
    private boolean isZooming = false;
    private double zoomLevel = 4.0; // Default zoom divisor (higher = more zoom)
    private double currentZoom = 1.0; // Current zoom state for smooth transition
    private double targetZoom = 1.0;
    private double originalFov = 70.0;
    private static final double ZOOM_SMOOTHNESS = 0.15; // Lower = smoother (0.1 - 0.5)
    private static final double MIN_ZOOM = 1.5; // Minimum zoom level
    private static final double MAX_ZOOM = 10.0; // Maximum zoom level
    private static final double SCROLL_SENSITIVITY = 0.5; // How much scroll changes zoom

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        if (!enabled) {
            currentZoom = 1.0;
            targetZoom = 1.0;
        }
        System.out.println("[LeafClient] Zoom " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "Zoom";
    }

    public boolean isZooming() {
        return isZooming && enabled;
    }

    public void setZooming(boolean zooming) {
        this.isZooming = zooming;
    }

    public double getZoomLevel() {
        return zoomLevel;
    }

    public void setZoomLevel(double zoomLevel) {
        this.zoomLevel = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, zoomLevel));
    }

    public void adjustZoomLevel(double delta) {
        // Scroll up = more zoom (higher divisor), scroll down = less zoom (lower divisor)
        this.zoomLevel += delta * SCROLL_SENSITIVITY;
        this.zoomLevel = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, this.zoomLevel));
    }

    public double getCurrentZoom() {
        return currentZoom;
    }

    public void updateZoom(double baseFov) {
        this.originalFov = baseFov;
        
        if (isZooming && enabled) {
            // Calculate target zoomed FOV
            targetZoom = baseFov / zoomLevel;
        } else {
            // Return to normal FOV
            targetZoom = baseFov;
        }
        
        // Smoothly interpolate towards target
        double diff = targetZoom - currentZoom;
        currentZoom += diff * ZOOM_SMOOTHNESS;
        
        // Snap to target if very close (prevents infinite interpolation)
        if (Math.abs(diff) < 0.01) {
            currentZoom = targetZoom;
        }
    }

    public double getOriginalFov() {
        return originalFov;
    }

    public void setOriginalFov(double fov) {
        this.originalFov = fov;
    }

    public void resetZoom() {
        currentZoom = originalFov;
        targetZoom = originalFov;
    }
}
