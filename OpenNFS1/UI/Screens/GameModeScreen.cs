using System;
using System.Collections.Generic;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Original NFS SE Game Mode menu, rendered exclusively from the player's
    /// GAMEMODE.QFS (no copyrighted game art shipped with this repository).
    /// Single Player and Exit are functional. Legacy modem/network transports
    /// are shown as unavailable until they have an actual implementation.
    /// </summary>
    internal sealed class GameModeScreen : IGameScreen
    {
        private sealed class Choice
        {
            public string NormalId;
            public string SelectedId;
            public Action Action;
            public BitmapEntry Normal;
            public BitmapEntry Selected;
        }

        private readonly BitmapEntry _background;
        private readonly SpriteFont _font;
        private readonly List<Choice> _choices = new List<Choice>();
        private int _selectedIndex;
        private string _message;

        public GameModeScreen()
        {
            var images = new QfsFile(@"FRONTEND\ART\CONTROL\GAMEMODE.QFS").Content.Header;
            _background = Require(images, "bgnd");
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");

            Add(images, "sind", "sinl", () =>
            {
                _message = null;
                Engine.Instance.Screen = new HomeScreen();
            });

            Add(images, "modd", "modl", () =>
                _message = "Modem/serial multiplayer is not implemented yet.");

            Add(images, "netd", "netl", () =>
                _message = "Network multiplayer is not implemented yet.");

            Add(images, "exid", "exil", () => Engine.Instance.Game.Exit());
        }

        private static BitmapEntry Require(BitmapChunk chunk, string id)
        {
            BitmapEntry result = chunk.FindByName(id);
            if (result?.Texture == null)
                throw new InvalidOperationException("Missing original Game Mode artwork: " + id);
            return result;
        }

        private void Add(BitmapChunk chunk, string normal, string selected, Action activate)
        {
            _choices.Add(new Choice
            {
                NormalId = normal,
                SelectedId = selected,
                Normal = Require(chunk, normal),
                Selected = Require(chunk, selected),
                Action = activate
            });
        }

        public void Update(GameTime gameTime)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.F6))
            {
                Engine.Instance.Screen = new PlayerNameScreen();
                return;
            }
            if (input.WasLeftMouseClicked)
            {
                Vector2 point = input.GameMousePosition;
                if (point.X >= 0)
                {
                    for (int i = 0; i < _choices.Count; i++)
                    {
                        BitmapEntry image = _choices[i].Normal;
                        Vector2 topLeft = image.GetDisplayAt();
                        Rectangle region = new Rectangle((int)topLeft.X - 8, (int)topLeft.Y - 6,
                            image.Texture.Width + 16, image.Texture.Height + 12);
                        if (region.Contains((int)point.X, (int)point.Y))
                        {
                            _selectedIndex = i;
                            _choices[i].Action();
                            return;
                        }
                    }
                }
            }

            if (input.WasPressed(Keys.Down) || input.WasPressed(Keys.Tab))
            {
                _selectedIndex = (_selectedIndex + 1) % _choices.Count;
                _message = null;
            }
            else if (input.WasPressed(Keys.Up))
            {
                _selectedIndex = (_selectedIndex + _choices.Count - 1) % _choices.Count;
                _message = null;
            }
            else if (input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space))
            {
                _choices[_selectedIndex].Action();
            }
            else if (input.WasPressed(Keys.Escape))
            {
                Engine.Instance.Game.Exit();
            }
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            sprite.Draw(_background.Texture, Vector2.Zero, Color.White);

            for (int i = 0; i < _choices.Count; i++)
            {
                BitmapEntry entry = i == _selectedIndex ? _choices[i].Selected : _choices[i].Normal;
                sprite.Draw(entry.Texture, entry.GetDisplayAt(), Color.White);
            }

            if (!string.IsNullOrEmpty(_message))
            {
                sprite.DrawString(_font, _message, new Vector2(55, 430), Color.Yellow,
                    0, Vector2.Zero, 0.43f, SpriteEffects.None, 0);
            }

            if (PlayerProfile.IsNamed)
                sprite.DrawString(_font, "PLAYER: " + PlayerProfile.Name + "  /  F6: CHANGE",
                    new Vector2(22, 451), Color.Yellow, 0, Vector2.Zero,
                    .28f, SpriteEffects.None, 0);
            sprite.End();
        }
    }
}
