package com.leafclient.ui;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.texture.NativeImage;
import net.minecraft.client.texture.NativeImageBackedTexture;
import net.minecraft.util.Identifier;

import java.awt.Color;

public class TextureUtil {

    public static Identifier createRoundedRectTexture(int width, int height, Color color, int radius) {
        try {
            NativeImage image = new NativeImage(NativeImage.Format.RGBA, width, height, false);

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    boolean isRoundedCorner = (x < radius && y < radius) ||
                                              (x > width - radius - 1 && y < radius) ||
                                              (x < radius && y > height - radius - 1) ||
                                              (x > width - radius - 1 && y > height - radius - 1);

                    if (!isRoundedCorner) {
                        image.setColor(x, y, color.getRGB());
                    } else {
                        // Simple anti-aliasing for corners (can be improved)
                        double dist;
                        if (x < radius && y < radius) { // Top-left
                            dist = distance(x, y, radius, radius);
                        } else if (x > width - radius - 1 && y < radius) { // Top-right
                            dist = distance(x, y, width - radius - 1, radius);
                        } else if (x < radius && y > height - radius - 1) { // Bottom-left
                            dist = distance(x, y, radius, height - radius - 1);
                        } else { // Bottom-right
                            dist = distance(x, y, width - radius - 1, height - radius - 1);
                        }

                        if (dist <= radius) {
                            float alpha = (float) (1.0 - (dist - (radius - 1)) / 1.0); // Simple fade
                            int blendedColor = blendColors(color.getRGB(), 0x00000000, alpha);
                            image.setColor(x, y, blendedColor);
                        } else {
                            image.setColor(x, y, 0x00000000); // Transparent
                        }
                    }
                }
            }

            NativeImageBackedTexture texture = new NativeImageBackedTexture(image);
            Identifier identifier = new Identifier("leafclient", "dynamic_texture_" + System.nanoTime());
            MinecraftClient.getInstance().getTextureManager().registerTexture(identifier, texture);
            return identifier;
        } catch (Exception e) {
            e.printStackTrace();
            return null;
        }
    }

    private static double distance(int x1, int y1, int x2, int y2) {
        return Math.sqrt(Math.pow(x1 - x2, 2) + Math.pow(y1 - y2, 2));
    }

    private static int blendColors(int color1, int color2, float ratio) {
        float iRatio = 1.0f - ratio;

        int a1 = (color1 >> 24 & 0xff);
        int r1 = ((color1 & 0x00FF0000) >> 16);
        int g1 = ((color1 & 0x0000FF00) >> 8);
        int b1 = (color1 & 0x000000FF);

        int a2 = (color2 >> 24 & 0xff);
        int r2 = ((color2 & 0x00FF0000) >> 16);
        int g2 = ((color2 & 0x0000FF00) >> 8);
        int b2 = (color2 & 0x000000FF);

        int a = (int)((a1 * iRatio) + (a2 * ratio));
        int r = (int)((r1 * iRatio) + (r2 * ratio));
        int g = (int)((g1 * iRatio) + (g2 * ratio));
        int b = (int)((b1 * iRatio) + (b2 * ratio));

        return a << 24 | r << 16 | g << 8 | b;
    }
}