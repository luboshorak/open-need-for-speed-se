using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GameEngine;
using OpenNFS1.UI.Screens;

namespace OpenNFS1
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager _graphics;
		RenderTarget2D _renderTarget;


		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			IsMouseVisible = true; // show the native cursor for main-menu interaction
			Content.RootDirectory = "Content";

			GameConfig.Load(Environment.GetCommandLineArgs());

			if (GameConfig.FullScreen)
			{
				_graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
				_graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
			}
			else
			{
				_graphics.PreferredBackBufferWidth = 800;
				_graphics.PreferredBackBufferHeight = 600;
			}
			_graphics.PreferredDepthStencilFormat = DepthFormat.Depth24;
            _graphics.PreferMultiSampling = true;
			_graphics.IsFullScreen = GameConfig.FullScreen;
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			// MonoGame manages the game window; avoid accessing private OpenTK fields.
			base.Initialize();
		}


		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			Engine.Create(this, _graphics);

			_renderTarget = new RenderTarget2D(Engine.Instance.Device, 640, 480, false, _graphics.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24, 1, RenderTargetUsage.DiscardContents);
			Engine.Instance.Device.SetRenderTarget(_renderTarget);
			Engine.Instance.ScreenSize = new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

			Engine.Instance.Screen = new OpenNFS1SplashScreen();
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
            MenuSoundPlayer.Instance.Dispose();
            OriginalMusicPlayer.Instance.Dispose();
			Engine.Instance.ContentManager.Unload();
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
            if (Engine.Instance.Screen != null)
            {
                OriginalMusicPlayer.Instance.Update(
                    Engine.Instance.Screen is DoRaceScreen,
                    Engine.Instance.Screen is RacePausedScreen);
                // Original FRONT_MW.BNK menu clicks, independent of AS4 music.
                MenuSoundPlayer.Instance.Update();
            }
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			Engine.Instance.Device.SetRenderTarget(_renderTarget);

			Color c = new Color(0.15f, 0.15f, 0.15f);
			_graphics.GraphicsDevice.Clear(c);

			base.Draw(gameTime);

			Engine.Instance.Device.SetRenderTarget(null);

			// Window.ClientBounds.X/Y are *desktop* coordinates on DesktopGL.
			// Backbuffer coordinates start at (0,0); using ClientBounds directly
			// shifted the entire 640x480 scene off the visible window.
			int backWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
			int backHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
			float scale = Math.Min(backWidth / 640f, backHeight / 480f);
			int scaledWidth = Math.Max(1, (int)Math.Round(640 * scale));
			int scaledHeight = Math.Max(1, (int)Math.Round(480 * scale));
			Rectangle destination = new Rectangle(
				(backWidth - scaledWidth) / 2, (backHeight - scaledHeight) / 2,
				scaledWidth, scaledHeight);
			GraphicsDevice.Clear(Color.Black); // letterbox/pillarbox bars
			using (SpriteBatch sprite = new SpriteBatch(Engine.Instance.Device))
			{
				sprite.Begin(samplerState: SamplerState.LinearClamp);
				sprite.Draw(_renderTarget, destination, Color.White);
				sprite.End();
			}
		}
	}
}
