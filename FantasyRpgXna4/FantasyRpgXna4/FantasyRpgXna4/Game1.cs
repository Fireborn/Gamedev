using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using CubeGameWorld;
using GameLib;
using FantasyRpgXna4.InputHandlers;

namespace FantasyRpgXna4
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GameWorld _gameWorld;
        Model _sun;
        BasicEffect _lightEffect;
        Vector3 _lightPosition;
        float _lightDistance;
        Vector3 _lightWorldPosition;
        Matrix _sunWorldMatrix;
        float _lightHorizontalAngle;
        float _lightVerticalAngle;

        IsometricChaseCamera _camera;
        Player _player;
        Matrix _projectionMatrix;

        InputHandler _inputHandler;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            _inputHandler = new KeyboardInputHandler();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            _gameWorld = GameWorld.Load("test.level", GraphicsDevice, Content);
            _player = new Player(new Vector3(215, 40, 145), Content.Load<Model>("orangecube"));
            _camera = new IsometricChaseCamera(20.0f, _player.Position);
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 2.0f, 4.0f / 3.0f, 1, 1000000);

            _sun = Content.Load<Model>("sun");

            _lightEffect = new BasicEffect(GraphicsDevice);
            _lightEffect.Projection = _projectionMatrix;
            _lightEffect.World = Matrix.Identity;

            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            //graphics.ToggleFullScreen();

            _lightEffect.View = _camera.ViewMatrix;

            _lightPosition = new Vector3(1, 0, 0) * _lightDistance;
            Matrix verticalRotation = Matrix.CreateRotationZ(_lightVerticalAngle);
            Matrix horizontalRotation = Matrix.CreateRotationY(_lightHorizontalAngle);

            // abuse the fact that I made the sun model and know that +z is the normal
            Vector3 currentNormal = Vector3.UnitZ;
            Vector3 desiredNormal = _camera.Position - _lightPosition;
            desiredNormal.Normalize();
            Vector3 axisOfRotation = Vector3.Cross(currentNormal, desiredNormal);
            float angleToRotate = (float)Math.Acos(Vector3.Dot(currentNormal, desiredNormal));

            _sunWorldMatrix = Matrix.CreateScale(75) * Matrix.CreateFromAxisAngle(axisOfRotation, angleToRotate) * Matrix.CreateTranslation(_lightPosition) * horizontalRotation * verticalRotation;

            _gameWorld.SetActiveRenderArea(new Vector3(10, 10, 10), 200);

            _lightWorldPosition = Vector3.Transform(Vector3.Zero, _sunWorldMatrix);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            if (gamePadState.IsConnected)
            {
                // Allows the game to exit
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                    this.Exit();

                if (GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.X == 1)
                    this.Exit();
            }

            _inputHandler.Update(gameTime);
            MovementDirection movementDirection = _inputHandler.GetMovementDirection();
            Vector3 worldSpaceMovementVector = Vector3.Zero;
            switch (movementDirection)
            {
                case MovementDirection.Up:
                    worldSpaceMovementVector = Vector3.Forward;
                    break;
                case MovementDirection.UpRight:
                    worldSpaceMovementVector = Vector3.Forward + Vector3.Right;
                    break;
                case MovementDirection.Right:
                    worldSpaceMovementVector = Vector3.Right;
                    break;
                case MovementDirection.RightDown:
                    worldSpaceMovementVector = Vector3.Right + Vector3.Backward;
                    break;
                case MovementDirection.Down:
                    worldSpaceMovementVector = Vector3.Backward;
                    break;
                case MovementDirection.DownLeft:
                    worldSpaceMovementVector = Vector3.Backward + Vector3.Left;
                    break;
                case MovementDirection.Left:
                    worldSpaceMovementVector = Vector3.Left;
                    break;
                case MovementDirection.LeftUp:
                    worldSpaceMovementVector = Vector3.Left + Vector3.Forward;
                    break;
            }

            Vector3 newPosition = _player.Position + worldSpaceMovementVector * 0.02f /*velocity in m/ms*/ * gameTime.ElapsedGameTime.Milliseconds;

            // Validate that the user can move in that direction
            if (!_gameWorld.CheckIntersection(newPosition))
            {
                // Things look good, but perhaps the player is falling? If so drop the position, else, keep this new position
                Vector3 fallingPosition = newPosition + Vector3.Down * 0.01f /*Velocity in m/ms*/ * gameTime.ElapsedGameTime.Milliseconds;

                if (!_gameWorld.CheckIntersection(fallingPosition))
                {
                    newPosition = fallingPosition;
                }
            }
            else
            {
                // The player intersected the environment, maybe he can move upwards a bit to overcome?
                Vector3 climbingPosition = newPosition + Vector3.Up * GameWorld.CubeSize;
                if (!_gameWorld.CheckIntersection(climbingPosition))
                {
                    newPosition = climbingPosition;
                }
                else
                {
                    newPosition = _player.Position;
                }
            }

            _player.Position = newPosition;

            _camera.Update(gameTime, _player.Position);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _gameWorld.Draw(gameTime, GraphicsDevice, Matrix.Identity, _camera.ViewMatrix, _projectionMatrix);

            _player.VisualModel.Draw(Matrix.CreateScale(0.01f, 0.01f, 0.01f) * Matrix.CreateTranslation(_player.Position), _camera.ViewMatrix, _projectionMatrix);

            base.Draw(gameTime);
        }
    }
}
