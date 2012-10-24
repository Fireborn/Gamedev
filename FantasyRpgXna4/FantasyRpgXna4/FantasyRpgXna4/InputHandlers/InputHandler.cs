using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace FantasyRpgXna4
{
    /// <summary>
    /// The possible directions to move.  North is 0 degrees...
    /// </summary>
    public enum MovementDirection
    {
        Up,
        UpRight,
        Right,
        RightDown,
        Down,
        DownLeft,
        Left,
        LeftUp,
        None
    }

    public abstract class InputHandler
    {
        public abstract void Update(GameTime gameTime);

        public abstract MovementDirection GetMovementDirection();
    }
}
