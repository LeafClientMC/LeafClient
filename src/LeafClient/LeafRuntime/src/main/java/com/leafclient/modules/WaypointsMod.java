package com.leafclient.modules;

import java.awt.Color;
import java.util.ArrayList;
import java.util.List;
import java.util.Random;
import java.util.stream.Collectors;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import net.minecraft.client.MinecraftClient;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.HashMap;
import java.util.Map;

public class WaypointsMod implements ILeafMod {
    private boolean enabled = false;
    private final List<Waypoint> allWaypoints = new ArrayList<>();
    private final File waypointsFile;
    private final Gson gson = new Gson();

    public WaypointsMod() {
        this.waypointsFile = new File(System.getenv("APPDATA") + File.separator + "LeafClient" + File.separator + "leafclient_mod_settings.txt");
    }

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        if (enabled) {
            loadWaypoints();
        } else {
            saveWaypoints();
        }
        System.out.println("[LeafClient] Waypoints " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "Waypoints";
    }

    // --- CONTEXT HELPERS ---
    public static String getServerContext() {
        MinecraftClient client = MinecraftClient.getInstance();
        
        // Multiplayer
        if (client.getCurrentServerEntry() != null) {
            return client.getCurrentServerEntry().address;
        }
        
        // Singleplayer
        if (client.isInSingleplayer()) {
            if (client.getServer() != null) {
                return "SP:" + client.getServer().getSaveProperties().getLevelName();
            }
            return "SP:Integrated";
        }
        
        return "127.0.0.1";
    }

    public static String getDimensionContext() {
        MinecraftClient client = MinecraftClient.getInstance();
        if (client.world != null) {
            return client.world.getRegistryKey().getValue().toString();
        }
        return "minecraft:overworld";
    }
    // -----------------------

    // Used by commands (Defaults to all options enabled)
    public void addWaypoint(String name, int x, int y, int z) {
        addWaypoint(name, x, y, z, new Random().nextInt(0xFFFFFF));
    }

    public void addWaypoint(String name, int x, int y, int z, int color) {
        String srv = getServerContext();
        String dim = getDimensionContext();

        // Check for duplicates
        for (Waypoint waypoint : allWaypoints) {
            if (waypoint.name.equalsIgnoreCase(name) && 
                waypoint.server.equalsIgnoreCase(srv) &&
                waypoint.dimension.equalsIgnoreCase(dim)) {
                return;
            }
        }
        
        // Add with defaults (true for all display options)
        allWaypoints.add(new Waypoint(name, x, y, z, color, srv, dim, true, true, true, true));
        saveWaypoints();
        System.out.println("[LeafClient] Added waypoint: " + name + " [" + dim + "] on " + srv);
    }

    public void removeWaypoint(Waypoint waypoint) {
        allWaypoints.remove(waypoint);
        saveWaypoints();
    }

    public void removeWaypoint(String name) {
        String srv = getServerContext();
        String dim = getDimensionContext();
        
        allWaypoints.removeIf(w -> w.name.equals(name) && w.server.equals(srv) && w.dimension.equals(dim));
        saveWaypoints();
    }

    public List<Waypoint> getWaypoints() {
        String currentServer = getServerContext();
        String currentDim = getDimensionContext();

        return allWaypoints.stream()
                .filter(w -> w.server.equalsIgnoreCase(currentServer) && w.dimension.equalsIgnoreCase(currentDim))
                .collect(Collectors.toList());
    }

    public void loadWaypoints() {
        if (waypointsFile.exists()) {
            try {
                String json = new String(Files.readAllBytes(Paths.get(waypointsFile.toURI())));
                if (json.isEmpty()) return;

                Map<String, Object> settings = gson.fromJson(json, new TypeToken<HashMap<String, Object>>(){}.getType());
                if (settings != null && settings.containsKey("WAYPOINTS")) {
                    Map<String, Object> waypointsData = (Map<String, Object>) settings.get("WAYPOINTS");
                    if (waypointsData.containsKey("WAYPOINTSDB")) {
                        allWaypoints.clear();
                        List<Map<String, Object>> waypointsDb = (List<Map<String, Object>>) waypointsData.get("WAYPOINTSDB");
                        
                        for (Map<String, Object> waypointMap : waypointsDb) {
                            String name = (String) waypointMap.get("waypoint_name");
                            int x = parseSafe(waypointMap.get("waypoint_x"));
                            int y = parseSafe(waypointMap.get("waypoint_y"));
                            int z = parseSafe(waypointMap.get("waypoint_z"));
                            int color = parseSafe(waypointMap.get("color"));
                            
                            // Context
                            String server = waypointMap.containsKey("server") ? (String) waypointMap.get("server") : "127.0.0.1";
                            String dimension = waypointMap.containsKey("dimension") ? (String) waypointMap.get("dimension") : "minecraft:overworld";

                            // Display Options
                            boolean showText = parseBool(waypointMap.get("show_text"));
                            boolean highlightBlock = parseBool(waypointMap.get("highlight_block"));
                            boolean showBeam = parseBool(waypointMap.get("show_beam"));
                            boolean showDistance = parseBool(waypointMap.get("show_distance"));

                            allWaypoints.add(new Waypoint(name, x, y, z, color, server, dimension, showText, highlightBlock, showBeam, showDistance));
                        }
                    }
                }
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
    }
    
    private int parseSafe(Object obj) {
        if (obj == null) return 0;
        try { return (int) Double.parseDouble(String.valueOf(obj)); } catch (Exception e) { return 0; }
    }

    private boolean parseBool(Object obj) {
        if (obj == null) return true; // Default to true if missing
        return Boolean.parseBoolean(String.valueOf(obj));
    }

    public void saveWaypoints() {
        waypointsFile.getParentFile().mkdirs();
        try {
            Map<String, Object> settings;
            if (waypointsFile.exists() && waypointsFile.length() > 0) {
                try (Reader reader = new FileReader(waypointsFile)) {
                    settings = gson.fromJson(reader, new TypeToken<HashMap<String, Object>>(){}.getType());
                } catch (Exception e) {
                    settings = new HashMap<>();
                }
                if (settings == null) settings = new HashMap<>();
            } else {
                settings = new HashMap<>();
            }

            Map<String, Object> waypointsData = (Map<String, Object>) settings.getOrDefault("WAYPOINTS", new HashMap<>());
            List<Map<String, String>> waypointsDb = new ArrayList<>();
            
            for (Waypoint waypoint : allWaypoints) {
                Map<String, String> waypointMap = new HashMap<>();
                waypointMap.put("waypoint_name", waypoint.name);
                waypointMap.put("waypoint_x", String.valueOf(waypoint.x));
                waypointMap.put("waypoint_y", String.valueOf(waypoint.y));
                waypointMap.put("waypoint_z", String.valueOf(waypoint.z));
                waypointMap.put("color", String.valueOf(waypoint.color));
                waypointMap.put("server", waypoint.server);
                waypointMap.put("dimension", waypoint.dimension);
                
                // Save Options
                waypointMap.put("show_text", String.valueOf(waypoint.showText));
                waypointMap.put("highlight_block", String.valueOf(waypoint.highlightBlock));
                waypointMap.put("show_beam", String.valueOf(waypoint.showBeam));
                waypointMap.put("show_distance", String.valueOf(waypoint.showDistance));
                
                waypointsDb.add(waypointMap);
            }
            waypointsData.put("WAYPOINTSDB", waypointsDb);
            settings.put("WAYPOINTS", waypointsData);
            settings.remove("WAYPOINTSDB");

            try (Writer writer = new FileWriter(waypointsFile)) {
                gson.toJson(settings, writer);
            }
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public static class Waypoint {
        public final String name;
        public final int x, y, z;
        public final int color;
        public final String server;
        public final String dimension;
        
        // Display Options
        public final boolean showText;
        public final boolean highlightBlock;
        public final boolean showBeam;
        public final boolean showDistance;

        public Waypoint(String name, int x, int y, int z, int color, String server, String dimension, 
                        boolean showText, boolean highlightBlock, boolean showBeam, boolean showDistance) {
            this.name = name;
            this.x = x; this.y = y; this.z = z;
            this.color = color;
            this.server = server;
            this.dimension = dimension;
            this.showText = showText;
            this.highlightBlock = highlightBlock;
            this.showBeam = showBeam;
            this.showDistance = showDistance;
        }

        // Legacy Constructor (Defaults options to True)
        public Waypoint(String name, int x, int y, int z, int color, String server, String dimension) {
            this(name, x, y, z, color, server, dimension, true, true, true, true);
        }

        public Color getColor() { return new Color(color); }
    }
}
