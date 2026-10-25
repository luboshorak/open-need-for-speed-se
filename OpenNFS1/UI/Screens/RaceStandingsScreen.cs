using System;
using System.Linq;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;
using OpenNFS1.Physics;
using OpenNFS1.Vehicles.AI;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Two distinct original 1996 results screens: STANDSEG/STANDTRK
    /// (personal result) followed by SGMTSTND/FINSTND (all competitors).
    /// The original images ALREADY contain titles, captions and column
    /// headers. Only variable numeric/text fields may be rendered over them.
    /// </summary>
    internal sealed class RaceStandingsScreen : IGameScreen
    {
        private readonly Race _race;
        private readonly TimeSpan _finishedTime;
        private readonly IGameScreen _back;
        private readonly SpriteFont _font;
        private readonly BitmapChunk _personalArt;
        private readonly BitmapChunk _standingsArt;
        private int _page;

        public RaceStandingsScreen(Race race, TimeSpan finishedTime, IGameScreen back,
            bool showStandingsFirst = false)
        {
            _race = race;
            _finishedTime = finishedTime;
            _back = back;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _personalArt = new QfsFile(@"FRONTEND\ART\STATS\" +
                (race.Track.IsOpenRoad ? "STANDSEG.QFS" : "STANDTRK.QFS")).Content.Header;
            _standingsArt = new QfsFile(@"FRONTEND\ART\STATS\" +
                (race.Track.IsOpenRoad ? "SGMTSTND.QFS" : "FINSTND.QFS")).Content.Header;
            _page = showStandingsFirst ? 1 : 0;
        }

        private static void DrawArt(BitmapChunk art, string id)
        {
            var image = art.FindByName(id);
            if (image?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(image.Texture, image.GetDisplayAt(), Color.White);
        }

        private void Value(string text, int x, int y, float scale = .37f)
        {
            Engine.Instance.SpriteBatch.DrawString(_font, text,
                new Vector2(x, y), Color.Yellow, 0f, Vector2.Zero,
                scale, SpriteEffects.None, 0f);
        }

        private static string Clock(TimeSpan value)
        {
            if (value < TimeSpan.Zero) value = TimeSpan.Zero;
            return ((int)value.TotalMinutes) + ":" + value.Seconds.ToString("00") +
                "." + (value.Milliseconds / 100);
        }

        private void DrawPersonal()
        {
            DrawArt(_personalArt, "bgnd");

            string track = GameConfig.SelectedTrackDescription?.Name ?? "";
            string courseArt = track.ToLowerInvariant() switch
            {
                "coastal" => "coas", "alpine" => "alpi", "city" => "city",
                _ => null
            };
            // The original course-specific 562x31 bitmap includes its own
            // text. Print only names lacking the source bitmap.
            if (courseArt != null && _personalArt.FindByName(courseArt)?.Texture != null)
                DrawArt(_personalArt, courseArt);
            else if (!string.IsNullOrEmpty(track))
                Value(track.ToUpperInvariant(), 8, 61, .65f);

            // Captions CURRENT STANDING / SEGMENT TIME / TOTAL TRACK TIME /
            // AVERAGE SPEED / TOP SPEED and the two RECORD rows are printed
            // into STANDSEG/STANDTRK bgnd. Never write them a second time.
            Value((_race.PlayerStats.Position + 1).ToString(), 307, 153, .48f);
            Value(Clock(_finishedTime), 307, 176, .48f);
            Value(Clock(_finishedTime), 307, 196, .48f);
            Value(_race.AveragePlayerSpeed.ToString("0.0") + " MPH", 307, 227, .42f);
            Value(_race.PeakPlayerSpeed.ToString("0.0") + " MPH", 307, 250, .42f);

            // Recorded historical bests must come from the record database,
            // not arbitrary invented times or names. Empty original record
            // fields are preferable to made-up benchmark holders.
            DrawArt(_personalArt, "dond");
        }

        private void DrawStandings()
        {
            DrawArt(_standingsArt, "bgnd");
            string track = GameConfig.SelectedTrackDescription?.Name ?? "";
            string trackArt = track.ToLowerInvariant() switch
            {
                "coastal" => "coas", "alpine" => "alpi", "city" => "city",
                "autumn valley" => "autu", "burnt sienna" => "burn",
                "rusty springs" => "rust", "transpolis" => "trop",
                _ => null
            };
            if (trackArt != null && _standingsArt.FindByName(trackArt)?.Texture != null)
                DrawArt(_standingsArt, trackArt);
            else if (!string.IsNullOrEmpty(track))
                Value(track.ToUpperInvariant(), 8, 61, .65f);

            // The SGMTSTND / FINSTND source background already has
            // RANK, NAME, TOTAL TIME and SEGMENT TIME headings.
            var drivers = _race.Drivers
                .Where(d => d is PlayerDriver || d is RacingAIDriver)
                .OrderBy(d => _race.RacerFinishTimes.ContainsKey(d) ? 0 : 1)
                .ThenBy(d => _race.RacerFinishTimes.TryGetValue(d, out var finish)
                    ? finish.TotalSeconds : double.MaxValue)
                .ThenByDescending(d => d.Vehicle.TrackPosition)
                .Take(8).ToList();
            for (int i = 0; i < drivers.Count; i++)
            {
                var driver = drivers[i];
                string name = driver is PlayerDriver
                    ? (PlayerProfile.IsNamed ? PlayerProfile.Name : "DRIVER")
                    : (driver.Vehicle as DrivableVehicle)?.Descriptor?.Name ?? "OPPONENT";
                bool hasTime = _race.RacerFinishTimes.TryGetValue(driver, out TimeSpan elapsed);
                if (!hasTime && driver is PlayerDriver)
                {
                    hasTime = true;
                    elapsed = _finishedTime;
                }
                string segment = hasTime ? Clock(elapsed) : "--:--.-";
                // Cumulative open-road totals for later segments are not
                // known from an individual Race, so leave them unknown.
                bool firstStage = !_race.Track.IsOpenRoad ||
                    GameConfig.SelectedTrackDescription?.FileName?.ToUpperInvariant()
                        .EndsWith("1.TRI") == true;
                string total = firstStage ? segment : "--:--.-";
                int y = 166 + i * 29;
                Value((i + 1).ToString(), 30, y, .37f);
                Value(name.ToUpperInvariant(), 123, y, .35f);
                Value(total, 309, y, .37f);
                if (_race.Track.IsOpenRoad)
                    Value(segment, 484, y, .37f);
            }
            DrawArt(_standingsArt, "Dond");
        }

        public void Update(GameTime gameTime)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape) || input.WasPressed(Keys.Enter) ||
                input.WasPressed(Keys.Space) || input.WasLeftMouseClicked)
            {
                if (_page == 0) _page = 1;
                else Engine.Instance.Screen = _back;
            }
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            if (_page == 0) DrawPersonal(); else DrawStandings();
            sprite.End();
        }
    }
}
