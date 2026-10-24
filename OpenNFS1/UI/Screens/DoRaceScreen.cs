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
			_car.AudioEnabled = GameConfig.SoundEffectsEnabled;

			_race = new Race(_track.IsOpenRoad ? 1 : Math.Clamp(GameConfig.CircuitLaps, 1, 5), _track, _playerDriver);
            // Mode rules are explicit: 1 (Head to Head), 7 (Single/Tournament),
            // 0 (Time Trial). Opponent count is NOT a generic slider.
            var rivals = RaceModeRules.BuildOpponents(
                GameConfig.CurrentRaceMode, GameConfig.UseOpponentPack,
                GameConfig.SelectedTrackDescription, GameConfig.SelectedVehicle,
                GameConfig.SelectedOpponent, VehicleDescription.Descriptions);
            foreach (VehicleDescription rival in rivals)
                _race.AddDriver(new RacingAIDriver(rival));

			_playerUI = new PlayerUI(_car);

						
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
			_car.AudioEnabled = GameConfig.SoundEffectsEnabled;
		}

		public void Draw()
		{
			Engine.Instance.Device.Viewport = _raceViewport;

			_race.Render(_playerUI.ShouldRenderCar);
			TyreSmokeParticleSystem.Instance.Render();

			Engine.Instance.Device.Viewport = _uiViewport;
            // Chase-camera gauge and the tiled bottom panel are below the
            // original HUD. Drawing the map first hid its lower 45 pixels.
			_playerUI.Render();
			_raceUI.Render();

			Engine.Instance.Device.Viewport = _raceViewport;
		}

		#endregion
	}
}
