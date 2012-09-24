using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace LevelEditor
{
    class FirstPersonCamera
    {
        public Vector3 Position { get; set; }

        public Vector3 FocalPoint { get; set; }

        public Vector3 UpVector { get; set; }

        private Vector3 _viewDirection;

        /// <summary>
        /// This angle is the amount rotated from the +z axis if the camera was at the origin.  It can
        /// range from 0 to 2pi.
        /// </summary>
        private float _horizontalAngleRadians;

        /// <summary>
        /// An angle of zero should be level with the ground.  As the angle grows positive, the camera
        /// will be pointing upward.  As it becomes negative, the camera will point downward.  This
        /// value should range from -pi/2 to +pi/2.
        /// </summary>
        private float _verticalAngleRadians;

        private const float _velocity = 10.0f;

        public FirstPersonCamera(Vector3 initialPosition, Vector3 focalPoint, Vector3 upVector)
        {
            Position = initialPosition;
            FocalPoint = focalPoint;
            UpVector = upVector;

            _viewDirection = focalPoint - initialPosition;
            _viewDirection.Normalize();
            if (_viewDirection == upVector || _viewDirection == -upVector)
            {
                throw new Exception("Cannot initialize the camera to point straight upward or downward");
            }

            _verticalAngleRadians = (float)Math.Acos(Vector3.Dot(_viewDirection, upVector));
            _verticalAngleRadians = (float)Math.PI / 2.0f - _verticalAngleRadians;

            // Project the view direction vector into the xz plane (assuming y is the up vector)
            Vector3 projectedVector = new Vector3(_viewDirection.X, 0, _viewDirection.Z);
            projectedVector.Normalize();
            _horizontalAngleRadians = (float)Math.Acos(Vector3.Dot(projectedVector, Vector3.UnitZ));
            if (projectedVector.X < 0)
            {
                _horizontalAngleRadians = ((float)Math.PI * 2.0f) - _horizontalAngleRadians;
            }
        }

        public void Update(int horizontalDelta, int verticalDelta)
        {
            // Transform the horizontal and vertical deltas into angles to produce a new
            // view direction
            float horizontalDegrees = -horizontalDelta;
            float verticalDegrees = verticalDelta;

            _horizontalAngleRadians += (horizontalDegrees * (float)Math.PI / 180.0f);
            _verticalAngleRadians += (verticalDegrees * (float)Math.PI / 108.0f);

            while (_horizontalAngleRadians >= 2.0f * (float)Math.PI)
            {
                _horizontalAngleRadians -= 2.0f * (float)Math.PI;
            }
            if (_verticalAngleRadians >= (float)Math.PI / 2.0f)
            {
                _verticalAngleRadians = (float)Math.PI / 2.0f - 0.01f;
            }
            if (_verticalAngleRadians <= -(float)Math.PI / 2.0f)
            {
                _verticalAngleRadians = -(float)Math.PI / 2.0f + 0.01f;
            }


            Matrix horizontalTransform = Matrix.CreateRotationY(_horizontalAngleRadians);
            Matrix verticalTransform = Matrix.CreateRotationX(_verticalAngleRadians);
            _viewDirection = Vector3.Transform(Vector3.UnitZ, verticalTransform);
            _viewDirection = Vector3.Transform(_viewDirection, horizontalTransform);
            
            FocalPoint = Position + _viewDirection;
        }

        public void MoveForward(GameTime time, bool speedBoost = false)
        {
            float velocity = _velocity;
            if (speedBoost)
            {
                velocity *= 10;
            }
            Position += _viewDirection * velocity * (time.ElapsedGameTime.Milliseconds / 1000.0f);
            FocalPoint = Position + _viewDirection;
        }

        public void MoveBackward(GameTime time, bool speedBoost = false)
        {
            float velocity = _velocity;
            if (speedBoost)
            {
                velocity *= 10;
            }
            Position -= _viewDirection * velocity * (time.ElapsedGameTime.Milliseconds / 1000.0f);
        }

        public void StrafeLeft(GameTime time, bool speedBoost = false)
        {
            float velocity = _velocity;
            if (speedBoost)
            {
                velocity *= 10;
            }

            Vector3 strafeDirection = Vector3.Cross(_viewDirection, Vector3.Up);
            strafeDirection.Normalize();

            Position -= strafeDirection * velocity * (time.ElapsedGameTime.Milliseconds / 1000.0f);
            FocalPoint = Position + _viewDirection;
        }

        public void StrafeRight(GameTime time, bool speedBoost = false)
        {
            float velocity = _velocity;
            if (speedBoost)
            {
                velocity *= 10;
            }

            Vector3 strafeDirection = Vector3.Cross(Vector3.Up, _viewDirection);
            strafeDirection.Normalize();

            Position -= strafeDirection * velocity * (time.ElapsedGameTime.Milliseconds / 1000.0f);
            FocalPoint = Position + _viewDirection;
        }

        public void FlyUp(GameTime time, bool speedBoost = false)
        {
            float velocity = _velocity;
            if (speedBoost)
            {
                velocity *= 10;
            }

            Position += Vector3.Up * velocity * (time.ElapsedGameTime.Milliseconds / 1000.0f);
            FocalPoint = Position + _viewDirection;
        }
    }
}
