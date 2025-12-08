package com.leafclient.api;

import com.leafclient.mixin.TitleScreenMixin;

public final class GuiOpacity {

    public static void set(float v) {
        try {
            var m = TitleScreenMixin.class
                      .getDeclaredMethod("setOpacity", float.class);
            m.setAccessible(true);
            m.invoke(null, v);
        } catch (Throwable ignored) {}
    }

    public static float get() {
        try {
            var m = TitleScreenMixin.class
                      .getDeclaredMethod("getOpacity");
            m.setAccessible(true);
            return (float) m.invoke(null);
        } catch (Throwable ignored) {
            return 1f;
        }
    }
}
