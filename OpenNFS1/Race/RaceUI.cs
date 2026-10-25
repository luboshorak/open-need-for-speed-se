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
        // Five original 120x28 HUD light states from MASKHI.FSH:
        // sgrn / srdd / srdl / syld / syll, verified in original CD manifest.
        private readonly BitmapEntry _startGreen;
        private readonly BitmapEntry _startRedDark;
        private readonly BitmapEntry _startRedLit;
        private readonly BitmapEntry _startYellowDark;
        private readonly BitmapEntry _startYellowLit;
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
            _startGreen = images.FindByName("sgrn");
            _startRedDark = images.FindByName("srdd");
            _startRedLit = images.FindByName("srdl");
            _startYellowDark = images.FindByName("syld");
            _startYellowLit = images.FindByName("syll");
            if (_startGreen?.Texture == null || _startRedDark?.Texture == null ||
                _startRedLit?.Texture == null || _startYellowDark?.Texture == null ||
                _startYellowLit?.Texture == null)
                throw new InvalidOperationException(
                    "Original NFS SE start-light bitmaps missing from SIMDATA/MISC/MASKHI.FSH.");


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

        // Stable opponent identity: identical color on the world marker and map.
        private static readonly Color[] RivalColors =
        {
            Color.Cyan, Color.Magenta, Color.LimeGreen, Color.Orange,
            Color.DeepSkyBlue, Color.HotPink, Color.Gold
        };

        private Color MarkerColor(OpenNFS1.Vehicles.AI.IDriver driver)
        {
            if (driver is PlayerDriver) return Color.Yellow;
            int index = 0;
            foreach (var rival in _race.Drivers)
            {
                if (rival is RacingAIDriver)
                {
                    if (ReferenceEquals(rival, driver))
                        return RivalColors[index % RivalColors.Length];
                    index++;
                }
            }
            return Color.Red;
        }

        private void RenderRivalMarkers(SpriteBatch sprite)
        {
            var camera = Engine.Instance.Camera;
            if (camera == null) return;
            var viewport = new Viewport(0, 0, 640, 400);
            foreach (var driver in _race.Drivers)
            {
                if (driver is not RacingAIDriver || driver.Vehicle == null)
                    continue;
                var car = driver.Vehicle;
                Vector3 position = car.Position + Vector3.Up * (car.CollisionHeight + 4);
                Vector3 screen = viewport.Project(position, camera.Projection, camera.View, Matrix.Identity);
                if (screen.Z < 0 || screen.Z > 1 || screen.X < 5 || screen.X > 635 ||
                    screen.Y < 5 || screen.Y > 390) continue;
                Color color = MarkerColor(driver);
                int x = (int)screen.X, y = (int)screen.Y;
                // Small down-pointing marker, no font glyph dependency.
                sprite.Draw(_pixel, new Rectangle(x - 6, y - 9, 13, 3), color);
                sprite.Draw(_pixel, new Rectangle(x - 4, y - 6, 9, 3), color);
                sprite.Draw(_pixel, new Rectangle(x - 2, y - 3, 5, 3), color);
                sprite.Draw(_pixel, new Rectangle(x, y, 1, 3), color);
            }
        }

        public void Render()
        {
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);

            // On the original CD, the five start-light tiles REPLACE the
            // upper 32px status bar during the grid sequence. The normal
            // counters do not appear behind the traffic lights.
            if (_race.StartLightsVisible)
            {
                RenderTrackMap(sprite);
                RenderRivalMarkers(sprite);
                RenderStartingCountdown(sprite);
                sprite.End();
                return;
            }

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
                    HudGeometry.Gear);
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
                592, 6, 11, 16);
            DrawNumber(sprite, Math.Max(1, racers), 637, 6, 11, 16);

            RenderTrackMap(sprite);
            RenderRivalMarkers(sprite);
            sprite.End();
        }

        private void RenderStartingCountdown(SpriteBatch sprite)
        {
            // Video reference: left-to-right RED RED RED, YELLOW YELLOW,
            // then all GREEN. Original 120x28 bitmap tiles occupy five
            // 128px HUD slots (120px artwork + 8px transparent interval).
            int stage = _race.StartLightStage;
            for (int index = 0; index < 5; index++)
            {
                BitmapEntry tile = stage >= 5 ? _startGreen :
                    index < 3 ? (index <= stage ? _startRedLit : _startRedDark) :
                    (index <= stage ? _startYellowLit : _startYellowDark);
                sprite.Draw(tile.Texture, new Vector2(index * 128, 0), Color.White);
            }
        }

        private void DrawClock(SpriteBatch sprite, TimeSpan time, int clock)
        {
            if (time < TimeSpan.Zero) time = TimeSpan.Zero;
            int minutes = Math.Min(99, (int)time.TotalMinutes);
            int seconds = time.Seconds;
            int[] values = { minutes / 10, minutes % 10,
                seconds / 10, seconds % 10, time.Milliseconds / 100 };
            for (int i = 0; i < values.Length; i++)
                DrawGlyph(sprite, _yellowDigits[values[i]],
                    HudGeometry.ClockDigit(clock, i));
        }

        private void DrawSpeed(SpriteBatch sprite, int speed)
        {
            string value = speed.ToString();
            // Source speed digits are 28x27. Their original source slot is
            // x=266..346, ending BEFORE the H/gear icon at x=349.
            for (int i = 0; i < value.Length; i++)
                DrawGlyph(sprite, _speedDigits[value[i] - '0'],
                    HudGeometry.SpeedDigit(value.Length, i));
        }

        private void DrawNumber(SpriteBatch sprite, int value,
            int rightEdge, int y, int digitWidth, int height)
        {
            string text = Math.Clamp(value, 0, 99).ToString();
            for (int i = 0; i < text.Length; i++)
                DrawGlyph(sprite, _yellowDigits[text[i] - '0'],
                    HudGeometry.ValueDigit(value, i, rightEdge, y, digitWidth, height));
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
                    MarkerColor(driver));
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
