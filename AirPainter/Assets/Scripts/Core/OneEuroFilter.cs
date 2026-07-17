using UnityEngine;

namespace AirPainter.Core
{
    /// <summary>
    /// Adaptive low-pass filter (1€ Filter) for noisy signals.
    /// Eliminates jitter at low speeds and reduces lag at high speeds.
    /// </summary>
    public class OneEuroFilter
    {
        private float minCutoff;
        private float beta;
        private float dCutoff;
        private float deadzone; // Sub-pixel threshold to ignore jitter

        private Vector3? xPrev;
        private Vector3 dxPrev;
        private float tPrev;
        private Vector3? lastOutput;

        public OneEuroFilter(float minCutoff = 1.0f, float beta = 0.0f, float dCutoff = 1.0f, float deadzone = 0.002f)
        {
            this.minCutoff = minCutoff;
            this.beta = beta;
            this.dCutoff = dCutoff;
            this.deadzone = deadzone;
        }

        public Vector3 Filter(Vector3 x, float t)
        {
            if (xPrev == null)
            {
                xPrev = x;
                dxPrev = Vector3.zero;
                tPrev = t;
                return x;
            }

            float te = t - tPrev;

            // The data might come in instantly or out of order
            if (te <= 0f) return xPrev.Value;

            // Calculate velocity
            Vector3 dx = (x - xPrev.Value) / te;
            
            // Filter velocity
            float ad = SmoothingFactor(te, dCutoff);
            Vector3 dxFiltered = Vector3.Lerp(dxPrev, dx, ad);

            // Calculate adaptive cutoff based on velocity magnitude
            float cutoff = minCutoff + beta * dxFiltered.magnitude;

            // Filter position
            float ae = SmoothingFactor(te, cutoff);
            Vector3 xFiltered = Vector3.Lerp(xPrev.Value, x, ae);
            
            // Apply Dead-zone (Anti-jitter)
            if (lastOutput == null) 
            {
                lastOutput = xFiltered;
            }
            else
            {
                float dist = Vector3.Distance(lastOutput.Value, xFiltered);
                if (dist > deadzone)
                {
                    // Interpolate slightly out of deadzone to prevent snapping
                    lastOutput = Vector3.Lerp(lastOutput.Value, xFiltered, 0.8f);
                }
            }

            // Update states
            xPrev = xFiltered;
            dxPrev = dxFiltered;
            tPrev = t;

            return lastOutput.Value;
        }

        public void Reset()
        {
            xPrev = null;
            dxPrev = Vector3.zero;
            lastOutput = null;
        }

        private float SmoothingFactor(float te, float cutoff)
        {
            float r = 2f * Mathf.PI * cutoff * te;
            return r / (r + 1f);
        }
    }
}
