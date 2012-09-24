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
using GameLib;
using CubeGameWorld;

namespace LevelEditor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class LevelEditor : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        //Level _level;
        GameWorld _gameWorld;
        MouseState _previousMouseState;
        KeyboardState _previousKeyboardState;
        FirstPersonCamera _camera;
        Matrix _viewMatrix;
        Matrix _projectionMatrix;
        GameConsole _console;
        BasicEffect _lightEffect;
        Vector3? _lastCubeAdded = null;
        Texture2D _crosshair;
        Rectangle _crosshairRectangle;
        int _screenCenterX;
        int _screenCenterY;
        int _activeCubeModelIndex = 0;
        Vector3 _lightPosition;
        float _lightHorizontalAngle = 0;
        float _lightVerticalAngle = 45;
        const float _lightDistance = 1000;
        Model _sun;
        Matrix _sunWorldMatrix;
        public List<IndexBuffer> _shadowIndexList = new List<IndexBuffer>();
        public List<VertexBuffer> _shadowVertexBufferList = new List<VertexBuffer>();
        List<Vector3> _shadowVertices = new List<Vector3>();
        List<UInt32> _shadowIndices = new List<UInt32>();
        List<UInt32> _shadowMeshIndices = new List<UInt32>();
        List<Vector3> _shadowMeshVertices = new List<Vector3>();
        Effect _shadowVolumeEffect;
        Effect _renderWithShadowsEffect;
        Vector3 _lightWorldPosition;

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
            //this.IsMouseVisible = true;

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

            //_level = new Level(GraphicsDevice, Content, 300, 300, 300);
            //_gameWorld = GameWorld.CreateNew(GraphicsDevice, Content, 300, 300, 300);
            _gameWorld = GameWorld.Load("test.level", GraphicsDevice, Content);
            _camera = new FirstPersonCamera(new Vector3(100, 100, 100), Vector3.Zero, Vector3.Up);
            _projectionMatrix = Matrix.CreatePerspectiveFieldOfView((float)Math.PI / 2.0f, 4.0f / 3.0f, 1, 1000000);

            _console = new GameConsole(Content);

            _sun = Content.Load<Model>("sun");

            _lightEffect = new BasicEffect(GraphicsDevice);
            _lightEffect.Projection = _projectionMatrix;
            _lightEffect.World = Matrix.Identity;

            _crosshair = Content.Load<Texture2D>("crosshair");

            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            graphics.ToggleFullScreen();

            _screenCenterX = GraphicsDevice.Viewport.Width / 2;
            _screenCenterY = GraphicsDevice.Viewport.Height / 2;
            _crosshairRectangle = new Rectangle(_screenCenterX - _crosshair.Width / 2, _screenCenterY - _crosshair.Height / 2, _crosshair.Width, _crosshair.Height);

            Mouse.SetPosition(_screenCenterX, _screenCenterY);
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();

            //_viewMatrix = Matrix.CreateLookAt(_camera.Position, _camera.FocalPoint, _camera.UpVector);
            _lightEffect.View = _viewMatrix;

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

            _viewMatrix = Matrix.CreateLookAt(_camera.Position, _camera.FocalPoint, _camera.UpVector);

            _gameWorld.SetActiveRenderArea(new Vector3(10, 10, 10), 200);

            _lightWorldPosition = Vector3.Transform(Vector3.Zero, _sunWorldMatrix);

            //ComputeShadowMesh(_lightWorldPosition);

            //_shadowVolumeEffect = Content.Load<Effect>("ShadowVolume");
            //_renderWithShadowsEffect = Content.Load<Effect>("RenderWithShadows");
            //_renderWithShadowsEffect.CurrentTechnique = _renderWithShadowsEffect.Techniques["Technique1"];
        }



        public void ComputeShadowMesh(Vector3 lightWorldPosition)
        {
            List<VertexBuffer> vertexBuffers;
            List<IndexBuffer> indexBuffers;
            _gameWorld.GetRenderables(out vertexBuffers, out indexBuffers);

            for (int i = 0; i < vertexBuffers.Count; ++i)
            {
                _shadowMeshIndices.Clear();
                _shadowVertices.Clear();
                _shadowIndices.Clear();

                VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[vertexBuffers[i].VertexCount];
                vertexBuffers[i].GetData<VertexPositionNormalTexture>(vertexData);

                UInt32[] vertexIndices = new UInt32[indexBuffers[i].IndexCount];
                indexBuffers[i].GetData<UInt32>(vertexIndices);

                foreach (VertexPositionNormalTexture vertex in vertexData)
                {
                    _shadowVertices.Add(vertex.Position);
                }

                foreach (UInt32 index in vertexIndices)
                {
                    _shadowIndices.Add(index);
                }

                Shadows.CreateShadowVolumeMesh(vertexData.ToList(), _shadowIndices, Vector3.Zero - lightWorldPosition, ref _shadowMeshIndices, ref _shadowMeshVertices);
                IndexBuffer shadowIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, _shadowMeshIndices.Count, BufferUsage.None);
                shadowIndexBuffer.SetData(_shadowMeshIndices.ToArray());
                _shadowIndexList.Add(shadowIndexBuffer);

                List<VertexPositionColor> shadowVertices = new List<VertexPositionColor>();
                foreach(Vector3 vertexPos in _shadowMeshVertices)
                {
                    shadowVertices.Add(new VertexPositionColor(vertexPos, Color.White));
                }

                VertexBuffer shadowVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), shadowVertices.Count, BufferUsage.None);
                shadowVertexBuffer.SetData(shadowVertices.ToArray());
                _shadowVertexBufferList.Add(shadowVertexBuffer);
            }
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

            _camera.Update(currentMouseState.X - _previousMouseState.X, currentMouseState.Y - _previousMouseState.Y);

            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {
                Ray pickingRay = Cursor.CalculateCursorRay(_projectionMatrix, _viewMatrix, new Vector2(_screenCenterX, _screenCenterY), GraphicsDevice);

                Vector3 cubePosition;
                Vector3 cubeNeighbor;
                if (_gameWorld.SelectCube(pickingRay, out cubePosition, out cubeNeighbor))
                {
                    if (!_lastCubeAdded.HasValue || _lastCubeAdded.Value != cubePosition)
                    {
                        _gameWorld.AddCube(cubeNeighbor, _activeCubeModelIndex);
                        _lastCubeAdded = cubeNeighbor;
                    }
                }
            }

            if (currentMouseState.LeftButton == ButtonState.Released && _previousMouseState.LeftButton == ButtonState.Pressed)
            {
                _lastCubeAdded = null;
            }

            if (currentMouseState.RightButton == ButtonState.Released &&
                _previousMouseState.RightButton == ButtonState.Pressed &&
                currentMouseState.X > _previousMouseState.X - 2 &&
                currentMouseState.X < _previousMouseState.X + 2 &&
                currentMouseState.Y > _previousMouseState.Y - 2 &&
                currentMouseState.Y < _previousMouseState.Y + 2)
            {
                Ray pickingRay = Cursor.CalculateCursorRay(_projectionMatrix, _viewMatrix, new Vector2(currentMouseState.X, currentMouseState.Y), GraphicsDevice);

                Vector3 cubePosition;
                Vector3 cubeNeighbor;
                if (_gameWorld.SelectCube(pickingRay, out cubePosition, out cubeNeighbor))
                {
                    _gameWorld.DeleteCube(cubePosition);
                }
            }

            Mouse.SetPosition(_screenCenterX, _screenCenterY);
            _previousMouseState = Mouse.GetState();

            KeyboardState keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Keys.W))
            {
                _camera.MoveForward(gameTime, keyState.IsKeyDown(Keys.LeftShift));
            }
            if (keyState.IsKeyDown(Keys.S))
            {
                _camera.MoveBackward(gameTime, keyState.IsKeyDown(Keys.LeftShift));
            }
            if (keyState.IsKeyDown(Keys.A))
            {
                _camera.StrafeLeft(gameTime, keyState.IsKeyDown(Keys.LeftShift));
            }
            if (keyState.IsKeyDown(Keys.D))
            {
                _camera.StrafeRight(gameTime, keyState.IsKeyDown(Keys.LeftShift));
            }
            if (keyState.IsKeyDown(Keys.Space))
            {
                _camera.FlyUp(gameTime);
            }
            if (keyState.IsKeyDown(Keys.D1))
            {
                if (_gameWorld.Cubes.Length > 0)
                {
                    _activeCubeModelIndex = 0;
                }
            }
            if (keyState.IsKeyDown(Keys.D2))
            {
                if (_gameWorld.Cubes.Length > 1)
                {
                    _activeCubeModelIndex = 1;
                }
            }
            if (keyState.IsKeyDown(Keys.D3))
            {
                if (_gameWorld.Cubes.Length > 2)
                {
                    _activeCubeModelIndex = 2;
                }
            }
            if (keyState.IsKeyDown(Keys.D4))
            {
                if (_gameWorld.Cubes.Length > 3)
                {
                    _activeCubeModelIndex = 3;
                }
            }
            if (keyState.IsKeyDown(Keys.D5))
            {
                if (_gameWorld.Cubes.Length > 4)
                {
                    _activeCubeModelIndex = 4;
                }
            }
            if (keyState.IsKeyDown(Keys.D6))
            {
                if (_gameWorld.Cubes.Length > 5)
                {
                    _activeCubeModelIndex = 5;
                }
            }
            if (keyState.IsKeyDown(Keys.D7))
            {
                if (_gameWorld.Cubes.Length > 6)
                {
                    _activeCubeModelIndex = 6;
                }
            }
            if (keyState.IsKeyDown(Keys.D8))
            {
                if (_gameWorld.Cubes.Length > 7)
                {
                    _activeCubeModelIndex = 7;
                }
            }
            if (keyState.IsKeyDown(Keys.D9))
            {
                if (_gameWorld.Cubes.Length > 8)
                {
                    _activeCubeModelIndex = 8;
                }
            }
            if (keyState.IsKeyDown(Keys.D0))
            {
                if (_gameWorld.Cubes.Length > 9)
                {
                    _activeCubeModelIndex = 9;
                }
            }
            if (keyState.IsKeyDown(Keys.LeftControl) && keyState.IsKeyUp(Keys.S) && _previousKeyboardState.IsKeyDown(Keys.S))
            {
                _gameWorld.Save("test.level");
            }

            if (keyState.IsKeyDown(Keys.LeftControl) && keyState.IsKeyUp(Keys.R) && _previousKeyboardState.IsKeyDown(Keys.R))
            {
                ComputeShadowMesh(_lightWorldPosition);
            }

            _previousKeyboardState = keyState;

            _viewMatrix = Matrix.CreateLookAt(_camera.Position, _camera.FocalPoint, _camera.UpVector);

            //_renderWithShadowsEffect.Parameters["ModelViewProjection"].SetValue(_viewMatrix * _projectionMatrix);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _gameWorld.Draw(gameTime, GraphicsDevice, Matrix.Identity, _viewMatrix, _projectionMatrix);

            // Set up shadow data in the stencil buffer
            //for (int i = 0; i < vertices.Count; ++i)
            //{
            //    Model model = models[i];
            //    IndexBuffer shadowIndices = _shadowIndexList[i];
            //    VertexBuffer shadowVertices = _shadowVertexBufferList[i];

            //    _shadowVolumeEffect.Parameters["World"].SetValue(Matrix.Identity);
            //    _shadowVolumeEffect.Parameters["View"].SetValue(_viewMatrix);
            //    _shadowVolumeEffect.Parameters["Projection"].SetValue(_projectionMatrix);
            //    _shadowVolumeEffect.CurrentTechnique = _shadowVolumeEffect.Techniques["ShadowVolume"];

            //    foreach (EffectPass pass in _shadowVolumeEffect.CurrentTechnique.Passes)
            //    {
            //        pass.Apply();

            //        GraphicsDevice.SetVertexBuffer(shadowVertices);
            //        GraphicsDevice.Indices = shadowIndices;

            //        int primitivesToRender = shadowIndices.IndexCount / 3;
            //        int startIndex = 0;
            //        while (primitivesToRender > _maxPrimitives)
            //        {
            //            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices[i].VertexCount, startIndex, _maxPrimitives);
            //            startIndex += _maxPrimitives * 3;
            //            primitivesToRender -= _maxPrimitives;
            //        }
            //        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices[i].VertexCount, startIndex, primitivesToRender);
            //    }
            //}

            // visualize the shadow mesh for funs
            //RasterizerState rasterizerState = RasterizerState.CullNone;
            //for (int i = 0; i < vertices.Count; ++i)
            //{
            //    Model model = models[i];
            //    IndexBuffer shadowIndices = _shadowIndexList[i];
            //    VertexBuffer shadowVertices = _shadowVertexBufferList[i];

            //    foreach (ModelMesh mesh in model.Meshes)
            //    {
            //        foreach (BasicEffect effect in mesh.Effects)
            //        {
            //            effect.World = Matrix.Identity;
            //            effect.View = _viewMatrix;
            //            effect.Projection = _projectionMatrix;
            //            effect.LightingEnabled = false;
            //            effect.TextureEnabled = false;

            //            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            //            {
            //                pass.Apply();

            //                GraphicsDevice.SetVertexBuffer(shadowVertices);
            //                GraphicsDevice.Indices = shadowIndices;

            //                int primitivesToRender = shadowIndices.IndexCount / 3;
            //                int startIndex = 0;
            //                while (primitivesToRender > _maxPrimitives)
            //                {
            //                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices[i].VertexCount, startIndex, _maxPrimitives);
            //                    startIndex += _maxPrimitives * 3;
            //                    primitivesToRender -= _maxPrimitives;
            //                }
            //                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices[i].VertexCount, startIndex, primitivesToRender);
            //            }
            //        }
            //    }
            //}

            foreach (ModelMesh sunMesh in _sun.Meshes)
            {
                foreach (ModelMeshPart sunMeshPart in sunMesh.MeshParts)
                {
                    foreach (BasicEffect effect in sunMesh.Effects)
                    {
                        effect.World = _sunWorldMatrix;
                        effect.View = _viewMatrix;
                        effect.Projection = _projectionMatrix;
                        //effect.AmbientLightColor = new Vector3(1.0f, 1.0f, 1.0f);
                        effect.LightingEnabled = false;

                        foreach (EffectPass effectPass in effect.CurrentTechnique.Passes)
                        {
                            effectPass.Apply();

                            GraphicsDevice.SetVertexBuffer(sunMeshPart.VertexBuffer);
                            GraphicsDevice.Indices = sunMeshPart.IndexBuffer;
                            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, sunMeshPart.NumVertices, 0, sunMeshPart.PrimitiveCount);
                        }
                    }
                }
            }

            spriteBatch.Begin();
            spriteBatch.Draw(_crosshair, _crosshairRectangle, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
