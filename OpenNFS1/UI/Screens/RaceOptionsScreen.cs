using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameEngine;

namespace OpenNFS1.UI.Screens
{
    /// <summary>Race setup usable with either keyboard or mouse.</summary>
    class RaceOptionsScreen : BaseUIScreen, IGameScreen
    {
        private int _selectedOption;
        private int _gearboxOption;
        private int _timeOfDayOption;

        public RaceOptionsScreen()
        {
            _selectedOption = 4;
            _gearboxOption = GameConfig.ManualGearbox ? 1 : 0;
            _timeOfDayOption = GameConfig.AlternativeTimeOfDay ? 1 : 0;
        }

        public void Update(GameTime gameTime)
        {
            var input = Engine.Instance.Input;
            if (input.WasLeftMouseClicked)
            {
                Vector2 p = input.GameMousePosition;
                if (p.X >= 0 && HandleClick((int)p.X, (int)p.Y))
                    return;
            }

            if (input.WasPressed(Keys.Escape))
            {
                Engine.Instance.Screen = new HomeScreen();
                return;
            }
            if (input.WasPressed(Keys.Down) || input.WasPressed(Keys.Tab))
                _selectedOption = (_selectedOption + 1) % 5;
            else if (input.WasPressed(Keys.Up))
                _selectedOption = (_selectedOption + 4) % 5;
            else if (input.WasPressed(Keys.Left))
                ChangeValue(-1);
            else if (input.WasPressed(Keys.Right))
                ChangeValue(1);
            else if (input.WasPressed(Keys.Enter))
            {
                if (_selectedOption == 4)
                    StartRace();
                else
                    ChangeValue(1);
            }
        }

        private bool HandleClick(int x, int y)
        {
            if (x >= 25 && x <= 370 && y >= 90 && y <= 153)
            {
                _selectedOption = 0;
                _gearboxOption = x < 110 ? 0 : 1;
            }
            else if (x >= 25 && x <= 370 && y >= 178 && y <= 238)
            {
                _selectedOption = 1;
                if (x < 115) _timeOfDayOption = 0;
                else if (GameConfig.SelectedTrackDescription.AlternativeTimeOfDay != null)
                    _timeOfDayOption = 1;
            }
            else if (x >= 25 && x <= 370 && y >= 256 && y <= 322)
            {
                _selectedOption = 2;
                // Opponent count is fixed by the chosen original mode.
                // Change the rival or The Pack in Control Central instead.
            }
            else if (x >= 25 && x <= 370 && y > 322 && y <= 375)
            {
                _selectedOption = 3;
                if (!GameConfig.SelectedTrackDescription.IsOpenRoad)
                    GameConfig.CircuitLaps = Math.Clamp(GameConfig.CircuitLaps +
                        (x < 160 ? -1 : 1), 1, 5);
            }
            else if (x >= 25 && x <= 255 && y > 375 && y <= 422)
            {
                _selectedOption = 4;
                StartRace();
            }
            else if (x >= 25 && x <= 255 && y > 422 && y <= 480)
            {
                Engine.Instance.Screen = new HomeScreen();
            }
            else
                return false;
            return true;
        }

        private void ChangeValue(int direction)
        {
            switch (_selectedOption)
            {
                case 0:
                    _gearboxOption = direction > 0 ? 1 : 0;
                    break;
                case 1:
                    _timeOfDayOption = direction > 0 &&
                        GameConfig.SelectedTrackDescription.AlternativeTimeOfDay != null ? 1 : 0;
                    break;
                case 2:
                    // Original single-player mode determines opponent count.
                    break;
                case 3:
                    if (!GameConfig.SelectedTrackDescription.IsOpenRoad)
                        GameConfig.CircuitLaps = Math.Clamp(GameConfig.CircuitLaps + direction, 1, 5);
                    break;
            }
        }

        private void StartRace()
        {
            GameConfig.ManualGearbox = _gearboxOption == 1;
            GameConfig.AlternativeTimeOfDay = _timeOfDayOption == 1;

            if (GameConfig.CurrentRaceMode == RaceMode.Tournament &&
                !RaceModeRules.IsVehicleAllowed(
                    RaceMode.Tournament, GameConfig.SelectedTrackDescription, GameConfig.SelectedVehicle))
            {
                GameConfig.SelectedVehicle = OpenNFS1.Vehicles.VehicleDescription.Descriptions.Find(
                    car => RaceModeRules.IsVehicleAllowed(
                        RaceMode.Tournament, GameConfig.SelectedTrackDescription, car));
            }

            // Begin a fresh local championship only when the player starts.
            // The next courses are selected by RaceFinishedScreen, not here.
            if (GameConfig.CurrentRaceMode == RaceMode.Tournament)
            {
                GameConfig.Tournament = new TournamentSeries(
                    GameConfig.SelectedTrackDescription, TrackDescription.Descriptions);
                GameConfig.SelectedTrackDescription = GameConfig.Tournament.CurrentCourse;
                // Resume point before the very first race, useful even if the
                // player has not yet reached the first results screen.
                try
                {
                    TournamentSaveManager.Save(0, GameConfig.SelectedTrackDescription);
                }
                catch (System.IO.IOException) { /* No writable profile: still allow racing */ }
                catch (UnauthorizedAccessException) { }
            }
            else
            {
                GameConfig.Tournament = null;
            }

            Engine.Instance.Screen = new LoadRaceScreen();
        }

        public override void Draw()
        {
            base.Draw();
            WriteLine("Race settings", Color.White, 20, 30, TitleSize);
            WriteLine("Gearbox", _selectedOption == 0, 55, 30, SectionSize);
            WriteLine("Auto", _gearboxOption == 0, 35, 40, TextSize);
            WriteLine("Manual", _gearboxOption == 1, 0, 130, TextSize);

            WriteLine("Time of day", _selectedOption == 1, 55, 30, SectionSize);
            WriteLine("Midday", _timeOfDayOption == 0, 35, 40, TextSize);
            if (GameConfig.SelectedTrackDescription.AlternativeTimeOfDay != null)
                WriteLine(GameConfig.SelectedTrackDescription.AlternativeTimeOfDay,
                    _timeOfDayOption == 1, 0, 135, TextSize);

            WriteLine("Opponents", _selectedOption == 2, 45, 30, SectionSize);
            string opponents = GameConfig.CurrentRaceMode switch
            {
                RaceMode.TimeTrial => "None (solo)",
                RaceMode.HeadToHead => "One selected rival",
                RaceMode.Tournament => "7 rivals (class)",
                _ => GameConfig.UseOpponentPack ? "7 cars - The Pack" : "7 cars - same model"
            };
            WriteLine(opponents, _selectedOption == 2, 35, 40, TextSize);

            WriteLine("Circuit laps", _selectedOption == 3, 36, 30, SectionSize);
            WriteLine(GameConfig.SelectedTrackDescription.IsOpenRoad ? "One stage" :
                "<  " + GameConfig.CircuitLaps + " lap(s)  >", _selectedOption == 3, 30, 40, TextSize);
            WriteLine("GO!", _selectedOption == 4, 38, 30, SectionSize);
            WriteLine("Back: Esc / click here", Color.LightGray, 40, 30, TextSize);
            Engine.Instance.SpriteBatch.End();
        }
    }
}
