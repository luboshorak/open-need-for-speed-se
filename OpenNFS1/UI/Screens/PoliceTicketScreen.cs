using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;

namespace OpenNFS1.UI.Screens
{
    // Arrest/game over uses the frontend image. Warning and speeding pictures
    // remain in RaceUI while the race simulation continues.
    internal sealed class PoliceTicketScreen : IGameScreen
    {
        private readonly BitmapEntry _background;
        public PoliceTicketScreen(Race race, IGameScreen back)
        {
            _background = new QfsFile(@"FRONTEND\ART\TICKET.QFS")
                .Content.Header.FindByName("bgnd");
        }
        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Escape) ||
                input.WasPressed(Keys.Space) || input.WasLeftMouseClicked)
            {
                VehicleController.ForceBrake = false;
                Engine.Instance.Screen = new HomeScreen();
            }
        }
        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            Engine.Instance.Device.Clear(Color.Black);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp);
            if (_background?.Texture != null)
                sprite.Draw(_background.Texture, _background.GetDisplayAt(), Color.White);
            sprite.End();
        }
    }
}
