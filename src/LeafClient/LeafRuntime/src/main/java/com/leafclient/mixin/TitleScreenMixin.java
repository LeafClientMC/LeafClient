package com.leafclient.mixin;

import com.leafclient.ui.CustomButtonWidget;
import com.mojang.blaze3d.systems.RenderSystem;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.screen.TitleScreen;
import net.minecraft.client.gui.screen.multiplayer.MultiplayerScreen;
import net.minecraft.client.gui.screen.world.SelectWorldScreen;
import net.minecraft.client.gui.screen.option.OptionsScreen;
import net.minecraft.client.gui.widget.ButtonWidget; // Added import for ButtonWidget
import net.minecraft.text.Text;
import net.minecraft.util.Identifier;
import net.minecraft.util.Util;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Unique;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;
import net.minecraft.client.render.*;
import com.mojang.blaze3d.platform.GlStateManager;
import net.minecraft.sound.SoundEvents;
import net.minecraft.client.sound.PositionedSoundInstance;
import java.io.File;

@Mixin(TitleScreen.class)
public abstract class TitleScreenMixin extends Screen {

    /* ------------------------------------------------------------------ */
    /* TEXTURES */
    /* ------------------------------------------------------------------ */
    private static final Identifier BG = id("bg3");
    private static final Identifier ICON_LOGO = id("logo_icon");
    private static final Identifier ICON_TEXT = id("logo_text");

    private static final Identifier BTN_SINGLE = idBtn("btn_singleplayer");
    private static final Identifier BTN_MULTI = idBtn("btn_multiplayer");
    private static final Identifier BTN_STORE = idBtn("btn_store");

    private static final Identifier I_ACCOUNTS = idIcn("btn_small_accounts");
    private static final Identifier I_DISCORD = idIcn("btn_small_discord");
    private static final Identifier I_CHANGELOG = idIcn("btn_small_changelog");
    private static final Identifier I_COSMETIC = idIcn("btn_small_cosmetics");
    private static final Identifier I_REALMS = idIcn("btn_small_realms");
    private static final Identifier I_REPLAY = idIcn("btn_small_replay");
    private static final Identifier I_MENU = idIcn("btn_small_settings");

    private static Identifier id(String p) {
        return new Identifier("leafclient", "textures/gui/title/" + p + ".png");
    }

    private static Identifier idBtn(String n) {
        return new Identifier("leafclient", "textures/gui/title/buttons/" + n + ".png");
    }

    private static Identifier idIcn(String n) {
        return new Identifier("leafclient", "textures/gui/title/icons/" + n + ".png");
    }

    /* ------------------------------------------------------------------ */
    /* LOGICAL (pre-GUI-scale) sizes */
    /* ------------------------------------------------------------------ */
    private static final int SMALL = 15;
    private static final int BIG_W = 170;
    private static final int BIG_H = 15;

    // Add these for ICON_TEXT size control
    private static final int TEXT_LOGO_W = 140;
    private static final int TEXT_LOGO_H = 20;

    /* ------------------------------------------------------------------ */
    /* BACKGROUND CONFIGURATION */
    /* ------------------------------------------------------------------ */
    private static final int BG_NATIVE_WIDTH = 728;
    private static final int BG_NATIVE_HEIGHT = 410;

    private static final float BG_SCROLL_SPEED = 0.5f;
    private static final float BG_MIN_ZOOM_FACTOR = 1.1f;

    // Blur configuration
    private static final int BLUR_PASSES = 15;
    private static final float BLUR_ALPHA = 0.12f;

    private float bgScrollX = 0f;
    private int bgScrollDirection = 1;

    // Account tooltip state tracking
    @Unique
    private int accountButtonX = 0;
    @Unique
    private int accountButtonY = 0;
    @Unique
    private int accountTooltipX = 0;
    @Unique
    private int accountTooltipY = 0;
    @Unique
    private int accountTooltipW = 0;
    @Unique
    private int accountTooltipH = 0;

    protected TitleScreenMixin(Text title) {
        super(title);
    }

    /* ------------------------------------------------------------------ */
    /* GLOBAL OPACITY (0.0 – 1.0) */
    /* ------------------------------------------------------------------ */
    @Unique
    private static float BUTTON_OPACITY = 0.55f;

    @Unique
    private static void setOpacity(float v) {
        BUTTON_OPACITY = Math.max(0f, Math.min(1f, v));
    }

    @Unique
    private static float getOpacity() {
        return BUTTON_OPACITY;
    }

    /* ------------------------------------------------------------------ */
    /* FILTER SETUP */
    /* ------------------------------------------------------------------ */
    private static boolean filterApplied = false;

    private static void applyLinearFiltering() {
        if (filterApplied)
            return;
        filterApplied = true;

        var tm = MinecraftClient.getInstance().getTextureManager();
        Identifier[] list = { BG, ICON_LOGO, ICON_TEXT,
                BTN_SINGLE, BTN_MULTI, BTN_STORE,
                I_ACCOUNTS, I_DISCORD, I_CHANGELOG, I_COSMETIC,
                I_REALMS, I_REPLAY, I_MENU };

        for (Identifier id : list) {
            var tex = tm.getTexture(id);
            if (tex != null)
                tex.setFilter(true, false);
        }
    }

    /* ------------------------------------------------------------------ */
    /* INIT */
    /* ------------------------------------------------------------------ */
    @Inject(method = "init", at = @At("HEAD"), cancellable = true)
    private void initLeaf(CallbackInfo ci) {
        ci.cancel();
        clearChildren();
        applyLinearFiltering();
    }

    /* ------------------------------------------------------------------ */
    /* RENDER */
    /* ------------------------------------------------------------------ */
    @Inject(method = "render", at = @At("HEAD"), cancellable = true)
    private void renderLeaf(DrawContext ctx, int mx, int my, float delta, CallbackInfo ci) {
        ci.cancel();

        RenderSystem.enableBlend();
        RenderSystem.defaultBlendFunc();

        // ------------------------------------------------------------
        // 1) SCALE: Fixed to "Cover" the screen + 10% Zoom for movement
        // ------------------------------------------------------------
        float screenAspectRatio = (float) this.width / this.height;
        float bgAspectRatio = (float) BG_NATIVE_WIDTH / BG_NATIVE_HEIGHT;

        float baseScale;
        if (screenAspectRatio > bgAspectRatio) {
            baseScale = (float) this.width / BG_NATIVE_WIDTH;
        } else {
            baseScale = (float) this.height / BG_NATIVE_HEIGHT;
        }

        // Ensure we always have slightly more image than screen to allow scrolling
        float finalScale = baseScale * BG_MIN_ZOOM_FACTOR;

        int bgRenderWidth = (int) (BG_NATIVE_WIDTH * finalScale);
        int bgRenderHeight = (int) (BG_NATIVE_HEIGHT * finalScale);

        // ------------------------------------------------------------
        // 2) SCROLL: Fixed to PAN (Ping-Pong) within bounds
        // ------------------------------------------------------------
        // Calculate the maximum we can scroll without seeing the edge
        float maxScroll = Math.max(0, bgRenderWidth - this.width);

        bgScrollX += delta * BG_SCROLL_SPEED * bgScrollDirection;

        // Clamp and Bounce
        if (bgScrollX > maxScroll) {
            bgScrollX = maxScroll;
            bgScrollDirection = -1;
        } else if (bgScrollX < 0f) {
            bgScrollX = 0f;
            bgScrollDirection = 1;
        }

        int drawX = -(int) bgScrollX;
        int drawY = (this.height - bgRenderHeight) / 2;

        // ------------------------------------------------------------
        // 3) DRAW BACKGROUND (No tiling needed)
        // ------------------------------------------------------------
        for (int pass = 0; pass <= BLUR_PASSES; pass++) {
            float alpha = (pass == BLUR_PASSES) ? 1f : BLUR_ALPHA;
            RenderSystem.setShaderColor(1f, 1f, 1f, alpha);

            // CORRECTED: Explicitly map the native texture size to the scaled render size
            ctx.drawTexture(
                    BG,
                    drawX, drawY, // X, Y
                    bgRenderWidth, bgRenderHeight, // Destination Width, Height (Screen)
                    0.0f, 0.0f, // U, V Start
                    BG_NATIVE_WIDTH, BG_NATIVE_HEIGHT, // Region Width, Height (Texture Source)
                    BG_NATIVE_WIDTH, BG_NATIVE_HEIGHT // Texture File Dimensions
            );
        }
        RenderSystem.setShaderColor(1f, 1f, 1f, 1f);

        ctx.fill(0, 0, this.width, this.height, 0x50000000);

        // ------------------------------------------------------------
        // 4) LOGO + BUTTONS (original size, smoothing on all UI textures)
        // ------------------------------------------------------------

        int iconW = 32, iconH = 32;
        int textLogoW = TEXT_LOGO_W;
        int textLogoH = TEXT_LOGO_H;

        int logoTextGap = 2;
        int textButtonGap = 16;
        int buttonGap = 6;

        int totalGroupHeight = iconH + logoTextGap + textLogoH + textButtonGap + (3 * BIG_H) + (2 * buttonGap);
        int groupStartY = this.height / 2 - totalGroupHeight / 2;
        int verticalOffset = -20;
        groupStartY += verticalOffset;

        var tm = MinecraftClient.getInstance().getTextureManager();
        Identifier[] smoothList = {
                ICON_LOGO, ICON_TEXT,
                BTN_SINGLE, BTN_MULTI, BTN_STORE,
                I_ACCOUNTS, I_DISCORD, I_CHANGELOG, I_COSMETIC,
                I_REALMS, I_REPLAY, I_MENU
        };
        for (Identifier id : smoothList) {
            var tex = tm.getTexture(id);
            if (tex != null)
                tex.setFilter(true, false);
        }

        // leaf icon
        int ix = this.width / 2 - iconW / 2;
        int iy = groupStartY;
        drawTexWithAlpha(ctx, ix, iy, iconW, iconH, ICON_LOGO);

        // text logo
        int textX = this.width / 2 - textLogoW / 2;
        int textY = iy + iconH + logoTextGap;
        drawTexWithAlpha(ctx, textX, textY, textLogoW, textLogoH, ICON_TEXT);

        // big buttons (same size as before, now with smoothing)
        int cx = this.width / 2 - BIG_W / 2;
        int sy = textY + textLogoH + textButtonGap;
        drawTexWithAlpha(ctx, cx, sy, BIG_W, BIG_H, BTN_SINGLE);
        drawTexWithAlpha(ctx, cx, sy + BIG_H + buttonGap, BIG_W, BIG_H, BTN_MULTI);
        drawTexWithAlpha(ctx, cx, sy + 2 * (BIG_H + buttonGap), BIG_W, BIG_H, BTN_STORE);

        // top-left icons
        int accX = 10, accY = 10;
        int discordX = accX + SMALL + 6, discordY = 10;

        // Store account button position
        accountButtonX = accX;
        accountButtonY = accY;

        drawTexWithAlpha(ctx, accX, accY, SMALL, SMALL, I_ACCOUNTS);
        drawTexWithAlpha(ctx, discordX, discordY, SMALL, SMALL, I_DISCORD);

        // bottom row icons
        int gapB = 6;
        int rowW = 5 * SMALL + 4 * gapB;
        int startX = this.width / 2 - rowW / 2;
        int by = this.height - SMALL - 10;
        Identifier[] arr = { I_CHANGELOG, I_COSMETIC, I_REALMS, I_REPLAY, I_MENU };
        String[] names = { "Changelog", "Cosmetics", "Realms", "Replay", "Settings" };

        for (int i = 0; i < 5; i++) {
            int bx = startX + i * (SMALL + gapB);
            drawTexWithAlpha(ctx, bx, by, SMALL, SMALL, arr[i]);
        }

        // tooltips for small buttons
        boolean mouseOverAccountButton = inside(mx, my, accX, accY, SMALL, SMALL);
        boolean mouseOverAccountTooltip = inside(mx, my, accountTooltipX, accountTooltipY, accountTooltipW,
                accountTooltipH);

        if (mouseOverAccountButton || mouseOverAccountTooltip) {
            String username = this.client.getSession().getUsername();
            drawAccountTooltip(ctx, mx, my, accX, accY + SMALL + 4, username);
        }

        // Discord tooltip
        if (inside(mx, my, discordX, discordY, SMALL, SMALL)) {
            drawSimpleTooltip(ctx, discordX + SMALL / 2, discordY + SMALL + 4, "Discord", true);
        }

        // Bottom row tooltips
        for (int i = 0; i < 5; i++) {
            int bx = startX + i * (SMALL + gapB);
            if (inside(mx, my, bx, by, SMALL, SMALL)) {
                drawSimpleTooltip(ctx, bx + SMALL / 2, by - 4, names[i], false);
            }
        }

        // footer
        String notice = "Not affiliated with Mojang or Microsoft. Do not distribute!";
        float scale = 0.7f;
        int w = textRenderer.getWidth(notice);
        ctx.getMatrices().push();
        ctx.getMatrices().scale(scale, scale, 1f);
        int x = (int) ((this.width - w * scale) / scale) - 4;
        int y = (int) ((this.height - textRenderer.fontHeight * scale) / scale) - 4;
        ctx.drawText(textRenderer, notice, x, y, 0x888888, false);
        ctx.getMatrices().pop();
    }

    private void drawSimpleTooltip(DrawContext ctx, int centerX, int y, String text, boolean below) {
        int w = textRenderer.getWidth(text) + 8;
        int h = 16;
        int x = centerX - w / 2;
        int ty = below ? y : y - h;

        ctx.fill(x, ty, x + w, ty + h, 0xD0000000);
        ctx.drawCenteredTextWithShadow(textRenderer, text, centerX, ty + 4, 0xFFFFFF);
    }

    private void drawAccountTooltip(DrawContext ctx, int mx, int my, int x, int y, String username) {
        int padding = 3;
        int headSize = 12;
        int textWidth = textRenderer.getWidth(username);
        int xButtonSize = 10;
        int gap = 4;

        // Calculate dimensions
        int w = padding + headSize + gap + textWidth + gap + xButtonSize + padding;
        int h = headSize + padding * 2;

        // Store tooltip bounds for hover detection
        accountTooltipX = x;
        accountTooltipY = y;
        accountTooltipW = w;
        accountTooltipH = h;

        // background
        ctx.fill(x, y, x + w, y + h, 0xD0000000);

        // player head (8x8 face from skin texture)
        Identifier skin = this.client.getSkinProvider()
                .loadSkin(this.client.getSession().getProfile());
        // Draw the face layer (u=8, v=8 is the front face in the skin texture)
        ctx.drawTexture(skin, x + padding, y + padding, headSize, headSize, 8.0f, 8.0f, 8, 8, 64, 64);

        // username
        int textY = y + (h - textRenderer.fontHeight) / 2;
        ctx.drawText(textRenderer, username, x + padding + headSize + gap, textY, 0xFFFFFF, true);

        // X button (logout)
        int xButtonX = x + w - padding - xButtonSize;
        int xButtonY = y + (h - xButtonSize) / 2;

        // X button background (red if hovered)
        boolean xButtonHovered = inside(mx, my, xButtonX, xButtonY, xButtonSize, xButtonSize);
        int xButtonColor = xButtonHovered ? 0xFFFF0000 : 0xFF880000;
        ctx.fill(xButtonX, xButtonY, xButtonX + xButtonSize, xButtonY + xButtonSize, xButtonColor);

        // Draw X symbol
        ctx.drawText(textRenderer, "X", xButtonX + 2, xButtonY + 1, 0xFFFFFF, false);
    }

    /* ------------------------------------------------------------------ */
    /* MOUSE CLICK */
    /* ------------------------------------------------------------------ */

    @Inject(method = "mouseClicked", at = @At("HEAD"), cancellable = true)
    private void onClickLeaf(double mx, double my, int button, CallbackInfoReturnable<Boolean> cir) {
        // Recalculate positions (same as render)
        int iconW = 32, iconH = 32;
        int textLogoW = TEXT_LOGO_W, textLogoH = TEXT_LOGO_H;
        int logoTextGap = 2;
        int textButtonGap = 16;
        int buttonGap = 6;

        int totalGroupHeight = iconH + logoTextGap + textLogoH + textButtonGap + (3 * BIG_H) + (2 * buttonGap);
        int groupStartY = this.height / 2 - totalGroupHeight / 2;
        int verticalOffset = -20;
        groupStartY += verticalOffset;

        int cx = this.width / 2 - BIG_W / 2;
        int sy = groupStartY + iconH + logoTextGap + textLogoH + textButtonGap;

        // Singleplayer (top button)
        if (inside(mx, my, cx, sy, BIG_W, BIG_H)) {
            this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));
            this.client.setScreen(new SelectWorldScreen(this));
            cir.setReturnValue(true);
            return;
        }
        // Multiplayer (middle button)
        else if (inside(mx, my, cx, sy + BIG_H + buttonGap, BIG_W, BIG_H)) {
            this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));
            this.client.setScreen(new MultiplayerScreen(this));
            cir.setReturnValue(true);
            return;
        }
        // Store (bottom big button)
        else if (inside(mx, my, cx, sy + 2 * (BIG_H + buttonGap), BIG_W, BIG_H)) {
            this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));

            this.client.setScreen(new Screen(Text.of("Store")) {

                @Override
                protected void init() {
                    int buttonWidth = 100;
                    int buttonHeight = 20;
                    int x = this.width / 2 - buttonWidth / 2;
                    int y = this.height / 2 + 20;

                    this.addDrawableChild(
                            ButtonWidget.builder(Text.of("Back"), (btn) -> this.client.setScreen(TitleScreenMixin.this))
                                    .dimensions(x, y, buttonWidth, buttonHeight)
                                    .build());
                }

                @Override
                public void render(DrawContext ctx, int mx2, int my2, float delta2) {
                    this.renderBackground(ctx);
                    super.render(ctx, mx2, my2, delta2);

                    String msg = "Store - Coming soon";
                    int msgWidth = textRenderer.getWidth(msg);
                    int x = this.width / 2 - msgWidth / 2;
                    int y = this.height / 2 - textRenderer.fontHeight / 2;
                    ctx.drawTextWithShadow(textRenderer, msg, x, y, 0xFFFFFF);
                }

                @Override
                public boolean shouldCloseOnEsc() {
                    return true;
                }

                @Override
                public void close() {
                    this.client.setScreen(TitleScreenMixin.this);
                }
            });

            cir.setReturnValue(true);
            return;
        }

        // Top-left buttons
        int accX = 10, accY = 10;
        int discordX = accX + SMALL + 6, discordY = 10;

        // Discord button
        if (inside(mx, my, discordX, discordY, SMALL, SMALL)) {
            this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));
            Util.getOperatingSystem().open("https://discord.gg/F4MW2CAT94");
            cir.setReturnValue(true);
            return;
        }

        // Account tooltip logout handling
        boolean mouseOverAccountButton = inside(mx, my, accX, accY, SMALL, SMALL);
        boolean mouseOverAccountTooltip = inside(mx, my, accountTooltipX, accountTooltipY, accountTooltipW,
                accountTooltipH);

        if (mouseOverAccountButton || mouseOverAccountTooltip) {
            int padding = 3;
            int headSize = 12;
            String username = this.client.getSession().getUsername();
            int textWidth = textRenderer.getWidth(username);
            int xButtonSize = 10;
            int gap = 4;
            int w = padding + headSize + gap + textWidth + gap + xButtonSize + padding;
            int h = headSize + padding * 2;

            int xButtonX = accountTooltipX + w - padding - xButtonSize;
            int xButtonY = accountTooltipY + (h - xButtonSize) / 2;

            if (inside(mx, my, xButtonX, xButtonY, xButtonSize, xButtonSize)) {
                this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));

                try {
                    File logoutFile = new File(this.client.runDirectory, "logout.signal");
                    logoutFile.createNewFile();
                    // Write timestamp just in case
                    java.nio.file.Files.write(
                            logoutFile.toPath(),
                            String.valueOf(System.currentTimeMillis()).getBytes());
                } catch (Exception ex) {
                    ex.printStackTrace();
                }

                this.client.scheduleStop();
                cir.setReturnValue(true);
                return;
            }
        }

        // Bottom row buttons
        int gapB = 6;
        int rowW = 5 * SMALL + 4 * gapB;
        int startX = this.width / 2 - rowW / 2;
        int by = this.height - SMALL - 10;
        int changelogBtnX = startX;
        if (inside(mx, my, changelogBtnX, by, SMALL, SMALL)) {
            this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));

            this.client.setScreen(new Screen(Text.of("Changelog")) {

                // Scroll position (in pixels)
                private double scrollOffset = 0;

                // Cached content height (without scroll applied)
                private int contentHeight = 0;

                // Vertical scroll step per wheel "tick"
                private static final int SCROLL_STEP = 12;

                @Override
                protected void init() {
                    int buttonWidth = 100;
                    int buttonHeight = 20;
                    int x = this.width / 2 - buttonWidth / 2;
                    int y = this.height - 35; // fixed, will NEVER be overlapped

                    this.addDrawableChild(
                            ButtonWidget.builder(Text.of("Back"), (btn) -> this.client.setScreen(TitleScreenMixin.this))
                                    .dimensions(x, y, buttonWidth, buttonHeight)
                                    .build());
                }

                public boolean mouseScrolled(double mouseX, double mouseY, double horizontal, double vertical) {
                    // vertical > 0 → scroll up, < 0 → scroll down
                    scrollOffset -= vertical * SCROLL_STEP;

                    // Visible scrollable area (top to just above the Back button)
                    int topY = 20;
                    int bottomY = this.height - 50; // a bit above the Back button
                    int visibleHeight = bottomY - topY;

                    // If content shorter than visible area, clamp to 0
                    if (contentHeight <= visibleHeight) {
                        scrollOffset = 0;
                        return true;
                    }

                    // contentHeight measured from topY: content extends from 0..contentHeight in
                    // its own coords
                    double minOffset = visibleHeight - contentHeight; // negative
                    double maxOffset = 0; // can't scroll past the top

                    if (scrollOffset > maxOffset)
                        scrollOffset = maxOffset;
                    if (scrollOffset < minOffset)
                        scrollOffset = minOffset;

                    return true;
                }

                @Override
                public void render(DrawContext ctx, int mx2, int my2, float delta2) {
                    this.renderBackground(ctx);
                    super.render(ctx, mx2, my2, delta2);

                    int color = 0xFFFFFF;

                    // Scrollable "page" region:
                    int topY = 20;
                    int bottomY = this.height - 50; // top edge of safe zone above Back button
                    int currentY = topY + (int) scrollOffset;

                    // Logo icon + logo text (scroll with content)
                    int iconW = 32, iconH = 32;
                    int textLogoWLocal = TEXT_LOGO_W, textLogoHLocal = TEXT_LOGO_H;
                    int logoGap = 4;

                    int iconX = this.width / 2 - iconW / 2;
                    int iconY = currentY;
                    drawTexWithAlpha(ctx, iconX, iconY, iconW, iconH, ICON_LOGO);

                    int logoTextX = this.width / 2 - textLogoWLocal / 2;
                    int logoTextY = iconY + iconH + logoGap;
                    drawTexWithAlpha(ctx, logoTextX, logoTextY, textLogoWLocal, textLogoHLocal, ICON_TEXT);

                    int titleY = logoTextY + textLogoHLocal + 12;
                    ctx.drawCenteredTextWithShadow(textRenderer, "Changelog", this.width / 2, titleY,
                            color);

                    // Changelog lines (left-aligned, block centered horizontally)
                    String[] lines = new String[] {
                            "v1.1 BETA",
                            "- Initial public build of Leaf Client.",
                            "- 20+ Mod Features",
                            "- Customizable Launcher",
                            "- Multiple Bug Fixes",
                            "- Custom Performance Mods Support (Lithium, Sodium, Iris, etc)",
                            "- Islamic Optional Prayer Time Reminder",
                            "",
                            "Planned future updates",
                            "- Custom Replay Mod & Replay Browser",
                            "- In-game Store",
                            "- Cosmetics Support with an Editor",
                            "- More mod features (revolving around PvP)",
                            "- More Bug Fixes",
                            "- More Custom Performance Mods Support",
                            "- Forge Support",
                            "- Chroma Support in HUDs"
                    };

                    int maxWidth = 0;
                    for (String line : lines) {
                        int lw = textRenderer.getWidth(line);
                        if (lw > maxWidth)
                            maxWidth = lw;
                    }
                    int blockLeftX = this.width / 2 - maxWidth / 2;

                    int lineY = titleY + textRenderer.fontHeight + 8;
                    for (String line : lines) {
                        if (line.isEmpty()) {
                            lineY += textRenderer.fontHeight;
                            continue;
                        }
                        // Only draw lines in the visible vertical range; optional but nice
                        if (lineY + textRenderer.fontHeight >= topY && lineY <= bottomY) {
                            ctx.drawTextWithShadow(textRenderer, line, blockLeftX, lineY, color);
                        }
                        lineY += textRenderer.fontHeight + 2;
                    }

                    // Update contentHeight (independent of scrollOffset)
                    contentHeight = lineY - topY;
                }

                @Override
                public boolean shouldCloseOnEsc() {
                    return true;
                }

                @Override
                public void close() {
                    this.client.setScreen(TitleScreenMixin.this);
                }
            });

            cir.setReturnValue(true);
            return;
        }

        // SETTINGS (index 4)
        int settingsBtnX = startX + 4 * (SMALL + gapB);
        if (inside(mx, my, settingsBtnX, by, SMALL, SMALL)) {
            this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));
            this.client.setScreen(new OptionsScreen(this, this.client.options));
            cir.setReturnValue(true);
            return;
        }

        // REALMS (index 2)
        int realmsBtnX = startX + 2 * (SMALL + gapB);
        if (inside(mx, my, realmsBtnX, by, SMALL, SMALL)) {
            this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));
            this.client.setScreen(new net.minecraft.client.realms.gui.screen.RealmsMainScreen(this));
            cir.setReturnValue(true);
            return;
        }

        // COSMETICS (index 1)
        int cosmeticsBtnX = startX + 1 * (SMALL + gapB);
        if (inside(mx, my, cosmeticsBtnX, by, SMALL, SMALL)) {
            this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));

            this.client.setScreen(new Screen(Text.of("Cosmetics")) {

                @Override
                protected void init() {
                    int buttonWidth = 100;
                    int buttonHeight = 20;
                    int x = this.width / 2 - buttonWidth / 2;
                    int y = this.height / 2 + 20;

                    this.addDrawableChild(
                            ButtonWidget.builder(Text.of("Back"), (btn) -> this.client.setScreen(TitleScreenMixin.this))
                                    .dimensions(x, y, buttonWidth, buttonHeight)
                                    .build());
                }

                @Override
                public void render(DrawContext ctx, int mx2, int my2, float delta2) {
                    this.renderBackground(ctx);
                    super.render(ctx, mx2, my2, delta2);

                    String msg = "Cosmetics - Coming soon";
                    int msgWidth = textRenderer.getWidth(msg);
                    int x = this.width / 2 - msgWidth / 2;
                    int y = this.height / 2 - textRenderer.fontHeight / 2;
                    ctx.drawTextWithShadow(textRenderer, msg, x, y, 0xFFFFFF);
                }

                @Override
                public boolean shouldCloseOnEsc() {
                    return true;
                }

                @Override
                public void close() {
                    this.client.setScreen(TitleScreenMixin.this);
                }
            });

            cir.setReturnValue(true);
            return;
        }

        // REPLAY (index 3)
        int replayBtnX = startX + 3 * (SMALL + gapB);
        if (inside(mx, my, replayBtnX, by, SMALL, SMALL)) {
            this.client.getSoundManager().play(PositionedSoundInstance.master(SoundEvents.UI_BUTTON_CLICK, 1.0f));

            this.client.setScreen(new Screen(Text.of("Replay")) {

                @Override
                protected void init() {
                    int buttonWidth = 100;
                    int buttonHeight = 20;
                    int x = this.width / 2 - buttonWidth / 2;
                    int y = this.height / 2 + 20;

                    this.addDrawableChild(
                            ButtonWidget.builder(Text.of("Back"), (btn) -> this.client.setScreen(TitleScreenMixin.this))
                                    .dimensions(x, y, buttonWidth, buttonHeight)
                                    .build());
                }

                @Override
                public void render(DrawContext ctx, int mx2, int my2, float delta2) {
                    this.renderBackground(ctx);
                    super.render(ctx, mx2, my2, delta2);

                    String msg = "Replay - Coming soon";
                    int msgWidth = textRenderer.getWidth(msg);
                    int x = this.width / 2 - msgWidth / 2;
                    int y = this.height / 2 - textRenderer.fontHeight / 2;
                    ctx.drawTextWithShadow(textRenderer, msg, x, y, 0xFFFFFF);
                }

                @Override
                public boolean shouldCloseOnEsc() {
                    return true;
                }

                @Override
                public void close() {
                    this.client.setScreen(TitleScreenMixin.this);
                }
            });

            cir.setReturnValue(true);
            return;
        }
    }

    /* ------------------------------------------------------------------ */
    /* HELPERS */
    /* ------------------------------------------------------------------ */
    private static boolean inside(double mx, double my, int x, int y, int w, int h) {
        return mx >= x && mx < x + w && my >= y && my < y + h;
    }

    private static void drawTexWithAlpha(DrawContext ctx, int x, int y, int w, int h, Identifier tex) {
        // Get the matrices
        var matrices = ctx.getMatrices();
        var matrix = matrices.peek().getPositionMatrix();

        // Bind texture
        RenderSystem.setShaderTexture(0, tex);
        RenderSystem.setShader(GameRenderer::getPositionTexProgram);
        RenderSystem.enableBlend();
        RenderSystem.blendFuncSeparate(
                GlStateManager.SrcFactor.SRC_ALPHA,
                GlStateManager.DstFactor.ONE_MINUS_SRC_ALPHA,
                GlStateManager.SrcFactor.ONE,
                GlStateManager.DstFactor.ONE_MINUS_SRC_ALPHA);

        // Apply global alpha
        RenderSystem.setShaderColor(1f, 1f, 1f, BUTTON_OPACITY);

        // Draw quad
        BufferBuilder buf = Tessellator.getInstance().getBuffer();
        buf.begin(VertexFormat.DrawMode.QUADS, VertexFormats.POSITION_TEXTURE);
        buf.vertex(matrix, x, y + h, 0).texture(0, 1).next();
        buf.vertex(matrix, x + w, y + h, 0).texture(1, 1).next();
        buf.vertex(matrix, x + w, y, 0).texture(1, 0).next();
        buf.vertex(matrix, x, y, 0).texture(0, 0).next();
        BufferRenderer.drawWithGlobalProgram(buf.end());

        RenderSystem.setShaderColor(1f, 1f, 1f, 1f);
        RenderSystem.defaultBlendFunc();
    }

}
