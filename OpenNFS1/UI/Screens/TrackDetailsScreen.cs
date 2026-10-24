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
    /// Track presentation from FRONTEND/ART/TRACKSEL/*SEL.QFS, shown when
    /// opening a course in Control Central. All layout art is genuine QFS.
    /// </summary>
    internal sealed class TrackDetailsScreen : IGameScreen
    {
        private readonly List<TrackDescription> _tracks;
        private readonly SpriteFont _font;
        private int _index, _photo;
        private bool _bestTimes;
        private BitmapChunk _art;
        private string _message;

        public TrackDetailsScreen(TrackDescription selected)
        {
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _tracks = TrackDescription.Descriptions.Where(t => !t.HideFromMenu &&
                (GameConfig.CurrentRaceMode != RaceMode.Tournament ||
                 RaceModeRules.TournamentClass(t) != 0)).ToList();
            _index = Math.Max(0, _tracks.FindIndex(t => t == selected));
            Load();
        }

        private static string TrackCode(TrackDescription track)
        {
            return track.Name switch
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

        private void Load()
        {
            _photo = 1;
            _bestTimes = false;
            _message = null;
            string code = TrackCode(_tracks[_index]);
            if (code == null)
            {
                _message = "No original track selection graphics for this course.";
                _art = null;
                return;
            }
            string filename = code + "SEL.QFS";
            string path = Path.Combine(GameConfig.CdDataPath,
                "FRONTEND", "ART", "TRACKSEL", filename);
            if (!File.Exists(path))
            {
                _message = "Original track graphics unavailable: " + filename;
                _art = null;
                return;
            }
            _art = new QfsFile(@"FRONTEND\ART\TRACKSEL\" + filename).Content.Header;
            GameConfig.SelectedTrackDescription = _tracks[_index];
        }

        private void NextTrack(int change)
        {
            if (_tracks.Count == 0) return;
            _index = (_index + change + _tracks.Count) % _tracks.Count;
            Load();
        }

        private void Back() => Engine.Instance.Screen = new HomeScreen();

        private BitmapEntry ArtEntry(string id) => _art?.FindByName(id);
        private void Art(string id)
        {
            BitmapEntry bmp = ArtEntry(id);
            if (bmp?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(bmp.Texture, bmp.GetDisplayAt(), Color.White);
        }

        private bool Hit(string name, Vector2 at)
        {
            var entry = ArtEntry(name);
            if (entry?.Texture == null || at.X < 0) return false;
            Vector2 position = entry.GetDisplayAt();
            return new Rectangle((int)position.X, (int)position.Y,
                entry.Texture.Width, entry.Texture.Height)
                .Contains((int)at.X, (int)at.Y);
        }

        private void ChangePhoto(int change) => _photo = (_photo + change + 2) % 3 + 1;

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
            if (input.WasPressed(Keys.A)) ChangePhoto(-1);
            if (input.WasPressed(Keys.D)) ChangePhoto(1);
            if (input.WasPressed(Keys.T))
                GameConfig.AlternativeTimeOfDay = !GameConfig.AlternativeTimeOfDay;

            if (!input.WasLeftMouseClicked) return;
            Vector2 p = input.GameMousePosition;
            if (Hit("dond", p) || Hit("donl", p)) { Back(); return; }
            if (Hit("pred", p)) { NextTrack(-1); return; }
            if (Hit("nexd", p)) { NextTrack(1); return; }
            if (Hit("timd", p) || Hit("timl", p))
            {
                GameConfig.AlternativeTimeOfDay = !GameConfig.AlternativeTimeOfDay;
                return;
            }
            if (Hit("besd", p) || Hit("besl", p))
            {
                _message = "Original best-time records are not connected yet.";
                return;
            }
            if (p.X >= 305 && p.X < 620 && p.Y >= 9 && p.Y < 433)
                ChangePhoto(1);
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            Art("bgnd");
            // Original track screenshots and side presentation artwork have
            // native sizes of 299x426 and 91x282 respectively.
            Art("bsg" + _photo);
            Art("ssg" + _photo);
            Art("timd");
            Art(GameConfig.AlternativeTimeOfDay ? "mord" : "midd");
            Art("pred"); Art("nexd"); Art("dond"); Art("besd");
            string hint = _message ?? "LEFT/RIGHT: TRACK   A/D: PHOTO   T: TIME   ENTER: DONE";
            sprite.DrawString(_font, hint, new Vector2(12, 420), Color.Yellow,
                0, Vector2.Zero, 0.27f, SpriteEffects.None, 0);
            sprite.End();
        }
    }
}
