using UnityEngine;
using UnityEngine.InputSystem;

namespace SemiconCity.Game
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SemiconPlayerController : MonoBehaviour
    {
        [SerializeField] private Transform movementCamera;
        [SerializeField, Min(0f)] private float walkSpeed = 5.2f;
        [SerializeField, Min(0f)] private float sprintSpeed = 8.2f;
        [SerializeField, Min(0f)] private float rotationSpeed = 13f;
        [SerializeField] private float gravity = -24f;

        private CharacterController controller;
        private float verticalVelocity;

        public bool InputEnabled { get; private set; } = true;
        public bool IsGrounded => controller != null && controller.isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            SetCursorLocked(true);
        }

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var moveInput = Vector2.zero;
            if (InputEnabled && keyboard != null)
            {
                moveInput.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                moveInput.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            }

            var cameraTransform = movementCamera != null ? movementCamera : Camera.main != null ? Camera.main.transform : null;
            var forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            var right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            var move = forward * moveInput.y + right * moveInput.x;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            if (move.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            var sprinting = InputEnabled && keyboard != null && keyboard.leftShiftKey.isPressed;
            var speed = sprinting ? sprintSpeed : walkSpeed;
            controller.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        public void ConfigureCamera(Transform cameraTransform)
        {
            movementCamera = cameraTransform;
        }

        public void SetInputEnabled(bool enabled)
        {
            InputEnabled = enabled;
            if (!enabled)
            {
                verticalVelocity = Mathf.Min(verticalVelocity, 0f);
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (controller == null)
            {
                transform.SetPositionAndRotation(position, rotation);
                return;
            }

            var wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            verticalVelocity = 0f;
            controller.enabled = wasEnabled;
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
