using System;
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
                Engine.Instance.Screen = _back;
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
            // Original STANDTRK / STANDSEG shows title+table headings.
            // Only report measured values; records/average speed are not
            // invented when the current physics engine does not track them.
            Value((_race.PlayerStats.Position + 1) + " / " +
                RaceModeRules.OpponentCount(GameConfig.CurrentRaceMode).ToString() +
                " OPPONENTS", 300, 140, 0.36f);
            TimeSpan elapsed = _finishedTime;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            Value(elapsed.ToString(@"mm\:ss\.f"), 310, 164, 0.48f);
            Value(GameConfig.SelectedVehicle?.Name ?? "PLAYER", 24, 310, 0.37f);
            Art("Dond");
            sprite.End();
        }
    }
}
