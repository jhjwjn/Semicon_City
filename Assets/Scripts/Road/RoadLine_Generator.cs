using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class SplineLineStripGenerator : MonoBehaviour
{
    [Header("Line Settings")]
    [Tooltip("차선 띠의 전체 너비")]
    [Min(0.01f)]
    public float lineWidth = 0.2f;

    [Tooltip("Spline보다 위에 띄울 높이. 바닥에 묻히면 올리세요.")]
    public float heightOffset = 0.05f;

    [Tooltip("굴곡을 표현하는 분할 수. 곡선이 각져 보이면 높이세요.")]
    [Range(2, 500)]
    public int sampleCount = 80;

    [Tooltip("곡선과 생성 면 사이에 허용할 최대 오차입니다. 값이 작을수록 굴곡을 더 세밀하게 따라갑니다.")]
    [Min(0.001f)]
    public float curveTolerance = 0.03f;

    [Tooltip("급한 코너에서 자동으로 세분화할 최대 단계입니다.")]
    [Range(1, 12)]
    public int maxSubdivisionDepth = 8;

    [Header("Material")]
    public Material lineMaterial;

    [Header("Optional")]
    [Tooltip("생성된 띠에 Mesh Collider를 추가합니다. 차선에는 보통 필요 없습니다.")]
    public bool addMeshCollider = false;

    private const string GeneratedObjectName = "Generated_Line";

    private SplineContainer splineContainer;

    [ContextMenu("Generate Line Strip")]
    public void GenerateLineStrip()
    {
        splineContainer = GetComponent<SplineContainer>();

        if (splineContainer == null)
        {
            Debug.LogError($"{name}: SplineContainer가 없습니다.");
            return;
        }

        if (splineContainer.Spline == null ||
            splineContainer.Spline.Count < 2)
        {
            Debug.LogError($"{name}: Spline 점이 2개 이상 필요합니다.");
            return;
        }

        if (lineMaterial == null)
        {
            Debug.LogError(
                $"{name}: Line Material이 비어 있습니다. " +
                "흰색 또는 노란색 Material을 넣어주세요."
            );
            return;
        }

        ClearGeneratedLine();

        GameObject generatedObject = new GameObject(GeneratedObjectName);
        generatedObject.transform.SetParent(transform, false);
        generatedObject.transform.localPosition = Vector3.zero;
        generatedObject.transform.localRotation = Quaternion.identity;
        generatedObject.transform.localScale = Vector3.one;

        Mesh mesh = BuildStripMesh(generatedObject.transform);

        MeshFilter meshFilter =
            generatedObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            generatedObject.AddComponent<MeshRenderer>();

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = lineMaterial;

        if (addMeshCollider)
        {
            MeshCollider collider =
                generatedObject.AddComponent<MeshCollider>();

            collider.sharedMesh = mesh;
        }

        Debug.Log(
            $"[{name}] 차선 띠 생성 완료 | " +
            $"Width: {lineWidth:F3}, Samples: {sampleCount}"
        );
    }

    [ContextMenu("Clear Generated Line")]
    public void ClearGeneratedLine()
    {
        Transform existing = transform.Find(GeneratedObjectName);

        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    private Mesh BuildStripMesh(Transform generatedTransform)
    {
        List<Vector3> centerPoints = BuildAdaptiveCenterline();

        if (centerPoints.Count < 2)
        {
            Debug.LogError($"{name}: Spline 샘플 점을 충분히 만들지 못했습니다.");
            return new Mesh { name = $"{name}_EmptyLineStripMesh" };
        }

        List<Vector3> vertices = new List<Vector3>(centerPoints.Count * 2);
        List<int> triangles = new List<int>((centerPoints.Count - 1) * 6);
        List<Vector2> uvs = new List<Vector2>(centerPoints.Count * 2);

        float halfWidth = lineWidth * 0.5f;
        float accumulatedDistance = 0f;
        Vector3 previousRight = Vector3.zero;

        for (int i = 0; i < centerPoints.Count; i++)
        {
            Vector3 worldPosition = centerPoints[i] + Vector3.up * heightOffset;

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

            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

            if (previousRight != Vector3.zero && Vector3.Dot(previousRight, right) < 0f)
                right = -right;

            previousRight = right;

            Vector3 leftWorld = worldPosition - right * halfWidth;
            Vector3 rightWorld = worldPosition + right * halfWidth;

            vertices.Add(generatedTransform.InverseTransformPoint(leftWorld));
            vertices.Add(generatedTransform.InverseTransformPoint(rightWorld));

            if (i > 0)
                accumulatedDistance += Vector3.Distance(centerPoints[i - 1], centerPoints[i]);

            uvs.Add(new Vector2(0f, accumulatedDistance));
            uvs.Add(new Vector2(1f, accumulatedDistance));

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
            name = $"{name}_LineStripMesh"
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
    private List<Vector3> BuildAdaptiveCenterline()
    {
        List<Vector3> points = new List<Vector3>();
        int baseSegments = Mathf.Max(2, sampleCount);

        Vector3 first = EvaluateWorldPosition(0f);
        points.Add(first);

        for (int i = 0; i < baseSegments; i++)
        {
            float t0 = i / (float)baseSegments;
            float t1 = (i + 1) / (float)baseSegments;

            Vector3 p0 = i == 0 ? first : EvaluateWorldPosition(t0);
            Vector3 p1 = EvaluateWorldPosition(t1);

            SubdivideCurve(points, t0, p0, t1, p1, 0);
        }

        return RemoveNearDuplicatePoints(points);
    }

    private void SubdivideCurve(
        List<Vector3> output,
        float t0,
        Vector3 p0,
        float t1,
        Vector3 p1,
        int depth)
    {
        float tm = (t0 + t1) * 0.5f;
        Vector3 pm = EvaluateWorldPosition(tm);
        Vector3 chordMidpoint = (p0 + p1) * 0.5f;

        float deviation = Vector3.Distance(pm, chordMidpoint);

        Vector3 firstDirection = pm - p0;
        Vector3 secondDirection = p1 - pm;

        float angle = 0f;
        if (firstDirection.sqrMagnitude > 0.000001f && secondDirection.sqrMagnitude > 0.000001f)
            angle = Vector3.Angle(firstDirection, secondDirection);

        bool requiresSubdivision =
            deviation > curveTolerance ||
            angle > 3f;

        if (requiresSubdivision && depth < maxSubdivisionDepth)
        {
            SubdivideCurve(output, t0, p0, tm, pm, depth + 1);
            SubdivideCurve(output, tm, pm, t1, p1, depth + 1);
            return;
        }

        output.Add(p1);
    }

    private Vector3 EvaluateWorldPosition(float t)
    {
        float3 value = splineContainer.EvaluatePosition(Mathf.Clamp01(t));
        return new Vector3(value.x, value.y, value.z);
    }

    private List<Vector3> RemoveNearDuplicatePoints(List<Vector3> source)
    {
        List<Vector3> cleaned = new List<Vector3>();

        foreach (Vector3 point in source)
        {
            if (cleaned.Count == 0 || Vector3.Distance(cleaned[cleaned.Count - 1], point) > 0.001f)
                cleaned.Add(point);
        }

        return cleaned;
    }

    private Vector3 CalculateFallbackTangent(float t)
    {
        float delta = 0.001f;

        float previousT = Mathf.Clamp01(t - delta);
        float nextT = Mathf.Clamp01(t + delta);

        float3 previousValue =
            splineContainer.EvaluatePosition(previousT);

        float3 nextValue =
            splineContainer.EvaluatePosition(nextT);

        Vector3 previous = new Vector3(
            previousValue.x,
            previousValue.y,
            previousValue.z
        );

        Vector3 next = new Vector3(
            nextValue.x,
            nextValue.y,
            nextValue.z
        );

        Vector3 tangent = next - previous;
        tangent.y = 0f;

        if (tangent.sqrMagnitude < 0.000001f)
            tangent = Vector3.forward;

        return tangent;
    }
}