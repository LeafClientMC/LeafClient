package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.FreelookMod;
import net.minecraft.client.render.Camera;
import net.minecraft.entity.Entity;
import net.minecraft.util.math.Vec3d;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(Camera.class)
public abstract class CameraMixin {
        
    @Shadow
    private Vec3d pos;
    
    @Shadow
    protected abstract void setPos(Vec3d pos);
    
    @Shadow
    protected abstract void setRotation(float yaw, float pitch);
    
    @Inject(method = "update", at = @At("TAIL"))
    private void onUpdateCamera(CallbackInfo ci) {
        FreelookMod freelookMod = LeafClient.modManager.getMod(FreelookMod.class);
        if (freelookMod != null && freelookMod.isEnabled() && freelookMod.isFreelooking()) {
            // Override camera position and rotation
            this.setPos(freelookMod.getCameraPosition());
            this.setRotation(freelookMod.getCameraYaw(), freelookMod.getCameraPitch());
        }
    }
}
