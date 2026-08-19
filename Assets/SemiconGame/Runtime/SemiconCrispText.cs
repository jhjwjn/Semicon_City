using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    /// <summary>
    /// Renders every legacy UI label from one high-resolution glyph size. This
    /// keeps Korean strokes crisp without asking Unity's dynamic atlas for a new
    /// copy of every character whenever a label uses a different point size.
    /// </summary>
    public sealed class SemiconCrispText : Text
    {
        public const int RasterFontSize = 48;

        private static readonly string[] PreferredFontNames =
        {
            "Malgun Gothic",
            "맑은 고딕",
            "Noto Sans KR",
            "Arial"
        };

        private static Font readableSystemFont;
        private readonly UIVertex[] temporaryVertices = new UIVertex[4];

        protected override void OnEnable()
        {
            base.OnEnable();
            // Generated scenes serialize the project-bundled static Korean font.
            // Only use the OS fallback for manually created labels without a font.
            if (font == null || font.name == "LegacyRuntime")
            {
                ApplyReadableSystemFont();
            }
            // SemiconUiBold already contains the true bold face. Keeping the style
            // override at Normal prevents Unity from allocating a second fake-bold
            // glyph set and avoids a texture rebuild between screens.
            fontStyle = FontStyle.Normal;
        }

        internal static Font GetSharedFont()
        {
            if (readableSystemFont == null)
            {
                // Runtime-created HUD labels cannot keep an editor asset reference.
                // Load the same pre-baked font used by generated scenes so every
                // label shares one immutable atlas instead of repacking OS glyphs.
                readableSystemFont = Resources.Load<Font>("Fonts/SemiconUiBold");
                if (readableSystemFont == null)
                {
                    readableSystemFont = Font.CreateDynamicFontFromOSFont(PreferredFontNames, 64);
                }
                if (readableSystemFont != null)
                {
                    if (readableSystemFont.material != null && readableSystemFont.material.mainTexture != null)
                    {
                        readableSystemFont.material.mainTexture.filterMode = FilterMode.Bilinear;
                    }
                }
            }

            return readableSystemFont;
        }

        private void ApplyReadableSystemFont()
        {
            var sharedFont = GetSharedFont();

            if (sharedFont != null && font != sharedFont)
            {
                font = sharedFont;
            }
        }

        public override float preferredWidth
        {
            get
            {
                if (font == null) return 0f;
                var rasterScale = RasterFontSize / (float)Mathf.Max(1, fontSize);
                var settings = GetRasterGenerationSettings(Vector2.zero);
                return cachedTextGeneratorForLayout.GetPreferredWidth(text, settings) /
                       Mathf.Max(1f, pixelsPerUnit) / rasterScale;
            }
        }

        public override float preferredHeight
        {
            get
            {
                if (font == null) return 0f;
                var rasterScale = RasterFontSize / (float)Mathf.Max(1, fontSize);
                var extents = new Vector2(rectTransform.rect.size.x * rasterScale, 0f);
                var settings = GetRasterGenerationSettings(extents);
                return cachedTextGeneratorForLayout.GetPreferredHeight(text, settings) /
                       Mathf.Max(1f, pixelsPerUnit) / rasterScale;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            if (font == null)
            {
                vertexHelper.Clear();
                return;
            }

            var requestedSize = Mathf.Max(1, fontSize);
            var rasterScale = RasterFontSize / (float)requestedSize;
            var layoutPixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
            var settings = GetRasterGenerationSettings(rectTransform.rect.size * rasterScale);
            settings.scaleFactor = layoutPixelsPerUnit;
            cachedTextGenerator.Populate(text, settings);

            IList<UIVertex> vertices = cachedTextGenerator.verts;
            var vertexCount = vertices.Count;
            if (vertexCount <= 0)
            {
                vertexHelper.Clear();
                return;
            }

            var unitsPerRasterPixel = 1f / (layoutPixelsPerUnit * rasterScale);
            var roundingOffset = new Vector2(vertices[0].position.x, vertices[0].position.y) * unitsPerRasterPixel;
            roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;

            vertexHelper.Clear();
            for (var index = 0; index < vertexCount; index++)
            {
                var quadIndex = index & 3;
                temporaryVertices[quadIndex] = vertices[index];
                temporaryVertices[quadIndex].position *= unitsPerRasterPixel;
                temporaryVertices[quadIndex].position.x += roundingOffset.x;
                temporaryVertices[quadIndex].position.y += roundingOffset.y;
                if (quadIndex == 3)
                {
                    vertexHelper.AddUIVertexQuad(temporaryVertices);
                }
            }
        }

        private TextGenerationSettings GetRasterGenerationSettings(Vector2 extents)
        {
            var settings = GetGenerationSettings(extents);
            settings.fontSize = RasterFontSize;
            settings.fontStyle = FontStyle.Normal;
            settings.resizeTextForBestFit = false;
            settings.resizeTextMinSize = RasterFontSize;
            settings.resizeTextMaxSize = RasterFontSize;
            settings.scaleFactor = Mathf.Max(1f, pixelsPerUnit);
            return settings;
        }

    }
}
