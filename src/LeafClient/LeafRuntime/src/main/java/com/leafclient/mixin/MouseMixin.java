package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.CPSMod;
import com.leafclient.modules.FreelookMod;
import com.leafclient.modules.ZoomMod;
import net.minecraft.client.Mouse;
import net.minecraft.client.network.ClientPlayerEntity;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.Redirect;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(Mouse.class)
public class MouseMixin {

    // CPS Mod Logic
    @Inject(method = "onMouseButton", at = @At("HEAD"))
    private void onMouseButton(long window, int button, int action, int mods, CallbackInfo ci) {
        if (action == 1) { 
            CPSMod cpsMod = LeafClient.modManager.getMod(CPSMod.class);
            if (cpsMod != null && cpsMod.isEnabled()) {
                if (button == 0) cpsMod.onLeftClick();
                else if (button == 1) cpsMod.onRightClick();
            }
        }
    }

    // Zoom Mod Logic
    @Inject(method = "onMouseScroll", at = @At("HEAD"), cancellable = true)
    private void onMouseScroll(long window, double horizontal, double vertical, CallbackInfo ci) {
        ZoomMod zoomMod = LeafClient.modManager.getMod(ZoomMod.class);
        if (zoomMod != null && zoomMod.isEnabled() && zoomMod.isZooming()) {
            zoomMod.adjustZoomLevel(vertical);
            ci.cancel();
        }
    }

    // FREELOOK
    @Redirect(method = "updateMouse", at = @At(value = "INVOKE", target = "Lnet/minecraft/client/network/ClientPlayerEntity;changeLookDirection(DD)V"))
    private void redirectLookDirection(ClientPlayerEntity player, double cursorDeltaX, double cursorDeltaY) {
        FreelookMod freelook = LeafClient.modManager.getMod(FreelookMod.class);
        
        if (freelook != null && freelook.isEnabled() && freelook.isFreelooking()) {
            // Update ONLY the Freelook camera angles
            freelook.updateCameraAngles((float) cursorDeltaX, (float) cursorDeltaY);
        } else {
            // Update Player rotation normally
            player.changeLookDirection(cursorDeltaX, cursorDeltaY);
        }
    }
}
