using System.Collections.Generic;
using UnityEngine;
using AirPainter.Drawing;

namespace AirPainter.MeshGeneration
{
    /// <summary>
    /// Converts a List of StrokePoints into a renderable 3D Mesh.
    /// Supports variable thickness based on pressure/velocity and anti-aliasing via UV generation.
    /// </summary>
    public static class MeshBuilder
    {
        public static Mesh GenerateStrokeMesh(List<StrokePoint> points)
        {
            if (points == null || points.Count < 2) return null;

            int vertexCount = points.Count * 2;
            int triangleCount = (points.Count - 1) * 6;

            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            Color[] colors = new Color[vertexCount];
            int[] triangles = new int[triangleCount];

            float totalLength = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    totalLength += Vector3.Distance(points[i].Position, points[i - 1].Position);
                }

                // Calculate the forward direction
                Vector3 forward = Vector3.zero;
                if (i == 0)
                    forward = (points[1].Position - points[0].Position).normalized;
                else if (i == points.Count - 1)
                    forward = (points[i].Position - points[i - 1].Position).normalized;
                else
                    forward = (points[i + 1].Position - points[i - 1].Position).normalized;

                // Calculate the perpendicular vector for thickness (assuming drawing on XY plane)
                Vector3 right = new Vector3(forward.y, -forward.x, 0).normalized;

                // Apply pressure to base width
                float halfWidth = (points[i].Width * points[i].Pressure) / 2f;

                // Taper the start and end of the stroke slightly for a natural look
                float taper = 1f;
                if (i < 3) taper = (i + 1) / 4f; // Taper in
                if (i > points.Count - 4) taper = (points.Count - i) / 4f; // Taper out
                halfWidth *= taper;

                int vIndex = i * 2;

                vertices[vIndex] = points[i].Position + (right * halfWidth);
                vertices[vIndex + 1] = points[i].Position - (right * halfWidth);

                // UV calculation mapping texture along the stroke
                float u = totalLength; 
                uvs[vIndex] = new Vector2(u, 1f);
                uvs[vIndex + 1] = new Vector2(u, 0f);

                // Multiply vertex color by pressure to allow shader to read it as opacity/strength
                Color pointColor = points[i].PointColor;
                pointColor.a *= points[i].Pressure;
                
                colors[vIndex] = pointColor;
                colors[vIndex + 1] = pointColor;

                // Generate triangles
                if (i < points.Count - 1)
                {
                    int tIndex = i * 6;
                    
                    triangles[tIndex] = vIndex;
                    triangles[tIndex + 1] = vIndex + 2;
                    triangles[tIndex + 2] = vIndex + 1;

                    triangles[tIndex + 3] = vIndex + 1;
                    triangles[tIndex + 4] = vIndex + 2;
                    triangles[tIndex + 5] = vIndex + 3;
                }
            }

            Mesh mesh = new Mesh
            {
                vertices = vertices,
                uv = uvs,
                colors = colors,
                triangles = triangles
            };

            // Optimization: RecalculateBounds is cheaper than RecalculateNormals
            // Since we use Unlit shaders for drawing, we don't need normals.
            mesh.RecalculateBounds();
            
            return mesh;
        }
    }
}
