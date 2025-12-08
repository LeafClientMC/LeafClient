package com.leafclient.util.render;

import com.leafclient.LeafClient;
import com.leafclient.modules.WaypointsMod;
import com.mojang.blaze3d.systems.RenderSystem;
import net.fabricmc.fabric.api.client.rendering.v1.WorldRenderContext;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.font.TextRenderer;
import net.minecraft.client.render.*;
import net.minecraft.client.util.math.MatrixStack;
import net.minecraft.util.math.RotationAxis;
import net.minecraft.util.math.Vec3d;
import org.joml.Matrix4f;
import org.lwjgl.opengl.GL11;

import java.awt.*;

public class WaypointRenderer {

    private static final float MINIMAL_SCALE_FACTOR = 0.005f;
    private static final float MINIMAL_NAME_TAG_MIN_SCALE = 0.02f;
    private static final float BEAM_HALF_WIDTH = 0.05f;
    private static final double NAME_TAG_VERTICAL_OFFSET = 1.5;

    public static void render(WorldRenderContext context) {
        if(!LeafClient.modManager.getMod(WaypointsMod.class).isEnabled())
            return;

        RenderSystem.disableCull();
        RenderSystem.setShaderFogEnd(2000.0f);
        BackgroundRenderer.clearFog();
        RenderSystem.enableBlend();
        RenderSystem.setShaderColor(1.0f, 1.0f, 1.0f, 1.0f);
        RenderSystem.defaultBlendFunc();

        for (WaypointsMod.Waypoint waypoint : LeafClient.modManager.getMod(WaypointsMod.class).getWaypoints()) {
            renderWaypoint(context, waypoint);
        }

        RenderSystem.disableBlend();
        RenderSystem.setShaderColor(1.0f, 1.0f, 1.0f, 1.0f);
        RenderSystem.enableCull();
        RenderSystem.enableDepthTest();
    }

    private static void renderWaypoint(WorldRenderContext context, WaypointsMod.Waypoint waypoint) {
        MinecraftClient client = MinecraftClient.getInstance();
        Camera camera = client.gameRenderer.getCamera();
        Vec3d cameraPos = camera.getPos();

        double dx = waypoint.x - cameraPos.x;
        double dy = waypoint.y - cameraPos.y + NAME_TAG_VERTICAL_OFFSET;
        double dz = waypoint.z - cameraPos.z;

        double distance = Math.sqrt(dx * dx + dy * dy + dz * dz);

        MatrixStack matrices = context.matrixStack();
        matrices.push();
        matrices.translate(dx, dy, dz);

        matrices.push();
        matrices.translate(0, -NAME_TAG_VERTICAL_OFFSET, 0);
        renderBeam(matrices, waypoint);
        matrices.pop();

        matrices.multiply(RotationAxis.POSITIVE_Y.rotationDegrees(-camera.getYaw()));
        matrices.multiply(RotationAxis.POSITIVE_X.rotationDegrees(camera.getPitch()));

        renderNameTag(matrices, waypoint, distance);

        matrices.pop();
    }

    private static void renderBeam(MatrixStack matrices, WaypointsMod.Waypoint waypoint) {
        Tessellator tessellator = Tessellator.getInstance();
        BufferBuilder buffer = tessellator.getBuffer();

        RenderSystem.setShader(GameRenderer::getPositionColorProgram);
        RenderSystem.disableDepthTest();
        RenderSystem.depthFunc(GL11.GL_LEQUAL);
        RenderSystem.depthMask(false);

        matrices.push();
        Matrix4f matrix = matrices.peek().getPositionMatrix();

        float[] colors = new Color(waypoint.color).getColorComponents(null);
        float r = colors[0];
        float g = colors[1];
        float b = colors[2];
        float baseAlpha = 0.5f;
        float glowAlpha = 0.2f;

        float beamHeight = (float) (256 - waypoint.y);

        buffer.begin(VertexFormat.DrawMode.QUADS, VertexFormats.POSITION_COLOR);
        float glowHalfWidth = BEAM_HALF_WIDTH * 2.0f;
        drawBeamQuads(buffer, matrix, r, g, b, glowAlpha, glowHalfWidth, beamHeight);
        tessellator.draw();

        buffer.begin(VertexFormat.DrawMode.QUADS, VertexFormats.POSITION_COLOR);
        drawBeamQuads(buffer, matrix, r, g, b, baseAlpha, BEAM_HALF_WIDTH, beamHeight);
        tessellator.draw();

        matrices.pop();

        RenderSystem.depthMask(true);
        RenderSystem.depthFunc(GL11.GL_LESS);
        RenderSystem.enableDepthTest();
    }

    private static void drawBeamQuads(BufferBuilder buffer, Matrix4f matrix, float r, float g, float b, float alpha, float halfWidth, float beamHeight) {
        buffer.vertex(matrix, -halfWidth, 0, halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, -halfWidth, beamHeight, halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, halfWidth, beamHeight, halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, halfWidth, 0, halfWidth).color(r, g, b, alpha).next();

        buffer.vertex(matrix, halfWidth, 0, -halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, halfWidth, beamHeight, -halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, -halfWidth, beamHeight, -halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, -halfWidth, 0, -halfWidth).color(r, g, b, alpha).next();

        buffer.vertex(matrix, halfWidth, 0, halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, halfWidth, beamHeight, halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, halfWidth, beamHeight, -halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, halfWidth, 0, -halfWidth).color(r, g, b, alpha).next();

        buffer.vertex(matrix, -halfWidth, 0, -halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, -halfWidth, beamHeight, -halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, -halfWidth, beamHeight, halfWidth).color(r, g, b, alpha).next();
        buffer.vertex(matrix, -halfWidth, 0, halfWidth).color(r, g, b, alpha).next();
    }

    private static void renderNameTag(MatrixStack matrices, WaypointsMod.Waypoint waypoint, double distance) {
    MinecraftClient client = MinecraftClient.getInstance();
    TextRenderer textRenderer = client.textRenderer;

    float scale = (float) (distance * MINIMAL_SCALE_FACTOR);
    scale = Math.max(MINIMAL_NAME_TAG_MIN_SCALE, scale);

    matrices.push();
    matrices.scale(scale, scale, scale);

    Matrix4f matrix = matrices.peek().getPositionMatrix();

    String text = waypoint.name + " [" + Math.round(distance) + "m]";
    float textWidth = textRenderer.getWidth(text);
    final float TEXT_HEIGHT = 9.0f;

    Tessellator tessellator = Tessellator.getInstance();
    BufferBuilder buffer = tessellator.getBuffer();

    float padding = 2.0f;
    float boxWidth = textWidth + padding * 2;
    float boxHeight = TEXT_HEIGHT + padding * 2;

    float x1 = -boxWidth / 2;
    float x2 = boxWidth / 2;
    float y1 = -boxHeight / 2;
    float y2 = boxHeight / 2;

    RenderSystem.disableDepthTest();
    RenderSystem.disableCull();
    RenderSystem.setShader(GameRenderer::getPositionColorProgram);

    // Background - render both sides
    float bgA = 0.6f;
    buffer.begin(VertexFormat.DrawMode.QUADS, VertexFormats.POSITION_COLOR);
    // Front face
    buffer.vertex(matrix, x1, y1, 0).color(0.0f, 0.0f, 0.0f, bgA).next();
    buffer.vertex(matrix, x1, y2, 0).color(0.0f, 0.0f, 0.0f, bgA).next();
    buffer.vertex(matrix, x2, y2, 0).color(0.0f, 0.0f, 0.0f, bgA).next();
    buffer.vertex(matrix, x2, y1, 0).color(0.0f, 0.0f, 0.0f, bgA).next();
    // Back face
    buffer.vertex(matrix, x2, y1, 0).color(0.0f, 0.0f, 0.0f, bgA).next();
    buffer.vertex(matrix, x2, y2, 0).color(0.0f, 0.0f, 0.0f, bgA).next();
    buffer.vertex(matrix, x1, y2, 0).color(0.0f, 0.0f, 0.0f, bgA).next();
    buffer.vertex(matrix, x1, y1, 0).color(0.0f, 0.0f, 0.0f, bgA).next();
    tessellator.draw();

    float[] colors = new Color(waypoint.color).getColorComponents(null);
    float r = colors[0];
    float g = colors[1];
    float b = colors[2];
    float glowThickness = 1.5f;
    float z_glow = -0.001f;

    buffer.begin(VertexFormat.DrawMode.QUADS, VertexFormats.POSITION_COLOR);
    // Top glow
    buffer.vertex(matrix, x1, y1, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x1, y1 + glowThickness, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x2, y1 + glowThickness, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x2, y1, z_glow).color(r, g, b, 0.4f).next();
    // Bottom glow
    buffer.vertex(matrix, x1, y2 - glowThickness, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x1, y2, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x2, y2, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x2, y2 - glowThickness, z_glow).color(r, g, b, 0.4f).next();
    // Left glow
    buffer.vertex(matrix, x1, y1, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x1, y2, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x1 + glowThickness, y2, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x1 + glowThickness, y1, z_glow).color(r, g, b, 0.4f).next();
    // Right glow
    buffer.vertex(matrix, x2 - glowThickness, y1, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x2 - glowThickness, y2, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x2, y2, z_glow).color(r, g, b, 0.4f).next();
    buffer.vertex(matrix, x2, y1, z_glow).color(r, g, b, 0.4f).next();
    tessellator.draw();

    float borderThickness = 1.0f;
    float z_border = -0.002f;

    buffer.begin(VertexFormat.DrawMode.QUADS, VertexFormats.POSITION_COLOR);
    // Top border
    buffer.vertex(matrix, x1, y1, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x1, y1 + borderThickness, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x2, y1 + borderThickness, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x2, y1, z_border).color(r, g, b, 1.0f).next();
    // Bottom border
    buffer.vertex(matrix, x1, y2 - borderThickness, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x1, y2, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x2, y2, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x2, y2 - borderThickness, z_border).color(r, g, b, 1.0f).next();
    // Left border
    buffer.vertex(matrix, x1, y1, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x1, y2, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x1 + borderThickness, y2, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x1 + borderThickness, y1, z_border).color(r, g, b, 1.0f).next();
    // Right border
    buffer.vertex(matrix, x2 - borderThickness, y1, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x2 - borderThickness, y2, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x2, y2, z_border).color(r, g, b, 1.0f).next();
    buffer.vertex(matrix, x2, y1, z_border).color(r, g, b, 1.0f).next();
    tessellator.draw();

    // Render text - use drawWithShadow with immediate mode
    RenderSystem.enableBlend();
    RenderSystem.defaultBlendFunc();

    matrices.push();
    matrices.scale(-1, -1, 1); // Flip both horizontally and vertically to make text readable
    matrices.translate(0, 0, -0.01f); // Move text forward

    float textX = -textWidth / 2;
    float textY = -TEXT_HEIGHT / 2 + 1.0f;

    // Use immediate mode rendering
    textRenderer.draw(text, textX, textY, 0xFFFFFFFF, false, 
        matrices.peek().getPositionMatrix(),
        client.getBufferBuilders().getEntityVertexConsumers(),
        TextRenderer.TextLayerType.SEE_THROUGH, 0, 15728880);

    matrices.pop();

    // Force draw the text buffer
    client.getBufferBuilders().getEntityVertexConsumers().draw();

    RenderSystem.setShaderColor(1.0f, 1.0f, 1.0f, 1.0f);
    RenderSystem.enableCull();
    RenderSystem.enableDepthTest();

    matrices.pop();
}


}
