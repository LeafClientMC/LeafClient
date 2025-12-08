package com.leafclient.ui;

import com.leafclient.LeafClient;
import com.leafclient.ModSettingsFileManager;
import com.leafclient.mixin.MinecraftClientAccessor;
import com.leafclient.modules.*;
import com.leafclient.util.render.MinimapRenderer;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.network.ClientPlayNetworkHandler;
import net.minecraft.client.network.ServerInfo;
import net.minecraft.item.ItemStack;
import com.leafclient.util.render.KeystrokesRenderer;
import com.leafclient.util.render.ItemCounterRenderer;
import com.leafclient.util.render.CrosshairRenderer;
import net.minecraft.util.Identifier;

import java.util.Map;

public class HudRenderer {

    private final MinecraftClient client;

    public HudRenderer(MinecraftClient client) {
        this.client = client;
    }

    public void render(DrawContext context) {
        
        if (client.world != null) {
            LeafClient.modManager.updateModStatesFromFile();
        }

        int screenWidth = client.getWindow().getScaledWidth();
        int screenHeight = client.getWindow().getScaledHeight();

        
        if (client.player != null) {
            renderLeftSideHuds(context, screenWidth, screenHeight);
            renderCompass(context, screenWidth);
            renderRightSideHuds(context, screenWidth, screenHeight);

            
            KeystrokesMod keystrokes = LeafClient.modManager.getMod(KeystrokesMod.class);
            if (keystrokes != null && keystrokes.isEnabled()) {
                HudPosition pos = getHudPosition("KEYSTROKES", 10, 10);
                float scale = ModSettingsFileManager.getHudScale("KEYSTROKES");

                context.getMatrices().push();
                context.getMatrices().translate(pos.getX(), pos.getY(), 0);
                context.getMatrices().scale(scale, scale, 1.0f);
                
                KeystrokesRenderer.render(context);
                
                context.getMatrices().pop();
            }
        }

        
        MinimapMod minimapMod = LeafClient.modManager.getMod(MinimapMod.class);
        if (minimapMod != null && minimapMod.isEnabled()) {
            MinimapRenderer.render(context, screenWidth, screenHeight);
        } else {
            MinimapRenderer.cleanup();
        }

        
        ItemCounterMod itemCounter = LeafClient.modManager.getMod(ItemCounterMod.class);
        if (itemCounter != null && itemCounter.isEnabled()) {
            HudPosition pos = getHudPosition("ITEMCOUNTER", 10, 300);
            float scale = ModSettingsFileManager.getHudScale("ITEMCOUNTER");

            context.getMatrices().push();
            context.getMatrices().translate(pos.getX(), pos.getY(), 0);
            context.getMatrices().scale(scale, scale, 1.0f);
            
            ItemCounterRenderer.render(context);
            
            context.getMatrices().pop();
        }

        
        CrosshairRenderer.render(context);
    }

    private HudPosition getHudPosition(String hudName, int defaultX, int defaultY) {
        Map<String, Object> posData = ModSettingsFileManager.getHudPosition(hudName);

        if (posData != null) {
            Object xObj = posData.get("x");
            Object yObj = posData.get("y");

            int x = (xObj instanceof Number) ? ((Number) xObj).intValue() : defaultX;
            int y = (yObj instanceof Number) ? ((Number) yObj).intValue() : defaultY;

            
            if (x == -1) x = defaultX;
            if (y == -1) y = defaultY;

            return new HudPosition(x, y);
        }

        
        ModSettingsFileManager.saveHudPosition(hudName, defaultX, defaultY);
        return new HudPosition(defaultX, defaultY);
    }

    private void renderLeftSideHuds(DrawContext context, int screenWidth, int screenHeight) {
        int defaultX = 5;
        int currentYPos = 5; 

        
        FPSMod fpsMod = LeafClient.modManager.getMod(FPSMod.class);
        if (fpsMod != null && fpsMod.isEnabled()) {
            String fpsText = ((MinecraftClientAccessor) client).getCurrentFps() + " FPS";
            renderScaledTextHud(context, "FPS", fpsText, defaultX, currentYPos, 0xFFFFFF);
            currentYPos = updateAutoY("FPS", currentYPos);
        }

        
        CPSMod cpsMod = LeafClient.modManager.getMod(CPSMod.class);
        if (cpsMod != null && cpsMod.isEnabled()) {
            String cpsText = String.format("CPS: %d | %d", cpsMod.getLeftCPS(), cpsMod.getRightCPS());
            renderScaledTextHud(context, "CPS", cpsText, defaultX, currentYPos, 0xFFFFFF);
            currentYPos = updateAutoY("CPS", currentYPos);
        }

        // Coordinates
        CoordinatesMod coordsMod = LeafClient.modManager.getMod(CoordinatesMod.class);
        if (coordsMod != null && coordsMod.isEnabled() && client.player != null) {
            HudPosition pos = getHudPosition("COORDINATES", defaultX, currentYPos);
            float scale = ModSettingsFileManager.getHudScale("COORDINATES");

            context.getMatrices().push();
            context.getMatrices().translate(pos.getX(), pos.getY(), 0);
            context.getMatrices().scale(scale, scale, 1.0f);
            
            // Draw content at 0,0 relative to translation
            drawCoordinatesContent(context);
            
            context.getMatrices().pop();

            // Update Auto Y
            Map<String, Object> posData = ModSettingsFileManager.getHudPosition("COORDINATES");
            if (posData == null || ((Number) posData.get("y")).intValue() == -1) {
                int boxHeight = (client.textRenderer.fontHeight * 5) + 12;
                currentYPos += (boxHeight * scale) + 5;
            }
        }

        // Performance Memory
        PerformanceMod performanceMod = LeafClient.modManager.getMod(PerformanceMod.class);
        if (performanceMod != null && performanceMod.isEnabled()) {
            if (performanceMod.isShowMemory()) {
                String memory = String.format("Mem: %d%%", performanceMod.getMemoryPercent());
                renderScaledTextHud(context, "PERFORMANCE_MEMORY", memory, defaultX, currentYPos, 0xFFFFFF);
                currentYPos = updateAutoY("PERFORMANCE_MEMORY", currentYPos);
            }

            // Performance CPU
            if (performanceMod.isShowCpu()) {
                double cpuUsage = performanceMod.getCpuUsage();
                String cpu = String.format("CPU: %.1f%%", cpuUsage);
                renderScaledTextHud(context, "PERFORMANCE_CPU", cpu, defaultX, currentYPos, 0xFFFFFF);
                currentYPos = updateAutoY("PERFORMANCE_CPU", currentYPos);
            }
        }
    }
    
    // Helper to render simple text HUDs with scaling
    private void renderScaledTextHud(DrawContext context, String key, String text, int defX, int defY, int color) {
        HudPosition pos = getHudPosition(key, defX, defY);
        float scale = ModSettingsFileManager.getHudScale(key);
        
        context.getMatrices().push();
        context.getMatrices().translate(pos.getX(), pos.getY(), 0);
        context.getMatrices().scale(scale, scale, 1.0f);
        
        drawSimpleHudBox(context, 0, 0, text, color); // Draw at 0,0
        
        context.getMatrices().pop();
    }
    
    // Helper to calculate next Y position based on scale
    private int updateAutoY(String key, int currentY) {
        Map<String, Object> posData = ModSettingsFileManager.getHudPosition(key);
        if (posData == null || ((Number) posData.get("y")).intValue() == -1) {
            float scale = ModSettingsFileManager.getHudScale(key);
            return currentY + (int)((client.textRenderer.fontHeight + 4) * scale) + 6;
        }
        return currentY;
    }

    private void renderCompass(DrawContext context, int screenWidth) {
        if (client.player == null) return;
    
        int barWidth = 200;
        int defaultX = (screenWidth / 2) - (barWidth / 2);
        int defaultY = 4;
    
        HudPosition pos = getHudPosition("DIRECTIONHUD", defaultX, defaultY);
        float scale = ModSettingsFileManager.getHudScale("DIRECTIONHUD");
        
        context.getMatrices().push();
        context.getMatrices().translate(pos.getX(), pos.getY(), 0);
        context.getMatrices().scale(scale, scale, 1.0f);
        
        // Calculate center relative to local 0,0
        int centerX = (barWidth / 2);
        int yPos = 0;
    
        float yaw = (client.player.getYaw() % 360 + 360) % 360;
        String yawText = String.format("%.0f", yaw);
        int yawWidth = client.textRenderer.getWidth(yawText);
        
        context.drawTextWithShadow(client.textRenderer, yawText, centerX - yawWidth / 2, yPos, 0xFFFFFF);
        yPos += client.textRenderer.fontHeight + 2;
        context.drawTextWithShadow(client.textRenderer, "▼", centerX - client.textRenderer.getWidth("▼") / 2, yPos, 0xFFFFFF);
        yPos += client.textRenderer.fontHeight + 2;
        
        drawCompassBar(context, centerX, yPos, yaw);
        
        context.getMatrices().pop();
    }

    private void drawCompassBar(DrawContext context, int centerX, int y, float yaw) {
        int barWidth = 200;
        int startX = centerX - barWidth / 2;

        float[] angles = { 0, 45, 90, 135, 180, 225, 270, 315, 360 };
        String[] labels = { "S", "SW", "W", "NW", "N", "NE", "E", "SE", "S" };

        for (int i = 0; i < angles.length; i++) {
            float angle = angles[i];
            float diff = angle - yaw;

            while (diff > 180) diff -= 360;
            while (diff < -180) diff += 360;

            float posX = centerX + (diff * 2.5f);

            if (posX >= startX && posX <= startX + barWidth) {
                float distFromCenter = Math.abs(posX - centerX);
                float maxDist = barWidth / 2f;
                float fadeAmount = 1.0f - (distFromCenter / maxDist);
                fadeAmount = Math.max(0.3f, fadeAmount);

                int alpha = (int) (fadeAmount * 255);
                int fadedColor = (alpha << 24) | 0xFFFFFF;

                context.fill((int) posX, y - 2, (int) posX + 1, y + 2, fadedColor);

                String angleText = String.format("%.0f", angle);
                context.drawTextWithShadow(client.textRenderer, angleText,
                        (int) posX - client.textRenderer.getWidth(angleText) / 2, y + 4, fadedColor);

                context.drawTextWithShadow(client.textRenderer, labels[i],
                        (int) posX - client.textRenderer.getWidth(labels[i]) / 2,
                        y + 4 + client.textRenderer.fontHeight, fadedColor);
            }
        }

        for (int angle = 0; angle < 360; angle += 15) {
            if (angle % 45 != 0) {
                float diff = angle - yaw;
                while (diff > 180) diff -= 360;
                while (diff < -180) diff += 360;

                float posX = centerX + (diff * 2.5f);

                if (posX >= startX && posX <= startX + barWidth) {
                    float distFromCenter = Math.abs(posX - centerX);
                    float maxDist = barWidth / 2f;
                    float fadeAmount = 1.0f - (distFromCenter / maxDist);
                    fadeAmount = Math.max(0.2f, fadeAmount);

                    int alpha = (int) (fadeAmount * 128);
                    int fadedColor = (alpha << 24) | 0xFFFFFF;

                    context.fill((int) posX, y, (int) posX + 1, y + 1, fadedColor);

                    String angleText = String.format("%d", angle);
                    int textAlpha = (int) (fadeAmount * 170);
                    int textColor = (textAlpha << 24) | 0xFFFFFF;
                    context.drawTextWithShadow(client.textRenderer, angleText,
                            (int) posX - client.textRenderer.getWidth(angleText) / 2, y + 4, textColor);
                }
            }
        }
    }

    private void renderRightSideHuds(DrawContext context, int screenWidth, int screenHeight) {
        int yPos = 5;

        // Ping
        PingMod pingMod = LeafClient.modManager.getMod(PingMod.class);
        if (pingMod != null && pingMod.isEnabled()) {
            String pingText = getPingText();
            if (pingText != null) {
                // FIX: Calculate total width based on FULL text ("Ping: 123 ms")
                int textWidth = client.textRenderer.getWidth(pingText);
                int defaultX = screenWidth - textWidth - 13;
                
                HudPosition pos = getHudPosition("PING", defaultX, yPos);
                float scale = ModSettingsFileManager.getHudScale("PING");

                context.getMatrices().push();
                context.getMatrices().translate(pos.getX(), pos.getY(), 0);
                context.getMatrices().scale(scale, scale, 1.0f);

                // Draw background for FULL width
                drawHudBoxBackground(context, 0, 0, textWidth + 8, client.textRenderer.fontHeight + 4);

                // Draw "Ping: "
                context.drawTextWithShadow(client.textRenderer, "Ping: ", 4, 2, 0xFFFFFF);
                
                // Draw Value
                String pingValue = getPingValue() + " ms";
                context.drawTextWithShadow(client.textRenderer, pingValue,
                        4 + client.textRenderer.getWidth("Ping: "), 2, getPingColor());
                
                context.getMatrices().pop();

                yPos = pos.getY() + (int)((client.textRenderer.fontHeight + 10) * scale);
            }
        }

        // Server Info
        ServerInfoMod serverInfoMod = LeafClient.modManager.getMod(ServerInfoMod.class);
        if (serverInfoMod != null && serverInfoMod.isEnabled()) {
            String serverText = getServerText();
            if (serverText != null) {
                // Get server icon if available
                ServerInfo serverInfo = client.getCurrentServerEntry();
                Identifier serverIcon = null;

                if (serverInfo != null) {
                    serverIcon = com.leafclient.texture.ServerIconManager.getServerIcon(serverInfo);
                }

                boolean hasIcon = serverIcon != null;
                int iconSize = 16;
                int iconPadding = hasIcon ? iconSize + 4 : 0;
                int textWidth = client.textRenderer.getWidth(serverText);
                int totalWidth = textWidth + iconPadding + 8;
                int defaultX = screenWidth - totalWidth - 5;

                HudPosition pos = getHudPosition("SERVERINFO", defaultX, yPos);
                float scale = ModSettingsFileManager.getHudScale("SERVERINFO");

                context.getMatrices().push();
                context.getMatrices().translate(pos.getX(), pos.getY(), 0);
                context.getMatrices().scale(scale, scale, 1.0f);

                // Draw background box
                int boxHeight = Math.max(iconSize + 4, client.textRenderer.fontHeight + 4);
                drawHudBoxBackground(context, 0, 0, totalWidth, boxHeight);

                int currentX = 4;

                // Draw server icon if available
                if (hasIcon) {
                    context.drawTexture(
                            serverIcon,
                            currentX,
                            2,
                            0, 0,
                            iconSize, iconSize,
                            iconSize, iconSize);
                    currentX += iconSize + 2;
                }

                // Draw server text
                context.drawTextWithShadow(
                        client.textRenderer,
                        serverText,
                        currentX,
                        (boxHeight - client.textRenderer.fontHeight) / 2,
                        0xFFFFFF);
                
                context.getMatrices().pop();
                yPos = pos.getY() + (int)((boxHeight + 5) * scale);
            }
        }

        // ArmorHUD
        ArmorHUDMod armorHUD = LeafClient.modManager.getMod(ArmorHUDMod.class);
        if (armorHUD != null && armorHUD.isEnabled() && client.player != null) {
            int defaultX = screenWidth - 16 - 50;
            int defaultY = screenHeight / 2 + 20;
            HudPosition pos = getHudPosition("ARMORHUD", defaultX, defaultY);
            float scale = ModSettingsFileManager.getHudScale("ARMORHUD");
            
            context.getMatrices().push();
            context.getMatrices().translate(pos.getX(), pos.getY(), 0);
            context.getMatrices().scale(scale, scale, 1.0f);
            
            renderArmorHUD(context, 0, 0);
            
            context.getMatrices().pop();
        }
    }

    private void renderArmorHUD(DrawContext context, int xPos, int yPos) {
        if (client.player == null) return;

        ItemStack helmet = client.player.getInventory().getArmorStack(3);
        ItemStack chestplate = client.player.getInventory().getArmorStack(2);
        ItemStack leggings = client.player.getInventory().getArmorStack(1);
        ItemStack boots = client.player.getInventory().getArmorStack(0);
        ItemStack mainHand = client.player.getInventory().getMainHandStack();

        int itemSize = 16;
        int spacing = 4;

        ItemStack[] items = { helmet, chestplate, leggings, boots, mainHand };

        for (ItemStack item : items) {
            if (!item.isEmpty()) {
                context.drawItem(item, xPos, yPos);
                context.drawItemInSlot(client.textRenderer, item, xPos, yPos);

                if (item.isDamageable()) {
                    int durability = item.getMaxDamage() - item.getDamage();
                    String durabilityText = String.valueOf(durability);
                    int textX = xPos + itemSize + 4;
                    int textY = yPos + (itemSize / 2) - (client.textRenderer.fontHeight / 2);

                    float durabilityPercent = (float) durability / item.getMaxDamage();
                    int color;
                    if (durabilityPercent > 0.5f) {
                        color = 0x00FF00;
                    } else if (durabilityPercent > 0.25f) {
                        color = 0xFFFF00;
                    } else {
                        color = 0xFF0000;
                    }

                    context.drawTextWithShadow(client.textRenderer, durabilityText, textX, textY, color);
                }
            }
            yPos += itemSize + spacing;
        }
    }

    private void drawCoordinatesContent(DrawContext context) {
        String xText = String.format("X: %.0f", client.player.getX());
        String yText = String.format("Y: %.0f", client.player.getY());
        String zText = String.format("Z: %.0f", client.player.getZ());

        int chunkX = (int) client.player.getX() >> 4;
        int chunkZ = (int) client.player.getZ() >> 4;
        String cText = String.format("C: %d/%d", chunkX, chunkZ);

        String biomeName = "Unknown";
        int biomeColor = 0xFFFFFF;
        if (client.world != null) {
            String rawBiomeName = client.player.getWorld().getBiome(client.player.getBlockPos()).getKey().get()
                    .getValue().toString();
            String biomeId = rawBiomeName.substring(rawBiomeName.indexOf(':') + 1);

            // Special case for The Void
            if (biomeId.equals("the_void")) {
                biomeName = "The Void";
                biomeColor = 0x000000; // Black
            } else {
                biomeName = biomeId.replace('_', ' ');
                biomeName = biomeName.substring(0, 1).toUpperCase() + biomeName.substring(1);
                biomeColor = getBiomeColor(biomeName);
            }
        }

        String direction = getDirection();
        String facing = getFacingSymbol();

        int maxWidth = Math.max(
                Math.max(client.textRenderer.getWidth(xText + "  " + facing),
                        client.textRenderer.getWidth(yText + "  " + direction)),
                Math.max(client.textRenderer.getWidth(zText + "  " + facing),
                        client.textRenderer.getWidth(cText)));
        maxWidth = Math.max(maxWidth, client.textRenderer.getWidth("Biome: " + biomeName));

        int boxHeight = (client.textRenderer.fontHeight * 5) + 12;

        drawHudBoxBackground(context, 0, 0, maxWidth + 8, boxHeight);

        context.drawTextWithShadow(client.textRenderer, xText, 4, 4, 0xFFFFFF);
        context.drawTextWithShadow(client.textRenderer, facing,
                maxWidth - client.textRenderer.getWidth(facing), 4, 0xFFFFFF);

        context.drawTextWithShadow(client.textRenderer, yText, 4,
                4 + client.textRenderer.fontHeight, 0xFFFFFF);
        context.drawTextWithShadow(client.textRenderer, direction,
                maxWidth - client.textRenderer.getWidth(direction),
                4 + client.textRenderer.fontHeight, 0xFFFFFF);

        context.drawTextWithShadow(client.textRenderer, zText, 4,
                4 + client.textRenderer.fontHeight * 2, 0xFFFFFF);
        context.drawTextWithShadow(client.textRenderer, facing,
                maxWidth - client.textRenderer.getWidth(facing),
                4 + client.textRenderer.fontHeight * 2, 0xFFFFFF);

        context.drawTextWithShadow(client.textRenderer, cText, 4,
                4 + client.textRenderer.fontHeight * 3, 0xFFFFFF);

        context.drawTextWithShadow(client.textRenderer, "Biome: ", 4,
                4 + client.textRenderer.fontHeight * 4, 0xFFFFFF);
        context.drawTextWithShadow(client.textRenderer, biomeName,
                4 + client.textRenderer.getWidth("Biome: "),
                4 + client.textRenderer.fontHeight * 4, biomeColor);
    }

    private void drawSimpleHudBox(DrawContext context, int x, int y, String text, int textColor) {
        int textWidth = client.textRenderer.getWidth(text);
        int boxWidth = textWidth + 8;
        int boxHeight = client.textRenderer.fontHeight + 4;

        drawHudBoxBackground(context, x, y, boxWidth, boxHeight);
        context.drawTextWithShadow(client.textRenderer, text, x + 4, y + 2, textColor);
    }

    private void drawHudBoxBackground(DrawContext context, int x, int y, int width, int height) {
        context.fill(x, y, x + width, y + height, 0x90000000);
    }

    private String getDirection() {
        if (client.player == null)
            return "";

        float yaw = (client.player.getYaw() % 360 + 360) % 360;

        if (yaw >= 337.5 || yaw < 22.5)
            return "S";
        else if (yaw >= 22.5 && yaw < 67.5)
            return "SW";
        else if (yaw >= 67.5 && yaw < 112.5)
            return "W";
        else if (yaw >= 112.5 && yaw < 157.5)
            return "NW";
        else if (yaw >= 157.5 && yaw < 202.5)
            return "N";
        else if (yaw >= 202.5 && yaw < 247.5)
            return "NE";
        else if (yaw >= 247.5 && yaw < 292.5)
            return "E";
        else
            return "SE";
    }

    private String getFacingSymbol() {
        if (client.player == null)
            return "";

        float yaw = (client.player.getYaw() % 360 + 360) % 360;

        if ((yaw >= 135 && yaw < 225) || (yaw >= 315 || yaw < 45)) {
            return "+";
        } else {
            return "-";
        }
    }

    private String getPingText() {
        if (client.player == null || client.getNetworkHandler() == null)
            return null;

        ClientPlayNetworkHandler networkHandler = client.getNetworkHandler();
        if (networkHandler.getPlayerListEntry(client.player.getUuid()) != null) {
            int ping = networkHandler.getPlayerListEntry(client.player.getUuid()).getLatency();
            return "Ping: " + ping + " ms";
        }

        return null;
    }

    private int getPingValue() {
        if (client.player == null || client.getNetworkHandler() == null)
            return 0;

        ClientPlayNetworkHandler networkHandler = client.getNetworkHandler();
        if (networkHandler.getPlayerListEntry(client.player.getUuid()) != null) {
            return networkHandler.getPlayerListEntry(client.player.getUuid()).getLatency();
        }

        return 0;
    }

    private int getPingColor() {
        if (client.player == null || client.getNetworkHandler() == null)
            return 0xFFFFFF;

        ClientPlayNetworkHandler networkHandler = client.getNetworkHandler();
        if (networkHandler.getPlayerListEntry(client.player.getUuid()) != null) {
            int ping = networkHandler.getPlayerListEntry(client.player.getUuid()).getLatency();

            if (ping < 50)
                return 0x00FF00;
            else if (ping < 100)
                return 0xFFFF00;
            else if (ping < 200)
                return 0xFFA500;
            else
                return 0xFF0000;
        }

        return 0xFFFFFF;
    }

    private String getServerText() {
        if (client.isInSingleplayer()) {
            return "Singleplayer";
        } else if (client.getCurrentServerEntry() != null) {
            ServerInfo serverInfo = client.getCurrentServerEntry();
            return serverInfo.address;
        }
        return null;
    }

    private int getBiomeColor(String biomeName) {
        biomeName = biomeName.toLowerCase();

        // Special case for The Void
        if (biomeName.contains("void"))
            return 0x8B00FF; // Dark Purple

        // Plains / Grasslands
        if (biomeName.contains("plains") && !biomeName.contains("sunflower") && !biomeName.contains("snowy"))
            return 0x00FF00; // Green
        if (biomeName.contains("sunflower"))
            return 0x9ACD32; // Yellow-Green
        if (biomeName.contains("meadow"))
            return 0x90EE90; // Light Green
        if (biomeName.contains("flower_forest"))
            return 0xFFC0CB; // Pink
        if (biomeName.contains("cherry_grove") || biomeName.contains("cherry grove"))
            return 0xFFB6C1; // Pink-White

        // Forests
        if (biomeName.contains("forest") && !biomeName.contains("dark") && !biomeName.contains("birch")
                && !biomeName.contains("flower"))
            return 0x228B22; // Green
        if (biomeName.contains("birch") && !biomeName.contains("old_growth"))
            return 0x90EE90; // Light Green
        if (biomeName.contains("old_growth_birch") || biomeName.contains("old growth birch"))
            return 0xC1FFC1; // Pale Green
        if (biomeName.contains("dark_forest") || biomeName.contains("dark forest"))
            return 0x013220; // Dark Green
        if (biomeName.contains("old_growth_pine") || biomeName.contains("old growth pine"))
            return 0x0B6623; // Deep Green
        if (biomeName.contains("old_growth_spruce") || biomeName.contains("old growth spruce"))
            return 0x008B8B; // Dark Teal

        // Taiga
        if (biomeName.contains("taiga") && !biomeName.contains("snowy") && !biomeName.contains("old"))
            return 0x2E8B57; // Pine Green
        if (biomeName.contains("snowy_taiga") || biomeName.contains("snowy taiga"))
            return 0xF0FFF0; // White-Green

        // Jungles
        if (biomeName.contains("jungle") && !biomeName.contains("sparse") && !biomeName.contains("bamboo"))
            return 0x00FF7F; // Bright Green
        if (biomeName.contains("sparse_jungle") || biomeName.contains("sparse jungle"))
            return 0x32CD32; // Lime Green
        if (biomeName.contains("bamboo"))
            return 0xADFF2F; // Yellow-Green

        // Snowy / Cold Biomes
        if (biomeName.contains("snowy_plains") || biomeName.contains("snowy plains"))
            return 0xFFFFFF; // White
        if (biomeName.contains("ice_spikes") || biomeName.contains("ice spikes"))
            return 0xAFEEEE; // Ice Blue
        if (biomeName.contains("snowy_slopes") || biomeName.contains("snowy slopes"))
            return 0xD3D3D3; // White-Gray
        if (biomeName.contains("frozen_peaks") || biomeName.contains("frozen peaks"))
            return 0xADD8E6; // Light Blue
        if (biomeName.contains("jagged_peaks") || biomeName.contains("jagged peaks"))
            return 0x808080; // Gray
        if (biomeName.contains("snowy_beach") || biomeName.contains("snowy beach"))
            return 0xF5DEB3; // White-Tan
        if (biomeName.contains("frozen_river") || biomeName.contains("frozen river"))
            return 0xB0E0E6; // Pale Blue

        // Mountains / Hills
        if (biomeName.contains("windswept_hills") || biomeName.contains("windswept hills"))
            return 0x8FBC8F; // Gray-Green
        if (biomeName.contains("windswept_forest") || biomeName.contains("windswept forest"))
            return 0x013220; // Dark Green
        if (biomeName.contains("windswept_gravelly") || biomeName.contains("windswept gravelly"))
            return 0x696969; // Gray
        if (biomeName.contains("stony_peaks") || biomeName.contains("stony peaks"))
            return 0x708090; // Stone Gray

        // Deserts / Dry Biomes
        if (biomeName.contains("desert"))
            return 0xD2B48C; // Tan
        if (biomeName.contains("badlands") && !biomeName.contains("eroded") && !biomeName.contains("wooded"))
            return 0xFFA500; // Orange
        if (biomeName.contains("eroded_badlands") || biomeName.contains("eroded badlands"))
            return 0xFF4500; // Red-Orange
        if (biomeName.contains("wooded_badlands") || biomeName.contains("wooded badlands"))
            return 0xCC5500; // Dark Orange
        if (biomeName.contains("savanna") && !biomeName.contains("plateau") && !biomeName.contains("windswept"))
            return 0xFFFF00; // Yellow
        if (biomeName.contains("savanna_plateau") || biomeName.contains("savanna plateau"))
            return 0xFFD700; // Golden Yellow
        if (biomeName.contains("windswept_savanna") || biomeName.contains("windswept savanna"))
            return 0xDAA520; // Dusty Yellow

        // Swamps
        if (biomeName.contains("swamp") && !biomeName.contains("mangrove"))
            return 0x556B2F; // Murky Green
        if (biomeName.contains("mangrove"))
            return 0x2F4F2F; // Deep Green

        // Oceans / Beaches / Rivers
        if (biomeName.contains("ocean") && !biomeName.contains("deep") && !biomeName.contains("cold")
                && !biomeName.contains("lukewarm") && !biomeName.contains("warm") && !biomeName.contains("frozen"))
            return 0x0000FF; // Blue
        if (biomeName.contains("deep_ocean") && !biomeName.contains("cold") && !biomeName.contains("lukewarm")
                && !biomeName.contains("warm") && !biomeName.contains("frozen"))
            return 0x00008B; // Deep Blue
        if (biomeName.contains("cold_ocean") && !biomeName.contains("deep"))
            return 0x008080; // Teal
        if (biomeName.contains("deep_cold_ocean") || biomeName.contains("deep cold ocean"))
            return 0x006666; // Dark Teal
        if (biomeName.contains("lukewarm_ocean") && !biomeName.contains("deep"))
            return 0x00FFFF; // Aqua
        if (biomeName.contains("deep_lukewarm_ocean") || biomeName.contains("deep lukewarm ocean"))
            return 0x00CED1; // Deep Aqua
        if (biomeName.contains("warm_ocean") && !biomeName.contains("deep"))
            return 0x00FFFF; // Cyan
        if (biomeName.contains("deep_warm_ocean") || biomeName.contains("deep warm ocean"))
            return 0x008B8B; // Dark Cyan
        if (biomeName.contains("river") && !biomeName.contains("frozen"))
            return 0x20B2AA; // Blue-Green
        if (biomeName.contains("frozen_ocean") && !biomeName.contains("deep"))
            return 0xE0FFFF; // Pale Blue
        if (biomeName.contains("deep_frozen_ocean") || biomeName.contains("deep frozen ocean"))
            return 0xB0E0E6; // Icy Blue
        if (biomeName.contains("beach") && !biomeName.contains("snowy") && !biomeName.contains("stone"))
            return 0xF4A460; // Sand
        if (biomeName.contains("stone_shore") || biomeName.contains("stone shore") || biomeName.contains("stony_shore"))
            return 0xA9A9A9; // Gray

        // Underground Biomes
        if (biomeName.contains("dripstone"))
            return 0x8B4513; // Brown
        if (biomeName.contains("lush_caves") || biomeName.contains("lush caves"))
            return 0x7FFF00; // Bright Green
        if (biomeName.contains("deep_dark") || biomeName.contains("deep dark"))
            return 0x00008B; // Dark Blue
        if (biomeName.contains("mossy_caverns") || biomeName.contains("mossy caverns"))
            return 0x8FBC8F; // Moss Green

        // Nether Biomes
        if (biomeName.contains("nether_wastes") || biomeName.contains("nether wastes"))
            return 0xFF0000; // Red
        if (biomeName.contains("crimson"))
            return 0xDC143C; // Crimson
        if (biomeName.contains("warped"))
            return 0x008080; // Teal
        if (biomeName.contains("basalt"))
            return 0x2F4F4F; // Black-Gray
        if (biomeName.contains("soul_sand") || biomeName.contains("soul sand"))
            return 0x6495ED; // Dusty Blue

        // End Biomes
        if (biomeName.equals("the_end") || biomeName.equals("the end"))
            return 0xE6E6FA; // Pale Purple
        if (biomeName.contains("end_highlands") || biomeName.contains("end highlands"))
            return 0xDDA0DD; // Soft Purple
        if (biomeName.contains("end_midlands") || biomeName.contains("end midlands"))
            return 0xE6E6FA; // Lavender
        if (biomeName.contains("small_end_islands") || biomeName.contains("small end islands"))
            return 0xD8BFD8; // Light Purple
        if (biomeName.contains("end_barrens") || biomeName.contains("end barrens"))
            return 0x9370DB; // Gray-Purple

        // Default fallback
        return 0xFFFFFF; // White
    }
}