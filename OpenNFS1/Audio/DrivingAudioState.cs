using System;

namespace OpenNFS1.Audio
{
    // Pure mix policy: physics remains authoritative; no sound from airborne tires.
    internal readonly record struct DrivingAudioState(float Skid, float OffRoad)
    {
        public static DrivingAudioState Compute(float speed, bool grounded, int offRoadWheels,
            float steering, float rearSlip, float brake, bool abs, bool handbrake,
            bool wheelspin, float collisionRotation)
        {
            if (!grounded) return default;
            float v = Math.Abs(speed);
            float road = 1f - Math.Clamp(offRoadWheels / 4f, 0f, 1f);
            float moving = Math.Clamp((v - 2f) / 14f, 0f, 1f);
            float corner = Math.Clamp((Math.Abs(steering) - .12f) / .65f, 0f, 1f) *
                Math.Clamp((v - 17f) / 50f, 0f, 1f);
            float drift = Math.Clamp(Math.Abs(rearSlip) * 3.5f, 0f, 1f) * moving;
            float braking = (handbrake || (!abs && brake > .65f)) ? .85f * moving : 0f;
            float spin = wheelspin ? .8f : 0f;
            float impact = Math.Abs(collisionRotation) > .03f ? moving : 0f;
            float skid = Math.Max(corner, Math.Max(drift, Math.Max(braking, Math.Max(spin, impact))));
            float offRoad = (1f - road) * Math.Clamp(v / 35f, 0f, 1f);
            return new DrivingAudioState(skid * road, offRoad);
        }
    }
}
