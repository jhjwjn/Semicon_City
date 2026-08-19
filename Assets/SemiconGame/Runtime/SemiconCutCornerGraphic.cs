using UnityEngine;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    [AddComponentMenu("UI/Semicon Cut Corner Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SemiconCutCornerGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float cornerCut = 18f;

        public float CornerCut
        {
            get => cornerCut;
            set
            {
                cornerCut = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var cut = Mathf.Min(cornerCut, Mathf.Min(rect.width, rect.height) * 0.45f);
            var points = new[]
            {
                new Vector2(rect.xMin + cut, rect.yMin),
                new Vector2(rect.xMax - cut, rect.yMin),
                new Vector2(rect.xMax, rect.yMin + cut),
                new Vector2(rect.xMax, rect.yMax - cut),
                new Vector2(rect.xMax - cut, rect.yMax),
                new Vector2(rect.xMin + cut, rect.yMax),
                new Vector2(rect.xMin, rect.yMax - cut),
                new Vector2(rect.xMin, rect.yMin + cut)
            };

            for (var index = 0; index < points.Length; index++)
            {
                vertexHelper.AddVert(points[index], color, Vector2.zero);
            }

            for (var index = 1; index < points.Length - 1; index++)
            {
                vertexHelper.AddTriangle(0, index + 1, index);
            }
        }
    }
}
