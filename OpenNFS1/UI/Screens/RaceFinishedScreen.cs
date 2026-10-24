using System;
using System.Collections.Generic;
using System.Text;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenNFS1.Parsers.Track;

namespace OpenNFS1.UI.Screens
{
    class RaceFinishedScreen : BaseUIScreen, IGameScreen
    {
        Race _race;
        Texture2D _background;
        Track _raceTrack;

        int _selectedOption = 0;

        public RaceFinishedScreen(Race race, Track raceTrack)
            : base()
        {
            _race = race;
            _raceTrack = raceTrack;
            _background = ScreenEffects.TakeScreenshot();

            // A results screen is created once after each race. Save points
            // here (not on keyboard input) so retries/clicks cannot score twice.
            if (GameConfig.CurrentRaceMode == RaceMode.Tournament && GameConfig.Tournament != null)
                GameConfig.Tournament.RecordResult(race.PlayerStats.Position, race.RaceTime);

            GC.Collect(); //force some memory freeness here.
        }


        #region IDrawableObject Members

        public void Update(GameTime gameTime)
        {
            if (Engine.Instance.Input.WasLeftMouseClicked)
            {
                var p = Engine.Instance.Input.GameMousePosition;
                if (p.X >= 20 && p.X <= 390 && p.Y >= 370 && p.Y < 419)
                {
                    _selectedOption = 0;
                    ActivateSelection();
                    return;
                }
                if (p.X >= 20 && p.X <= 390 && p.Y >= 419 && p.Y < 480)
                {
                    _selectedOption = 1;
                    ActivateSelection();
                    return;
                }
            }

            if (UIController.Back)
            {
                ReturnToMenu();
                return;
            }

            if (UIController.Up && _selectedOption > 0)
                _selectedOption--;
            else if (UIController.Down && _selectedOption < 1)
                _selectedOption++;

            if (UIController.Ok)
                ActivateSelection();
        }

        private void ReturnToMenu()
        {
            GameConfig.Tournament = null;
            Engine.Instance.Screen = new HomeScreen();
        }

        private void ActivateSelection()
        {
            if (_selectedOption == 1)
            {
                ReturnToMenu();
                return;
            }

            if (GameConfig.CurrentRaceMode == RaceMode.Tournament &&
                GameConfig.Tournament != null)
            {
                if (GameConfig.Tournament.TryAdvance())
                {
                    GameConfig.SelectedTrackDescription = GameConfig.Tournament.CurrentCourse;
                    Engine.Instance.Screen = new LoadRaceScreen();
                }
                else
                {
                    ReturnToMenu();
                }
                return;
            }

            if (!GameConfig.SelectedTrackDescription.IsOpenRoad)
                Engine.Instance.Screen = new DoRaceScreen(_raceTrack);
            else
            {
                GameConfig.SelectedTrackDescription =
                    TrackDescription.GetNextOpenRoadStage(GameConfig.SelectedTrackDescription);
                Engine.Instance.Screen = GameConfig.SelectedTrackDescription == null
                    ? new HomeScreen() : new LoadRaceScreen();
            }
        }

		public override void Draw()
		{
			base.Draw();

            Engine.Instance.SpriteBatch.Draw(_background, Vector2.Zero, new Color(255, 255, 255, 100));

			if (GameConfig.SelectedTrackDescription.IsOpenRoad)
                DrawOpenRoadResult();
            else
                DrawCircuitResult();

            Engine.Instance.SpriteBatch.End();
        }

        #endregion

        private void DrawCircuitResult()
        {
			WriteLine(GameConfig.SelectedTrackDescription.Name + "- Race completed", Color.Gray, 20, 30, TitleSize);
            
            int totalSeconds = 0;
            for (int i = 0; i < _race.PlayerStats.LapTimes.Count; i++)
            {
				WriteLine("Lap " + (i + 1) + ": " + TimeSpan.FromSeconds(_race.PlayerStats.LapTimes[i]).ToString());
                totalSeconds += _race.PlayerStats.LapTimes[i];
            }

			WriteLine("Total time: " + TimeSpan.FromSeconds(totalSeconds));
			
            if (GameConfig.CurrentRaceMode == RaceMode.Tournament &&
                GameConfig.Tournament != null)
            {
                var series = GameConfig.Tournament;
                Engine.Instance.SpriteBatch.DrawString(Font,
                    "TOURNAMENT " + series.CompletedStages + "/" + series.StageCount +
                    "   SCORE " + series.Points,
                    new Vector2(30, 311), Color.Yellow,
                    0f, Vector2.Zero, 0.52f, SpriteEffects.None, 0);
                DrawMenuActions(series.IsComplete ? "Finish tournament" : "Next tournament race");
            }
            else
            {
                DrawMenuActions("Race again");
            }
        }

        private void DrawOpenRoadResult()
        {
			WriteLine(GameConfig.SelectedTrackDescription.Name + " - Stage completed", Color.Gray, 20, 30, TitleSize);
            
			WriteLine("Time: " + TimeSpan.FromSeconds(_race.PlayerStats.LapTimes[0]));

            DrawMenuActions(
                TrackDescription.GetNextOpenRoadStage(GameConfig.SelectedTrackDescription) != null
                    ? "Continue to next stage" : "Main menu");
        }

        private void DrawMenuActions(string firstAction)
        {
            var sprite = Engine.Instance.SpriteBatch;
            sprite.DrawString(Font, firstAction, new Vector2(30, 377),
                _selectedOption == 0 ? Color.Yellow : Color.LightGray,
                0, Vector2.Zero, SectionSize, SpriteEffects.None, 0);
            sprite.DrawString(Font, "Main menu", new Vector2(30, 421),
                _selectedOption == 1 ? Color.Yellow : Color.LightGray,
                0, Vector2.Zero, SectionSize, SpriteEffects.None, 0);
        }

    }
}
