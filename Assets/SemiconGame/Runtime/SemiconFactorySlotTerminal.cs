using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconFactorySlotTerminal : SemiconInteractable
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private SemiconFactoryLoadoutPanel loadoutPanel;
        [SerializeField] private GameObject machineVisual;
        [SerializeField] private Transform statusLight;

        public override string Prompt
        {
            get
            {
                var slot = SemiconGameState.Instance?.GetFactorySlot(slotIndex);
                return slot != null && slot.machineInstalled
                    ? $"[E]  SLOT {slotIndex + 1:00} 설비 구성\n인력·디스크 배정"
                    : $"[E]  SLOT {slotIndex + 1:00} 설비 배치";
            }
        }

        private void Start()
        {
            if (SemiconGameState.Instance != null)
            {
                SemiconGameState.Instance.StateChanged += Refresh;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (SemiconGameState.Instance != null)
            {
                SemiconGameState.Instance.StateChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (statusLight == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.2f + slotIndex) * 0.06f;
            statusLight.localScale = Vector3.one * pulse;
        }

        public void Configure(int index, SemiconFactoryLoadoutPanel panel, GameObject visual, Transform lightTransform)
        {
            slotIndex = index;
            loadoutPanel = panel;
            machineVisual = visual;
            statusLight = lightTransform;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            loadoutPanel?.Open(slotIndex, player, followCamera);
        }

        private void Refresh()
        {
            var slot = SemiconGameState.Instance?.GetFactorySlot(slotIndex);
            if (machineVisual != null) machineVisual.SetActive(slot != null && slot.machineInstalled);
        }
    }
}
