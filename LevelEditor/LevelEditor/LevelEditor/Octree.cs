using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace LevelEditor
{
    class OctreeNode
    {
        public OctreeNode(int boundingBoxIndex, Vector3 boundingBoxPosition)
        {
            BoundingBoxIndex = boundingBoxIndex;
            BoundingBoxPosition = boundingBoxPosition;
        }

        public int BoundingBoxIndex;
        public Vector3 BoundingBoxPosition;
        public List<Vector3> InternalObjectPositions = new List<Vector3>();
        public List<BoundingBox> InternalObjectBoundingBoxes = new List<BoundingBox>();
        public OctreeNode[] Children = null;
    }

    class Octree
    {
        private OctreeNode root;
        private List<BoundingBox> _boundingBoxes = new List<BoundingBox>();
        private const int _maxItemsPerNode = 500;

        public Octree(BoundingBox topLevelBoundingBox, Vector3 topLevelBoundingBoxPosition)
        {
            _boundingBoxes.Add(topLevelBoundingBox);
            root = new OctreeNode(_boundingBoxes.Count - 1, topLevelBoundingBoxPosition);
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
            BoundingBox translatedBoundingBox = _boundingBoxes[node.BoundingBoxIndex];
            translatedBoundingBox.Max += node.BoundingBoxPosition;
            translatedBoundingBox.Min += node.BoundingBoxPosition;

            if (translatedBoundingBox.Intersects(boundingBox))
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

            BoundingBox nodeBoundingBox = _boundingBoxes[node.BoundingBoxIndex];
            nodeBoundingBox.Max += node.BoundingBoxPosition;
            nodeBoundingBox.Min += node.BoundingBoxPosition;

            Vector3[] nodeCorners = nodeBoundingBox.GetCorners();
            Vector3 nodeCenter = (nodeBoundingBox.Max + nodeBoundingBox.Min) / 2.0f;

            Vector3[] points = new Vector3[2];

            for (int i = 0; i < node.Children.Length; ++i)
            {
                Vector3 subRegionCenter = (nodeCorners[i] + nodeCenter) / 2.0f;
                //if (subRegionCenter.X < 0 || subRegionCenter.Y < 0 || subRegionCenter.Z < 0)
                //{
                //    // TODO: None of this shit is right
                //    subRegionCenter = nodeCenter + nodeCorners[i] / 2.0f;
                //}

                // shrink our bounding box by a factor of 2
                points[0] = _boundingBoxes[node.BoundingBoxIndex].Max / 2.0f;
                points[1] = _boundingBoxes[node.BoundingBoxIndex].Min / 2.0f;

                BoundingBox newBoundingBox = BoundingBox.CreateFromPoints(points);

                if (_boundingBoxes.Contains(newBoundingBox))
                {
                    int index = _boundingBoxes.FindIndex(x => x == newBoundingBox);
                    node.Children[i] = new OctreeNode(index, subRegionCenter);
                }
                else
                {
                    _boundingBoxes.Add(newBoundingBox);
                    node.Children[i] = new OctreeNode(_boundingBoxes.Count - 1, subRegionCenter);
                }
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

            BoundingBox translatedBoundingBox = _boundingBoxes[node.BoundingBoxIndex];
            translatedBoundingBox.Max += node.BoundingBoxPosition;
            translatedBoundingBox.Min += node.BoundingBoxPosition;

            if (ray.Intersects(translatedBoundingBox) != null)
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
