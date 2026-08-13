using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconMarketTerminal : SemiconInteractable
    {
        [SerializeField] private SemiconMarketPanel marketPanel;
        [SerializeField] private Transform glowTransform;

        public override string Prompt => "[E]  자재 거래소\n원재료 구매 및 완제품 출하";

        private void Update()
        {
            if (glowTransform == null)
            {
                return;
            }
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }

        public void Configure(SemiconMarketPanel panel, Transform glow)
        {
            marketPanel = panel;
            glowTransform = glow;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            marketPanel?.Open(player, followCamera);
        }
    }
}
