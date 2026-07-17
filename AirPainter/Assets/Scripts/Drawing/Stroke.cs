using System;
using System.Collections.Generic;
using UnityEngine;

namespace AirPainter.Drawing
{
    /// <summary>
    /// Represents a single point in a brush stroke.
    /// </summary>
    [Serializable]
    public struct StrokePoint
    {
        public Vector3 Position;
        public float Pressure;
        public float Velocity;
        public Color PointColor;
        public float Width;

        public StrokePoint(Vector3 position, float pressure, float velocity, Color pointColor, float width)
        {
            Position = position;
            Pressure = pressure;
            Velocity = velocity;
            PointColor = pointColor;
            Width = width;
        }
    }

    /// <summary>
    /// Data container for a complete brush stroke. 
    /// Does not handle rendering, only the mathematical representation of the stroke.
    /// </summary>
    [Serializable]
    public class Stroke
    {
        public string Id { get; private set; }
        public List<StrokePoint> Points { get; private set; }
        
        // Settings active when stroke was drawn
        public string BrushId { get; private set; }
        public Color PrimaryColor { get; private set; }
        public float BaseSize { get; private set; }
        
        // Bounding box for optimization and culling
        public Bounds Bounds { get; private set; }

        public Stroke(string brushId, Color color, float baseSize)
        {
            Id = Guid.NewGuid().ToString();
            Points = new List<StrokePoint>(1024); // Pre-allocate to reduce GC
            BrushId = brushId;
            PrimaryColor = color;
            BaseSize = baseSize;
        }

        public void AddPoint(StrokePoint point)
        {
            Points.Add(point);
            UpdateBounds(point.Position);
        }

        private void UpdateBounds(Vector3 newPos)
        {
            if (Points.Count == 1)
            {
                Bounds = new Bounds(newPos, Vector3.zero);
            }
            else
            {
                Bounds bounds = Bounds;
                bounds.Encapsulate(newPos);
                Bounds = bounds;
            }
        }
        
        public void Clear()
        {
            Points.Clear();
            Bounds = new Bounds(Vector3.zero, Vector3.zero);
        }
    }
}
