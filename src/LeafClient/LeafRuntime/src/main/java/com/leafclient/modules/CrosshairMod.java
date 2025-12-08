package com.leafclient.modules;

public class CrosshairMod implements ILeafMod {
    private boolean enabled = false;
    private CrosshairSettings settings;

    public CrosshairMod() {
        this.settings = CrosshairSettings.load();
    }

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] Crosshair " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "Crosshair";
    }

    public CrosshairSettings getSettings() {
        return settings;
    }
}