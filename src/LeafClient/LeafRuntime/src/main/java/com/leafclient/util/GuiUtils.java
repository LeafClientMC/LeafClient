package com.leafclient.util;

import net.minecraft.client.gui.DrawContext;
import net.minecraft.util.Identifier;

import java.awt.Color;

public class GuiUtils {

    /* ---------- TEXTURES ---------- */
    private static final Identifier LEAF_LOGO = new Identifier("leafclient", "leaf_logo.png");
    private static final Identifier LEAF_TEXT = new Identifier("leafclient", "leaf_client_text.png");

    /* ---------- BASIC SHAPES ---------- */

    public static void drawRoundedRect(DrawContext ctx, int x, int y, int w, int h, int r, Color col) {
        int argb = rgba(col);
        int x1 = x + r, y1 = y + r, x2 = x + w - r, y2 = y + h - r;

        // centre
        ctx.fill(x1, y1, x2, y2, argb);
        // sides
        ctx.fill(x,  y1, x1, y2, argb);
        ctx.fill(x2, y1, x + w, y2, argb);
        ctx.fill(x1, y,  x2, y1, argb);
        ctx.fill(x1, y2, x2, y + h, argb);

        // corners
        circlePart(ctx, x1, y1, r,   0,  90, argb);
        circlePart(ctx, x2, y1, r,  90, 180, argb);
        circlePart(ctx, x2, y2, r, 180, 270, argb);
        circlePart(ctx, x1, y2, r, 270, 360, argb);
    }

    public static void drawNinePatchRect(DrawContext ctx, int x, int y, int w, int h, Color bg) {
        // Placeholder: just a rounded rect until a 9-patch border texture is added
        drawRoundedRect(ctx, x, y, w, h, 4, bg);
    }

    private static void circlePart(DrawContext ctx, int cx, int cy, int r, int startDeg, int endDeg, int argb) {
        for (int a = startDeg; a < endDeg; a++) {
            double rad = Math.toRadians(a);
            int px = (int)(cx + Math.cos(rad) * r);
            int py = (int)(cy - Math.sin(rad) * r);
            ctx.fill(px, py, px + 1, py + 1, argb);
        }
    }

    private static int rgba(Color c) {
        return (c.getAlpha() << 24) | (c.getRed() << 16) | (c.getGreen() << 8) | c.getBlue();
    }

    /* ---------- LOGO / TEXT ---------- */

    public static void drawLeafLogo(DrawContext ctx, int x, int y, int sz) {
        ctx.drawTexture(LEAF_LOGO, x, y, 0, 0, sz, sz, sz, sz);
    }

    public static void drawLeafText(DrawContext ctx, int x, int y, int w, int h) {
        ctx.drawTexture(LEAF_TEXT, x, y, 0, 0, w, h, w, h);
    }
}
