package com.leafclient;

import net.fabricmc.api.ModInitializer;

public class LeafClientCommon implements ModInitializer {
    @Override
    public void onInitialize() {
        // Common / server-side init logic
        System.out.println("LeafClientCommon initialized!");
    }
}
