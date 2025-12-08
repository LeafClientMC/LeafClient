package com.leafclient.core;

import com.leafclient.LeafClient;
import com.leafclient.config.ConfigManager;
import net.fabricmc.api.ClientModInitializer;
import net.minecraft.client.MinecraftClient;

/**
 * LeafClient Runtime Entry Point
 *
 * This is the Fabric entrypoint that initializes everything.
 */
public class RuntimeMain implements ClientModInitializer {

    private static RuntimeMain INSTANCE;
    private static LeafClient client;
    private static ConfigManager configManager;

    @Override
    public void onInitializeClient() {
        INSTANCE = this;

        System.out.println("------------------------------------------");
        System.out.println("|     LeafClient Runtime v1.0.0          |");
        System.out.println("------------------------------------------");

        long startTime = System.currentTimeMillis();

        try {
            // Phase 1: Initialize core (creates ModManager internally)
            initCore();

            // Phase 2: Load configuration
            initConfig();

            // Phase 3: Register keybinds, tick handlers, commands
            initUI();

            long elapsed = System.currentTimeMillis() - startTime;
            System.out.println("- LeafClient initialized in " + elapsed + "ms");
            System.out.println("- Loaded " + LeafClient.modManager.getMods().size() + " mods");

        } catch (Exception e) {
            System.err.println("X LeafClient initialization failed!");
            e.printStackTrace();
            throw new RuntimeException("Failed to initialize LeafClient", e);
        }
    }

    private void initCore() {
        System.out.println("[1/3] Initializing core...");
        client = new LeafClient(); // Creates ModManager internally
        System.out.println("  - Core initialized");
    }

    private void initConfig() {
        System.out.println("[2/3] Loading configuration...");
        configManager = new ConfigManager();
        configManager.loadAll();
        LeafClient.modManager.updateModStatesFromFile();
        System.out.println("  - Configuration loaded");
    }

    private void initUI() {
        System.out.println("[3/3] Initializing UI...");
        client.registerKeybinds();
        client.registerTickHandlers();
        client.registerNetworkEvents();
        client.registerCommands();
        System.out.println("  - UI initialized");
    }

    // Getters
    public static RuntimeMain getInstance() { return INSTANCE; }
    public static LeafClient getClient() { return client; }
    public static ConfigManager getConfigManager() { return configManager; }
}
