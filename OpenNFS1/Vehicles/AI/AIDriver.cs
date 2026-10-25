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
        private readonly DrivableVehicle _vehicle;
        private float _stalled, _reverseSeconds, _laneCooldown;
        private int _recoveryAttempts;
        public RacingAIDriver(VehicleDescription descriptor)
        {
            _vehicle = new DrivableVehicle(descriptor, false);
            _vehicle.SteeringSpeed = 6; _vehicle.AutoDrift = false;
            Vehicle = _vehicle;
        }
        private Vector3 RoadTarget(int lane)
        {
            var ahead = _vehicle.CurrentNode;
            int look = Math.Clamp(2 + (int)(Math.Abs(_vehicle.Speed) / 25), 2, 7);
            for (int i=0;i<look && ahead.Next!=null;i++) ahead=ahead.Next;
            return Vector3.Lerp(ahead.GetLeftVerge2(), ahead.GetRightVerge2(), lane / 4f);
        }
        private float Along(Vector3 point) => Vector3.Dot(point - _vehicle.Position, _vehicle.Direction);
        public override void Update(List<IDriver> others)
        {
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            var node = _vehicle.CurrentNode;
            if (node?.Next?.Next == null) { _vehicle.Speed=0; AtEndOfTrack=true; return; }
            int minimumLane = (_vehicle.Track.RoadNodes[0].Flag1 >> 4) != 0 ? 2 : 0;
            VirtualLane = Math.Clamp(VirtualLane, minimumLane, 4);
            var threats = new List<RacerTrafficPlanner.Obstacle>();
            bool rearClear = true;
            foreach (var other in others)
            {
                if (other==this || other.Vehicle?.CurrentNode==null) continue;
                Vector3 delta = other.Vehicle.Position - _vehicle.Position;
                if (Math.Abs(delta.Y) > (_vehicle.CollisionHeight+other.Vehicle.CollisionHeight)*.5f) continue;
                float along = Along(other.Vehicle.Position);
                if (along < -35 || along > 650) continue;
                float side=Vector3.Dot(delta,_vehicle.Right);
                float width=(_vehicle.CollisionWidth+other.Vehicle.CollisionWidth)*.5f+1;
                if (along < 0 && Math.Abs(side)<width && along > -35) rearClear=false;
                float closing=(_vehicle.Speed - other.Vehicle.Speed *
                    Vector3.Dot(_vehicle.Direction,other.Vehicle.Direction))*2.5f;
                threats.Add(new RacerTrafficPlanner.Obstacle(along,side,width,closing,
                    (_vehicle.CollisionLength+other.Vehicle.CollisionLength)*.5f));
            }
            float[] laneOffsets = new float[5];
            for(int lane=0;lane<5;lane++) laneOffsets[lane]=Vector3.Dot(RoadTarget(lane)-_vehicle.Position,_vehicle.Right);
            var plan=RacerTrafficPlanner.Plan(VirtualLane, minimumLane, laneOffsets, threats);
            _laneCooldown=Math.Max(0,_laneCooldown-dt);
            if (_laneCooldown<=0 && plan.Lane!=VirtualLane)
            { VirtualLane=plan.Lane; _laneCooldown=1.2f; }
            if (_reverseSeconds>0)
            {
                _reverseSeconds=Math.Max(0,_reverseSeconds-dt);
                _vehicle.ThrottlePedalInput=0; _vehicle.BrakePedalInput=0;
                _vehicle.Speed=rearClear ? -10 : 0;
                _vehicle.SteeringInput=0;
                _vehicle.Update();
                if (_reverseSeconds==0)
                {
                    _vehicle.Speed=0; _stalled=0;
                    _vehicle.Motor.Gearbox.CurrentGear=1;
                    _vehicle.RotateCarAfterCollision=0;
                    // A crash can leave the vehicle facing sideways or backwards.
                    // Realign on its existing node, without advancing race progress.
                    var forward=Vector3.Transform(Vector3.Forward, Matrix.CreateRotationY(MathHelper.ToRadians(node.Orientation)));
                    if (Vector3.Dot(_vehicle.Direction,forward)<.4f)
                    { _vehicle.Direction=forward; _vehicle.RenderDirection=forward; }
                    VirtualLane=plan.Lane;
                }
                return;
            }
            Vector3 target=RoadTarget(VirtualLane)-_vehicle.Position; target.Y=0;
            float angle=target.LengthSquared()>.01f ? Utility.GetSignedAngleBetweenVectors(_vehicle.Direction,target,true) : 0;
            float desiredWheel=Math.Clamp(-angle*.55f,-Vehicle.MaxSteeringLock,Vehicle.MaxSteeringLock);
            _vehicle.SteeringInput=Math.Clamp((desiredWheel-_vehicle._steeringWheel)/Math.Max(.001f,6*dt),-1,1);
            float curveSpeed=55+150/(1+Math.Abs(angle)*4);
            _vehicle.ThrottlePedalInput=plan.Brake>0 || Math.Abs(_vehicle.Speed)>curveSpeed ? 0 : .9f;
            _vehicle.BrakePedalInput=Math.Max(plan.Brake,Math.Abs(_vehicle.Speed)>curveSpeed ? .35f : 0);
            if (_vehicle.Motor.Gearbox.GearEngaged && _vehicle.Motor.Gearbox.CurrentGear<=0)
                _vehicle.Motor.Gearbox.CurrentGear=1;
            // Stalls are geometric, not restricted to a tiny cone in front.
            _stalled=Math.Abs(_vehicle.Speed)<4 ? _stalled+dt : 0;
            if (Math.Abs(_vehicle.Speed)>15) _recoveryAttempts=0;
            if (_stalled>1.5f && rearClear)
            { _reverseSeconds=1.3f; _stalled=0; _recoveryAttempts++; }
            if (_stalled>5 || _recoveryAttempts>=3)
            {
                // Last-resort crash recovery: find a clear pose at THIS road node.
                // Never jump forward or restore the car inside another vehicle.
                for(int lane=4;lane>=minimumLane;lane--)
                {
                    var position=Vector3.Lerp(node.GetLeftVerge2(),node.GetRightVerge2(),lane/4f);
                    bool clear=true;
                    foreach(var other in others)
                        if(other!=this && Vector3.Distance(other.Vehicle.Position,position)<
                            (_vehicle.CollisionLength+other.Vehicle.CollisionLength)*.5f+3) { clear=false; break; }
                    if(!clear) continue;
                    _vehicle.Position=position;
                    _vehicle.Direction=Vector3.Transform(Vector3.Forward,Matrix.CreateRotationY(MathHelper.ToRadians(node.Orientation)));
                    _vehicle.RenderDirection=_vehicle.Direction; _vehicle.RotateCarAfterCollision=0;
                    _vehicle.Speed=0; VirtualLane=lane; _stalled=0; _reverseSeconds=0; _recoveryAttempts=0;
                    break;
                }
            }
            _vehicle.Update();
        }
    }
}
