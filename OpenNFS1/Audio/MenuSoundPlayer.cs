using System;
using GameEngine;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Audio;
using OpenNFS1.Parsers.Audio;
using OpenNFS1.UI.Screens;

namespace OpenNFS1
{
    /// <summary>
    /// Native front-end UI sound effects from the user's FRONT_MW.BNK.
    /// Unlike AS4/ASF music, BNK samples decode with the existing parser;
    /// no FFmpeg or new asset downloads are necessary.
    /// </summary>
    internal sealed class MenuSoundPlayer : IDisposable
    {
        public static MenuSoundPlayer Instance { get; } = new MenuSoundPlayer();

        private SoundEffect _move, _select;
        private bool _initialized;
        private bool _available;
        private MenuSoundPlayer() { }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                var samples = new BnkFile("FRONT_MW.BNK").Samples;
                if (samples.Count < 2)
                    throw new InvalidOperationException("FRONT_MW.BNK has no UI sound samples.");
                _move = VehicleAudioProvider.CreateEffect(samples[0]);
                _select = VehicleAudioProvider.CreateEffect(samples[1]);
                _available = true;
            }
            catch (Exception error)
            {
                // UI should still work if the original sound bank is absent.
                AudioDiagnostics.Record(error);
            }
        }

        public void Update()
        {
            if (!GameConfig.SoundEffectsEnabled || !Engine.Instance.Game.IsActive) return;
            if (Engine.Instance.Screen is DoRaceScreen ||
                Engine.Instance.Screen is LoadRaceScreen ||
                Engine.Instance.Screen is OpenNFS1SplashScreen ||
                Engine.Instance.Screen is ChooseDataDownloadScreen)
                return;

            var input = Engine.Instance.Input;
            bool select = input.WasLeftMouseClicked || input.WasPressed(Keys.Enter) ||
                input.WasPressed(Keys.Space) || input.WasPressed(Keys.Escape);
            bool move = input.WasPressed(Keys.Left) || input.WasPressed(Keys.Right) ||
                input.WasPressed(Keys.Up) || input.WasPressed(Keys.Down) ||
                input.WasPressed(Keys.Tab) || input.WasPressed(Keys.A) ||
                input.WasPressed(Keys.D) || input.WasPressed(Keys.W) ||
                input.WasPressed(Keys.S) || input.WasPressed(Keys.G) ||
                input.WasPressed(Keys.H) || input.WasPressed(Keys.M) ||
                input.WasPressed(Keys.P);
            if (!select && !move) return;

            Initialize();
            if (!_available) return;
            try
            {
                if (select) _select.Play(0.48f, 0f, 0f);
                else _move.Play(0.40f, 0f, 0f);
            }
            catch (Exception ex) when (ex is InvalidOperationException ||
                                        ex is ObjectDisposedException)
            {
                AudioDiagnostics.Record(ex);
            }
        }

        public void Dispose()
        {
            _move?.Dispose();
            _select?.Dispose();
            _move = null;
            _select = null;
            _initialized = false;
            _available = false;
        }
    }
}
