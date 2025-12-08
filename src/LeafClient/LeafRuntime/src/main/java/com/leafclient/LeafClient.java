package com.leafclient;

import com.leafclient.ModManager;
import com.leafclient.ModSettingsFileManager;
import com.leafclient.modules.*;
import com.leafclient.replay.ReplayKeyframe;
import com.leafclient.replay.ReplayPlayer;
import com.leafclient.replay.ReplaySettings;
import com.leafclient.util.render.MinimapRenderer;
import com.leafclient.util.render.WaypointRenderer;
import com.mojang.brigadier.arguments.*;
import net.fabricmc.fabric.api.client.command.v2.ClientCommandManager;
import net.fabricmc.fabric.api.client.command.v2.ClientCommandRegistrationCallback;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientTickEvents;
import net.fabricmc.fabric.api.client.keybinding.v1.KeyBindingHelper;
import net.fabricmc.fabric.api.client.networking.v1.ClientPlayConnectionEvents;
import net.fabricmc.fabric.api.client.rendering.v1.WorldRenderEvents;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.option.KeyBinding;
import net.minecraft.client.util.InputUtil;
import net.minecraft.text.Text;
import org.lwjgl.glfw.GLFW;
import com.leafclient.util.render.KeystrokesRenderer;
import com.leafclient.util.render.ItemCounterRenderer;
import com.leafclient.macro.ChatMacroHandler;
import com.leafclient.screen.ChatMacrosScreen;
import com.leafclient.ui.CrosshairSettingsScreen;

import java.io.File;
import java.io.IOException;
import java.util.List;
import java.util.Map;
import java.util.HashMap;
import java.io.FileWriter;


/**
 * LeafClient Core - Main client logic
 * 
 * This is now called by RuntimeMain.java instead of being the entrypoint
 * itself.
 */
public class LeafClient {

    public static LeafClient INSTANCE;
    public static ModManager modManager;
    public static boolean leafBadgeRenderedThisFrame = false;

    // --- ADD THIS ---
    private int stateSyncTimer = 0;

    /*
     * ------------------------------------------------------------
     * KEYBINDINGS
     * ------------------------------------------------------------
     */
    private static final KeyBinding launcherKeyBind = new KeyBinding(
            "key.leafclient.modmanager",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_RIGHT_SHIFT,
            "category.leafclient");

    private static final KeyBinding waypointsKeyBind = new KeyBinding("key.leafclient.waypoints",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_M,
            "category.leafclient.waypoints");

    private static final KeyBinding zoomKeyBind = new KeyBinding("key.leafclient.zoom",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_C,
            "category.leafclient.keys");

    private static final KeyBinding freelookKeyBind = new KeyBinding("key.leafclient.freelook",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_LEFT_ALT,
            "category.leafclient.keys");

    private static final KeyBinding minimapZoomInKeyBind = new KeyBinding("key.leafclient.minimap.zoomin",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_EQUAL,
            "category.leafclient.keys");

    private static final KeyBinding minimapZoomOutKeyBind = new KeyBinding("key.leafclient.minimap.zoomout",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_MINUS,
            "category.leafclient.keys");

    private static final KeyBinding replayRecordKeyBind = new KeyBinding("key.leafclient.replay.record",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_R,
            "category.leafclient.replay");

    private static final KeyBinding replayStopKeyBind = new KeyBinding("key.leafclient.replay.stop",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_T,
            "category.leafclient.replay");

    private static final KeyBinding macroEditorKeyBind = new KeyBinding("key.leafclient.macroeditor",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_RIGHT_BRACKET,
            "category.leafclient.keys");

    private static final KeyBinding crosshairKeyBind = new KeyBinding("key.leafclient.crosshair",
            InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_G,
            "category.leafclient.keys");

    private static float cachedMinimapZoom = Float.NaN;
    private static int cachedMinimapMapSize = -1;

    /*
     * ------------------------------------------------------------
     * CONSTRUCTOR (called by RuntimeMain)
     * ------------------------------------------------------------
     */
    public LeafClient() {
        INSTANCE = this;
        modManager = new ModManager(MinecraftClient.getInstance().runDirectory);

        /* title-screen button opacity: 30 % */
        com.leafclient.api.GuiOpacity.set(0.0f);

        WorldRenderEvents.END.register(WaypointRenderer::render);
    }

    /*
     * ------------------------------------------------------------
     * PUBLIC INITIALIZATION METHODS (called by RuntimeMain)
     * ------------------------------------------------------------
     */
    public void registerKeybinds() {
        KeyBindingHelper.registerKeyBinding(launcherKeyBind);
        KeyBindingHelper.registerKeyBinding(crosshairKeyBind);
        KeyBindingHelper.registerKeyBinding(waypointsKeyBind);
        KeyBindingHelper.registerKeyBinding(zoomKeyBind);
        KeyBindingHelper.registerKeyBinding(freelookKeyBind);
        KeyBindingHelper.registerKeyBinding(minimapZoomInKeyBind);
        KeyBindingHelper.registerKeyBinding(minimapZoomOutKeyBind);
        KeyBindingHelper.registerKeyBinding(replayRecordKeyBind);
        KeyBindingHelper.registerKeyBinding(replayStopKeyBind);
        KeyBindingHelper.registerKeyBinding(macroEditorKeyBind);
    }

    public void registerTickHandlers() {
        ClientTickEvents.END_CLIENT_TICK.register(client -> {

            // --- ADD THIS BLOCK ---
            // Sync state to C# App (Every 20 ticks / 1 second)
            if (stateSyncTimer++ > 20) {
                stateSyncTimer = 0;
                writeClientState();
            }

            /* ---------- HUD Editor Signal Check (Launcher -> Client) ---------- */
            File hudSignal = new File(MinecraftClient.getInstance().runDirectory, "leaf_open_hud_editor.signal");
            if (hudSignal.exists()) {
                hudSignal.delete(); // Consume signal
                // Open the HUD Editor Screen
                client.setScreen(new com.leafclient.screen.HudEditorScreen(client.currentScreen));
            }

            /* ---------- Mod Manager ---------- */
            while (launcherKeyBind.wasPressed()) {
                try {
                    // Create a signal file in the run directory (.minecraft)
                    File signalFile = new File(MinecraftClient.getInstance().runDirectory, "leaf_open_mods.signal");
                    if (!signalFile.exists()) {
                        signalFile.createNewFile();
                    }
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }

            /* ---------- Keystrokes Update ---------- */
            KeystrokesRenderer.updateKeyStates();

            /* ---------- Item Counter Update ---------- */
            ItemCounterRenderer.updateItemCounts();

            /* ---------- Chat Macros ---------- */
            ChatMacroHandler.tick();

            /* ---------- Macro Editor ---------- */
            while (macroEditorKeyBind.wasPressed()) {
                client.setScreen(new ChatMacrosScreen(client.currentScreen));
            }

            /* ---------- Zoom ---------- */
            ZoomMod zoomMod = modManager.getMod(ZoomMod.class);
            if (zoomMod != null && zoomMod.isEnabled()) {
                boolean prev = zoomMod.isZooming();
                boolean now = zoomKeyBind.isPressed();
                zoomMod.setZooming(now);
                if (prev && !now)
                    zoomMod.resetZoom();
            }

            /* ---------- Crosshair Editor ---------- */
            while (crosshairKeyBind.wasPressed()) {
                CrosshairMod crosshairMod = modManager.getMod(CrosshairMod.class);
                if (crosshairMod != null) {
                    client.setScreen(new CrosshairSettingsScreen(crosshairMod.getSettings()));
                }
            }

            /* ---------- Minimap Zoom ---------- */
            MinimapMod minimap = modManager.getMod(MinimapMod.class);
            if (minimap != null && minimap.isEnabled()) {
                float z = minimap.getZoom();
                if (minimapZoomInKeyBind.wasPressed())
                    z = Math.max(0.5f, z - 0.25f);
                if (minimapZoomOutKeyBind.wasPressed())
                    z = Math.min(32f, z + 0.25f);
                minimap.setZoom(z);

                if (Float.isNaN(cachedMinimapZoom))
                    cachedMinimapZoom = z;
                if (cachedMinimapMapSize < 0)
                    cachedMinimapMapSize = minimap.getMapSize();

                if (Math.abs(z - cachedMinimapZoom) > 0.001f
                        || minimap.getMapSize() != cachedMinimapMapSize) {
                    cachedMinimapZoom = minimap.getZoom();
                    cachedMinimapMapSize = minimap.getMapSize();
                    MinimapRenderer.cleanup();
                }
            }

            /* ---------- Freelook ---------- */
            if (client.player != null) {
                FreelookMod fl = modManager.getMod(FreelookMod.class);
                if (fl != null && fl.isEnabled()) {
                    if (freelookKeyBind.wasPressed()) {
                        fl.toggleFreelook(
                                client.player.getPos(),
                                client.player.getYaw(),
                                client.player.getPitch());
                    }
                    fl.interpolateCameraPosition();

                    if (fl.isFreelooking()) {
                        float f = 0, s = 0, u = 0;
                        if (client.options.forwardKey.isPressed())
                            f += 1;
                        if (client.options.backKey.isPressed())
                            f -= 1;
                        if (client.options.rightKey.isPressed())
                            s += 1;
                        if (client.options.leftKey.isPressed())
                            s -= 1;
                        if (client.options.jumpKey.isPressed())
                            u += 1;
                        if (client.options.sneakKey.isPressed())
                            u -= 1;
                        fl.updateTargetCameraPosition(f, s, u);
                    } else {
                        fl.setCameraPosition(client.player.getPos()
                                .add(0, client.player.getStandingEyeHeight(), 0));
                        fl.setCameraYaw(client.player.getYaw());
                        fl.setCameraPitch(client.player.getPitch());
                    }
                }

                ToggleSprintMod ts = modManager.getMod(ToggleSprintMod.class);
                if (ts != null && ts.isEnabled() && (fl == null || !fl.isFreelooking())) {
                    if (client.options.sprintKey.wasPressed()) {
                        ts.toggleSprint();
                        if (client.player != null) {
                            client.player.sendMessage(net.minecraft.text.Text.of(
                                    "§7[ToggleSprint] " + (ts.isSprintToggled() ? "§aON" : "§cOFF")), true);
                        }
                    }

                    if (client.options.sneakKey.wasPressed()) {
                        ts.toggleSneak();
                        if (client.player != null) {
                            client.player.sendMessage(net.minecraft.text.Text.of(
                                    "§7[ToggleSneak] " + (ts.isSneakToggled() ? "§aON" : "§cOFF")), true);
                        }
                    }
                }
            }

            /* ---------- Replay ---------- */
            ReplayMod replay = modManager.getMod(ReplayMod.class);
            if (replay != null && replay.isEnabled()) {
                replay.tick();

                if (replayRecordKeyBind.wasPressed() && !replay.isRecording())
                    replay.startRecording();
                if (replayStopKeyBind.wasPressed()) {
                    if (replay.isRecording())
                        replay.stopRecording();
                    else if (replay.isPlaying())
                        replay.stopPlayback();
                }

                if (replay.isPlaying() && replay.getPlayer() != null) {
                    handleReplayFreecamInput(replay.getPlayer());
                }
            }
        });
    }

    // --- ADD THIS METHOD ---
    private void writeClientState() {
        try {
            File stateFile = new File(System.getenv("APPDATA") + "/LeafClient/leaf_client_state.json");
            stateFile.getParentFile().mkdirs();
            
            String currentContext = WaypointsMod.getServerContext();
            
            // Default coords
            int x = 0, y = 64, z = 0;
            
            // Get Player Coords
            MinecraftClient client = MinecraftClient.getInstance();
            if (client.player != null) {
                x = (int) client.player.getX();
                y = (int) client.player.getY();
                z = (int) client.player.getZ();
            }
            
            // Write JSON with Server + Coords
            try (FileWriter writer = new FileWriter(stateFile)) {
                String json = String.format("{\"server\": \"%s\", \"x\": %d, \"y\": %d, \"z\": %d}", 
                    currentContext, x, y, z);
                writer.write(json);
            }
        } catch (Exception e) {
            // Ignore errors
        }
    }


    private static void handleReplayFreecamInput(ReplayPlayer player) {
        MinecraftClient c = MinecraftClient.getInstance();
        float f = 0, s = 0, u = 0;
        if (c.options.forwardKey.isPressed())
            f += 1;
        if (c.options.backKey.isPressed())
            f -= 1;
        if (c.options.rightKey.isPressed())
            s += 1;
        if (c.options.leftKey.isPressed())
            s -= 1;
        if (c.options.jumpKey.isPressed())
            u += 1;
        if (c.options.sneakKey.isPressed())
            u -= 1;
        player.updateCameraPosition(f, s, u);
    }

    public void registerNetworkEvents() {
        ClientPlayConnectionEvents.JOIN.register((handler, sender, client) -> {
            ReplayMod rm = modManager.getMod(ReplayMod.class);
            if (rm != null && rm.getRecorder() != null) {
                rm.getRecorder().setShouldLogActions(true);
            }
        });

        ClientPlayConnectionEvents.DISCONNECT.register((handler, client) -> {
            MinimapRenderer.cleanup();
            ReplayMod rm = modManager.getMod(ReplayMod.class);
            if (rm != null && rm.getRecorder() != null) {
                rm.getRecorder().setShouldLogActions(false);
            }
        });
    }

    /*
     * ------------------------------------------------------------
     * COMMANDS (original full registration kept)
     * ------------------------------------------------------------
     */
    public void registerCommands() {
        ClientCommandRegistrationCallback.EVENT.register((dispatcher, registryAccess) -> {

            /* ---------------- Waypoints ---------------- */
            dispatcher.register(ClientCommandManager.literal("waypoint")
                    .then(ClientCommandManager.literal("add")
                            .then(ClientCommandManager.argument("name", StringArgumentType.word())
                                    .executes(ctx -> {
                                        WaypointsMod waypoints = modManager.getMod(WaypointsMod.class);
                                        MinecraftClient client = MinecraftClient.getInstance();
                                        if (waypoints != null && waypoints.isEnabled() && client.player != null) {
                                            String name = StringArgumentType.getString(ctx, "name");
                                            int x = (int) client.player.getX();
                                            int y = (int) client.player.getY();
                                            int z = (int) client.player.getZ();
                                            waypoints.addWaypoint(name, x, y, z);
                                            client.player.sendMessage(Text.of("Waypoint '" + name +
                                                    "' added at " + x + ", " + y + ", " + z), false);
                                        }
                                        return 1;
                                    }))));

            /* ---------------- Replay ---------------- */
            dispatcher.register(ClientCommandManager.literal("replay")
                    .then(ClientCommandManager.literal("record")
                            .executes(ctx -> {
                                ReplayMod rm = modManager.getMod(ReplayMod.class);
                                MinecraftClient mc = MinecraftClient.getInstance();
                                if (rm != null && rm.isEnabled()) {
                                    rm.startRecording();
                                    if (mc.player != null)
                                        mc.player.sendMessage(Text.of("§a[Replay] Recording started"), false);
                                }
                                return 1;
                            }))
                    .then(ClientCommandManager.literal("stop")
                            .executes(ctx -> {
                                ReplayMod rm = modManager.getMod(ReplayMod.class);
                                MinecraftClient mc = MinecraftClient.getInstance();
                                if (rm != null && mc.player != null) {
                                    if (rm.isRecording()) {
                                        rm.stopRecording();
                                        mc.player.sendMessage(Text.of("§a[Replay] Recording stopped"), false);
                                    } else if (rm.isPlaying()) {
                                        rm.stopPlayback();
                                        mc.player.sendMessage(Text.of("§a[Replay] Playback stopped"), false);
                                    }
                                }
                                return 1;
                            }))
                    .then(ClientCommandManager.literal("play")
                            .then(ClientCommandManager.argument("filename", StringArgumentType.greedyString())
                                    .executes(ctx -> {
                                        ReplayMod rm = modManager.getMod(ReplayMod.class);
                                        MinecraftClient mc = MinecraftClient.getInstance();
                                        if (rm != null && rm.isEnabled() && mc.player != null) {
                                            String filename = StringArgumentType.getString(ctx, "filename");
                                            File replayFile = new File(rm.getReplayDirectory(), filename);
                                            if (replayFile.exists()) {
                                                rm.startPlayback(replayFile);
                                                mc.player.sendMessage(Text.of("§a[Replay] Playing: " + filename),
                                                        false);
                                            } else {
                                                mc.player.sendMessage(Text.of("§c[Replay] File not found: " + filename),
                                                        false);
                                            }
                                        }
                                        return 1;
                                    })))
                    .then(ClientCommandManager.literal("list")
                            .executes(ctx -> {
                                ReplayMod rm = modManager.getMod(ReplayMod.class);
                                MinecraftClient mc = MinecraftClient.getInstance();
                                if (rm != null && mc.player != null) {
                                    File[] replays = rm.getReplayFiles();
                                    if (replays.length == 0) {
                                        mc.player.sendMessage(Text.of("§e[Replay] No replays found"), false);
                                    } else {
                                        mc.player.sendMessage(Text.of("§a[Replay] Available replays:"), false);
                                        for (File f : replays) {
                                            mc.player.sendMessage(Text.of("§7- " + f.getName()), false);
                                        }
                                    }
                                }
                                return 1;
                            }))
                    .then(ClientCommandManager.literal("pause")
                            .executes(ctx -> {
                                ReplayMod rm = modManager.getMod(ReplayMod.class);
                                MinecraftClient mc = MinecraftClient.getInstance();
                                if (rm != null && rm.isPlaying() && mc.player != null) {
                                    rm.getPlayer().pause();
                                    mc.player.sendMessage(Text.of("§a[Replay] Paused"), false);
                                }
                                return 1;
                            }))
                    .then(ClientCommandManager.literal("resume")
                            .executes(ctx -> {
                                ReplayMod rm = modManager.getMod(ReplayMod.class);
                                MinecraftClient mc = MinecraftClient.getInstance();
                                if (rm != null && rm.isPlaying() && mc.player != null) {
                                    rm.getPlayer().resume();
                                    mc.player.sendMessage(Text.of("§a[Replay] Resumed"), false);
                                }
                                return 1;
                            }))
                    .then(ClientCommandManager.literal("seek")
                            .then(ClientCommandManager.argument("tick", IntegerArgumentType.integer(0))
                                    .executes(ctx -> {
                                        ReplayMod rm = modManager.getMod(ReplayMod.class);
                                        MinecraftClient mc = MinecraftClient.getInstance();
                                        if (rm != null && rm.isPlaying() && mc.player != null) {
                                            int t = IntegerArgumentType.getInteger(ctx, "tick");
                                            rm.getPlayer().seekTo(t);
                                            mc.player.sendMessage(Text.of("§a[Replay] Seeked to tick " + t), false);
                                        }
                                        return 1;
                                    })))
                    .then(ClientCommandManager.literal("freecam")
                            .executes(ctx -> {
                                ReplayMod rm = modManager.getMod(ReplayMod.class);
                                MinecraftClient mc = MinecraftClient.getInstance();
                                if (rm != null && rm.isPlaying() && mc.player != null) {
                                    rm.getPlayer().toggleFreecam();
                                    boolean mode = rm.getPlayer().isFreecamMode();
                                    mc.player.sendMessage(Text.of("§a[Replay] Freecam: " + (mode ? "ON" : "OFF")),
                                            false);
                                }
                                return 1;
                            }))
                    .then(ClientCommandManager.literal("keyframe")
                            .then(ClientCommandManager.literal("add")
                                    .executes(ctx -> {
                                        ReplayMod rm = modManager.getMod(ReplayMod.class);
                                        MinecraftClient mc = MinecraftClient.getInstance();
                                        if (rm != null && rm.isPlaying() && mc.player != null) {
                                            rm.getPlayer().addKeyframe();
                                            int tick = rm.getPlayer().getCurrentTick();
                                            mc.player.sendMessage(Text.of("§a[Replay] Keyframe added at tick " + tick),
                                                    false);
                                        }
                                        return 1;
                                    }))
                            .then(ClientCommandManager.literal("clear")
                                    .executes(ctx -> {
                                        ReplayMod rm = modManager.getMod(ReplayMod.class);
                                        MinecraftClient mc = MinecraftClient.getInstance();
                                        if (rm != null && rm.isPlaying() && mc.player != null) {
                                            rm.getPlayer().clearKeyframes();
                                            mc.player.sendMessage(Text.of("§a[Replay] All keyframes cleared"), false);
                                        }
                                        return 1;
                                    }))
                            .then(ClientCommandManager.literal("enable")
                                    .executes(ctx -> {
                                        ReplayMod rm = modManager.getMod(ReplayMod.class);
                                        MinecraftClient mc = MinecraftClient.getInstance();
                                        if (rm != null && rm.isPlaying() && mc.player != null) {
                                            rm.getPlayer().setUseKeyframes(true);
                                            mc.player.sendMessage(Text.of("§a[Replay] Keyframe path enabled"), false);
                                        }
                                        return 1;
                                    }))
                            .then(ClientCommandManager.literal("disable")
                                    .executes(ctx -> {
                                        ReplayMod rm = modManager.getMod(ReplayMod.class);
                                        MinecraftClient mc = MinecraftClient.getInstance();
                                        if (rm != null && rm.isPlaying() && mc.player != null) {
                                            rm.getPlayer().setUseKeyframes(false);
                                            mc.player.sendMessage(Text.of("§a[Replay] Keyframe path disabled"), false);
                                        }
                                        return 1;
                                    }))
                            .then(ClientCommandManager.literal("list")
                                    .executes(ctx -> {
                                        ReplayMod rm = modManager.getMod(ReplayMod.class);
                                        MinecraftClient mc = MinecraftClient.getInstance();
                                        if (rm != null && rm.isPlaying() && mc.player != null) {
                                            List<ReplayKeyframe> kfs = rm.getPlayer()
                                                    .getKeyframeManager().getKeyframes();
                                            if (kfs.isEmpty()) {
                                                mc.player.sendMessage(Text.of("§e[Replay] No keyframes"), false);
                                            } else {
                                                mc.player.sendMessage(Text.of("§a[Replay] Keyframes:"), false);
                                                for (int i = 0; i < kfs.size(); i++) {
                                                    ReplayKeyframe k = kfs.get(i);
                                                    mc.player.sendMessage(Text.of(
                                                            String.format("§7%d. Tick %d - Pos: %.1f, %.1f, %.1f",
                                                                    i, k.tick, k.position.x, k.position.y,
                                                                    k.position.z)),
                                                            false);
                                                }
                                            }
                                        }
                                        return 1;
                                    })))
                    .then(ClientCommandManager.literal("export")
                            .then(ClientCommandManager.literal("start")
                                    .executes(ctx -> {
                                        ReplayMod rm = modManager.getMod(ReplayMod.class);
                                        MinecraftClient mc = MinecraftClient.getInstance();
                                        if (rm != null && rm.isPlaying() && mc.player != null) {
                                            File exportDir = new File(rm.getReplayDirectory(), "exports");
                                            String ts = String.valueOf(System.currentTimeMillis());
                                            File out = new File(exportDir, "export_" + ts);
                                            rm.startExport(out);
                                            mc.player.sendMessage(Text.of("§a[Replay] Export started: "
                                                    + out.getName()), false);
                                        } else if (mc.player != null) {
                                            mc.player.sendMessage(Text.of("§c[Replay] No replay is playing"), false);
                                        }
                                        return 1;
                                    }))
                            .then(ClientCommandManager.literal("stop")
                                    .executes(ctx -> {
                                        ReplayMod rm = modManager.getMod(ReplayMod.class);
                                        MinecraftClient mc = MinecraftClient.getInstance();
                                        if (rm != null && rm.isExporting() && mc.player != null) {
                                            rm.stopExport();
                                            mc.player.sendMessage(Text.of("§a[Replay] Export stopped"), false);
                                        }
                                        return 1;
                                    })))
                    .then(ClientCommandManager.literal("settings")
                            .then(ClientCommandManager.literal("preset")
                                    .then(ClientCommandManager.literal("performance")
                                            .executes(ctx -> {
                                                ReplaySettings.getInstance()
                                                        .setPerformancePreset(
                                                                ReplaySettings.PerformancePreset.MAXIMUM_PERFORMANCE);
                                                sendPlayerMessage("§a[Replay] Performance preset: MAXIMUM PERFORMANCE");
                                                return 1;
                                            }))
                                    .then(ClientCommandManager.literal("balanced")
                                            .executes(ctx -> {
                                                ReplaySettings.getInstance()
                                                        .setPerformancePreset(
                                                                ReplaySettings.PerformancePreset.BALANCED);
                                                sendPlayerMessage("§a[Replay] Performance preset: BALANCED");
                                                return 1;
                                            }))
                                    .then(ClientCommandManager.literal("quality")
                                            .executes(ctx -> {
                                                ReplaySettings.getInstance()
                                                        .setPerformancePreset(
                                                                ReplaySettings.PerformancePreset.MAXIMUM_QUALITY);
                                                sendPlayerMessage("§a[Replay] Performance preset: MAXIMUM QUALITY");
                                                return 1;
                                            })))
                            .then(ClientCommandManager.literal("speed")
                                    .then(ClientCommandManager.argument("value",
                                            FloatArgumentType.floatArg(0.1f, 10.0f))
                                            .executes(ctx -> {
                                                float v = FloatArgumentType.getFloat(ctx, "value");
                                                ReplaySettings.getInstance().setPlaybackSpeed(v);
                                                sendPlayerMessage("§a[Replay] Playback speed: " + v + "x");
                                                return 1;
                                            })))
                            .then(ClientCommandManager.literal("camera_speed")
                                    .then(ClientCommandManager.argument("value",
                                            FloatArgumentType.floatArg(0.1f, 5.0f))
                                            .executes(ctx -> {
                                                float v = FloatArgumentType.getFloat(ctx, "value");
                                                ReplaySettings.getInstance().setCameraSpeed(v);
                                                sendPlayerMessage("§a[Replay] Camera speed: " + v);
                                                return 1;
                                            })))
                            .then(ClientCommandManager.literal("sensitivity")
                                    .then(ClientCommandManager.argument("value",
                                            FloatArgumentType.floatArg(0.01f, 1.0f))
                                            .executes(ctx -> {
                                                float v = FloatArgumentType.getFloat(ctx, "value");
                                                ReplaySettings.getInstance().setCameraSensitivity(v);
                                                sendPlayerMessage("§a[Replay] Camera sensitivity: " + v);
                                                return 1;
                                            })))
                            .then(ClientCommandManager.literal("record_entities")
                                    .then(ClientCommandManager.argument("enabled",
                                            BoolArgumentType.bool())
                                            .executes(ctx -> {
                                                boolean e = BoolArgumentType.getBool(ctx, "enabled");
                                                ReplaySettings.getInstance().setRecordEntities(e);
                                                sendPlayerMessage("§a[Replay] Record entities: " + e);
                                                return 1;
                                            })))
                            .then(ClientCommandManager.literal("record_particles")
                                    .then(ClientCommandManager.argument("enabled",
                                            BoolArgumentType.bool())
                                            .executes(ctx -> {
                                                boolean e = BoolArgumentType.getBool(ctx, "enabled");
                                                ReplaySettings.getInstance().setRecordParticles(e);
                                                sendPlayerMessage("§a[Replay] Record particles: " + e);
                                                return 1;
                                            })))));
        });
    }

    private static void sendPlayerMessage(String msg) {
        MinecraftClient c = MinecraftClient.getInstance();
        if (c.player != null)
            c.player.sendMessage(Text.of(msg), false);
    }

    /*
     * ------------------------------------------------------------
     * PERSISTENCE UTIL
     * ------------------------------------------------------------
     */
    public static void saveModEnabledFlag(ILeafMod mod) {
        Map<String, Object> root = ModSettingsFileManager.readModSettings();
        String key = mod.getName().toUpperCase();
        root.computeIfAbsent(key, k -> new HashMap<String, Object>());
        @SuppressWarnings("unchecked")
        Map<String, Object> section = (Map<String, Object>) root.get(key);
        section.put("enabled", mod.isEnabled());
        ModSettingsFileManager.writeModSettings(root);
    }
}
