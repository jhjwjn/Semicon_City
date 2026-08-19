using UnityEngine;
using UnityEngine.InputSystem;

namespace SemiconCity.Game
{
    public sealed class SemiconPlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float interactionRadius = 4.2f;
        [SerializeField] private SemiconHud hud;
        [SerializeField] private SemiconPlayerController playerController;
        [SerializeField] private SemiconThirdPersonCamera followCamera;

        private readonly Collider[] overlaps = new Collider[24];
        private SemiconInteractable currentInteractable;

        private void Update()
        {
            currentInteractable = FindNearestInteractable();
            hud?.SetInteraction(currentInteractable != null ? currentInteractable.Prompt : string.Empty,
                currentInteractable != null);

            if (currentInteractable != null && playerController != null && playerController.InputEnabled &&
                Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                currentInteractable.Interact(playerController, followCamera);
            }
        }

        public void Configure(SemiconHud targetHud, SemiconPlayerController controller, SemiconThirdPersonCamera cameraController)
        {
            hud = targetHud;
            playerController = controller;
            followCamera = cameraController;
        }

        private SemiconInteractable FindNearestInteractable()
        {
            var count = Physics.OverlapSphereNonAlloc(transform.position, interactionRadius, overlaps, ~0, QueryTriggerInteraction.Collide);
            SemiconInteractable nearest = null;
            var nearestDistance = float.MaxValue;
            for (var index = 0; index < count; index++)
            {
                var collider = overlaps[index];
                var interactable = collider != null ? collider.GetComponentInParent<SemiconInteractable>() : null;
                if (interactable == null)
                {
                    continue;
                }

                var distance = (interactable.InteractionPosition - transform.position).sqrMagnitude;
                if (distance >= nearestDistance)
                {
                    continue;
                }
                nearestDistance = distance;
                nearest = interactable;
            }
            return nearest;
        }
    }
}
