using UnityEngine;
using UnityEngine.UI;

namespace AirPainter.UI
{
    /// <summary>
    /// Displays production-level debugging metrics.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        public Text debugText;
        
        private float deltaTime = 0.0f;
        private int trackingLossCount = 0;
        private float simulatedLatency = 12.5f; // Placeholder until SDK integration
        private float trackingConfidence = 0.95f; // Placeholder

        void Update()
        {
            // Calculate FPS
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            
            // Build debug string
            if (debugText != null)
            {
                string text = $"<b>AirPainter Engine Debug</b>\n" +
                              $"FPS: {Mathf.Ceil(fps)}\n" +
                              $"Tracking Confidence: {trackingConfidence:F2}\n" +
                              $"Brush Latency: {simulatedLatency:F1} ms\n" +
                              $"Tracking Lost: {trackingLossCount}\n" +
                              $"Render Thread: Active";
                              
                debugText.text = text;
            }
        }
        
        public void IncrementTrackingLoss()
        {
            trackingLossCount++;
        }
        
        public void UpdateTrackingConfidence(float confidence)
        {
            trackingConfidence = confidence;
        }
    }
}
