using UnityEngine;

namespace AirPainter.Core
{
    /// <summary>
    /// Predicts future positions based on current velocity.
    /// Helps reduce perceived latency by drawing ahead of the camera feed.
    /// Implemented as a struct to prevent GC allocations per stroke.
    /// </summary>
    public struct MotionPredictor
    {
        private bool hasLastPos;
        private Vector3 lastPos;
        private float lastTime;
        private int predictionFrames;
        
        public MotionPredictor(int framesAhead = 2)
        {
            this.predictionFrames = framesAhead;
            this.hasLastPos = false;
            this.lastPos = Vector3.zero;
            this.lastTime = 0f;
        }

        public Vector3 Predict(Vector3 currentPos, float time)
        {
            if (!hasLastPos)
            {
                hasLastPos = true;
                lastPos = currentPos;
                lastTime = time;
                return currentPos;
            }

            float dt = time - lastTime;
            if (dt <= 0) return currentPos;

            // Velocity in units per second
            Vector3 velocity = (currentPos - lastPos) / dt;

            // Assuming target rendering at 60 FPS (16.6ms per frame)
            float predictionTime = predictionFrames * (1f / 60f);

            Vector3 predictedPos = currentPos + (velocity * predictionTime);

            lastPos = currentPos;
            lastTime = time;

            return predictedPos;
        }

        public void Reset()
        {
            hasLastPos = false;
        }
    }
}
