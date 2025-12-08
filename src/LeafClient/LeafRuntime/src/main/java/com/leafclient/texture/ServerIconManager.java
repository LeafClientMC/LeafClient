package com.leafclient.texture;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.network.ServerInfo;
import net.minecraft.client.texture.NativeImage;
import net.minecraft.client.texture.NativeImageBackedTexture;
import net.minecraft.util.Identifier;

import java.util.HashMap;
import java.util.Map;

public class ServerIconManager {
    
    private static final Map<String, Identifier> iconCache = new HashMap<>();
    private static final MinecraftClient client = MinecraftClient.getInstance();
    
    public static Identifier getServerIcon(ServerInfo serverInfo) {
        if (serverInfo == null) {
            return null;
        }
        
        String address = serverInfo.address;
        
        // Check cache first
        if (iconCache.containsKey(address)) {
            return iconCache.get(address);
        }
        
        // Try to get the favicon bytes
        try {
            byte[] iconBytes = serverInfo.getFavicon();
            if (iconBytes != null && iconBytes.length > 0) {
                // Create texture from bytes
                NativeImage nativeImage = NativeImage.read(new java.io.ByteArrayInputStream(iconBytes));
                NativeImageBackedTexture texture = new NativeImageBackedTexture(nativeImage);
                
                // Register texture
                Identifier iconId = new Identifier("leafclient", "server_icon_" + address.replaceAll("[^a-zA-Z0-9]", "_"));
                client.getTextureManager().registerTexture(iconId, texture);
                
                // Cache it
                iconCache.put(address, iconId);
                
                return iconId;
            }
        } catch (Exception e) {
            System.err.println("[LeafClient] Failed to load server icon: " + e.getMessage());
        }
        
        return null;
    }
    
    public static void clearCache() {
        for (Identifier id : iconCache.values()) {
            try {
                client.getTextureManager().destroyTexture(id);
            } catch (Exception e) {
                // Ignore
            }
        }
        iconCache.clear();
    }
}
