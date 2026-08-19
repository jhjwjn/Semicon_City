using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconGachaTerminal : SemiconInteractable
    {
        [SerializeField] private SemiconGachaPanel panel;
        [SerializeField] private Transform glowTransform;

        public override string Prompt => "[E]  로봇 보급 센터\n작업 로봇 모집 및 특성 디스크 추첨";

        public void Configure(SemiconGachaPanel targetPanel, Transform glow)
        {
            panel = targetPanel;
            glowTransform = glow;
        }

        private void Update()
        {
            if (glowTransform == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.35f) * 0.06f;
            glowTransform.localScale = Vector3.one * pulse;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            panel?.Open(player, followCamera);
        }
    }
}
