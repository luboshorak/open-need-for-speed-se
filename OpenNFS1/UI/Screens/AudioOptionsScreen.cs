using System;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>The original AUDIO.QFS submenu, with functional music choices.</summary>
    internal sealed class AudioOptionsScreen : IGameScreen
    {
        private readonly BitmapChunk _art;
        private readonly IGameScreen _parent;
        private readonly SpriteFont _font;
        private readonly bool _musicBefore, _menuBefore, _soundBefore, _rockBefore;
        private readonly float _volumeBefore;
        private int _selected;

        private static readonly int[] Y = { 104, 146, 189, 232, 276, 319, 362, 442 };
        private static readonly string[] Off = { "Sond", "Racd", "Mend", "Soud", "Sped", "Audd", "3ded", "Dond" };
        private static readonly string[] On = { "Sonl", "Racl", "Menl", "Soul", "Spel", "Audl", "3del", "Donl" };

        public AudioOptionsScreen(IGameScreen parent)
        {
            _parent = parent;
            _art = new QfsFile(@"FRONTEND\ART\OPTION\AUDIO.QFS").Content.Header;
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            _musicBefore = GameConfig.MusicEnabled;
            _menuBefore = GameConfig.MenuMusicEnabled;
            _soundBefore = GameConfig.SoundEffectsEnabled;
            _rockBefore = GameConfig.RockMusic;
            _volumeBefore = GameConfig.MusicVolume;
        }

        private void DrawArt(string id)
        {
            BitmapEntry bitmap = _art.FindByName(id);
            if (bitmap?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(bitmap.Texture, bitmap.GetDisplayAt(), Color.White);
        }

        private void Label(string s, int y, float scale = 0.40f)
        {
            Engine.Instance.SpriteBatch.DrawString(_font, s, new Vector2(317, y),
                Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private void Change(int direction)
        {
            switch (_selected)
            {
                case 0: GameConfig.MusicEnabled = !GameConfig.MusicEnabled; break;
                case 1: GameConfig.RockMusic = !GameConfig.RockMusic; break;
                case 2: GameConfig.MenuMusicEnabled = !GameConfig.MenuMusicEnabled; break;
                case 3: GameConfig.SoundEffectsEnabled = !GameConfig.SoundEffectsEnabled; break;
                case 4:
                    GameConfig.MusicVolume = Math.Clamp(GameConfig.MusicVolume + direction * 0.1f, 0f, 1f);
                    break;
                // Sound card and surround effects remain unsupported by MonoGame.
            }
        }

        private void Done()
        {
            Engine.Instance.Screen = _parent;
        }

        private void Cancel()
        {
            GameConfig.MusicEnabled = _musicBefore;
            GameConfig.MenuMusicEnabled = _menuBefore;
            GameConfig.SoundEffectsEnabled = _soundBefore;
            GameConfig.RockMusic = _rockBefore;
            GameConfig.MusicVolume = _volumeBefore;
            Engine.Instance.Screen = _parent;
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape)) { Cancel(); return; }
            if (input.WasPressed(Keys.Down) || input.WasPressed(Keys.Tab)) _selected = (_selected + 1) % Y.Length;
            else if (input.WasPressed(Keys.Up)) _selected = (_selected + Y.Length - 1) % Y.Length;
            else if (input.WasPressed(Keys.Left)) Change(-1);
            else if (input.WasPressed(Keys.Right)) Change(1);
            else if (input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space))
            {
                if (_selected == 7) Done(); else Change(1);
            }
            if (input.WasLeftMouseClicked)
            {
                Vector2 p = input.GameMousePosition;
                if (p.X < 0) return;
                if (p.Y >= 432)
                {
                    if (p.X >= 490) { Cancel(); return; }
                    if (p.X <= 145) { Done(); return; }
                }
                for (int i = 0; i < Y.Length - 1; i++)
                    if (p.Y >= Y[i] - 7 && p.Y <= Y[i] + 30)
                    {
                        _selected = i;
                        Change(1);
                        return;
                    }
            }
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
            DrawArt("bgnd");
            for (int i = 0; i < Y.Length; i++) DrawArt(i == _selected ? On[i] : Off[i]);
            DrawArt("Cand");

            Label(GameConfig.MusicEnabled ? "ON" : "OFF", 106);
            Label(GameConfig.RockMusic ? "ROCK" : "TECHNO", 149);
            Label(GameConfig.MenuMusicEnabled ? "ON" : "OFF", 192);
            Label(GameConfig.SoundEffectsEnabled ? "ON" : "OFF", 235);
            Label(((int)(GameConfig.MusicVolume * 100)).ToString() + "%", 279);
            Label("AUTO", 321, 0.31f);
            Label("STEREO", 365, 0.31f);

            string message = OriginalMusicPlayer.Instance.Status;
            if (!string.IsNullOrWhiteSpace(message))
            {
                if (message.Length > 70) message = message.Substring(0, 67) + "...";
                sprite.DrawString(_font, message, new Vector2(15, 404), Color.Yellow,
                    0f, Vector2.Zero, 0.29f, SpriteEffects.None, 0);
            }
            sprite.End();
        }
    }
}
