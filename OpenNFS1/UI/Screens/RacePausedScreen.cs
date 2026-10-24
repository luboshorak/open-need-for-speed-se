using System;
using System.Collections.Generic;
using System.Text;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenNFS1.Physics;

namespace OpenNFS1.UI.Screens
{
    class RacePausedScreen : BaseUIScreen, IGameScreen
    {
        DoRaceScreen _currentRace;
        int _selectedOption = 0;

        public RacePausedScreen(DoRaceScreen currentRace)
            : base()
        {
            _currentRace = currentRace;
        }

        #region IDrawableObject Members

        public void Update(GameTime gameTime)
        {
			//Engine.Instance.Device.Viewport = FullViewport;

            if (UIController.Up && _selectedOption > 0)
                _selectedOption--;
            else if (UIController.Down && _selectedOption < 1)
                _selectedOption++;

            if (Engine.Instance.Input.WasLeftMouseClicked)
            {
                var point = Engine.Instance.Input.GameMousePosition;
                if (point.X >= 20 && point.X <= 350 && point.Y >= 75 && point.Y <= 120)
                {
                    ContinueRace();
                    return;
                }
                if (point.X >= 20 && point.X <= 350 && point.Y > 120 && point.Y <= 178)
                {
                    Engine.Instance.Screen = new HomeScreen();
                    return;
                }
            }

            if (UIController.Back)
            {
                ContinueRace();
                return;
            }

            if (UIController.Ok)
            {
                if (_selectedOption == 0)
                {
                    ContinueRace();
                }
                else
                {
                    Engine.Instance.Screen = new HomeScreen();
                }
            }
        }

		private void ContinueRace()
        {
            _currentRace.Resume();
            Engine.Instance.Screen = _currentRace;
        }

		public override void Draw()
		{
			base.Draw();
			
			WriteLine("Race paused", Color.White, 20, 30, TitleSize);
			WriteLine("Continue", _selectedOption == 0, 60, 30, SectionSize);
			WriteLine("Main menu", _selectedOption == 1, 40, 30, SectionSize);

            Engine.Instance.SpriteBatch.End();
        }

        #endregion
    }
}
