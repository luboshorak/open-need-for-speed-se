using System;
using System.Collections.Generic;
using System.Text;
using OpenNFS1.Parsers.Track;
using Microsoft.Xna.Framework;
using GameEngine;
using OpenNFS1.Physics;
using OpenNFS1.Tracks;
using Microsoft.Xna.Framework.Graphics;
using OpenNFS1.Vehicles.AI;

namespace OpenNFS1.Vehicles
{
    class TrafficController
    {
        Race _race;
        private PoliceDriver _police;
        private int _lastPoliceScan, _policeReplacements;
        private float _policeHold;
        private void RemovePolice()
        {
            if (_police != null) _race.Drivers.Remove(_police);
            _police = null;
        }
        private void UpdatePolice()
        {
            var player = _race.Player.Vehicle;
            int at = player.CurrentNode.Number;
            float dt = Math.Clamp(Engine.Instance.FrameTime, 0, .1f);
            if (_policeHold > 0)
            {
                _policeHold = Math.Max(0, _policeHold - dt);
                if (_policeHold <= 0)
                {
                    _race.PoliceWarned = _race.PoliceTicketed = _race.PolicePlayerHeld = false;
                    VehicleController.ForceBrake = _race.PoliceArrested || _race.Finished;
                    if (player is DrivableVehicle car) car.ReleaseFromPolice();
                }
                return;
            }
            if (_race.Finished || at >= _race.FinishNode || _race.PoliceArrested)
            {
                _pursuit.Drop(); RemovePolice();
                _race.PolicePursuitActive = _race.PoliceRadarNear = false;
                return;
            }
            // Scan every newly passed node once, 50 nodes ahead. Police are
            // selected by the original TRI flags, not by a speeding timer.
            if (at > _lastPoliceScan)
            {
                if (_police == null)
                for (int n = _lastPoliceScan + 50; n <= at + 49 && n < _race.FinishNode; n++)
                {
                    var mark = _race.Track.RoadNodes[n];
                    if ((mark.PoliceFlags & 1) == 0) continue;
                    if (!PolicePursuitLogic.SpawnAllowed(mark.PoliceFlags, Engine.Instance.Random.Next(128))) break;
                    try
                    {
                        var cop = new PoliceDriver(_race.Player, _pursuit);
                        _race.AddDriver(cop, mark);
                        bool left = (mark.PoliceFlags & 2) != 0;
                        cop.Oncoming = left && (_race.Track.RoadNodes[0].Flag1 >> 4) != 0;
                        cop.Parked = (mark.PoliceFlags & 4) == 0;
                        cop.VirtualLane = left ? 0 : 4;
                        Vector3 edge = left ? mark.GetLeftVerge() : mark.GetRightVerge();
                        Vector3 across = mark.GetRightVerge() - mark.GetLeftVerge();
                        if (across.LengthSquared() > .01f)
                            edge += Vector3.Normalize(across) * (left ? 1 : -1) * cop.Vehicle.CollisionWidth * .5f;
                        cop.Vehicle.Position = edge;
                        if (cop.Oncoming) cop.Vehicle.Direction = -cop.Vehicle.Direction;
                        cop.Vehicle.Speed = cop.Parked ? 0 : mark.TrafficSpeed * PolicePursuitLogic.MpsToGameSpeed;
                        cop.ResetHeading(); _police = cop; _policeReplacements = 0;
                    }
                    catch (System.IO.IOException error) { Audio.AudioDiagnostics.Record(error); }
                    break;
                }
                _lastPoliceScan = at;
            }
            int gap = _police == null ? int.MaxValue : _police.Vehicle.CurrentNode.Number - at;
            _pursuit.Update(dt, Math.Abs(player.Speed), Math.Abs(_police?.Vehicle.Speed ?? 0),
                gap, player.CurrentNode.PoliceLimit * PolicePursuitLogic.MpsToGameSpeed, _police != null,
                player is not DrivableVehicle suspect || suspect.CanPoliceStop,
                _police != null && !_police.Oncoming && !_police.Parked);
            if (_police != null && _pursuit.IsPursuing && _police.Oncoming && gap < 0)
            {
                // Original turn/placement restores a forward patrol behind the player.
                _police.Vehicle.PlaceOnTrack(_race.Track, _race.Track.RoadNodes[Math.Max(1, at - 18)]);
                _police.Oncoming = _police.Parked = false; _police.ResetHeading();
                _police.Vehicle.Speed = Math.Abs(player.Speed) * .5f;
            }
            if (_police != null && (_police.AtEndOfTrack || gap < -95 || gap > 140))
            {
                if (_pursuit.IsPursuing && _policeReplacements++ == 0 && at > 19)
                {
                    _police.Vehicle.PlaceOnTrack(_race.Track, _race.Track.RoadNodes[at - 18]);
                    _police.Oncoming = _police.Parked = false; _police.ResetHeading();
                    _police.Vehicle.Speed = Math.Abs(player.Speed);
                }
                else { RemovePolice(); _pursuit.Drop(); }
            }
            if (_pursuit.Caught) VehicleController.ForceBrake = true;
            if (_pursuit.WarningIssued || _pursuit.TicketIssued)
            {
                _race.RegisterPoliceStop(_pursuit.TicketIssued);
                Audio.AudioDiagnostics.Note("Police stop: ticket=" + _pursuit.TicketIssued +
                    " warnings=" + _race.PoliceWarningsCount + " tickets=" + _race.PoliceTicketsCount +
                    " arrested=" + _race.PoliceArrested);
                _race.PolicePlayerHeld = true;
                _policeHold = PolicePursuitLogic.PenaltySeconds;
                player.Speed = 0; VehicleController.ForceBrake = true;
                var node = player.CurrentNode;
                Vector3 across = node.GetRightVerge() - node.GetLeftVerge();
                if (across.LengthSquared() > .01f)
                {
                    // Shift only across the road; keep the exact along-road location.
                    var pos = player.Position; var left = node.GetLeftVerge(); var right = node.GetRightVerge();
                    var pulled = PoliceStopPlacement.ToRight(new System.Numerics.Vector3(pos.X,pos.Y,pos.Z),
                        new System.Numerics.Vector3(left.X,left.Y,left.Z),
                        new System.Numerics.Vector3(right.X,right.Y,right.Z), player.CollisionWidth);
                    player.Position = new Vector3(pulled.X,pulled.Y,pulled.Z);
                }
                player.Direction = Vector3.Transform(Vector3.Forward, Matrix.CreateRotationY(MathHelper.ToRadians(node.Orientation)));
                if (player is DrivableVehicle car) { car.RenderDirection = player.Direction; car.HoldForPolice(); }
                _pursuit.ClearEvents(); RemovePolice();
            }
            if (!_pursuit.IsPursuing && !_race.PolicePlayerHeld && !_race.PoliceArrested && !_race.Finished)
                VehicleController.ForceBrake = false;
            _race.PolicePursuitActive = _police != null && _pursuit.IsPursuing;
            _race.PoliceRadarNear = _police != null && Math.Abs(_police.Vehicle.CurrentNode.Number - at) < 10;
        }
        private readonly PolicePursuitLogic _pursuit = new PolicePursuitLogic();
		List<TrafficDriver> _traffic = new List<TrafficDriver>();
		static string[] _trafficModels;
		static TrafficController()
		{
			_trafficModels = new string[] {
				@"SIMDATA\CARFAMS\axxess.CFM",
				@"SIMDATA\CARFAMS\vandura.CFM",
				@"SIMDATA\CARFAMS\bmw.CFM",
				// copmust.CFM has its own dedicated pursuit driver; not civilian traffic.
				@"SIMDATA\CARFAMS\crx.CFM",
				@"SIMDATA\CARFAMS\jeep.CFM",
				@"SIMDATA\CARFAMS\jetta.CFM",
				@"SIMDATA\CARFAMS\lemans.CFM",
				@"SIMDATA\CARFAMS\pickup.CFM",
				@"SIMDATA\CARFAMS\probe.CFM",
				@"SIMDATA\CARFAMS\rodeo.CFM",
				@"SIMDATA\CARFAMS\sunbird.CFM",
				@"SIMDATA\CARFAMS\wagon.CFM"
			};
		}

        public TrafficController(Race race)
        {
            _race = race;
        }


        public void Update()
        {
			var player = _race.Player;
            UpdatePolice();
            for (int i = _traffic.Count - 1; i >= 0; i--)
            {
				var driver = _traffic[i];

				// too far from player
				if (driver.Vehicle.CurrentNode.Number < player.Vehicle.CurrentNode.Number - 30 || driver.Vehicle.CurrentNode.Number > player.Vehicle.CurrentNode.Number + 140)
				{
					_race.Drivers.Remove(driver);
					_traffic.Remove(driver);
					continue;
				}

				// end of track, just stop
				if (driver.Vehicle.CurrentNode.Next == null || driver.Vehicle.CurrentNode.Next.Next == null)
				{
					driver.Vehicle.Speed = 0;
                    driver.AtEndOfTrack = true;
				}

				// start of track just stop
				if (driver.Vehicle.CurrentNode.Prev == null || driver.Vehicle.CurrentNode.Prev.Prev == null)
				{
					driver.Vehicle.Speed = 0;
					//_race.Drivers.Remove(driver);
					//_traffic.Remove(driver);
					//continue;
				}
            }

			// if player is close to the start or end of track, don't spawn new traffic
			if (_race.Player.Vehicle.CurrentNode.Number < 20 || _race.Player.Vehicle.CurrentNode.Number > _race.Track.RoadNodes.Count - 20)
				return;

			while (_traffic.Count < 5)
			{
				int cfmIndex = Engine.Instance.Random.Next(_trafficModels.Length);

				// about 1/3rd of cars should go backwards
				var direction = Engine.Instance.Random.Next() % 3 == 0 ? TrafficDriverDirection.Backward : TrafficDriverDirection.Forward;
				var driver = new TrafficDriver(_trafficModels[cfmIndex], direction);
				int distanceFromPlayer;
				//if (direction == TrafficDriverDirection.Forward)
					distanceFromPlayer = Engine.Instance.Random.Next(40, 200);
				//else
				//	distanceFromPlayer = Engine.Instance.Random.Next(-100, -30);
				int nodeIndex = (_race.Player.Vehicle.CurrentNode.Number + distanceFromPlayer) % _race.Track.RoadNodes.Count - 1;
				nodeIndex = Math.Max(1, nodeIndex);
				_race.AddDriver(driver, _race.Track.RoadNodes[nodeIndex]);
				_traffic.Add(driver);
			}
        }
    }
}
