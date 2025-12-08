package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.CrosshairMod;
import com.leafclient.replay.PacketCaptureManager;
import com.leafclient.replay.ReplayHudOverlay;
import com.leafclient.ui.HudRenderer;
import com.leafclient.texture.ServerIconManager;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.hud.InGameHud;
import net.minecraft.client.render.item.ItemRenderer;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(InGameHud.class)
public class InGameHudMixin {

    private HudRenderer hudRenderer;

    @Inject(method = "<init>", at = @At("RETURN"))
    private void onInit(MinecraftClient client, ItemRenderer itemRenderer, CallbackInfo ci) {
        hudRenderer = new HudRenderer(client);
    }

    @Inject(method = "render", at = @At("RETURN"))
    private void onRender(DrawContext context, float tickDelta, CallbackInfo ci) {
        hudRenderer.render(context);

        MinecraftClient client = MinecraftClient.getInstance();
        ReplayHudOverlay.render(
            context,
            client.getWindow().getScaledWidth(),
            client.getWindow().getScaledHeight()
        );

        if (PacketCaptureManager.isRecording()) {
            String info = "Packets: " + PacketCaptureManager.getPacketCount();
            context.drawTextWithShadow(
                client.textRenderer,
                info,
                10,
                10,
                0xFFFFFF
            );
        }
    }

    @Inject(method = "renderCrosshair", at = @At("HEAD"), cancellable = true)
    private void onRenderCrosshair(DrawContext context, CallbackInfo ci) {
        CrosshairMod mod = LeafClient.modManager.getMod(CrosshairMod.class);
        if (mod != null && mod.isEnabled()) {
            ci.cancel();
        }
    }

    @Inject(method = "clear", at = @At("HEAD"))
    private void onClear(CallbackInfo ci) {
        ServerIconManager.clearCache();
    }
}
