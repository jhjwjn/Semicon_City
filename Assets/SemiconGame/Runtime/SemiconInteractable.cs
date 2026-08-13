using UnityEngine;

namespace SemiconCity.Game
{
    /// <summary>
    /// Shared interaction point for outdoor terminals, factory entrances and
    /// future indoor production machines.
    /// </summary>
    public abstract class SemiconInteractable : MonoBehaviour
    {
        public abstract string Prompt { get; }

        public virtual Vector3 InteractionPosition => transform.position;

        public abstract void Interact(
            SemiconPlayerController player,
            SemiconThirdPersonCamera followCamera);
    }
}
