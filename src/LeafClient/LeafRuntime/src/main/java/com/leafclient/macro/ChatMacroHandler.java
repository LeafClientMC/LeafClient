// src/main/java/com/leafclient/macro/ChatMacroHandler.java
package com.leafclient.macro;

import com.leafclient.LeafClient;
import com.leafclient.modules.ChatMacrosMod;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.util.InputUtil;
import org.lwjgl.glfw.GLFW;

public class ChatMacroHandler {
    
    private static final MinecraftClient client = MinecraftClient.getInstance();
    
    public static void tick() {
        ChatMacrosMod mod = LeafClient.modManager.getMod(ChatMacrosMod.class);
        if (mod == null || !mod.isEnabled() || client.player == null) return;
        
        // Don't process macros if in a GUI (except our own macro editor)
        if (client.currentScreen != null) {
            // Reset all wasPressed states when in GUI
            for (ChatMacro macro : mod.getMacros()) {
                macro.setWasPressed(false);
            }
            return;
        }
        
        long windowHandle = client.getWindow().getHandle();
        
        for (ChatMacro macro : mod.getMacros()) {
            if (!macro.isEnabled()) continue;
            
            boolean isPressed = InputUtil.isKeyPressed(windowHandle, macro.getKey());
            
            if (isPressed) {
                // Check if we should send
                if (macro.isAllowRepeat()) {
                    // Repeat mode: send at intervals
                    if (macro.canSend()) {
                        sendMacro(mod, macro);
                    }
                } else {
                    // Single press mode: only send once per key press
                    if (!macro.wasPressed() && macro.canSend()) {
                        sendMacro(mod, macro);
                    }
                }
                macro.setWasPressed(true);
            } else {
                // Key released
                macro.setWasPressed(false);
            }
        }
    }
    
    private static void sendMacro(ChatMacrosMod mod, ChatMacro macro) {
        if (client.player == null || client.player.networkHandler == null) return;
        
        String message = mod.applyPlaceholders(macro.getMessage());
        
        // Send the message
        if (message.startsWith("/")) {
            // It's a command
            client.player.networkHandler.sendChatCommand(message.substring(1));
        } else {
            // It's a chat message
            client.player.networkHandler.sendChatMessage(message);
        }
        
        macro.setLastSentTimestamp(System.currentTimeMillis());
        
        System.out.println("[ChatMacros] Sent: " + message);
    }
}
