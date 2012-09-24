using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GameLib
{
    public class Shadows
    {
        /// 
        /// <summary>
        /// Given an input list of vertices and triangle list style indices into the vertex list, create a new triangle list style list of indices
        /// which only contain the triangles that face the light source
        /// </summary>
        /// 
        /// <param name="objectVertices">
        /// The vertices of the input object
        /// </param>
        /// 
        /// <param name="objectTriangleIndices">
        /// The indices of the input object in a triangle list format (i.e. every three indices specifies a triangle)
        /// </param>
        /// 
        /// <param name="lightDirection">
        /// The direction in which the light is pointing (assuming a distant point light source)
        /// </param>
        /// 
        /// <returns>
        /// A list of indices, again in triangle list format, specifying the triangles that are facing the input light
        /// </returns>
        /// 
        public static void CreateShadowVolumeMesh(List<VertexPositionNormalTexture> objectVertices, List<UInt32> objectTriangleIndices, Vector3 lightDirection, ref List<UInt32> shadowMeshIndices, ref List<Vector3> shadowMeshVertices)
        {
            if (objectTriangleIndices.Count % 3 != 0)
            {
                throw new Exception("The input triangle indices has an incomplete triangle (input length not a multiple of 3)");
            }

            lightDirection.Normalize();

            for (int i = 0; i < objectTriangleIndices.Count - 2; i += 3)
            {
                VertexPositionNormalTexture vertex1 = objectVertices[(int)objectTriangleIndices[i]];
                VertexPositionNormalTexture vertex2 = objectVertices[(int)objectTriangleIndices[i + 1]];
                VertexPositionNormalTexture vertex3 = objectVertices[(int)objectTriangleIndices[i + 2]];

                // We want to compute the normal coming out of the front of this object face.  We will assume that the vertices are in clockwise order to
                // indicate the front side.  From here, we can create vectors along two edges such that the cross product will produce a vector pointing
                // outwards from the front of the triangle face.
                //Vector3 edge1 = vertex3 - vertex1;
                //Vector3 edge2 = vertex2 - vertex1;
                //edge1.Normalize();
                //edge2.Normalize();

                //Vector3 normal = Vector3.Cross(edge1, edge2);
                //normal.Normalize();

                // If the angle between the light direction and the normal is greater than 90 degrees, then the face is pointing towards the light and
                // should be used to project out a shadow volume
                float cosineOfAngle = Vector3.Dot(vertex1.Normal, lightDirection);
                float angle = (float)Math.Acos(cosineOfAngle);

                if (angle > (float)Math.PI / 2.0f)
                {
                    //shadowMeshIndices.Add(objectTriangleIndices[i]);
                    //shadowMeshIndices.Add(objectTriangleIndices[i + 1]);
                    //shadowMeshIndices.Add(objectTriangleIndices[i + 2]);
                    // Now we need to identify each edge of the polygon and extrude out a quadrilateral (as triangles so we can render)
                    {
                        Tuple<Vector3, Vector3>[] edges = new Tuple<Vector3,Vector3>[]
                        {
                            new Tuple<Vector3, Vector3>(vertex2.Position, vertex1.Position),
                            new Tuple<Vector3, Vector3>(vertex3.Position, vertex2.Position),
                            new Tuple<Vector3, Vector3>(vertex1.Position, vertex3.Position)
                        };

                        foreach (Tuple<Vector3, Vector3> edge in edges)
                        {
                            // extrude the first point and make a triangle, extrude the 2nd point and make the other triange
                            // add triangle vertices to vertex list and indices to index list
                            Vector3 firstExtrudedVertex = edge.Item1 + lightDirection * 10000000;

                            uint startingIndex = (uint)shadowMeshVertices.Count;

                            shadowMeshVertices.Add(edge.Item1);
                            shadowMeshVertices.Add(firstExtrudedVertex);
                            shadowMeshVertices.Add(edge.Item2);

                            shadowMeshIndices.Add(startingIndex);
                            shadowMeshIndices.Add(startingIndex + 1);
                            shadowMeshIndices.Add(startingIndex + 2);

                            Vector3 secondExtrudedVertex = edge.Item2 + lightDirection * 10000000;

                            shadowMeshVertices.Add(edge.Item2);
                            shadowMeshVertices.Add(firstExtrudedVertex);
                            shadowMeshVertices.Add(secondExtrudedVertex);

                            shadowMeshIndices.Add(startingIndex + 3);
                            shadowMeshIndices.Add(startingIndex + 4);
                            shadowMeshIndices.Add(startingIndex + 5);
                        }
                    }
                }
            }
        }
    }
}
