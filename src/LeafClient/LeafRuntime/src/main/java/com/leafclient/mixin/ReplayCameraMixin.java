package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.ReplayMod;
import com.leafclient.replay.ReplayPlayer;
import net.minecraft.client.render.Camera;
import net.minecraft.entity.Entity;
import net.minecraft.util.math.Vec3d;
import net.minecraft.world.BlockView;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(Camera.class)
public abstract class ReplayCameraMixin {
    
    @Shadow
    protected abstract void setPos(Vec3d pos);
    
    @Shadow
    protected abstract void setRotation(float yaw, float pitch);
    
    private static int tickCounter = 0;
    
    @Inject(method = "update", at = @At("RETURN"))
    private void onUpdateCamera(BlockView area, Entity focusedEntity, boolean thirdPerson, boolean inverseView, float tickDelta, CallbackInfo ci) {
        ReplayMod replayMod = LeafClient.modManager.getMod(ReplayMod.class);
        if (replayMod != null && replayMod.isPlaying()) {
            ReplayPlayer player = replayMod.getPlayer();
            if (player != null) {
                Vec3d newPos = player.getCameraPos();
                float newYaw = player.getCameraYaw();
                float newPitch = player.getCameraPitch();
                
                if (newPos != null) {
                    this.setPos(newPos);
                    this.setRotation(newYaw, newPitch);
                    
                    // Log every 20 ticks
                    tickCounter++;
                    if (tickCounter % 20 == 0) {
                        System.out.println("[CameraMixin] APPLIED: " + 
                            String.format("%.1f, %.1f, %.1f", newPos.x, newPos.y, newPos.z) +
                            " | Yaw: " + String.format("%.1f", newYaw));
                        tickCounter = 0;
                    }
                } else {
                    System.err.println("[CameraMixin] Camera position is NULL!");
                }
            }
        }
    }
}
