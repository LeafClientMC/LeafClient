package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.ReplayMod;
import com.leafclient.replay.ReplayPlayer;
import net.minecraft.client.render.GameRenderer;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(GameRenderer.class)
public class ReplayFovMixin {

    @Inject(method = "getFov", at = @At("RETURN"), cancellable = true)
    private void onGetFov(CallbackInfoReturnable<Double> cir) {
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod != null && replayMod.isPlaying()) {
            ReplayPlayer player = replayMod.getPlayer();
            if (player != null) {
                cir.setReturnValue((double) player.getCameraFov());
            }
        }
    }
}
