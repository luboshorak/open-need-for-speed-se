using System;

namespace OpenNFS1
{
    /// <summary>
    /// Direction-sensitive arcade controls. Gear is -1 for R, 0 for N and
    /// 1..N for forward. Down brakes forward motion but acts as a reversing
    /// throttle once reverse has engaged; Up brakes backward motion.
    /// </summary>
    internal static class ReverseDriveControl
    {
        internal readonly struct Command
        {
            public readonly float Throttle, Brake;
            public readonly bool ShiftUp, ShiftDown;

            public Command(float throttle, float brake, bool up, bool down)
            {
                Throttle = throttle;
                Brake = brake;
                ShiftUp = up;
                ShiftDown = down;
            }
        }

        public static Command Compute(bool manual, int gear, float speed,
            float forwardPedal, float backwardPedal, bool manualUp, bool manualDown)
        {
            float forward = Math.Clamp(forwardPedal, 0, 1);
            float backward = Math.Clamp(backwardPedal, 0, 1);
            bool rollingForward = speed > 2f;
            bool rollingBackward = speed < -2f;

            if (manual)
            {
                // In manual mode Z/A select the gear, and arrow Down
                // accelerates ONLY when R is already engaged.
                if (gear < 0)
                    return new Command(rollingForward ? 0 : backward,
                        rollingForward ? backward : forward, manualUp, manualDown);
                return new Command(rollingBackward ? 0 : forward,
                    rollingBackward ? forward : backward, manualUp, manualDown);
            }

            if (backward > 0 && forward <= 0)
            {
                if (rollingForward)
                    return new Command(0, backward, false, false);

                if (gear >= 0)
                    return new Command(0, 0, false, true); // 1st -> N -> R

                return new Command(backward, 0, false, false);
            }

            if (forward > 0 && backward <= 0)
            {
                if (rollingBackward)
                    return new Command(0, forward, false, false);

                if (gear <= 0)
                    return new Command(0, 0, true, false); // R -> N -> 1st

                return new Command(forward, 0, false, false);
            }

            // Both pedals => prioritize braking, never drive through collision.
            return new Command(0, Math.Max(forward, backward), false, false);
        }
    }
}
