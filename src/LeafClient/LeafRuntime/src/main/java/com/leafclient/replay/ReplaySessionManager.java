package com.leafclient.replay;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.network.ClientPlayNetworkHandler;
import net.minecraft.network.ClientConnection;
import net.minecraft.text.Text;

/**
 * Manages entering/leaving a replay session by:
 * - Disconnecting from the real server.
 * - Attaching a fake ClientPlayNetworkHandler (future step).
 * - Feeding packets during replay ticks.
 */
public class ReplaySessionManager {

    private static boolean inReplaySession = false;
    private static ClientPlayNetworkHandler originalNetworkHandler;
    private static ClientConnection originalConnection;

    public static boolean isInReplaySession() {
        return inReplaySession;
    }

    public static void beginReplaySession() {
        MinecraftClient client = MinecraftClient.getInstance();

        // Already in a replay session
        if (inReplaySession) {
            return;
        }

        // If connected to a server, disconnect and remember the handler
        if (client.getNetworkHandler() != null) {
            originalNetworkHandler = client.getNetworkHandler();
            originalConnection = originalNetworkHandler.getConnection();

            System.out.println("[ReplaySession] Disconnecting from live server.");
            originalConnection.disconnect(Text.literal("[Replay] Starting replay session"));
        } else {
            originalNetworkHandler = null;
            originalConnection = null;
        }

        // At this point, there is no live network. The current world stays loaded
        // (as client-side snapshot). We will treat it as the replay world.

        inReplaySession = true;
        System.out.println("[ReplaySession] Replay session started.");
    }

    public static void endReplaySession() {
        if (!inReplaySession) return;

        // For now, we just mark the session as ended.
        // In the future, we can restore the original handler/connection if desired.
        inReplaySession = false;
        originalNetworkHandler = null;
        originalConnection = null;

        System.out.println("[ReplaySession] Replay session ended.");
    }
}
