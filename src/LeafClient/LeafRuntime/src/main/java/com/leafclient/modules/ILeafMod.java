package com.leafclient.modules;

import net.minecraft.client.gui.screen.Screen;

/**
 *  Base interface for every Leaf mod.
 */
public interface ILeafMod {

    /* ------------------------------------------------------------
     *  CORE
     * ------------------------------------------------------------ */
    String  getName();
    boolean isEnabled();
    void    setEnabled(boolean enabled);

    /* ------------------------------------------------------------
     *  OPTIONAL CONFIG GUI SUPPORT
     * ------------------------------------------------------------ */

    /**
     *  @return true if this mod provides a custom configuration GUI.
     */
    default boolean hasConfigGui() {
        return false;
    }

    /**
     *  @param parent the screen that opened this config GUI (usually the Mod-Manager).
     *  @return a Screen with the mod’s configuration, or null if none.
     */
    default Screen getConfigScreen(Screen parent) {
        return null;                       // implement in individual mods to supply GUI
    }
}
