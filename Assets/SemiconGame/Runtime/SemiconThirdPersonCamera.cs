using UnityEngine;
using UnityEngine.InputSystem;

namespace SemiconCity.Game
{
    public sealed class SemiconThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.7f, 0f);
        [SerializeField] private float distance = 7.5f;
        [SerializeField] private float yaw = 180f;
        [SerializeField] private float pitch = 22f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float followSharpness = 14f;

        public bool LookEnabled { get; private set; } = true;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (LookEnabled && Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                var delta = Mouse.current.delta.ReadValue();
                yaw += delta.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, 8f, 62f);
            }

            var pivot = target.position + targetOffset;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var desiredPosition = pivot - rotation * Vector3.forward * distance;

            var direction = desiredPosition - pivot;
            if (Physics.SphereCast(pivot, 0.25f, direction.normalized, out var hit, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                desiredPosition = pivot + direction.normalized * Mathf.Max(0.4f, hit.distance - 0.2f);
            }

            var blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
            transform.rotation = rotation;
        }

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                var rotation = Quaternion.Euler(pitch, yaw, 0f);
                transform.position = target.position + targetOffset - rotation * Vector3.forward * distance;
                transform.rotation = rotation;
            }
        }

        public void SetLookEnabled(bool enabled)
        {
            LookEnabled = enabled;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }
            yaw = target.eulerAngles.y;
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = target.position + targetOffset - rotation * Vector3.forward * distance;
            transform.rotation = rotation;
        }

        public void SetInteriorMode(bool interior)
        {
            distance = interior ? 4.5f : 7.5f;
            pitch = interior ? 16f : 22f;
        }
    }
}
