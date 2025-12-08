package com.leafclient.modules;

public class ToggleSprintMod implements ILeafMod {
    private boolean enabled = false;
    private boolean sprintToggled = false;
    private boolean sneakToggled = false;
    private boolean flyBoost = true; // Enable fly boost by default

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        if (!enabled) {
            sprintToggled = false;
            sneakToggled = false;
        }
        System.out.println("[LeafClient] ToggleSprint " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "ToggleSprint";
    }

    public boolean isSprintToggled() {
        return sprintToggled && enabled;
    }

    public void setSprintToggled(boolean toggled) {
        this.sprintToggled = toggled;
    }

    public void toggleSprint() {
        this.sprintToggled = !this.sprintToggled;
    }

    public boolean isSneakToggled() {
        return sneakToggled && enabled;
    }

    public void setSneakToggled(boolean toggled) {
        this.sneakToggled = toggled;
    }

    public void toggleSneak() {
        this.sneakToggled = !this.sneakToggled;
    }

    public boolean isFlyBoostEnabled() {
        return flyBoost;
    }

    public void setFlyBoost(boolean flyBoost) {
        this.flyBoost = flyBoost;
    }
}
