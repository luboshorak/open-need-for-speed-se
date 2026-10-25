using System;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace OpenNFS1.UI.Screens
{
    // Functional pause menu. Keep the live race object intact.
    internal sealed class RacePausedScreen : BaseUIScreen, IGameScreen
    {
        private readonly DoRaceScreen _race;
        private readonly string[] _items =
        {
            "CONTINUE", "REPLAY", "AUDIO", "GRAPHICS DETAIL",
            "CHAT MODE", "QUIT CURRENT GAME"
        };
        private int _selected;

        public RacePausedScreen(DoRaceScreen race) { _race = race; }

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
                case 1: // Replay is not implemented; do not mislabel a new race as a replay.
                case 2:
                case 3:
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
            if (UIController.Up) _selected = (_selected + _items.Length - 1) % _items.Length;
            if (UIController.Down) _selected = (_selected + 1) % _items.Length;
            if (input.WasLeftMouseClicked)
            {
                var p = input.GameMousePosition;
                if (p.X >= 10 && p.X < 360 && p.Y >= 160 && p.Y < 160 + _items.Length * 48)
                {
                    _selected = Math.Clamp(((int)p.Y - 160) / 48, 0, _items.Length - 1);
                    Activate();
                    return;
                }
            }
            if (UIController.Ok) Activate();
        }

        public override void Draw()
        {
            base.Draw();
            WriteLine("PAUSE", Color.Red, 16, 14, 0.90f);
            for (int i = 0; i < _items.Length; i++)
                WriteLine(_items[i], i == _selected, 48, 14, .58f);
            Engine.Instance.SpriteBatch.End();
        }
    }
}
