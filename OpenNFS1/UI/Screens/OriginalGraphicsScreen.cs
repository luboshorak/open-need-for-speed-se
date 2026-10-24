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
    /// All six entries now lead to a real display feature or renderer change.
    /// The original bitmap IDs and their 1996 pixel positions are preserved.
    /// </summary>
    internal sealed class OriginalGraphicsScreen : IGameScreen
    {
        private readonly IGameScreen _back;
        private readonly BitmapChunk _art;
        private readonly SpriteFont _font;
        private int _selected;
        private bool _originalFullScreen;
        private int _originalDrawDistance;
        private int _originalFilter;
        private bool _originalCarDetail, _originalHorizon;
        private string _notice = "F9: ADVANCED DISPLAY / GAMMA / UPSCALING";

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
            AcceptChanges();
        }

        private void Art(string id)
        {
            var e = _art.FindByName(id);
            if (e?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(e.Texture, e.GetDisplayAt(), Color.White);
        }

        internal void AcceptChanges()
        {
            _originalFullScreen = GameConfig.FullScreen;
            _originalDrawDistance = GameConfig.DrawDistance;
            _originalFilter = GameConfig.TextureQuality;
            _originalCarDetail = GameConfig.HighCarDetail;
            _originalHorizon = GameConfig.HorizonEnabled;
        }

        private void Cancel()
        {
            bool changed = GameConfig.FullScreen != _originalFullScreen;
            GameConfig.FullScreen = _originalFullScreen;
            GameConfig.DrawDistance = _originalDrawDistance;
            GameConfig.TextureQuality = _originalFilter;
            GameConfig.HighCarDetail = _originalCarDetail;
            GameConfig.HorizonEnabled = _originalHorizon;
            if (changed)
            {
                try { ((Game1)Engine.Instance.Game).ApplyDisplaySettings(); }
                catch (Exception ex) { _notice = ex.Message; }
            }
            Engine.Instance.Screen = _back ?? new OptionsScreen();
        }

        private void Done()
        {
            GameConfig.SaveUserSettings();
            AcceptChanges();
            Engine.Instance.Screen = _back ?? new OptionsScreen();
        }

        private void Change(int direction)
        {
            switch (_selected)
            {
                case 0:
                    // The original game's 320x200 render target is not present
                    // in this engine. Offer a WORKING resolution/scaling screen
                    // instead of pretending a nonfunctional legacy toggle.
                    Engine.Instance.Screen = new DisplayOptionsScreen(this);
                    return;
                case 1:
                    bool oldFullScreen = GameConfig.FullScreen;
                    GameConfig.FullScreen = !oldFullScreen;
                    try { ((Game1)Engine.Instance.Game).ApplyDisplaySettings(); }
                    catch (Exception ex)
                    {
                        GameConfig.FullScreen = oldFullScreen;
                        _notice = "DISPLAY CHANGE FAILED: " + ex.Message;
                    }
                    return;
                case 2:
                    GameConfig.TextureQuality = (GameConfig.TextureQuality +
                        direction + 3) % 3;
                    _notice = "TEXTURE FILTER: POINT / LINEAR / ANISOTROPIC";
                    return;
                case 3:
                    int[] distances = { 2500, 5000, 7500 };
                    int index = Array.IndexOf(distances, GameConfig.DrawDistance);
                    GameConfig.DrawDistance = distances[
                        (Math.Max(index, 0) + direction + distances.Length) % distances.Length];
                    return;
                case 4:
                    // In this remake, LOW disables AI car shadows while
                    // retaining every car's collision body and wheel physics.
                    GameConfig.HighCarDetail = !GameConfig.HighCarDetail;
                    return;
                case 5:
                    GameConfig.HorizonEnabled = !GameConfig.HorizonEnabled;
                    return;
            }
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape))
            {
                Cancel();
                return;
            }
            // F1-F4 are printed on the original EA background, and must
            // retain their historical meanings. F9 is our new extension.
            if (input.WasPressed(Keys.F9))
            {
                Engine.Instance.Screen = new DisplayOptionsScreen(this);
                return;
            }
            if (input.WasPressed(Keys.F1)) { _selected = 1; Change(1); return; }
            if (input.WasPressed(Keys.F2)) { _selected = 2; Change(1); return; }
            if (input.WasPressed(Keys.F3)) { _selected = 3; Change(1); return; }
            if (input.WasPressed(Keys.F4)) { _selected = 5; Change(1); return; }
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
                    Cancel();
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

            Art("640d"); // Source engine remains 640x480, use F9 for output.
            Art(GameConfig.FullScreen ? "fuld" : "wmdd");
            Art(GameConfig.TextureQuality == 2 ? "ihid" :
                GameConfig.TextureQuality == 1 ? "imdd" : "ilwd");
            Art(GameConfig.DrawDistance == 2500 ? "clsd" :
                GameConfig.DrawDistance == 5000 ? "vmdd" : "fard");
            Art(GameConfig.HighCarDetail ? "chid" : "clwd");
            Art(GameConfig.HorizonEnabled ? "hond" : "offd");
            Art("dond");
            Art("cand");

            sprite.DrawString(_font, _notice.Length > 68 ? _notice.Substring(0, 68) : _notice,
                new Vector2(24, 403), Color.Yellow, 0, Vector2.Zero,
                0.30f, SpriteEffects.None, 0);
            sprite.End();
        }
    }
}
