package com.leafclient.modules;

public class ServerInfoMod implements ILeafMod {
    private boolean enabled = false;

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] ServerInfo " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "ServerInfo";
    }
}
