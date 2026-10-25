using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public BitmapEntry OpponentBitmap { get; private set; }
        public VehicleDescription Descriptor { get; private set; }

        public VehicleUIControl(VehicleDescription desc)
        {
            Descriptor = desc;
            QfsFile qfs = new QfsFile(@"Frontend\Art\Control\" + desc.UIImageFile);
            Bitmap = qfs.Content.Header.FindByName("0000");

            // The CD has TWO images per car: *1 is the large PLAYER image
            // (309x166 at 21,114), *2 is the smaller OPPONENT image
            // (284x117 at 341,99). Never scale the player image into this panel.
            string source = desc.UIImageFile;
            string opponentFile = source.EndsWith("1.qfs", StringComparison.OrdinalIgnoreCase)
                ? source.Substring(0, source.Length - 5) + "2.qfs" : source;
            OpponentBitmap = new QfsFile(@"Frontend\Art\Control\" + opponentFile)
                .Content.Header.FindByName("0000");
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
        private static bool _assetIndexWritten;
        private const int VehicleSelected = 0;
        private const int OpponentSelected = 1;
        private const int RaceTypeSelected = 2;
        private const int TrackSelected = 3;
        private const int RaceButtonSelected = 4;
        private const int OptionsButtonSelected = 5;
        private const int CancelButtonSelected = 6;
        private const int SelectionCount = 7;

        // Original 640x480 coordinate space, not physical window pixels.
        private static readonly Rectangle VehicleArea = new Rectangle(20, 112, 310, 177);
        private static readonly Rectangle OpponentArea = new Rectangle(340, 98, 284, 126);
        private static readonly Rectangle RaceTypeArea = new Rectangle(91, 315, 243, 68);
        private static readonly Rectangle TrackArea = new Rectangle(338, 254, 211, 131);
        private static readonly Rectangle RaceButtonArea = new Rectangle(0, 437, 94, 43);
        private static readonly Rectangle OptionsButtonArea = new Rectangle(158, 438, 143, 42);
        private static readonly Rectangle CancelButtonArea = new Rectangle(514, 437, 126, 43);

        private readonly BitmapEntry _background, _vehicleSelection, _opponentSelection,
            _raceTypeSelection, _trackSelection, _raceButtonSelection;
        private readonly BitmapEntry _singleRaceArt;
        private readonly BitmapEntry _tournamentArt;
        private readonly BitmapEntry _headToHeadArt;
        private readonly BitmapEntry _noMercyArt;
        private readonly BitmapEntry _packArt;
        private readonly BitmapEntry _timeTrialArt, _timeTrialOpponentArt;
        private readonly BitmapEntry[] _tournamentClassArt = new BitmapEntry[3];
        private readonly BitmapEntry _computerOpponentsArt;
        private readonly BitmapEntry _optionsSelection, _cancelSelection;
        private readonly List<VehicleUIControl> _vehicles = new List<VehicleUIControl>();
        private readonly List<TrackUIControl> _tracks = new List<TrackUIControl>();
        private readonly SpriteFont _font;
        private int _currentVehicle = 2;
        private int _currentOpponent = 0;
        private int _currentTrack = 4;
        private int _selectedControl = RaceButtonSelected;

        public HomeScreen()
        {
            QfsFile qfs = new QfsFile(@"FRONTEND\ART\control\central.qfs");
            _background = qfs.Content.Header.FindByName("bgnd");
            _vehicleSelection = qfs.Content.Header.FindByName("Tlb1");
            _opponentSelection = qfs.Content.Header.FindByName("Trb3");
            _raceTypeSelection = qfs.Content.Header.FindByName("Blb2");
            _trackSelection = qfs.Content.Header.FindByName("Brb4");
            _raceButtonSelection = qfs.Content.Header.FindByName("Ra1l");
            _optionsSelection = qfs.Content.Header.FindByName("optl");
            _cancelSelection = qfs.Content.Header.FindByName("canl");
            // Original SINGLE.QFS is 238x66 at (92,316), exactly the
            // race type placeholder in CENTRAL.QFS.
            _singleRaceArt = new QfsFile(@"FRONTEND\ART\CONTROL\SINGLE.QFS")
                .Content.Header.FindByName("0000");
            _tournamentArt = new QfsFile(@"FRONTEND\ART\CONTROL\TOURNY.QFS")
                .Content.Header.FindByName("0000");
            _headToHeadArt = new QfsFile(@"FRONTEND\ART\CONTROL\HEADHEAD.QFS")
                .Content.Header.FindByName("0000");
            _noMercyArt = new QfsFile(@"FRONTEND\ART\CONTROL\NOMERCY.QFS")
                .Content.Header.FindByName("0000");
            _packArt = new QfsFile(@"FRONTEND\ART\CONTROL\SLEW.QFS")
                .Content.Header.FindByName("0000");
            // The ORIGINAL Time Trial artwork uses the WATCH1/WATCH2 names.
            _timeTrialArt = new QfsFile(@"FRONTEND\ART\CONTROL\WATCH1.QFS")
                .Content.Header.FindByName("0000");
            _timeTrialOpponentArt = new QfsFile(@"FRONTEND\ART\CONTROL\WATCH2.QFS")
                .Content.Header.FindByName("0000");
            for (int i = 0; i < 3; i++)
                _tournamentClassArt[i] =
                    new QfsFile(@"FRONTEND\ART\CONTROL\TOURNY" + (i + 1) + ".QFS")
                    .Content.Header.FindByName("0000");
            _computerOpponentsArt = qfs.Content.Header.FindByName("Coml");
            WriteArtInventory(qfs);

            foreach (var vehicle in VehicleDescription.Descriptions)
                _vehicles.Add(new VehicleUIControl(vehicle));
            foreach (var track in TrackDescription.Descriptions)
                if (!track.HideFromMenu)
                    _tracks.Add(new TrackUIControl(track));

            if (GameConfig.SelectedTrackDescription != null)
            {
                int index = _tracks.FindIndex(a => a.Descriptor == GameConfig.SelectedTrackDescription);
                if (index < 0 && GameConfig.SelectedTrackDescription.IsOpenRoad)
                {
                    string stageFile = GameConfig.SelectedTrackDescription.FileName.Replace('\\','/');
                    string family = stageFile.Substring(stageFile.LastIndexOf('/') + 1, 2);
                    index = _tracks.FindIndex(t => t.Descriptor.IsOpenRoad &&
                        t.Descriptor.FileName.Replace('\\','/').Split('/').Last()
                            .StartsWith(family, StringComparison.OrdinalIgnoreCase));
                }
                if (index >= 0) _currentTrack = index;
            }
            if (GameConfig.SelectedVehicle != null)
            {
                int index = _vehicles.FindIndex(a => a.Descriptor == GameConfig.SelectedVehicle);
                if (index >= 0) _currentVehicle = index;
            }
            if (GameConfig.SelectedOpponent != null)
            {
                int index = _vehicles.FindIndex(a => a.Descriptor == GameConfig.SelectedOpponent);
                if (index >= 0) _currentOpponent = index;
            }
            _currentVehicle = Math.Clamp(_currentVehicle, 0, _vehicles.Count - 1);
            _currentTrack = Math.Clamp(_currentTrack, 0, _tracks.Count - 1);
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            if (GameConfig.CurrentRaceMode == RaceMode.Tournament)
            {
                _currentTrack = FindTournamentVenue(_currentTrack, 1);
                EnsureTournamentVehicle(1);
            }
        }

        // Returning from track/vehicle details must preserve the existing
        // Control Central screen and every other selection.
        internal void RefreshFromGameConfig()
        {
            if (GameConfig.SelectedVehicle != null)
            {
                int car = _vehicles.FindIndex(v => v.Descriptor == GameConfig.SelectedVehicle);
                if (car >= 0) _currentVehicle = car;
            }
            if (GameConfig.SelectedTrackDescription != null)
            {
                TrackDescription chosen = GameConfig.SelectedTrackDescription;
                int track = _tracks.FindIndex(t => t.Descriptor == chosen ||
                    OpenRoadStageSelector.SameFamily(
                        t.Descriptor.FileName, t.Descriptor.IsOpenRoad,
                        chosen.FileName, chosen.IsOpenRoad));
                if (track >= 0) _currentTrack = track;
            }
        }

        /// <summary>
        /// Inventory the genuine QFS assets available in the user's own CD copy.
        /// This helps identify artwork for the missing Race Type/Opponent panels
        /// without inventing asset names or redistributing proprietary artwork.
        /// </summary>
        private static void WriteArtInventory(QfsFile control)
        {
            if (_assetIndexWritten) return;
            _assetIndexWritten = true;
            try
            {
                var lines = new List<string>
                {
                    "Original NFS SE control artwork inventory",
                    "Generated: " + DateTime.UtcNow.ToString("O"),
                    "CENTRAL.QFS bitmap resources:"
                };
                foreach (var bmp in control.Content.Header.Bitmaps)
                {
                    lines.Add(string.Format("{0} | {1} | {2}x{3} | at ({4},{5})",
                        bmp.Id, bmp.Type, bmp.Texture?.Width ?? 0, bmp.Texture?.Height ?? 0,
                        bmp.Misc[2], bmp.Misc[3]));
                }
                lines.Add("");
                lines.Add("Control QFS files in original CD data:");
                string directory = Path.Combine(GameConfig.CdDataPath, "FRONTEND", "ART", "CONTROL");
                if (Directory.Exists(directory))
                    lines.AddRange(Directory.GetFiles(directory, "*.qfs")
                        .Select(Path.GetFileName).OrderBy(name => name, StringComparer.OrdinalIgnoreCase));

                File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "ui-assets.txt"), lines);
            }
            catch (IOException) { /* Original artwork stays usable on read-only installations. */ }
            catch (UnauthorizedAccessException) { }
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

            if (input.WasPressed(Keys.I))
            {
                OpenShowroom();
                return;
            }

            if (input.WasPressed(Keys.Escape))
            {
                Engine.Instance.Screen = new GameModeScreen();
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
                if (_selectedControl == RaceButtonSelected) OpenRaceOptions();
                else if (_selectedControl == OptionsButtonSelected)
                    Engine.Instance.Screen = new OptionsScreen(this);
                else if (_selectedControl == CancelButtonSelected)
                    Engine.Instance.Screen = new GameModeScreen();
                else if (_selectedControl == VehicleSelected)
                    OpenShowroom();
                else if (_selectedControl == TrackSelected)
                    OpenTrackDetails();
                else ChangeSelected(1);
            }
        }

        private bool HandleMouseClick(int x, int y)
        {
            if (VehicleArea.Contains(x, y))
            {
                _selectedControl = VehicleSelected;
                // Native car photo is an information hotspot. Small strips at
                // each side still provide fast previous/next vehicle changes.
                if (x < VehicleArea.Left + 42) ChangeSelected(-1);
                else if (x >= VehicleArea.Right - 42) ChangeSelected(1);
                else OpenShowroom();
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
                // Center of the original track photo opens its native TRACKSEL
                // detail screen; narrow edge strips retain prev/next cycling.
                if (x < TrackArea.Left + 32) ChangeSelected(-1);
                else if (x >= TrackArea.Right - 32) ChangeSelected(1);
                else OpenTrackDetails();
            }
            else if (RaceButtonArea.Contains(x, y))
            {
                _selectedControl = RaceButtonSelected;
                OpenRaceOptions();
            }
            else if (OptionsButtonArea.Contains(x, y))
            {
                _selectedControl = OptionsButtonSelected;
                Engine.Instance.Screen = new OptionsScreen();
            }
            else if (CancelButtonArea.Contains(x, y))
            {
                _selectedControl = CancelButtonSelected;
                Engine.Instance.Screen = new GameModeScreen();
            }
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
                    EnsureTournamentVehicle(direction);
                    break;
                case OpponentSelected:
                    if (GameConfig.CurrentRaceMode == RaceMode.HeadToHead)
                        _currentOpponent = (_currentOpponent + direction + _vehicles.Count) % _vehicles.Count;
                    else if (GameConfig.CurrentRaceMode == RaceMode.SingleRace)
                    {
                        // The Pack is one option, followed by each car model.
                        int currentIndex = GameConfig.UseOpponentPack ? 0 : _currentOpponent + 1;
                        int nextIndex = (currentIndex + direction + _vehicles.Count + 1) % (_vehicles.Count + 1);
                        int current = nextIndex - 1;
                        GameConfig.UseOpponentPack = current < 0;
                        if (current >= 0) _currentOpponent = current;
                    }
                    break;
                case RaceTypeSelected:
                    GameConfig.CurrentRaceMode = (RaceMode)(
                        ((int)GameConfig.CurrentRaceMode + direction + 4) % 4);
                    GameConfig.NoMercy = GameConfig.CurrentRaceMode == RaceMode.HeadToHead &&
                        Engine.Instance.Input.IsKeyDown(Keys.End);
                    // Six original A/B/C venues, including open-road courses.
                    if (GameConfig.CurrentRaceMode == RaceMode.Tournament)
                    {
                        _currentTrack = FindTournamentVenue(_currentTrack, 1);
                        EnsureTournamentVehicle(1);
                    }
                    break;
                case TrackSelected:
                    _currentTrack = (_currentTrack + direction + _tracks.Count) % _tracks.Count;
                    if (GameConfig.CurrentRaceMode == RaceMode.Tournament)
                    {
                        _currentTrack = FindTournamentVenue(_currentTrack, direction);
                        EnsureTournamentVehicle(1);
                    }
                    break;
            }
        }

        private int FindTournamentVenue(int start, int direction)
        {
            for (int offset = 0; offset < _tracks.Count; offset++)
            {
                int index = (start + offset * direction + _tracks.Count * 2) % _tracks.Count;
                if (RaceModeRules.TournamentClass(_tracks[index].Descriptor) > 0)
                    return index;
            }
            return start;
        }

        private void EnsureTournamentVehicle(int direction)
        {
            if (GameConfig.CurrentRaceMode != RaceMode.Tournament) return;
            TrackDescription track = _tracks[_currentTrack].Descriptor;
            if (RaceModeRules.IsVehicleAllowed(RaceMode.Tournament, track,
                _vehicles[_currentVehicle].Descriptor)) return;

            for (int n = 1; n <= _vehicles.Count; n++)
            {
                int index = (_currentVehicle + n * direction + _vehicles.Count * 2) % _vehicles.Count;
                if (RaceModeRules.IsVehicleAllowed(RaceMode.Tournament, track,
                    _vehicles[index].Descriptor))
                {
                    _currentVehicle = index;
                    return;
                }
            }
        }

        private void OpenShowroom()
        {
            GameConfig.SelectedVehicle = _vehicles[_currentVehicle].Descriptor;
            Engine.Instance.Screen = new VehicleShowroomScreen(GameConfig.SelectedVehicle, this);
        }

        private void OpenTrackDetails()
        {
            // Preserve AL2/AL3 (CL/CY) when reopening the details of the
            // SAME selected road. Switching to another road selects its first
            // segment instead of retaining an unrelated prior road.
            TrackDescription course = _tracks[_currentTrack].Descriptor;
            TrackDescription selected = GameConfig.SelectedTrackDescription;
            if (selected == null || !OpenRoadStageSelector.SameFamily(
                    course.FileName, course.IsOpenRoad,
                    selected.FileName, selected.IsOpenRoad))
                GameConfig.SelectedTrackDescription = course;
            Engine.Instance.Screen = new TrackDetailsScreen(
                GameConfig.SelectedTrackDescription, this);
        }

        private void OpenRaceOptions()
        {
            GameConfig.SelectedVehicle = _vehicles[_currentVehicle].Descriptor;
            GameConfig.SelectedOpponent = _vehicles[_currentOpponent].Descriptor;
            // The Control Central thumbnail represents a COURSE, while
            // TrackDetails may have selected its AL2/AL3 (CL/CY) segment.
            // Do not overwrite that stage when the user presses RACE.
            TrackDescription selected = _tracks[_currentTrack].Descriptor;
            TrackDescription detailed = GameConfig.SelectedTrackDescription;
            if (detailed == null || !OpenRoadStageSelector.SameFamily(
                selected.FileName, selected.IsOpenRoad,
                detailed.FileName, detailed.IsOpenRoad))
                GameConfig.SelectedTrackDescription = selected;
            RaceLauncher.Start();
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

            // The opponent panel is mode-dependent. The original *2.QFS
            // image stays at its native 284x117 position without rescaling.
            if (GameConfig.CurrentRaceMode == RaceMode.HeadToHead ||
                (GameConfig.CurrentRaceMode == RaceMode.SingleRace && !GameConfig.UseOpponentPack))
            {
                BitmapEntry opponent = _vehicles[_currentOpponent].OpponentBitmap;
                if (opponent?.Texture != null)
                    sprite.Draw(opponent.Texture, opponent.GetDisplayAt(), Color.White);
            }
            else if (GameConfig.CurrentRaceMode == RaceMode.SingleRace && GameConfig.UseOpponentPack)
            {
                // SLEW.QFS is EA's ORIGINAL 284x117 "THE PACK" opponent
                // photograph, including its caption and native coordinates.
                if (_packArt?.Texture != null)
                    sprite.Draw(_packArt.Texture, _packArt.GetDisplayAt(), Color.White);
            }
            else if (GameConfig.CurrentRaceMode == RaceMode.Tournament)
            {
                int carClass = RaceModeRules.TournamentClass(_tracks[_currentTrack].Descriptor);
                // Original EA artwork: TOURNY1=A, TOURNY2=B, TOURNY3=C.
                if (carClass >= 1 && carClass <= 3)
                {
                    BitmapEntry original = _tournamentClassArt[carClass - 1];
                    if (original?.Texture != null)
                        sprite.Draw(original.Texture, original.GetDisplayAt(), Color.White);
                }
            }
            else if (_timeTrialOpponentArt?.Texture != null)
            {
                sprite.Draw(_timeTrialOpponentArt.Texture,
                    _timeTrialOpponentArt.GetDisplayAt(), Color.White);
            }
            if (GameConfig.CurrentRaceMode != RaceMode.TimeTrial && _computerOpponentsArt?.Texture != null)
                sprite.Draw(_computerOpponentsArt.Texture, _computerOpponentsArt.GetDisplayAt(), Color.White);

            BitmapEntry raceTypeArt = GameConfig.CurrentRaceMode switch
            {
                RaceMode.SingleRace => _singleRaceArt,
                RaceMode.Tournament => _tournamentArt,
                RaceMode.HeadToHead => GameConfig.NoMercy ? _noMercyArt : _headToHeadArt,
                RaceMode.TimeTrial => _timeTrialArt,
                _ => null
            };
            if (raceTypeArt?.Texture != null)
                sprite.Draw(raceTypeArt.Texture, raceTypeArt.GetDisplayAt(), Color.White);

            switch (_selectedControl)
            {
                case VehicleSelected:
                    sprite.Draw(_vehicleSelection.Texture, _vehicleSelection.GetDisplayAt(), Color.White);
                    break;
                case TrackSelected:
                    sprite.Draw(_trackSelection.Texture, _trackSelection.GetDisplayAt(), Color.White);
                    break;
                case OpponentSelected:
                    sprite.Draw(_opponentSelection.Texture, _opponentSelection.GetDisplayAt(), Color.White);
                    break;
                case RaceTypeSelected:
                    sprite.Draw(_raceTypeSelection.Texture, _raceTypeSelection.GetDisplayAt(), Color.White);
                    break;
                case RaceButtonSelected:
                    sprite.Draw(_raceButtonSelection.Texture, _raceButtonSelection.GetDisplayAt(), Color.White);
                    break;
                case OptionsButtonSelected:
                    if (_optionsSelection?.Texture != null)
                        sprite.Draw(_optionsSelection.Texture, _optionsSelection.GetDisplayAt(), Color.White);
                    break;
                case CancelButtonSelected:
                    if (_cancelSelection?.Texture != null)
                        sprite.Draw(_cancelSelection.Texture, _cancelSelection.GetDisplayAt(), Color.White);
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
