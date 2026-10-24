using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameEngine;
using OpenNFS1.Parsers;
using OpenNFS1.Vehicles;

namespace OpenNFS1.UI.Screens
{
    class VehicleUIControl
    {
        public BitmapEntry Bitmap { get; private set; }
        public VehicleDescription Descriptor { get; private set; }

        public VehicleUIControl(VehicleDescription desc)
        {
            Descriptor = desc;
            QfsFile qfs = new QfsFile(@"Frontend\Art\Control\" + desc.UIImageFile);
            Bitmap = qfs.Content.Header.Bitmaps.Find(a => a.Id == "0000");
        }
    }

    class TrackUIControl
    {
        public BitmapEntry Bitmap { get; private set; }
        public TrackDescription Descriptor { get; private set; }

        public TrackUIControl(TrackDescription desc)
        {
            Descriptor = desc;
            QfsFile qfs = new QfsFile(@"Frontend\Art\Control\" + desc.ImageFile);
            Bitmap = qfs.Content.Header.Bitmaps.Find(a => a.Id == "0000");
        }
    }

    class HomeScreen : IGameScreen
    {
        private const int VehicleSelected = 0;
        private const int OpponentSelected = 1;
        private const int RaceTypeSelected = 2;
        private const int TrackSelected = 3;
        private const int RaceButtonSelected = 4;
        private const int SelectionCount = 5;

        // Original 640x480 coordinate space, not physical window pixels.
        private static readonly Rectangle VehicleArea = new Rectangle(20, 112, 310, 177);
        private static readonly Rectangle OpponentArea = new Rectangle(340, 98, 284, 126);
        private static readonly Rectangle RaceTypeArea = new Rectangle(91, 315, 243, 68);
        private static readonly Rectangle TrackArea = new Rectangle(338, 254, 211, 131);
        private static readonly Rectangle RaceButtonArea = new Rectangle(0, 437, 94, 43);
        private static readonly Rectangle OptionsButtonArea = new Rectangle(158, 438, 143, 42);
        private static readonly Rectangle CancelButtonArea = new Rectangle(514, 437, 126, 43);

        private readonly BitmapEntry _background, _vehicleSelection, _trackSelection, _raceButtonSelection;
        private readonly List<VehicleUIControl> _vehicles = new List<VehicleUIControl>();
        private readonly List<TrackUIControl> _tracks = new List<TrackUIControl>();
        private readonly SpriteFont _font;
        private int _currentVehicle = 2;
        private int _currentTrack = 4;
        private int _selectedControl = RaceButtonSelected;

        public HomeScreen()
        {
            QfsFile qfs = new QfsFile(@"FRONTEND\ART\control\central.qfs");
            _background = qfs.Content.Header.FindByName("bgnd");
            _vehicleSelection = qfs.Content.Header.FindByName("Tlb1");
            _trackSelection = qfs.Content.Header.FindByName("Brb4");
            _raceButtonSelection = qfs.Content.Header.FindByName("Ra1l");

            foreach (var vehicle in VehicleDescription.Descriptions)
                _vehicles.Add(new VehicleUIControl(vehicle));
            foreach (var track in TrackDescription.Descriptions)
                if (!track.HideFromMenu)
                    _tracks.Add(new TrackUIControl(track));

            if (GameConfig.SelectedTrackDescription != null)
            {
                int index = _tracks.FindIndex(a => a.Descriptor == GameConfig.SelectedTrackDescription);
                if (index >= 0) _currentTrack = index;
            }
            if (GameConfig.SelectedVehicle != null)
            {
                int index = _vehicles.FindIndex(a => a.Descriptor == GameConfig.SelectedVehicle);
                if (index >= 0) _currentVehicle = index;
            }
            _currentVehicle = Math.Clamp(_currentVehicle, 0, _vehicles.Count - 1);
            _currentTrack = Math.Clamp(_currentTrack, 0, _tracks.Count - 1);
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
        }

        public void Update(GameTime gameTime)
        {
            var input = Engine.Instance.Input;
            if (input.WasLeftMouseClicked)
            {
                var position = input.GameMousePosition;
                if (position.X >= 0 && HandleMouseClick((int)position.X, (int)position.Y))
                    return;
            }

            if (input.WasPressed(Keys.Escape))
            {
                Engine.Instance.Game.Exit();
                return;
            }
            if (input.WasPressed(Keys.Down) || input.WasPressed(Keys.Tab))
                _selectedControl = (_selectedControl + 1) % SelectionCount;
            else if (input.WasPressed(Keys.Up))
                _selectedControl = (_selectedControl + SelectionCount - 1) % SelectionCount;
            else if (input.WasPressed(Keys.Left))
                ChangeSelected(-1);
            else if (input.WasPressed(Keys.Right))
                ChangeSelected(1);
            else if (input.WasPressed(Keys.Enter))
            {
                if (_selectedControl == RaceButtonSelected)
                    OpenRaceOptions();
                else
                    ChangeSelected(1);
            }
        }

        private bool HandleMouseClick(int x, int y)
        {
            if (VehicleArea.Contains(x, y))
            {
                _selectedControl = VehicleSelected;
                ChangeSelected(x < VehicleArea.Center.X ? -1 : 1);
            }
            else if (OpponentArea.Contains(x, y))
            {
                _selectedControl = OpponentSelected;
                ChangeSelected(x < OpponentArea.Center.X ? -1 : 1);
            }
            else if (RaceTypeArea.Contains(x, y))
            {
                _selectedControl = RaceTypeSelected;
                ChangeSelected(1);
            }
            else if (TrackArea.Contains(x, y))
            {
                _selectedControl = TrackSelected;
                ChangeSelected(x < TrackArea.Center.X ? -1 : 1);
            }
            else if (RaceButtonArea.Contains(x, y) || OptionsButtonArea.Contains(x, y))
            {
                _selectedControl = RaceButtonSelected;
                OpenRaceOptions();
            }
            else if (CancelButtonArea.Contains(x, y))
                Engine.Instance.Game.Exit();
            else
                return false;
            return true;
        }

        private void ChangeSelected(int direction)
        {
            switch (_selectedControl)
            {
                case VehicleSelected:
                    _currentVehicle = (_currentVehicle + direction + _vehicles.Count) % _vehicles.Count;
                    break;
                case OpponentSelected:
                    GameConfig.OpponentCount = Math.Clamp(GameConfig.OpponentCount + direction, 0, 10);
                    break;
                case RaceTypeSelected:
                    GameConfig.TimeTrial = !GameConfig.TimeTrial;
                    break;
                case TrackSelected:
                    _currentTrack = (_currentTrack + direction + _tracks.Count) % _tracks.Count;
                    break;
            }
        }

        private void OpenRaceOptions()
        {
            GameConfig.SelectedVehicle = _vehicles[_currentVehicle].Descriptor;
            GameConfig.SelectedTrackDescription = _tracks[_currentTrack].Descriptor;
            Engine.Instance.Screen = new RaceOptionsScreen();
        }

        public void Draw()
        {
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            sprite.Draw(_background.Texture, Vector2.Zero, Color.White);
            sprite.Draw(_vehicles[_currentVehicle].Bitmap.Texture,
                _vehicles[_currentVehicle].Bitmap.GetDisplayAt(), Color.White);
            sprite.Draw(_tracks[_currentTrack].Bitmap.Texture,
                _tracks[_currentTrack].Bitmap.GetDisplayAt(), Color.White);

            // Fill in the two originally blank menu controls with real settings.
            DrawLabel("OPPONENTS", new Vector2(356, 119), Color.Black, 0.54f);
            DrawLabel(GameConfig.TimeTrial ? "SOLO (TIME TRIAL)" :
                "<   " + GameConfig.OpponentCount + " AI CARS   >",
                new Vector2(354, 152), Color.Black, 0.49f);
            DrawLabel(GameConfig.TimeTrial ? "TIME TRIAL" : "SINGLE RACE",
                new Vector2(105, 337), Color.Black, 0.55f);

            switch (_selectedControl)
            {
                case VehicleSelected:
                    sprite.Draw(_vehicleSelection.Texture, _vehicleSelection.GetDisplayAt(), Color.White);
                    break;
                case TrackSelected:
                    sprite.Draw(_trackSelection.Texture, _trackSelection.GetDisplayAt(), Color.White);
                    break;
                case OpponentSelected:
                    DrawLabel("SELECTED: OPPONENTS", new Vector2(347, 194), Color.DarkRed, 0.31f);
                    break;
                case RaceTypeSelected:
                    DrawLabel("SELECTED", new Vector2(103, 365), Color.DarkRed, 0.30f);
                    break;
                case RaceButtonSelected:
                    sprite.Draw(_raceButtonSelection.Texture, _raceButtonSelection.GetDisplayAt(), Color.White);
                    break;
            }
            sprite.End();
        }

        private void DrawLabel(string text, Vector2 position, Color color, float scale)
        {
            Engine.Instance.SpriteBatch.DrawString(_font, text, position, color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
