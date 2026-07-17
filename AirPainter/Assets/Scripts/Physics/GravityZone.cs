using UnityEngine;

namespace AirPainter.Physics
{
    public enum GravityType
    {
        Directional, // Constant gravity in a specific direction (like normal gravity)
        Spherical,   // Gravity pulls towards the center of this object (like a planet)
        Cylindrical  // Gravity pulls towards the central axis of this object
    }

    /// <summary>
    /// Defines an area of space with specific gravity rules.
    /// Can be attached to walls, planets, or floating objects in the AR space.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GravityZone : MonoBehaviour
    {
        [Header("Gravity Settings")]
        public GravityType gravityType = GravityType.Directional;
        public float gravityStrength = 9.81f;
        
        [Tooltip("Custom direction for Directional gravity (Local Space)")]
        public Vector3 localGravityDirection = Vector3.down;

        private Collider zoneCollider;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true; // Gravity zones should not cause physical collisions
        }

        private void OnEnable()
        {
            if (GravityManager.Instance != null)
                GravityManager.Instance.RegisterZone(this);
        }

        private void OnDisable()
        {
            if (GravityManager.Instance != null)
                GravityManager.Instance.UnregisterZone(this);
        }

        public bool IsInsideZone(Vector3 position)
        {
            // Optimization: Use Bounds check before complex raycasting/ClosestPoint
            if (!zoneCollider.bounds.Contains(position)) return false;

            // Accurate check for complex colliders
            return zoneCollider.ClosestPoint(position) == position;
        }

        public Vector3 GetGravityVector(Vector3 position)
        {
            switch (gravityType)
            {
                case GravityType.Directional:
                    // Convert local direction to world direction
                    return transform.TransformDirection(localGravityDirection.normalized) * gravityStrength;

                case GravityType.Spherical:
                    // Pull towards the center of the transform
                    Vector3 directionToCenter = (transform.position - position).normalized;
                    return directionToCenter * gravityStrength;

                case GravityType.Cylindrical:
                    // Pull towards the local Y axis of the transform
                    Vector3 localPos = transform.InverseTransformPoint(position);
                    localPos.y = 0; // Ignore Y height
                    Vector3 axisPoint = transform.TransformPoint(localPos);
                    Vector3 directionToAxis = (axisPoint - position).normalized;
                    return directionToAxis * gravityStrength;

                default:
                    return Vector3.down * gravityStrength;
            }
        }
    }
}
