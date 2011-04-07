using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace LevelEditor
{
    class OctreeNode
    {
        public OctreeNode(BoundingBox nodeBoundingBox)
        {
            NodeBoundingBox = nodeBoundingBox;
        }

        public BoundingBox NodeBoundingBox;
        public List<Vector3> InternalObjectPositions = new List<Vector3>();
        public List<BoundingBox> InternalObjectBoundingBoxes = new List<BoundingBox>();
        public OctreeNode[] Children = null;
    }

    class Octree
    {
        private OctreeNode root;
        private static readonly int _maxItemsPerNode = 5000;

        public Octree(BoundingBox topLevelBoundingBox)
        {
            root = new OctreeNode(topLevelBoundingBox);
        }

        public void AddItems(Vector3[] positions, BoundingBox[] boundingBoxes)
        {
            for (int i = 0; i < positions.Length; ++i)
            {
                FindContainingNode(root, positions[i], boundingBoxes[i]);
            }
        }

        private void FindContainingNode(OctreeNode node, Vector3 position, BoundingBox boundingBox)
        {
            if (node.NodeBoundingBox.Intersects(boundingBox))
            {
                if (node.Children == null)
                {
                    if (node.InternalObjectPositions.Count + 1 >= _maxItemsPerNode)
                    {
                        BreakDownNode(node);
                        FindContainingNode(node, position, boundingBox);
                    }
                    else
                    {
                        node.InternalObjectPositions.Add(position);
                        node.InternalObjectBoundingBoxes.Add(boundingBox);
                    }
                }
                else
                {
                    foreach (OctreeNode childNode in node.Children)
                    {
                        FindContainingNode(childNode, position, boundingBox);
                    }
                }
            }
        }

        private void BreakDownNode(OctreeNode node)
        {
            // We shouldn't ever call this on a node that has already been broken down but if for some reason
            // we do we should fail in a noticable manner so we can debug the problem rather than tracking
            // down the silent error based on some unpredicted side effect.
            if (node.Children != null)
            {
                throw new ArgumentException("Called Octree::BreakDownNode on a node which has already been broken down");
            }

            node.Children = new OctreeNode[8];

            Vector3[] nodeCorners = node.NodeBoundingBox.GetCorners();
            Vector3 nodeCenter = (node.NodeBoundingBox.Max - node.NodeBoundingBox.Min) / 2.0f;

            Vector3[] points = new Vector3[2];

            for (int i = 0; i < node.Children.Length; ++i)
            {
                // Center + 1 Corner define opposite corners of one of our 8 sub-bounding boxes
                points[0] = nodeCorners[i];
                points[1] = nodeCenter;

                node.Children[i] = new OctreeNode(BoundingBox.CreateFromPoints(points));
            }

            for (int i = 0; i < node.InternalObjectPositions.Count; ++i)
            {
                FindContainingNode(node, node.InternalObjectPositions[i], node.InternalObjectBoundingBoxes[i]);
            }

            node.InternalObjectBoundingBoxes.Clear();
            node.InternalObjectPositions.Clear();
        }

        public float? Intersects(Ray ray, out Vector3 intersectedPosition)
        {
            return RayIntersection(root, ray, out intersectedPosition);
        }

        private float? RayIntersection(OctreeNode node, Ray ray, out Vector3 intersectedPosition)
        {
            intersectedPosition = new Vector3();

            if(ray.Intersects(node.NodeBoundingBox) != null)
            {
                if (node.Children == null)
                {
                    float shortestDistance = float.MaxValue;
                    int shortestDistanceIndex = -1;
                    for (int i = 0; i < node.InternalObjectPositions.Count; ++i )
                    {
                        float? distance = ray.Intersects(node.InternalObjectBoundingBoxes[i]);
                        if (distance.HasValue && distance.Value < shortestDistance)
                        {
                            shortestDistance = distance.Value;
                            shortestDistanceIndex = i;
                        }
                    }
                    if(shortestDistance == float.MaxValue)
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
                    foreach(OctreeNode childNode in node.Children)
                    {
                        Vector3 position;
                        float? distance = RayIntersection(childNode, ray, out position);
                        if(distance.HasValue && distance.Value < shortestDistance)
                        {
                            shortestDistance = distance.Value;
                            intersectedPosition = position;
                        }
                    }

                    if(shortestDistance == float.MaxValue)
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
