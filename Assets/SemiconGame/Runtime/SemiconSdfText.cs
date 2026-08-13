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

            // Unity reads the installed Noto Sans KR variable font at its thinnest
            // instance on some Windows machines. Use a static Korean face so that
            // glyph weight and vertical metrics remain stable in every UI state.
            const string hancomGothicPath = @"C:\Windows\Fonts\Hancom Gothic Regular.ttf";
            if (File.Exists(hancomGothicPath))
            {
                sharedKoreanFont = TMP_FontAsset.CreateFontAsset(hancomGothicPath, 0, 84, 10,
                    GlyphRenderMode.SDFAA, 1024, 1024);
                if (sharedKoreanFont != null)
                {
                    sharedKoreanFont.name = "Semicon UI SDF / Hancom Gothic Regular";
                    sharedKoreanFont.isMultiAtlasTexturesEnabled = true;
                    Debug.Log("[Semicon Font] Hancom Gothic Regular SDF loaded.");
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
