package com.leafclient.screen;

import com.leafclient.LeafClient;
import com.leafclient.modules.ILeafMod;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.text.Text;
import net.minecraft.util.Identifier;

import java.util.List;

public class ModMenuScreen extends Screen {

    
    private static final int WINDOW_WIDTH = 580;
    private static final int WINDOW_HEIGHT = 300;
    private int windowX, windowY;

    
    private static final int BG_DARK = 0xFF1A1A1A;
    private static final int BG_MEDIUM = 0xFF2A2A2A;
    private static final int BG_LIGHT = 0xFF3A3A3A;
    private static final int ACCENT_BLUE = 0xFF5C9FFF;
    private static final int TEXT_WHITE = 0xFFFFFFFF;
    private static final int TEXT_GRAY = 0xFF999999;
    private static final int ENABLED_GREEN = 0xFF4CAF50;
    private static final int DISABLED_RED = 0xFFE74C3C;

    
    private static final int SIDEBAR_WIDTH = 160;
    private static final int HEADER_HEIGHT = 40;
    private static final int TAB_HEIGHT = 35;
    private static final int MOD_CARD_WIDTH = 170;
    private static final int MOD_CARD_HEIGHT = 140;
    private static final int CARD_SPACING = 10;

    
    private String selectedCategory = "ALL";
    private float scrollOffset = 0;
    private int maxScroll = 0;

    
    private static final String[] CATEGORIES = { "ALL", "NEW", "HUD", "SERVER", "MECHANIC" };

    
    private static final Identifier CUSTOM_FONT = new Identifier("leafclient", "custom_font");

    public ModMenuScreen() {
        super(Text.literal("Mod Menu"));
    }

    @Override
    protected void init() {
        super.init();

        this.windowX = (this.width - WINDOW_WIDTH) / 2;
        this.windowY = (this.height - WINDOW_HEIGHT) / 2;
        
        calculateMaxScroll();
    }

    private Text createStyledText(String text) {
        return Text.literal(text).styled(style -> style.withFont(CUSTOM_FONT));
    }

    private void drawCustomText(DrawContext context, String text, int x, int y, int color) {
        context.drawText(textRenderer, createStyledText(text), x, y, color, false);
    }

    private int textWidth(String text) {
        return textRenderer.getWidth(createStyledText(text));
    }

    @Override
    public void render(DrawContext context, int mouseX, int mouseY, float delta) {
        this.renderBackground(context);

        // Main window background
        context.fill(windowX, windowY, windowX + WINDOW_WIDTH, windowY + WINDOW_HEIGHT, BG_DARK);

        // Header
        renderHeader(context);

        // Sidebar
        renderSidebar(context, mouseX, mouseY);

        // Content area
        renderContent(context, mouseX, mouseY);

        super.render(context, mouseX, mouseY, delta);
    }

    private void renderHeader(DrawContext context) {
        int headerY = windowY;

        // Header background
        context.fill(windowX, headerY, windowX + WINDOW_WIDTH, headerY + HEADER_HEIGHT, BG_MEDIUM);

        // Logo and title
        drawCustomText(context, "LEAF CLIENT", windowX + 15, headerY + 15, TEXT_WHITE);

        // Tabs
        int tabX = windowX + WINDOW_WIDTH - 300;
        String[] tabs = { "MODS", "SETTINGS", "WAYPOINTS" };

        for (int i = 0; i < tabs.length; i++) {
            boolean active = i == 0;
            int color = active ? TEXT_WHITE : TEXT_GRAY;
            int bg = active ? BG_LIGHT : BG_MEDIUM;

            int tabWidth = 90;
            int x = tabX + (i * tabWidth);

            context.fill(x, headerY + 8, x + tabWidth - 5, headerY + HEADER_HEIGHT - 8, bg);
            drawCustomText(context, tabs[i], x + (tabWidth - textWidth(tabs[i])) / 2, headerY + 17, color);
        }

        // Close button
        drawCustomText(context, "X", windowX + WINDOW_WIDTH - 30, headerY + 15, TEXT_GRAY);
    }

    private void renderSidebar(DrawContext context, int mouseX, int mouseY) {
        int x = windowX;
        int y = windowY + HEADER_HEIGHT;
        int h = WINDOW_HEIGHT - HEADER_HEIGHT;

        context.fill(x, y, x + SIDEBAR_WIDTH, y + h, BG_MEDIUM);

        drawCustomText(context, "Default", x + 15, y + 15, TEXT_WHITE);

        String[] profiles = { "UHC", "Hypixel Skyblock", "Arena PvP" };
        for (int i = 0; i < profiles.length; i++) {
            drawCustomText(context, profiles[i], x + 15, y + 45 + (i * 25), TEXT_GRAY);
        }

        int btnY = y + h - 80;

        context.fill(x + 10, btnY, x + SIDEBAR_WIDTH - 10, btnY + 25, BG_LIGHT);
        drawCustomText(
                context,
                "SAVE AS NEW PROFILE",
                x + (SIDEBAR_WIDTH - textWidth("SAVE AS NEW PROFILE")) / 2,
                btnY + 8,
                TEXT_GRAY);

        btnY += 35;
        context.fill(x + 10, btnY, x + SIDEBAR_WIDTH - 10, btnY + 25, ACCENT_BLUE);
        drawCustomText(
                context,
                "EDIT HUD LAYOUT",
                x + (SIDEBAR_WIDTH - textWidth("EDIT HUD LAYOUT")) / 2,
                btnY + 8,
                TEXT_WHITE);
    }

    private void renderContent(DrawContext context, int mouseX, int mouseY) {

        int contentX = windowX + SIDEBAR_WIDTH;
        int contentY = windowY + HEADER_HEIGHT;
        int contentWidth = WINDOW_WIDTH - SIDEBAR_WIDTH;
        int contentHeight = WINDOW_HEIGHT - HEADER_HEIGHT;

        // Category Tabs
        int tabY = contentY + 10;

        for (int i = 0; i < CATEGORIES.length; i++) {
            String cat = CATEGORIES[i];
            boolean selected = cat.equals(selectedCategory);

            int w = 60;
            int x = contentX + 15 + (i * (w + 5));

            context.fill(x, tabY, x + w, tabY + TAB_HEIGHT - 10, selected ? ACCENT_BLUE : BG_LIGHT);
            drawCustomText(context, cat, x + (w - textWidth(cat)) / 2, tabY + 8, selected ? TEXT_WHITE : TEXT_GRAY);
        }

        // Grid
        int gridY = contentY + TAB_HEIGHT + 15;
        int gridHeight = contentHeight - TAB_HEIGHT - 25;

        context.enableScissor(contentX, gridY, contentX + contentWidth, gridY + gridHeight);

        List<ILeafMod> mods = getFilteredMods();
        int cols = 3;

        for (int i = 0; i < mods.size(); i++) {

            int col = i % cols;
            int row = i / cols;

            int cardX = contentX + 15 + col * (MOD_CARD_WIDTH + CARD_SPACING);
            int cardY = gridY + row * (MOD_CARD_HEIGHT + CARD_SPACING) - (int) scrollOffset;

            renderModCard(context, mods.get(i), cardX, cardY, mouseX, mouseY);
        }

        context.disableScissor();

        if (maxScroll > 0) {
            renderScrollbar(context, contentX + contentWidth - 8, gridY, gridHeight);
        }
    }

    private void renderModCard(DrawContext context, ILeafMod mod, int x, int y, int mouseX, int mouseY) {

        context.fill(x, y, x + MOD_CARD_WIDTH, y + MOD_CARD_HEIGHT, BG_MEDIUM);

        int icon = 40;
        int ix = x + (MOD_CARD_WIDTH - icon) / 2;
        context.fill(ix, y + 20, ix + icon, y + 20 + icon, BG_LIGHT);

        String name = mod.getName();
        drawCustomText(context, name, x + (MOD_CARD_WIDTH - textWidth(name)) / 2, y + 70, TEXT_WHITE);

        int btnY = y + 90;
        context.fill(x + 10, btnY, x + MOD_CARD_WIDTH - 10, btnY + 20, BG_LIGHT);
        drawCustomText(
                context,
                "OPTIONS",
                x + (MOD_CARD_WIDTH - textWidth("OPTIONS")) / 2,
                btnY + 6,
                TEXT_GRAY);

        btnY += 25;
        boolean on = mod.isEnabled();
        String text = on ? "ENABLED" : "DISABLED";

        context.fill(x + 10, btnY, x + MOD_CARD_WIDTH - 10, btnY + 20, on ? ENABLED_GREEN : DISABLED_RED);
        drawCustomText(
                context,
                text,
                x + (MOD_CARD_WIDTH - textWidth(text)) / 2,
                btnY + 6,
                TEXT_WHITE);
    }

    private void renderScrollbar(DrawContext context, int x, int y, int height) {

        context.fill(x, y, x + 6, y + height, BG_LIGHT);

        float p = scrollOffset / maxScroll;
        int h = Math.max(20, (int) (height * 0.3f));
        int sy = y + (int) ((height - h) * p);

        context.fill(x, sy, x + 6, sy + h, TEXT_GRAY);
    }

    private List<ILeafMod> getFilteredMods() {
        return LeafClient.modManager.getMods();
    }

    private void calculateMaxScroll() {

        int rows = (int) Math.ceil(getFilteredMods().size() / 3.0);
        int total = rows * (MOD_CARD_HEIGHT + CARD_SPACING);
        int visible = WINDOW_HEIGHT - HEADER_HEIGHT - TAB_HEIGHT - 40;

        maxScroll = Math.max(0, total - visible);
    }

    @Override
    public boolean mouseClicked(double mouseX, double mouseY, int button) {

        int contentX = windowX + SIDEBAR_WIDTH;
        int contentY = windowY + HEADER_HEIGHT;
        int gridY = contentY + TAB_HEIGHT + 15;

        // Category tabs
        int tabY = contentY + 10;
        for (int i = 0; i < CATEGORIES.length; i++) {

            int w = 60;
            int x = contentX + 15 + i * (w + 5);

            if (mouseX >= x && mouseX <= x + w && mouseY >= tabY && mouseY <= tabY + TAB_HEIGHT - 10) {
                selectedCategory = CATEGORIES[i];
                scrollOffset = 0;
                calculateMaxScroll();
                return true;
            }
        }

        // Toggle buttons
        List<ILeafMod> mods = getFilteredMods();
        for (int i = 0; i < mods.size(); i++) {

            int col = i % 3;
            int row = i / 3;

            int x = contentX + 15 + col * (MOD_CARD_WIDTH + CARD_SPACING);
            int y = gridY + row * (MOD_CARD_HEIGHT + CARD_SPACING) - (int) scrollOffset;
            int btnY = y + 115;

            if (mouseX >= x + 10 && mouseX <= x + MOD_CARD_WIDTH - 10 &&
                    mouseY >= btnY && mouseY <= btnY + 20) {

                ILeafMod mod = mods.get(i);
                mod.setEnabled(!mod.isEnabled());
                LeafClient.saveModEnabledFlag(mod);
                return true;
            }
        }

        // Close button
        if (mouseX >= windowX + WINDOW_WIDTH - 30 && mouseY >= windowY + 10 &&
                mouseX <= windowX + WINDOW_WIDTH - 15 && mouseY <= windowY + 30) {
            close();
            return true;
        }

        return super.mouseClicked(mouseX, mouseY, button);
    }

    @Override
    public boolean mouseScrolled(double mouseX, double mouseY, double amount) {

        if (maxScroll > 0) {
            scrollOffset -= amount * 15;
            scrollOffset = Math.max(0, Math.min(scrollOffset, maxScroll));
            return true;
        }

        return super.mouseScrolled(mouseX, mouseY, amount);
    }

    @Override
    public boolean shouldPause() {
        return false;
    }
}