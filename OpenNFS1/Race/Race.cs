using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNFS1.Physics;
using GameEngine;
using OpenNFS1.UI.Screens;
using OpenNFS1.Parsers.Track;
using OpenNFS1.Tracks;
using OpenNFS1.Vehicles.AI;
using OpenNFS1.Vehicles;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace OpenNFS1
{

	class Race
	{
		int _nbrLaps;
		DateTime _countdownStartTime, _raceStartTime;
		bool _started;
		private readonly RaceGateTracker _finishGate;
		public bool Finished { get; private set; }
		public PlayerDriver Player { get; private set; }
		TrafficController _trafficController;
		public List<IDriver> Drivers { get; private set; }
		public PlayerRaceStats PlayerStats { get; private set; }
		public Track Track { get; private set; }

		public Race(int nbrLaps, Track track, PlayerDriver player)
		{
			_nbrLaps = nbrLaps;
			Player = player;
			Track = track;
			PlayerStats = new PlayerRaceStats();
			Drivers = new List<IDriver>();
			AddDriver(player);
			int checkpoint = track.CheckpointNode > 1
				? Math.Min(track.CheckpointNode, track.RoadNodes.Count - 2)
				: track.RoadNodes.Count - 2;
			_finishGate = new RaceGateTracker(
				player.Vehicle.CurrentNode.Number, checkpoint, track.IsOpenRoad);
			if (track.Description.IsOpenRoad)
			{
				_trafficController = new TrafficController(this);
			}
		}


		public int NbrLaps
		{
			get { return _nbrLaps; }
		}

		public TimeSpan RaceTime
		{
			get { return new TimeSpan(DateTime.Now.Ticks - _raceStartTime.Ticks); }
		}

		public int SecondsTillStart
		{
			get { return 3 - (int)new TimeSpan(DateTime.Now.Ticks - _countdownStartTime.Ticks).TotalSeconds; }
		}


		public void StartCountdown()
		{
			VehicleController.ForceBrake = false;
			_countdownStartTime = DateTime.Now;
		}

		public void AddDriver(IDriver d)
		{
			AddDriver(d, Track.RoadNodes[1]);
		}

		// add a driver to the race, placing him in a starting grid
		public void AddDriver(IDriver d, TrackNode startNode)
		{
			d.Vehicle.PlaceOnTrack(Track, startNode);
			if (d is TrafficDriver)
			{
			}
			else if (d is RacingAIDriver || d is PlayerDriver)
			{
				// place on starting grid
				int lane = (Drivers.Count % 2 == 0 ? AIDriver.MaxVirtualLanes - 1 : 1);
				if (d is RacingAIDriver)
				{
					((RacingAIDriver)d).VirtualLane = lane;
				}
				Vector3 pos = d.Vehicle.CurrentNode.Position;
				pos.Z -= Drivers.Count * 30;
				pos.X = Vector3.Lerp(d.Vehicle.CurrentNode.GetLeftVerge2(), d.Vehicle.CurrentNode.GetRightVerge2(), (float)lane / (AIDriver.MaxVirtualLanes)).X;
				d.Vehicle.Position = pos;
			}
			Drivers.Add(d);
		}

		public void Update()
		{
			foreach (var driver in Drivers)
				driver.Update(Drivers);

            // Resolve contacts after every vehicle has moved this frame.
            // Previously cars could drive straight through one another.
            VehicleCollisionResolver.Resolve(Drivers);

			Track.Update();

			// Gate detection must handle jumping across several nodes in one update.
			// Do not count start-grid movement before the countdown completes.
			if (_started && !Finished &&
				_finishGate.Update(Player.Vehicle.CurrentNode.Number))
			{
				PlayerStats.OnLapStarted(); // records the just-finished lap or road stage
				if (PlayerStats.CurrentLap > _nbrLaps)
				{
					// Waiting for exact Speed == 0 left some races unfinished forever.
					Finished = true;
					VehicleController.ForceBrake = true;
				}
			}

			var racingDrivers = new List<IDriver>(Drivers.Where(a => a is RacingAIDriver || a is PlayerDriver));
			racingDrivers.Sort((a1, a2) => a2.Vehicle.TrackPosition.CompareTo(a1.Vehicle.TrackPosition));
			PlayerStats.Position = racingDrivers.FindIndex(a => a == Player);

			if (SecondsTillStart <= 0 && !_started)
			{
				foreach (var d in racingDrivers)
				{
					((DrivableVehicle)d.Vehicle).Motor.Gearbox.CurrentGear = 1;
				}
				_raceStartTime = DateTime.Now;
				PlayerStats.OnLapStarted();
				_started = true;
			}


			if (_trafficController != null) _trafficController.Update();
		}

		public void Render(bool renderPlayerVehicle)
		{
			Engine.Instance.Device.BlendState = BlendState.Opaque;
			Engine.Instance.Device.DepthStencilState = DepthStencilState.Default;
			Engine.Instance.Device.SamplerStates[0] = GameConfig.WrapSampler;

			Track.Render(Engine.Instance.Camera.Position, Player.Vehicle.CurrentNode);

			var frustum = new BoundingFrustum(Engine.Instance.Camera.View * Engine.Instance.Camera.Projection);
			
			foreach (var driver in Drivers)
			{
				bool isPlayer = driver == Player;
				if (isPlayer && !renderPlayerVehicle)
					continue;

				if (!frustum.Intersects(driver.Vehicle.BoundingSphere))
					continue;
				if (driver.Vehicle is DrivableVehicle)
				{
					((DrivableVehicle)driver.Vehicle).RenderShadow(isPlayer);
				}
                if (driver is AIDriver && ((AIDriver)driver).AtEndOfTrack)
                    continue; 

				driver.Vehicle.Render();
			}
		}
	}
}
