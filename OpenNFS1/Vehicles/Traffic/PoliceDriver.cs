using System;
using System.Collections.Generic;
using GameEngine;
using Microsoft.Xna.Framework;
using OpenNFS1.Vehicles.AI;

namespace OpenNFS1.Vehicles
{
    /// <summary>
    /// Road-following patrol Mustang from the original NFS SE COPMUST.CFM.
    /// Uses stable look-ahead steering and traffic gap planning instead of
    /// inheriting civilian car's immediate 90-degree direction snapping.
    /// </summary>
    internal sealed class PoliceDriver : TrafficDriver
    {
        private readonly IDriver _suspect;
        private float _laneChangeCooldown;
        private int _preferredLane = 3;

        public PoliceDriver(IDriver suspect)
            : base(@"SIMDATA\CARFAMS\copmust.CFM", TrafficDriverDirection.Forward)
        {
            _suspect = suspect;
            VirtualLane = 3; // Same carriageway as forward-moving traffic.
            Vehicle.SteeringSpeed = 5.5f;
        }

        public override void Update(List<IDriver> others)
        {
            var node = Vehicle.CurrentNode;
            if (node?.Next?.Next == null || node.Prev == null)
            {
                Vehicle.Speed = 0;
                AtEndOfTrack = true;
                return;
            }

            int gap = (_suspect.Vehicle.CurrentNode?.Number ?? node.Number) - node.Number;
            if (gap < -8)
            {
                // We've passed the suspect; slow down, never circle violently
                // trying to aim backwards on a one-way path-following AI.
                Vehicle.Speed = Math.Max(0, Vehicle.Speed - 65 * Engine.Instance.FrameTime);
                FollowRoadAhead();
                Vehicle.Update();
                return;
            }

            bool blocked = false;
            float blockedSpeed = 0;
            bool leftClear = true, rightClear = true;
            foreach (var driver in others)
            {
                if (driver == this || driver == _suspect || driver.Vehicle?.CurrentNode == null)
                    continue;

                int separation = driver.Vehicle.CurrentNode.Number - node.Number;
                if (separation <= 0 || separation >= 12)
                    continue;

                int otherLane = driver is AIDriver ai ? ai.VirtualLane :
                    EstimateLane(driver.Vehicle.Position, node);
                if (otherLane == _preferredLane && separation < 9)
                {
                    blocked = true;
                    blockedSpeed = driver.Vehicle.Speed;
                }
                if (otherLane == _preferredLane - 1) leftClear = false;
                if (otherLane == _preferredLane + 1) rightClear = false;
            }

            _laneChangeCooldown = Math.Max(0, _laneChangeCooldown - Engine.Instance.FrameTime);
            var decision = PoliceRoadPlanner.Plan(_suspect.Vehicle.Speed, gap,
                _preferredLane, blocked, blockedSpeed, leftClear, rightClear);
            if (_laneChangeCooldown <= 0 && decision.TargetLane != _preferredLane)
            {
                _preferredLane = decision.TargetLane;
                _laneChangeCooldown = 2.4f;
            }
            VirtualLane = _preferredLane;

            // Speed cannot teleport from 0 to 200 each frame after contact.
            float blend = MathHelper.Clamp(Engine.Instance.FrameTime * 1.6f, 0, 1);
            Vehicle.Speed = MathHelper.Lerp(Math.Max(0, Vehicle.Speed),
                decision.TargetSpeed, blend);
            if (Vehicle.Speed < 0.3f) Vehicle.Speed = 0;
            FollowRoadAhead();
            Vehicle.Update();
        }

        private static int EstimateLane(Vector3 position, Tracks.TrackNode node)
        {
            Vector3 left = node.GetLeftVerge2();
            Vector3 right = node.GetRightVerge2();
            Vector3 cross = right - left;
            float lengthSq = cross.X * cross.X + cross.Z * cross.Z;
            if (lengthSq < 0.0001f) return 3;
            float t = ((position.X - left.X) * cross.X +
                       (position.Z - left.Z) * cross.Z) / lengthSq;
            return Math.Clamp((int)Math.Round(t * 4), 0, 4);
        }

        private void FollowRoadAhead()
        {
            var ahead = Vehicle.CurrentNode;
            for (int i = 0; i < 4 && ahead.Next != null; i++)
                ahead = ahead.Next;
            Vector3 target = Vector3.Lerp(ahead.GetLeftVerge2(),
                ahead.GetRightVerge2(), VirtualLane / 4f);
            Vector3 heading = Vehicle.Direction;
            heading.Y = 0;
            Vector3 targetDirection = target - Vehicle.Position;
            targetDirection.Y = 0;
            if (targetDirection.LengthSquared() < 0.1f ||
                heading.LengthSquared() < 0.1f)
            {
                Vehicle.SteeringInput = 0;
                return;
            }
            float angle = Utility.GetSignedAngleBetweenVectors(
                heading, targetDirection, true);
            // Continuous proportional steering, never a binary left/right
            // snap and never an instant 180-degree heading reset.
            Vehicle.SteeringInput = MathHelper.Clamp(-angle / 0.65f, -1f, 1f);
        }
    }
}
