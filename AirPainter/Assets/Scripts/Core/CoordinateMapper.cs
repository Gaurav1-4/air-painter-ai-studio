using UnityEngine;

namespace AirPainter.Core
{
    /// <summary>
    /// Corrects aspect ratio distortion between the camera feed and screen/canvas.
    /// Ensures 1:1 finger mapping so that physical circles map to digital circles, not ovals.
    /// </summary>
    public static class CoordinateMapper
    {
        public static Vector3 NormalizeToScreen(Vector3 normalizedCameraPos, float cameraAspect, float screenAspect)
        {
            Vector3 corrected = normalizedCameraPos;

            // If camera and screen aspect ratios don't match, the raw coordinates will stretch.
            if (Mathf.Abs(cameraAspect - screenAspect) > 0.01f)
            {
                // Typical case: Camera is 4:3 (1.33) and Screen is 16:9 (1.77).
                // The camera feed is narrower, so X gets stretched.
                // We must scale X down proportionally to maintain aspect ratio,
                // and center it.
                float scaleX = cameraAspect / screenAspect;
                
                corrected.x = (normalizedCameraPos.x - 0.5f) * scaleX + 0.5f;
            }

            // Map to screen pixel coordinates
            return new Vector3(
                corrected.x * Screen.width,
                corrected.y * Screen.height,
                corrected.z // Depth remains untouched
            );
        }

        public static Vector3 ScreenToWorld(Camera mainCamera, Vector3 screenPos, float zDepth)
        {
            screenPos.z = zDepth;
            return mainCamera.ScreenToWorldPoint(screenPos);
        }
    }
}
