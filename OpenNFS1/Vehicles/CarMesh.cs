using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenNFS1.Parsers;
using GameEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenNFS1.Vehicles
{
	class CarMesh : Mesh
	{
		Polygon _rightRearWheel, _leftRearWheel, _rightFrontWheel, _leftFrontWheel;
		Polygon _leftBrakeLight, _rightBrakeLight;
		Texture2D _wheelTexture, _brakeOnTexture, _brakeOffTexture;
		
		static Color BrakeOffColor = new Color(88, 10, 5); //color of brake-off light color...
		static Color BrakeOnColor = new Color(158, 110, 6); //color of brake-off light color...

		public CarMesh(MeshChunk meshChunk, BitmapChunk bmpChunk, Color brakeColor)
			: base(meshChunk, bmpChunk)
		{
			foreach (var poly in _polys)
			{
				// Some CFM editions use padded/mixed-case ORIP polygon labels.
				switch (poly.Label?.Trim().ToLowerInvariant())
				{
					case "rt_rear":
						_rightRearWheel = poly;
						break;
					case "lt_rear":
						_leftRearWheel = poly;
						break;
					case "rt_frnt":
						_rightFrontWheel = poly;
						break;
					case "lt_frnt":
						_leftFrontWheel = poly;
						break;
					case "bkll":
						_leftBrakeLight = poly;
						break;
					case "bklr":
						_rightBrakeLight = poly;
						break;
				}
			}
			// An ORIP mesh can lack one or more conventional wheel labels even when
			// the body and textures are valid. Record those cases instead of crashing
			// when DrivableVehicle asks for the axle positions.
			if (_leftFrontWheel == null || _rightFrontWheel == null ||
			    _leftRearWheel == null || _rightRearWheel == null)
			{
				try
				{
					string labels = string.Join(", ", _polys
					    .Select(p => p.Label).Where(label => !string.IsNullOrWhiteSpace(label))
					    .Distinct(StringComparer.OrdinalIgnoreCase));
					System.IO.File.AppendAllText(
					    System.IO.Path.Combine(AppContext.BaseDirectory, "model-warnings.log"),
					    $"{DateTime.UtcNow:O} Mesh '{Identifier}' missing wheel labels:" +
					    $" left front={_leftFrontWheel != null}, right front={_rightFrontWheel != null}," +
					    $" left rear={_leftRearWheel != null}, right rear={_rightRearWheel != null}." +
					    $" Available polygon labels: {labels}{Environment.NewLine}");
				}
				catch (System.IO.IOException) { /* Logging must not prevent loading a model. */ }
				catch (UnauthorizedAccessException) { /* Read-only installation folder. */ }
			}

			var tyreEntry = bmpChunk.FindByName("tyr1");
			if (tyreEntry != null)
				_wheelTexture = tyreEntry.Texture;

			// This seems like it should be done in a shader but I couldn't get it to work well enough 
			// (dealing with original paletted colors doesn't work so well in a texture stretched over a polygon)
			
			var rsidPoly = _polys.FirstOrDefault(a => a.TextureName == "rsid");
			if (rsidPoly != null)
			{
				// Generate a new texture for brake lights on.  
				Color[] pixels = new Color[rsidPoly.Texture.Width * rsidPoly.Texture.Height];
				rsidPoly.Texture.GetData<Color>(pixels);
				for (int i = 0; i < pixels.Length; i++)
				{
					if (pixels[i] == brakeColor)
						pixels[i] = BrakeOnColor;
				}

				_brakeOnTexture = new Texture2D(Engine.Instance.Device, rsidPoly.Texture.Width, rsidPoly.Texture.Height);
				_brakeOnTexture.SetData<Color>(pixels);

				// Generate a new texture for brake lights off.
				for (int i = 0; i < pixels.Length; i++)
				{
					if (pixels[i] == BrakeOnColor)
						pixels[i] = BrakeOffColor;
				}

				_brakeOffTexture = new Texture2D(Engine.Instance.Device, rsidPoly.Texture.Width, rsidPoly.Texture.Height);
				_brakeOffTexture.SetData<Color>(pixels);
			}
		}

		public Vector3 LeftFrontWheelPos { get { return GetWheelAxlePoint(_leftFrontWheel, true, true); } }
		public Vector3 RightFrontWheelPos { get { return GetWheelAxlePoint(_rightFrontWheel, false, true); } }
		public Vector3 LeftRearWheelPos { get { return GetWheelAxlePoint(_leftRearWheel, true, false); } }
		public Vector3 RightRearWheelPos { get { return GetWheelAxlePoint(_rightRearWheel, false, false); } }

		private static bool HasGeometry(Polygon polygon)
		{
			return polygon?.Vertices != null && polygon.Vertices.Length > 0;
		}

		private static Vector3 PolygonAxle(Polygon polygon)
		{
			return new Vector3(
				polygon.Vertices[0].X,
				(polygon.Vertices.Max(v => v.Y) + polygon.Vertices.Min(v => v.Y)) / 2,
				(polygon.Vertices.Max(v => v.Z) + polygon.Vertices.Min(v => v.Z)) / 2);
		}

		private Vector3 GetWheelAxlePoint(Polygon wheelPoly, bool left, bool front)
		{
			if (HasGeometry(wheelPoly))
				return PolygonAxle(wheelPoly);

			// Prefer mirroring the real wheel on the opposite side of the same axle.
			Polygon opposite = front
				? (left ? _rightFrontWheel : _leftFrontWheel)
				: (left ? _rightRearWheel : _leftRearWheel);
			float centerX = (BoundingBox.Min.X + BoundingBox.Max.X) / 2;
			if (HasGeometry(opposite))
			{
				Vector3 axle = PolygonAxle(opposite);
				axle.X = 2 * centerX - axle.X;
				return axle;
			}

			// If both labels are missing, infer a conservative axle position from
			// the actual mesh bounds. Z negative is forward in our vehicle coordinates.
			Polygon sameSide = left ? _leftRearWheel : _rightRearWheel;
			if (!front) sameSide = left ? _leftFrontWheel : _rightFrontWheel;
			float width = Math.Max(0.01f, BoundingBox.Max.X - BoundingBox.Min.X);
			float length = Math.Max(0.01f, BoundingBox.Max.Z - BoundingBox.Min.Z);
			float height = Math.Max(0.01f, BoundingBox.Max.Y - BoundingBox.Min.Y);
			float x = HasGeometry(sameSide)
				? PolygonAxle(sameSide).X
				: centerX + (left ? -1 : 1) * width * 0.44f;
			float y = HasGeometry(sameSide)
				? PolygonAxle(sameSide).Y
				: BoundingBox.Min.Y + height * 0.22f;
			float z = front
				? BoundingBox.Min.Z + length * 0.20f
				: BoundingBox.Max.Z - length * 0.20f;
			return new Vector3(x, y, z);
		}

		private static float WheelDiameter(Polygon a, Polygon b, BoundingBox bounds)
		{
			Polygon reference = HasGeometry(a) ? a : HasGeometry(b) ? b : null;
			if (reference != null)
			{
				float diameter = reference.Vertices.Max(v => v.Y) - reference.Vertices.Min(v => v.Y);
				if (diameter > 0.001f) return diameter;
			}
			return Math.Max(0.1f, (bounds.Max.Y - bounds.Min.Y) * 0.34f);
		}

		public float RearWheelSize { get { return WheelDiameter(_leftRearWheel, _rightRearWheel, BoundingBox); } }
		public float FrontWheelSize { get { return WheelDiameter(_leftFrontWheel, _rightFrontWheel, BoundingBox); } }

		public Texture2D WheelTexture { get { return _wheelTexture; } }

		public void Render(Effect effect, bool enableBrakeLights)
		{
			Engine.Instance.Device.SetVertexBuffer(_vertexBuffer);

			effect.CurrentTechnique.Passes[0].Apply();

			foreach (Polygon poly in _polys)
			{
				if (poly == _rightFrontWheel || poly == _leftFrontWheel || poly == _leftRearWheel || poly == _rightRearWheel)
				{
					continue;
				}
				else if (_brakeOnTexture != null && poly.TextureName == "rsid")
				{
					Engine.Instance.Device.Textures[0] = enableBrakeLights ? _brakeOnTexture : _brakeOffTexture;
				}
				else
				{
					Engine.Instance.Device.Textures[0] = poly.Texture;
				}
				Engine.Instance.Device.DrawPrimitives(PrimitiveType.TriangleList, poly.VertexBufferIndex, poly.VertexCount / 3);
			}
		}
	}
}
