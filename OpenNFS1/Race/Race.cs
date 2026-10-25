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
        private DateTime? _pausedAt;
        public bool IsPaused => _pausedAt.HasValue;
        public void PauseClock() { if (!_pausedAt.HasValue) _pausedAt = DateTime.Now; }
        public void ResumeClock() { if (_pausedAt.HasValue) { var delta = DateTime.Now - _pausedAt.Value; _raceStartTime += delta; _countdownStartTime += delta; _pausedAt = null; } }
        private TimeSpan _completedRaceTime;
        public bool Started => _started;
		private readonly RaceGateTracker _finishGate;
        private readonly Dictionary<IDriver, TimeSpan> _racerFinishTimes = new Dictionary<IDriver, TimeSpan>();
        private readonly Dictionary<IDriver, RaceGateTracker> _racerGates =
            new Dictionary<IDriver, RaceGateTracker>();
        private readonly Dictionary<IDriver, int> _racerLapCounts =
            new Dictionary<IDriver, int>();
        public IReadOnlyDictionary<IDriver, TimeSpan> RacerFinishTimes => _racerFinishTimes;
        public int FinishNode { get; private set; }
		public bool Finished { get; private set; }
        public bool PolicePursuitActive { get; internal set; }
        public bool PoliceTicketed { get; internal set; }
        public bool PoliceWarned { get; internal set; }
        public bool PoliceArrested { get; internal set; }
        public int PoliceTicketsCount { get; internal set; }
        public float PeakPlayerSpeed { get; private set; }
        public float AveragePlayerSpeed => _speedSampleSeconds > 0 ?
            _accumulatedSpeed / _speedSampleSeconds : 0;
        private float _accumulatedSpeed, _speedSampleSeconds;
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
			FinishNode = checkpoint;
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
			get { return Finished ? _completedRaceTime : _started ? (_pausedAt ?? DateTime.Now) - _raceStartTime : TimeSpan.Zero; }
		}

        // Original NFS SE starting lights (verified against DOSBox recording):
        // 0..2 = three red lights, 3..4 = two yellow lights,
        // 5 = green; after stage 5 the normal racing HUD returns.
        // The vehicle releases when the green phase begins, not when it ends.
        public double StartLightsElapsedSeconds =>
            _countdownStartTime == default ? 0 :
            Math.Max(0, ((_pausedAt ?? DateTime.Now) - _countdownStartTime).TotalSeconds);

        public int StartLightStage => Math.Clamp((int)StartLightsElapsedSeconds, 0, 5);
        public bool StartLightsVisible =>
            _countdownStartTime != default && StartLightsElapsedSeconds < 6.0;

        public int SecondsTillStart => Math.Max(0, 5 - (int)StartLightsElapsedSeconds);


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
            if (d is RacingAIDriver || d is PlayerDriver)
            {
                int gateNode = Track.CheckpointNode > 1
                    ? Math.Min(Track.CheckpointNode, Track.RoadNodes.Count - 2)
                    : Track.RoadNodes.Count - 2;
                _racerGates[d] = new RaceGateTracker(
                    d.Vehicle.CurrentNode?.Number ?? 0, gateNode, Track.IsOpenRoad);
                _racerLapCounts[d] = 0;
            }
		}

		public void Update()
		{
            // Measured on each racing frame; used for authentic Best Times
            // speed records rather than an invented default table.
            if (_started && !Finished)
            {
                float speed = Math.Abs(Player.Vehicle.Speed);
                PeakPlayerSpeed = Math.Max(PeakPlayerSpeed, speed);
                float dt = Math.Clamp(Engine.Instance.FrameTime, 0f, .1f);
                _accumulatedSpeed += speed * dt;
                _speedSampleSeconds += dt;
            }
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

            // Each competitor has an independent real finish/lap gate.
            // This produces actual elapsed times for AI racers that crossed
            // before the player on circuits as well as open-road segments.
            if (_started)
            {
                foreach (var driver in Drivers)
                {
                    if (driver is not RacingAIDriver && driver is not PlayerDriver)
                        continue;
                    if (!_racerGates.TryGetValue(driver, out var gate))
                        continue;
                    int node = driver.Vehicle.CurrentNode?.Number ?? 0;
                    if (gate.Update(node) && !_racerFinishTimes.ContainsKey(driver))
                    {
                        int laps = _racerLapCounts[driver] + 1;
                        _racerLapCounts[driver] = laps;
                        if (Track.IsOpenRoad || laps >= _nbrLaps)
                            _racerFinishTimes.Add(driver, DateTime.Now - _raceStartTime);
                    }
                }
            }

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
