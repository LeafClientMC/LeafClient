package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.LeafLogoMod;
import com.mojang.authlib.GameProfile;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.font.TextRenderer;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.hud.PlayerListHud;
import net.minecraft.client.network.PlayerListEntry;
import net.minecraft.text.OrderedText;
import net.minecraft.text.Text;
import net.minecraft.util.Identifier;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Unique;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Redirect;

@Mixin(PlayerListHud.class)
public abstract class PlayerListHudMixin {

    // ------------------------------------------------------------
    // STATE FLAGS (PER ENTRY)
    // ------------------------------------------------------------
    @Unique
    private boolean leaf$isLocalPlayer = false;

    @Unique
    private PlayerListEntry leaf$currentEntry = null;

    @Unique
    private boolean leaf$shouldDrawHead = true; // Added to control custom head drawing (Problem 2)

    // ------------------------------------------------------------
    // TEXTURES
    // ------------------------------------------------------------
    private static final Identifier LEAF_LOGO =
            new Identifier("leafclient", "textures/logo_icon_shadowed.png");

    // ------------------------------------------------------------
    // SIZE CONFIG
    // ------------------------------------------------------------
    private static final int HEAD_SIZE = 8;
    private static final int LOGO_SIZE = 8; // ✅ Reduced from 12 to fit better and align with head (Problem 1)
    private static final int PADDING = 1; // ✅ Reduced from 2 for a tighter fit (Problem 1)
    private static final int PING_BAR_WIDTH = 12; // ✅ New: Space for the ping bar (Problem 3)

    private static final int BASE_HEAD_SPACE = HEAD_SIZE + PADDING; // 9
    private static final int BASE_LOGO_SPACE = LOGO_SIZE + PADDING; // 9
    private static final int BASE_PING_SPACE = PING_BAR_WIDTH + PADDING; // 13

    // ------------------------------------------------------------
    // DETECT WHICH ENTRY IS BEING RENDERED
    // ------------------------------------------------------------
    @Redirect(
        method = "render",
        at = @At(
            value = "INVOKE",
            target = "Lnet/minecraft/client/network/PlayerListEntry;getProfile()Lcom/mojang/authlib/GameProfile;"
        )
    )
    private GameProfile captureEntry(PlayerListEntry entry) {

        this.leaf$currentEntry = entry;

        MinecraftClient client = MinecraftClient.getInstance();
        if (client != null &&
            entry.getProfile().getId().equals(client.player.getUuid())) {
            this.leaf$isLocalPlayer = true;
        } else {
            this.leaf$isLocalPlayer = false;
        }

        return entry.getProfile();
    }

    // ------------------------------------------------------------
    // WIDTH: NAME (Text)
    // ------------------------------------------------------------
    @Redirect(
        method = "render",
        at = @At(
            value = "INVOKE",
            target = "Lnet/minecraft/client/font/TextRenderer;getWidth(Lnet/minecraft/text/Text;)I"
        ),
        require = 0
    )
    private int expandWidthText(TextRenderer renderer, Text text) {

        int width = renderer.getWidth(text);

        if (leaf$shouldDrawHead) {
            width += BASE_HEAD_SPACE;
        }

        if (shouldDrawLogo()) {
            width += BASE_LOGO_SPACE;
        }

        width += BASE_PING_SPACE;

        return width;
    }

    // ------------------------------------------------------------
    // WIDTH: NAME (OrderedText)
    // ------------------------------------------------------------
    @Redirect(
        method = "render",
        at = @At(
            value = "INVOKE",
            target = "Lnet/minecraft/client/font/TextRenderer;getWidth(Lnet/minecraft/text/OrderedText;)I"
        ),
        require = 0
    )
    private int expandWidthOrdered(TextRenderer renderer, OrderedText text) {

        int width = renderer.getWidth(text);

        if (leaf$shouldDrawHead) {
            width += BASE_HEAD_SPACE;
        }

        if (shouldDrawLogo()) {
            width += BASE_LOGO_SPACE;
        }

        width += BASE_PING_SPACE;

        return width;
    }

    // ------------------------------------------------------------
    // DRAW: HEAD FOR ALL, LOGO FOR LOCAL, SHIFT NAME
    // ------------------------------------------------------------
    @Redirect(
        method = "render",
        at = @At(
            value = "INVOKE",
            target = "Lnet/minecraft/client/gui/DrawContext;drawTextWithShadow" +
                     "(Lnet/minecraft/client/font/TextRenderer;Lnet/minecraft/text/Text;III)I"
        )
    )
    private int renderIcons(DrawContext context, TextRenderer renderer,
                            Text text, int x, int y, int color) {

        MinecraftClient client = MinecraftClient.getInstance();
        if (client == null || leaf$currentEntry == null) {
            return context.drawTextWithShadow(renderer, text, x, y, color);
        }

        int drawX = x;

        // ------------------------------------------------
        // PLAYER HEAD (ALL PLAYERS) - Problem 2 fix: Conditional drawing
        // ------------------------------------------------
        if (leaf$shouldDrawHead) {
            Identifier skin = leaf$currentEntry.getSkinTexture();

            context.drawTexture(
                skin,
                drawX,
                y + 1,
                8, 8,
                HEAD_SIZE,
                HEAD_SIZE,
                64, 64
            );

            drawX += BASE_HEAD_SPACE;
        }

        // ------------------------------------------------
        // LOGO (ONLY LOCAL PLAYER) - Problem 1 fix: Adjusted size and y offset
        // ------------------------------------------------
        if (shouldDrawLogo()) {
            int logoY = y + 1;

            context.drawTexture(
                LEAF_LOGO,
                drawX,
                logoY,
                0, 0,
                LOGO_SIZE,
                LOGO_SIZE,
                LOGO_SIZE,
                LOGO_SIZE
            );

            drawX += BASE_LOGO_SPACE;
        }

        // ------------------------------------------------
        // TEXT
        // ------------------------------------------------
        int result = context.drawTextWithShadow(renderer, text, drawX, y, color);

        leaf$currentEntry = null;
        leaf$isLocalPlayer = false;

        return result;
    }

    // ------------------------------------------------------------
    // WIDTH: ROW (PING SAFE)
    // ------------------------------------------------------------
    @Redirect(
        method = "render",
        at = @At(value = "INVOKE", target = "Ljava/lang/Math;max(II)I"),
        require = 0
    )
    private int expandRow(int a, int b) {

        int base = Math.max(a, b);

        if (leaf$shouldDrawHead) {
            base += BASE_HEAD_SPACE;
        }

        if (shouldDrawLogo()) {
            base += BASE_LOGO_SPACE;
        }

        base += BASE_PING_SPACE;

        return base;
    }

    // ------------------------------------------------------------
    // UTIL
    // ------------------------------------------------------------
    private boolean shouldDrawLogo() {
        if (!leaf$isLocalPlayer) return false;
        if (LeafClient.modManager == null) return false;

        LeafLogoMod mod = LeafClient.modManager.getMod(LeafLogoMod.class);
        return mod != null && mod.isEnabled();
    }
}
