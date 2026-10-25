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
        private TimeSpan _completedRaceTime;
        public bool Started => _started;
		private readonly RaceGateTracker _finishGate;
		public bool Finished { get; private set; }
        public bool PolicePursuitActive { get; internal set; }
        public bool PoliceTicketed { get; internal set; }
        public bool PoliceWarned { get; internal set; }
        public bool PoliceArrested { get; internal set; }
        public int PoliceTicketsCount { get; internal set; }
        public float PeakPlayerSpeed { get; private set; }
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
			// In NFS SE open-road traffic belongs to Head to Head only.
            // No Mercy suppresses traffic; cops still require separate AI work.
            if (track.Description.IsOpenRoad &&
                GameConfig.CurrentRaceMode == RaceMode.HeadToHead &&
                !GameConfig.NoMercy)
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
			get { return Finished ? _completedRaceTime : _started ? DateTime.Now - _raceStartTime : TimeSpan.Zero; }
		}

		public int SecondsTillStart
		{
			get { return 3 - (int)new TimeSpan(DateTime.Now.Ticks - _countdownStartTime.Ticks).TotalSeconds; }
		}


		public void StartCountdown()
		{
			VehicleController.ForceBrake = true;
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
            // Measured on each racing frame; used for authentic Best Times
            // speed records rather than an invented default table.
            if (_started && !Finished)
                PeakPlayerSpeed = Math.Max(PeakPlayerSpeed,
                    Math.Abs(Player.Vehicle.Speed));
            if (!_started && SecondsTillStart > 0)
            {
                foreach (var driver in Drivers)
                {
                    driver.Vehicle.Speed = 0;
                    if (driver.Vehicle is DrivableVehicle car)
                    {
                        car.ThrottlePedalInput = 0;
                        car.BrakePedalInput = 1;
                    }
                }
                Track.Update();
                return;
            }
            if (!_started)
            {
                var racers = Drivers.Where(d => d is RacingAIDriver || d is PlayerDriver);
                foreach (var d in racers)
                    ((DrivableVehicle)d.Vehicle).Motor.Gearbox.CurrentGear = 1;
                _raceStartTime = DateTime.Now;
                PlayerStats.OnLapStarted();
                _started = true;
                VehicleController.ForceBrake = false;
            }
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
					_completedRaceTime = DateTime.Now - _raceStartTime;
                    Finished = true;
                    VehicleController.ForceBrake = true;
				}
			}

			var racingDrivers = new List<IDriver>(Drivers.Where(a => a is RacingAIDriver || a is PlayerDriver));
			racingDrivers.Sort((a1, a2) => a2.Vehicle.TrackPosition.CompareTo(a1.Vehicle.TrackPosition));
			PlayerStats.Position = racingDrivers.FindIndex(a => a == Player);


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
					if (isPlayer || GameConfig.HighCarDetail)
                        ((DrivableVehicle)driver.Vehicle).RenderShadow(isPlayer);
				}
                if (driver is AIDriver && ((AIDriver)driver).AtEndOfTrack)
                    continue; 

				driver.Vehicle.Render();
			}
		}
	}
}
