using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class SplineWalkwayGenerator : MonoBehaviour
{
    [Header("Natural Walkway")]
    [Min(0.1f)]
    public float pathWidth = 1.5f;

    [Tooltip("기존 지반보다 살짝 위로 띄울 높이입니다.")]
    public float heightOffset = 0.045f;

    [Tooltip("길 양쪽 가장자리를 살짝 울퉁불퉁하게 만듭니다.")]
    [Range(0f, 0.35f)]
    public float edgeJitter = 0.08f;

    [Tooltip("생성해도 같은 가장자리 형태가 나오게 하는 값입니다.")]
    public int randomSeed = 4321;

    [Header("Texture")]
    [Tooltip("흙길/자갈길 텍스처가 몇 미터마다 반복될지 설정합니다.")]
    [Min(0.05f)]
    public float textureTileLength = 1.5f;

    [Header("Curve Accuracy")]
    [Range(10, 1000)]
    public int sampleCount = 240;

    [Min(0.001f)]
    public float curveTolerance = 0.03f;

    [Range(1, 12)]
    public int maxSubdivisionDepth = 8;

    [Header("Material")]
    [Tooltip("흙길/자갈길 Material을 지정하세요.")]
    public Material walkwayMaterial;

    [Header("Collider")]
    [Tooltip("풀/식생 배치에서 길 영역을 제외할 수 있도록 MeshCollider를 생성합니다.")]
    public bool addMeshCollider = true;

    private const string GeneratedName = "Generated_Walkway";
    private SplineContainer splineContainer;

    [ContextMenu("Generate Walkway")]
    public void GenerateWalkway()
    {
        splineContainer = GetComponent<SplineContainer>();

        if (splineContainer == null ||
            splineContainer.Splines == null ||
            splineContainer.Splines.Count == 0)
        {
            Debug.LogError($"{name}: Spline 점이 2개 이상 필요합니다.");
            return;
        }

        if (walkwayMaterial == null)
        {
            Debug.LogError($"{name}: Walkway Material을 지정하세요.");
            return;
        }

        ClearGeneratedWalkway();

        GameObject generated = new GameObject(GeneratedName);
        generated.transform.SetParent(transform, false);
        generated.transform.localPosition = Vector3.zero;
        generated.transform.localRotation = Quaternion.identity;
        generated.transform.localScale = Vector3.one;

        int generatedCount = 0;

        for (int splineIndex = 0; splineIndex < splineContainer.Splines.Count; splineIndex++)
        {
            Spline spline = splineContainer.Splines[splineIndex];

            if (spline == null || spline.Count < 2)
                continue;

            GameObject path = new GameObject($"Path_{splineIndex:00}");
            path.transform.SetParent(generated.transform, false);
            path.transform.localPosition = Vector3.zero;
            path.transform.localRotation = Quaternion.identity;
            path.transform.localScale = Vector3.one;

            Mesh mesh = BuildNaturalPathMesh(path.transform, splineIndex);

            MeshFilter filter = path.AddComponent<MeshFilter>();
            MeshRenderer renderer = path.AddComponent<MeshRenderer>();

            filter.sharedMesh = mesh;
            renderer.sharedMaterial = walkwayMaterial;

            if (addMeshCollider)
            {
                MeshCollider collider = path.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            generatedCount++;
        }

        if (generatedCount == 0)
        {
            Debug.LogError($"{name}: 점이 2개 이상인 Spline을 찾지 못했습니다.");
            ClearGeneratedWalkway();
            return;
        }

        Debug.Log(
            $"[{name}] 자연형 순환로 생성 완료 | Splines: {generatedCount}, Width: {pathWidth:F2}"
        );
    }

    [ContextMenu("Clear Generated Walkway")]
    public void ClearGeneratedWalkway()
    {
        Transform existing = transform.Find(GeneratedName);

        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    private Mesh BuildNaturalPathMesh(Transform generatedTransform, int splineIndex)
    {
        List<Vector3> centerPoints = BuildAdaptiveCenterline(splineIndex);

        if (centerPoints.Count < 2)
        {
            Debug.LogError($"{name}: Spline {splineIndex} 샘플 점을 충분히 만들지 못했습니다.");
            return new Mesh { name = $"{name}_Spline{splineIndex}_EmptyNaturalPathMesh" };
        }

        List<Vector3> vertices = new List<Vector3>(centerPoints.Count * 2);
        List<int> triangles = new List<int>((centerPoints.Count - 1) * 6);
        List<Vector2> uvs = new List<Vector2>(centerPoints.Count * 2);

        float halfWidth = pathWidth * 0.5f;
        float accumulatedDistance = 0f;
        Vector3 previousRight = Vector3.zero;
        System.Random random = new System.Random(randomSeed + splineIndex * 9973);

        for (int i = 0; i < centerPoints.Count; i++)
        {
            Vector3 center =
                centerPoints[i] + Vector3.up * heightOffset;

            Vector3 tangent;

            if (i == 0)
                tangent = centerPoints[1] - centerPoints[0];
            else if (i == centerPoints.Count - 1)
                tangent = centerPoints[i] - centerPoints[i - 1];
            else
                tangent = centerPoints[i + 1] - centerPoints[i - 1];

            tangent.y = 0f;

            if (tangent.sqrMagnitude < 0.000001f)
                tangent = Vector3.forward;

            tangent.Normalize();

            Vector3 right =
                Vector3.Cross(Vector3.up, tangent).normalized;

            if (previousRight != Vector3.zero &&
                Vector3.Dot(previousRight, right) < 0f)
            {
                right = -right;
            }

            previousRight = right;

            float leftJitter =
                RandomRange(random, -edgeJitter, edgeJitter);

            float rightJitter =
                RandomRange(random, -edgeJitter, edgeJitter);

            Vector3 leftPoint =
                center - right * Mathf.Max(0.05f, halfWidth + leftJitter);

            Vector3 rightPoint =
                center + right * Mathf.Max(0.05f, halfWidth + rightJitter);

            vertices.Add(generatedTransform.InverseTransformPoint(leftPoint));
            vertices.Add(generatedTransform.InverseTransformPoint(rightPoint));

            if (i > 0)
                accumulatedDistance += Vector3.Distance(centerPoints[i - 1], centerPoints[i]);

            float v = accumulatedDistance / Mathf.Max(0.01f, textureTileLength);
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));

            if (i < centerPoints.Count - 1)
            {
                int index = i * 2;

                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 1);

                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index + 3);
            }
        }

        Mesh mesh = new Mesh
        {
            name = $"{name}_Spline{splineIndex}_NaturalPathMesh"
        };

        if (vertices.Count > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private List<Vector3> BuildAdaptiveCenterline(int splineIndex)
    {
        List<Vector3> points = new List<Vector3>();
        int baseSegments = Mathf.Max(10, sampleCount);

        Vector3 first = EvaluateWorldPosition(splineIndex, 0f);
        points.Add(first);

        for (int i = 0; i < baseSegments; i++)
        {
            float t0 = i / (float)baseSegments;
            float t1 = (i + 1) / (float)baseSegments;

            Vector3 p0 = i == 0
                ? first
                : EvaluateWorldPosition(splineIndex, t0);

            Vector3 p1 = EvaluateWorldPosition(splineIndex, t1);

            SubdivideCurve(points, splineIndex, t0, p0, t1, p1, 0);
        }

        return RemoveDuplicatePoints(points);
    }

    private void SubdivideCurve(
        List<Vector3> output,
        int splineIndex,
        float t0,
        Vector3 p0,
        float t1,
        Vector3 p1,
        int depth)
    {
        float middleT = (t0 + t1) * 0.5f;
        Vector3 middlePoint = EvaluateWorldPosition(splineIndex, middleT);
        Vector3 straightMiddle = (p0 + p1) * 0.5f;

        float deviation = Vector3.Distance(
            middlePoint,
            straightMiddle
        );

        Vector3 firstDirection = middlePoint - p0;
        Vector3 secondDirection = p1 - middlePoint;
        float angle = 0f;

        if (firstDirection.sqrMagnitude > 0.000001f &&
            secondDirection.sqrMagnitude > 0.000001f)
        {
            angle = Vector3.Angle(firstDirection, secondDirection);
        }

        bool subdivide =
            deviation > curveTolerance ||
            angle > 3f;

        if (subdivide && depth < maxSubdivisionDepth)
        {
            SubdivideCurve(output, splineIndex, t0, p0, middleT, middlePoint, depth + 1);
            SubdivideCurve(output, splineIndex, middleT, middlePoint, t1, p1, depth + 1);
            return;
        }

        output.Add(p1);
    }

    private Vector3 EvaluateWorldPosition(int splineIndex, float t)
    {
        float3 value =
            splineContainer.EvaluatePosition(splineIndex, Mathf.Clamp01(t));

        return new Vector3(value.x, value.y, value.z);
    }

    private static List<Vector3> RemoveDuplicatePoints(
        List<Vector3> source)
    {
        List<Vector3> cleaned = new List<Vector3>();

        foreach (Vector3 point in source)
        {
            if (cleaned.Count == 0 ||
                Vector3.Distance(cleaned[cleaned.Count - 1], point) > 0.001f)
            {
                cleaned.Add(point);
            }
        }

        return cleaned;
    }

    private float RandomRange(System.Random random, float min, float max)
    {
        if (Mathf.Approximately(min, max))
            return min;

        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
