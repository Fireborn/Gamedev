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

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
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
            _player = new Player(new Vector3(0, 2, 0), Content.Load<Model>("DirtCube"));
            _camera = new IsometricChaseCamera(20.0f, _player.Position);
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 2.0f, 4.0f / 3.0f, 1, 1000000);

            //_console = new GameConsole(Content);

            _sun = Content.Load<Model>("sun");

            _lightEffect = new BasicEffect(GraphicsDevice);
            _lightEffect.Projection = _projectionMatrix;
            _lightEffect.World = Matrix.Identity;

            //_crosshair = Content.Load<Texture2D>("crosshair");

            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            //graphics.ToggleFullScreen();

            //_screenCenterX = GraphicsDevice.Viewport.Width / 2;
            //_screenCenterY = GraphicsDevice.Viewport.Height / 2;
            //_crosshairRectangle = new Rectangle(_screenCenterX - _crosshair.Width / 2, _screenCenterY - _crosshair.Height / 2, _crosshair.Width, _crosshair.Height);

            //Mouse.SetPosition(_screenCenterX, _screenCenterY);
            //_previousMouseState = Mouse.GetState();
            //_previousKeyboardState = Keyboard.GetState();

            //_viewMatrix = Matrix.CreateLookAt(_camera.Position, _camera.FocalPoint, _camera.UpVector);
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

            //_viewMatrix = Matrix.CreateLookAt(_camera.Position, _camera.FocalPoint, _camera.UpVector);

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
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

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

            base.Draw(gameTime);
        }
    }
}
