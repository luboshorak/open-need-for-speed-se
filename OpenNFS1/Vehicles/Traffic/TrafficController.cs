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
            int playerNode = player.Vehicle.CurrentNode?.Number ?? 0;
            float nodeGap = _police == null ? float.MaxValue :
                Math.Abs(playerNode - (_police.Vehicle.CurrentNode?.Number ?? playerNode));
            bool spawn = _pursuit.Update(Engine.Instance.FrameTime,
                Math.Abs(player.Vehicle.Speed), nodeGap);

            if (_pursuit.TicketIssued)
            {
                _race.PoliceTicketed = true;
                _pursuit.ClearTicket();
                if (_police != null)
                {
                    _race.Drivers.Remove(_police);
                    _police = null;
                }
            }

            if (_police != null && (_police.AtEndOfTrack || nodeGap > 95))
            {
                _race.Drivers.Remove(_police);
                _police = null;
                _pursuit.ResetPursuit();
            }

            if (spawn && _police == null && playerNode >= 30 &&
                playerNode < _race.Track.RoadNodes.Count - 40)
            {
                int spawnNode = Math.Max(2, playerNode - 15);
                try
                {
                    var officer = new PoliceDriver(player);
                    _race.AddDriver(officer, _race.Track.RoadNodes[spawnNode]);
                    _police = officer;
                }
                catch (Exception ex) when (ex is System.IO.IOException ||
                    ex is InvalidOperationException || ex is NullReferenceException)
                {
                    System.Diagnostics.Trace.WriteLine("Police Mustang cannot spawn: " + ex);
                    _pursuit.ResetPursuit();
                }
            }
            _race.PolicePursuitActive = _police != null;
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
