using System;
using System.Linq;
using OpenNFS1.Vehicles.AI;
using OpenNFS1.Physics;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Original track/segment standings artwork. Shown from FINMENU Game
    /// Standings without re-recording championship points or auto-saving.
    /// </summary>
    internal sealed class RaceStandingsScreen : IGameScreen
    {
        private readonly Race _race;
        private readonly TimeSpan _finishedTime;
        private readonly IGameScreen _back;
        private readonly SpriteFont _font;
        private readonly BitmapChunk _art;
        private int _page; // 0: personal segment results, 1: complete standings

        public RaceStandingsScreen(Race race, TimeSpan finishedTime, IGameScreen back)
        {
            _race = race;
            _finishedTime = finishedTime;
            _back = back;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            string asset = race.Track.IsOpenRoad ? "STANDSEG.QFS" : "STANDTRK.QFS";
            _art = new QfsFile(@"FRONTEND\ART\STATS\" + asset).Content.Header;
        }

        private void Art(string id)
        {
            var entry = _art.FindByName(id);
            if (entry?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(entry.Texture, entry.GetDisplayAt(), Color.White);
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape) || input.WasPressed(Keys.Enter) ||
                input.WasPressed(Keys.Space) || input.WasLeftMouseClicked)
            {
                if (_page == 0) _page = 1;
                else Engine.Instance.Screen = _back;
            }
        }

        private void Value(string value, int x, int y, float scale = 0.42f)
        {
            Engine.Instance.SpriteBatch.DrawString(_font, value, new Vector2(x,y),
                Color.Yellow, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            Art("bgnd");
            if (_page == 0)
            {
                Value((GameConfig.SelectedTrackDescription?.Name ?? "RACE").ToUpperInvariant(), 12, 108, .65f);
                Value(_race.Track.IsOpenRoad ? "SEGMENT RESULTS" : "RACE RESULTS", 12, 142, .43f);
                Value("CURRENT STANDING", 20, 206, .38f);
                Value((_race.PlayerStats.Position + 1).ToString(), 360, 206, .46f);
                Value("SEGMENT TIME", 20, 243, .38f);
                Value(_finishedTime.ToString(@"m\:ss\.f"), 360, 243, .46f);
                Value("TOP SPEED", 20, 280, .38f);
                Value(_race.PeakPlayerSpeed.ToString("0.0"), 360, 280, .46f);
                Value("DONE - VIEW STANDINGS", 20, 422, .37f);
                Art("Dond");
                sprite.End();
                return;
            }
            // Standings show real race participants. The original game displays
            // names and per-racer times, not a one-line player position label.
            string course = GameConfig.SelectedTrackDescription?.Name ?? "RACE";
            Value(course.ToUpperInvariant(), 12, 104, .65f);
            Value(_race.Track.IsOpenRoad ? "SEGMENT RESULTS" : "RACE RESULTS", 12, 132, .43f);
            Value("RANK", 18, 183, .36f);
            Value("NAME / CAR", 126, 183, .36f);
            Value("TOTAL TIME", 316, 183, .36f);
            Value("SEGMENT TIME", 485, 183, .36f);

            var racers = _race.Drivers.Where(d => d is PlayerDriver || d is RacingAIDriver)
                .OrderBy(d => _race.RacerFinishTimes.ContainsKey(d) ? 0 : 1)
                .ThenBy(d => _race.RacerFinishTimes.TryGetValue(d, out var finish)
                    ? finish.TotalSeconds : double.MaxValue)
                .ThenByDescending(d => d.Vehicle.TrackPosition)
                .ToList();
            for (int i = 0; i < racers.Count && i < 8; i++)
            {
                var driver = racers[i];
                bool player = driver is PlayerDriver;
                string name = player ? "PLAYER" :
                    (driver.Vehicle as DrivableVehicle)?.Descriptor?.Name ?? "OPPONENT";
                string time = _race.RacerFinishTimes.TryGetValue(driver, out var elapsed)
                    ? elapsed.ToString(@"m\\:ss\\.f")
                    : (player ? _finishedTime.ToString(@"m\\:ss\\.f") : "--:--.-");
                int y = 224 + i * 33;
                Value((i + 1).ToString(), 30, y, .40f);
                Value(name.ToUpperInvariant(), 122, y, .34f);
                Value(time, 323, y, .40f);
                Value(time, 504, y, .40f);
            }
            Art("Dond");
            sprite.End();
        }
    }
}
