//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework;
//using GameLib;

//namespace LevelEditor
//{
//    class LevelCluster
//    {
//        public List<Model> Models = new List<Model>();
//        public List<List<Vector3>> ModelPositions = new List<List<Vector3>>();
//    }

//    class Level
//    {
//        private int _levelLength;
//        private int _levelWidth;
//        private int _levelHeight;
//        private Model[] _cubeTextures;
//        public Model[] CubeModels
//        {
//            get
//            {
//                return _cubeTextures;
//            }
//        }

//        private List<Vector3> _clusterPositions = new List<Vector3>();
//        private Dictionary<int, Octree> _clusterOctrees = new Dictionary<int, Octree>();
//        private int[,,,] _clusterCubeIndices;
//        private int _clusterXDimension;
//        private int _clusterYDimension;
//        private int _clusterZDimension;

//        List<List<VertexPositionNormalTexture>> _vertices = new List<List<VertexPositionNormalTexture>>();

//        List<VertexBuffer> _vertexBuffers = new List<VertexBuffer>();
//        List<Model> _modelRenderList = new List<Model>();
//        List<IndexBuffer> _indexBuffers = new List<IndexBuffer>();
//        List<int> _clusterIdToVertexBuffer = new List<int>();

//        List<List<int>> _indices = new List<List<int>>();
//        List<Effect> _effects = new List<Effect>();
//        //Dictionary<Model, List<Vector3>> _renderables = new Dictionary<Model, List<Vector3>>();
//        List<LevelCluster> _clusterRenderables = new List<LevelCluster>();
//        GraphicsDevice _graphicsDevice;
//        Octree _levelOctree;

//        private static readonly Plane _topCubeFace = new Plane(Vector3.Up, -1);
//        private static readonly Plane _bottomCubeFace = new Plane(Vector3.Down, -1);
//        private static readonly Plane _leftCubeFace = new Plane(Vector3.Left, -1);
//        private static readonly Plane _rightCubeFace = new Plane(Vector3.Right, -1);
//        private static readonly Plane _frontCubeFace = new Plane(Vector3.Forward, -1);
//        private static readonly Plane _backCubeFace = new Plane(Vector3.Backward, -1);

//        public Level(GraphicsDevice graphicsDevice, ContentManager content, int levelLength, int levelWidth, int levelHeight)
//        {
//            _graphicsDevice = graphicsDevice;

//            _cubeTextures = new Model[4];
//            _cubeTextures[0] = content.Load<Model>("GrassCube");
//            _cubeTextures[1] = content.Load<Model>("StoneCube");
//            _cubeTextures[2] = content.Load<Model>("DirtCube");
//            _cubeTextures[3] = content.Load<Model>("WaterCube");

//            int clusterXCount = 6;
//            int clusterYCount = 6;
//            int clusterZCount = 6;

//            _clusterXDimension = 50;
//            _clusterYDimension = 50;
//            _clusterZDimension = 50;

//            // lets start off with a base level which is 3x3x3 clusters (or 300x300x300 cubes)
//            for (int x = 0; x < clusterXCount; ++x)
//            {
//                for (int y = 0; y < clusterYCount; ++y)
//                {
//                    for (int z = 0; z < clusterZCount; ++z)
//                    {
//                        Vector3 clusterCentroid = new Vector3(
//                            x * _clusterXDimension, 
//                            y * _clusterYDimension, 
//                            z * _clusterZDimension);


//                        _clusterPositions.Add(clusterCentroid);
//                    }
//                }
//            }

//            _clusterCubeIndices = new int[_clusterPositions.Count, _clusterXDimension, _clusterYDimension, _clusterZDimension];

//            _levelLength = clusterXCount * _clusterXDimension;
//            _levelWidth = clusterZCount * _clusterZDimension;
//            _levelHeight = clusterYCount * _clusterYDimension;

//            //_level = new int[levelLength, levelHeight, levelWidth];
//            _levelOctree = new Octree(new BoundingBox(new Vector3(-_levelLength, -_levelHeight, -_levelWidth), new Vector3(_levelLength, _levelHeight, _levelWidth)), new Vector3(_levelLength, _levelHeight, _levelWidth));
//            //_levelCubeBoundingBoxes = new BoundingBox[levelLength, levelHeight, levelWidth];

//            for (int x = 0; x < levelLength; ++x)
//            {
//                for (int y = 0; y < levelHeight; ++y)
//                {
//                    for (int z = 0; z < levelWidth; ++z)
//                    {
//                        Vector3 clusterCentroid = new Vector3(
//                                (float)Math.Floor((double)x / (double)_clusterXDimension) * _clusterXDimension,
//                                (float)Math.Floor((double)y / (double)_clusterYDimension) * _clusterYDimension,
//                                (float)Math.Floor((double)z / (double)_clusterZDimension) * _clusterZDimension);

//                        int index = _clusterPositions.FindIndex(position => position == clusterCentroid);

//                        int clusterX = x % _clusterXDimension;
//                        int clusterY = y % _clusterYDimension;
//                        int clusterZ = z % _clusterZDimension;

//                        if (y == 0)
//                        {
//                            _clusterCubeIndices[index, clusterX, clusterY, clusterZ] = 0;
//                        }
//                        else
//                        {
//                            _clusterCubeIndices[index, clusterX, clusterY, clusterZ] = -1;
//                        }
//                    }
//                }
//            }

//            SetRenderables();
//        }

//        private void SetRenderables()
//        {
//            _vertices.Clear();
//            _vertexBuffers.Clear();
//            _indices.Clear();
//            _indexBuffers.Clear();
//            _clusterRenderables.Clear();
//            _effects.Clear();
//            _clusterIdToVertexBuffer.Clear();

//            _vertexBuffers = new List<VertexBuffer>(_clusterPositions.Count);

//            for (int clusterIndex = 0; clusterIndex < _clusterPositions.Count; ++clusterIndex)
//            {
//                GenerateCluster(clusterIndex);

//            }
//        }

//        public void GetRenderables(out List<VertexBuffer> vertices, out List<IndexBuffer> indices, out List<Model> models)
//        {
//            vertices = _vertexBuffers;
//            indices = _indexBuffers;
//            models = _modelRenderList;
//        }

//        internal bool SelectCube(Ray pickingRay, out Vector3 intersectedCubePosition, out Vector3 neighboringCubePosition)
//        {
//            float minimumDistance = float.MaxValue;
//            Vector3 cubePosition = new Vector3();
//            intersectedCubePosition = cubePosition;
//            neighboringCubePosition = cubePosition;

//            foreach (Octree octree in _clusterOctrees.Values)
//            {
//                float? distance = octree.Intersects(pickingRay, out cubePosition);
//                if (distance.HasValue && distance.Value < minimumDistance)
//                {
//                    minimumDistance = distance.Value;
//                    intersectedCubePosition = cubePosition;
//                }
//            }

//            if (minimumDistance != float.MaxValue)
//            {
//                // transform picking ray to intersect the same cube if it were in fact a unit cube
//                Vector3 cubeCenter = (intersectedCubePosition * 2) + Vector3.One;
//                pickingRay.Position -= cubeCenter;

//                float minimumFaceDistance = float.MaxValue;
//                Vector3 minimumFaceDirection = Vector3.Zero;

//                float? faceDistance = pickingRay.Intersects(_topCubeFace);
//                if (faceDistance.HasValue && faceDistance.Value < minimumFaceDistance)
//                {
//                    Vector3 intersectionPoint = pickingRay.Position + pickingRay.Direction * faceDistance.Value;
//                    if (intersectionPoint.X <= 1 &&
//                        intersectionPoint.Y <= 1 &&
//                        intersectionPoint.Z <= 1 &&
//                        intersectionPoint.X >= -1 &&
//                        intersectionPoint.Y >= -1 &&
//                        intersectionPoint.Z >= -1)
//                    {
//                        minimumFaceDistance = faceDistance.Value;
//                        minimumFaceDirection = Vector3.Up;
//                    }
//                }
//                faceDistance = pickingRay.Intersects(_bottomCubeFace);
//                if (faceDistance.HasValue && faceDistance.Value < minimumFaceDistance)
//                {
//                    Vector3 intersectionPoint = pickingRay.Position + pickingRay.Direction * faceDistance.Value;
//                    if (intersectionPoint.X <= 1 &&
//                        intersectionPoint.Y <= 1 &&
//                        intersectionPoint.Z <= 1 &&
//                        intersectionPoint.X >= -1 &&
//                        intersectionPoint.Y >= -1 &&
//                        intersectionPoint.Z >= -1)
//                    {
//                        minimumFaceDistance = faceDistance.Value;
//                        minimumFaceDirection = Vector3.Down;
//                    }
//                }
//                faceDistance = pickingRay.Intersects(_leftCubeFace);
//                if (faceDistance.HasValue && faceDistance.Value < minimumFaceDistance)
//                {
//                    Vector3 intersectionPoint = pickingRay.Position + pickingRay.Direction * faceDistance.Value;
//                    if (intersectionPoint.X <= 1 &&
//                        intersectionPoint.Y <= 1 &&
//                        intersectionPoint.Z <= 1 &&
//                        intersectionPoint.X >= -1 &&
//                        intersectionPoint.Y >= -1 &&
//                        intersectionPoint.Z >= -1)
//                    {
//                        minimumFaceDistance = faceDistance.Value;
//                        minimumFaceDirection = Vector3.Left;
//                    }
//                }
//                faceDistance = pickingRay.Intersects(_rightCubeFace);
//                if (faceDistance.HasValue && faceDistance.Value < minimumFaceDistance)
//                {
//                    Vector3 intersectionPoint = pickingRay.Position + pickingRay.Direction * faceDistance.Value;
//                    if (intersectionPoint.X <= 1 &&
//                        intersectionPoint.Y <= 1 &&
//                        intersectionPoint.Z <= 1 &&
//                        intersectionPoint.X >= -1 &&
//                        intersectionPoint.Y >= -1 &&
//                        intersectionPoint.Z >= -1)
//                    {
//                        minimumFaceDistance = faceDistance.Value;
//                        minimumFaceDirection = Vector3.Right;
//                    }
//                }
//                faceDistance = pickingRay.Intersects(_frontCubeFace);
//                if (faceDistance.HasValue && faceDistance.Value < minimumFaceDistance)
//                {
//                    Vector3 intersectionPoint = pickingRay.Position + pickingRay.Direction * faceDistance.Value;
//                    if (intersectionPoint.X <= 1 &&
//                        intersectionPoint.Y <= 1 &&
//                        intersectionPoint.Z <= 1 &&
//                        intersectionPoint.X >= -1 &&
//                        intersectionPoint.Y >= -1 &&
//                        intersectionPoint.Z >= -1)
//                    {
//                        minimumFaceDistance = faceDistance.Value;
//                        minimumFaceDirection = Vector3.Forward;
//                    }
//                }
//                faceDistance = pickingRay.Intersects(_backCubeFace);
//                if (faceDistance.HasValue && faceDistance.Value < minimumFaceDistance)
//                {
//                    Vector3 intersectionPoint = pickingRay.Position + pickingRay.Direction * faceDistance.Value;
//                    if (intersectionPoint.X <= 1 &&
//                        intersectionPoint.Y <= 1 &&
//                        intersectionPoint.Z <= 1 &&
//                        intersectionPoint.X >= -1 &&
//                        intersectionPoint.Y >= -1 &&
//                        intersectionPoint.Z >= -1)
//                    {
//                        minimumFaceDistance = faceDistance.Value;
//                        minimumFaceDirection = Vector3.Backward;
//                    }
//                }

//                neighboringCubePosition = intersectedCubePosition + minimumFaceDirection;
//                //neighboringCubePosition = intersectedCubePosition + Vector3.Up;

//                return true;
//            }

//            return false;
//        }

//        public void AddCube(Vector3 position, int cubeIndexToAdd)
//        {
//            int x = (int)position.X;
//            int y = (int)position.Y;
//            int z = (int)position.Z;

//            Vector3 clusterCentroid = new Vector3(
//                (float)Math.Floor((double)x / (double)_clusterXDimension) * _clusterXDimension,
//                (float)Math.Floor((double)y / (double)_clusterYDimension) * _clusterYDimension,
//                (float)Math.Floor((double)z / (double)_clusterZDimension) * _clusterZDimension);

//            int clusterIndex = _clusterPositions.FindIndex(item => item == clusterCentroid);

//            if (clusterIndex == -1)
//            {
//                // Someone tried to add a cube in a position which we don't have a cluster for
//                return;
//            }

//            int clusterX = (int)position.X % _clusterXDimension;
//            int clusterY = (int)position.Y % _clusterYDimension;
//            int clusterZ = (int)position.Z % _clusterZDimension;

//            if (_clusterCubeIndices[clusterIndex, clusterX, clusterY, clusterZ] == -1)
//            {
//                _clusterCubeIndices[clusterIndex, clusterX, clusterY, clusterZ] = cubeIndexToAdd;
//                GenerateCluster(clusterIndex);
//            }  
//        }

//        //private void AddRenderable(int clusterIndex, int x, int y, int z)
//        //{
//        //    Vector3 basePosition = new Vector3(x, y, z);
//        //    Vector3 position = 2 * basePosition;

//        //    Matrix translation = Matrix.CreateTranslation(position);

//        //    int modelIndex = _level[x, y, z];
//        //    Model model = _cubeTextures[modelIndex];

//        //    List<VertexPositionNormalTexture> vertices;
//        //    List<int> indices;
//        //    GetModelVerticesAndIndices(model, out vertices, out indices);

//        //    int vertexCount = _vertices[modelIndex].Count;

//        //    VertexPositionNormalTexture[] translatedCopy = new VertexPositionNormalTexture[vertices.Count];
//        //    vertices.CopyTo(translatedCopy);

//        //    Vector3[] translatedPositions = new Vector3[translatedCopy.Length];
//        //    for (int i = 0; i < translatedCopy.Length; ++i)
//        //    {
//        //        translatedCopy[i].Position = Vector3.Transform(translatedCopy[i].Position, translation);
//        //        translatedPositions[i] = translatedCopy[i].Position;
//        //    }

//        //    _levelOctree.AddItems(new Vector3[] {basePosition}, new BoundingBox[] { BoundingBox.CreateFromPoints(translatedPositions) });
//        //    //_levelCubeBoundingBoxes[(int)position.X / 2, (int)position.Y / 2, (int)position.Z / 2] = BoundingBox.CreateFromPoints(translatedPositions);

//        //    for (int i = 0; i < indices.Count(); ++i)
//        //    {
//        //        indices[i] += (int)vertexCount;
//        //    }

//        //    _vertices[modelIndex].AddRange(translatedCopy);
//        //    _indices[modelIndex].AddRange(indices);

//        //    _vertexBuffers[modelIndex].Dispose();
//        //    _vertexBuffers[modelIndex] = new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalTexture), _vertices[modelIndex].Count, BufferUsage.None);
//        //    _vertexBuffers[modelIndex].SetData<VertexPositionNormalTexture>(_vertices[modelIndex].ToArray());

//        //    _indexBuffers[modelIndex].Dispose();
//        //    _indexBuffers[modelIndex] = new IndexBuffer(_graphicsDevice, typeof(int), _indices[modelIndex].Count, BufferUsage.None);
//        //    _indexBuffers[modelIndex].SetData<int>(_indices[modelIndex].ToArray());
//        //}

//        private void GetModelVerticesAndIndices(Model model, out List<VertexPositionNormalTexture> vertices, out List<int> indices)
//        {
//            vertices = new List<VertexPositionNormalTexture>();
//            indices = new List<int>();
//            foreach (ModelMesh mesh in model.Meshes)
//            {
//                foreach (ModelMeshPart meshPart in mesh.MeshParts)
//                {
//                    _effects.Add(meshPart.Effect);

//                    UInt16[] shortIndexData = new UInt16[meshPart.PrimitiveCount * 3];
//                    meshPart.IndexBuffer.GetData<UInt16>(shortIndexData);
//                    int[] indexData = new int[shortIndexData.Length];
//                    for (int i = 0; i < indexData.Length; ++i)
//                    {
//                        indexData[i] = shortIndexData[i] + vertices.Count;
//                    }
//                    indices.AddRange(indexData);

//                    VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[meshPart.NumVertices];
//                    meshPart.VertexBuffer.GetData<VertexPositionNormalTexture>(vertexData);
//                    vertices.AddRange(vertexData);
//                }
//            }
//        }

//        private void GenerateCluster(int clusterIndex)
//        {
//            Vector3 clusterBasePosition = _clusterPositions[clusterIndex];
//            LevelCluster clusterRenderables = new LevelCluster();

//            // Clean up renderables associated with this cluster
//            for (int renderableIndex = 0; renderableIndex < _clusterIdToVertexBuffer.Count; ++renderableIndex)
//            {
//                if (_clusterIdToVertexBuffer[renderableIndex] == clusterIndex)
//                {
//                    _modelRenderList.RemoveAt(renderableIndex);
//                    _vertexBuffers.RemoveAt(renderableIndex);
//                    _indexBuffers.RemoveAt(renderableIndex);
//                    _clusterIdToVertexBuffer.RemoveAt(renderableIndex);
                    
//                    renderableIndex--;
//                }
//            }

//            BoundingBox clusterBoundingBox = new BoundingBox(
//                new Vector3(-_clusterXDimension, -_clusterYDimension, -_clusterZDimension),
//                new Vector3(_clusterXDimension, _clusterYDimension, _clusterZDimension));

//            Vector3 clusterCentroid = _clusterPositions[clusterIndex] * 2 + new Vector3(_clusterXDimension, _clusterYDimension, _clusterZDimension);

//            _clusterOctrees.Remove(clusterIndex);
//            _clusterOctrees.Add(clusterIndex, new Octree(clusterBoundingBox, clusterCentroid));

//            // Each cube is 2x2, and we'll assume 0,0 is centered at the origin
//            for (int x = 0; x < _clusterXDimension; ++x)
//            {
//                for (int y = 0; y < _clusterYDimension; ++y)
//                {
//                    for (int z = 0; z < _clusterZDimension; ++z)
//                    {
//                        int modelIndex = _clusterCubeIndices[clusterIndex, x, y, z];

//                        if (modelIndex != -1)
//                        {
//                            Model model = _cubeTextures[modelIndex];
//                            int clusterModelIndex = clusterRenderables.Models.FindIndex(clusterModel => clusterModel == model);

//                            if (clusterModelIndex == -1)
//                            {
//                                clusterRenderables.Models.Add(model);
//                                clusterRenderables.ModelPositions.Add(new List<Vector3>());
//                                clusterModelIndex = clusterRenderables.Models.Count - 1;
//                            }

//                            clusterRenderables.ModelPositions[clusterModelIndex].Add(new Vector3(
//                                clusterBasePosition.X + x,
//                                clusterBasePosition.Y + y,
//                                clusterBasePosition.Z + z));
//                        }
//                    }
//                }
//            }

//            List<BoundingBox> boundingBoxes = new List<BoundingBox>();
//            List<Vector3> cubePositions = new List<Vector3>();

//            for (int modelIndex = 0; modelIndex < clusterRenderables.Models.Count; ++modelIndex)
//            {
//                Model model = clusterRenderables.Models[modelIndex];
//                List<Vector3> positions = clusterRenderables.ModelPositions[modelIndex];

//                List<VertexPositionNormalTexture> vertices;
//                List<int> indices;
//                GetModelVerticesAndIndices(model, out vertices, out indices);

//                List<VertexPositionNormalTexture> finalVertices = new List<VertexPositionNormalTexture>();
//                List<int> finalIndices = new List<int>();
//                int vertexCount = vertices.Count;
//                foreach (Vector3 position in positions)
//                {
//                    VertexPositionNormalTexture[] translatedCopy = new VertexPositionNormalTexture[vertices.Count];
//                    vertices.CopyTo(translatedCopy);

//                    Matrix translation = Matrix.CreateTranslation((position * 2) + Vector3.One);
//                    Vector3[] translatedPositions = new Vector3[translatedCopy.Length];
//                    for (int i = 0; i < translatedCopy.Length; ++i)
//                    {
//                        translatedCopy[i].Position = Vector3.Transform(translatedCopy[i].Position, translation);
//                        translatedPositions[i] = translatedCopy[i].Position;
//                    }

//                    boundingBoxes.Add(BoundingBox.CreateFromPoints(translatedPositions));
//                    cubePositions.Add(position);

//                    for (int i = 0; i < indices.Count(); ++i)
//                    {
//                        indices[i] += (int)vertexCount;
//                    }
//                    finalIndices.AddRange(indices);

//                    finalVertices.AddRange(translatedCopy);
//                }

//                _vertexBuffers.Add(new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalTexture), finalVertices.Count, BufferUsage.None));
//                _vertexBuffers.Last().SetData<VertexPositionNormalTexture>(finalVertices.ToArray());

//                _indexBuffers.Add(new IndexBuffer(_graphicsDevice, typeof(int), finalIndices.Count, BufferUsage.None));
//                _indexBuffers.Last().SetData<int>(finalIndices.ToArray());

//                _modelRenderList.Add(model);

//                _clusterIdToVertexBuffer.Add(clusterIndex);
//            }

//            _clusterOctrees[clusterIndex].AddItems(cubePositions.ToArray(), boundingBoxes.ToArray());
//            int count = _clusterOctrees[clusterIndex].Count;

//            for (int i = 0; i < _vertices.Count; ++i)
//            {
//                _vertexBuffers.Add(new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalTexture), _vertices[i].Count, BufferUsage.None));
//                _vertexBuffers[i].SetData<VertexPositionNormalTexture>(_vertices[i].ToArray());
//            }

//            for (int i = 0; i < _indices.Count; ++i)
//            {
//                _indexBuffers.Add(new IndexBuffer(_graphicsDevice, typeof(int), _indices[i].Count, BufferUsage.None));
//                _indexBuffers[i].SetData<int>(_indices[i].ToArray());
//            }
//        }

//        internal void DeleteCube(Vector3 position)
//        {
//            int x = (int)position.X;
//            int y = (int)position.Y;
//            int z = (int)position.Z;

//            Vector3 clusterCentroid = new Vector3(
//                (float)Math.Floor((double)x / (double)_clusterXDimension) * _clusterXDimension,
//                (float)Math.Floor((double)y / (double)_clusterYDimension) * _clusterYDimension,
//                (float)Math.Floor((double)z / (double)_clusterZDimension) * _clusterZDimension);

//            int clusterIndex = _clusterPositions.FindIndex(item => item == clusterCentroid);

//            if (clusterIndex == -1)
//            {
//                // Someone tried to add a cube in a position which we don't have a cluster for
//                return;
//            }

//            int clusterX = (int)position.X % _clusterXDimension;
//            int clusterY = (int)position.Y % _clusterYDimension;
//            int clusterZ = (int)position.Z % _clusterZDimension;

//            if (_clusterCubeIndices[clusterIndex, clusterX, clusterY, clusterZ] != -1)
//            {
//                _clusterCubeIndices[clusterIndex, clusterX, clusterY, clusterZ] = -1;
//                GenerateCluster(clusterIndex);
//            }  
//        }
//    }
//}
