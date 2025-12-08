package com.leafclient.util.render;

import com.leafclient.LeafClient;
import com.leafclient.ModSettingsFileManager;
import com.leafclient.modules.MinimapMod;
import com.leafclient.modules.WaypointsMod;
import com.mojang.blaze3d.systems.RenderSystem;
import net.minecraft.block.BlockState;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.render.*;
import net.minecraft.client.texture.NativeImage;
import net.minecraft.client.texture.NativeImageBackedTexture;
import net.minecraft.registry.tag.FluidTags;
import net.minecraft.util.Identifier;
import net.minecraft.util.Util;
import net.minecraft.util.math.BlockPos;
import net.minecraft.util.math.RotationAxis;
import net.minecraft.world.Heightmap;
import net.minecraft.world.World;
import org.lwjgl.opengl.GL11;
import org.lwjgl.opengl.GL12;

import java.util.Map;
import java.util.concurrent.atomic.AtomicBoolean;

public class MinimapRenderer {

    private static final MinecraftClient client = MinecraftClient.getInstance();
    private static final Identifier MAP_TEXTURE_ID = new Identifier("leafclient", "minimap");

    /* ------------------------ runtime state ------------------------ */
    private static NativeImageBackedTexture mapTexture;
    private static NativeImage mapImage;
    private static boolean textureRegistered = false;
    private static int lastCenterX = Integer.MAX_VALUE;
    private static int lastCenterZ = Integer.MAX_VALUE;
    private static int lastTextureSize = 0;

    private static final int UPDATE_INTERVAL = 5;
    private static int tickCounter = 0;

    /* flag to stop worker when world is gone */
    private static final AtomicBoolean disposed = new AtomicBoolean(false);

    /* =================================================================================================
       Entry-point
       ================================================================================================= */
    public static void render(DrawContext ctx, int screenW, int screenH) {
        MinimapMod minimap = LeafClient.modManager.getMod(MinimapMod.class);
        
        if (minimap == null || !minimap.isEnabled()) {
            cleanup(); 
            return;
        }

        final int texSize = minimap.getMapSize();
        
        // --- SCALE LOGIC ---
        float scale = ModSettingsFileManager.getHudScale("MINIMAP");
        final int displaySize = (int)(minimap.getDisplaySize() * scale);

        /* ---------- (Re)initialise texture ---------- */
        boolean newTexture = false;
        if (mapTexture == null || mapImage == null || lastTextureSize != texSize || !textureRegistered) {
            initTexture(texSize);
            lastTextureSize = texSize;
            newTexture = true;
            lastCenterX = lastCenterZ = Integer.MAX_VALUE;
        }

        /* ---------- update buffer if required ---------- */
        int cx = (int) client.player.getX();
        int cz = (int) client.player.getZ();

        boolean moved = Math.abs(cx - lastCenterX) >= 1 || Math.abs(cz - lastCenterZ) >= 1;
        if (++tickCounter >= UPDATE_INTERVAL || newTexture || moved) {
            tickCounter = 0;
            lastCenterX = cx;
            lastCenterZ = cz;
            scheduleGeneration(cx, cz, texSize, minimap.getZoom());
        }

        /* ---------- screen position (From Settings Manager) ---------- */
        Map<String, Object> posData = ModSettingsFileManager.getHudPosition("MINIMAP");
        int mapX = (posData != null && posData.containsKey("x")) ? ((Number)posData.get("x")).intValue() : -1;
        int mapY = (posData != null && posData.containsKey("y")) ? ((Number)posData.get("y")).intValue() : -1;

        // Default Position (Top Right) if not set
        if (mapX == -1 || mapY == -1) {
            mapX = screenW - displaySize - 10;
            mapY = 10;
            ModSettingsFileManager.saveHudPosition("MINIMAP", mapX, mapY);
        }

        /* ---------- draw ---------- */
        drawMinimap(ctx, mapX, mapY, displaySize, texSize, minimap);
        drawPlayerArrow(ctx, mapX, mapY, displaySize);
        drawCardinals(ctx, mapX, mapY, displaySize);
        if (minimap.isShowCoordinates())
            drawCoords(ctx, mapX, mapY, displaySize);
    }

    /* =================================================================================================
       Texture allocation / disposal
       ================================================================================================= */
    private static void initTexture(int size) {
        try {
            disposed.set(false);
            if (mapImage != null) mapImage.close();
            if (mapTexture != null && textureRegistered) {
                client.getTextureManager().destroyTexture(MAP_TEXTURE_ID);
                textureRegistered = false;
            }
            mapImage   = new NativeImage(size, size, true);
            mapImage.fillRect(0, 0, size, size, 0xFF000000); // opaque black base
            mapTexture = new NativeImageBackedTexture(mapImage);
            client.getTextureManager().registerTexture(MAP_TEXTURE_ID, mapTexture);
            
            // FIX: Clamp texture to edge to prevent tiling/wrapping artifacts
            mapTexture.bindTexture();
            RenderSystem.texParameter(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_WRAP_S, GL12.GL_CLAMP_TO_EDGE);
            RenderSystem.texParameter(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_WRAP_T, GL12.GL_CLAMP_TO_EDGE);
            
            textureRegistered = true;
        } catch (Exception e) {
            System.err.println("[LeafClient] Minimap init failed: " + e.getMessage());
            cleanup();
        }
    }

    public static void cleanup() {
        disposed.set(true);
        if (mapTexture != null) {
            mapTexture.close();
            mapTexture = null;
        }
        if (mapImage != null) {
            mapImage.close();
            mapImage = null;
        }
        if (textureRegistered) {
            client.getTextureManager().destroyTexture(MAP_TEXTURE_ID);
        }
        textureRegistered = false;
    }

    /* =================================================================================================
       Background generation -> safe upload on render thread
       ================================================================================================= */
    private static void scheduleGeneration(int cx, int cz, int texSize, float bpp) {
        Util.getIoWorkerExecutor().execute(() -> {
            if (disposed.get()) return;
            if (client.world == null) return;
            
            generatePixels(cx, cz, texSize, bpp);
            
            RenderSystem.recordRenderCall(() -> {
                if (!disposed.get() && mapTexture != null && mapImage != null && textureRegistered) {
                    mapTexture.upload();
                    // Re-apply clamp on upload just in case
                    mapTexture.bindTexture();
                    RenderSystem.texParameter(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_WRAP_S, GL12.GL_CLAMP_TO_EDGE);
                    RenderSystem.texParameter(GL11.GL_TEXTURE_2D, GL11.GL_TEXTURE_WRAP_T, GL12.GL_CLAMP_TO_EDGE);
                }
            });
        });
    }

    private static void generatePixels(int centerX, int centerZ, int texSize, float bpp) {
        World world = client.world;
        if (world == null || mapImage == null) return;
    
        int half = texSize / 2;
        int bottomY = world.getBottomY();
        int step = Math.max(1, Math.round(bpp));
    
        BlockPos.Mutable probe = new BlockPos.Mutable();
    
        for (int px = 0; px < texSize; px++) {
            int baseX = centerX + (px - half) * step;
    
            for (int pz = 0; pz < texSize; pz++) {
                int baseZ = centerZ + (pz - half) * step;
    
                int rSum = 0, gSum = 0, bSum = 0, samples = 0;
    
                for (int ox = 0; ox < step; ox++) {
                    int worldX = baseX + ox;
                    for (int oz = 0; oz < step; oz++) {
                        int worldZ = baseZ + oz;
    
                        if (!world.isChunkLoaded(worldX >> 4, worldZ >> 4))
                            continue;
    
                        int topY = world.getTopY(Heightmap.Type.MOTION_BLOCKING, worldX, worldZ) - 1;
                        if (topY < bottomY) continue;
    
                        probe.set(worldX, topY, worldZ);
                        BlockState state = world.getBlockState(probe);
    
                        int argb = pickColor(world, probe, state);
                        rSum += (argb >> 16) & 0xFF;
                        gSum += (argb >> 8)  & 0xFF;
                        bSum +=  argb        & 0xFF;
                        samples++;
                    }
                }
    
                if (samples == 0) continue;
    
                int r = (rSum / samples) & 0xFF;
                int g = (gSum / samples) & 0xFF;
                int b = (bSum / samples) & 0xFF;
                int argb = 0xFF000000 | (r << 16) | (g << 8) | b;
    
                mapImage.setColor(px, texSize - 1 - pz, argbToAbgr(argb));
            }
        }
    }
    
    /* =================================================================================================
       Drawing helpers
       ================================================================================================= */
    private static void drawMinimap(DrawContext ctx, int x, int y, int size, int texSize, MinimapMod minimap) {
        // Border & Background
        ctx.fill(x - 2, y - 2, x + size + 2, y + size + 2, 0xFFFFFFFF);
        ctx.fill(x, y, x + size, y + size, 0xFF1A1A1A);

        ctx.getMatrices().push();
        try {
            double scale = client.getWindow().getScaleFactor();
            RenderSystem.enableScissor((int)(x * scale),
                                       (int)(client.getWindow().getHeight() - (y + size) * scale),
                                       (int)(size * scale),
                                       (int)(size * scale));

            ctx.getMatrices().translate(x + size / 2f, y + size / 2f, 0);
            ctx.getMatrices().multiply(RotationAxis.POSITIVE_Z.rotationDegrees(client.player.getYaw()));

            RenderSystem.enableBlend();
            RenderSystem.defaultBlendFunc();
            RenderSystem.setShader(GameRenderer::getPositionTexProgram);
            RenderSystem.setShaderTexture(0, MAP_TEXTURE_ID);

            // Calculate size to cover corners when rotated
            float drawSize = (float)(size * Math.sqrt(2));
            
            // Draw Texture stretched to cover the quad to fix tiling issues
            ctx.drawTexture(MAP_TEXTURE_ID,
                            (int)(-drawSize / 2), (int)(-drawSize / 2), // x, y
                            (int)drawSize, (int)drawSize,               // width, height
                            0, 0,                                       // u, v
                            texSize, texSize,                           // regionWidth, regionHeight
                            texSize, texSize);                          // textureWidth, textureHeight

            if (minimap.isShowWaypoints())
                renderWaypoints(ctx, size, lastCenterX, lastCenterZ, minimap.getZoom());

            RenderSystem.disableBlend();
        } finally {
            ctx.getMatrices().pop();
            RenderSystem.disableScissor();
        }
    }

    private static void drawPlayerArrow(DrawContext ctx, int mapX, int mapY, int size) {
        int cx = mapX + size / 2;
        int cy = mapY + size / 2;

        ctx.getMatrices().push();
        ctx.getMatrices().translate(cx, cy, 0);

        Tessellator tess = Tessellator.getInstance();
        BufferBuilder buf = tess.getBuffer();
        RenderSystem.setShader(GameRenderer::getPositionColorProgram);

        buf.begin(VertexFormat.DrawMode.TRIANGLES, VertexFormats.POSITION_COLOR);
        buf.vertex(ctx.getMatrices().peek().getPositionMatrix(), 0, -5, 0).color(1f, 1f, 1f, 1f).next();
        buf.vertex(ctx.getMatrices().peek().getPositionMatrix(), -4, 4, 0).color(1f, 1f, 1f, 1f).next();
        buf.vertex(ctx.getMatrices().peek().getPositionMatrix(), 4, 4, 0).color(1f, 1f, 1f, 1f).next();
        tess.draw();

        ctx.getMatrices().pop();
    }

    private static void drawCardinals(DrawContext ctx, int x, int y, int size) {
        float yaw = client.player.getYaw() + 180;
        float cx  = x + size / 2f;
        float cy  = y + size / 2f;
        float d   = size / 2f + 8f;

        drawCardinal(ctx, "N", 0   - yaw, cx, cy, d);
        drawCardinal(ctx, "E", 90  - yaw, cx, cy, d);
        drawCardinal(ctx, "S", 180 - yaw, cx, cy, d);
        drawCardinal(ctx, "W", 270 - yaw, cx, cy, d);
    }

    private static void drawCardinal(DrawContext ctx, String t, float a, float cx, float cy, float d) {
        float rad = (float) Math.toRadians(-a);
        float r   = d / Math.max(Math.abs((float)Math.cos(rad)), Math.abs((float)Math.sin(rad)));
        float tx  = cx + r * (float) Math.sin(rad);
        float ty  = cy - r * (float) Math.cos(rad);

        int w = client.textRenderer.getWidth(t);
        ctx.drawTextWithShadow(client.textRenderer, t,
                (int) (tx - w / 2f),
                (int) (ty - client.textRenderer.fontHeight / 2f),
                0xFFFFFFFF);
    }

    private static void renderWaypoints(DrawContext ctx, int dispSize, int playerX, int playerZ, float bpp) {
        WaypointsMod mod = LeafClient.modManager.getMod(WaypointsMod.class);
        if (mod == null || !mod.isEnabled()) return;

        for (WaypointsMod.Waypoint w : mod.getWaypoints()) {
            int sx = Math.round((w.x - playerX) / bpp);
            int sy = Math.round((w.z - playerZ) / bpp);

            if (Math.hypot(sx, sy) > dispSize / 2f * 1.5f) continue;

            ctx.fill(sx - 2, sy - 2, sx + 2, sy + 2, w.color | 0xFF000000);
        }
    }

    private static void drawCoords(DrawContext ctx, int mapX, int mapY, int size) {
        String txt = String.format("X: %.0f, Z: %.0f", client.player.getX(), client.player.getZ());
        int w = client.textRenderer.getWidth(txt);
        int tx = mapX + (size - w) / 2;
        int ty = mapY + size + 8;

        ctx.fill(tx - 2, ty - 1, tx + w + 2, ty + client.textRenderer.fontHeight, 0x90000000);
        ctx.drawTextWithShadow(client.textRenderer, txt, tx, ty, 0xFFFFFFFF);
    }

    /* =================================================================================================
       Colour helpers
       ================================================================================================= */
    private static int pickColor(World world, BlockPos pos, BlockState state) {
        if (state.getFluidState().isIn(FluidTags.WATER)) return 0xFF3F76E4;
        if (state.getFluidState().isIn(FluidTags.LAVA))  return 0xFFFF6600;

        int mc = state.getMapColor(world, pos).color;
        return mc == 0 ? 0xFF606060 : 0xFF000000 | mc;
    }

    private static int argbToAbgr(int c) {
        int a = c & 0xFF000000;
        int r = (c >> 16) & 0xFF;
        int g = c & 0x00FF00;
        int b = (c << 16) & 0x00FF0000;
        return a | b | g | r;
    }
}
