using System;

namespace OpenNFS1.Vehicles
{
    /// <summary>
    /// NFS SE-inspired patrol states: prompt compliance after a siren gives
    /// a warning, being caught later earns a citation, and three citations
    /// in one road stage trigger arrest. Distances are track node gaps.
    /// </summary>
    internal sealed class PolicePursuitLogic
    {
        private float _speedingSeconds, _stoppedSeconds, _cooldownSeconds, _chaseSeconds;
        public bool IsPursuing { get; private set; }
        public bool TicketIssued { get; private set; }
        public bool WarningIssued { get; private set; }
        public int TicketCount { get; private set; }
        public bool IsArrested => TicketCount >= 3;

        public bool Update(float deltaSeconds, float playerSpeedKph,
            float distanceToPoliceNodes)
        {
            deltaSeconds = Math.Clamp(deltaSeconds, 0, 0.1f);
            if (_cooldownSeconds > 0)
            {
                _cooldownSeconds = Math.Max(0, _cooldownSeconds - deltaSeconds);
                return false;
            }
            if (IsArrested)
                return false;

            if (!IsPursuing)
            {
                _speedingSeconds = playerSpeedKph > 110
                    ? _speedingSeconds + deltaSeconds
                    : Math.Max(0, _speedingSeconds - deltaSeconds * 2);
                if (_speedingSeconds >= 3.5f)
                {
                    IsPursuing = true;
                    _speedingSeconds = 0;
                    _chaseSeconds = 0;
                    return true;
                }
                return false;
            }

            _chaseSeconds += deltaSeconds;
            bool stopped = Math.Abs(playerSpeedKph) < 12;

            // In the original game pulling over immediately after the siren
            // leads to a warning; do not give an arbitrary speeding ticket.
            if (stopped && _chaseSeconds < 5f &&
                distanceToPoliceNodes <= 20)
            {
                WarningIssued = true;
                EndPursuit(12);
                return false;
            }

            _stoppedSeconds = distanceToPoliceNodes <= 4 && stopped
                ? _stoppedSeconds + deltaSeconds : 0;
            if (_stoppedSeconds >= 2.5f)
            {
                TicketCount++;
                TicketIssued = true;
                EndPursuit(25);
            }
            return false;
        }

        private void EndPursuit(float cooldown)
        {
            IsPursuing = false;
            _stoppedSeconds = 0;
            _speedingSeconds = 0;
            _chaseSeconds = 0;
            _cooldownSeconds = cooldown;
        }

        public void ResetPursuit() => EndPursuit(12);
        public void ClearTicket() => TicketIssued = false;
        public void ClearWarning() => WarningIssued = false;
    }
}
