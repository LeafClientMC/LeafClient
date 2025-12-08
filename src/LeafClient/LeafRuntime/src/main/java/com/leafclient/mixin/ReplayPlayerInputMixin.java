package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.ReplayMod;
import com.leafclient.replay.ReplayPlayer;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.Mouse;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(Mouse.class)
public class ReplayPlayerInputMixin {

    @Shadow @Final
    private MinecraftClient client;

    @Shadow
    private double cursorDeltaX;

    @Shadow
    private double cursorDeltaY;

    @Inject(method = "updateMouse", at = @At("TAIL"))
    private void onMouseMove(CallbackInfo ci) {
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod != null && replayMod.isPlaying()) {
            ReplayPlayer player = replayMod.getPlayer();
            if (player != null && player.isFreecamMode()) {
                float sensitivity = 0.15f;
                float deltaYaw = (float) cursorDeltaX * sensitivity;
                float deltaPitch = (float) cursorDeltaY * sensitivity;
                
                player.updateCameraRotation(deltaYaw, -deltaPitch);
            }
        }
    }
}
