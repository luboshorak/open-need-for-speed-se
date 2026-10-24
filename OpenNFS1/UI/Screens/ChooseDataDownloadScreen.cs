using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GameEngine;

namespace OpenNFS1.UI.Screens
{
    // No automatic downloads of proprietary files; guide users to their own CD.
    class ChooseDataDownloadScreen : BaseUIScreen, IGameScreen
    {
        public void Update(GameTime gameTime)
        {
            if (Engine.Instance.Input.WasPressed(Keys.Enter) && GameConfig.HasOriginalGameData())
                Engine.Instance.Screen = new HomeScreen();
            else if (Engine.Instance.Input.WasPressed(Keys.Escape))
                Engine.Instance.Game.Exit();
        }

        public override void Draw()
        {
            base.Draw();
            WriteLine("Original game data missing", Color.Red, 0, 30, TextSize);
            WriteLine("Copy files from your Need for Speed SE CD", TextColor, 65, 30, TextSize);
            WriteLine("into the CD_Data folder beside the game.", TextColor, 30, 30, TextSize);
            WriteLine("Required example directories:", TextColor, 50, 30, TextSize);
            WriteLine("FRONTEND/ART/CONTROL", TextColor, 30, 50, TextSize);
            WriteLine("SIMDATA/MISC and SIMDATA/CARFAMS", TextColor, 30, 50, TextSize);
            WriteLine("Or configure cdDataPath in gameconfig.json", TextColor, 50, 30, TextSize);
            WriteLine("Enter: Retry      Esc: Quit", Color.Yellow, 65, 30, TextSize);
            Engine.Instance.SpriteBatch.End();
        }
    }
}
