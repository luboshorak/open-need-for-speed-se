using System;
using System.IO;
using System.Linq;
using GameEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OpenNFS1.Parsers;
using OpenNFS1.Vehicles;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// The original car showroom. All photographic/illustrated layers are
    /// loaded directly from the owner's FRONTEND/SHOW QFS files.
    /// </summary>
    internal sealed class VehicleShowroomScreen : IGameScreen
    {
        private enum Page { Index, General, History, Mechanical, Performance }
        private static readonly string[] ShowCars =
            { "RX7", "NSX", "SUPRA", "911", "VETTE", "VIPER", "512TR", "DIABLO" };

        private int _vehicleIndex;
        private int _variant;
        private Page _page;
        private BitmapChunk _art;
        private SpriteFont _font;
        private string _message;

        public VehicleShowroomScreen(VehicleDescription selectedCar)
        {
            _vehicleIndex = Math.Max(0, VehicleDescription.Descriptions.FindIndex(
                car => car == selectedCar));
            if (_vehicleIndex >= ShowCars.Length)
                _vehicleIndex = 0; // Warrior lacks original showroom artwork
            _font = Engine.Instance.ContentManager.Load<SpriteFont>("ArialBlack-Italic");
            LoadPage(Page.Index);
        }

        private static string GetPageFile(int carIndex, Page page)
        {
            string baseName = ShowCars[carIndex];
            if (page == Page.Index) return baseName + ".QFS";

            // The source CD uses abbreviated stems for the four submenus.
            string prefix = baseName switch
            {
                "SUPRA" => "SUP",
                "VETTE" => "ZR1",
                "VIPER" => "VIP",
                "DIABLO" => "DIA",
                "512TR" => "512",
                _ => baseName
            };
            string suffix = page switch
            {
                Page.General => "GEN",
                Page.History => "HIS",
                Page.Mechanical => "MEC",
                Page.Performance => "PER",
                _ => ""
            };
            return prefix + suffix + ".QFS";
        }

        private void LoadPage(Page page)
        {
            _page = page;
            _variant = 0;
            _message = null;
            string path = Path.Combine(GameConfig.CdDataPath, "FRONTEND", "SHOW",
                GetPageFile(_vehicleIndex, page));
            if (!File.Exists(path))
            {
                _message = "The original showroom artwork is missing: " + Path.GetFileName(path);
                if (page != Page.Index) LoadPage(Page.Index);
                return;
            }
            _art = new QfsFile(Path.Combine("FRONTEND", "SHOW",
                GetPageFile(_vehicleIndex, page))).Content.Header;
        }

        private BitmapEntry Find(string key) => _art?.FindByName(key);

        private void DrawArt(string key)
        {
            BitmapEntry entry = Find(key);
            if (entry?.Texture != null)
                Engine.Instance.SpriteBatch.Draw(entry.Texture, entry.GetDisplayAt(), Color.White);
        }

        private bool Hit(string key, Vector2 point)
        {
            var image = Find(key);
            if (image?.Texture == null || point.X < 0) return false;
            Vector2 pos = image.GetDisplayAt();
            return new Rectangle((int)pos.X, (int)pos.Y,
                image.Texture.Width, image.Texture.Height).Contains((int)point.X, (int)point.Y);
        }

        private void NextCar(int direction)
        {
            _vehicleIndex = (_vehicleIndex + direction + ShowCars.Length) % ShowCars.Length;
            GameConfig.SelectedVehicle = VehicleDescription.Descriptions[_vehicleIndex];
            LoadPage(Page.Index);
        }

        private int MaxVariant()
        {
            return _page switch
            {
                Page.General => 5,
                Page.Mechanical => 3,
                Page.Performance => 3,
                _ => 1
            };
        }

        private void Previous()
        {
            if (_page == Page.Index) NextCar(-1);
            else if (_page != Page.History)
                _variant = (_variant + MaxVariant() - 1) % MaxVariant();
        }

        private void Next()
        {
            if (_page == Page.Index) NextCar(1);
            else if (_page != Page.History)
                _variant = (_variant + 1) % MaxVariant();
        }

        public void Update(GameTime time)
        {
            var input = Engine.Instance.Input;
            if (input.WasPressed(Keys.Escape))
            {
                if (_page == Page.Index)
                    Engine.Instance.Screen = new HomeScreen();
                else
                    LoadPage(Page.Index);
                return;
            }

            if (input.WasPressed(Keys.Left)) Previous();
            if (input.WasPressed(Keys.Right)) Next();
            if (input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space))
            {
                if (_page == Page.Index) LoadPage(Page.General);
                else LoadPage(Page.Index);
                return;
            }
            if (_page == Page.Index)
            {
                if (input.WasPressed(Keys.G)) LoadPage(Page.General);
                if (input.WasPressed(Keys.H)) LoadPage(Page.History);
                if (input.WasPressed(Keys.M)) LoadPage(Page.Mechanical);
                if (input.WasPressed(Keys.P)) LoadPage(Page.Performance);
            }

            if (!input.WasLeftMouseClicked) return;
            var point = input.GameMousePosition;
            if (point.X < 0) return;

            // Native showroom controls, not estimated fixed text hitboxes.
            if (Hit("dond", point) || Hit("donl", point))
            {
                if (_page == Page.Index) Engine.Instance.Screen = new HomeScreen();
                else LoadPage(Page.Index);
                return;
            }
            if (Hit("pred", point) || Hit("prel", point)) { Previous(); return; }
            if (Hit("nexd", point) || Hit("nexl", point)) { Next(); return; }

            if (_page == Page.Index)
            {
                if (Hit("gend", point)) LoadPage(Page.General);
                else if (Hit("hisd", point)) LoadPage(Page.History);
                else if (Hit("engd", point)) LoadPage(Page.Mechanical);
                else if (Hit("perd", point)) LoadPage(Page.Performance);
                else if (Hit("vidd", point))
                    _message = "Original TGV video playback is not implemented yet.";
            }
            else if (_page == Page.General)
            {
                if (Hit("fr1d", point) || Hit("fr2d", point) ||
                    Hit("f1rt", point) || Hit("f2rt", point))
                    Next();
                else if (Hit("f1lf", point) || Hit("f2lf", point))
                    Previous();
            }
            else if (_page == Page.Mechanical || _page == Page.Performance)
            {
                if (Hit("righ", point)) Next();
                else if (Hit("left", point)) Previous();
            }
        }

        public void Draw()
        {
            Engine.Instance.Device.Viewport = new Viewport(0, 0, 640, 480);
            var sprite = Engine.Instance.SpriteBatch;
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointClamp);
            DrawArt("bgnd");

            if (_page == Page.Index)
            {
                DrawArt("gend");
                DrawArt("hisd");
                DrawArt("engd");
                DrawArt("perd");
                DrawArt("vidd");
            }
            else if (_page == Page.General)
            {
                DrawArt("fr1d");
                DrawArt("fr2d");
                DrawArt("f1s" + _variant);
                DrawArt("f2s" + _variant);
                DrawArt("f1lf"); DrawArt("f1rt");
                DrawArt("f2lf"); DrawArt("f2rt");
            }
            else if (_page == Page.Mechanical)
            {
                DrawArt("frmd");
                DrawArt(new[] { "chas", "engn", "driv" }[_variant]);
                DrawArt("left"); DrawArt("righ");
            }
            else if (_page == Page.Performance)
            {
                DrawArt("frmd");
                DrawArt(new[] { "acc1", "bra1", "han1" }[_variant]);
                DrawArt(new[] { "accl", "brak", "hand" }[_variant]);
                DrawArt("left"); DrawArt("righ");
            }

            DrawArt("pred"); DrawArt("nexd"); DrawArt("dond");
            if (!string.IsNullOrEmpty(_message))
            {
                sprite.DrawString(_font, _message, new Vector2(22, 412),
                    Color.Yellow, 0, Vector2.Zero, 0.32f, SpriteEffects.None, 0);
            }
            sprite.End();
        }
    }
}
