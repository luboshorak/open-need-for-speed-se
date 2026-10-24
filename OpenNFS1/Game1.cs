using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        private Effect _colorCorrection;


		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			IsMouseVisible = true; // show the native cursor for main-menu interaction
			Content.RootDirectory = "Content";

			GameConfig.Load(Environment.GetCommandLineArgs());

            var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            var fittedWindow = PresentationLayout.FitWindow(
                GameConfig.WindowWidth, GameConfig.WindowHeight, mode.Width, mode.Height);
            _graphics.PreferredBackBufferWidth = GameConfig.FullScreen
                ? mode.Width : fittedWindow.X;
            _graphics.PreferredBackBufferHeight = GameConfig.FullScreen
                ? mode.Height : fittedWindow.Y;
            _graphics.HardwareModeSwitch = false; // borderless native resolution fullscreen
            DisplaySettings.IntegerScaling = GameConfig.ScalingMode == 2;
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
            PlayerProfile.Load();
            _colorCorrection = Engine.Instance.ContentManager.Load<Effect>("ColorCorrection");

			_renderTarget = new RenderTarget2D(Engine.Instance.Device, 640, 480, false, _graphics.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24, 1, RenderTargetUsage.DiscardContents);
			Engine.Instance.Device.SetRenderTarget(_renderTarget);
			Engine.Instance.ScreenSize = new Vector2(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

			Engine.Instance.Screen = new OpenNFS1SplashScreen();
		}

        /// <summary>
        /// Applies fullscreen and window-size changes without restarting the
        /// game, changing original virtual UI positions or reallocating CD art.
        /// </summary>
        public void ApplyDisplaySettings()
        {
            var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.IsFullScreen = GameConfig.FullScreen;
            _graphics.HardwareModeSwitch = false;
            var fittedWindow = PresentationLayout.FitWindow(
                GameConfig.WindowWidth, GameConfig.WindowHeight, mode.Width, mode.Height);
            _graphics.PreferredBackBufferWidth = GameConfig.FullScreen
                ? mode.Width : fittedWindow.X;
            _graphics.PreferredBackBufferHeight = GameConfig.FullScreen
                ? mode.Height : fittedWindow.Y;
            _graphics.ApplyChanges();

            // The chosen window size may be larger than the monitor or the
            // existing window may have been partly off-screen. Recenter.
            if (!GameConfig.FullScreen)
            {
                Window.Position = new Point(
                    Math.Max(0, (mode.Width - _graphics.PreferredBackBufferWidth) / 2),
                    Math.Max(0, (mode.Height - _graphics.PreferredBackBufferHeight) / 2 - 24));
            }
            DisplaySettings.IntegerScaling = GameConfig.ScalingMode == 2;
            if (Engine.Instance != null)
                Engine.Instance.ScreenSize = new Vector2(
                    GraphicsDevice.PresentationParameters.BackBufferWidth,
                    GraphicsDevice.PresentationParameters.BackBufferHeight);
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
            // Panic key: always restore a usable 800x600 window, even if the
            // graphics menu itself is inaccessible. Available from any screen.
            if (Engine.Instance.Input.WasPressed(Keys.F8))
            {
                // Emergency one-key return to the unmodified DOS palette,
                // regardless of saved gamma/brightness/contrast.
                GameConfig.OriginalColors = true;
                GameConfig.Gamma = 1;
                GameConfig.Brightness = 0;
                GameConfig.Contrast = 1;
                GameConfig.SaveUserSettings();
            }
            if (Engine.Instance.Input.WasPressed(Keys.F10))
            {
                GameConfig.FullScreen = false;
                GameConfig.WindowWidth = 800;
                GameConfig.WindowHeight = 600;
                ApplyDisplaySettings();
                GameConfig.SaveUserSettings();
                if (Engine.Instance.Screen is DisplayOptionsScreen)
                    Engine.Instance.Screen = new OptionsScreen();
            }
            else if (Engine.Instance.Input.WasPressed(Keys.F11))
            {
                GameConfig.FullScreen = !GameConfig.FullScreen;
                ApplyDisplaySettings();
                GameConfig.SaveUserSettings();
            }
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

            // Presentation and mouse picking use exactly the same virtual
            // 640x480 → backbuffer mapping, including 4:3 pillarboxing.
            int backWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int backHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
            Rectangle destination = PresentationLayout.Calculate(backWidth, backHeight,
                GameConfig.ScalingMode == 2);
            GraphicsDevice.Viewport = new Viewport(0, 0, backWidth, backHeight);
            GraphicsDevice.Clear(Color.Black);

            SamplerState sampling = GameConfig.ScalingMode == 0
                ? SamplerState.LinearClamp : SamplerState.PointClamp;
            Effect correction = null;
            if (!GameConfig.OriginalColors)
            {
                _colorCorrection.Parameters["Texture"].SetValue(_renderTarget);
                _colorCorrection.Parameters["Gamma"].SetValue(GameConfig.Gamma);
                _colorCorrection.Parameters["Brightness"].SetValue(GameConfig.Brightness);
                _colorCorrection.Parameters["Contrast"].SetValue(GameConfig.Contrast);
                correction = _colorCorrection;
            }

            using (var sprite = new SpriteBatch(GraphicsDevice))
            {
                sprite.Begin(SpriteSortMode.Immediate, BlendState.Opaque, sampling,
                    DepthStencilState.None, RasterizerState.CullNone,
                    effect: correction);
                sprite.Draw(_renderTarget, destination, Color.White);
                sprite.End();
            }

		}
	}
}
