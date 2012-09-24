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
using System.IO;
using GameLib;

namespace CubeGameWorld
{
    /// 
    /// <summary>
    /// A class to encapsulate a game world.  This class handles pre-allocation optimized renderables and collision detection
    /// and picking within the environment.
    /// </summary>
    /// 
    public class GameWorld
    {
        #region Constants

        /// <summary>
        /// The number of cubes along the x direction which make up a cluster
        /// </summary>
        private const int _clusterXDimension = 50;

        /// <summary>
        /// The number of cubes along the y direction which make up a cluster
        /// </summary>
        private const int _clusterYDimension = 50;

        /// <summary>
        /// The number of cubes along the z direction which make up a cluster
        /// </summary>
        private const int _clusterZDimension = 50;

        /// <summary>
        /// The dimensions (in world coordinates) of the cube models composing the environment
        /// </summary>
        private const int _cubeSize = 2;

        /// <summary>
        /// A plane which is parallel to the top cube face of a cube centered at the origin
        /// </summary>
        private static readonly Plane _topCubeFace = new Plane(Vector3.Up, -_cubeSize / 2);

        /// <summary>
        /// A plane which is parallel to the bottom cube face of a cube centered at the origin
        /// </summary>
        private static readonly Plane _bottomCubeFace = new Plane(Vector3.Down, -_cubeSize / 2);

        /// <summary>
        /// A plane which is parallel to the left cube face of a cube centered at the origin
        /// </summary>
        private static readonly Plane _leftCubeFace = new Plane(Vector3.Left, -_cubeSize / 2);

        /// <summary>
        /// A plane which is parallel to the right cube face of a cube centered at the origin
        /// </summary>
        private static readonly Plane _rightCubeFace = new Plane(Vector3.Right, -_cubeSize / 2);

        /// <summary>
        /// A plane which is parallel to the front cube face of a cube centered at the origin
        /// </summary>
        private static readonly Plane _frontCubeFace = new Plane(Vector3.Forward, -_cubeSize / 2);

        /// <summary>
        /// A plane which is parallel to the back cube face of a cube centered at the origin
        /// </summary>
        private static readonly Plane _backCubeFace = new Plane(Vector3.Backward, -_cubeSize / 2);

        /// <summary>
        /// The maximum number of geometric primitives allowed per draw call
        /// </summary>
        private static readonly int _maxPrimitives = 1048575;

        #endregion

        #region Private Member Variables

        /// <summary>
        /// The number of clusters in the x direction which make up the entire game world 
        /// </summary>
        private int _clusterCountX;

        /// <summary>
        /// The number of clusters in the y direction which make up the entire game world
        /// </summary>
        private int _clusterCountY;

        /// <summary>
        /// The number ofclusters in the z direction which make up the entire game world
        /// </summary>
        private int _clusterCountZ;

        /// <summary>
        /// Each cluster is a set of cubes in a grid.  The cluster itself is identified by the smallest cube coordinate in the cluster
        /// (i.e. the minimum corner in grid space).  We then use that corner with this data structure to look up the cluster index
        /// which we can use to find cluster specific data in the other lists of cluster data tracked by this class.
        /// </summary>
        private Dictionary<Vector3, int> _clusterGridPositionToClusterIndex = new Dictionary<Vector3, int>();

        /// <summary>
        /// An array indicated the three dimensional contents of each cluster of the game world, indexed
        /// as [clusterIndex, clusterXPosition, clusterYPosition, clusterZPosition] where the cluster
        /// positions range from 0 to the dimensions of a given cluster in that axis.
        /// </summary>
        private int[, , ,] _gameWorldCubeIndices;

        /// <summary>
        /// The world coordinates of the minimum corner of each cluster, based on the cluster index
        /// </summary>
        private Vector3[] _clusterWorldPositions;

        /// <summary>
        /// The set of active cluster data used for rendering, picking, and collision detecting
        /// </summary>
        private ActiveClusterData _activeClusterData = new ActiveClusterData(_clusterXDimension, _clusterYDimension, _clusterZDimension, _cubeSize);

        /// <summary>
        /// A reference to the current graphics device
        /// </summary>
        private GraphicsDevice _graphicsDevice;

        #endregion

        #region Private Member Functions

        /// 
        /// <summary>
        /// Constructor which creates a fresh game world instance with nothing but a flat ground
        /// </summary>
        /// 
        /// <param name="worldXDimension">
        /// The number of cubes along the x dimension which make up the entire game world grid
        /// </param>
        /// 
        /// <param name="worldYDimension">
        /// The number of cubes along the y dimension which make up the entire game world grid
        /// </param>
        /// 
        /// <param name="worldZDimension">
        /// The number of cubes along the z dimension which make up the entire game world grid
        /// </param>
        /// 
        /// <param name="graphicsDevice">
        /// A reference to the graphics device so that the game world can preallocated renderables and such
        /// </param>
        /// 
        /// <param name="content">
        /// A reference to the content manager so the game world can managed it's own content in an optimized fashion
        /// </param>
        /// 
        private GameWorld(int worldXDimension, int worldYDimension, int worldZDimension, GraphicsDevice graphicsDevice, ContentManager content)
        {
            BasicInit(graphicsDevice, worldXDimension, worldYDimension, worldZDimension);

            Cubes = new Model[4];
            Cubes[0] = content.Load<Model>("GrassCube");
            Cubes[1] = content.Load<Model>("StoneCube");
            Cubes[2] = content.Load<Model>("DirtCube");
            Cubes[3] = content.Load<Model>("WaterCube");

            _cubeNames = new string[4];
            _cubeNames[0] = "GrassCube";
            _cubeNames[1] = "StoneCube";
            _cubeNames[2] = "DirtCube";
            _cubeNames[3] = "WaterCube";

            HashSet<Tuple<int, int, int, int>> visited = new HashSet<Tuple<int, int, int, int>>();
            // Create a flat surface centered at the origin
            for (int x = 0; x < worldXDimension; ++x)
            {
                for (int y = 0; y < worldYDimension; ++y)
                {
                    for (int z = 0; z < worldZDimension; ++z)
                    {
                        Vector3 worldPosition = new Vector3(x, y, z);
                        Vector3 clusterPosition = ClusterPosition(x, y, z);
                        Vector3 localPosition = LocalClusterPosition(x, y, z);

                        int index = _clusterGridPositionToClusterIndex[clusterPosition];

                        // Only place a cube where the "up" component is zero.
                        if (worldPosition * Vector3.Up == Vector3.Zero)
                        {
                            _gameWorldCubeIndices[index, (int)localPosition.X, (int)localPosition.Y, (int)localPosition.Z] = 0;
                        }
                        else
                        {
                            _gameWorldCubeIndices[index, (int)localPosition.X, (int)localPosition.Y, (int)localPosition.Z] = -1;
                        }
                    }
                }
            }
        }



        /// 
        /// <summary>
        /// Constructor which creates a game world based on a previously saved game world
        /// </summary>
        /// 
        /// <param name="filePath">
        /// The path of the game world file
        /// </param>
        /// 
        /// <param name="graphicsDevice">
        /// A reference to the graphics device so that the game world can pre-allocate renderables and such
        /// </param>
        /// 
        /// <param name="content">
        /// A reference to the content manager so the game world can do some optimized handling of it's own content
        /// </param>
        /// 
        private GameWorld(string filePath, GraphicsDevice graphicsDevice, ContentManager content)
        {
            using (StreamReader reader = new StreamReader(File.Open(filePath, FileMode.Open)))
            {
                int cubeCount = int.Parse(reader.ReadLine());

                Cubes = new Model[cubeCount];
                _cubeNames = new string[cubeCount];

                for (int i = 0; i < cubeCount; ++i)
                {
                    _cubeNames[i] = reader.ReadLine();
                    Cubes[i] = content.Load<Model>(_cubeNames[i]);
                }

                int worldXDimension = int.Parse(reader.ReadLine());
                int worldYDimension = int.Parse(reader.ReadLine());
                int worldZDimension = int.Parse(reader.ReadLine());

                BasicInit(graphicsDevice, worldXDimension, worldYDimension, worldZDimension);

                // Construct an array for the entire game world and dump it to file
                int[, ,] world = new int[worldXDimension, worldYDimension, worldZDimension];
                for (int x = 0; x < worldXDimension; ++x)
                {
                    for (int y = 0; y < worldYDimension; ++y)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(' ');
                        if (values.Length != worldZDimension)
                        {
                            throw new FileLoadException("Game world file appears to be corrupt");
                        }

                        for (int z = 0; z < worldZDimension; ++z)
                        {
                            world[x, y, z] = int.Parse(values[z]);
                        }
                    }
                }

                foreach (KeyValuePair<Vector3, int> cluster in _clusterGridPositionToClusterIndex)
                {
                    int clusterIndex = cluster.Value;
                    Vector3 clusterBasePosition = cluster.Key;
                    clusterBasePosition.X *= _clusterXDimension;
                    clusterBasePosition.Y *= _clusterYDimension;
                    clusterBasePosition.Z *= _clusterZDimension;

                    for (int x = 0; x < _clusterXDimension; ++x)
                    {
                        for (int y = 0; y < _clusterYDimension; ++y)
                        {
                            for (int z = 0; z < _clusterZDimension; ++z)
                            {
                                Vector3 worldPos = clusterBasePosition + new Vector3(x, y, z);

                                _gameWorldCubeIndices[clusterIndex, x, y, z] = world[(int)worldPos.X, (int)worldPos.Y, (int)worldPos.Z];
                            }
                        }
                    }
                }
            }
        }



        /// 
        /// <summary>
        /// Given a grid position, find the local grid positon within the cluster that it belongs to
        /// </summary>
        /// 
        /// <param name="x">
        /// The x coordinate of the grid position
        /// </param>
        /// 
        /// <param name="y">
        /// The y coordinate of the grid position
        /// </param>
        /// 
        /// <param name="z">
        /// The z coordinate of the grid position
        /// </param>
        /// 
        /// <returns>
        /// The local grid position relative to the minimum grid position of the containing cluster
        /// </returns>
        /// 
        private Vector3 LocalClusterPosition(int x, int y, int z)
        {
            Vector3 localPos = new Vector3();
            localPos.X = x % _clusterXDimension;
            localPos.Y = y % _clusterYDimension;
            localPos.Z = z % _clusterZDimension;

            return localPos;
        }



        /// 
        /// <summary>
        /// Given an input grid position in the game world, find the base grid position of the cluster that it belongs to
        /// </summary>
        /// 
        /// <param name="x">
        /// The x coordinate of a grid position
        /// </param>
        /// 
        /// <param name="y">
        /// The y coordinate of a grid position
        /// </param>
        /// 
        /// <param name="z">
        /// The z coordinate of a grid position
        /// </param>
        /// 
        /// <returns>
        /// The smallest grid position within the cluster that contains the input grid position
        /// </returns>
        /// 
        private Vector3 ClusterPosition(int x, int y, int z)
        {
            // TODO: Does the C# spec define what happens with integer division?  I might be doing all these casts and calling Floor() for nothing...
            Vector3 clusterPos = new Vector3();
            clusterPos.X = (float)Math.Floor((float)x / (float)_clusterXDimension);
            clusterPos.Y = (float)Math.Floor((float)y / (float)_clusterYDimension);
            clusterPos.Z = (float)Math.Floor((float)z / (float)_clusterZDimension);

            return clusterPos;
        }



        /// 
        /// <summary>
        /// Perform basic initialization of a GameWorld object
        /// </summary>
        /// 
        /// <param name="graphicsDevice">
        /// A reference to the graphics device
        /// </param>
        /// 
        /// <param name="worldXDimension">
        /// The number of cubes along the x axis needed to make up the entire game world
        /// </param>
        /// 
        /// <param name="worldYDimension">
        /// The number of cubes along the y axis needed to make up the entire game world
        /// </param>
        /// 
        /// <param name="worldZDimension">
        /// The number of cubes along the z axis needed to make up the entire game world
        /// </param>
        /// 
        private void BasicInit(GraphicsDevice graphicsDevice, int worldXDimension, int worldYDimension, int worldZDimension)
        {
            _graphicsDevice = graphicsDevice;

            _clusterCountX = (int)Math.Ceiling((double)worldXDimension / (double)_clusterXDimension);
            _clusterCountY = (int)Math.Ceiling((double)worldYDimension / (double)_clusterYDimension);
            _clusterCountZ = (int)Math.Ceiling((double)worldZDimension / (double)_clusterZDimension);

            int numberOfClusters = _clusterCountX * _clusterCountY * _clusterCountZ;
            _gameWorldCubeIndices = new int[numberOfClusters, _clusterXDimension, _clusterYDimension, _clusterZDimension];

            int clusterIndex = 0;
            for (int x = 0; x < worldXDimension; ++x)
            {
                for (int y = 0; y < worldYDimension; ++y)
                {
                    for (int z = 0; z < worldZDimension; ++z)
                    {
                        Vector3 clusterPos = ClusterPosition(x, y, z);
                        if (!_clusterGridPositionToClusterIndex.ContainsKey(clusterPos))
                        {
                            _clusterGridPositionToClusterIndex.Add(clusterPos, clusterIndex);
                            ++clusterIndex;
                        }
                    }
                }
            }

            _clusterWorldPositions = new Vector3[_clusterGridPositionToClusterIndex.Count];
            foreach (KeyValuePair<Vector3, int> cluster in _clusterGridPositionToClusterIndex)
            {
                _clusterWorldPositions[cluster.Value] = new Vector3(
                    cluster.Key.X * _clusterXDimension * _cubeSize,
                    cluster.Key.Y * _clusterYDimension * _cubeSize,
                    cluster.Key.Z * _clusterZDimension * _cubeSize);
            }
        }



        /// 
        /// <summary>
        /// Given a picking ray which intersects a cube centered at the origin and a minimum distance, check to see if the ray intersects one of the cube faces
        /// of that cube in a shorter distance than the minimum distance.  If so, we want to update the minimum distance and the direction corresponding to the cube
        /// face we just checked.
        /// </summary>
        /// 
        /// <param name="pickingRay">
        /// A picking ray which intersects a cube at the origin
        /// </param>
        /// 
        /// <param name="cubeFacePlane">
        /// A plane representing one of the cube faces
        /// </param>
        /// 
        /// <param name="cubeFaceVector">
        /// A vector indicating which cube face plane was passed in
        /// </param>
        /// 
        /// <param name="minimumFaceDistance">
        /// The minimum distance to a cube face seen thus far.  If we find a better distance, we will replace this
        /// </param>
        /// 
        /// <param name="minimumFaceDirection">
        /// The vector indicating which cube face has the minimum distance intersection thus far.  if we find a better minimum distance we need to update this.
        /// </param>
        /// 
        private void CheckMinimumFace(Ray pickingRay, Plane cubeFacePlane, Vector3 cubeFaceVector, ref float minimumFaceDistance, ref Vector3 minimumFaceDirection)
        {
            float? faceDistance = pickingRay.Intersects(cubeFacePlane);
            if (faceDistance.HasValue && faceDistance.Value < minimumFaceDistance)
            {
                Vector3 intersectionPoint = pickingRay.Position + pickingRay.Direction * faceDistance.Value;
                if (intersectionPoint.X <= _cubeSize / 2 &&
                    intersectionPoint.Y <= _cubeSize / 2 &&
                    intersectionPoint.Z <= _cubeSize / 2 &&
                    intersectionPoint.X >= -_cubeSize / 2 &&
                    intersectionPoint.Y >= -_cubeSize / 2 &&
                    intersectionPoint.Z >= -_cubeSize / 2)
                {
                    minimumFaceDistance = faceDistance.Value;
                    minimumFaceDirection = cubeFaceVector;
                }
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// The master list of cubes models used to represent the game world
        /// </summary>
        public Model[] Cubes { get; private set; }

        /// <summary>
        /// The actual content name strings associated with the list of cubes
        /// </summary>
        public String[] _cubeNames { get; private set; }

        /// 
        /// <summary>
        /// Delete a cube based on the grid position of that cube.  Caution: This will cause an entire cluster of the game world to be
        /// regenerated, which may have a large performance impact.
        /// </summary>
        /// 
        /// <param name="position">
        /// The grid position of the cube to delete
        /// </param>
        /// 
        public void DeleteCube(Vector3 position)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            int z = (int)position.Z;

            Vector3 clusterBasePosition = ClusterPosition(x, y, z);
            if (!_clusterGridPositionToClusterIndex.ContainsKey(clusterBasePosition))
            {
                // Someone tried to add a cube in a position which we don't have a cluster for
                return;
            }

            int clusterIndex = _clusterGridPositionToClusterIndex[clusterBasePosition];
            Vector3 localClusterPosition = LocalClusterPosition(x, y, z);

            if (_gameWorldCubeIndices[clusterIndex, (int)localClusterPosition.X, (int)localClusterPosition.Y, (int)localClusterPosition.Z] != -1)
            {
                _gameWorldCubeIndices[clusterIndex, (int)localClusterPosition.X, (int)localClusterPosition.Y, (int)localClusterPosition.Z] = -1;

                // Regenerate the cluster from scratch to reflect the changes
                _activeClusterData.RemoveActiveCluster(clusterIndex);
                _activeClusterData.AddActiveCluster(_clusterWorldPositions[clusterIndex], clusterIndex, _gameWorldCubeIndices, Cubes, _graphicsDevice);
            }
        }



        /// 
        /// <summary>
        /// Add a cube to the input grid position.  Caution: This will cause an entire cluster of the game world to be
        /// regenerated, which may have a large performance impact. 
        /// </summary>
        /// 
        /// <param name="position">
        /// The grid position to add the cube to
        /// </param>
        /// 
        /// <param name="cubeIndexToAdd">
        /// The index of the cube model to be added in the input position
        /// </param>
        /// 
        public void AddCube(Vector3 position, int cubeIndexToAdd)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            int z = (int)position.Z;

            Vector3 clusterBasePosition = ClusterPosition(x, y, z);
            if (!_clusterGridPositionToClusterIndex.ContainsKey(clusterBasePosition))
            {
                // Someone tried to add a cube in a position which we don't have a cluster for
                return;
            }

            int clusterIndex = _clusterGridPositionToClusterIndex[clusterBasePosition];
            Vector3 localClusterPosition = LocalClusterPosition(x, y, z);

            if (_gameWorldCubeIndices[clusterIndex, (int)localClusterPosition.X, (int)localClusterPosition.Y, (int)localClusterPosition.Z] == -1)
            {
                _gameWorldCubeIndices[clusterIndex, (int)localClusterPosition.X, (int)localClusterPosition.Y, (int)localClusterPosition.Z] = cubeIndexToAdd;

                // Regenerate the cluster from scratch to reflect the changes
                _activeClusterData.RemoveActiveCluster(clusterIndex);
                _activeClusterData.AddActiveCluster(_clusterWorldPositions[clusterIndex], clusterIndex, _gameWorldCubeIndices, Cubes, _graphicsDevice);
            }
        }



        /// 
        /// <summary>
        /// Create a brand new game world with nothing but a flat ground plane
        /// </summary>
        /// 
        /// <param name="graphicsDevice">
        /// A reference to the graphics device so that we can pre-allocate renderables and such
        /// </param>
        /// 
        /// <param name="content">
        /// A reference to the content manager so that we can load the game world content and use it to create optimized renderables, collision bounding boxes, etc.
        /// </param>
        /// 
        /// <param name="worldXDimension">
        /// The number of cubes which make up the entire game world in the x direction
        /// </param>
        /// 
        /// <param name="worldYDimension">
        /// The number of cubes which make up the entire game world in the y direction
        /// </param>
        /// 
        /// <param name="worldZDimension">
        /// The number of cubes which make up the entire game world in the z direction
        /// </param>
        /// 
        /// <returns>
        /// A newly created GameWorld instance
        /// </returns>
        /// 
        public static GameWorld CreateNew(
            GraphicsDevice graphicsDevice,
            ContentManager content,
            int worldXDimension,
            int worldYDimension,
            int worldZDimension)
        {
            return new GameWorld(worldXDimension, worldYDimension, worldZDimension, graphicsDevice, content);
        }



        /// 
        /// <summary>
        /// Create a new GameWorld object by loading data from a file
        /// </summary>
        /// 
        /// <param name="filePath">
        /// The path of the game world file
        /// </param>
        /// 
        /// <param name="graphicsDevice">
        /// A reference to the graphics device so the GameWorld can pre-allocate renderables
        /// </param>
        /// 
        /// <param name="content">
        /// A reference to the content manager so the GameWorld can load it's content and create optimized data structures for rendering/picking/etc.
        /// </param>
        /// 
        /// <returns>
        /// A newly created GameWorld object which is ready to render based off of the contents of the input file
        /// </returns>
        /// 
        public static GameWorld Load(
            string filePath,
            GraphicsDevice graphicsDevice,
            ContentManager content)
        {
            return new GameWorld(filePath, graphicsDevice, content);
        }



        /// 
        /// <summary>
        /// Seralize the game world to file
        /// </summary>
        /// 
        /// <param name="filePath">
        /// The path/filename to write the GameWorld data to
        /// </param>
        /// 
        public void Save(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(File.Open(filePath, FileMode.Create)))
            {
                writer.WriteLine(_cubeNames.Length);

                foreach (string cube in _cubeNames)
                {
                    writer.WriteLine(cube);
                }

                int worldXDimension = _clusterCountX * _clusterXDimension;
                int worldYDimension = _clusterCountY * _clusterYDimension;
                int worldZDimension = _clusterCountZ * _clusterZDimension;

                writer.WriteLine(worldXDimension);
                writer.WriteLine(worldYDimension);
                writer.WriteLine(worldZDimension);

                // Construct an array for the entire game world and dump it to file
                int[, ,] world = new int[worldXDimension, worldYDimension, worldZDimension];

                foreach (KeyValuePair<Vector3, int> cluster in _clusterGridPositionToClusterIndex)
                {
                    int clusterIndex = cluster.Value;
                    Vector3 clusterBasePosition = cluster.Key;
                    clusterBasePosition.X *= _clusterXDimension;
                    clusterBasePosition.Y *= _clusterYDimension;
                    clusterBasePosition.Z *= _clusterZDimension;

                    for (int x = 0; x < _clusterXDimension; ++x)
                    {
                        for (int y = 0; y < _clusterYDimension; ++y)
                        {
                            for (int z = 0; z < _clusterZDimension; ++z)
                            {
                                Vector3 worldPos = clusterBasePosition + new Vector3(x, y, z);

                                int cubeIndex = _gameWorldCubeIndices[clusterIndex, x, y, z];

                                world[(int)worldPos.X, (int)worldPos.Y, (int)worldPos.Z] = cubeIndex;
                            }
                        }
                    }
                }

                for (int x = 0; x < worldXDimension; ++x)
                {
                    for (int y = 0; y < worldYDimension; ++y)
                    {
                        for (int z = 0; z < worldZDimension; ++z)
                        {
                            writer.Write("{0}", world[x, y, z]);
                            if (z != worldZDimension - 1)
                            {
                                writer.Write(" ");
                            }
                        }

                        writer.WriteLine();
                    }
                }
            }
        }



        /// 
        /// <summary>
        /// Specify what data within the GameWorld should be rendered and used for things like picking and collision detection
        /// </summary>
        /// 
        /// <param name="centroid">
        /// A grid position indicating the center of the region which should be active
        /// </param>
        /// 
        /// <param name="minimumRadius">
        /// The "radius" of cubes which should be active.  This actually just lets us know which clusters to load into the active set,
        /// so we don't really get a spherical data set.
        /// </param>
        /// 
        public void SetActiveRenderArea(Vector3 centroid, float minimumRadius)
        {
            int clusterXRadius = (int)Math.Ceiling(minimumRadius / (float)_clusterXDimension);
            int clusterYRadius = (int)Math.Ceiling(minimumRadius / (float)_clusterYDimension);
            int clusterZRadius = (int)Math.Ceiling(minimumRadius / (float)_clusterZDimension);

            Vector3 centerClusterPos = ClusterPosition((int)centroid.X, (int)centroid.Y, (int)centroid.Z);
            for (int x = -clusterXRadius; x < clusterXRadius; ++x)
            {
                for (int y = -clusterYRadius; y < clusterYRadius; ++y)
                {
                    for (int z = -clusterZRadius; z < clusterZRadius; ++z)
                    {
                        Vector3 clusterPos = centerClusterPos + new Vector3(x, y, z);

                        if (_clusterGridPositionToClusterIndex.ContainsKey(clusterPos))
                        {
                            int clusterIndex = _clusterGridPositionToClusterIndex[clusterPos];
                            if (!_activeClusterData.Contains(clusterIndex))
                            {
                                _activeClusterData.AddActiveCluster(_clusterWorldPositions[clusterIndex], clusterIndex, _gameWorldCubeIndices, Cubes, _graphicsDevice);
                            }
                        }
                    }
                }
            }
        }



        /// 
        /// <summary>
        /// Get pre-allocated data for rendering.  There is a 1 to 1 mapping betwen the three output lists.  They are all guarnteed to be the same size
        /// </summary>
        /// 
        /// <param name="vertices">
        /// A list of vertex buffers to be rendered
        /// </param>
        /// 
        /// <param name="indices">
        /// A list of index buffers corresponding to the vertex buffers prepared for Triangle List type rendering
        /// </param>
        /// 
        /// <param name="models">
        /// A list of model data corresponding to the vertex buffers so we know what textures/effects to use when rendering
        /// </param>
        /// 
        public void GetRenderables(out List<VertexBuffer> vertices, out List<IndexBuffer> indices, out List<Model> models)
        {
            _activeClusterData.GetRenderables(out vertices, out indices, out models);
        }



        /// 
        /// <summary>
        /// Get pre-allocated data for rendering.  There is a 1 to 1 mapping betwen the three output lists.  They are all guarnteed to be the same size
        /// </summary>
        /// 
        /// <param name="vertices">
        /// A list of vertex buffers to be rendered
        /// </param>
        /// 
        /// <param name="indices">
        /// A list of index buffers corresponding to the vertex buffers prepared for Triangle List type rendering
        /// </param>
        /// 
        public void GetRenderables(out List<VertexBuffer> vertices, out List<IndexBuffer> indices)
        {
            _activeClusterData.GetRenderables(out vertices, out indices);
        }



        /// 
        /// <summary>
        /// Given a picking ray, find the cube that the ray intersects.  The neighboring cube position is also outputted and
        /// is the grid position which is adjacent to the cube face that the ray intersects
        /// </summary>
        /// 
        /// <param name="pickingRay">
        /// The picking ray to project into the game world to find a cube intersection
        /// </param>
        /// 
        /// <param name="intersectedCubePosition">
        /// The grid position of the nearest cube intersected by the picking ray (undefined if there is no intersection)
        /// </param>
        /// 
        /// <param name="neighboringCubePosition">
        /// The grid position adjacent to the cube face intersected by the picking ray (undefined if there is no intersection)
        /// </param>
        /// 
        /// <returns>
        /// True if there was an intersection, false if there was no intersection
        /// </returns>
        /// 
        public bool SelectCube(Ray pickingRay, out Vector3 intersectedCubePosition, out Vector3 neighboringCubePosition)
        {
            float minimumDistance = float.MaxValue;
            Vector3 cubePosition = new Vector3();
            intersectedCubePosition = cubePosition;
            neighboringCubePosition = cubePosition;

            foreach (Octree octree in _activeClusterData.Octrees.Values)
            {
                float? distance = octree.Intersects(pickingRay, out cubePosition);
                if (distance.HasValue && distance.Value < minimumDistance)
                {
                    minimumDistance = distance.Value;
                    intersectedCubePosition = cubePosition;
                }
            }

            if (minimumDistance != float.MaxValue)
            {
                // transform picking ray to intersect the same cube if it were in fact a unit cube
                Vector3 cubeCenter = (intersectedCubePosition) + new Vector3(_cubeSize / 2, _cubeSize / 2, _cubeSize / 2);
                pickingRay.Position -= cubeCenter;

                float minimumFaceDistance = float.MaxValue;
                Vector3 minimumFaceDirection = Vector3.Zero;

                CheckMinimumFace(pickingRay, _topCubeFace, Vector3.Up, ref minimumFaceDistance, ref minimumFaceDirection);
                CheckMinimumFace(pickingRay, _bottomCubeFace, Vector3.Down, ref minimumFaceDistance, ref minimumFaceDirection);
                CheckMinimumFace(pickingRay, _leftCubeFace, Vector3.Left, ref minimumFaceDistance, ref minimumFaceDirection);
                CheckMinimumFace(pickingRay, _rightCubeFace, Vector3.Right, ref minimumFaceDistance, ref minimumFaceDirection);
                CheckMinimumFace(pickingRay, _frontCubeFace, Vector3.Forward, ref minimumFaceDistance, ref minimumFaceDirection);
                CheckMinimumFace(pickingRay, _backCubeFace, Vector3.Backward, ref minimumFaceDistance, ref minimumFaceDirection);

                intersectedCubePosition = (intersectedCubePosition / _cubeSize);
                neighboringCubePosition = intersectedCubePosition + minimumFaceDirection;

                return true;
            }

            return false;
        }

        public void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, Matrix world, Matrix view, Matrix projection)
        {
            List<VertexBuffer> vertices;
            List<IndexBuffer> indices;
            List<Model> models;
            GetRenderables(out vertices, out indices, out models);

            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            for (int i = 0; i < vertices.Count; ++i)
            {
                Model model = models[i];
                foreach (ModelMesh mesh in model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = world;
                        effect.View = view;
                        effect.Projection = projection;
                        effect.LightingEnabled = true;
                        effect.EnableDefaultLighting();
                        effect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);

                        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();

                            graphicsDevice.SetVertexBuffer(vertices[i]);
                            graphicsDevice.Indices = indices[i];

                            int primitivesToRender = indices[i].IndexCount / 3;
                            int startIndex = 0;
                            while (primitivesToRender > _maxPrimitives)
                            {
                                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices[i].VertexCount, startIndex, _maxPrimitives);
                                startIndex += _maxPrimitives * 3;
                                primitivesToRender -= _maxPrimitives;
                            }
                            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices[i].VertexCount, startIndex, primitivesToRender);
                        }
                    }
                }
            }
        }

        #endregion  
    }
}
