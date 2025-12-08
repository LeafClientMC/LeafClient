package com.leafclient.modules;

import net.minecraft.util.math.Vec3d;

public class FreelookMod implements ILeafMod {
    private boolean enabled = false;
    private boolean isFreelooking = false;
    private Vec3d originalPosition = Vec3d.ZERO;
    private float originalYaw = 0;
    private float originalPitch = 0;
    
    private Vec3d cameraPosition = Vec3d.ZERO; // Actual rendered camera position
    private Vec3d targetCameraPosition = Vec3d.ZERO; // Where the user *wants* the camera to be
    private float cameraYaw = 0;
    private float cameraPitch = 0;
    
    private float moveSpeed = 0.5f; // Increased for faster movement
    private static final float CAMERA_MOVE_SMOOTHNESS = 0.3f; // Increased for smoother/faster interpolation

    @Override
    public boolean isEnabled() {
        return enabled;
    }

    @Override
    public void setEnabled(boolean enabled) {
        this.enabled = enabled;
        if (!enabled) {
            isFreelooking = false;
        }
        System.out.println("[LeafClient] Freelook " + (enabled ? "enabled" : "disabled"));
    }

    @Override
    public String getName() {
        return "Freelook";
    }

    public boolean isFreelooking() {
        return isFreelooking && enabled;
    }

    public void toggleFreelook(Vec3d playerPos, float yaw, float pitch) {
        isFreelooking = !isFreelooking;
        if (isFreelooking) {
            originalPosition = playerPos;
            originalYaw = yaw;
            originalPitch = pitch;
            
            // Set initial camera position slightly above player eyes
            cameraPosition = playerPos.add(0, 1.62, 0);
            targetCameraPosition = cameraPosition; // Target starts at current
            
            cameraYaw = yaw;
            cameraPitch = pitch;
        } else {
            // When stopping freelook, snap camera back to player eye level
            cameraPosition = originalPosition.add(0, 1.62, 0);
            targetCameraPosition = cameraPosition;
        }
    }

    public Vec3d getOriginalPosition() {
        return originalPosition;
    }

    public float getOriginalYaw() {
        return originalYaw;
    }

    public float getOriginalPitch() {
        return originalPitch;
    }

    public Vec3d getCameraPosition() {
        return cameraPosition;
    }

    public void setCameraPosition(Vec3d position) {
        this.cameraPosition = position;
    }

    public float getCameraYaw() {
        return cameraYaw;
    }

    public void setCameraYaw(float yaw) {
        this.cameraYaw = yaw;
    }

    public float getCameraPitch() {
        return cameraPitch;
    }

    public void setCameraPitch(float pitch) {
        this.cameraPitch = pitch;
    }

    public float getMoveSpeed() {
        return moveSpeed;
    }

    public void setMoveSpeed(float speed) {
        this.moveSpeed = speed;
    }

    public void updateCameraAngles(float deltaYaw, float deltaPitch) {
        this.cameraYaw += deltaYaw;
        this.cameraPitch += deltaPitch;
        this.cameraPitch = Math.max(-90.0f, Math.min(90.0f, this.cameraPitch));
    }

    public void updateTargetCameraPosition(float forward, float strafe, float up) {
        if (!isFreelooking) return;

        double yawRad = Math.toRadians(cameraYaw);
        double pitchRad = Math.toRadians(cameraPitch);

        double forwardX = -Math.sin(yawRad) * Math.cos(pitchRad) * forward * moveSpeed;
        double forwardY = -Math.sin(pitchRad) * forward * moveSpeed;
        double forwardZ = Math.cos(yawRad) * Math.cos(pitchRad) * forward * moveSpeed;

        double strafeX = -Math.cos(yawRad) * strafe * moveSpeed;
        double strafeZ = -Math.sin(yawRad) * strafe * moveSpeed;

        double upY = up * moveSpeed;

        targetCameraPosition = targetCameraPosition.add(forwardX + strafeX, forwardY + upY, forwardZ + strafeZ);
    }
    
    // Call this every tick to smoothly move the camera
    public void interpolateCameraPosition() {
        if (!isFreelooking) {
            // When not freelooking, slowly interpolate back to player if not already there
            if (cameraPosition.distanceTo(targetCameraPosition) > 0.01) {
                cameraPosition = cameraPosition.add((targetCameraPosition.x - cameraPosition.x) * CAMERA_MOVE_SMOOTHNESS,
                                                    (targetCameraPosition.y - cameraPosition.y) * CAMERA_MOVE_SMOOTHNESS,
                                                    (targetCameraPosition.z - cameraPosition.z) * CAMERA_MOVE_SMOOTHNESS);
            } else {
                 cameraPosition = targetCameraPosition; // Snap if very close
            }
            return;
        }

        // Interpolate camera towards target position
        cameraPosition = cameraPosition.add((targetCameraPosition.x - cameraPosition.x) * CAMERA_MOVE_SMOOTHNESS,
                                            (targetCameraPosition.y - cameraPosition.y) * CAMERA_MOVE_SMOOTHNESS,
                                            (targetCameraPosition.z - cameraPosition.z) * CAMERA_MOVE_SMOOTHNESS);
        
        // Snap if very close to target (to prevent infinite interpolation)
        if (cameraPosition.distanceTo(targetCameraPosition) < 0.01) {
            cameraPosition = targetCameraPosition;
        }
    }
}