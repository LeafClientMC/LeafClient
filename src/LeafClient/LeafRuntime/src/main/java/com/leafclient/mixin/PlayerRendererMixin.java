package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.ReplayMod;
import com.leafclient.replay.ReplayPlayer;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.network.AbstractClientPlayerEntity;
import net.minecraft.client.render.entity.PlayerEntityRenderer;
import net.minecraft.client.render.entity.model.PlayerEntityModel;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(PlayerEntityRenderer.class)
public abstract class PlayerRendererMixin {

    @Inject(method = "setModelPose", at = @At("HEAD"), cancellable = true)
    private void onSetModelPose(AbstractClientPlayerEntity abstractClientPlayerEntity, CallbackInfo ci) {
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod != null && replayMod.isPlaying()) {
            // Prevent normal model pose setting during replay
            // This is to stop the actual player model from animating on its own
            ci.cancel();
        }
    }

    @Inject(method = "render(Lnet/minecraft/client/network/AbstractClientPlayerEntity;FFLnet/minecraft/client/util/math/MatrixStack;Lnet/minecraft/client/render/VertexConsumerProvider;I)V", at = @At("HEAD"), cancellable = true)
    private void onRender(AbstractClientPlayerEntity abstractClientPlayerEntity, float f, float g, net.minecraft.client.util.math.MatrixStack matrixStack, net.minecraft.client.render.VertexConsumerProvider vertexConsumerProvider, int i, CallbackInfo ci) {
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod != null && replayMod.isPlaying()) {
            ReplayPlayer player = replayMod.getPlayer();
            MinecraftClient client = MinecraftClient.getInstance();

            if (player != null && client.player != null && abstractClientPlayerEntity == client.player) {
                // Not Implemented
            }
        }
    }
}
