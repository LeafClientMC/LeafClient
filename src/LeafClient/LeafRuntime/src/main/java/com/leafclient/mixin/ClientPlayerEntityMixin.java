// src/main/java/com/leafclient/mixin/ClientPlayerEntityMixin.java
package com.leafclient.mixin;

import com.leafclient.LeafClient;
import com.leafclient.modules.ToggleSprintMod;
import com.leafclient.modules.FreelookMod;
import net.minecraft.client.network.ClientPlayerEntity;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

@Mixin(ClientPlayerEntity.class)
public class ClientPlayerEntityMixin {
    
    @Inject(method = "tickMovement", at = @At("HEAD"), cancellable = true)
private void onTickMovement(CallbackInfo ci) {
    ClientPlayerEntity player = (ClientPlayerEntity) (Object) this;
    
    // FREELOOK: Freeze player completely
    FreelookMod freelook = LeafClient.modManager.getMod(FreelookMod.class);
    if (freelook != null && freelook.isEnabled() && freelook.isFreelooking()) {
        ci.cancel(); // Stop all movement
        return;
    }
    
    // TOGGLE SPRINT (only runs if not freelooking)
    ToggleSprintMod toggleSprint = LeafClient.modManager.getMod(ToggleSprintMod.class);
    if (toggleSprint != null && toggleSprint.isEnabled()) {
        if (toggleSprint.isSprintToggled() && !player.isSprinting()) {
            if (player.forwardSpeed > 0 && !player.isSneaking() && !player.isSubmergedInWater()) {
                player.setSprinting(true);
            }
        }
        
        if (toggleSprint.isSneakToggled()) {
            player.input.sneaking = true;
        }
    }
}

}
