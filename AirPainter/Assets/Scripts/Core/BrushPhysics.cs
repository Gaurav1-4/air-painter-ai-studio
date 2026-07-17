using UnityEngine;

namespace AirPainter.Core
{
    /// <summary>
    /// Simulates brush physics (mass, spring, damper) for a realistic, elastic drawing feel.
    /// Connects a "virtual brush" to the "raw finger" using a physical spring.
    /// </summary>
    public class BrushPhysics
    {
        public float mass = 1.0f;
        public float stiffness = 150.0f; // Higher = tighter spring
        public float damping = 12.0f;    // Higher = less bounciness / overshoot

        private Vector3 position;
        private Vector3 velocity;

        public BrushPhysics(Vector3 startPos)
        {
            position = startPos;
            velocity = Vector3.zero;
        }

        public void Reset(Vector3 newPos)
        {
            position = newPos;
            velocity = Vector3.zero;
        }

        /// <summary>
        /// Updates the physics simulation and returns the new physical position of the brush.
        /// </summary>
        /// <param name="targetPos">The position of the finger (filtered)</param>
        /// <param name="dt">Delta time</param>
        public Vector3 Update(Vector3 targetPos, float dt)
        {
            if (dt <= 0) return position;

            // Hooke's Law: F = -k * x
            // x is displacement (position - targetPos)
            Vector3 displacement = position - targetPos;
            Vector3 springForce = -stiffness * displacement;

            // Damping force: F = -c * v
            Vector3 dampingForce = -damping * velocity;

            // Total force
            Vector3 force = springForce + dampingForce;

            // Acceleration: a = F / m
            Vector3 acceleration = force / mass;

            // Integrate to get velocity and position
            velocity += acceleration * dt;
            position += velocity * dt;

            return position;
        }

        public Vector3 GetVelocity()
        {
            return velocity;
        }
    }
}
