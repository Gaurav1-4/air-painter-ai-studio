using System.Collections.Generic;
using UnityEngine;
using AirPainter.Drawing;

namespace AirPainter.MeshGeneration
{
    /// <summary>
    /// Converts a List of StrokePoints into a renderable 3D Mesh.
    /// Uses static, pre-allocated buffers to prevent Garbage Collection allocations during active drawing.
    /// </summary>
    public static class MeshBuilder
    {
        // Reusable static buffers to avoid GC allocations per frame
        private static readonly List<Vector3> vertices = new List<Vector3>(8192);
        private static readonly List<Vector2> uvs = new List<Vector2>(8192);
        private static readonly List<Color> colors = new List<Color>(8192);
        private static readonly List<int> triangles = new List<int>(24576);

        /// <summary>
        /// Generates or updates the stroke mesh using the provided Stroke points and Mesh.
        /// </summary>
        public static void UpdateStrokeMesh(List<StrokePoint> points, Mesh mesh)
        {
            if (points == null || points.Count < 2 || mesh == null) return;

            vertices.Clear();
            uvs.Clear();
            colors.Clear();
            triangles.Clear();

            float totalLength = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    totalLength += Vector3.Distance(points[i].Position, points[i - 1].Position);
                }

                // Calculate the forward direction
                Vector3 forward;
                if (i == 0)
                    forward = (points[1].Position - points[0].Position).normalized;
                else if (i == points.Count - 1)
                    forward = (points[i].Position - points[i - 1].Position).normalized;
                else
                    forward = (points[i + 1].Position - points[i - 1].Position).normalized;

                // Handle zero vector edge case when points are too close
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

                // Calculate the perpendicular vector for thickness (assuming drawing primarily on camera facing plane)
                // For a more robust 3D brush, we would need camera forward vector, but this matches previous logic
                Vector3 right = new Vector3(forward.y, -forward.x, 0).normalized;
                if (right.sqrMagnitude < 0.001f) right = Vector3.right;

                // Apply pressure to base width
                float halfWidth = (points[i].Width * points[i].Pressure) * 0.5f;

                // Taper the start and end of the stroke slightly for a natural look
                float taper = 1f;
                if (i < 3) taper = (i + 1) / 4f; // Taper in
                if (i > points.Count - 4) taper = (points.Count - i) / 4f; // Taper out
                halfWidth *= taper;

                vertices.Add(points[i].Position + (right * halfWidth));
                vertices.Add(points[i].Position - (right * halfWidth));

                // UV calculation mapping texture along the stroke
                uvs.Add(new Vector2(totalLength, 1f));
                uvs.Add(new Vector2(totalLength, 0f));

                // Multiply vertex color by pressure to allow shader to read it as opacity/strength
                Color pointColor = points[i].PointColor;
                pointColor.a *= points[i].Pressure;
                
                colors.Add(pointColor);
                colors.Add(pointColor);

                // Generate triangles
                if (i < points.Count - 1)
                {
                    int vIndex = i * 2;
                    
                    triangles.Add(vIndex);
                    triangles.Add(vIndex + 2);
                    triangles.Add(vIndex + 1);

                    triangles.Add(vIndex + 1);
                    triangles.Add(vIndex + 2);
                    triangles.Add(vIndex + 3);
                }
            }

            // Apply to mesh
            mesh.Clear(false); // keep vertex layout to avoid allocation
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, false);
            
            // RecalculateBounds is cheaper than RecalculateNormals
            mesh.RecalculateBounds();
        }
    }
}
