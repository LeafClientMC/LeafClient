package com.leafclient;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;

import java.io.*;
import java.util.HashMap;
import java.util.Map;

public class ModSettingsFileManager {

    private static final File FILE = new File(System.getenv("APPDATA") + "/LeafClient/leafclient_mod_settings.txt");
    private static final Gson GSON = new Gson();
    
    // Caches
    private static Map<String, Map<String, Object>> hudPositions = new HashMap<>();
    private static Map<String, Object> generalSettings = new HashMap<>();

    static {
        load();
    }

    @SuppressWarnings("unchecked")
    public static void load() {
        if (!FILE.exists()) return;
        try (Reader reader = new FileReader(FILE)) {
            Map<String, Object> root = GSON.fromJson(reader, new TypeToken<Map<String, Object>>(){}.getType());
            if (root != null) {
                if (root.containsKey("HUDPOSITIONS")) {
                    hudPositions = (Map<String, Map<String, Object>>) root.get("HUDPOSITIONS");
                }
                if (root.containsKey("GENERAL")) {
                    generalSettings = (Map<String, Object>) root.get("GENERAL");
                }
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public static void save() {
        try {
            Map<String, Object> root;
            if (FILE.exists()) {
                try (Reader reader = new FileReader(FILE)) {
                    root = GSON.fromJson(reader, new TypeToken<Map<String, Object>>(){}.getType());
                } catch (Exception e) {
                    root = new HashMap<>();
                }
                if (root == null) root = new HashMap<>();
            } else {
                root = new HashMap<>();
            }

            root.put("HUDPOSITIONS", hudPositions);
            root.put("GENERAL", generalSettings);

            FILE.getParentFile().mkdirs();

            try (Writer writer = new FileWriter(FILE)) {
                GSON.toJson(root, writer);
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public static Map<String, Object> readModSettings() {
        if (!FILE.exists()) return new HashMap<>();
        try (Reader reader = new FileReader(FILE)) {
            Map<String, Object> root = GSON.fromJson(reader, new TypeToken<Map<String, Object>>(){}.getType());
            return root != null ? root : new HashMap<>();
        } catch (IOException e) {
            e.printStackTrace();
            return new HashMap<>();
        }
    }

    public static void writeModSettings(Map<String, Object> settings) {
        try {
            FILE.getParentFile().mkdirs();
            try (Writer writer = new FileWriter(FILE)) {
                GSON.toJson(settings, writer);
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    // --- HUD POSITION & SCALE ---
    public static Map<String, Object> getHudPosition(String hudName) {
        return hudPositions.get(hudName);
    }

    public static void saveHudPosition(String hudName, int x, int y) {
        setHudPosition(hudName, x, y);
    }

    public static void setHudPosition(String hudName, int x, int y) {
        Map<String, Object> pos = hudPositions.getOrDefault(hudName, new HashMap<>());
        pos.put("x", x);
        pos.put("y", y);
        if (!pos.containsKey("scale")) pos.put("scale", 1.0f);
        hudPositions.put(hudName, pos);
        save();
    }

    public static void saveHudScale(String hudName, float scale) {
        Map<String, Object> pos = hudPositions.getOrDefault(hudName, new HashMap<>());
        pos.put("scale", scale);
        if (!pos.containsKey("x")) pos.put("x", 10);
        if (!pos.containsKey("y")) pos.put("y", 10);
        hudPositions.put(hudName, pos);
        save();
    }

    public static float getHudScale(String hudName) {
        Map<String, Object> pos = hudPositions.get(hudName);
        if (pos != null && pos.containsKey("scale")) {
            Object val = pos.get("scale");
            if (val instanceof Number) {
                return ((Number) val).floatValue();
            }
        }
        return 1.0f;
    }

    // --- GENERAL SETTINGS ---
    public static boolean isHudSnappingEnabled() {
        Object val = generalSettings.get("hudSnapping");
        return val instanceof Boolean ? (Boolean) val : true; // Default True
    }

    public static int getHudSnapRange() {
        Object val = generalSettings.get("hudSnapRange");
        if (val instanceof Number) return ((Number) val).intValue();
        return 4; // Default 4
    }

    public static boolean isShowHudGrid() {
        Object val = generalSettings.get("showHudGrid");
        return val instanceof Boolean ? (Boolean) val : false; // Default False
    }
}
