using System;
using System.Diagnostics;
using System.IO;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Officer has pulled the player over. Uses the original commercial
    /// TICKET.QFS when available, with a safe fallback if the CD variant
    /// cannot be decoded yet.
    /// </summary>
    internal sealed class PoliceTicketScreen : IGameScreen
    {
        private readonly IGameScreen _return;
        private readonly Race _race;
        private readonly BitmapChunk _ticket;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        public PoliceTicketScreen(Race race, IGameScreen back)
        {
            _race = race;
            _return = back;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _pixel = new Texture2D(Engine.Instance.Device, 1, 1);
            _pixel.SetData(new[] { Color.White });
            try
            {
                _ticket = new QfsFile(@"FRONTEND\ART\TICKET.QFS").Content.Header;
            }
            catch (Exception error) when (error is IOException ||
                error is InvalidDataException || error is IndexOutOfRangeException ||
                error is ArgumentOutOfRangeException)
            {
                Trace.WriteLine("Original police ticket art unavailable: " + error);
            }
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Escape) ||
                input.WasPressed(Keys.Space) || input.WasLeftMouseClicked)
            {
                _race.PoliceTicketed = false;
                if (_return is DoRaceScreen driving)
                    driving.Resume(); // restore engine audio after the citation
                Engine.Instance.Screen = _return;
            }
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            sprite.Draw(_pixel, new Rectangle(0, 0, 640, 480), Color.Black);
            var art = _ticket?.FindByName("bgnd") ??
                _ticket?.FindByName("0000");
            if (art?.Texture != null)
                sprite.Draw(art.Texture, art.GetDisplayAt(), Color.White);
            sprite.Draw(_pixel, new Rectangle(32, 376, 576, 88), Color.Black * .78f);
            sprite.DrawString(_font, "PULLED OVER BY POLICE", new Vector2(65, 390),
                Color.Yellow, 0, Vector2.Zero, .53f, SpriteEffects.None, 0);
            sprite.DrawString(_font, "ENTER / CLICK: RETURN TO THE ROAD",
                new Vector2(65, 428), Color.White, 0,
                Vector2.Zero, .31f, SpriteEffects.None, 0);
            sprite.End();
        }
    }
}
