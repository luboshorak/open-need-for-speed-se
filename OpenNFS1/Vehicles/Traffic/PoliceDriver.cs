using System;
using System.Collections.Generic;
using OpenNFS1.Physics;
using OpenNFS1.Vehicles.AI;

namespace OpenNFS1.Vehicles
{
    /// <summary>
    /// Dedicated patrol car driver, using the original COPMUST.CFM model.
    /// Unlike ordinary TrafficDriver it accelerates to pursue the player's
    /// location along the course instead of maintaining constant 50 km/h.
    /// </summary>
    internal sealed class PoliceDriver : TrafficDriver
    {
        private readonly IDriver _suspect;

        public PoliceDriver(IDriver suspect)
            : base(@"SIMDATA\CARFAMS\copmust.CFM", TrafficDriverDirection.Forward)
        {
            _suspect = suspect;
            VirtualLane = 2;
        }

        public override void Update(List<IDriver> otherDrivers)
        {
            var road = Vehicle.CurrentNode;
            if (road?.Next?.Next == null)
            {
                Vehicle.Speed = 0;
                AtEndOfTrack = true;
                return;
            }

            int separation = _suspect.Vehicle.CurrentNode.Number - road.Number;
            float targetSpeed = Math.Max(95,
                Math.Abs(_suspect.Vehicle.Speed) + (separation > 5 ? 24 : 7));
            Vehicle.Speed = Math.Clamp(targetSpeed, 75, 220);

            // Keep aiming down the road; near the suspect, adjust toward the
            // same lane. Existing vehicle collision resolver handles contacts.
            if (Math.Abs(separation) <= 10)
                VirtualLane = 2;
            FollowTrack();
            Vehicle.Update();
        }
    }
}
