using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameEngine;
using OpenNFS1.Parsers;
using OpenNFS1.Physics;

namespace OpenNFS1.Views
{
	class BaseExternalView
	{
		static BitmapEntry _bottomFill, _tacho, _map;
		static Texture2D _tachLineTexture;
		const int _size = 160;
		const float _needleLength = 2.5f;
		const float _needleWidth = 3f;

		public BaseExternalView()
		{
			if (_bottomFill == null)
			{
				var fsh = new FshFile(@"Simdata\Misc\MaskHi.fsh");
				_bottomFill = fsh.Header.Bitmaps.Find(a => a.Id == "0002");
				_tacho = fsh.Header.Bitmaps.Find(a => a.Id == "tach");
				_map = fsh.Header.Bitmaps.Find(a => a.Id == "mpbd");

				_tachLineTexture = new Texture2D(Engine.Instance.Device, (int)_needleWidth, 25);
				Color[] pixels = new Color[_tachLineTexture.Width * _tachLineTexture.Height];
				for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.Red;
				_tachLineTexture.SetData<Color>(pixels);
			}
		}

		public void RenderBackground(DrivableVehicle car)
		{
			float carRpm = MathHelper.Clamp(car.Motor.Rpm / Math.Max(1f, car.Motor.RedlineRpm), 0f, 1f);
            var sprite = Engine.Instance.SpriteBatch;
            // Deferred drawing is intentional: the native tachometer overlaps
            // the bottom panel. FrontToBack previously hid its bottom 45 pixels.
            sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied,
                SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);

            // The original MASKHI.FSH uses small 32x32 tiles, NOT a 640x60
            // bitmap. Tile the existing dark texture into the lower 80 pixels.
            if (_bottomFill?.Texture != null)
            {
                sprite.Draw(_bottomFill.Texture, new Rectangle(0, 400, 640, 80),
                    new Rectangle(0, 0, 640, 80), Color.White);
            }

            // Draw the tach on TOP of the bottom panel at its native
            // (10,289) position and 156x156 size, down to screen row 445.
            if (_tacho?.Texture != null)
            {
                Vector2 position = _tacho.GetDisplayAt();
                int width = _tacho.Texture.Width;
                int height = _tacho.Texture.Height;
                sprite.Draw(_tacho.Texture, position, Color.White);

                float rotation = (float)(carRpm * Math.PI * 1.4f) - 2.56f;
                sprite.Draw(_tachLineTexture,
                    position + new Vector2(width / 2f, height / 2f), null,
                    Color.Red, rotation, new Vector2(_needleWidth / 2, 25),
                    new Vector2(1f, _needleLength), SpriteEffects.None, 0);
            }

            sprite.End();
		}
	}
}
