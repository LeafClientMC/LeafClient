package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.ZoomMod;
import net.minecraft.client.render.GameRenderer;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;

@Mixin(GameRenderer.class)
public class GameRendererMixin {
    
    @Inject(method = "getFov", at = @At("RETURN"), cancellable = true)
    private void onGetFov(CallbackInfoReturnable<Double> cir) {
        ZoomMod zoomMod = LeafClient.modManager.getMod(ZoomMod.class);
        if (zoomMod != null && zoomMod.isEnabled()) {
            double currentFov = cir.getReturnValue();
            zoomMod.updateZoom(currentFov);
            
            if (zoomMod.isZooming() || zoomMod.getCurrentZoom() != currentFov) {
                cir.setReturnValue(zoomMod.getCurrentZoom());
            }
        }
    }
}
