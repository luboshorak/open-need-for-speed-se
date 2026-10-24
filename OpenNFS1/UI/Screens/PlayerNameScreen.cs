using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>First-launch NFS SE player-name prompt, using the original
    /// GAMEMODE.QFS backdrop. Subsequent launches load the saved profile.</summary>
    internal sealed class PlayerNameScreen : IGameScreen
    {
        private readonly Texture2D _background;
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        private string _name = "";

        public PlayerNameScreen()
        {
            // Load the genuine NFS SE title artwork, not our generic
            // GAMEMODE.QFS with a large improvised black card. Different
            // retail releases may store the title image under a different ID.
            Texture2D artwork = null;
            try
            {
                var title = new QfsFile(@"FRONTEND\ART\TITLE.QFS").Content.Header;
                artwork = title.FindByName("bgnd")?.Texture ??
                    title.FindByName("0000")?.Texture ??
                    title.Bitmaps.Where(b => b.Texture != null)
                        .OrderByDescending(b => b.Texture.Width * b.Texture.Height)
                        .Select(b => b.Texture).FirstOrDefault();
            }
            catch (Exception error) when (error is IOException ||
                error is InvalidDataException ||
                error is InvalidOperationException ||
                error is IndexOutOfRangeException)
            {
                Trace.WriteLine("Original name-entry title art: " + error);
            }
            _background = artwork ?? new QfsFile(
                @"FRONTEND\ART\CONTROL\GAMEMODE.QFS")
                .Content.Header.FindByName("bgnd").Texture;
            _name = PlayerProfile.Name ?? "";
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _pixel = new Texture2D(Engine.Instance.Device, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Enter))
            {
                if (_name.Trim().Length > 0)
                {
                    try
                    {
                        PlayerProfile.Save(_name);
                        Engine.Instance.Screen = new GameModeScreen();
                    }
                    catch (Exception ex) when (ex is System.IO.IOException ||
                                               ex is UnauthorizedAccessException)
                    {
                        // Don't trap the player on a nonwritable profile.
                        Engine.Instance.Screen = new GameModeScreen();
                    }
                }
                return;
            }
            if (input.WasPressed(Keys.Back) && _name.Length > 0)
                _name = _name.Substring(0, _name.Length - 1);
            else if (input.WasPressed(Keys.Space) && _name.Length < 16)
                _name += " ";
            else if (_name.Length < 16)
            {
                for (int key = (int)Keys.A; key <= (int)Keys.Z; key++)
                {
                    if (input.WasPressed((Keys)key))
                    {
                        _name += (char)('A' + key - (int)Keys.A);
                        break;
                    }
                }
                for (int key = (int)Keys.D0; key <= (int)Keys.D9; key++)
                {
                    if (input.WasPressed((Keys)key))
                    {
                        _name += (char)('0' + key - (int)Keys.D0);
                        break;
                    }
                }
            }
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            sprite.Draw(_background, new Rectangle(0, 0, 640, 480), Color.White);
            // Preserve original full-screen EA title artwork; use a smaller
            // name-entry band instead of obscuring most of the screen.
            sprite.Draw(_pixel, new Rectangle(40, 269, 560, 138),
                Color.Black * 0.72f);
            Text(sprite, "ENTER YOUR NAME", 65, 285, .56f, Color.Yellow);
            Text(sprite, _name.Length == 0 ? "_" : _name + "_",
                70, 319, .70f, Color.White);
            Text(sprite, "UP TO 16 CHARACTERS  /  ENTER TO CONFIRM",
                65, 373, .32f, Color.Yellow);
            sprite.End();
        }

        private void Text(SpriteBatch sprite, string message,
            int x, int y, float scale, Color color)
        {
            sprite.DrawString(_font, message, new Vector2(x, y),
                color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }
}
