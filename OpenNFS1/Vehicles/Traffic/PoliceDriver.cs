using System;
using System.Collections.Generic;
using GameEngine;
using Microsoft.Xna.Framework;
using OpenNFS1.Vehicles.AI;
using OpenNFS1.Tracks;

namespace OpenNFS1.Vehicles
{
    internal sealed class PoliceDriver : TrafficDriver
    {
        private readonly IDriver _suspect;
        private readonly PolicePursuitLogic _pursuit;
        private readonly PatrolVehicle _car;
        public bool Parked { get; set; }
        public bool Oncoming { get; set; }
        private float _lane = 3;
        public PoliceDriver(IDriver suspect, PolicePursuitLogic pursuit)
            : base(new PatrolVehicle(), TrafficDriverDirection.Forward)
        {
            _suspect = suspect; _pursuit = pursuit;
            // Reuse the already loaded mesh through CarModelCache.
            _car = (PatrolVehicle)Vehicle; VirtualLane = 3;
        }
        public void ResetHeading() { _car.ClearTurn(); _lane = Oncoming ? 1 : 3; }
        public override void Update(List<IDriver> others)
        {
            var node = Vehicle.CurrentNode;
            if (node?.Next?.Next == null || node.Prev == null)
            { Vehicle.Speed = 0; AtEndOfTrack = true; return; }
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            int gap = _suspect.Vehicle.CurrentNode.Number - node.Number;
            if (_pursuit.IsPursuing && gap > 0) Parked = false;
            if (Parked) { Vehicle.Speed = 0; return; }
            int lane = (int)Math.Round(_lane);
            bool blocked = false, left = true, right = true;
            float blockerSpeed = float.MaxValue;
            foreach (var other in others)
            {
                if (other == this || other == _suspect || other.Vehicle?.CurrentNode == null) continue;
                int separation = other.Vehicle.CurrentNode.Number - node.Number;
                if (separation < -2 || separation > 9) continue;
                int otherLane = EstimateLane(other.Vehicle.Position, node);
                if (otherLane == lane && separation >= 0)
                { blocked = true; blockerSpeed = Math.Min(blockerSpeed, Math.Abs(other.Vehicle.Speed)); }
                if (otherLane == lane - 1) left = false;
                if (otherLane == lane + 1) right = false;
            }
            var decision = PoliceRoadPlanner.Plan(_suspect.Vehicle.Speed, gap, lane,
                blocked, blockerSpeed, left, right);
            float speed = node.TrafficSpeed * PolicePursuitLogic.MpsToGameSpeed;
            int targetLane = Oncoming ? 1 : 3;
            if (_pursuit.IsPursuing)
            {
                speed = decision.TargetSpeed;
                targetLane = decision.TargetLane;
                // Once alongside/ahead early in a high-speed chase, match speed.
                if (gap <= -4 || (gap <= 2 && _pursuit.ChaseTicks < 900 &&
                    _suspect.Vehicle.Speed > node.ChaseLimit * PolicePursuitLogic.MpsToGameSpeed))
                    speed = Math.Abs(_suspect.Vehicle.Speed);
                if (_pursuit.Caught) { speed = 0; targetLane = 4; }
                // Choose an adjacent passing lane when occupying the suspect's lane.
                else if (gap > 0 && gap < 8 && lane == EstimateLane(_suspect.Vehicle.Position, node))
                    targetLane = lane < 4 && right ? lane + 1 : lane > 2 && left ? lane - 1 : lane;
            }
            _lane += Math.Clamp(targetLane - _lane, -dt * .8f, dt * .8f);
            VirtualLane = Math.Clamp((int)Math.Round(_lane), 0, 4);
            var ahead = node;
            int look = Math.Clamp(2 + (int)(Vehicle.Speed / 30), 2, 7);
            for (int i = 0; i < look; i++)
            { var next = Oncoming ? ahead.Prev : ahead.Next; if (next == null) break; ahead = next; }
            Vector3 target = Vector3.Lerp(ahead.GetLeftVerge2(), ahead.GetRightVerge2(), _lane / 4);
            Vector3 delta = target - Vehicle.Position; delta.Y = 0;
            if (delta.LengthSquared() > .01f)
            {
                float angle = Utility.GetSignedAngleBetweenVectors(Vehicle.Direction, delta, true);
                // Curves limit speed; this is adapted to the remake's physical scale.
                speed = Math.Min(speed, 35 + 180 / (1 + Math.Abs(angle) * 6));
                _car.TurnError = angle;
            }
            float rate = speed < Vehicle.Speed ? 70 : 35;
            Vehicle.Speed += Math.Clamp(speed - Vehicle.Speed, -rate * dt, rate * dt);
            Vehicle.SteeringInput = 0;
            Vehicle.Update();
        }
        private static int EstimateLane(Vector3 position, TrackNode node)
        {
            var left = node.GetLeftVerge2(); var across = node.GetRightVerge2() - left;
            float length = across.X * across.X + across.Z * across.Z;
            return length < .001f ? 3 : Math.Clamp((int)Math.Round(
                ((position.X - left.X) * across.X + (position.Z - left.Z) * across.Z) / length * 4), 0, 4);
        }
        private sealed class PatrolVehicle : Vehicle
        {
            public float TurnError;
            public PatrolVehicle() : base(@"SIMDATA\CARFAMS\copmust.CFM") { }
            public void ClearTurn() { _rotationChange = 0; _steeringWheel = 0; TurnError = 0; }
            public override void HandleExtraSteeringPhysics()
            {
                // Base traffic steering accumulates a binary wheel input per frame.
                // Use bounded yaw per second for this path follower instead.
                _rotationChange = PoliceRoadPlanner.YawStep(TurnError, Speed, Engine.Instance.FrameTime);
                if (Speed < .1f) _rotationChange = 0;
            }
        }
    }
}
