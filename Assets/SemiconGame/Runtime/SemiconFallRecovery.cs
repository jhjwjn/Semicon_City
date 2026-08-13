using UnityEngine;

namespace SemiconCity.Game
{
    /// <summary>
    /// Safety net for imported city geometry. The authored walkable meshes still
    /// provide normal collision; this component only restores the player if a
    /// missed seam or an unloaded collider lets them leave the world.
    /// </summary>
    [RequireComponent(typeof(SemiconPlayerController))]
    public sealed class SemiconFallRecovery : MonoBehaviour
    {
        [SerializeField] private float recoveryHeight = -4f;
        [SerializeField] private float safePositionSampleInterval = 0.35f;

        private SemiconPlayerController controller;
        private Vector3 lastSafePosition;
        private Quaternion lastSafeRotation;
        private float nextSampleTime;

        private void Awake()
        {
            controller = GetComponent<SemiconPlayerController>();
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
        }

        private void Update()
        {
            if (transform.position.y < recoveryHeight)
            {
                controller.Teleport(lastSafePosition + Vector3.up * 0.12f, lastSafeRotation);
                return;
            }

            if (Time.time < nextSampleTime || !controller.IsGrounded)
            {
                return;
            }

            nextSampleTime = Time.time + safePositionSampleInterval;
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
        }

        public void SetSafePosition(Vector3 position, Quaternion rotation)
        {
            lastSafePosition = position;
            lastSafeRotation = rotation;
        }
    }
}
