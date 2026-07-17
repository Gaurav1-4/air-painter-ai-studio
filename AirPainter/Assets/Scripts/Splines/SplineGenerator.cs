using System.Collections.Generic;
using UnityEngine;

namespace AirPainter.Splines
{
    /// <summary>
    /// Generates smooth curves between raw points using Catmull-Rom splines.
    /// This prevents angular, jagged lines when moving the hand quickly.
    /// </summary>
    public static class SplineGenerator
    {
        /// <summary>
        /// Generates a point on a Catmull-Rom spline between p1 and p2.
        /// </summary>
        public static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // The coefficients of the cubic polynomial
            Vector3 a = 2f * p1;
            Vector3 b = p2 - p0;
            Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
            Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

            // The cubic polynomial: a + b*t + c*t^2 + d*t^3
            Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
            return pos;
        }

        /// <summary>
        /// Interpolates a full array of raw points into a smoothed list of points.
        /// </summary>
        public static List<Vector3> GenerateSmoothPath(List<Vector3> rawPoints, int segmentsPerCurve = 5)
        {
            if (rawPoints == null || rawPoints.Count < 2) return rawPoints;

            List<Vector3> smoothPoints = new List<Vector3>(rawPoints.Count * segmentsPerCurve);

            for (int i = 0; i < rawPoints.Count - 1; i++)
            {
                // Clamp indices to prevent out of bounds when selecting control points
                int p0Index = Mathf.Max(0, i - 1);
                int p1Index = i;
                int p2Index = i + 1;
                int p3Index = Mathf.Min(rawPoints.Count - 1, i + 2);

                Vector3 p0 = rawPoints[p0Index];
                Vector3 p1 = rawPoints[p1Index];
                Vector3 p2 = rawPoints[p2Index];
                Vector3 p3 = rawPoints[p3Index];

                for (int j = 0; j < segmentsPerCurve; j++)
                {
                    float t = j / (float)segmentsPerCurve;
                    smoothPoints.Add(GetCatmullRomPosition(t, p0, p1, p2, p3));
                }
            }

            // Add the very last point
            smoothPoints.Add(rawPoints[rawPoints.Count - 1]);
            return smoothPoints;
        }
    }
}
