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
                _speedDigits[i] = images.FindByName("red" + i);

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

            // The original stat bitmap has dedicated fields. Keep all text
            // inside those fields: no "GEAR" or large 207 km/h over icons.
            DrawText(sprite, FormatTime(raceTime), 34, 7, 0.30f);
            DrawText(sprite, FormatTime(lapTime), 160, 7, 0.30f);

            int speed = Math.Clamp((int)Math.Abs(_race.Player.Vehicle.Speed), 0, 999);
            string speedValue = speed.ToString();
            int speedX = 308 + (3 - speedValue.Length) * 12;
            foreach (char c in speedValue)
            {
                var digit = _speedDigits[c - '0'];
                if (digit?.Texture != null)
                    sprite.Draw(digit.Texture, new Rectangle(speedX, 3, 18, 23), Color.White);
                speedX += 13;
            }

            if (_race.Player.Vehicle is DrivableVehicle car)
            {
                int gear = car.Motor.Gearbox.CurrentGear;
                DrawText(sprite, gear < 0 ? "R" : gear == 0 ? "N" : gear.ToString(),
                    379, 6, 0.43f);
            }

            if (_race.Track.IsOpenRoad)
            {
                if (_mileLabel?.Texture != null)
                    sprite.Draw(_mileLabel.Texture, _mileLabel.GetDisplayAt(), Color.White);
                int remaining = Math.Max(0, _race.Track.RoadNodes.Count - 1 -
                    (_race.Player.Vehicle.CurrentNode?.Number ?? 0));
                // This is an approximate remaining-node indicator; actual
                // miles require distance data from the original TRI parser.
                DrawText(sprite, (remaining * 0.01f).ToString("0.0"), 468, 7, 0.33f);
            }
            else
            {
                if (_lapLabel?.Texture != null)
                    sprite.Draw(_lapLabel.Texture, _lapLabel.GetDisplayAt(), Color.White);
                int lap = Math.Clamp(_race.PlayerStats.CurrentLap, 1, _race.NbrLaps);
                DrawText(sprite, lap + " / " + _race.NbrLaps, 469, 7, 0.34f);
            }

            int racers = _race.Drivers.Count(d => d is RacingAIDriver || d is PlayerDriver);
            DrawText(sprite,
                Math.Clamp(_race.PlayerStats.Position + 1, 1, Math.Max(1, racers)) +
                "/" + Math.Max(1, racers), 577, 7, 0.37f);

            RenderTrackMap(sprite);
            sprite.End();
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time < TimeSpan.Zero) time = TimeSpan.Zero;
            return ((int)time.TotalMinutes).ToString("00") + ":" +
                time.Seconds.ToString("00") + "." + (time.Milliseconds / 100);
        }

        private void DrawText(SpriteBatch sprite, string text, float x, float y, float scale)
        {
            sprite.DrawString(_font, text, new Vector2(x, y), Color.Yellow,
                0, Vector2.Zero, scale, SpriteEffects.None, 0);
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
