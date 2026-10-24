using System;
using System.IO;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;
using OpenNFS1.Vehicles;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// Original NFS SE car showroom. Bottom PREV / NEXT switch the vehicle
    /// on every page, while the small in-image arrows change local illustrations.
    /// </summary>
    internal sealed class VehicleShowroomScreen : IGameScreen
    {
        private readonly ShowroomNavigator _nav;
        private readonly SpriteFont _font;
        private BitmapChunk _art;
        private string _message;
        private bool _previewFallback;

        public VehicleShowroomScreen(VehicleDescription selectedCar)
        {
            _nav = new ShowroomNavigator(selectedCar?.Name);
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            // Warrior has no original FRONTEND/SHOW artwork. A temporary RX7
            // preview must not silently change the vehicle chosen for the race.
            _previewFallback = selectedCar != null &&
                !Array.Exists(ShowroomNavigator.Cars, name => string.Equals(
                    name, selectedCar.Name, StringComparison.OrdinalIgnoreCase));
            if (!_previewFallback) SyncSelectedVehicle();
            LoadArt();
            if (_previewFallback)
                _message = "No original Warrior showroom; showing RX7. Use NEXT to select a car.";
        }

        private static string PageFile(string car, ShowroomNavigator.Page page)
        {
            string baseName = car switch
            {
                "Supra" => "SUPRA",
                "ZR1" => "VETTE",
                "Viper" => "VIPER",
                "F512" => "512TR",
                "Diablo" => "DIABLO",
                _ => car
            };
            if (page == ShowroomNavigator.Page.Index) return baseName + ".QFS";

            string prefix = car switch
            {
                "Supra" => "SUP",
                "ZR1" => "ZR1",
                "Viper" => "VIP",
                "F512" => "512",
                "Diablo" => "DIA",
                _ => car
            };
            string suffix = page switch
            {
                ShowroomNavigator.Page.General => "GEN",
                ShowroomNavigator.Page.History => "HIS",
                ShowroomNavigator.Page.Mechanical => "MEC",
                ShowroomNavigator.Page.Performance => "PER",
                _ => ""
            };
            return prefix + suffix + ".QFS";
        }

        private void SyncSelectedVehicle()
        {
            GameConfig.SelectedVehicle = VehicleDescription.Descriptions.Find(
                car => string.Equals(car.Name, _nav.CarName, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadArt()
        {
            string file = PageFile(_nav.CarName, _nav.CurrentPage);
            string path = Path.Combine(GameConfig.CdDataPath, "FRONTEND", "SHOW", file);
            if (!File.Exists(path))
            {
                _message = "Missing original showroom artwork: " + file;
                _art = null;
                return;
            }
            try
            {
                _art = new QfsFile(Path.Combine("FRONTEND", "SHOW", file)).Content.Header;
                _message = null;
            }
            catch (IOException ex)
            {
                _art = null;
                _message = "Unable to read " + file + ": " + ex.Message;
            }
        }

        private void ChangeCar(int delta)
        {
            _nav.ChangeCar(delta);
            _previewFallback = false;
            SyncSelectedVehicle();
            LoadArt(); // Retain the chosen detail page for the NEXT actual car
        }

        private void OpenPage(ShowroomNavigator.Page page)
        {
            _nav.GoTo(page);
            LoadArt();
        }

        private void Back()
        {
            if (_nav.Back())
                Engine.Instance.Screen = new HomeScreen();
            else
                LoadArt();
        }

        private BitmapEntry Find(string key) => _art?.FindByName(key);

        private void DrawArt(string key)
        {
            BitmapEntry image = Find(key);
            if (image?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(image.Texture, image.GetDisplayAt(), Color.White);
        }

        private bool Hit(string id, Vector2 point)
        {
            BitmapEntry image = Find(id);
            if (image?.Texture == null || point.X < 0) return false;
            Vector2 at = image.GetDisplayAt();
            return new Rectangle((int)at.X, (int)at.Y,
                image.Texture.Width, image.Texture.Height).Contains((int)point.X, (int)point.Y);
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape)) { Back(); return; }

            // Bottom PREV and NEXT always change the CAR, never the photo.
            if (input.WasPressed(Keys.Left)) { ChangeCar(-1); return; }
            if (input.WasPressed(Keys.Right)) { ChangeCar(1); return; }

            // In-page navigation is separate from changing the selected car.
            if (input.WasPressed(Keys.A))
                ChangeLocal(-1, false);
            if (input.WasPressed(Keys.D))
                ChangeLocal(1, false);
            if (input.WasPressed(Keys.W))
                ChangeLocal(-1, true);
            if (input.WasPressed(Keys.S))
                ChangeLocal(1, true);

            if (input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space))
            {
                if (_nav.CurrentPage == ShowroomNavigator.Page.Index)
                    OpenPage(ShowroomNavigator.Page.General);
                else Back();
                return;
            }
            if (_nav.CurrentPage == ShowroomNavigator.Page.Index)
            {
                if (input.WasPressed(Keys.G)) OpenPage(ShowroomNavigator.Page.General);
                else if (input.WasPressed(Keys.H)) OpenPage(ShowroomNavigator.Page.History);
                else if (input.WasPressed(Keys.M)) OpenPage(ShowroomNavigator.Page.Mechanical);
                else if (input.WasPressed(Keys.P)) OpenPage(ShowroomNavigator.Page.Performance);
            }

            if (!input.WasLeftMouseClicked) return;
            Vector2 p = input.GameMousePosition;
            if (p.X < 0) return;

            if (Hit("dond", p) || Hit("donl", p)) { Back(); return; }
            if (Hit("pred", p) || Hit("prel", p)) { ChangeCar(-1); return; }
            if (Hit("nexd", p) || Hit("nexl", p)) { ChangeCar(1); return; }

            switch (_nav.CurrentPage)
            {
                case ShowroomNavigator.Page.Index:
                    if (Hit("gend", p) || Hit("genl", p)) OpenPage(ShowroomNavigator.Page.General);
                    else if (Hit("hisd", p) || Hit("hisl", p)) OpenPage(ShowroomNavigator.Page.History);
                    else if (Hit("engd", p) || Hit("engl", p)) OpenPage(ShowroomNavigator.Page.Mechanical);
                    else if (Hit("perd", p) || Hit("perl", p)) OpenPage(ShowroomNavigator.Page.Performance);
                    else if (Hit("vidd", p) || Hit("vidl", p))
                        _message = "Original TGV car presentation is not implemented yet.";
                    break;
                case ShowroomNavigator.Page.General:
                    // There are TWO independent photo carousels, each 5 images.
                    if (Hit("f1lf", p)) _nav.ChangePhoto(false, -1);
                    else if (Hit("f1rt", p)) _nav.ChangePhoto(false, 1);
                    else if (Hit("f2lf", p)) _nav.ChangePhoto(true, -1);
                    else if (Hit("f2rt", p)) _nav.ChangePhoto(true, 1);
                    break;
                case ShowroomNavigator.Page.Mechanical:
                case ShowroomNavigator.Page.Performance:
                    if (Hit("left", p)) _nav.ChangeSection(-1);
                    else if (Hit("righ", p)) _nav.ChangeSection(1);
                    break;
            }
        }

        private void ChangeLocal(int delta, bool upper)
        {
            if (_nav.CurrentPage == ShowroomNavigator.Page.General)
                _nav.ChangePhoto(upper, delta);
            else
                _nav.ChangeSection(delta);
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            DrawArt("bgnd");

            switch (_nav.CurrentPage)
            {
                case ShowroomNavigator.Page.Index:
                    DrawArt("gend"); DrawArt("hisd"); DrawArt("engd");
                    DrawArt("perd"); DrawArt("vidd");
                    break;
                case ShowroomNavigator.Page.General:
                    // The opaque frame/backdrop must go UNDER the photos.
                    DrawArt("fr1d"); DrawArt("fr2d");
                    DrawArt("f1s" + _nav.BottomPhoto);
                    DrawArt("f2s" + _nav.TopPhoto);
                    DrawArt("f1lf"); DrawArt("f1rt");
                    DrawArt("f2lf"); DrawArt("f2rt");
                    break;
                case ShowroomNavigator.Page.Mechanical:
                    DrawArt("frmd");
                    DrawArt(new[] { "chas", "engn", "driv" }[_nav.Section]);
                    DrawArt("left"); DrawArt("righ");
                    break;
                case ShowroomNavigator.Page.Performance:
                    DrawArt("frmd");
                    DrawArt(new[] { "acc1", "bra1", "han1" }[_nav.Section]);
                    DrawArt(new[] { "accl", "brak", "hand" }[_nav.Section]);
                    DrawArt("left"); DrawArt("righ");
                    break;
            }

            DrawArt("pred"); DrawArt("nexd"); DrawArt("dond");
            if (!string.IsNullOrWhiteSpace(_message))
            {
                sprite.DrawString(_font, _message, new Vector2(24, 405),
                    Color.Yellow, 0, Vector2.Zero, 0.30f, SpriteEffects.None, 0);
            }
            sprite.End();
        }
    }
}
