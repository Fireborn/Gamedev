using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace FantasyRpgXna4.InputHandlers
{
    /// <summary>
    /// Handle keyboard input, save state, and allow the app to query against us
    /// </summary>
    public class KeyboardInputHandler : InputHandler
    {
        /// <summary>
        /// The order in which the key presses is relevant for diagonal movement and such.  Keep it on a stack!
        /// </summary>
        List<Keys> _movementKeyPressStack;

        /// <summary>
        /// Default constructor
        /// </summary>
        public KeyboardInputHandler()
        {
            _movementKeyPressStack = new List<Keys>();
        }

        /// <summary>
        /// Update the keyboard input
        /// </summary>
        /// <param name="gameTime">The amoun of elapsed time since the last update. TODO: do we even need this param?</param>
        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.W) && !_movementKeyPressStack.Contains(Keys.W))
            {
                _movementKeyPressStack.Add(Keys.W);
            }

            if (keyboardState.IsKeyUp(Keys.W) && _movementKeyPressStack.Contains(Keys.W))
            {
                _movementKeyPressStack.Remove(Keys.W);
            }

            if (keyboardState.IsKeyDown(Keys.A) && !_movementKeyPressStack.Contains(Keys.A))
            {
                _movementKeyPressStack.Add(Keys.A);
            }

            if (keyboardState.IsKeyUp(Keys.A) && _movementKeyPressStack.Contains(Keys.A))
            {
                _movementKeyPressStack.Remove(Keys.A);
            }

            if (keyboardState.IsKeyDown(Keys.S) && !_movementKeyPressStack.Contains(Keys.S))
            {
                _movementKeyPressStack.Add(Keys.S);
            }

            if (keyboardState.IsKeyUp(Keys.S) && _movementKeyPressStack.Contains(Keys.S))
            {
                _movementKeyPressStack.Remove(Keys.S);
            }

            if (keyboardState.IsKeyDown(Keys.D) && !_movementKeyPressStack.Contains(Keys.D))
            {
                _movementKeyPressStack.Add(Keys.D);
            }

            if (keyboardState.IsKeyUp(Keys.D) && _movementKeyPressStack.Contains(Keys.D))
            {
                _movementKeyPressStack.Remove(Keys.D);
            }
        }

        public override MovementDirection GetMovementDirection()
        {
            if (_movementKeyPressStack.Count == 0)
            {
                return MovementDirection.None;
            }

            // We're going to figure out dual key presses with vectors for now, and then translate that into
            // movement directions when we're done.  To start, we know there is at least one keypress on the
            // stack
            Vector2 movementDirection = Vector2.Zero;
            switch (_movementKeyPressStack.Last())
            {
                case Keys.W:
                    movementDirection = new Vector2(0, 1);
                    break;
                case Keys.A:
                    movementDirection = new Vector2(-1, 0);
                    break;
                case Keys.S:
                    movementDirection = new Vector2(0, -1);
                    break;
                case Keys.D:
                    movementDirection = new Vector2(1, 0);
                    break;
            }
                
            if(_movementKeyPressStack.Count > 1)
            {
                // Check the 2nd element
                switch (_movementKeyPressStack[_movementKeyPressStack.Count - 2])
                {
                    case Keys.W:
                        movementDirection += new Vector2(0, 1);
                        break;
                    case Keys.A:
                        movementDirection += new Vector2(-1, 0);
                        break;
                    case Keys.S:
                        movementDirection += new Vector2(0, -1);
                        break;
                    case Keys.D:
                        movementDirection += new Vector2(1, 0);
                        break;
                }
            }

            if (movementDirection.X == 0 && movementDirection.Y == 1)
            {
                return MovementDirection.Up;
            }
            if (movementDirection.X == 1 && movementDirection.Y == 1)
            {
                return MovementDirection.UpRight;
            }
            if (movementDirection.X == 1 && movementDirection.Y == 0)
            {
                return MovementDirection.Right;
            }
            if (movementDirection.X == 1 && movementDirection.Y == -1)
            {
                return MovementDirection.RightDown;
            }
            if (movementDirection.X == 0 && movementDirection.Y == -1)
            {
                return MovementDirection.Down;
            }
            if (movementDirection.X == -1 && movementDirection.Y == -1)
            {
                return MovementDirection.DownLeft;
            }
            if (movementDirection.X == -1 && movementDirection.Y == 0)
            {
                return MovementDirection.Left;
            }
            if (movementDirection.X == -1 && movementDirection.Y == 1)
            {
                return MovementDirection.LeftUp;
            }

            return MovementDirection.None;
        }
    }
}
