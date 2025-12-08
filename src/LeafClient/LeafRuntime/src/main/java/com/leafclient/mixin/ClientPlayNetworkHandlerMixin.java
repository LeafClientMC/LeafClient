// NEW FILE: com/leafclient/mixin/ClientPlayNetworkHandlerMixin.java

package com.leafclient.mixin;

import com.leafclient.replay.PacketCaptureManager;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.network.ClientPlayNetworkHandler;
import net.minecraft.network.ClientConnection;
import net.minecraft.network.packet.Packet;
import net.minecraft.text.Text;
import org.spongepowered.asm.mixin.Final;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.Shadow;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/**
 * Intercepts ALL clientbound packets for:
 *  - Recording: forward to PacketCaptureManager.recordPacket(packet)
 *  - Replay playback: (future step) optionally cancel real network traffic
 */
@Mixin(ClientPlayNetworkHandler.class)
public class ClientPlayNetworkHandlerMixin {

    @Shadow @Final
    private MinecraftClient client;

    @Shadow @Final
    private ClientConnection connection;

    @Inject(method = "onPacket", at = @At("HEAD"))
    private void leaf$onPacketHead(Packet<?> packet, CallbackInfo ci) {
        if (!PacketCaptureManager.isRecording()) {
            return;
        }

        try {
            PacketCaptureManager.recordPacket(packet);
        } catch (Throwable t) {
            System.err.println("[Leaf Replay] Failed to record packet: " + packet.getClass().getName());
            t.printStackTrace();
        }
    }
}
