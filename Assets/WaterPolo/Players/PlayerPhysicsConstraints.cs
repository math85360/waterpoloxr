using UnityEngine;

namespace WaterPolo.Players
{
    /// <summary>
    /// Optional component to limit player physics when not using Rigidbody constraints.
    /// Allows vertical movement and rotation while preventing unrealistic behavior.
    /// Use this if you DON'T freeze Position Y or Rotation X/Z in Rigidbody.
    /// </summary>
    public class PlayerPhysicsConstraints : MonoBehaviour
    {
        [Header("Vertical Constraints")]
        [SerializeField] private float _waterSurfaceY = 0f;
        [SerializeField] private float _maxElevation = 0.8f; // Max height above water
        [SerializeField] private float _minDepth = -0.5f;    // Max depth below water
        [SerializeField] private bool _enableVerticalConstraint = true;

        [Header("Rotation Constraints")]
        [SerializeField] private float _maxTiltAngle = 30f;  // Max lean angle in degrees
        [SerializeField] private bool _enableRotationConstraint = true;

        [Header("Stabilization")]
        [SerializeField] private float _uprightingForce = 50f; // Force to return upright
        [SerializeField] private float _buoyancyForce = 10f;   // Upward force at water level

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogError($"PlayerPhysicsConstraints on {gameObject.name} requires a Rigidbody!");
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null) return;

            // Apply vertical constraints
            if (_enableVerticalConstraint)
            {
                ConstrainVerticalPosition();
                ApplyBuoyancy();
            }

            // Apply rotation constraints
            if (_enableRotationConstraint)
            {
                ConstrainRotation();
            }
        }

        /// <summary>
        /// Limit vertical position within allowed range.
        /// </summary>
        private void ConstrainVerticalPosition()
        {
            Vector3 pos = _rigidbody.position;
            float minY = _waterSurfaceY + _minDepth;
            float maxY = _waterSurfaceY + _maxElevation;

            if (pos.y < minY)
            {
                pos.y = minY;
                _rigidbody.position = pos;

                // Stop downward velocity
                Vector3 vel = _rigidbody.linearVelocity;
                vel.y = Mathf.Max(vel.y, 0);
                _rigidbody.linearVelocity = vel;
            }
            else if (pos.y > maxY)
            {
                pos.y = maxY;
                _rigidbody.position = pos;

                // Stop upward velocity
                Vector3 vel = _rigidbody.linearVelocity;
                vel.y = Mathf.Min(vel.y, 0);
                _rigidbody.linearVelocity = vel;
            }
        }

        /// <summary>
        /// Apply buoyancy force to keep player at water surface.
        /// </summary>
        private void ApplyBuoyancy()
        {
            float depth = _rigidbody.position.y - _waterSurfaceY;

            // If below surface, apply upward force
            if (depth < 0)
            {
                float buoyancy = -depth * _buoyancyForce;
                _rigidbody.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
            }
            // If above surface, apply slight downward force (gravity substitute)
            else if (depth > 0.1f)
            {
                _rigidbody.AddForce(Vector3.down * 5f, ForceMode.Acceleration);
            }
        }

        /// <summary>
        /// Limit rotation to prevent flipping over.
        /// </summary>
        private void ConstrainRotation()
        {
            Vector3 euler = _rigidbody.rotation.eulerAngles;

            // Normalize angles to -180 to 180
            float angleX = NormalizeAngle(euler.x);
            float angleZ = NormalizeAngle(euler.z);

            // Clamp tilt angles
            bool needsCorrection = false;
            if (Mathf.Abs(angleX) > _maxTiltAngle)
            {
                angleX = Mathf.Clamp(angleX, -_maxTiltAngle, _maxTiltAngle);
                needsCorrection = true;
            }

            if (Mathf.Abs(angleZ) > _maxTiltAngle)
            {
                angleZ = Mathf.Clamp(angleZ, -_maxTiltAngle, _maxTiltAngle);
                needsCorrection = true;
            }

            if (needsCorrection)
            {
                Quaternion targetRotation = Quaternion.Euler(angleX, euler.y, angleZ);
                _rigidbody.MoveRotation(targetRotation);
            }

            // Apply uprighting torque
            ApplyUprightingTorque();
        }

        /// <summary>
        /// Apply torque to naturally return player to upright position.
        /// </summary>
        private void ApplyUprightingTorque()
        {
            Vector3 currentUp = transform.up;
            Vector3 targetUp = Vector3.up;

            // Calculate torque needed to align with world up
            Vector3 torqueDirection = Vector3.Cross(currentUp, targetUp);
            float torqueMagnitude = Vector3.Angle(currentUp, targetUp) / 180f;

            _rigidbody.AddTorque(torqueDirection * torqueMagnitude * _uprightingForce, ForceMode.Acceleration);
        }

        /// <summary>
        /// Normalize angle to -180 to 180 range.
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Draw water surface
            Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
            Vector3 waterPos = transform.position;
            waterPos.y = _waterSurfaceY;
            Gizmos.DrawWireCube(waterPos, new Vector3(2f, 0.01f, 2f));

            // Draw elevation limits
            Gizmos.color = Color.green;
            Vector3 maxPos = waterPos;
            maxPos.y = _waterSurfaceY + _maxElevation;
            Gizmos.DrawWireCube(maxPos, new Vector3(2f, 0.01f, 2f));

            Gizmos.color = Color.red;
            Vector3 minPos = waterPos;
            minPos.y = _waterSurfaceY + _minDepth;
            Gizmos.DrawWireCube(minPos, new Vector3(2f, 0.01f, 2f));
        }

        #endregion
    }
}
