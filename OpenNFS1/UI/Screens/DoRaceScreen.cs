using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameEngine;
using OpenNFS1.Parsers.Track;
using OpenNFS1.Physics;
using OpenNFS1.UI;
using OpenNFS1.UI.Screens;
using OpenNFS1.Vehicles;
using OpenNFS1.Vehicles.AI;


namespace OpenNFS1
{
	class DoRaceScreen : IGameScreen
	{
		Track _track;
		DrivableVehicle _car;
		Race _race;
		RaceUI _raceUI;
		PlayerUI _playerUI;
		Viewport _raceViewport, _uiViewport;
		PlayerDriver _playerDriver;

		public DoRaceScreen(Track track)
		{
			_track = track;
			_car = new DrivableVehicle(GameConfig.SelectedVehicle);

			_playerDriver = new PlayerDriver(_car);
			_car.AudioEnabled = true;

			_race = new Race(_track.IsOpenRoad ? 1 : Math.Clamp(GameConfig.CircuitLaps, 1, 5), _track, _playerDriver);
			// Time trial is solo; standard races use the opponent count chosen in the menu.
			int opponentCount = GameConfig.TimeTrial ? 0 : Math.Clamp(GameConfig.OpponentCount, 0, 10);
			for (int i = 0; i < opponentCount; i++)
			{
				// The first AI racer uses the vehicle chosen in Control Central.
				// The other AI drivers keep the classic randomized selection.
				VehicleDescription opponent = i == 0 && GameConfig.SelectedOpponent != null
					? GameConfig.SelectedOpponent
					: VehicleDescription.Descriptions[Engine.Instance.Random.Next(VehicleDescription.Descriptions.Count)];
				_race.AddDriver(new RacingAIDriver(opponent));
			}
				//_race.AddDriver(new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "Viper")));
				//_race.AddDriver(new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "Viper")));
				//_race.AddDriver(new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "Viper")));
				//_race.AddDriver(new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "Viper")));
				_playerUI = new PlayerUI(_car);
			/*
			d = new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "911"));
			_aiDrivers.Add(d);
			_track.AddDriver(d);
			d = new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "Viper"));
			_aiDrivers.Add(d);
			_track.AddDriver(d);
			d = new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "Diablo"));
			_aiDrivers.Add(d);
			_track.AddDriver(d);
			d = new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "F512"));
			_aiDrivers.Add(d);
			_track.AddDriver(d);
			d = new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "ZR1"));
			_aiDrivers.Add(d);
			_track.AddDriver(d);
			d = new AIDriver(VehicleDescription.Descriptions.Find(a => a.Name == "NSX"));
			_aiDrivers.Add(d);
			_track.AddDriver(d);
			*/
						
			_raceUI = new RaceUI(_race);
			_race.StartCountdown();

			_raceViewport = new Viewport(0, 0, 640, 400);
			_uiViewport = new Viewport(0, 0, 640, 480);
		}

		#region IDrawableObject Members

		public void Update(GameTime gameTime)
		{
			Engine.Instance.Device.Viewport = _raceViewport;

			_race.Update();
			TyreSmokeParticleSystem.Instance.Update();
			
			_playerUI.Update(gameTime);

			if (_race.Finished)
			{
				_car.AudioEnabled = false;
				Engine.Instance.Screen = new RaceFinishedScreen(_race, _track);
				return;
			}

			if (UIController.Pause)
			{
				Pause();
				return;
			}

			// Always provide an escape hatch when debugging unfinished races.
			// Esc opens the pause menu; M goes straight back to Control Central.
			if (Engine.Instance.Input.WasPressed(Keys.M))
			{
				_car.AudioEnabled = false;
				VehicleController.ForceBrake = false;
				Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
				Engine.Instance.Screen = new HomeScreen();
				return;
			}

			if (Engine.Instance.Input.WasPressed(Keys.R))
			{
				_car.Reset();
			}

			TyreSmokeParticleSystem.Instance.SetCamera(Engine.Instance.Camera);
			Engine.Instance.Camera.Update(gameTime);
		}

		public void Pause()
		{
			_car.AudioEnabled = false;
			Engine.Instance.Screen = new RacePausedScreen(this);
		}

		public void Resume()
		{
			_car.AudioEnabled = true;
		}

		public void Draw()
		{
			Engine.Instance.Device.Viewport = _raceViewport;

			_race.Render(_playerUI.ShouldRenderCar);
			TyreSmokeParticleSystem.Instance.Render();

			Engine.Instance.Device.Viewport = _uiViewport;
			_raceUI.Render();
			_playerUI.Render();

			Engine.Instance.Device.Viewport = _raceViewport;
		}

		#endregion
	}
}
