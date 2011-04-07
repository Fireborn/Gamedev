using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace LevelEditor
{
    /// <summary>
    /// Class which uses 2 dimensional input (x and y) to move a camera around a fixed point.
    /// </summary>
    class TrackballCamera
    {
        public Vector3 Position { get; set; }

        public Vector3 FocalPoint { get; set; }

        public Vector3 UpVector { get; set; }

        private float _distanceToFocal;
        private float _horizontalAngle;
        private float _verticalAngle;

        //GameConsole _console;

        public TrackballCamera(Vector3 initialPosition, Vector3 initialFocalPoint, Vector3 upVector)//, GameConsole console)
        {
            Position = initialPosition;
            FocalPoint = initialFocalPoint;
            UpVector = upVector;

            _distanceToFocal = Vector3.Distance(initialPosition, initialFocalPoint);
            _verticalAngle = (float)Math.Atan(Position.Y / Position.X);
            _horizontalAngle = (float)Math.Atan(Position.Z / Position.X);

            //_console = console;
        }

        public void UpdatePosition(int horizontalDelta, int verticalDelta)
        {
            if (horizontalDelta == 0 && verticalDelta == 0)
            {
                return;
            }

            // Positive horizontal delta indicates moving to the right
            _horizontalAngle += horizontalDelta / 20.0f;

            // Positive vertical delta indicates moving downward
            _verticalAngle += verticalDelta / 20.0f;

            if (_verticalAngle >= Math.PI / 2.0)
            {
                _verticalAngle = (float)Math.PI / 2.0f - float.Epsilon;
            }
            if (_verticalAngle <= -Math.PI / 2.0)
            {
                _verticalAngle = (float)-Math.PI / 2.0f + float.Epsilon;
            }

            Position = new Vector3(
                (float)Math.Cos(_horizontalAngle),
                (float)Math.Sin(_verticalAngle),
                (float)Math.Sin(_horizontalAngle));

            Position = Position * _distanceToFocal;
        }

        public void Zoom(int zoomDistance)
        {
            // each click of my scroll wheel appears to be 120...
            _distanceToFocal += -zoomDistance / 120.0f;

            if (_distanceToFocal <= 0)
            {
                _distanceToFocal = 1;
            }

            Vector3 positionVector = Position;
            positionVector.Normalize();
            Position = positionVector * _distanceToFocal;
        }
    }
}
