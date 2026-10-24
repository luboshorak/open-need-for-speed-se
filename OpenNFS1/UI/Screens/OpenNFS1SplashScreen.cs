using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameEngine;

namespace OpenNFS1.UI.Screens
{
	class OpenNFS1SplashScreen : BaseUIScreen, IGameScreen
	{
		float _elapsedTime;
		public void Update(GameTime gameTime)
		{
			_elapsedTime += Engine.Instance.FrameTime;
			if (Engine.Instance.Input.WasPressed(Keys.Enter) ||
                Engine.Instance.Input.WasLeftMouseClicked)
			{
				if (!GameConfig.HasOriginalGameData())
					Engine.Instance.Screen = new ChooseDataDownloadScreen();
				else
				{
					Engine.Instance.Screen = PlayerProfile.IsNamed ?
                            new GameModeScreen() : new PlayerNameScreen();
				}
			}
		}

        public override void Draw()
        {
            base.Draw();

            WriteLine("OPENNFS1 SE RELOADED", Color.Yellow, 28, 30, 0.91f);
            WriteLine("Need for Speed Special Edition - modern redevelopment",
                Color.White, 63, 30, 0.52f);
            WriteLine("Modernization, new game systems and ongoing development:",
                Color.LightGray, 48, 30, 0.43f);
            WriteLine("LUBOS HORAK", Color.Yellow, 34, 30, 0.70f);

            WriteLine("Built on the original OpenNFS1 engine by Jeff Harris.",
                Color.LightGray, 64, 30, 0.39f);
            WriteLine("Original NFS SE game and assets: Electronic Arts.",
                Color.LightGray, 35, 30, 0.39f);
            WriteLine("An unofficial fan project - not affiliated with EA.",
                Color.LightGray, 29, 30, 0.37f);

            WriteLine("ENTER OR CLICK TO CONTINUE", Color.Yellow, 84, 30, 0.53f);
            Engine.Instance.SpriteBatch.End();
        }

	}
}
