package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.LeafClientMod;
import net.minecraft.client.render.LightmapTextureManager;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.ModifyArgs;
import org.spongepowered.asm.mixin.injection.invoke.arg.Args;

@Mixin(LightmapTextureManager.class)
public class LightmapTextureManagerMixin {

    @ModifyArgs(
            method = "update",
            at = @At(
                    value = "INVOKE",
                    target = "Lnet/minecraft/client/texture/NativeImage;setColor(III)V"
            )
    )
    private void onUpdate(Args args) {
        if (LeafClient.modManager.getMod("FullBright").isEnabled()) {
            int originalColor = args.get(2);
            int alpha = (originalColor >> 24) & 0xFF;
            int newColor = (alpha << 24) | 0xFFFFFF; 
            args.set(2, newColor);
        }
    }
}