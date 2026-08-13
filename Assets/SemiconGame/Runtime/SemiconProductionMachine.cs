using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconProductionMachine : SemiconInteractable
    {
        [SerializeField] private SemiconProductionPanel productionPanel;
        [SerializeField] private Transform statusLight;
        [SerializeField] private int slotIndex;

        public override string Prompt
        {
            get
            {
                var job = SemiconGameState.Instance?.GetProductionJob(slotIndex) ?? default;
                if (job.HasJob && job.IsComplete)
                    return $"[E]  SLOT {slotIndex + 1:00} 생산 완료\n완료품 회수 대기";
                if (job.HasJob)
                    return $"[E]  SLOT {slotIndex + 1:00} 생산 중\n남은 시간 {job.RemainingSeconds:0.0}초";
                return $"[E]  SLOT {slotIndex + 1:00} 공정 제어\n레시피 선택 및 가동";
            }
        }

        private void Update()
        {
            if (statusLight == null)
            {
                return;
            }
            var job = SemiconGameState.Instance?.GetProductionJob(slotIndex) ?? default;
            var speed = job.HasJob && !job.IsComplete ? 7.5f : job.IsComplete ? 4.5f : 3.2f;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * speed) * (job.HasJob ? 0.1f : 0.05f);
            statusLight.localScale = Vector3.one * pulse;
        }

        public void Configure(SemiconProductionPanel panel, Transform lightTransform, int index = 0)
        {
            productionPanel = panel;
            statusLight = lightTransform;
            slotIndex = index;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            productionPanel?.Open(player, followCamera, slotIndex);
        }
    }
}
