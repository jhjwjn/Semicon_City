using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SemiconCity.Game
{
    /// <summary>
    /// TextMeshPro label backed by a dynamic Korean OS font. SDF rendering keeps
    /// glyph edges stable while the reference-resolution canvas is scaled.
    /// </summary>
    [AddComponentMenu("UI/Semicon SDF Text")]
    public sealed class SemiconSdfText : TextMeshProUGUI
    {
        private static readonly (string family, string style)[] FontCandidates =
        {
            ("Malgun Gothic", "Regular"),
            ("Noto Sans KR", "Regular"),
            ("Arial", "Regular")
        };

        private static TMP_FontAsset sharedKoreanFont;

        protected override void Awake()
        {
            if (Application.isPlaying)
            {
                var readableFont = GetReadableFont();
                if (readableFont != null)
                {
                    font = readableFont;
                }
            }
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Application.isPlaying)
            {
                return;
            }

            var readableFont = GetReadableFont();
            if (readableFont != null && font != readableFont)
            {
                font = readableFont;
            }
        }

        private static TMP_FontAsset GetReadableFont()
        {
            if (sharedKoreanFont != null)
            {
                return sharedKoreanFont;
            }

            // Use one modern Korean family for every label. A larger SDF atlas and
            // calibrated synthetic weights keep small captions clean while avoiding
            // the loose vertical metrics of the previous Hancom Gothic runtime font.
            const string koreanUiFontPath = @"C:\Windows\Fonts\malgun.ttf";
            if (File.Exists(koreanUiFontPath))
            {
                sharedKoreanFont = TMP_FontAsset.CreateFontAsset(koreanUiFontPath, 0, 112, 12,
                    GlyphRenderMode.SDFAA, 4096, 4096);
                if (sharedKoreanFont != null)
                {
                    sharedKoreanFont.name = "Semicon UI SDF / Malgun Gothic";
                    sharedKoreanFont.isMultiAtlasTexturesEnabled = true;
                    sharedKoreanFont.normalStyle = 0.45f;
                    sharedKoreanFont.boldStyle = 0.9f;
                    sharedKoreanFont.boldSpacing = 0.5f;
                    sharedKoreanFont.normalSpacingOffset = 0f;
                    Debug.Log("[Semicon Font] Malgun Gothic high-resolution SDF loaded.");
                    return sharedKoreanFont;
                }
            }

            foreach (var candidate in FontCandidates)
            {
                sharedKoreanFont = TMP_FontAsset.CreateFontAsset(candidate.family, candidate.style, 72);
                if (sharedKoreanFont == null)
                {
                    continue;
                }

                sharedKoreanFont.name = "Semicon UI SDF / " + candidate.family;
                sharedKoreanFont.isMultiAtlasTexturesEnabled = true;
                return sharedKoreanFont;
            }

            return null;
        }
    }
}
