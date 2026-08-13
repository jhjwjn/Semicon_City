using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    /// <summary>
    /// Keeps the original layout size while supersampling glyphs whenever the
    /// Canvas is rendered below its reference resolution.
    /// </summary>
    public sealed class SemiconCrispText : Text
    {
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
            ApplyReadableSystemFont();
        }

        private void ApplyReadableSystemFont()
        {
            if (readableSystemFont == null)
            {
                readableSystemFont = Font.CreateDynamicFontFromOSFont(PreferredFontNames, 32);
                if (readableSystemFont != null)
                {
                    readableSystemFont.name = "Semicon UI Korean Font";
                }
            }

            if (readableSystemFont != null && font != readableSystemFont)
            {
                font = readableSystemFont;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            if (font == null)
            {
                vertexHelper.Clear();
                return;
            }

            var layoutPixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
            var canvasScale = canvas != null ? Mathf.Max(0.25f, canvas.scaleFactor) : 1f;
            var supersample = Mathf.Clamp(1f / canvasScale, 1f, 2f);
            var settings = GetGenerationSettings(rectTransform.rect.size * supersample);
            settings.fontSize = Mathf.Max(1, Mathf.RoundToInt(settings.fontSize * supersample));
            settings.resizeTextMinSize = Mathf.Max(1, Mathf.RoundToInt(settings.resizeTextMinSize * supersample));
            settings.resizeTextMaxSize = Mathf.Max(1, Mathf.RoundToInt(settings.resizeTextMaxSize * supersample));
            settings.scaleFactor = layoutPixelsPerUnit;
            cachedTextGenerator.Populate(text, settings);

            IList<UIVertex> vertices = cachedTextGenerator.verts;
            var vertexCount = vertices.Count;
            if (vertexCount <= 0)
            {
                vertexHelper.Clear();
                return;
            }

            var unitsPerRasterPixel = 1f / (layoutPixelsPerUnit * supersample);
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
    }
}
