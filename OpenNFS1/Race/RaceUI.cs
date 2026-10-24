using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenNFS1.Vehicles.AI;
using OpenNFS1.Parsers;
using OpenNFS1.Physics;

namespace OpenNFS1.UI
{
    /// <summary>
    /// Driving HUD: original MASKHI.FSH speed digits and LAP artwork,
    /// with compact readable live data rather than the old debug-sized labels.
    /// </summary>
    class RaceUI
    {
        private readonly Race _race;
        private readonly Texture2D _darkBackground;
        private readonly SpriteFont _font;
        private readonly BitmapEntry[] _speedDigits = new BitmapEntry[10];
        private readonly BitmapEntry _lapLabel;

        public RaceUI(Race race)
        {
            _race = race;
            _darkBackground = new Texture2D(Engine.Instance.Device, 1, 1);
            _darkBackground.SetData(new[] { Color.Black });
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");

            var original = new FshFile(@"SIMDATA\MISC\MASKHI.FSH").Header;
            _lapLabel = original.FindByName("lap#");
            for (int i = 0; i < _speedDigits.Length; i++)
                _speedDigits[i] = original.FindByName("red" + i);
        }

        public void Render()
        {
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
            sprite.Draw(_darkBackground, new Rectangle(0, 0, 640, 32), Color.White);

            if (_race.SecondsTillStart > 0)
                DrawText(sprite, _race.SecondsTillStart.ToString(), 300, 55, 1.35f, Color.Yellow);
            else if (_race.SecondsTillStart == 0)
                DrawText(sprite, "GO!", 297, 55, 1.05f, Color.Yellow);

            TimeSpan elapsed = _race.SecondsTillStart > 0
                ? TimeSpan.Zero : _race.PlayerStats.CurrentLapTime;
            DrawText(sprite, string.Format("{0:00}:{1:00}", (int)elapsed.TotalMinutes, elapsed.Seconds),
                13, 5, 0.50f, Color.Yellow);

            int kph = Math.Clamp((int)Math.Abs(_race.Player.Vehicle.Speed), 0, 999);
            string speed = kph.ToString();
            int speedX = 225;
            foreach (char digit in speed)
            {
                var entry = _speedDigits[digit - '0'];
                if (entry?.Texture != null)
                    sprite.Draw(entry.Texture, new Vector2(speedX, 2), Color.White);
                speedX += 28;
            }
            DrawText(sprite, "km/h", speedX + 5, 11, 0.30f, Color.White);

            int gear = ((DrivableVehicle)_race.Player.Vehicle).Motor.Gearbox.CurrentGear;
            DrawText(sprite, "GEAR " + GearToString(gear), 374, 6, 0.44f, Color.Yellow);

            if (!GameConfig.SelectedTrackDescription.IsOpenRoad)
            {
                if (_lapLabel?.Texture != null)
                    sprite.Draw(_lapLabel.Texture, _lapLabel.GetDisplayAt(), Color.White);
                DrawText(sprite, Math.Max(1, Math.Min(_race.PlayerStats.CurrentLap, _race.NbrLaps))
                    .ToString(), 475, 8, 0.40f, Color.Yellow);
                DrawText(sprite, _race.NbrLaps.ToString(), 512, 8, 0.40f, Color.Yellow);
            }
            else
            {
                DrawText(sprite, "STAGE", 468, 8, 0.38f, Color.Yellow);
            }

            int participants = _race.Drivers.Count(d => d is RacingAIDriver || d is PlayerDriver);
            DrawText(sprite, string.Format("{0}/{1}",
                Math.Max(1, _race.PlayerStats.Position + 1), participants),
                562, 6, 0.44f, Color.Yellow);

            sprite.End();
        }

        private void DrawText(SpriteBatch sprite, string text, float x, float y, float scale, Color color)
        {
            sprite.DrawString(_font, text, new Vector2(x, y), color,
                0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private static string GearToString(int gear)
        {
            return gear < 0 ? "R" : gear == 0 ? "N" : gear.ToString();
        }
    }
}
