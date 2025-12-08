package com.leafclient.screen;

import com.leafclient.LeafClient;
import com.leafclient.ModSettingsFileManager;
import com.leafclient.modules.ILeafMod;
import com.leafclient.modules.MinimapMod;
import com.leafclient.modules.PerformanceMod;
import com.leafclient.ui.HudRenderer;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.text.Text;

import java.awt.Color;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public class HudEditorScreen extends Screen {

    private final Screen parent;
    private final HudRenderer hudRenderer;
    
    private String draggingKey = null;
    private String resizingKey = null;
    
    private int dragOffsetX = 0;
    private int dragOffsetY = 0;
    
    private float initialScale = 1.0f;
    private float initialWidth = 0;

    private Integer activeGuideX = null;
    private Integer activeGuideY = null;

    public HudEditorScreen(Screen parent) {
        super(Text.of("HUD Editor"));
        this.parent = parent;
        this.hudRenderer = new HudRenderer(MinecraftClient.getInstance());
    }

    @Override
    protected void init() {
        this.addDrawableChild(ButtonWidget.builder(Text.of("Done"), (btn) -> this.close())
                .dimensions(this.width / 2 - 50, this.height - 40, 100, 20).build());
    }

    @Override
    public void render(DrawContext context, int mouseX, int mouseY, float delta) {
        this.renderBackground(context); 
        context.fill(0, 0, width, height, new Color(0, 0, 0, 100).getRGB());

        // --- GRID RENDERING ---
        if (ModSettingsFileManager.isShowHudGrid()) {
            drawGrid(context);
        }

        hudRenderer.render(context);

        // Borders & Handles
        for (ILeafMod mod : LeafClient.modManager.getMods()) {
            if (!mod.isEnabled()) continue;

            if (mod instanceof PerformanceMod) {
                PerformanceMod pm = (PerformanceMod) mod;
                if (pm.isShowCpu()) renderEditorBox(context, "PERFORMANCE_CPU");
                if (pm.isShowMemory()) renderEditorBox(context, "PERFORMANCE_MEMORY");
            } 
            else if (isHudMod(mod)) {
                renderEditorBox(context, mod.getName().toUpperCase().replace(" ", ""));
            }
        }
        renderEditorBox(context, "DIRECTIONHUD");
        if (LeafClient.modManager.getMod(MinimapMod.class).isEnabled()) {
            renderEditorBox(context, "MINIMAP");
        }

        // Snap Guides
        if (activeGuideX != null) context.fill(activeGuideX, 0, activeGuideX + 1, height, 0xFF00FFFF); 
        if (activeGuideY != null) context.fill(0, activeGuideY, width, activeGuideY + 1, 0xFF00FFFF);

        super.render(context, mouseX, mouseY, delta);
    }

    private void drawGrid(DrawContext context) {
        int gridSize = 20;
        int color = 0x20FFFFFF; // Faint white
        for (int x = 0; x < width; x += gridSize) {
            context.fill(x, 0, x + 1, height, color);
        }
        for (int y = 0; y < height; y += gridSize) {
            context.fill(0, y, width, y + 1, color);
        }
    }

    private void renderEditorBox(DrawContext context, String key) {
        Rect bounds = getBounds(key);
        
        int color = Color.WHITE.getRGB();
        if (key.equals(draggingKey)) color = Color.GREEN.getRGB();
        if (key.equals(resizingKey)) color = Color.YELLOW.getRGB();

        drawBorder(context, bounds.x - 2, bounds.y - 2, bounds.w + 4, bounds.h + 4, color);
        
        int handleSize = 6;
        int hX = bounds.x + bounds.w + 2 - handleSize;
        int hY = bounds.y + bounds.h + 2 - handleSize;
        context.fill(hX, hY, hX + handleSize, hY + handleSize, 0xFF00FFFF);
    }

    private void drawBorder(DrawContext context, int x, int y, int width, int height, int color) {
        context.fill(x, y, x + width, y + 1, color); 
        context.fill(x, y + height - 1, x + width, y + height, color); 
        context.fill(x, y + 1, x + 1, y + height - 1, color); 
        context.fill(x + width - 1, y + 1, x + width, y + height - 1, color); 
    }

    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {
        if (button == 0) {
            String hitKey = checkAllCollisions(mouseX, mouseY, true);
            if (hitKey != null) {
                resizingKey = hitKey;
                initialScale = ModSettingsFileManager.getHudScale(hitKey);
                initialWidth = getUnscaledWidth(hitKey);
                return true;
            }

            hitKey = checkAllCollisions(mouseX, mouseY, false);
            if (hitKey != null) {
                draggingKey = hitKey;
                Rect b = getBounds(hitKey);
                dragOffsetX = (int)mouseX - b.x;
                dragOffsetY = (int)mouseY - b.y;
                return true;
            }
        }
        return super.mouseClicked(mouseX, mouseY, button);
    }

    private String checkAllCollisions(double mouseX, double mouseY, boolean checkHandle) {
        for (ILeafMod mod : LeafClient.modManager.getMods()) {
            if (!mod.isEnabled()) continue;
            if (mod instanceof PerformanceMod) {
                PerformanceMod pm = (PerformanceMod) mod;
                if (pm.isShowCpu() && checkCollision("PERFORMANCE_CPU", mouseX, mouseY, checkHandle)) return "PERFORMANCE_CPU";
                if (pm.isShowMemory() && checkCollision("PERFORMANCE_MEMORY", mouseX, mouseY, checkHandle)) return "PERFORMANCE_MEMORY";
            } else if (isHudMod(mod)) {
                String key = mod.getName().toUpperCase().replace(" ", "");
                if (checkCollision(key, mouseX, mouseY, checkHandle)) return key;
            }
        }
        if (checkCollision("DIRECTIONHUD", mouseX, mouseY, checkHandle)) return "DIRECTIONHUD";
        if (LeafClient.modManager.getMod(MinimapMod.class).isEnabled()) {
            if (checkCollision("MINIMAP", mouseX, mouseY, checkHandle)) return "MINIMAP";
        }
        return null;
    }

    private boolean checkCollision(String key, double mx, double my, boolean handle) {
        Rect b = getBounds(key);
        if (handle) {
            int hSize = 8;
            return mx >= (b.x + b.w - hSize) && mx <= (b.x + b.w + 4) &&
                   my >= (b.y + b.h - hSize) && my <= (b.y + b.h + 4);
        } else {
            return mx >= b.x - 2 && mx <= b.x + b.w + 2 && 
                   my >= b.y - 2 && my <= b.y + b.h + 2;
        }
    }

    @Override
    public boolean mouseDragged(double mouseX, double mouseY, int button, double deltaX, double deltaY) {
        if (resizingKey != null) {
            Rect currentBounds = getBounds(resizingKey);
            float currentWidth = (float) (mouseX - currentBounds.x);
            if (currentWidth < 10) currentWidth = 10;
            float newScale = currentWidth / initialWidth;
            if (newScale < 0.5f) newScale = 0.5f;
            if (newScale > 3.0f) newScale = 3.0f;
            ModSettingsFileManager.saveHudScale(resizingKey, newScale);
            return true;
        }

        if (draggingKey != null) {
            int proposedX = (int)mouseX - dragOffsetX;
            int proposedY = (int)mouseY - dragOffsetY;

            Rect myBounds = getBounds(draggingKey);
            activeGuideX = null;
            activeGuideY = null;

            // --- SNAPPING LOGIC ---
            if (ModSettingsFileManager.isHudSnappingEnabled()) {
                int snapRange = ModSettingsFileManager.getHudSnapRange();
                
                List<Rect> targets = new ArrayList<>();
                for (ILeafMod mod : LeafClient.modManager.getMods()) {
                    if (!mod.isEnabled()) continue;
                    if (mod instanceof PerformanceMod) {
                        PerformanceMod pm = (PerformanceMod) mod;
                        if (pm.isShowCpu() && !"PERFORMANCE_CPU".equals(draggingKey)) targets.add(getBounds("PERFORMANCE_CPU"));
                        if (pm.isShowMemory() && !"PERFORMANCE_MEMORY".equals(draggingKey)) targets.add(getBounds("PERFORMANCE_MEMORY"));
                    } else if (isHudMod(mod)) {
                        String key = mod.getName().toUpperCase().replace(" ", "");
                        if (!key.equals(draggingKey)) targets.add(getBounds(key));
                    }
                }
                if (!draggingKey.equals("DIRECTIONHUD")) targets.add(getBounds("DIRECTIONHUD"));
                if (!draggingKey.equals("MINIMAP") && LeafClient.modManager.getMod(MinimapMod.class).isEnabled()) 
                    targets.add(getBounds("MINIMAP"));

                for (Rect other : targets) {
                    if (activeGuideX == null) {
                        if (Math.abs(proposedX - other.x) <= snapRange) { proposedX = other.x; activeGuideX = other.x; }
                        else if (Math.abs(proposedX - (other.x + other.w)) <= snapRange) { proposedX = other.x + other.w; activeGuideX = other.x + other.w; }
                        else if (Math.abs((proposedX + myBounds.w) - other.x) <= snapRange) { proposedX = other.x - myBounds.w; activeGuideX = other.x; }
                        else if (Math.abs((proposedX + myBounds.w) - (other.x + other.w)) <= snapRange) { proposedX = other.x + other.w - myBounds.w; activeGuideX = other.x + other.w; }
                    }
                    if (activeGuideY == null) {
                        if (Math.abs(proposedY - other.y) <= snapRange) { proposedY = other.y; activeGuideY = other.y; }
                        else if (Math.abs(proposedY - (other.y + other.h)) <= snapRange) { proposedY = other.y + other.h; activeGuideY = other.y + other.h; }
                        else if (Math.abs((proposedY + myBounds.h) - other.y) <= snapRange) { proposedY = other.y - myBounds.h; activeGuideY = other.y; }
                        else if (Math.abs((proposedY + myBounds.h) - (other.y + other.h)) <= snapRange) { proposedY = other.y + other.h - myBounds.h; activeGuideY = other.y + other.h; }
                    }
                }

                int centerX = this.width / 2;
                int centerY = this.height / 2;
                if (activeGuideX == null && Math.abs((proposedX + myBounds.w / 2) - centerX) <= snapRange) { 
                    proposedX = centerX - (myBounds.w / 2); activeGuideX = centerX; 
                }
                if (activeGuideY == null && Math.abs((proposedY + myBounds.h / 2) - centerY) <= snapRange) { 
                    proposedY = centerY - (myBounds.h / 2); activeGuideY = centerY; 
                }
            }

            ModSettingsFileManager.setHudPosition(draggingKey, proposedX, proposedY);
            return true;
        }
        return super.mouseDragged(mouseX, mouseY, button, deltaX, deltaY);
    }

    @Override
    public boolean mouseReleased(double mouseX, double mouseY, int button) {
        if (draggingKey != null || resizingKey != null) {
            ModSettingsFileManager.save();
        }
        draggingKey = null;
        resizingKey = null;
        activeGuideX = null;
        activeGuideY = null;
        return super.mouseReleased(mouseX, mouseY, button);
    }

    @Override
    public void close() { this.client.setScreen(null); }

    private static class Rect { 
        int x, y, w, h; 
        Rect(int x, int y, int w, int h) { this.x = x; this.y = y; this.w = w; this.h = h; }
    }

    private Rect getBounds(String key) {
        Map<String, Object> pos = ModSettingsFileManager.getHudPosition(key);
        int x = (pos != null) ? ((Number)pos.get("x")).intValue() : 10;
        int y = (pos != null) ? ((Number)pos.get("y")).intValue() : 10;
        float scale = ModSettingsFileManager.getHudScale(key);

        float baseW = getUnscaledWidth(key);
        float baseH = getUnscaledHeight(key);

        return new Rect(x, y, (int)(baseW * scale), (int)(baseH * scale));
    }

    private float getUnscaledWidth(String key) {
        switch (key) {
            case "FPS": return client.textRenderer.getWidth("000 FPS") + 8;
            case "CPS": return client.textRenderer.getWidth("CPS: 00 | 00") + 8;
            case "PING": return client.textRenderer.getWidth("Ping: 999 ms") + 8;
            case "COORDINATES": return 100;
            case "ARMORHUD": return 16;
            case "KEYSTROKES": return 68;
            case "ITEMCOUNTER": return 80;
            case "DIRECTIONHUD": return 200;
            case "PERFORMANCE_CPU": return client.textRenderer.getWidth("CPU: 00.0%") + 8;
            case "PERFORMANCE_MEMORY": return client.textRenderer.getWidth("Mem: 100%") + 8;
            case "SERVERINFO": return 100;
            case "MINIMAP": // Added Minimap
                MinimapMod mm = LeafClient.modManager.getMod(MinimapMod.class);
                return mm != null ? mm.getDisplaySize() : 128;
        }
        return 50;
    }

    private float getUnscaledHeight(String key) {
        int fh = client.textRenderer.fontHeight;
        switch (key) {
            case "COORDINATES": return (fh * 5) + 12;
            case "ARMORHUD": return (16 + 4) * 5;
            case "KEYSTROKES": return 46;
            case "ITEMCOUNTER": return 80;
            case "DIRECTIONHUD": return 40;
            case "SERVERINFO": return 20;
            case "MINIMAP": // Added Minimap
                MinimapMod mm = LeafClient.modManager.getMod(MinimapMod.class);
                return mm != null ? mm.getDisplaySize() : 128;
        }
        return fh + 4;
    }

    private boolean isHudMod(ILeafMod mod) {
        String name = mod.getName().toUpperCase().replace(" ", "");
        return name.equals("FPS") || name.equals("CPS") || name.equals("COORDINATES") || 
               name.equals("ARMORHUD") || name.equals("KEYSTROKES") || 
               name.equals("PING") || name.equals("SERVERINFO") || name.equals("ITEMCOUNTER") ||
               name.equals("MINIMAP"); // ADDED MINIMAP
    }
}
