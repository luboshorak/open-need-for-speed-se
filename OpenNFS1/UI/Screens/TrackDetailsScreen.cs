using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Original track presentation from TRACKSEL/*SEL.QFS. Circuit maps,
    /// captions and buttons are original EA artwork; lap/time/length values
    /// are laid into the empty native text slots, not into a debug footer.
    /// </summary>
    internal sealed class TrackDetailsScreen : IGameScreen
    {
        private readonly List<TrackDescription> _tracks;
        private readonly IGameScreen _returnScreen;
        private readonly SpriteFont _font;
        private int _index, _photo = 1;
        private readonly List<TrackDescription> _segments = new List<TrackDescription>();
        private BitmapChunk _art;

        private static string StageFamily(TrackDescription track) =>
            OpenRoadStageSelector.Family(track?.FileName, track?.IsOpenRoad ?? false);

        private void SetSegment(int stageIndex)
        {
            if (_segments.Count == 0) return;
            _photo = (stageIndex + _segments.Count) % _segments.Count + 1;
            GameConfig.SelectedTrackDescription = _segments[_photo - 1];
        }

        private void ChangeSegment(int delta)
        {
            if (_segments.Count > 1)
                SetSegment((_photo - 1 + delta + _segments.Count) % _segments.Count);
        }

        internal static string TrackCode(TrackDescription track)
        {
            return track?.Name switch
            {
                "Alpine" => "ALP",
                "Coastal" => "COAST",
                "City" => "CITY",
                "Rusty Springs" => "RUST",
                "Autumn Valley" => "AUT",
                "Vertigo Ridge" => "VERT",
                "Burnt Sienna" => "BRUN",
                "Transpolis" => "TROP",
                "Lost Vegas" => "LOST",
                _ => null
            };
        }

        public TrackDetailsScreen(TrackDescription selected, IGameScreen returnScreen = null)
        {
            _returnScreen = returnScreen;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _tracks = TrackDescription.Descriptions.Where(t =>
                !t.HideFromMenu && TrackCode(t) != null &&
                (GameConfig.CurrentRaceMode != RaceMode.Tournament ||
                 RaceModeRules.TournamentClass(t) != 0)).ToList();
            if (_tracks.Count == 0)
                throw new InvalidOperationException("No original track selection screens are available.");
            // AL2/AL3, CL2/CL3, CY2/CY3 are intentionally hidden from
            // the top-level track list. Map the selected segment back to
            // its visible COURSE rather than silently opening Alpine.
            _index = Math.Max(0, _tracks.FindIndex(t => t == selected ||
                OpenRoadStageSelector.SameFamily(
                    t.FileName, t.IsOpenRoad,
                    selected?.FileName, selected?.IsOpenRoad ?? false)));
            Load();
        }

        private void Load()
        {
            _segments.Clear();
            string family = StageFamily(_tracks[_index]);
            if (family != null)
                _segments.AddRange(TrackDescription.Descriptions.Where(t =>
                    StageFamily(t) == family).OrderBy(t => t.FileName));
            _photo = 1;
            string fileName = TrackCode(_tracks[_index]) + "SEL.QFS";
            string location = Path.Combine(GameConfig.CdDataPath,
                "FRONTEND", "ART", "TRACKSEL", fileName);
            if (!File.Exists(location))
                throw new FileNotFoundException("Original track screen is missing.", location);
            _art = new QfsFile(@"FRONTEND\ART\TRACKSEL\" + fileName).Content.Header;
            if (_segments.Count > 0)
            {
                int current = _segments.FindIndex(t =>
                    t == GameConfig.SelectedTrackDescription);
                SetSegment(Math.Max(0, current));
            }
            else
                GameConfig.SelectedTrackDescription = _tracks[_index];
        }

        internal TrackDescription CycleTrack(int direction)
        {
            _index = (_index + direction + _tracks.Count) % _tracks.Count;
            Load();
            return _tracks[_index];
        }

        private void NextTrack(int direction) => CycleTrack(direction);

        private void Back()
        {
            if (_returnScreen is HomeScreen home) home.RefreshFromGameConfig();
            Engine.Instance.Screen = _returnScreen ?? new HomeScreen();
        }
        private BitmapEntry Entry(string id) => _art?.FindByName(id);
        private void Art(string id)
        {
            var item = Entry(id);
            if (item?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(item.Texture, item.GetDisplayAt(), Color.White);
        }

        private bool Hit(string name, Vector2 point)
        {
            var item = Entry(name);
            if (item?.Texture == null || point.X < 0) return false;
            var position = item.GetDisplayAt();
            return new Rectangle((int)position.X, (int)position.Y,
                item.Texture.Width, item.Texture.Height)
                .Contains((int)point.X, (int)point.Y);
        }

        private void BestTimes() =>
            Engine.Instance.Screen = new TrackBestTimesScreen(_tracks[_index], this);

        private void ChangePhoto(int direction) => ChangeSegment(direction);

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape) || input.WasPressed(Keys.Enter))
            {
                Back();
                return;
            }
            if (input.WasPressed(Keys.Left)) { NextTrack(-1); return; }
            if (input.WasPressed(Keys.Right)) { NextTrack(1); return; }
            if (input.WasPressed(Keys.A) || input.WasPressed(Keys.Q) || input.WasPressed(Keys.PageUp))
                ChangeSegment(-1);
            if (input.WasPressed(Keys.D) || input.WasPressed(Keys.E) || input.WasPressed(Keys.PageDown))
                ChangeSegment(1);
            if (_segments.Count > 0)
            {
                if (input.WasPressed(Keys.D1) || input.WasPressed(Keys.NumPad1)) SetSegment(0);
                if (input.WasPressed(Keys.D2) || input.WasPressed(Keys.NumPad2)) SetSegment(1);
                if (input.WasPressed(Keys.D3) || input.WasPressed(Keys.NumPad3)) SetSegment(2);
            }
            if (input.WasPressed(Keys.B)) { BestTimes(); return; }
            if (input.WasPressed(Keys.T))
                GameConfig.AlternativeTimeOfDay = !GameConfig.AlternativeTimeOfDay;
            if (input.WasPressed(Keys.L) && !_tracks[_index].IsOpenRoad)
                GameConfig.CircuitLaps = GameConfig.CircuitLaps % 12 + 1;

            if (!input.WasLeftMouseClicked) return;
            Vector2 p = input.GameMousePosition;
            if (Hit("dond", p) || Hit("donl", p)) { Back(); return; }
            if (Hit("pred", p) || Hit("prel", p)) { NextTrack(-1); return; }
            if (Hit("nexd", p) || Hit("nexl", p)) { NextTrack(1); return; }
            if (Hit("besd", p) || Hit("besl", p)) { BestTimes(); return; }
            // Midday/Morning are alternate sprites at the SAME coordinates.
            // Their original hitboxes overlap, so a click must toggle the
            // current value rather than always selecting the first ID.
            if (Hit("midd", p) || Hit("midl", p) ||
                Hit("mord", p) || Hit("morl", p))
            {
                GameConfig.AlternativeTimeOfDay = !GameConfig.AlternativeTimeOfDay;
                return;
            }
            // The original bsg1/2/3 photo and ssg1/2/3 small map are
            // actual different stage images, not synthesized backgrounds.
            // Make their original image regions clickable. The left half
            // selects the previous segment, the right half the next.
            // Bottom pred/nex buttons continue to choose a DIFFERENT road.
            if (_segments.Count > 1)
            {
                BitmapEntry photo = Entry("bsg" + _photo);
                BitmapEntry map = Entry("ssg" + _photo);
                foreach (var art in new[] { photo, map })
                {
                    if (art?.Texture == null) continue;
                    Vector2 origin = art.GetDisplayAt();
                    Rectangle bounds = new Rectangle((int)origin.X, (int)origin.Y,
                        art.Texture.Width, art.Texture.Height);
                    if (bounds.Contains((int)p.X, (int)p.Y))
                    {
                        ChangeSegment(p.X < origin.X + art.Texture.Width / 2f ? -1 : 1);
                        return;
                    }
                }
            }
            // Native track selection uses a small number of click targets.
            // No arbitrary "click on the map changes photos" behavior.
            if (!_tracks[_index].IsOpenRoad &&
                p.X >= 190 && p.X <= 245 && p.Y >= 362 && p.Y < 392)
                GameConfig.CircuitLaps = GameConfig.CircuitLaps % 12 + 1;
        }

        private void Value(string text, int x, int y, float scale = .40f)
        {
            Engine.Instance.SpriteBatch.DrawString(_font, text,
                new Vector2(x, y), Color.White, 0, Vector2.Zero, scale,
                SpriteEffects.None, 0);
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            Art("bgnd");

            bool openRoad = _tracks[_index].IsOpenRoad;
            if (openRoad)
            {
                Art("bsg" + _photo);
                Art("ssg" + _photo);
                // Stage selection now changes the actual AL/CL/CY TRI file,
                // not only the illustration. Keep feedback in the data field.
                if (_segments.Count > 1)
                {
                    Value("<", 335, 350, .60f);
                    Value("SEGMENT " + _photo + " / " + _segments.Count, 385, 350, .35f);
                    Value(">", 545, 350, .60f);
                    Value("Q / E OR 1-3", 375, 377, .26f);
                }
            }
            else
            {
                // Circuit maps are baked into bgnd, not in bsg/ssg sprites.
                Art("lapd");
                Value(GameConfig.CircuitLaps.ToString(), 205, 362);
                // Only display measured/confirmed original miles. Vertigo
                // Ridge is 3.0 miles as confirmed by the user's DOS screenshot.
                if (_tracks[_index].Name == "Vertigo Ridge")
                    Value("3.0 MILES", 201, 339);
            }

            Art("timd");
            Art(GameConfig.AlternativeTimeOfDay ? "morl" : "midl");
            Art("pred"); Art("nexd"); Art("dond"); Art("besd");
            sprite.End();
        }
    }
}
