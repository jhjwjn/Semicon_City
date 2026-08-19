using UnityEngine;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    [AddComponentMenu("UI/Semicon Photo Wafer Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SemiconPhotoWaferGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)] private float patternReveal = 0.28f;
        [SerializeField, Range(0f, 1f)] private float scanProgress;
        [SerializeField] private bool showScan;
        [SerializeField] private bool completed;

        private static readonly Color32 WaferCenter = new Color32(166, 220, 246, 235);
        private static readonly Color32 WaferEdge = new Color32(87, 139, 221, 235);
        private static readonly Color32 GridColor = new Color32(220, 248, 255, 122);
        private static readonly Color32 PatternColor = new Color32(50, 220, 231, 238);
        private static readonly Color32 ScanColor = new Color32(222, 253, 255, 255);
        private static readonly Color32 CompleteColor = new Color32(45, 204, 157, 255);
        private static readonly Color32 StageShadow = new Color32(4, 25, 39, 80);
        private static readonly Color32 StageOuter = new Color32(20, 54, 72, 238);
        private static readonly Color32 StageMetal = new Color32(126, 166, 184, 235);
        private static readonly Color32 StageInner = new Color32(8, 34, 53, 248);

        public float PatternReveal
        {
            get => patternReveal;
            set
            {
                patternReveal = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }

        public float ScanProgress
        {
            get => scanProgress;
            set
            {
                scanProgress = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }

        public bool ShowScan
        {
            get => showScan;
            set
            {
                showScan = value;
                SetVerticesDirty();
            }
        }

        public bool Completed
        {
            get => completed;
            set
            {
                completed = value;
                SetVerticesDirty();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            if (rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            var center = rect.center + new Vector2(0f, -4f);
            var stageRadiusX = rect.width * 0.485f;
            var stageRadiusY = rect.height * 0.42f;
            AddExposureBeam(vertexHelper, center, stageRadiusX, stageRadiusY);
            AddSolidEllipse(vertexHelper, center + new Vector2(0f, -12f), stageRadiusX, stageRadiusY, StageShadow);
            AddSolidEllipse(vertexHelper, center, stageRadiusX, stageRadiusY, StageOuter);
            AddRing(vertexHelper, center, stageRadiusX * 0.965f, stageRadiusY * 0.96f, StageMetal, 12f);
            AddRing(vertexHelper, center, stageRadiusX * 0.91f, stageRadiusY * 0.885f,
                new Color32(206, 232, 239, 220), 3f);
            AddSolidEllipse(vertexHelper, center, stageRadiusX * 0.86f, stageRadiusY * 0.81f, StageInner);
            AddEquipmentDetails(vertexHelper, center, stageRadiusX, stageRadiusY);

            var radiusX = rect.width * 0.39f;
            var radiusY = rect.height * 0.305f;
            AddEllipse(vertexHelper, center, radiusX, radiusY);
            AddRing(vertexHelper, center, radiusX * 0.955f, radiusY * 0.955f,
                new Color32(226, 249, 255, 108), 1.5f);
            AddGrid(vertexHelper, center, radiusX, radiusY);
            AddDieDetails(vertexHelper, center, radiusX, radiusY);
            AddCircuitPattern(vertexHelper, center, radiusX, radiusY);
            AddFiducials(vertexHelper, center, radiusX, radiusY);
            AddRegistrationMark(vertexHelper, center, radiusX, radiusY);
            AddRing(vertexHelper, center, radiusX, radiusY,
                completed ? CompleteColor : new Color32(176, 239, 255, 246), completed ? 4.5f : 3f);

            if (showScan)
            {
                AddScanLine(vertexHelper, center, radiusX, radiusY);
            }
        }

        private void AddExposureBeam(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY)
        {
            var source = center + new Vector2(0f, radiusY * 1.22f);
            AddTriangle(vertexHelper, source, center + new Vector2(-radiusX * 0.54f, radiusY * 0.16f),
                center + new Vector2(radiusX * 0.54f, radiusY * 0.16f), new Color32(83, 214, 229, 24));
            AddLine(vertexHelper, source, center + new Vector2(-radiusX * 0.54f, radiusY * 0.16f),
                2f, new Color32(66, 183, 211, 74));
            AddLine(vertexHelper, source, center + new Vector2(radiusX * 0.54f, radiusY * 0.16f),
                2f, new Color32(66, 183, 211, 74));
        }

        private void AddEquipmentDetails(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY)
        {
            var tickColor = new Color32(111, 199, 218, 186);
            for (var index = 0; index < 12; index++)
            {
                var angle = index * Mathf.PI * 2f / 12f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var tangent = new Vector2(-direction.y, direction.x);
                var start = center + new Vector2(direction.x * radiusX * 0.9f, direction.y * radiusY * 0.9f);
                var end = center + new Vector2(direction.x * radiusX * 0.96f, direction.y * radiusY * 0.96f);
                AddLine(vertexHelper, start - tangent * 4f, end + tangent * 4f, 2f, tickColor);
            }

            var slotColor = new Color32(201, 229, 237, 180);
            AddQuad(vertexHelper, center + new Vector2(-radiusX * 0.88f, -6f),
                center + new Vector2(-radiusX * 0.73f, 6f), slotColor);
            AddQuad(vertexHelper, center + new Vector2(radiusX * 0.73f, -6f),
                center + new Vector2(radiusX * 0.88f, 6f), slotColor);
            AddLine(vertexHelper, center + new Vector2(-radiusX * 0.62f, -radiusY * 0.7f),
                center + new Vector2(radiusX * 0.62f, -radiusY * 0.7f), 2f, new Color32(69, 139, 170, 130));
        }

        private void AddSolidEllipse(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY,
            Color colorValue)
        {
            const int segments = 128;
            var centerIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, Tint(colorValue), new Vector2(0.5f, 0.5f));
            for (var index = 0; index <= segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var point = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                vertexHelper.AddVert(point, Tint(colorValue), new Vector2((Mathf.Cos(angle) + 1f) * 0.5f,
                    (Mathf.Sin(angle) + 1f) * 0.5f));
            }
            for (var index = 0; index < segments; index++)
            {
                vertexHelper.AddTriangle(centerIndex, centerIndex + index + 1, centerIndex + index + 2);
            }
        }

        private void AddEllipse(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY)
        {
            const int segments = 128;
            var centerIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, Tint(WaferCenter), new Vector2(0.5f, 0.5f));
            for (var index = 0; index <= segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var point = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                var highlight = Mathf.Clamp01((Mathf.Sin(angle) + 1f) * 0.28f);
                var edge = Color.Lerp(WaferEdge, WaferCenter, highlight);
                vertexHelper.AddVert(point, Tint(edge), new Vector2((Mathf.Cos(angle) + 1f) * 0.5f,
                    (Mathf.Sin(angle) + 1f) * 0.5f));
            }
            for (var index = 0; index < segments; index++)
            {
                vertexHelper.AddTriangle(centerIndex, centerIndex + index + 1, centerIndex + index + 2);
            }
        }

        private void AddGrid(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY)
        {
            for (var index = -7; index <= 7; index++)
            {
                var normalizedX = index / 8f;
                var halfHeight = Mathf.Sqrt(Mathf.Max(0f, 1f - normalizedX * normalizedX)) * radiusY;
                var x = center.x + normalizedX * radiusX;
                AddLine(vertexHelper, new Vector2(x, center.y - halfHeight), new Vector2(x, center.y + halfHeight),
                    1.5f, GridColor);
            }

            for (var index = -5; index <= 5; index++)
            {
                var normalizedY = index / 6f;
                var halfWidth = Mathf.Sqrt(Mathf.Max(0f, 1f - normalizedY * normalizedY)) * radiusX;
                var y = center.y + normalizedY * radiusY;
                AddLine(vertexHelper, new Vector2(center.x - halfWidth, y), new Vector2(center.x + halfWidth, y),
                    1.5f, GridColor);
            }
        }

        private void AddDieDetails(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY)
        {
            var fill = new Color32(225, 250, 255, 42);
            var edge = new Color32(211, 246, 255, 92);
            for (var row = -3; row <= 3; row++)
            {
                for (var column = -5; column <= 5; column++)
                {
                    if ((row * row + column * column) % 5 != 0)
                    {
                        continue;
                    }

                    var normalizedX = column / 6.5f;
                    var normalizedY = row / 4.5f;
                    if (normalizedX * normalizedX + normalizedY * normalizedY > 0.78f)
                    {
                        continue;
                    }

                    var dieCenter = center + new Vector2(normalizedX * radiusX, normalizedY * radiusY);
                    var size = new Vector2(radiusX * 0.075f, radiusY * 0.105f);
                    AddQuad(vertexHelper, dieCenter - size * 0.5f, dieCenter + size * 0.5f, fill);
                    AddLine(vertexHelper, dieCenter + new Vector2(-size.x * 0.32f, 0f),
                        dieCenter + new Vector2(size.x * 0.32f, 0f), 1.4f, edge);
                }
            }
        }

        private void AddFiducials(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY)
        {
            var colorValue = new Color32(222, 250, 255, 190);
            var width = 2f;
            var armX = radiusX * 0.055f;
            var armY = radiusY * 0.075f;
            var positions = new[]
            {
                center + new Vector2(-radiusX * 0.63f, radiusY * 0.48f),
                center + new Vector2(radiusX * 0.63f, radiusY * 0.48f),
                center + new Vector2(-radiusX * 0.63f, -radiusY * 0.48f),
                center + new Vector2(radiusX * 0.63f, -radiusY * 0.48f)
            };
            foreach (var position in positions)
            {
                AddLine(vertexHelper, position - new Vector2(armX, 0f), position + new Vector2(armX, 0f),
                    width, colorValue);
                AddLine(vertexHelper, position - new Vector2(0f, armY), position + new Vector2(0f, armY),
                    width, colorValue);
            }
        }

        private void AddCircuitPattern(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY)
        {
            var revealCutoff = Mathf.Lerp(radiusY, -radiusY, patternReveal);
            for (var row = -4; row <= 4; row++)
            {
                var normalizedY = row / 5f;
                var y = center.y + normalizedY * radiusY;
                if (y < center.y + revealCutoff)
                {
                    continue;
                }
                var halfWidth = Mathf.Sqrt(Mathf.Max(0f, 1f - normalizedY * normalizedY)) * radiusX * 0.86f;
                var inset = (Mathf.Abs(row) % 2 == 0 ? 0.12f : 0.28f) * radiusX;
                AddLine(vertexHelper, new Vector2(center.x - halfWidth, y), new Vector2(center.x - inset, y),
                    2.4f, PatternColor);
                AddLine(vertexHelper, new Vector2(center.x + inset, y), new Vector2(center.x + halfWidth, y),
                    2.4f, PatternColor);
            }

            for (var column = -4; column <= 4; column++)
            {
                var normalizedX = column / 5f;
                var x = center.x + normalizedX * radiusX;
                var halfHeight = Mathf.Sqrt(Mathf.Max(0f, 1f - normalizedX * normalizedX)) * radiusY * 0.82f;
                var bottom = Mathf.Max(center.y - halfHeight, center.y + revealCutoff);
                var top = center.y + halfHeight;
                if (bottom >= top)
                {
                    continue;
                }
                var split = Mathf.Lerp(bottom, top, 0.5f + (column % 2) * 0.12f);
                AddLine(vertexHelper, new Vector2(x, bottom), new Vector2(x, split - 7f), 2.1f, PatternColor);
                AddLine(vertexHelper, new Vector2(x, split + 7f), new Vector2(x, top), 2.1f, PatternColor);
            }

            var diamondRadiusX = radiusX * 0.26f;
            var diamondRadiusY = radiusY * 0.35f;
            var diamondBottom = center.y - diamondRadiusY;
            if (center.y + diamondRadiusY >= center.y + revealCutoff)
            {
                var diamondColor = new Color32(37, 220, 225, 238);
                AddClippedLine(vertexHelper, center + new Vector2(0f, diamondRadiusY),
                    center + new Vector2(diamondRadiusX, 0f), center.y + revealCutoff, 5f, diamondColor);
                AddClippedLine(vertexHelper, center + new Vector2(diamondRadiusX, 0f),
                    center + new Vector2(0f, -diamondRadiusY), center.y + revealCutoff, 5f, diamondColor);
                AddClippedLine(vertexHelper, center + new Vector2(0f, -diamondRadiusY),
                    center + new Vector2(-diamondRadiusX, 0f), center.y + revealCutoff, 5f, diamondColor);
                AddClippedLine(vertexHelper, center + new Vector2(-diamondRadiusX, 0f),
                    center + new Vector2(0f, diamondRadiusY), center.y + revealCutoff, 5f, diamondColor);
            }
        }

        private void AddRegistrationMark(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY)
        {
            var markColor = new Color32(231, 252, 255, 205);
            AddLine(vertexHelper, center + new Vector2(-radiusX * 0.08f, 0f),
                center + new Vector2(radiusX * 0.08f, 0f), 1.6f, markColor);
            AddLine(vertexHelper, center + new Vector2(0f, -radiusY * 0.12f),
                center + new Vector2(0f, radiusY * 0.12f), 1.6f, markColor);
        }

        private void AddRing(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY,
            Color colorValue, float width)
        {
            const int segments = 128;
            var previous = center + new Vector2(radiusX, 0f);
            for (var index = 1; index <= segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var next = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                AddLine(vertexHelper, previous, next, width, colorValue);
                previous = next;
            }
        }

        private void AddScanLine(VertexHelper vertexHelper, Vector2 center, float radiusX, float radiusY)
        {
            var normalizedY = 1f - scanProgress * 2f;
            var halfWidth = Mathf.Sqrt(Mathf.Max(0f, 1f - normalizedY * normalizedY)) * radiusX;
            var y = center.y + normalizedY * radiusY;
            AddLine(vertexHelper, new Vector2(center.x - halfWidth, y), new Vector2(center.x + halfWidth, y),
                9f, new Color32(134, 244, 255, 110));
            AddLine(vertexHelper, new Vector2(center.x - halfWidth, y), new Vector2(center.x + halfWidth, y),
                2.5f, ScanColor);
        }

        private void AddClippedLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, float cutoff,
            float width, Color colorValue)
        {
            if (start.y < cutoff && end.y < cutoff)
            {
                return;
            }
            if (start.y < cutoff || end.y < cutoff)
            {
                var denominator = end.y - start.y;
                if (Mathf.Abs(denominator) > 0.001f)
                {
                    var t = Mathf.Clamp01((cutoff - start.y) / denominator);
                    var clipped = Vector2.Lerp(start, end, t);
                    if (start.y < cutoff) start = clipped;
                    else end = clipped;
                }
            }
            AddLine(vertexHelper, start, end, width, colorValue);
        }

        private void AddLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, float width, Color colorValue)
        {
            var direction = end - start;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }
            var normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            var baseIndex = vertexHelper.currentVertCount;
            var tinted = Tint(colorValue);
            vertexHelper.AddVert(start - normal, tinted, Vector2.zero);
            vertexHelper.AddVert(start + normal, tinted, Vector2.up);
            vertexHelper.AddVert(end + normal, tinted, Vector2.one);
            vertexHelper.AddVert(end - normal, tinted, Vector2.right);
            vertexHelper.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            vertexHelper.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
        }

        private void AddQuad(VertexHelper vertexHelper, Vector2 min, Vector2 max, Color colorValue)
        {
            var baseIndex = vertexHelper.currentVertCount;
            var tinted = Tint(colorValue);
            vertexHelper.AddVert(new Vector2(min.x, min.y), tinted, Vector2.zero);
            vertexHelper.AddVert(new Vector2(min.x, max.y), tinted, Vector2.up);
            vertexHelper.AddVert(new Vector2(max.x, max.y), tinted, Vector2.one);
            vertexHelper.AddVert(new Vector2(max.x, min.y), tinted, Vector2.right);
            vertexHelper.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            vertexHelper.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
        }

        private void AddTriangle(VertexHelper vertexHelper, Vector2 first, Vector2 second, Vector2 third,
            Color colorValue)
        {
            var baseIndex = vertexHelper.currentVertCount;
            var tinted = Tint(colorValue);
            vertexHelper.AddVert(first, tinted, Vector2.up);
            vertexHelper.AddVert(second, tinted, Vector2.zero);
            vertexHelper.AddVert(third, tinted, Vector2.right);
            vertexHelper.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
        }

        private Color Tint(Color source)
        {
            return new Color(source.r * color.r, source.g * color.g, source.b * color.b, source.a * color.a);
        }
    }
}
