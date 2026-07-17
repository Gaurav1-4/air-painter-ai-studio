using UnityEngine;

namespace AirPainter.Physics
{
    /// <summary>
    /// Attaches to any object (Player, Camera, or dynamic Strokes) that needs to respond to the custom Anti-Gravity system.
    /// Handles smooth rotation alignment and gravity application without jitter.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class AntiGravityController : MonoBehaviour
    {
        [Header("Alignment Settings")]
        public bool alignToGravity = true;
        public float rotationSpeed = 10f;
        
        [Header("Physics Override")]
        public bool disableNativeGravity = true;

        private Rigidbody rb;
        private Vector3 currentGravity;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (disableNativeGravity)
            {
                rb.useGravity = false;
            }
        }

        private void FixedUpdate()
        {
            if (GravityManager.Instance == null) return;

            // 1. Get Gravity from the global manager for our current position
            currentGravity = GravityManager.Instance.GetGravityAtPosition(transform.position);

            // 2. Apply Custom Gravity Force
            rb.AddForce(currentGravity, ForceMode.Acceleration);

            // 3. Smoothly align object so that its 'down' matches the gravity pull
            if (alignToGravity && currentGravity.sqrMagnitude > 0.01f)
            {
                AlignToGravity(currentGravity);
            }
        }

        private void AlignToGravity(Vector3 gravityDir)
        {
            // The direction 'down' is the direction of the gravity vector
            Vector3 targetDown = gravityDir.normalized;
            Vector3 currentUp = transform.up;
            Vector3 targetUp = -targetDown;

            // Calculate the rotation needed to get from currentUp to targetUp
            Quaternion targetRotation = Quaternion.FromToRotation(currentUp, targetUp) * transform.rotation;
            
            // Smoothly interpolate the Rigidbody's rotation to avoid jitter
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}
