package com.leafclient.mixin;

import com.leafclient.replay.PacketCaptureManager;
import io.netty.channel.ChannelHandlerContext;
import net.minecraft.network.ClientConnection;
import net.minecraft.network.packet.Packet;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/**
 * Intercepts every incoming Packet<?> at the ClientConnection Netty handler
 * and forwards it to PacketCaptureManager when recording is active.
 *
 * This captures all incoming vanilla packets in one place.
 */
@Mixin(ClientConnection.class)
public class ClientConnectionMixin {

    @Inject(method = "channelRead0", at = @At("HEAD"))
    private void onChannelRead0(ChannelHandlerContext ctx, Packet<?> packet, CallbackInfo ci) {
        try {
            if (PacketCaptureManager.isRecording()) {
                PacketCaptureManager.recordPacket(packet);
                if (PacketCaptureManager.getPacketCount() % 200 == 0) {
                    System.out.println("[REPLAY] Packets recorded so far: " + PacketCaptureManager.getPacketCount());
                }
            }
        } catch (Throwable t) {
            // Don't let packet capture break the network processing
            t.printStackTrace();
        }
    }
}
