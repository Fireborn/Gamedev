using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace GameLib
{
    public class Octree
    {
        private class OctreeNode
        {
            /// 
            /// <summary>
            /// C'tor
            /// </summary>
            /// 
            /// <param name="boundingBoxIndex">
            /// An index into a master list of bounding boxes, all centered at the origin
            /// </param>
            /// 
            /// <param name="boundingBoxPosition">
            /// The position of the minimum corner of the bounding box
            /// </param>
            /// 
            public OctreeNode(int regionBoundingBoxIndex, Vector3 regionBoundingBoxPosition)
            {
                BoundingBoxIndex = regionBoundingBoxIndex;
                BoundingBoxPosition = regionBoundingBoxPosition;
                InternalObjectPositions = new List<Vector3>();
                InternalObjectBoundingBoxes = new List<BoundingBox>();
            }



            /// 
            /// <summary>
            /// Add an object to the node's internal storage (this object should intersect the node's bounding box)
            /// </summary>
            /// 
            /// <param name="objectPosition">
            /// The position of the object (centroid)
            /// </param>
            /// 
            /// <param name="objectBoundingBox">
            /// The bounding box of the object
            /// </param>
            /// 
            public void AddObject(Vector3 objectPosition, BoundingBox objectBoundingBox)
            {
                InternalObjectPositions.Add(objectPosition);
                InternalObjectBoundingBoxes.Add(objectBoundingBox);
            }



            /// 
            /// <summary>
            /// Remove all objects within this node region
            /// </summary>
            /// 
            public void ClearObjects()
            {
                InternalObjectBoundingBoxes.Clear();
                InternalObjectPositions.Clear();
            }

            /// <summary>
            /// The index into a master list of bounding boxes which represents a bounding box for
            /// this node of the octree
            /// </summary>
            public int BoundingBoxIndex;

            /// <summary>
            /// The position of the minimum corner of the node bounding box
            /// </summary>
            public Vector3 BoundingBoxPosition;

            /// <summary>
            /// The positions of the objects contained within this region of space (centroid).
            /// This should only be populated for leaf nodes.
            /// </summary>
            public List<Vector3> InternalObjectPositions { get; private set; }
            
            /// <summary>
            /// The bounding boxes of the objects contained within this region of space.
            /// This should only be populated for leaf nodes.
            /// </summary>
            public List<BoundingBox> InternalObjectBoundingBoxes { get; private set; }

            /// <summary>
            /// If the region of space described by this node is broken apart, then the array of children
            /// will be exactly 8 elements.  Otherwise, this is a leaf node and it will be null.
            /// </summary>
            public OctreeNode[] Children = null;
        }

        /// <summary>
        /// The root node of the octree
        /// </summary>
        private OctreeNode _root;

        /// <summary>
        /// The master list of bounding boxes so that we can re-use bounding boxes of the same size in multiple
        /// positions instead of storing a bunch of distinct, yet same-volume bounding boxes.
        /// </summary>
        private List<BoundingBox> _masterBoundingBoxList = new List<BoundingBox>();

        /// <summary>
        /// The maximum number of objects that a quadtree node can hold until it is broken down into multiple nodes
        /// to speed up queries.
        /// </summary>
        private const int _maximumObjectsInNode = 500;



        /// 
        /// <summary>
        /// C'tor
        /// </summary>
        /// 
        /// <param name="topLevelBoundingBox">
        /// A top level bounding showing the size of the region that this octree will represent
        /// </param>
        /// 
        /// <param name="topLevelBoundingBoxPosition">
        /// The world coordinates of the minimum corner of the top level bounding box
        /// </param>
        /// 
        public Octree(BoundingBox topLevelBoundingBox, Vector3 topLevelBoundingBoxPosition)
        {
            _masterBoundingBoxList.Add(topLevelBoundingBox);
            _root = new OctreeNode(_masterBoundingBoxList.Count - 1, topLevelBoundingBoxPosition);
        }



        /// 
        /// <summary>
        /// Add items to the Octree
        /// </summary>
        /// 
        /// <param name="itemPositions">
        /// The positions (centroid) of the items being added
        /// </param>
        /// 
        /// <param name="itemBoundingBoxes">
        /// The bounding boxes for each item being added
        /// </param>
        /// 
        public void AddItems(Vector3[] itemPositions, BoundingBox[] itemBoundingBoxes)
        {
            AddItems(itemPositions, itemBoundingBoxes, _root);
        }



        /// 
        /// <summary>
        /// Add items to the Octree
        /// </summary>
        /// 
        /// <param name="itemPositions">
        /// The positions (centroid) of the items being added
        /// </param>
        /// 
        /// <param name="itemBoundingBoxes">
        /// The bounding boxes for each item being added
        /// </param>
        /// 
        /// <param name="searchRoot">
        /// The node to attempt to add the items within
        /// </param>
        /// 
        private void AddItems(Vector3[] itemPositions, BoundingBox[] itemBoundingBoxes, OctreeNode searchRoot)
        {
            if (itemPositions.Length != itemBoundingBoxes.Length)
            {
                throw new ArgumentException("there must be as many item positions as there are item bounding boxes");
            }

            for (int i = 0; i < itemPositions.Length; ++i)
            {
                List<OctreeNode> nodes = FindContainingNodes(searchRoot, itemBoundingBoxes[i]);

                foreach (OctreeNode node in nodes)
                {
                    if (node.InternalObjectPositions.Count + 1 >= _maximumObjectsInNode)
                    {
                        BreakDownNode(node);
                        List<OctreeNode> newNodes = FindContainingNodes(node, itemBoundingBoxes[i]);
                        foreach (OctreeNode newNode in newNodes)
                        {
                            // These nodes will be brand new, so we can always add 1 item to them without checks
                            newNode.AddObject(itemPositions[i], itemBoundingBoxes[i]);
                        }
                    }
                    else
                    {
                        node.AddObject(itemPositions[i], itemBoundingBoxes[i]);
                    }
                }
            }
        }



        /// 
        /// <summary>
        /// Given a bounding box, find all the child octree node which intersect the bounding box (in case
        /// the bounding box crosses octree regions)
        /// </summary>
        /// 
        /// <param name="parentNode">
        /// The root node to start the search from
        /// </param>
        /// 
        /// <param name="boundingBox">
        /// The bounding box to intersect against the octree regions
        /// </param>
        /// 
        private List<OctreeNode> FindContainingNodes(OctreeNode parentNode, BoundingBox boundingBox)
        {
            List<OctreeNode> nodes = new List<OctreeNode>();

            BoundingBox translatedBoundingBox = _masterBoundingBoxList[parentNode.BoundingBoxIndex];
            translatedBoundingBox.Max += parentNode.BoundingBoxPosition;
            translatedBoundingBox.Min += parentNode.BoundingBoxPosition;

            if (translatedBoundingBox.Intersects(boundingBox))
            {
                if (parentNode.Children == null)
                {
                    nodes.Add(parentNode);
                    return nodes;
                }
                else
                {
                    foreach (OctreeNode childNode in parentNode.Children)
                    {
                        nodes.AddRange(FindContainingNodes(childNode, boundingBox));
                    }
                    return nodes;
                }
            }

            return nodes;
        }



        /// 
        /// <summary>
        /// Subdivide a leaf node into 8 equal volumes and distribute the objects within that node across the 8 new children
        /// </summary>
        /// 
        /// <param name="node">
        /// The node to break down
        /// </param>
        /// 
        private void BreakDownNode(OctreeNode node)
        {
            // We shouldn't ever call this on a node that has already been broken down but if for some reason
            // we do we should fail in a noticable manner so we can debug the problem rather than tracking
            // down the silent error based on some unpredicted side effect.
            if (node.Children != null)
            {
                throw new ArgumentException("Called Octree.BreakDownNode on a node which has already been broken down");
            }

            node.Children = new OctreeNode[8];

            BoundingBox nodeBoundingBox = _masterBoundingBoxList[node.BoundingBoxIndex];
            nodeBoundingBox.Max += node.BoundingBoxPosition;
            nodeBoundingBox.Min += node.BoundingBoxPosition;

            Vector3[] nodeCorners = nodeBoundingBox.GetCorners();
            Vector3 nodeCenter = (nodeBoundingBox.Max + nodeBoundingBox.Min) / 2.0f;

            // shrink our bounding box by a factor of 2.  We will use the same bounding box for all 8 children
            Vector3[] points = new Vector3[2]; 
            points[0] = _masterBoundingBoxList[node.BoundingBoxIndex].Max / 2.0f;
            points[1] = _masterBoundingBoxList[node.BoundingBoxIndex].Min / 2.0f;
            BoundingBox newBoundingBox = BoundingBox.CreateFromPoints(points);

            int boundingBoxIndex;
            if (_masterBoundingBoxList.Contains(newBoundingBox))
            {
                boundingBoxIndex = _masterBoundingBoxList.FindIndex(x => x == newBoundingBox);
            }
            else
            {
                _masterBoundingBoxList.Add(newBoundingBox);
                boundingBoxIndex =_masterBoundingBoxList.Count - 1;
            }

            for (int i = 0; i < node.Children.Length; ++i)
            {
                Vector3 subRegionCenter = (nodeCorners[i] + nodeCenter) / 2.0f;

                node.Children[i] = new OctreeNode(boundingBoxIndex, subRegionCenter);
            }

            AddItems(node.InternalObjectPositions.ToArray(), node.InternalObjectBoundingBoxes.ToArray(), node);
            node.ClearObjects();
        }



        /// 
        /// <summary>
        /// The number of objects stored within the octree
        /// </summary>
        /// 
        public int Count
        {
            get
            {
                return GetNodeCount(_root);
            }
        }



        /// 
        /// <summary>
        /// Recursively extract the number of objects from the nodes of the octree
        /// </summary>
        /// 
        /// <param name="node">
        /// The root node to start counting from
        /// </param>
        /// 
        /// <returns>
        /// The number of objects stored within the input root node
        /// </returns>
        /// 
        private int GetNodeCount(OctreeNode node)
        {
            int nodeCount = 0;

            if (node.Children == null)
            {
                nodeCount += node.InternalObjectPositions.Count;
            }
            else
            {
                foreach (OctreeNode childNode in node.Children)
                {
                    nodeCount += GetNodeCount(childNode);
                }
            }

            return nodeCount;
        }



        /// 
        /// <summary>
        /// Find the nearest object within the octree that intersects the ray
        /// </summary>
        /// 
        /// <param name="ray">
        /// The ray to check against for intersections
        /// </param>
        /// 
        /// <param name="intersectedPosition">
        /// [output] The position of the nearest object which was intersected by the input ray
        /// </param>
        /// 
        /// <returns>
        /// The distance to the object intersected by the ray (null if no objects were intersected)
        /// </returns>
        /// 
        public float? Intersects(Ray ray, out Vector3 intersectedPosition)
        {
            return RayIntersection(_root, ray, out intersectedPosition);
        }



        /// 
        /// <summary>
        /// Recursively traverse the octree looking for the closest object intersected by the rage
        /// </summary>
        /// 
        /// <param name="node">
        /// The node to traverse from
        /// </param>
        /// 
        /// <param name="ray">
        /// The ray to check for intersections against
        /// </param>
        /// 
        /// <param name="intersectedPosition">
        /// [output] the postion of the closest object intersected
        /// </param>
        /// 
        /// <returns>
        /// The distance to the nearest object intersected by the input ray (null if there are no intersections)
        /// </returns>
        /// 
        private float? RayIntersection(OctreeNode node, Ray ray, out Vector3 intersectedPosition)
        {
            intersectedPosition = new Vector3();

            BoundingBox translatedBoundingBox = _masterBoundingBoxList[node.BoundingBoxIndex];
            translatedBoundingBox.Max += node.BoundingBoxPosition;
            translatedBoundingBox.Min += node.BoundingBoxPosition;

            if (ray.Intersects(translatedBoundingBox) != null)
            {
                if (node.Children == null)
                {
                    float shortestDistance = float.MaxValue;
                    int shortestDistanceIndex = -1;
                    for (int i = 0; i < node.InternalObjectPositions.Count; ++i)
                    {
                        float? distance = ray.Intersects(node.InternalObjectBoundingBoxes[i]);
                        if (distance.HasValue && distance.Value < shortestDistance)
                        {
                            shortestDistance = distance.Value;
                            shortestDistanceIndex = i;
                        }
                    }
                    if (shortestDistance == float.MaxValue)
                    {
                        return null;
                    }
                    else
                    {
                        intersectedPosition = node.InternalObjectPositions[shortestDistanceIndex];
                        return shortestDistance;
                    }
                }
                else
                {
                    float shortestDistance = float.MaxValue;
                    foreach (OctreeNode childNode in node.Children)
                    {
                        Vector3 position;
                        float? distance = RayIntersection(childNode, ray, out position);
                        if (distance.HasValue && distance.Value < shortestDistance)
                        {
                            shortestDistance = distance.Value;
                            intersectedPosition = position;
                        }
                    }

                    if (shortestDistance == float.MaxValue)
                    {
                        return null;
                    }
                    else
                    {
                        return shortestDistance;
                    }
                }
            }

            return null;
        }
    }
}
