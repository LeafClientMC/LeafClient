package com.leafclient.modules;

public class CPSMod implements ILeafMod {
    private boolean enabled = false;
    private int leftClicks = 0;
    private int rightClicks = 0;
    private long lastResetTime = System.currentTimeMillis();

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        System.out.println("[LeafClient] CPS " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "CPS";
    }

    public void onLeftClick() {
        leftClicks++;
    }

    public void onRightClick() {
        rightClicks++;
    }

    public int getLeftCPS() {
        updateCPS();
        return leftClicks;
    }

    public int getRightCPS() {
        updateCPS();
        return rightClicks;
    }

    private void updateCPS() {
        long currentTime = System.currentTimeMillis();
        if (currentTime - lastResetTime >= 1000) {
            leftClicks = 0;
            rightClicks = 0;
            lastResetTime = currentTime;
        }
    }
}
