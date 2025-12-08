package com.leafclient.texture;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.texture.NativeImage;
import net.minecraft.client.texture.NativeImageBackedTexture;
import net.minecraft.resource.Resource;
import net.minecraft.util.Identifier;

import java.io.InputStream;
import java.util.LinkedList;
import java.util.Queue;

/**
 * Converts pure-white / near-white pixels to transparent and registers a runtime texture.
 */
public final class LogoTextureUtil {

    private LogoTextureUtil() {}

    public static Identifier processWhiteToTransparent(Identifier originalId, String outputPath) {
        try {
            MinecraftClient mc = MinecraftClient.getInstance();
            Resource res = mc.getResourceManager().getResource(originalId).orElse(null);
            if (res == null) return originalId;

            try (InputStream in = res.getInputStream()) {
                NativeImage img = NativeImage.read(in);
                int w = img.getWidth();
                int h = img.getHeight();

                final int THRESH = 250;
                
                // Flood fill from edges to only remove background white
                boolean[][] toRemove = new boolean[w][h];
                boolean[][] visited = new boolean[w][h];
                Queue<int[]> queue = new LinkedList<>();

                // Add all edge pixels that are white to the queue
                for (int x = 0; x < w; x++) {
                    checkAndAdd(img, queue, visited, x, 0, THRESH);
                    checkAndAdd(img, queue, visited, x, h - 1, THRESH);
                }
                for (int y = 0; y < h; y++) {
                    checkAndAdd(img, queue, visited, 0, y, THRESH);
                    checkAndAdd(img, queue, visited, w - 1, y, THRESH);
                }

                // Flood fill to find all connected white pixels from edges
                while (!queue.isEmpty()) {
                    int[] pos = queue.poll();
                    int x = pos[0];
                    int y = pos[1];
                    
                    toRemove[x][y] = true;

                    // Check 4 neighbors
                    checkAndAdd(img, queue, visited, x + 1, y, THRESH);
                    checkAndAdd(img, queue, visited, x - 1, y, THRESH);
                    checkAndAdd(img, queue, visited, x, y + 1, THRESH);
                    checkAndAdd(img, queue, visited, x, y - 1, THRESH);
                }

                // Apply transparency only to background white pixels
                for (int y = 0; y < h; y++) {
                    for (int x = 0; x < w; x++) {
                        int abgr = img.getColor(x, y);
                        int a = (abgr >> 24) & 0xFF;
                        int b = (abgr >> 16) & 0xFF;
                        int g = (abgr >> 8) & 0xFF;
                        int r = (abgr) & 0xFF;

                        if (toRemove[x][y]) {
                            a = 0;
                        }

                        img.setColor(x, y, (a << 24) | (b << 16) | (g << 8) | r);
                    }
                }

                NativeImageBackedTexture tex = new NativeImageBackedTexture(img);
                Identifier outId = new Identifier("leafclient", outputPath);
                mc.getTextureManager().registerTexture(outId, tex);
                return outId;
            }
        } catch (Exception e) {
            System.err.println("[LeafClient] Logo transparency fix failed: " + e.getMessage());
            return originalId;
        }
    }

    private static void checkAndAdd(NativeImage img, Queue<int[]> queue, boolean[][] visited, int x, int y, int thresh) {
        int w = img.getWidth();
        int h = img.getHeight();
        
        if (x < 0 || x >= w || y < 0 || y >= h || visited[x][y]) {
            return;
        }

        int abgr = img.getColor(x, y);
        int r = (abgr) & 0xFF;
        int g = (abgr >> 8) & 0xFF;
        int b = (abgr >> 16) & 0xFF;

        if (r >= thresh && g >= thresh && b >= thresh) {
            visited[x][y] = true;
            queue.add(new int[]{x, y});
        }
    }
}