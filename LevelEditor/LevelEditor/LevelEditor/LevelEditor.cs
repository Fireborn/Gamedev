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
using System.Diagnostics;

namespace LevelEditor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class LevelEditor : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Level _level;
        MouseState _previousMouseState;
        TrackballCamera _camera;
        Matrix _viewMatrix;
        Matrix _projectionMatrix;
        GameConsole _console;
        BasicEffect _basicEffect;
        static readonly int _maxPrimitives = 1048575;
        public LevelEditor()
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
            this.IsMouseVisible = true;

            Components.Add(new FrameRateCounter(this));

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

            _level = new Level(GraphicsDevice, Content, 300, 300, 300);
            _camera = new TrackballCamera(new Vector3(100, 100, 100), Vector3.Zero, Vector3.Up);
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 2.0f, 4.0f / 3.0f, 1, 10000);

            _console = new GameConsole(Content);

            _basicEffect = new BasicEffect(GraphicsDevice);
            _basicEffect.Projection = _projectionMatrix;
            _basicEffect.World = Matrix.Identity;

            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            //graphics.ToggleFullScreen();
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

            MouseState currentMouseState = Mouse.GetState();

            if (_previousMouseState != null)
            {
                if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Pressed)
                {
                    _camera.UpdatePosition(currentMouseState.X - _previousMouseState.X, currentMouseState.Y - _previousMouseState.Y);
                }

                if (currentMouseState.ScrollWheelValue != _previousMouseState.ScrollWheelValue)
                {
                    int scrollDelta = currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
                    _camera.Zoom(scrollDelta);
                } 
                if (currentMouseState.LeftButton == ButtonState.Released && 
                    _previousMouseState.LeftButton == ButtonState.Pressed &&
                    currentMouseState.X > _previousMouseState.X - 2 && 
                    currentMouseState.X < _previousMouseState.X + 2 && 
                    currentMouseState.Y > _previousMouseState.Y - 2 && 
                    currentMouseState.Y < _previousMouseState.Y + 2)
                {
                    Ray pickingRay = Cursor.CalculateCursorRay(_projectionMatrix, _viewMatrix, new Vector2(currentMouseState.X, currentMouseState.Y), GraphicsDevice);

                    Vector3 cubePosition;
                    if (_level.SelectCube(pickingRay, out cubePosition))
                    {
                        _level.AddCube(cubePosition + Vector3.Up);
                    }
                    
                }
            }

            _previousMouseState = currentMouseState;

            _viewMatrix = Matrix.CreateLookAt(_camera.Position, _camera.FocalPoint, _camera.UpVector);
            _basicEffect.View = _viewMatrix;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            List<VertexBuffer> vertices;
            List<IndexBuffer> indices;
            List<Effect> effects;
            _level.GetRenderables(out vertices, out indices, out effects);

            GraphicsDevice.DepthStencilState = new DepthStencilState();

            for (int i = 0; i < vertices.Count; ++i)
            {
                BasicEffect effect = (BasicEffect)effects[i];
                effect.World = Matrix.Identity;
                effect.View = _viewMatrix;
                effect.Projection = _projectionMatrix;
                effect.LightingEnabled = true;

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    GraphicsDevice.SetVertexBuffer(vertices[i]);
                    GraphicsDevice.Indices = indices[i];

                    int primitivesToRender = indices[i].IndexCount / 3;
                    int startIndex = 0;
                    while (primitivesToRender > _maxPrimitives)
                    {
                        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices[i].VertexCount, startIndex, _maxPrimitives);
                        startIndex += _maxPrimitives * 3;
                        primitivesToRender -= _maxPrimitives;
                    }
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices[i].VertexCount, startIndex, primitivesToRender);
                }
            }

            base.Draw(gameTime);
        }
    }
}
