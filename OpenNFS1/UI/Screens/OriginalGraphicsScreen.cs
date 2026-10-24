using System;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Original Need for Speed SE Graphics Detail screen from GRAPHICS.QFS.
    /// Old graphics toggles that the remake engine cannot honor yet are
    /// explicitly read-only; F2 opens the separate modern display extensions.
    /// </summary>
    internal sealed class OriginalGraphicsScreen : IGameScreen
    {
        private readonly IGameScreen _back;
        private readonly BitmapChunk _art;
        private readonly SpriteFont _font;
        private int _selected;
        private string _notice = "F2: MODERN DISPLAY / GAMMA / UPSCALING";

        private static readonly string[] Labels =
            { "scrd", "wind", "imad", "vand", "card", "hord" };
        private static readonly string[] SelectedLabels =
            { "scrl", "winl", "imal", "vanl", "carl", "horl" };
        private static readonly int[] Y = { 106, 159, 210, 262, 315, 366 };

        public OriginalGraphicsScreen(IGameScreen back)
        {
            _back = back;
            _art = new QfsFile(@"FRONTEND\ART\OPTION\GRAPHICS.QFS").Content.Header;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
        }

        private void Art(string id)
        {
            var e = _art.FindByName(id);
            if (e?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(e.Texture, e.GetDisplayAt(), Color.White);
        }

        private void Done()
        {
            GameConfig.SaveUserSettings();
            Engine.Instance.Screen = _back ?? new OptionsScreen();
        }

        private void Change(int direction)
        {
            switch (_selected)
            {
                case 0:
                    _notice = "INTERNAL GAME GRAPHICS REMAIN 640 X 480.";
                    break;
                case 1:
                    GameConfig.FullScreen = !GameConfig.FullScreen;
                    try { ((Game1)Engine.Instance.Game).ApplyDisplaySettings(); }
                    catch (Exception ex)
                    {
                        GameConfig.FullScreen = !GameConfig.FullScreen;
                        _notice = "DISPLAY CHANGE FAILED: " + ex.Message;
                    }
                    break;
                case 2:
                    _notice = "ORIGINAL IMAGE DETAIL MODES: NOT YET SUPPORTED.";
                    break;
                case 3:
                    int[] values = { 2500, 5000, 7500 };
                    int index = Array.IndexOf(values, GameConfig.DrawDistance);
                    GameConfig.DrawDistance = values[(Math.Max(index, 0) + direction + 3) % 3];
                    break;
                case 4:
                    _notice = "CAR GEOMETRY DETAIL SWITCH: NOT YET SUPPORTED.";
                    break;
                case 5:
                    _notice = "ORIGINAL HORIZON OPTION: NOT YET SUPPORTED.";
                    break;
            }
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape))
            {
                Engine.Instance.Screen = _back ?? new OptionsScreen();
                return;
            }
            if (input.WasPressed(Keys.F2))
            {
                Engine.Instance.Screen = new DisplayOptionsScreen(this);
                return;
            }
            if (input.WasPressed(Keys.Up))
                _selected = (_selected + Labels.Length - 1) % Labels.Length;
            else if (input.WasPressed(Keys.Down) || input.WasPressed(Keys.Tab))
                _selected = (_selected + 1) % Labels.Length;
            else if (input.WasPressed(Keys.Left)) Change(-1);
            else if (input.WasPressed(Keys.Right) ||
                input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space))
                Change(1);

            if (!input.WasLeftMouseClicked) return;
            Vector2 p = input.GameMousePosition;
            if (p.X < 0) return;
            if (p.Y >= 434)
            {
                if (p.X < 140) Done();
                else if (p.X > 490)
                    Engine.Instance.Screen = _back ?? new OptionsScreen();
                return;
            }
            if (p.Y >= 397 && p.Y < 434)
            {
                Engine.Instance.Screen = new DisplayOptionsScreen(this);
                return;
            }
            for (int i = 0; i < Y.Length; i++)
            {
                if (p.Y >= Y[i] - 3 && p.Y <= Y[i] + 30)
                {
                    _selected = i;
                    Change(p.X >= 345 ? 1 : -1);
                    return;
                }
            }
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            Art("bgnd");
            for (int i = 0; i < Labels.Length; i++)
                Art(i == _selected ? SelectedLabels[i] : Labels[i]);

            Art("640d"); // Actual native UI and 3D backing surface.
            Art(GameConfig.FullScreen ? "fuld" : "wmdd");
            Art("ihid"); // Legacy original maximum image quality display.
            Art(GameConfig.DrawDistance == 2500 ? "clsd" :
                GameConfig.DrawDistance == 5000 ? "vmdd" : "fard");
            Art("chid"); // Car-detail option is not yet connected.
            Art("hond"); // Native horizon bitmap remains visible.
            Art("dond");
            Art("cand");

            sprite.DrawString(_font, _notice.Length > 68 ? _notice.Substring(0, 68) : _notice,
                new Vector2(24, 403), Color.Yellow, 0, Vector2.Zero,
                0.30f, SpriteEffects.None, 0);
            sprite.End();
        }
    }
}
