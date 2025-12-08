package com.leafclient.modules;

public class ArmorHUDMod implements ILeafMod {
    private boolean enabled = false;

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] ArmorHUD " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "ArmorHUD";
    }
}
