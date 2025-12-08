package com.leafclient.modules;

public class PingMod implements ILeafMod {
    private boolean enabled = false;

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] Ping " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "Ping";
    }
}
