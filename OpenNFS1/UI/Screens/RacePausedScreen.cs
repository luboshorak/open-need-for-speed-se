using System;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// The actual NFS SE 640x480 PAUSE.QFS screen. The six lines and their
    /// selected/deselected states are bitmap entries at original CD positions.
    /// No arbitrary font, replacement background or manually drawn labels.
    /// </summary>
    internal sealed class RacePausedScreen : IGameScreen
    {
        private readonly DoRaceScreen _race;
        private readonly BitmapChunk _art;
        private int _selected;

        private static readonly string[] Normal =
            { "cond", "repd", "audd", "grad", "chad", "quid" };
        private static readonly string[] Lit =
            { "conl", "repl", "audl", "gral", "chal", "quil" };
        private static readonly int[] ItemY = { 110, 160, 208, 256, 304, 352 };

        public RacePausedScreen(DoRaceScreen race)
        {
            _race = race;
            _art = new QfsFile(@"FRONTEND\ART\CHECK\PAUSE.QFS").Content.Header;
            if (_art.FindByName("bgnd")?.Texture == null)
                throw new InvalidOperationException("Missing original NFS SE PAUSE.QFS background.");
            foreach (string id in Normal)
                RequireArt(id);
            foreach (string id in Lit)
                RequireArt(id);
        }

        private void RequireArt(string id)
        {
            if (_art.FindByName(id)?.Texture == null)
                throw new InvalidOperationException("Original NFS SE PAUSE.QFS bitmap missing: " + id);
        }

        private void Art(string id)
        {
            BitmapEntry entry = _art.FindByName(id);
            Engine.Instance.SpriteBatch.Draw(entry.Texture, entry.GetDisplayAt(), Color.White);
        }

        private void ContinueRace()
        {
            _race.Resume();
            Engine.Instance.Screen = _race;
        }

        private void Activate()
        {
            switch (_selected)
            {
                case 0: ContinueRace(); break;
                // Original replay data playback is not yet supported.
                case 1: break;
                case 2: Engine.Instance.Screen = new AudioOptionsScreen(this); break;
                case 3: Engine.Instance.Screen = new OriginalGraphicsScreen(this); break;
                // Chat mode is for original network support, still unmapped.
                case 4: break;
                case 5:
                    VehicleController.ForceBrake = false;
                    Engine.Instance.Screen = new HomeScreen();
                    break;
            }
        }

        public void Update(GameTime gameTime)
        {
            var input = Engine.Instance.Input;
            if (UIController.Back || input.WasPressed(Keys.P))
            {
                ContinueRace();
                return;
            }
            if (UIController.Up)
                _selected = (_selected + ItemY.Length - 1) % ItemY.Length;
            else if (UIController.Down || input.WasPressed(Keys.Tab))
                _selected = (_selected + 1) % ItemY.Length;
            if (input.WasLeftMouseClicked)
            {
                Vector2 point = input.GameMousePosition;
                for (int i = 0; i < ItemY.Length; i++)
                {
                    var entry = _art.FindByName(Normal[i]);
                    if (point.X >= entry.GetDisplayAt().X &&
                        point.X < entry.GetDisplayAt().X + entry.Texture.Width &&
                        point.Y >= ItemY[i] &&
                        point.Y < ItemY[i] + entry.Texture.Height)
                    {
                        _selected = i;
                        Activate();
                        return;
                    }
                }
            }
            if (UIController.Ok)
                Activate();
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            Art("bgnd");
            for (int i = 0; i < Normal.Length; i++)
                Art(i == _selected ? Lit[i] : Normal[i]);
            sprite.End();
        }
    }
}
