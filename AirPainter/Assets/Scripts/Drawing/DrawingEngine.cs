using System.Collections.Generic;
using UnityEngine;
using AirPainter.Rendering;
using AirPainter.Splines;
using AirPainter.Core;

namespace AirPainter.Drawing
{
    /// <summary>
    /// Core engine that manages the active drawing session.
    /// Optimized for zero GC allocation during drawing frames.
    /// </summary>
    public class DrawingEngine : MonoBehaviour
    {
        [Header("Drawing Settings")]
        public float pointMinDistance = 0.01f; // Min distance to register a new point (reduced for physics precision)
        public int splineSegments = 5;         // Segments between points for smoothness
        
        [Header("Physics & Filters")]
        public float filterMinCutoff = 1.0f;
        public float filterBeta = 0.007f;
        public int predictionFrames = 2;
        
        private Stroke activeStroke;
        private StrokeRenderer activeRenderer;
        
        private OneEuroFilter positionFilter;
        
        private MotionPredictor predictor;
        private BrushPhysics physicsEngine;
        
        private float currentPressure;
        
        // References (injected by DrawingManager)
        private Material currentMaterial;
        private Color currentColor;
        private float currentBrushSize;

        /// <summary>
        /// Begins a new stroke at the given position.
        /// </summary>
        public void StartStroke(Vector3 startPosition, string brushId, Color color, float size, StrokeRenderer renderer, Material mat)
        {
            currentColor = color;
            currentBrushSize = size;
            currentMaterial = mat;
            
            activeStroke = new Stroke(brushId, color, size);
            activeRenderer = renderer;
            activeRenderer.Initialize(activeStroke, currentMaterial);

            // Initialize Structs
            positionFilter = new OneEuroFilter(filterMinCutoff, filterBeta);
            
            predictor = new MotionPredictor(predictionFrames);
            physicsEngine = new BrushPhysics(startPosition);

            currentPressure = 0.5f; // Initial tap pressure

            AddProcessedPoint(startPosition, currentPressure, 0f);
        }

        /// <summary>
        /// Updates the current stroke with a new raw position (e.g., from index fingertip).
        /// </summary>
        public void UpdateStroke(Vector3 currentPosition, float deltaTime)
        {
            if (activeStroke == null || activeRenderer == null) return;

            float time = Time.time;

            // 1. 1€ Filtering (Removes jitter and micro-shakes)
            Vector3 filteredPosition = positionFilter.Filter(currentPosition, time);

            // 2. Motion Prediction (Reduces latency)
            Vector3 predictedPosition = predictor.Predict(filteredPosition, time);

            // 3. Brush Physics (Spring-Damper for realistic inertia and elasticity)
            Vector3 physicalPosition = physicsEngine.Update(predictedPosition, deltaTime);
            
            // 4. Spatial Filtering on the physical brush position
            Vector3 lastPos = activeStroke.Points.Count > 0 ? activeStroke.Points[activeStroke.Points.Count - 1].Position : physicalPosition;
            float minDistanceSq = pointMinDistance * pointMinDistance;
            
            // Optimized distance check to avoid square root
            if ((physicalPosition - lastPos).sqrMagnitude < minDistanceSq)
                return;

            // 5. Velocity and Pressure Calculation
            float velocityMagnitude = physicsEngine.GetVelocity().magnitude;
            
            // Pressure Simulation
            float targetPressure = Mathf.Clamp01(1f - (velocityMagnitude / 10.0f));
            currentPressure = Mathf.Lerp(currentPressure, targetPressure, 10f * deltaTime); 

            // 6. Add Point to Stroke Data
            AddProcessedPoint(physicalPosition, currentPressure, velocityMagnitude);

            // 7. Update Renderer Mesh
            activeRenderer.UpdateMesh();
        }

        /// <summary>
        /// Finalizes the current stroke.
        /// </summary>
        public Stroke EndStroke()
        {
            if (activeStroke == null) return null;

            Stroke finishedStroke = activeStroke;
            
            activeStroke = null;
            activeRenderer = null;
            
            return finishedStroke;
        }

        private void AddProcessedPoint(Vector3 position, float pressure, float velocity)
        {
            // If we have enough points, we generate spline points between the last segment
            if (activeStroke.Points.Count >= 3)
            {
                int count = activeStroke.Points.Count;
                Vector3 p0 = activeStroke.Points[count - 3].Position;
                Vector3 p1 = activeStroke.Points[count - 2].Position;
                Vector3 p2 = activeStroke.Points[count - 1].Position;
                Vector3 p3 = position;

                for (int i = 1; i < splineSegments; i++)
                {
                    float t = i / (float)splineSegments;
                    Vector3 splinePos = SplineGenerator.GetCatmullRomPosition(t, p0, p1, p2, p3);
                    
                    // Interpolate attributes
                    float interpPressure = Mathf.Lerp(activeStroke.Points[count - 2].Pressure, pressure, t);
                    
                    activeStroke.AddPoint(new StrokePoint(splinePos, interpPressure, velocity, currentColor, currentBrushSize));
                }
            }

            // Always add the actual hard point
            activeStroke.AddPoint(new StrokePoint(position, pressure, velocity, currentColor, currentBrushSize));
        }
    }
}
