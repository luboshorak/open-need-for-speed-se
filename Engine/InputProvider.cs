using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace GameEngine
{
    public enum Actions
    {
        MoveForwardsPrimary,
        MoveForwardsAlternate,
        MoveBackwardsPrimary,
        MoveBackwardsAlternate,
        StrafeRightPrimary,
        StrafeRightAlternate,
        StrafeLeftPrimary,
        StrafeLeftAlternate,
        RunPrimary,
        RunAlternate,
        CrouchPrimary,
        CrouchAlternate,
        JumpPrimary,
        JumpAlternate
    };

    public enum MouseInputMode
    {
        FPS,
        FreeMouse
    }

    public class InputProvider : GameComponent
    {
        private GamePadState _gamePadState, _previousGamePadState;

        private KeyboardState _keyboardState, _previousKeyboardState;
        private MouseState _mouseState, _previousMouseState;
        
        private const float SENSITIVITY = 1.0f;
        
        private float _perFrameMultiplier;
        

        public InputProvider(Game game)
            : base(game)
        {
            
        }

        public GamePadState GamePadState
        {
            get { return _gamePadState; }
        }


        public override void Update(GameTime gameTime)
        {
            float frameTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _perFrameMultiplier = frameTime * SENSITIVITY;
            
            _previousKeyboardState = _keyboardState;
            _previousGamePadState = _gamePadState;
            _previousMouseState = _mouseState;
            _keyboardState = Keyboard.GetState();
            _gamePadState = GamePad.GetState(PlayerIndex.One);
            _mouseState = Mouse.GetState();

            if (_previousKeyboardState == null)
                _previousKeyboardState = _keyboardState;
            
            base.Update(gameTime);
        }

        private Vector2 GetScreenCenter()
        {
            GameWindow window = Engine.Instance.Game.Window;
            return new Vector2(window.ClientBounds.Width / 2, window.ClientBounds.Height / 2);
        }

        public float MoveForward
        {
            get
            {
                if (_gamePadState.ThumbSticks.Left.Y != 0)
                    return _gamePadState.ThumbSticks.Left.Y * _perFrameMultiplier;
                else if (_keyboardState.IsKeyDown(Keys.W))
                    return 1.0f * _perFrameMultiplier;
                else if (_keyboardState.IsKeyDown(Keys.S))
                    return -1.0f * _perFrameMultiplier;
                else
                    return 0.0f;
            }
        }
        public float Strafe
        {
            get
            {
                if (_gamePadState.ThumbSticks.Left.X != 0)
                    return _gamePadState.ThumbSticks.Left.X * _perFrameMultiplier;
                else if (_keyboardState.IsKeyDown(Keys.A))
                    return -1.0f * _perFrameMultiplier;
                else if (_keyboardState.IsKeyDown(Keys.D))
                    return 1.0f * _perFrameMultiplier;
                else
                    return 0.0f;
            }
        }

        /// <summary>True after a left mouse button was released.</summary>
        public bool WasLeftMouseClicked
        {
            get { return Game.IsActive &&
                    _previousMouseState.LeftButton == ButtonState.Pressed &&
                    _mouseState.LeftButton == ButtonState.Released; }
        }

        /// <summary>
        /// Map mouse coordinates to the virtual 640 x 480 render target, preserving
        /// centered 4:3 letterboxing. (-1,-1) means outside the game image.
        /// </summary>
        public Vector2 GameMousePosition
        {
            get
            {
                if (!Game.IsActive)
                    return new Vector2(-1, -1);
                var presentation = Engine.Instance.Device.PresentationParameters;
                int w = presentation.BackBufferWidth;
                int h = presentation.BackBufferHeight;
                if (w <= 0 || h <= 0)
                    return new Vector2(-1, -1);

                float scale = Math.Min(w / 640f, h / 480f);
                int destWidth = Math.Max(1, (int)Math.Round(640 * scale));
                int destHeight = Math.Max(1, (int)Math.Round(480 * scale));
                int x0 = (w - destWidth) / 2;
                int y0 = (h - destHeight) / 2;
                int x = _mouseState.X - x0;
                int y = _mouseState.Y - y0;
                if (x < 0 || y < 0 || x >= destWidth || y >= destHeight)
                    return new Vector2(-1, -1);
                return new Vector2(x * 640f / destWidth, y * 480f / destHeight);
            }
        }

        public bool WasPressed(Keys key)
        {
            return _previousKeyboardState.IsKeyDown(key) && !_keyboardState.IsKeyDown(key);
        }

        public bool WasPressed(Buttons button)
        {
            return _previousGamePadState.IsButtonDown(button) && !_gamePadState.IsButtonDown(button);
        }

        public bool IsKeyDown(Keys key)
        {
            return _keyboardState.IsKeyDown(key);
        }
    }
}
