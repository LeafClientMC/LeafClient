package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.FreelookMod;
import net.minecraft.client.input.Input;
import net.minecraft.client.input.KeyboardInput;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(KeyboardInput.class)
public class KeyboardInputMixin extends Input {

    @Inject(method = "tick", at = @At("HEAD"), cancellable = true)
    private void onTick(boolean slowDown, float f, CallbackInfo ci) {
        FreelookMod freelook = LeafClient.modManager.getMod(FreelookMod.class);

        if (freelook != null && freelook.isEnabled() && freelook.isFreelooking()) {
            this.movementForward = 0;
            this.movementSideways = 0;
            this.jumping = false;
            this.sneaking = false;
        }
    }
}
