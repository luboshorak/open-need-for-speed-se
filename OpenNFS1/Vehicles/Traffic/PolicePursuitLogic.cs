using System;

namespace OpenNFS1.Vehicles
{
    /// <summary>
    /// Head-to-head, open-road speed enforcement. Time-based so behavior
    /// remains stable when the MonoGame frame rate changes.
    /// </summary>
    internal sealed class PolicePursuitLogic
    {
        private float _speedingSeconds, _stoppedSeconds, _cooldownSeconds;
        public bool IsPursuing { get; private set; }
        public bool TicketIssued { get; private set; }

        public bool Update(float deltaSeconds, float playerSpeedKph,
            float distanceToPoliceNodes)
        {
            deltaSeconds = Math.Clamp(deltaSeconds, 0, 0.1f);
            if (_cooldownSeconds > 0)
            {
                _cooldownSeconds = Math.Max(0, _cooldownSeconds - deltaSeconds);
                return false;
            }

            if (!IsPursuing)
            {
                _speedingSeconds = playerSpeedKph > 110 ?
                    _speedingSeconds + deltaSeconds :
                    Math.Max(0, _speedingSeconds - deltaSeconds * 2);
                if (_speedingSeconds >= 3.5f)
                {
                    IsPursuing = true;
                    _speedingSeconds = 0;
                    return true; // caller spawns copmust.CFM
                }
                return false;
            }

            bool nearby = distanceToPoliceNodes <= 4;
            bool stopped = Math.Abs(playerSpeedKph) < 12;
            _stoppedSeconds = nearby && stopped ?
                _stoppedSeconds + deltaSeconds : 0;
            if (_stoppedSeconds >= 2.5f)
            {
                TicketIssued = true;
                IsPursuing = false;
                _stoppedSeconds = 0;
                _cooldownSeconds = 25;
            }
            return false;
        }

        public void ResetPursuit()
        {
            IsPursuing = false;
            _stoppedSeconds = 0;
            _speedingSeconds = 0;
            _cooldownSeconds = 12;
        }

        public void ClearTicket() => TicketIssued = false;
    }
}
