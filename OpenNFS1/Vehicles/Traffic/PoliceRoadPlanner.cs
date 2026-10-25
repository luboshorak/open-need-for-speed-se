using System;

namespace OpenNFS1.Vehicles
{
    /// <summary>
    /// Deterministic highway patrol behavior, separate from MonoGame movement.
    /// The patrol may pass the suspect, and a blocker in the
    /// current lane prompts a safe adjacent-lane choice instead of ramming it.
    /// </summary>
    internal static class PoliceRoadPlanner
    {
        public static float YawStep(float error, float speed, float dt) => speed < .1f ? 0 :
            Math.Clamp(error * 2, -1.4f, 1.4f) * Math.Clamp(dt, 0, .1f);
        internal readonly struct Decision
        {
            public readonly float TargetSpeed;
            public readonly int TargetLane;
            public Decision(float speed, int lane) { TargetSpeed = speed; TargetLane = lane; }
        }

        public static Decision Plan(float suspectSpeed, int suspectNodeGap,
            int lane, bool blockerAhead, float blockerSpeed,
            bool leftLaneClear, bool rightLaneClear)
        {
            float playerSpeed = Math.Abs(suspectSpeed);
            float targetSpeed;
            // Passing is necessary before the original caught flag can be set.
            targetSpeed = suspectNodeGap < -3 ? playerSpeed : playerSpeed + 20;
            if (playerSpeed < 3 && suspectNodeGap <= 0) targetSpeed = 0;
            targetSpeed = Math.Clamp(targetSpeed, 0, 330);
            int preferredLane = Math.Clamp(lane, 0, 4);
            if (blockerAhead)
            {
                // Prefer another forward-traffic lane, never jump straight
                // into oncoming lane 0 or 1 on a two-way road.
                if (rightLaneClear && preferredLane < 4)
                    preferredLane++;
                else if (leftLaneClear && preferredLane > 2)
                    preferredLane--;
                else
                    targetSpeed = Math.Min(targetSpeed, Math.Max(0, blockerSpeed - 7));
            }
            return new Decision(targetSpeed, preferredLane);
        }
    }
}
