using UnityEngine;

namespace AirPainter.Core
{
    /// <summary>
    /// Simulates brush physics (mass, spring, damper) for a realistic, elastic drawing feel.
    /// Connects a "virtual brush" to the "raw finger" using a physical spring.
    /// Implemented as a struct to avoid GC allocations when created per-stroke.
    /// </summary>
    public struct BrushPhysics
    {
        public float mass;
        public float stiffness;
        public float damping;

        private Vector3 position;
        private Vector3 velocity;

        public BrushPhysics(Vector3 startPos, float mass = 1.0f, float stiffness = 150.0f, float damping = 12.0f)
        {
            this.position = startPos;
            this.velocity = Vector3.zero;
            this.mass = mass;
            this.stiffness = stiffness;
            this.damping = damping;
        }

        public void Reset(Vector3 newPos)
        {
            position = newPos;
            velocity = Vector3.zero;
        }

        /// <summary>
        /// Updates the physics simulation and returns the new physical position of the brush.
        /// </summary>
        public Vector3 Update(Vector3 targetPos, float dt)
        {
            if (dt <= 0) return position;

            // Hooke's Law: F = -k * x
            Vector3 displacement = position - targetPos;
            Vector3 springForce = -stiffness * displacement;

            // Damping force: F = -c * v
            Vector3 dampingForce = -damping * velocity;

            // Total force
            Vector3 force = springForce + dampingForce;

            // Acceleration: a = F / m
            Vector3 acceleration = force / mass;

            // Integrate
            velocity += acceleration * dt;
            position += velocity * dt;

            return position;
        }

        public Vector3 GetVelocity()
        {
            return velocity;
        }
        
        public Vector3 GetPosition()
        {
            return position;
        }
    }
}
