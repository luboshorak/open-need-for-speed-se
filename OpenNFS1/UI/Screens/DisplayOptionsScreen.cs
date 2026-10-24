using System;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// New display-only options beside the original 1996 graphics controls.
    /// The underlying menus remain in native 640x480 coordinates.
    /// </summary>
    internal sealed class DisplayOptionsScreen : IGameScreen
    {
        private static readonly (int Width, int Height)[] Sizes =
        {
            (800,600), (1024,768), (1280,720), (1280,960),
            (1600,900), (1920,1080), (2560,1440)
        };
        private const int Rows = 10;
        private readonly IGameScreen _back;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly Texture2D _background;

        private readonly bool _originalFull, _savedOriginalColors, _savedVividColors;
        private bool _previewVividColors;
        private bool _previewOriginalColors;
        private readonly int _originalW, _originalH, _originalScaling, _originalDrawDistance;
        private readonly float _originalGamma, _originalBrightness, _originalContrast;

        private bool _full;
        private int _sizeIndex, _scaling, _selected, _drawDistance;
        private float _gamma, _brightness, _contrast;
        private string _status = "Large windows auto-fit your monitor";

        public DisplayOptionsScreen(IGameScreen back)
        {
            _back = back;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _background = Engine.Instance.ContentManager.Load<Texture2D>("splash-screen");
            _pixel = new Texture2D(Engine.Instance.Device, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _originalFull = _full = GameConfig.FullScreen;
            _savedOriginalColors = _previewOriginalColors = GameConfig.OriginalColors;
            _savedVividColors = _previewVividColors = GameConfig.VividColors;
            _originalW = GameConfig.WindowWidth;
            _originalH = GameConfig.WindowHeight;
            _originalScaling = _scaling = GameConfig.ScalingMode;
            _originalGamma = _gamma = GameConfig.Gamma;
            _originalBrightness = _brightness = GameConfig.Brightness;
            _originalContrast = _contrast = GameConfig.Contrast;
            _originalDrawDistance = _drawDistance = GameConfig.DrawDistance;

            int found = Array.FindIndex(Sizes, s =>
                s.Width == _originalW && s.Height == _originalH);
            _sizeIndex = found >= 0 ? found : 3;
        }

        private void PreviewColor()
        {
            GameConfig.OriginalColors = _previewOriginalColors;
            GameConfig.VividColors = _previewVividColors;
            GameConfig.Gamma = _gamma;
            GameConfig.Brightness = _brightness;
            GameConfig.Contrast = _contrast;
            GameConfig.ScalingMode = _scaling;
            DisplaySettings.IntegerScaling = _scaling == 2;
        }

        private void Change(int direction)
        {
            switch (_selected)
            {
                case 0: _full = !_full; break;
                case 1:
                    _sizeIndex = (_sizeIndex + direction + Sizes.Length) % Sizes.Length;
                    break;
                case 2: _scaling = (_scaling + direction + 3) % 3; break;
                case 3: _gamma = Math.Clamp(_gamma + direction * 0.05f, 0.60f, 1.60f); _previewOriginalColors = false; _previewVividColors = false; break;
                case 4: _brightness = Math.Clamp(_brightness + direction * 0.025f, -0.30f, 0.30f); _previewOriginalColors = false; break;
                case 5: _contrast = Math.Clamp(_contrast + direction * 0.05f, 0.60f, 1.60f); _previewOriginalColors = false; break;
                case 6:
                    int[] distances = { 2500, 5000, 7500 };
                    int index = Array.IndexOf(distances, _drawDistance);
                    if (index < 0) index = 1;
                    _drawDistance = distances[(index + direction + distances.Length) % distances.Length];
                    break;
                case 7: ResetColors(); break;
                case 8: Apply(); return;
                case 9: Cancel(); return;
            }
            PreviewColor();
        }

        private void ResetColors()
        {
            _previewOriginalColors = true;
            _previewVividColors = false;
            _gamma = 1.0f;
            _brightness = 0;
            _contrast = 1.0f;
            PreviewColor();
        }

        private void Apply()
        {
            GameConfig.FullScreen = _full;
            GameConfig.WindowWidth = Sizes[_sizeIndex].Width;
            GameConfig.WindowHeight = Sizes[_sizeIndex].Height;
            PreviewColor();
            GameConfig.DrawDistance = _drawDistance;
            try
            {
                ((Game1)Engine.Instance.Game).ApplyDisplaySettings();
                GameConfig.SaveUserSettings();
                if (_back is OriginalGraphicsScreen graphics)
                    graphics.AcceptChanges();
                Engine.Instance.Screen = _back ?? new OptionsScreen();
            }
            catch (Exception ex)
            {
                _status = "Display change failed: " + ex.Message;
                GameConfig.DrawDistance = _originalDrawDistance;
                GameConfig.FullScreen = _originalFull;
                GameConfig.WindowWidth = _originalW;
                GameConfig.WindowHeight = _originalH;
                _scaling = _originalScaling;
                _previewOriginalColors = _savedOriginalColors;
                _previewVividColors = _savedVividColors;
                _gamma = _originalGamma;
                _brightness = _originalBrightness;
                _contrast = _originalContrast;
                PreviewColor();
            }
        }

        private void Cancel()
        {
            GameConfig.DrawDistance = _originalDrawDistance;
            _scaling = _originalScaling;
            _previewOriginalColors = _savedOriginalColors;
            _gamma = _originalGamma;
            _brightness = _originalBrightness;
            _contrast = _originalContrast;
            PreviewColor();
            Engine.Instance.Screen = _back ?? new OptionsScreen();
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape)) { Cancel(); return; }
            if (input.WasPressed(Keys.Down) || input.WasPressed(Keys.Tab))
                _selected = (_selected + 1) % Rows;
            else if (input.WasPressed(Keys.Up))
                _selected = (_selected + Rows - 1) % Rows;
            else if (input.WasPressed(Keys.Left)) Change(-1);
            else if (input.WasPressed(Keys.Right)) Change(1);
            else if (input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space))
                Change(1);

            if (!input.WasLeftMouseClicked) return;
            Vector2 p = input.GameMousePosition;
            if (p.X < 30 || p.X >= 620 || p.Y < 83 || p.Y >= 443)
                return;
            _selected = Math.Clamp(((int)p.Y - 83) / 36, 0, Rows - 1);
            Change(p.X < 340 ? -1 : 1);
        }

        private static string Signed(float number) => number.ToString("+0.00;-0.00;0.00");
        private string Value(int row)
        {
            switch (row)
            {
                case 0: return _full ? "FULLSCREEN" : "WINDOWED";
                case 1: return Sizes[_sizeIndex].Width + " x " + Sizes[_sizeIndex].Height;
                case 2: return new[] { "SMOOTH", "SHARP", "INTEGER PIXELS" }[_scaling];
                case 3: return _gamma.ToString("0.00");
                case 4: return Signed(_brightness);
                case 5: return _contrast.ToString("0.00");
                case 6: return _drawDistance == 2500 ? "LOW" :
                        _drawDistance == 5000 ? "MEDIUM" : "HIGH";
                case 7: return "RESTORE RAW COLORS";
                case 8: return "APPLY & SAVE";
                default: return "CANCEL";
            }
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            sprite.Draw(_background, new Rectangle(0,0,640,480), Color.White);
            sprite.Draw(_pixel, new Rectangle(0,0,640,480), Color.Black * .82f);
            Label(sprite, "OPENNFS1 SE RELOADED  /  DISPLAY", 28, 24, 0.72f, Color.White);
            Label(sprite, "F7: vivid  F8: raw colors  F10: window  F11: fullscreen", 30, 60, .35f, Color.LightGray);
            string[] labels = { "SCREEN", "WINDOW SIZE", "UPSCALING",
                "GAMMA", "BRIGHTNESS", "CONTRAST",
                "DRAW DISTANCE", "RESET", "SAVE", "BACK" };
            for (int i = 0; i < Rows; i++)
            {
                int y = 83 + i * 36;
                if (_selected == i)
                    sprite.Draw(_pixel, new Rectangle(25,y,590,34), Color.DarkSlateGray * 0.95f);
                Label(sprite, labels[i], 40, y+7, .45f,
                    _selected == i ? Color.Yellow : Color.LightGray);
                Label(sprite, Value(i), 332, y+7, .45f,
                    _selected == i ? Color.Yellow : Color.White);
            }
            Label(sprite, _status, 28, 461, .29f, Color.LightGray);
            sprite.End();
        }

        private void Label(SpriteBatch sprite, string text, int x, int y, float scale, Color color)
        {
            sprite.DrawString(_font, text, new Vector2(x,y), color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }
}
