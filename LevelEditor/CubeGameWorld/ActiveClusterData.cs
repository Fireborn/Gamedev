using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLib;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;

namespace CubeGameWorld
{
    /// 
    /// <summary>
    /// A class which manages "active" clusters of data in a GameWorld instance which should be rendered.  For very large
    /// game worlds, we may want to load the whole level into memory, but only render a subset of the data.
    /// </summary>
    /// 
    class ActiveClusterData
    {
        #region Private Member Variables 

        /// <summary>
        /// The rendering data for each cluster, keyed by the cluster index
        /// </summary>
        private Dictionary<int, RenderData> _clusterRenderData;

        /// <summary>
        /// The X dimension of an individual cluster indicating how many cubes long a cluster is along the X axis
        /// </summary>
        private int _clusterXDimension;

        /// <summary>
        /// The Y dimension of an individual cluster indicating how many cubes long a cluster is along the Y axis
        /// </summary>
        private int _clusterYDimension;

        /// <summary>
        /// The Y dimension of an individual cluster indicating how many cubes long a cluster is along the Z axis
        /// </summary>
        private int _clusterZDimension;

        /// <summary>
        /// The "world" dimensions of each cube that makes up the level
        /// </summary>
        private int _cubeSize;

        /// <summary>
        /// The vertex buffers needed to render all of the active cluster data.  There is one vertex buffer per cluster, per model in that cluster
        /// </summary>
        private List<VertexBuffer> _allVertexBuffers = new List<VertexBuffer>();

        /// <summary>
        /// The index buffers needed to render all of the active cluster data.  There is one index buffer per cluster, per model in that cluster
        /// </summary>
        private List<IndexBuffer> _allIndexBuffers = new List<IndexBuffer>();

        /// <summary>
        /// The list of all the models needed to render the active cluster data. There may be repeats in here.  This matches 1 to 1 with the vertex
        /// and index buffer lists
        /// </summary>
        private List<Model> _allModels = new List<Model>();

        #endregion     

        #region Private Member Functions

        /// 
        /// <summary>
        /// Given an input model, extract the internal vertex list and the indices into that list specifying all the triangles of the model
        /// </summary>
        /// 
        /// <param name="model">
        /// The model to extract the vertices and indices from
        /// </param>
        /// 
        /// <param name="vertices">
        /// A list of all vertices in the model.  There may be repeat vertices with different normal or texture coordinates.
        /// </param>
        /// 
        /// <param name="indices">
        /// A list of indices specifying the vertices which make up the model triangles (every 3 indices specifies another triangle)
        /// </param>
        /// 
        private void GetModelVerticesAndIndices(Model model, out List<VertexPositionNormalTexture> vertices, out List<int> indices)
        {
            vertices = new List<VertexPositionNormalTexture>();
            indices = new List<int>();
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
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

        #endregion   

        #region Public Interface

        /// <summary>
        /// The octree for each cluster keyed by the cluster index.
        /// </summary>
        public Dictionary<int, Octree> Octrees { get; private set; }



        /// 
        /// <summary>
        /// Constructor
        /// </summary>
        /// 
        /// <param name="clusterXDimension">
        /// The number of cubes which make up a single cluster in the x direction
        /// </param>
        /// 
        /// <param name="clusterYDimension">
        /// The number of cubes which make up a single cluster in the y direction
        /// </param>
        /// 
        /// <param name="clusterZDimension">
        /// The number of cubes which make up a single cluster in the z direction
        /// </param>
        /// 
        /// <param name="cubeSize">
        /// The size of each cube in world coordinates
        /// </param>
        /// 
        public ActiveClusterData(int clusterXDimension, int clusterYDimension, int clusterZDimension, int cubeSize)
        {
            Octrees = new Dictionary<int, Octree>();
            _clusterRenderData = new Dictionary<int, RenderData>();

            _clusterXDimension = clusterXDimension;
            _clusterYDimension = clusterYDimension;
            _clusterZDimension = clusterZDimension;

            _cubeSize = cubeSize;
        }

        

        /// 
        /// <summary>
        /// Get everything needed to render the active set of clusters
        /// </summary>
        /// 
        /// <param name="vertices">
        /// A list of vertex buffers which need to be rendered
        /// </param>
        /// 
        /// <param name="indices">
        /// A list of index buffers corresponding to the output vertex buffers which need to be rendered
        /// </param>
        /// 
        /// <param name="models">
        /// A list of models corresponding to the output vertex buffers which need to be rendered.
        /// </param>
        /// 
        /// TODO: Don't necessarily need the entire model object to render.  Look at extracting the necessary peices and
        /// only passing those around.
        /// 
        public void GetRenderables(out List<VertexBuffer> vertices, out List<IndexBuffer> indices, out List<Model> models)
        {
            vertices = _allVertexBuffers;
            indices = _allIndexBuffers;
            models = _allModels;
        }



        /// 
        /// <summary>
        /// Get everything needed to render the active set of clusters
        /// </summary>
        /// 
        /// <param name="vertices">
        /// A list of vertex buffers which need to be rendered
        /// </param>
        /// 
        /// <param name="indices">
        /// A list of index buffers corresponding to the output vertex buffers which need to be rendered
        /// </param>
        /// 
        public void GetRenderables(out List<VertexBuffer> vertices, out List<IndexBuffer> indices)
        {
            vertices = _allVertexBuffers;
            indices = _allIndexBuffers;
        }



        /// 
        /// <summary>
        /// Add a new cluster to the set of clusters which are getting rendered.  If the cluster has already been added, this is effectively a no-op
        /// </summary>
        /// 
        /// <param name="clusterBaseWorldPosition">
        /// The base position of the cluster in world coordaintes (i.e. the minimum corner of the cluster bounding box)
        /// </param>
        /// 
        /// <param name="clusterIndex">
        /// The integer ID of the cluster, used to index into lists of cluster specific data
        /// </param>
        /// 
        /// <param name="clusterCubeIndices">
        /// An array of indices into the input cubes list indicating which cubes make up a given cluster.  The array is 4 dimensional
        /// where array[clusterIndex, x, y, z] will get you the index of the cube at local grid position  x, y, z in the cluster (or
        /// -1 if there is no cube in that grid position)
        /// </param>
        /// 
        /// <param name="cubes">
        /// A master list of cubes that the clusterCubeIndices references
        /// </param>
        /// 
        /// <param name="graphicsDevice">
        /// A graphics device instance, needed to build up vertex buffer objects in advance
        /// </param>
        /// 
        public void AddActiveCluster(Vector3 clusterBaseWorldPosition, int clusterIndex, int[, , ,] clusterCubeIndices, Model[] cubes, GraphicsDevice graphicsDevice)
        {
            // Do nothing if the cluster has already been added
            if (_clusterRenderData.ContainsKey(clusterIndex))
            {
                return;
            }

            RenderData renderData = new RenderData();
            _clusterRenderData.Add(clusterIndex, renderData);

            BoundingBox clusterBoundingBox = new BoundingBox(
                new Vector3(
                    -(_cubeSize * _clusterXDimension / 2),
                    -(_cubeSize * _clusterYDimension / 2),
                    -(_cubeSize * _clusterZDimension / 2)),
                new Vector3(
                    _cubeSize * _clusterXDimension / 2,
                    _cubeSize * _clusterYDimension / 2,
                    _cubeSize * _clusterZDimension / 2));

            Vector3 clusterCentroid = clusterBaseWorldPosition + new Vector3(_clusterXDimension, _clusterYDimension, _clusterZDimension) * _cubeSize / 2;

            Octrees.Add(clusterIndex, new Octree(clusterBoundingBox, clusterCentroid));

            // Build up the different positions of each model used within a cluster
            ModelData clusterModelData = new ModelData();

            for (int x = 0; x < _clusterXDimension; ++x)
            {
                for (int y = 0; y < _clusterYDimension; ++y)
                {
                    for (int z = 0; z < _clusterZDimension; ++z)
                    {
                        int modelIndex = clusterCubeIndices[clusterIndex, x, y, z];

                        if (modelIndex != -1)
                        {
                            Model model = cubes[modelIndex];
                            int clusterModelIndex = clusterModelData.FindIndex(model);

                            if (clusterModelIndex == -1)
                            {
                                clusterModelData.Add(model, new List<Vector3>());
                                clusterModelIndex = clusterModelData.Count - 1;
                            }

                            clusterModelData.ModelPositions(clusterModelIndex).Add(new Vector3(
                                clusterBaseWorldPosition.X + (x * _cubeSize),
                                clusterBaseWorldPosition.Y + (y * _cubeSize),
                                clusterBaseWorldPosition.Z + (z * _cubeSize)));
                        }
                    }
                }
            }

            List<BoundingBox> boundingBoxes = new List<BoundingBox>();
            List<Vector3> cubePositions = new List<Vector3>();

            for (int modelIndex = 0; modelIndex < clusterModelData.Count; ++modelIndex)
            {
                Model model = clusterModelData.Models(modelIndex);
                List<Vector3> positions = clusterModelData.ModelPositions(modelIndex);

                List<VertexPositionNormalTexture> vertices;
                List<int> indices;
                GetModelVerticesAndIndices(model, out vertices, out indices);

                List<VertexPositionNormalTexture> finalVertices = new List<VertexPositionNormalTexture>();
                List<int> finalIndices = new List<int>();
                int vertexCount = vertices.Count;
                foreach (Vector3 position in positions)
                {
                    VertexPositionNormalTexture[] translatedCopy = new VertexPositionNormalTexture[vertices.Count];
                    vertices.CopyTo(translatedCopy);

                    Matrix translation = Matrix.CreateTranslation((position) + Vector3.One);
                    Vector3[] translatedPositions = new Vector3[translatedCopy.Length];
                    for (int i = 0; i < translatedCopy.Length; ++i)
                    {
                        translatedCopy[i].Position = Vector3.Transform(translatedCopy[i].Position, translation);
                        translatedPositions[i] = translatedCopy[i].Position;
                    }

                    boundingBoxes.Add(BoundingBox.CreateFromPoints(translatedPositions));
                    cubePositions.Add(position);

                    finalIndices.AddRange(indices);

                    for (int i = 0; i < indices.Count(); ++i)
                    {
                        indices[i] += (int)vertexCount;
                    }

                    finalVertices.AddRange(translatedCopy);
                }

                VertexBuffer newVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), finalVertices.Count, BufferUsage.None);
                newVertexBuffer.SetData<VertexPositionNormalTexture>(finalVertices.ToArray());

                IndexBuffer newIndexBuffer = new IndexBuffer(graphicsDevice, typeof(int), finalIndices.Count, BufferUsage.None);
                newIndexBuffer.SetData<int>(finalIndices.ToArray());

                renderData.AddData(newVertexBuffer, newIndexBuffer, model);
            }

            Octrees[clusterIndex].AddItems(cubePositions.ToArray(), boundingBoxes.ToArray());
            int count = Octrees[clusterIndex].Count;

            _allModels.Clear();
            _allVertexBuffers.Clear();
            _allIndexBuffers.Clear();

            foreach (RenderData data in _clusterRenderData.Values)
            {
                ReadOnlyCollection<Model> models;
                ReadOnlyCollection<VertexBuffer> vertexBuffers;
                ReadOnlyCollection<IndexBuffer> indexBuffers;

                data.GetData(out vertexBuffers, out indexBuffers, out models);

                _allModels.AddRange(models);
                _allVertexBuffers.AddRange(vertexBuffers);
                _allIndexBuffers.AddRange(indexBuffers);
            }
        }



        /// 
        /// <summary>
        /// Remove a cluster from the set of active clusters to be rendered.  After this call, the cluster index passed in
        /// will not have it's renderable data returned by GetRenderables.
        /// </summary>
        /// 
        /// <param name="clusterIndex">
        /// The integer ID of the cluster to be removed
        /// </param>
        /// 
        public void RemoveActiveCluster(int clusterIndex)
        {
            _clusterRenderData.Remove(clusterIndex);
            Octrees.Remove(clusterIndex);
        }



        /// 
        /// <summary>
        /// Check to see if a given cluster has already been added to the active set of cluster data
        /// </summary>
        /// 
        /// <param name="clusterIndex">
        /// The integer ID of the cluster to check
        /// </param>
        /// 
        /// <returns>
        /// True if the cluster has already been added, false if it has not (or was since removed)
        /// </returns>
        /// 
        public bool Contains(int clusterIndex)
        {
            return _clusterRenderData.ContainsKey(clusterIndex);
        }

        #endregion
    }
}
