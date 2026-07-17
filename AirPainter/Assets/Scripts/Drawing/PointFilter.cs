using System.Collections.Generic;
using UnityEngine;

namespace AirPainter.Drawing
{
    /// <summary>
    /// Handles filtering of raw input points to reduce noise and optimize point density.
    /// Uses spatial and temporal filtering.
    /// </summary>
    public static class PointFilter
    {
        /// <summary>
        /// Filters a new point based on the previous point and a minimum distance threshold.
        /// Helps in reducing the number of points processed when the hand moves very slowly.
        /// </summary>
        public static bool ShouldKeepPoint(Vector3 newPoint, Vector3 lastPoint, float minDistanceSquared)
        {
            float sqrDist = (newPoint - lastPoint).sqrMagnitude;
            return sqrDist >= minDistanceSquared;
        }

        /// <summary>
        /// Applies a simple low-pass filter (exponential moving average) to smooth out raw input points.
        /// </summary>
        public static Vector3 SmoothPoint(Vector3 rawPoint, Vector3 previousFiltered, float alpha = 0.5f)
        {
            return Vector3.Lerp(previousFiltered, rawPoint, alpha);
        }

        /// <summary>
        /// Filters an entire array of points using Douglas-Peucker algorithm for line simplification.
        /// Useful for optimizing a stroke after it's finished drawing.
        /// </summary>
        public static List<Vector3> SimplifyPath(List<Vector3> points, float tolerance)
        {
            if (points == null || points.Count < 3)
                return points == null ? new List<Vector3>() : new List<Vector3>(points);

            int firstPoint = 0;
            int lastPoint = points.Count - 1;
            List<int> pointIndexsToKeep = new List<int>();

            pointIndexsToKeep.Add(firstPoint);
            pointIndexsToKeep.Add(lastPoint);

            while (points[firstPoint].Equals(points[lastPoint]))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(points, firstPoint, lastPoint, tolerance, ref pointIndexsToKeep);

            pointIndexsToKeep.Sort();
            List<Vector3> simplifiedPoints = new List<Vector3>();

            foreach (int index in pointIndexsToKeep)
            {
                simplifiedPoints.Add(points[index]);
            }

            return simplifiedPoints;
        }

        private static void DouglasPeuckerReduction(List<Vector3> points, int firstPoint, int lastPoint, float tolerance, ref List<int> pointIndexsToKeep)
        {
            float maxDistance = 0;
            int indexFarthest = 0;

            for (int index = firstPoint; index < lastPoint; index++)
            {
                float distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0)
            {
                pointIndexsToKeep.Add(indexFarthest);
                DouglasPeuckerReduction(points, firstPoint, indexFarthest, tolerance, ref pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexFarthest, lastPoint, tolerance, ref pointIndexsToKeep);
            }
        }

        private static float PerpendicularDistance(Vector3 Point1, Vector3 Point2, Vector3 Point)
        {
            float area = Mathf.Abs(.5f * (Point1.x * Point2.y + Point2.x * Point.y + Point.x * Point1.y - Point2.x * Point1.y - Point.x * Point2.y - Point1.x * Point.y));
            float bottom = Mathf.Sqrt(Mathf.Pow(Point1.x - Point2.x, 2) + Mathf.Pow(Point1.y - Point2.y, 2));
            return area / bottom * 2;
        }
    }
}
