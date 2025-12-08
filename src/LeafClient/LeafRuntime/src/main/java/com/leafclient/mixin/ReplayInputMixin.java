package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.ReplayMod;
import com.leafclient.replay.ReplayPlayer;
import net.minecraft.client.Mouse;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(Mouse.class)
public class ReplayInputMixin {

    @Inject(method = "updateMouse", at = @At("HEAD"), cancellable = true)
    private void onMouseUpdate(CallbackInfo ci) {
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod != null && replayMod.isPlaying()) {
            ReplayPlayer player = replayMod.getPlayer();
            if (player != null && player.isFreecamMode()) {
                // Allow mouse movement for camera control during replay
                // The actual rotation update happens in the tick method
            }
        }
    }
}
