using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using GameEngine;
using OpenNFS1.Parsers.Track;
using OpenNFS1.Physics;
using OpenNFS1.Tracks;

namespace OpenNFS1.Vehicles.AI
{

	abstract class AIDriver : IDriver
	{
		public int VirtualLane { get; set; }
		public Vehicle Vehicle { get; protected set; }
        public Boolean AtEndOfTrack { get; set; }

		public const int MaxVirtualLanes = 4;

		public virtual Vector3 GetNextTarget()
		{
			return Vector3.Lerp(Vehicle.CurrentNode.Next.Next.GetLeftVerge2(), Vehicle.CurrentNode.Next.Next.GetRightVerge2(), (float)VirtualLane / (MaxVirtualLanes));
		}
				
		protected virtual void FollowTrack()
		{
			var target = GetNextTarget();
			if (GameConfig.DrawDebugInfo)
			{
				Engine.Instance.GraphicsUtils.AddCube(Matrix.CreateTranslation(target), Color.Yellow);
			}
			float angle = Utility.GetSignedAngleBetweenVectors(Vehicle.Direction, target - Vehicle.Position, true);
			if (angle < -0.1f)
			{
				Vehicle.SteeringInput = 0.5f;
			}
			else if (angle > 0.1f)
			{
				Vehicle.SteeringInput = -0.5f;
			}
			else
			{
				Vehicle.SteeringInput = 0;
			}
		}

		public abstract void Update(List<IDriver> otherDrivers);

	}

	class RacingAIDriver : AIDriver
	{		
		private float _firstLaneChangeAllowed;  //avoid all racers changing lanes immediately
		private DrivableVehicle _vehicle;
        private float _blockedSeconds;
        private float _reverseSeconds;
        private float _previousProgress;
        public bool AtEndOfTrack;

		public RacingAIDriver(VehicleDescription vehicleDescriptor)
		{
			_vehicle = new DrivableVehicle(vehicleDescriptor);
			_vehicle.SteeringSpeed = 6;
			_vehicle.AutoDrift = false;
			Vehicle = _vehicle;
			_firstLaneChangeAllowed = Engine.Instance.Random.Next(5, 40);
		}

        public override void Update(List<IDriver> otherDrivers)
        {
            float dt = MathHelper.Clamp(Engine.Instance.FrameTime, 0, 0.1f);
            var node = _vehicle.CurrentNode;
            if (node?.Next?.Next == null)
            {
                _vehicle.ThrottlePedalInput = 0;
                _vehicle.BrakePedalInput = 1;
                AtEndOfTrack = true;
                return;
            }

            // Recovery for bumper-to-bumper pileups: a racing AI must not
            // indefinitely hold the throttle against a stationary vehicle.
            if (_reverseSeconds > 0)
            {
                _reverseSeconds -= dt;
                _vehicle.ThrottlePedalInput = 0;
                _vehicle.BrakePedalInput = 0;
                _vehicle.Speed = -Math.Max(8f, Math.Min(14f, Math.Abs(_vehicle.Speed)));
                _vehicle.SteeringInput = 0;
                _vehicle.Update();
                if (_reverseSeconds <= 0)
                {
                    _vehicle.Speed = 0;
                    _blockedSeconds = 0;
                    VirtualLane = (VirtualLane + 1) % (MaxVirtualLanes + 1);
                }
                return;
            }

            bool blockedAhead = false;
            float nearest = float.MaxValue;
            foreach (var driver in otherDrivers)
            {
                if (driver == this || driver.Vehicle?.CurrentNode == null) continue;
                Vector3 delta = driver.Vehicle.Position - _vehicle.Position;
                delta.Y = 0;
                float distance = delta.Length();
                // Geometric detection also includes the player, rather than
                // relying on possibly stale virtual lane indices.
                if (distance < 24f && distance > 0.01f &&
                    Vector3.Dot(delta, _vehicle.Direction) > 0 &&
                    distance < nearest)
                {
                    nearest = distance;
                    blockedAhead = true;
                }
            }

            FollowTrack();
            _vehicle.ThrottlePedalInput = blockedAhead ? 0 : 0.9f;
            _vehicle.BrakePedalInput = blockedAhead && nearest < 17f ? 0.65f : 0;
            if (blockedAhead && nearest < 18f)
            {
                // A stationary blocker should eventually cause a cautious
                // reversal instead of trapping both cars forever.
                bool stationary = Math.Abs(_vehicle.Speed) < 10f;
                _blockedSeconds = stationary ? _blockedSeconds + dt : 0f;
                if (_blockedSeconds >= 1.5f)
                {
                    _reverseSeconds = 1.3f;
                    _blockedSeconds = 0;
                    _vehicle.ThrottlePedalInput = 0;
                    _vehicle.BrakePedalInput = 0;
                }
            }
            else
            {
                _blockedSeconds = 0;
            }
            _vehicle.Update();
        }

		public void Render()
		{
			
		}
	}
}
