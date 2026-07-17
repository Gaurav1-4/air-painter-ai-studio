using System.Collections.Generic;
using UnityEngine;

namespace AirPainter.Physics
{
    /// <summary>
    /// Global manager for the anti-gravity system.
    /// Tracks all active gravity zones and determines the dominant gravity vector for any position.
    /// </summary>
    public class GravityManager : MonoBehaviour
    {
        public static GravityManager Instance { get; private set; }

        private List<GravityZone> activeZones = new List<GravityZone>();

        [Header("Global Settings")]
        public Vector3 defaultGravity = new Vector3(0, -9.81f, 0);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterZone(GravityZone zone)
        {
            if (!activeZones.Contains(zone))
            {
                activeZones.Add(zone);
            }
        }

        public void UnregisterZone(GravityZone zone)
        {
            if (activeZones.Contains(zone))
            {
                activeZones.Remove(zone);
            }
        }

        /// <summary>
        /// Gets the combined gravity vector at a specific world position.
        /// Handles overlapping gravity zones smoothly.
        /// </summary>
        public Vector3 GetGravityAtPosition(Vector3 position)
        {
            Vector3 totalGravity = Vector3.zero;
            int zoneCount = 0;

            foreach (var zone in activeZones)
            {
                if (zone.IsInsideZone(position))
                {
                    totalGravity += zone.GetGravityVector(position);
                    zoneCount++;
                }
            }

            if (zoneCount > 0)
            {
                // Average the gravity if inside multiple intersecting zones
                return totalGravity / zoneCount;
            }

            // Fallback to global gravity
            return defaultGravity;
        }
    }
}
