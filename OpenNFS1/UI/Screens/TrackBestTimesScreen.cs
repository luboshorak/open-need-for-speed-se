using System;
using System.Linq;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Authentic per-track *BST.QFS background (32FB Huffman) with player
    /// race records actually collected by this remake. No fabricated scores.
    /// </summary>
    internal sealed class TrackBestTimesScreen : IGameScreen
    {
        private readonly TrackDescription _track;
        private readonly IGameScreen _back;
        private readonly BitmapChunk _art;
        private readonly SpriteFont _font;

        public TrackBestTimesScreen(TrackDescription track, IGameScreen back)
        {
            _track = track;
            _back = back;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            string code = TrackDetailsScreen.TrackCode(track);
            if (code == null)
                throw new InvalidOperationException("No original best-times artwork for this track.");
            _art = new QfsFile(@"FRONTEND\ART\TRACKSEL\" + code + "BST.QFS")
                .Content.Header;
        }

        private BitmapEntry ArtEntry(string id) => _art.FindByName(id);
        private void Art(string id)
        {
            BitmapEntry entry = ArtEntry(id);
            if (entry?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(entry.Texture, entry.GetDisplayAt(), Color.White);
        }

        private bool Hit(string id, Vector2 point)
        {
            BitmapEntry entry = ArtEntry(id);
            if (entry?.Texture == null || point.X < 0) return false;
            Vector2 p = entry.GetDisplayAt();
            return new Rectangle((int)p.X, (int)p.Y,
                entry.Texture.Width, entry.Texture.Height)
                .Contains((int)point.X, (int)point.Y);
        }

        private void Cycle(int direction)
        {
            if (_back is TrackDetailsScreen details)
                Engine.Instance.Screen =
                    new TrackBestTimesScreen(details.CycleTrack(direction), details);
            else
                Engine.Instance.Screen = _back;
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape) || input.WasPressed(Keys.Enter))
            {
                Engine.Instance.Screen = _back;
                return;
            }
            if (input.WasPressed(Keys.Left)) { Cycle(-1); return; }
            if (input.WasPressed(Keys.Right)) { Cycle(1); return; }
            if (input.WasLeftMouseClicked)
            {
                Vector2 p = input.GameMousePosition;
                if (Hit("dond", p) || Hit("donl", p) ||
                    Hit("desd", p) || Hit("desl", p))
                    Engine.Instance.Screen = _back;
                else if (Hit("lard", p) || Hit("larl", p))
                    Cycle(-1);
                else if (Hit("rard", p) || Hit("rarl", p))
                    Cycle(1);
            }
        }

        private void Text(SpriteBatch s, string value, int x, int y,
            float scale, Color color)
        {
            s.DrawString(_font, value, new Vector2(x, y), color, 0,
                Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            Art("bgnd");

            var records = TrackRecords.Get(_track.FileName);
            if (records.Count == 0)
            {
                Text(sprite, "NO RECORDS YET", 15, 145, .51f, Color.Yellow);
                Text(sprite, "FINISH A RACE TO APPEAR HERE", 15, 173, .32f, Color.White);
            }
            else
            {
                for (int i = 0; i < records.Count && i < 10; i++)
                {
                    var result = records[i];
                    int y = 144 + i * 22;
                    Text(sprite, result.Player, 10, y, .40f, Color.Yellow);
                    var elapsed = TimeSpan.FromMilliseconds(result.Milliseconds);
                    string clock = ((int)elapsed.TotalMinutes).ToString("00") +
                        ":" + elapsed.Seconds.ToString("00") +
                        "." + (elapsed.Milliseconds / 100);
                    Text(sprite, clock, 282, y, .42f, Color.Yellow);
                    string car = result.Car.Length > 12
                        ? result.Car.Substring(0, 12) : result.Car;
                    Text(sprite, car, 475, y, .36f, Color.Yellow);
                }
            }

            var fastest = TrackRecords.FastestSpeed(_track.FileName);
            if (fastest != null && fastest.TopSpeed > 0)
            {
                Text(sprite, fastest.Player, 10, 376, .38f, Color.Yellow);
                Text(sprite, fastest.TopSpeed.ToString("0.0") + " KPH", 282, 376, .38f, Color.Yellow);
                Text(sprite, fastest.Car, 475, 376, .33f, Color.Yellow);
            }

            Art("dond");
            Art("lard");
            Art("rard");
            Art("desd");
            sprite.End();
        }
    }
}
