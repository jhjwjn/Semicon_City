using UnityEngine;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconRuntimeFont : MonoBehaviour
    {
        private const string CommonDynamicCharacters = "0123456789,.-+/%₩m초분개";

        private Font runtimeFont;

        private void Awake()
        {
            runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Noto Sans KR", "Malgun Gothic", "맑은 고딕", "Arial" },
                28);
            if (runtimeFont == null)
            {
                return;
            }

            if (runtimeFont.material != null && runtimeFont.material.mainTexture != null)
            {
                runtimeFont.material.mainTexture.filterMode = FilterMode.Bilinear;
            }

            var labels = GetComponentsInChildren<Text>(true);
            foreach (var label in labels)
            {
                runtimeFont.RequestCharactersInTexture(
                    (label.text ?? string.Empty) + CommonDynamicCharacters,
                    label.fontSize,
                    label.fontStyle);
            }

            // Preload before any UI Text references the font. Runtime number changes then reuse
            // the existing atlas instead of invalidating unrelated Korean labels for one frame.
            foreach (var label in labels)
            {
                label.font = runtimeFont;
            }
        }
    }
}
