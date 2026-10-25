using System;
namespace OpenNFS1.Vehicles
{
    // Original decision cadence: every fourth 30-ms simulation step.
    internal sealed class PolicePursuitLogic
    {
        public const float TickSeconds = .12f;
        public const float MpsToGameSpeed = 2.2369363f; // legacy HUD/physics speed is mph
        public const float PenaltySeconds = 60 * .03f;
        private double _pending;
        public int ChaseTicks { get; private set; }
        public bool IsPursuing { get; private set; }
        public bool Caught { get; private set; }
        public bool HasJoinedPursuit { get; private set; }
        public bool TicketIssued { get; private set; }
        public bool WarningIssued { get; private set; }
        public int TicketCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool IsArrested => TicketCount >= 2;
        public bool RadarNear { get; private set; }
        public bool Update(float dt, float playerSpeed, float policeSpeed, int policeMinusPlayer,
            float limit, bool present, bool playerStable = true, bool policeFacingForward = true)
        {
            bool started = false;
            _pending += Math.Clamp(dt, 0, .1f);
            while (_pending + .0000001 >= TickSeconds)
            {
                _pending -= TickSeconds;
                RadarNear = present && Math.Abs(policeMinusPlayer) < 10;
                if (!present || IsArrested) { Drop(); continue; }
                if (!IsPursuing)
                {
                    if (Math.Abs(policeMinusPlayer) < 5 && playerSpeed > limit)
                    { IsPursuing = true; HasJoinedPursuit = policeMinusPlayer < 0 && policeFacingForward; ChaseTicks = 0; started = true; }
                    continue;
                }
                ChaseTicks += 4;
                if (policeMinusPlayer < 0 && policeFacingForward) HasJoinedPursuit = true;
                if (HasJoinedPursuit && policeFacingForward && (policeMinusPlayer > 2 ||
                    (policeMinusPlayer >= 0 && playerSpeed < 3 * MpsToGameSpeed &&
                     policeSpeed < 3 * MpsToGameSpeed))) Caught = true;
                if (!playerStable || !Caught || playerSpeed >= MpsToGameSpeed || policeSpeed >= MpsToGameSpeed) continue;
                if (ChaseTicks < 400) { WarningCount++; WarningIssued = true; }
                else { TicketCount++; TicketIssued = true; }
                Drop();
            }
            return started;
        }
        public void Drop() { IsPursuing = Caught = HasJoinedPursuit = false; ChaseTicks = 0; }
        public void ClearEvents() { WarningIssued = TicketIssued = false; }
        public static bool SpawnAllowed(byte flags, int roll) => (flags & 1) != 0 &&
            roll <= new[] { 32, 64, 96, 128 }[(flags & 0x18) >> 3];
    }
}
