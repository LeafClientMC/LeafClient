package com.leafclient.modules;

public class FullBrightMod implements ILeafMod {
    private boolean enabled = false;

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] FullBrightMod enabled: " + enabled);
    }

    @Override
    public String getName() {
        return "FullBright";
    }
}