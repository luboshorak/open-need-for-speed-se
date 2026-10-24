using System;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Original 1996 OPTIONS.QFS artwork and button positions. No replacement
    /// menu backgrounds or invented coordinates for source bitmap elements.
    /// </summary>
    internal sealed class OptionsScreen : IGameScreen
    {
        private readonly BitmapChunk _art;
        private readonly SpriteFont _font;
        private readonly bool _manualBefore, _absBefore, _tractionBefore;
        private readonly int _distanceBefore;
        private readonly bool _musicBefore, _menuMusicBefore, _soundBefore, _rockBefore;
        private readonly float _volumeBefore;
        private int _selected;
        private string _message;

        private static readonly int[] RowY = { 107, 140, 175, 208, 243, 277, 313, 348, 442 };
        private static readonly string[] Normal =
            { "Trad", "Absd", "Trcd", "Audd", "Grad", "Cond", "Load", "Cred", "Dond" };
        private static readonly string[] Active =
            { "Tral", "Absl", "Trcl", "Audl", "Gral", "Conl", "Loal", "Crel", "Donl" };

        public OptionsScreen()
        {
            _art = new QfsFile(@"FRONTEND\ART\OPTION\OPTIONS.QFS").Content.Header;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _manualBefore = GameConfig.ManualGearbox;
            _absBefore = GameConfig.AbsEnabled;
            _tractionBefore = GameConfig.TractionControlEnabled;
            _distanceBefore = GameConfig.DrawDistance;
            _musicBefore = GameConfig.MusicEnabled;
            _menuMusicBefore = GameConfig.MenuMusicEnabled;
            _soundBefore = GameConfig.SoundEffectsEnabled;
            _rockBefore = GameConfig.RockMusic;
            _volumeBefore = GameConfig.MusicVolume;
        }

        private void DrawArt(string id)
        {
            var bitmap = _art.FindByName(id);
            if (bitmap?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(bitmap.Texture, bitmap.GetDisplayAt(), Color.White);
        }

        private void DrawText(string message, int x, int y, float scale = 0.37f)
        {
            Engine.Instance.SpriteBatch.DrawString(_font, message, new Vector2(x, y),
                Color.DarkRed, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private void Cancel()
        {
            GameConfig.ManualGearbox = _manualBefore;
            GameConfig.AbsEnabled = _absBefore;
            GameConfig.TractionControlEnabled = _tractionBefore;
            GameConfig.DrawDistance = _distanceBefore;
            GameConfig.MusicEnabled = _musicBefore;
            GameConfig.MenuMusicEnabled = _menuMusicBefore;
            GameConfig.SoundEffectsEnabled = _soundBefore;
            GameConfig.RockMusic = _rockBefore;
            GameConfig.MusicVolume = _volumeBefore;
            Engine.Instance.Screen = new HomeScreen();
        }

        private void Done()
        {
            GameConfig.SaveUserSettings();
            Engine.Instance.Screen = new HomeScreen();
        }

        private void Change(int direction)
        {
            _message = null;
            switch (_selected)
            {
                case 0: GameConfig.ManualGearbox = direction > 0; break;
                case 1: GameConfig.AbsEnabled = !GameConfig.AbsEnabled; break;
                case 2: GameConfig.TractionControlEnabled = !GameConfig.TractionControlEnabled; break;
                case 4:
                    Engine.Instance.Screen = new OriginalGraphicsScreen(this);
                    break;
                default: Activate(); break;
            }
        }

        private void Activate()
        {
            switch (_selected)
            {
                case 3:
                    Engine.Instance.Screen = new AudioOptionsScreen(this);
                    break;
                case 5:
                    _message = "Driving: arrows / WASD, Enter, Esc, M, R. Remapping pending.";
                    break;
                case 6:
                    Engine.Instance.Screen = new LoadSaveScreen(this);
                    break;
                case 7:
                    Engine.Instance.Screen = new CreditsScreen(this);
                    break;
                case 8: Done(); break;
                default: ChangeOnEnter(); break;
            }
        }

        private void ChangeOnEnter()
        {
            if (_selected == 0) GameConfig.ManualGearbox = !GameConfig.ManualGearbox;
            else if (_selected <= 2 || _selected == 4) Change(1);
        }

        public void Update(GameTime gameTime)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape)) { Cancel(); return; }
            if (input.WasPressed(Keys.Down) || input.WasPressed(Keys.Tab))
                _selected = (_selected + 1) % RowY.Length;
            else if (input.WasPressed(Keys.Up))
                _selected = (_selected + RowY.Length - 1) % RowY.Length;
            else if (input.WasPressed(Keys.Left)) Change(-1);
            else if (input.WasPressed(Keys.Right)) Change(1);
            else if (input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space)) Activate();

            if (input.WasLeftMouseClicked)
            {
                Vector2 p = input.GameMousePosition;
                if (p.X < 0) return;
                if (p.Y >= 432)
                {
                    if (p.X >= 490) { Cancel(); return; }
                    if (p.X < 135) { Done(); return; }
                }
                for (int i = 0; i < RowY.Length - 1; i++)
                {
                    if (p.Y >= RowY[i] - 7 && p.Y <= RowY[i] + 29)
                    {
                        _selected = i;
                        if (i == 0) GameConfig.ManualGearbox = p.X >= 430;
                        else if (i <= 2 || i == 4) Change(1);
                        else Activate();
                        return;
                    }
                }
            }
        }

        public void Draw()
        {
            var sprite = Engine.Instance.SpriteBatch;
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
            DrawArt("bgnd");
            for (int i = 0; i < Normal.Length; i++)
                DrawArt(i == _selected ? Active[i] : Normal[i]);
            DrawArt("Cand");
            DrawArt(GameConfig.ManualGearbox ? "Mand" : "Autd");
            DrawArt(GameConfig.AbsEnabled ? "On1d" : "Of1d");
            DrawArt(GameConfig.TractionControlEnabled ? "On2d" : "Of2d");

            if (!string.IsNullOrEmpty(_message))
                DrawText(_message, 24, 394, 0.30f);
            sprite.End();
        }
    }
}
