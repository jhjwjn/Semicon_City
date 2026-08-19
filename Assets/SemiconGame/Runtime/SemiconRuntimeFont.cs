using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    public sealed class SemiconRuntimeFont : MonoBehaviour
    {
        [SerializeField, TextArea] private string preloadCharacters;

        private void Awake()
        {
            var sharedFont = SemiconCrispText.GetSharedFont();
            if (sharedFont == null)
            {
                return;
            }

            // Complete this once before the first canvas render. Runtime counters,
            // tab changes and tutorial messages then reuse the same atlas forever.
            sharedFont.RequestCharactersInTexture(
                preloadCharacters ?? string.Empty,
                SemiconCrispText.RasterFontSize,
                FontStyle.Normal);
            var atlas = sharedFont.material != null ? sharedFont.material.mainTexture : null;
            var atlasSize = atlas != null ? $"{atlas.width}x{atlas.height}" : "none";
            Debug.Log($"[Semicon Font] Legacy UI atlas warmed / glyphs={preloadCharacters?.Length ?? 0} / " +
                      $"raster={SemiconCrispText.RasterFontSize} / texture=" +
                      atlasSize);

            var labels = GetComponentsInChildren<Text>(true);
            var hudFont = Resources.Load<Font>("Fonts/SemiconHudBold");
            if (hudFont != null)
            {
                var hudCharacters = new StringBuilder(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~₩·→▶◀×");
                foreach (var label in labels)
                {
                    if (IsHudLabel(label.transform)) hudCharacters.Append(label.text);
                }
                hudFont.RequestCharactersInTexture(hudCharacters.ToString(), SemiconCrispText.RasterFontSize,
                    FontStyle.Normal);
            }

            foreach (var label in labels)
            {
                label.font = hudFont != null && IsHudLabel(label.transform) ? hudFont : sharedFont;
                label.fontStyle = FontStyle.Normal;
                label.SetVerticesDirty();
            }
        }

        private static bool IsHudLabel(Transform label)
        {
            for (var current = label; current != null; current = current.parent)
            {
                if (current.name.EndsWith(" Screen", System.StringComparison.Ordinal)) return false;
            }
            return true;
        }
    }
}
