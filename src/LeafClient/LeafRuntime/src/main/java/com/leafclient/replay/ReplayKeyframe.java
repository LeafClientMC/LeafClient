package com.leafclient.replay;

import net.minecraft.util.math.Vec3d;

import java.util.ArrayList;
import java.util.List;

public class ReplayKeyframe {
    public int tick;
    public Vec3d position;
    public float yaw;
    public float pitch;
    public float fov;

    public ReplayKeyframe(int tick, Vec3d position, float yaw, float pitch, float fov) {
        this.tick = tick;
        this.position = position;
        this.yaw = yaw;
        this.pitch = pitch;
        this.fov = fov;
    }

    public static class KeyframeManager {
        private final List<ReplayKeyframe> keyframes = new ArrayList<>();
        private int currentKeyframeIndex = 0;

        public void addKeyframe(ReplayKeyframe keyframe) {
            keyframes.add(keyframe);
            keyframes.sort((a, b) -> Integer.compare(a.tick, b.tick));
        }

        public void removeKeyframe(int index) {
            if (index >= 0 && index < keyframes.size()) {
                keyframes.remove(index);
            }
        }

        public void clearKeyframes() {
            keyframes.clear();
            currentKeyframeIndex = 0;
        }

        public List<ReplayKeyframe> getKeyframes() {
            return keyframes;
        }

        public ReplayKeyframe interpolate(int currentTick) {
            if (keyframes.isEmpty()) return null;
            if (keyframes.size() == 1) return keyframes.get(0);

            // Find surrounding keyframes
            ReplayKeyframe before = null;
            ReplayKeyframe after = null;

            for (int i = 0; i < keyframes.size(); i++) {
                ReplayKeyframe kf = keyframes.get(i);
                if (kf.tick <= currentTick) {
                    before = kf;
                } else {
                    after = kf;
                    break;
                }
            }

            // If before start or after end, return boundary keyframe
            if (before == null) return after;
            if (after == null) return before;

            // Linear interpolation
            float t = (float)(currentTick - before.tick) / (after.tick - before.tick);
            t = Math.max(0, Math.min(1, t));

            Vec3d pos = before.position.lerp(after.position, t);
            float yaw = lerpAngle(before.yaw, after.yaw, t);
            float pitch = lerpAngle(before.pitch, after.pitch, t);
            float fov = before.fov + (after.fov - before.fov) * t;

            return new ReplayKeyframe(currentTick, pos, yaw, pitch, fov);
        }

        public ReplayKeyframe smoothInterpolate(int currentTick) {
            if (keyframes.isEmpty()) return null;
            if (keyframes.size() == 1) return keyframes.get(0);

            // Find surrounding keyframes
            ReplayKeyframe before = null;
            ReplayKeyframe after = null;

            for (int i = 0; i < keyframes.size(); i++) {
                ReplayKeyframe kf = keyframes.get(i);
                if (kf.tick <= currentTick) {
                    before = kf;
                } else {
                    after = kf;
                    break;
                }
            }

            if (before == null) return after;
            if (after == null) return before;

            // Cubic interpolation (ease in/out)
            float t = (float)(currentTick - before.tick) / (after.tick - before.tick);
            t = Math.max(0, Math.min(1, t));
            
            // Smoothstep function: 3t² - 2t³
            float smoothT = t * t * (3 - 2 * t);

            Vec3d pos = before.position.lerp(after.position, smoothT);
            float yaw = lerpAngle(before.yaw, after.yaw, smoothT);
            float pitch = lerpAngle(before.pitch, after.pitch, smoothT);
            float fov = before.fov + (after.fov - before.fov) * smoothT;

            return new ReplayKeyframe(currentTick, pos, yaw, pitch, fov);
        }

        private float lerpAngle(float start, float end, float t) {
            // Handle angle wrapping
            float diff = end - start;
            while (diff > 180) diff -= 360;
            while (diff < -180) diff += 360;
            return start + diff * t;
        }

        public void saveToFile(String filename) {
            // TODO: Implement file save
        }

        public void loadFromFile(String filename) {
            // TODO: Implement file load
        }
    }
}
