using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenNFS1.Parsers;
using OpenNFS1.Physics;
using OpenNFS1.Vehicles.AI;

namespace OpenNFS1.UI
{
    /// <summary>
    /// Original NFS SE status bar (stat) and track map (mpbd) from
    /// SIMDATA/MISC/MASKHI.FSH, not an improvised black text overlay.
    /// Gameplay counters are laid out within the native bitmap regions.
    /// </summary>
    class RaceUI
    {
        private readonly Race _race;
        private readonly SpriteFont _font;
        private readonly BitmapEntry _statusBar;
        private readonly BitmapEntry _lapLabel;
        private readonly BitmapEntry _mileLabel;
        private readonly BitmapEntry _mapBackground;
        private readonly BitmapEntry[] _speedDigits = new BitmapEntry[10];
        private readonly BitmapEntry[] _yellowDigits = new BitmapEntry[10];
        private readonly BitmapEntry _yellowReverse;
        private readonly BitmapEntry _yellowNeutral;
        private readonly Texture2D _pixel;
        private readonly Vector2[] _circuitMap;

        public RaceUI(Race race)
        {
            _race = race;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _pixel = new Texture2D(Engine.Instance.Device, 1, 1);
            _pixel.SetData(new[] { Color.White });

            var images = new FshFile(@"SIMDATA\MISC\MASKHI.FSH").Header;
            _statusBar = images.FindByName("stat");
            _lapLabel = images.FindByName("lap#");
            _mileLabel = images.FindByName("mile");
            _mapBackground = images.FindByName("mpbd");
            for (int i = 0; i < _speedDigits.Length; i++)
            {
                _speedDigits[i] = images.FindByName("red" + i);
                _yellowDigits[i] = images.FindByName("yel" + i);
            }
            _yellowReverse = images.FindByName("yelr");
            _yellowNeutral = images.FindByName("yeln");

            // Precompute the real circuit outline only once, not every frame.
            if (!race.Track.IsOpenRoad && race.Track.RoadNodes.Count > 1)
            {
                var nodes = race.Track.RoadNodes;
                float minX = nodes.Min(n => n.Position.X);
                float maxX = nodes.Max(n => n.Position.X);
                float minZ = nodes.Min(n => n.Position.Z);
                float maxZ = nodes.Max(n => n.Position.Z);
                _circuitMap = new Vector2[nodes.Count];
                for (int n = 0; n < nodes.Count; n++)
                    _circuitMap[n] = MapProjection.ToMap(
                        nodes[n].Position.X, nodes[n].Position.Z,
                        minX, maxX, minZ, maxZ);
            }
        }

        public void Render()
        {
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);

            if (_statusBar?.Texture != null)
                sprite.Draw(_statusBar.Texture, _statusBar.GetDisplayAt(), Color.White);
            else
                sprite.Draw(_pixel, new Rectangle(0, 0, 640, 32), Color.Black);

            TimeSpan raceTime = _race.SecondsTillStart > 0
                ? TimeSpan.Zero : _race.RaceTime;
            TimeSpan lapTime = _race.SecondsTillStart > 0
                ? TimeSpan.Zero : _race.PlayerStats.CurrentLapTime;

            // MASKHI.FSH/stat has built-in colon, tenths separator, flags,
            // LAP/MILES artwork and the position slash. Only draw actual
            // numeric glyphs in its EMPTY slots; no duplicate separators.
            DrawClock(sprite, raceTime, 0);
            DrawClock(sprite, lapTime, 1);

            int speed = Math.Clamp((int)Math.Abs(_race.Player.Vehicle.Speed), 0, 999);
            DrawSpeed(sprite, speed);

            if (_race.Player.Vehicle is DrivableVehicle car)
            {
                int gear = car.Motor.Gearbox.CurrentGear;
                DrawGlyph(sprite, gear < 0 ? _yellowReverse :
                    gear == 0 ? _yellowNeutral : _yellowDigits[Math.Clamp(gear, 0, 9)],
                    new Rectangle(377, 7, 15, 16));
            }

            if (_race.Track.IsOpenRoad)
            {
                // Do not invent mileage from node count; real distance needs
                // the original TRI coordinate-unit calibration.
                DrawArt(sprite, _mileLabel);
            }
            else
            {
                DrawArt(sprite, _lapLabel);
                int lap = Math.Clamp(_race.PlayerStats.CurrentLap, 1, _race.NbrLaps);
                DrawNumber(sprite, lap, 479, 6, 12, 15);
                DrawNumber(sprite, _race.NbrLaps, 531, 6, 12, 15);
            }

            int racers = _race.Drivers.Count(d => d is RacingAIDriver || d is PlayerDriver);
            DrawNumber(sprite,
                Math.Clamp(_race.PlayerStats.Position + 1, 1, Math.Max(1, racers)),
                591, 6, 11, 16);
            DrawNumber(sprite, Math.Max(1, racers), 637, 6, 11, 16);

            RenderTrackMap(sprite);
            sprite.End();
        }

        private static readonly int[] FirstClockDigits =
            { 30, 42, 64, 76, 99 };
        private static readonly int[] SecondClockDigits =
            { 149, 161, 192, 204, 235 };

        private void DrawClock(SpriteBatch sprite, TimeSpan time, int clock)
        {
            if (time < TimeSpan.Zero) time = TimeSpan.Zero;
            int minutes = Math.Min(99, (int)time.TotalMinutes);
            int seconds = time.Seconds;
            int[] values = { minutes / 10, minutes % 10,
                seconds / 10, seconds % 10, time.Milliseconds / 100 };
            var xs = clock == 0 ? FirstClockDigits : SecondClockDigits;
            for (int i = 0; i < values.Length; i++)
                DrawGlyph(sprite, _yellowDigits[values[i]],
                    new Rectangle(xs[i], 8, 11, 13));
        }

        private void DrawSpeed(SpriteBatch sprite, int speed)
        {
            string value = speed.ToString();
            // Source speed digits are 28x27. Their original source slot is
            // x=266..346, ending BEFORE the H/gear icon at x=349.
            int x = 266 + (3 - value.Length) * 26;
            foreach (char digit in value)
            {
                DrawGlyph(sprite, _speedDigits[digit - '0'],
                    new Rectangle(x, 2, 28, 27));
                x += 26;
            }
        }

        private void DrawNumber(SpriteBatch sprite, int value,
            int rightEdge, int y, int digitWidth, int height)
        {
            string text = Math.Clamp(value, 0, 99).ToString();
            int x = rightEdge - text.Length * digitWidth;
            foreach (char digit in text)
            {
                DrawGlyph(sprite, _yellowDigits[digit - '0'],
                    new Rectangle(x, y, digitWidth, height));
                x += digitWidth;
            }
        }

        private static void DrawArt(SpriteBatch sprite, BitmapEntry entry)
        {
            if (entry?.Texture != null)
                sprite.Draw(entry.Texture, entry.GetDisplayAt(), Color.White);
        }

        private static void DrawGlyph(SpriteBatch sprite, BitmapEntry entry, Rectangle bounds)
        {
            if (entry?.Texture != null)
                sprite.Draw(entry.Texture, bounds, Color.White);
        }

        private void RenderTrackMap(SpriteBatch sprite)
        {
            if (_mapBackground?.Texture == null) return;
            sprite.Draw(_mapBackground.Texture, _mapBackground.GetDisplayAt(), Color.White);

            if (_race.Track.IsOpenRoad)
            {
                // Original road map is a narrow vertical course strip.
                sprite.Draw(_pixel, new Rectangle(569, 315, 2, 116), Color.LightGray);
            }
            else if (_circuitMap?.Length > 1)
            {
                int stride = Math.Max(1, _circuitMap.Length / 90);
                Vector2 previous = _circuitMap[0];
                for (int i = stride; i < _circuitMap.Length; i += stride)
                {
                    DrawLine(sprite, previous, _circuitMap[i], Color.LightGray);
                    previous = _circuitMap[i];
                }
                DrawLine(sprite, previous, _circuitMap[_circuitMap.Length - 1],
                    Color.LightGray);
                DrawLine(sprite, _circuitMap[_circuitMap.Length - 1], _circuitMap[0],
                    Color.LightGray);
            }

            foreach (var driver in _race.Drivers)
            {
                if (driver is not PlayerDriver && driver is not RacingAIDriver)
                    continue;
                int node = driver.Vehicle.CurrentNode?.Number ?? 0;
                Vector2 point;
                if (_race.Track.IsOpenRoad)
                {
                    float fraction = MathHelper.Clamp(node /
                        (float)Math.Max(1, _race.Track.RoadNodes.Count - 1), 0, 1);
                    point = new Vector2(570, 431 - fraction * 116);
                }
                else
                {
                    point = _circuitMap != null && _circuitMap.Length > 0
                        ? _circuitMap[Math.Clamp(node, 0, _circuitMap.Length - 1)]
                        : new Vector2(570, 370);
                }
                bool player = driver is PlayerDriver;
                sprite.Draw(_pixel,
                    new Rectangle((int)point.X - (player ? 3 : 2),
                        (int)point.Y - (player ? 3 : 2),
                        player ? 7 : 5, player ? 7 : 5),
                    player ? Color.Yellow : Color.Red);
            }
        }

        private void DrawLine(SpriteBatch sprite, Vector2 a, Vector2 b, Color color)
        {
            Vector2 diff = b - a;
            float length = diff.Length();
            if (length < 0.25f) return;
            sprite.Draw(_pixel, a, null, color, (float)Math.Atan2(diff.Y, diff.X),
                Vector2.Zero, new Vector2(length, 1.5f), SpriteEffects.None, 0);
        }
    }
}
