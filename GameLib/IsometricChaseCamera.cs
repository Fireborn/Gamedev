using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameLib
{
    /// <summary>
    /// A camera that stays at a constant altitude and looks down at the player.  It should follow the player as
    /// it updates it's position.
    /// </summary>
    public class IsometricChaseCamera
    {
        private float _altitude;

        public IsometricChaseCamera(float altitude, Vector3 playerPosition)
        {
            _altitude = altitude;
            Position = playerPosition + Vector3.Up * _altitude;
            ViewMatrix = Matrix.CreateLookAt(Position, playerPosition, Vector3.Forward);
        }

        public Matrix ViewMatrix
        {
            get;
            private set;
        }

        public Vector3 Position
        {
            get;
            private set;
        }

        public void Update(GameTime gameTime, Vector3 playerPosition)
        {
            Position = playerPosition + Vector3.Up * _altitude;
            ViewMatrix = Matrix.CreateLookAt(Position, playerPosition, Vector3.Forward);
        }
    }
}
