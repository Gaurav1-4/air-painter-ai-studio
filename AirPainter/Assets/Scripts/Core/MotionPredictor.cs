using UnityEngine;

namespace AirPainter.Core
{
    /// <summary>
    /// Predicts future positions based on current velocity.
    /// Helps reduce perceived latency by drawing ahead of the camera feed.
    /// </summary>
    public class MotionPredictor
    {
        private Vector3? lastPos;
        private float lastTime;
        private int predictionFrames;
        
        public MotionPredictor(int framesAhead = 2)
        {
            this.predictionFrames = framesAhead;
        }

        public Vector3 Predict(Vector3 currentPos, float time)
        {
            if (lastPos == null)
            {
                lastPos = currentPos;
                lastTime = time;
                return currentPos;
            }

            float dt = time - lastTime;
            if (dt <= 0) return currentPos;

            // Velocity in units per second
            Vector3 velocity = (currentPos - lastPos.Value) / dt;

            // Assuming target rendering at 60 FPS (16.6ms per frame)
            float predictionTime = predictionFrames * (1f / 60f);

            Vector3 predictedPos = currentPos + (velocity * predictionTime);

            lastPos = currentPos;
            lastTime = time;

            return predictedPos;
        }

        public void Reset()
        {
            lastPos = null;
        }
    }
}
