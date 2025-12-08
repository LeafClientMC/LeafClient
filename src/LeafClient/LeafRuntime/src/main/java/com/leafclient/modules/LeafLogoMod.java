package com.leafclient.modules;

public class LeafLogoMod implements ILeafMod {
    
    private boolean enabled = true; // Default to enabled
    
    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] Leaf Logo " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "Leaf Logo";
    }
}