using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// New fork credits with present-day redevelopment first and preserved
    /// original OpenNFS1 provenance; the CD's EA game assets remain credited.
    /// </summary>
    internal sealed class CreditsScreen : IGameScreen
    {
        private readonly IGameScreen _back;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly Texture2D _background;

        public CreditsScreen(IGameScreen back)
        {
            _back = back;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _background = Engine.Instance.ContentManager.Load<Texture2D>("splash-screen");
            _pixel = new Texture2D(Engine.Instance.Device, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape) ||
                input.WasPressed(Keys.Enter) || input.WasLeftMouseClicked)
                Engine.Instance.Screen = _back ?? new OptionsScreen();
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            sprite.Draw(_background, new Rectangle(0, 0, 640, 480), Color.White);
            sprite.Draw(_pixel, new Rectangle(0, 0, 640, 480), Color.Black * 0.87f);

            Write(sprite, "OPENNFS1 SE RELOADED", 32, 28, .83f, Color.Yellow);
            Write(sprite, "Redevelopment and current project maintenance", 33, 102, .42f, Color.White);
            Write(sprite, "Lubos Horak", 33, 129, .73f, Color.White);

            Write(sprite, "ORIGINAL OPENNFS1 FOUNDATION", 33, 206, .53f, Color.Yellow);
            Write(sprite, "Jeff Harris / 1am Studios", 33, 239, .46f, Color.White);
            Write(sprite, "Engine, codebase and early NFS format integration", 33, 264, .34f, Color.LightGray);
            Write(sprite, "Further format research: Ian Brown, Denis Auroux", 33, 288, .33f, Color.LightGray);

            Write(sprite, "THE NEED FOR SPEED SE", 33, 341, .50f, Color.Yellow);
            Write(sprite, "Original game and assets: Electronic Arts and creators", 33, 372, .34f, Color.White);
            Write(sprite, "Unofficial fan redevelopment - not endorsed by EA", 33, 396, .32f, Color.LightGray);

            Write(sprite, "ENTER / ESC / CLICK TO RETURN", 33, 451, .39f, Color.Yellow);
            sprite.End();
        }

        private void Write(SpriteBatch batch, string text, int x, int y, float scale, Color color)
        {
            batch.DrawString(_font, text, new Vector2(x,y), color,
                0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }
}
