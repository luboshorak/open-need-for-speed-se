using System;

namespace OpenNFS1.Vehicles
{
    /// <summary>
    /// Deterministic highway patrol behavior, separate from MonoGame movement.
    /// Target closing speed falls away near the suspect, and a blocker in the
    /// current lane prompts a safe adjacent-lane choice instead of ramming it.
    /// </summary>
    internal static class PoliceRoadPlanner
    {
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
            if (suspectNodeGap <= 1)
                targetSpeed = Math.Min(30, playerSpeed + 3); // hold, don't ram
            else if (suspectNodeGap <= 3)
                targetSpeed = Math.Min(60, playerSpeed + 9);
            else if (suspectNodeGap <= 7)
                targetSpeed = Math.Max(60, playerSpeed + 12);
            else
                targetSpeed = Math.Max(95, playerSpeed + 24);

            targetSpeed = Math.Clamp(targetSpeed, 0, 175);
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
