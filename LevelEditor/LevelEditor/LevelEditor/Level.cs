using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace LevelEditor
{
    class Level
    {
        //private BoundingBox[, ,] _levelCubeBoundingBoxes;
        private int[, ,] _level;
        private int _levelLength;
        private int _levelWidth;
        private int _levelHeight;
        private Model[] _levelCubes;
        List<List<VertexPositionNormalTexture>> _vertices = new List<List<VertexPositionNormalTexture>>();
        List<VertexBuffer> _vertexBuffers = new List<VertexBuffer>();
        List<List<int>> _indices = new List<List<int>>();
        List<IndexBuffer> _indexBuffers = new List<IndexBuffer>();
        List<Effect> _effects = new List<Effect>();
        Dictionary<Model, List<Vector3>> _renderables = new Dictionary<Model, List<Vector3>>();
        GraphicsDevice _graphicsDevice;
        Octree _levelOctree;

        public Level(GraphicsDevice graphicsDevice, ContentManager content, int levelLength, int levelWidth, int levelHeight)
        {
            _graphicsDevice = graphicsDevice;

            _levelCubes = new Model[1];
            _levelCubes[0] = content.Load<Model>("GrassCube");

            _levelLength = levelLength;
            _levelWidth = levelWidth;
            _levelHeight = levelHeight;

            _level = new int[levelLength, levelHeight, levelWidth];
            _levelOctree = new Octree(new BoundingBox(Vector3.Zero, new Vector3(2*_levelLength, 2*_levelHeight, 2*_levelWidth)));
            //_levelCubeBoundingBoxes = new BoundingBox[levelLength, levelHeight, levelWidth];

            for (int x = 0; x < levelLength; ++x)
            {
                for (int y = 0; y < levelHeight; ++y)
                {
                    for (int z = 0; z < levelWidth; ++z)
                    {
                        if (y == 0)
                        {
                            _level[x, y, z] = 0;
                        }
                        else
                        {
                            _level[x, y, z] = -1;
                        }
                    }
                }
            }

            SetRenderables();
        }

        private void SetRenderables()
        {
            _vertices.Clear();
            _vertexBuffers.Clear();
            _indices.Clear();
            _indexBuffers.Clear();
            _renderables.Clear();
            _effects.Clear();

            // Each cube is 2x2, and we'll assume 0,0 is centered at the origin
            for (int x = 0; x < _levelLength; ++x)
            {
                for (int y = 0; y < _levelHeight; ++y)
                {
                    for (int z = 0; z < _levelWidth; ++z)
                    {
                        int modelIndex = _level[x, y, z];

                        if (modelIndex != -1)
                        {
                            if(!_renderables.ContainsKey(_levelCubes[modelIndex]))
                            {
                                _renderables.Add(_levelCubes[modelIndex], new List<Vector3>());
                            }
                        
                            _renderables[_levelCubes[modelIndex]].Add(new Vector3(x*2, y*2, z*2));
                        }
                    }
                }
            }

            List<BoundingBox> boundingBoxes = new List<BoundingBox>();
            List<Vector3> positions = new List<Vector3>();

            foreach(KeyValuePair<Model, List<Vector3>> modelPositions in _renderables)
            {
                List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
                List<int> indices = new List<int>();
                GetModelVerticesAndIndices(modelPositions.Key, out vertices, out indices);

                List<VertexPositionNormalTexture> finalVertices = new List<VertexPositionNormalTexture>();
                List<int> finalIndices = new List<int>();
                int vertexCount = vertices.Count;
                foreach (Vector3 position in modelPositions.Value)
                {
                    VertexPositionNormalTexture[] translatedCopy = new VertexPositionNormalTexture[vertices.Count];
                    vertices.CopyTo(translatedCopy);

                    Matrix translation = Matrix.CreateTranslation(position);
                    Vector3[] translatedPositions = new Vector3[translatedCopy.Length];
                    for (int i = 0; i < translatedCopy.Length; ++i)
                    {
                        translatedCopy[i].Position = Vector3.Transform(translatedCopy[i].Position, translation);
                        translatedPositions[i] = translatedCopy[i].Position;
                    }

                    boundingBoxes.Add(BoundingBox.CreateFromPoints(translatedPositions));
                    positions.Add(new Vector3(position.X / 2, position.Y / 2, position.Z / 2));

                    for (int i = 0; i < indices.Count(); ++i)
                    {
                        indices[i] += (int)vertexCount;
                    }
                    finalIndices.AddRange(indices);

                    finalVertices.AddRange(translatedCopy);
                }

                _vertices.Add(finalVertices);
                _indices.Add(finalIndices);
            }

            _levelOctree.AddItems(positions.ToArray(), boundingBoxes.ToArray());

            for (int i = 0; i < _vertices.Count; ++i)
            {
                _vertexBuffers.Add(new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalTexture), _vertices[i].Count, BufferUsage.None));
                _vertexBuffers[i].SetData<VertexPositionNormalTexture>(_vertices[i].ToArray());
            }

            for (int i = 0; i < _indices.Count; ++i)
            {
                _indexBuffers.Add(new IndexBuffer(_graphicsDevice, typeof(int), _indices[i].Count, BufferUsage.None));
                _indexBuffers[i].SetData<int>(_indices[i].ToArray());
            }
        }

        public void GetRenderables(out List<VertexBuffer> vertices, out List<IndexBuffer> indices, out List<Effect> effects)
        {
            vertices = _vertexBuffers;
            indices = _indexBuffers;
            effects = _effects;
        }

        internal bool SelectCube(Ray pickingRay, out Vector3 cubePosition)
        {
            float? distance = _levelOctree.Intersects(pickingRay, out cubePosition);

            return distance.HasValue;

            //float shortestDistance = float.MaxValue;
            //cubePosition = new Vector3();

            //for (int x = 0; x < _levelLength; ++x)
            //{
            //    for (int y = 0; y < _levelHeight; ++y)
            //    {
            //        for (int z = 0; z < _levelWidth; ++z)
            //        {
            //            if (_levelCubeBoundingBoxes[x,y,z] != null)
            //            {
            //                float? distance = pickingRay.Intersects(_levelCubeBoundingBoxes[x, y, z]);
            //                if (distance.HasValue && distance.Value < shortestDistance)
            //                {
            //                    shortestDistance = distance.Value;

            //                    cubePosition.X = x;
            //                    cubePosition.Y = y;
            //                    cubePosition.Z = z;
            //                }
            //            }
            //        }
            //    }
            //}

            //if (shortestDistance == float.MaxValue)
            //{
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
        }

        public void AddCube(Vector3 position)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            int z = (int)position.Z;

            if (x >= _levelLength || x < 0 ||
                y >= _levelHeight || y < 0 ||
                z >= _levelWidth || z < 0)
            {
                return;
            }

            if (_level[x, y, z] == -1)
            {
                _level[x, y, z] = 0;
                AddRenderable(x, y, z);
            }  
        }

        private void AddRenderable(int x, int y, int z)
        {
            Vector3 basePosition = new Vector3(x, y, z);
            Vector3 position = 2 * basePosition;

            Matrix translation = Matrix.CreateTranslation(position);

            int modelIndex = _level[x, y, z];
            Model model = _levelCubes[modelIndex];

            List<VertexPositionNormalTexture> vertices;
            List<int> indices;
            GetModelVerticesAndIndices(model, out vertices, out indices);

            int vertexCount = _vertices[modelIndex].Count;

            VertexPositionNormalTexture[] translatedCopy = new VertexPositionNormalTexture[vertices.Count];
            vertices.CopyTo(translatedCopy);

            Vector3[] translatedPositions = new Vector3[translatedCopy.Length];
            for (int i = 0; i < translatedCopy.Length; ++i)
            {
                translatedCopy[i].Position = Vector3.Transform(translatedCopy[i].Position, translation);
                translatedPositions[i] = translatedCopy[i].Position;
            }

            _levelOctree.AddItems(new Vector3[] {basePosition}, new BoundingBox[] { BoundingBox.CreateFromPoints(translatedPositions) });
            //_levelCubeBoundingBoxes[(int)position.X / 2, (int)position.Y / 2, (int)position.Z / 2] = BoundingBox.CreateFromPoints(translatedPositions);

            for (int i = 0; i < indices.Count(); ++i)
            {
                indices[i] += (int)vertexCount;
            }

            _vertices[modelIndex].AddRange(translatedCopy);
            _indices[modelIndex].AddRange(indices);

            _vertexBuffers[modelIndex].Dispose();
            _vertexBuffers[modelIndex] = new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalTexture), _vertices[modelIndex].Count, BufferUsage.None);
            _vertexBuffers[modelIndex].SetData<VertexPositionNormalTexture>(_vertices[modelIndex].ToArray());

            _indexBuffers[modelIndex].Dispose();
            _indexBuffers[modelIndex] = new IndexBuffer(_graphicsDevice, typeof(int), _indices[modelIndex].Count, BufferUsage.None);
            _indexBuffers[modelIndex].SetData<int>(_indices[modelIndex].ToArray());
        }

        private void GetModelVerticesAndIndices(Model model, out List<VertexPositionNormalTexture> vertices, out List<int> indices)
        {
            vertices = new List<VertexPositionNormalTexture>();
            indices = new List<int>();
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    _effects.Add(meshPart.Effect);

                    UInt16[] shortIndexData = new UInt16[meshPart.PrimitiveCount * 3];
                    meshPart.IndexBuffer.GetData<UInt16>(shortIndexData);
                    int[] indexData = new int[shortIndexData.Length];
                    for (int i = 0; i < indexData.Length; ++i)
                    {
                        indexData[i] = shortIndexData[i] + vertices.Count;
                    }
                    indices.AddRange(indexData);

                    VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[meshPart.NumVertices];
                    meshPart.VertexBuffer.GetData<VertexPositionNormalTexture>(vertexData);
                    vertices.AddRange(vertexData);
                }
            }
        }
    }
}
