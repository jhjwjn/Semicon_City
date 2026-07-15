using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class SplineCurbBlockGenerator : MonoBehaviour
{
    [Header("Legacy Curb Blocks")]
    [Tooltip("꺼두면 콘크리트 경계석을 만들지 않습니다. 자연형 호수에는 꺼두세요.")]
    public bool generateCurbBlocks = false;

    [Min(0.05f)]
    public float curbWidth = 0.7f;

    [Min(0.02f)]
    public float curbHeight = 0.25f;

    [Min(0.1f)]
    public float blockLength = 1.5f;

    [Min(0f)]
    public float blockGap = 0.05f;

    [Tooltip("Spline 높이를 기준으로 연석을 위아래로 이동합니다.")]
    public float curbHeightOffset = 0.03f;

    [Tooltip("양수면 Spline 오른쪽, 음수면 왼쪽으로 연석을 이동합니다.")]
    public float curbLateralOffset = 0f;

    [Header("Water Surface")]
    [Tooltip("Spline 경계보다 물을 안쪽으로 줄이는 거리입니다.")]
    [Min(0f)]
    public float waterInset = 0.35f;

    [Tooltip("Spline 높이를 기준으로 수면을 위아래로 이동합니다.")]
    public float waterHeightOffset = -0.08f;

    [Header("Natural Lake Edge")]
    public bool generateNaturalBank = true;

    [Tooltip("기존 공원 지반을 덮는 바깥쪽 자연 경계 폭입니다.")]
    [Min(0.05f)]
    public float bankOuterWidth = 1.2f;

    [Tooltip("물가 안쪽으로 들어가는 경계 폭입니다.")]
    [Min(0.05f)]
    public float bankInnerWidth = 0.5f;

    [Tooltip("바깥 경계 높이. 기존 지반보다 아주 살짝 위로 두세요.")]
    public float bankOuterHeightOffset = 0.025f;

    [Tooltip("물가 경계 높이. 수면보다 살짝 낮거나 비슷하게 두세요.")]
    public float bankInnerHeightOffset = -0.11f;

    [Tooltip("호수 내부 기존 초록 지반을 가릴 바닥 마스크 높이입니다.")]
    public float lakeBedHeightOffset = -0.13f;

    [Tooltip("기존 City.002 지반을 실제로 파지 않고, 호수/둔덕 Mesh를 위에 덮어서 자연 호수처럼 보이게 합니다.")]
    public bool coverExistingGround = true;

    [Tooltip("Cover Existing Ground 모드에서 호수 바닥 마스크가 기존 지반보다 올라오는 높이입니다.")]
    public float visibleLakeBedOffset = 0.006f;

    [Tooltip("Cover Existing Ground 모드에서 수면 높이입니다. 바닥보다 높고 바깥 둔덕보다 낮게 두세요.")]
    public float visibleWaterOffset = 0.018f;

    [Tooltip("Cover Existing Ground 모드에서 바깥 둔덕 높이입니다.")]
    public float visibleBankOuterOffset = 0.04f;

    [Tooltip("Cover Existing Ground 모드에서 물가 안쪽 둔덕 높이입니다.")]
    public float visibleBankInnerOffset = 0.022f;

    [Header("Curve Accuracy")]
    [Range(20, 2000)]
    public int sampleCount = 400;

    [Header("Materials")]
    public Material curbMaterial;
    public Material waterMaterial;
    public Material bankMaterial;
    public Material lakeBedMaterial;

    [Header("Collider")]
    [Tooltip("풀 배치에서 호수/연석 영역을 제외할 수 있도록 MeshCollider를 생성합니다.")]
    public bool addMeshColliders = true;

    private const string GeneratedRootName = "Generated_Lake";
    private const string GeneratedCurbName = "Curb_Blocks";
    private const string GeneratedWaterName = "Water_Surface";
    private const string GeneratedBankName = "Lake_Bank";
    private const string GeneratedBedName = "Lake_Bed_Mask";

    private SplineContainer splineContainer;

    private struct PathPoint
    {
        public Vector3 position;
        public Vector3 tangent;

        public PathPoint(Vector3 position, Vector3 tangent)
        {
            this.position = position;
            this.tangent = tangent;
        }
    }

    [ContextMenu("Generate Lake")]
    public void GenerateLake()
    {
        splineContainer = GetComponent<SplineContainer>();

        if (splineContainer == null ||
            splineContainer.Spline == null ||
            splineContainer.Spline.Count < 3)
        {
            Debug.LogError(
                $"{name}: 호수 Spline에는 점이 3개 이상 필요합니다."
            );
            return;
        }

        if (!splineContainer.Spline.Closed)
        {
            Debug.LogError(
                $"{name}: Spline Container의 Closed를 켜주세요."
            );
            return;
        }

        if (generateCurbBlocks && curbMaterial == null)
        {
            Debug.LogError(
                $"{name}: Curb Material을 지정하세요."
            );
            return;
        }

        if (waterMaterial == null)
        {
            Debug.LogError(
                $"{name}: Water Material을 지정하세요."
            );
            return;
        }

        ClearGeneratedLake();

        List<Vector3> sampledPath = SampleClosedSpline();

        if (sampledPath.Count < 3)
        {
            Debug.LogError(
                $"{name}: 닫힌 Spline 경로를 생성하지 못했습니다."
            );
            return;
        }

        GameObject generatedRoot = new GameObject(GeneratedRootName);
        generatedRoot.transform.SetParent(transform, false);
        generatedRoot.transform.localPosition = Vector3.zero;
        generatedRoot.transform.localRotation = Quaternion.identity;
        generatedRoot.transform.localScale = Vector3.one;

        if (generateNaturalBank)
        {
            CreateLakeBedObject(sampledPath, generatedRoot.transform);
            CreateLakeBankObject(sampledPath, generatedRoot.transform);
        }

        if (generateCurbBlocks)
        {
            CreateCurbObject(
                sampledPath,
                generatedRoot.transform
            );
        }

        CreateWaterObject(
            sampledPath,
            generatedRoot.transform
        );

        Debug.Log(
            $"[{name}] 호수 생성 완료 | " +
            $"Natural Bank: {generateNaturalBank}, " +
            $"Curb: {generateCurbBlocks}, " +
            $"Water Inset: {waterInset:F2}"
        );
    }

    [ContextMenu("Apply Natural Lake Preset")]
    public void ApplyNaturalLakePreset()
    {
        generateCurbBlocks = false;
        generateNaturalBank = true;
        coverExistingGround = true;
        waterInset = 0.45f;
        bankOuterWidth = 1.4f;
        bankInnerWidth = 0.65f;
        visibleLakeBedOffset = 0.006f;
        visibleWaterOffset = 0.018f;
        visibleBankOuterOffset = 0.04f;
        visibleBankInnerOffset = 0.022f;
    }

    [ContextMenu("Clear Generated Lake")]
    public void ClearGeneratedLake()
    {
        Transform existing = transform.Find(GeneratedRootName);

        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    private void CreateCurbObject(
        List<Vector3> path,
        Transform parent)
    {
        GameObject curbObject =
            new GameObject(GeneratedCurbName);

        curbObject.transform.SetParent(parent, false);
        curbObject.transform.localPosition = Vector3.zero;
        curbObject.transform.localRotation = Quaternion.identity;
        curbObject.transform.localScale = Vector3.one;

        MeshFilter filter =
            curbObject.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            curbObject.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            BuildCurbMesh(path, curbObject.transform);

        renderer.sharedMaterial = curbMaterial;

        if (addMeshColliders)
        {
            MeshCollider collider =
                curbObject.AddComponent<MeshCollider>();

            collider.sharedMesh = filter.sharedMesh;
        }
    }

    private void CreateWaterObject(
        List<Vector3> path,
        Transform parent)
    {
        GameObject waterObject =
            new GameObject(GeneratedWaterName);

        waterObject.transform.SetParent(parent, false);
        waterObject.transform.localPosition = Vector3.zero;
        waterObject.transform.localRotation = Quaternion.identity;
        waterObject.transform.localScale = Vector3.one;

        MeshFilter filter =
            waterObject.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            waterObject.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            BuildWaterMesh(path, waterObject.transform);

        renderer.sharedMaterial = waterMaterial;

        if (addMeshColliders)
        {
            MeshCollider collider =
                waterObject.AddComponent<MeshCollider>();

            collider.sharedMesh = filter.sharedMesh;
        }
    }

    private void CreateLakeBankObject(
        List<Vector3> path,
        Transform parent)
    {
        GameObject bankObject =
            new GameObject(GeneratedBankName);

        bankObject.transform.SetParent(parent, false);
        bankObject.transform.localPosition = Vector3.zero;
        bankObject.transform.localRotation = Quaternion.identity;
        bankObject.transform.localScale = Vector3.one;

        MeshFilter filter =
            bankObject.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            bankObject.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            BuildLakeBankMesh(path, bankObject.transform);

        renderer.sharedMaterial =
            bankMaterial != null
                ? bankMaterial
                : curbMaterial;

        if (addMeshColliders)
        {
            MeshCollider collider =
                bankObject.AddComponent<MeshCollider>();

            collider.sharedMesh = filter.sharedMesh;
        }
    }

    private void CreateLakeBedObject(
        List<Vector3> path,
        Transform parent)
    {
        GameObject bedObject =
            new GameObject(GeneratedBedName);

        bedObject.transform.SetParent(parent, false);
        bedObject.transform.localPosition = Vector3.zero;
        bedObject.transform.localRotation = Quaternion.identity;
        bedObject.transform.localScale = Vector3.one;

        MeshFilter filter =
            bedObject.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            bedObject.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            BuildLakeBedMesh(path, bedObject.transform);

        renderer.sharedMaterial =
            lakeBedMaterial != null
                ? lakeBedMaterial
                : curbMaterial;
    }

    private List<Vector3> SampleClosedSpline()
    {
        int count = Mathf.Max(20, sampleCount);

        List<Vector3> points =
            new List<Vector3>(count);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;

            float3 value =
                splineContainer.EvaluatePosition(t);

            Vector3 point = new Vector3(
                value.x,
                value.y,
                value.z
            );

            if (points.Count == 0 ||
                Vector3.Distance(
                    points[points.Count - 1],
                    point
                ) > 0.001f)
            {
                points.Add(point);
            }
        }

        return points;
    }

    private Mesh BuildCurbMesh(
        List<Vector3> path,
        Transform generatedTransform)
    {
        List<Vector3> closedPath =
            new List<Vector3>(path);

        closedPath.Add(path[0]);

        List<float> cumulativeDistances =
            BuildCumulativeDistances(closedPath);

        float totalLength =
            cumulativeDistances[
                cumulativeDistances.Count - 1
            ];

        float interval = Mathf.Max(
            0.01f,
            blockLength + blockGap
        );

        int blockCount = Mathf.Max(
            1,
            Mathf.FloorToInt(totalLength / interval)
        );

        List<Vector3> vertices =
            new List<Vector3>(blockCount * 8);

        List<int> triangles =
            new List<int>(blockCount * 36);

        for (int blockIndex = 0;
             blockIndex < blockCount;
             blockIndex++)
        {
            float centerDistance =
                blockIndex * interval +
                blockLength * 0.5f;

            if (centerDistance + blockLength * 0.5f >
                totalLength)
            {
                break;
            }

            PathPoint pathPoint =
                EvaluatePathAtDistance(
                    closedPath,
                    cumulativeDistances,
                    centerDistance
                );

            Vector3 tangent = pathPoint.tangent;
            tangent.y = 0f;

            if (tangent.sqrMagnitude < 0.000001f)
                tangent = Vector3.forward;

            tangent.Normalize();

            Vector3 right =
                Vector3.Cross(
                    Vector3.up,
                    tangent
                ).normalized;

            Vector3 center =
                pathPoint.position +
                right * curbLateralOffset +
                Vector3.up *
                (curbHeightOffset +
                 curbHeight * 0.5f);

            AddBox(
                vertices,
                triangles,
                generatedTransform,
                center,
                tangent,
                right
            );
        }

        Mesh mesh = new Mesh
        {
            name = $"{name}_CurbBlockMesh"
        };

        if (vertices.Count > 65000)
        {
            mesh.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Mesh BuildWaterMesh(
        List<Vector3> path,
        Transform generatedTransform)
    {
        Vector3 centroid = CalculateCentroid(path);

        List<Vector3> worldPoints =
            new List<Vector3>(path.Count);

        List<Vector2> polygon2D =
            new List<Vector2>(path.Count);

        foreach (Vector3 point in path)
        {
            Vector3 directionToCenter =
                centroid - point;

            directionToCenter.y = 0f;

            Vector3 insetPoint = point;

            if (directionToCenter.sqrMagnitude >
                0.000001f)
            {
                insetPoint +=
                    directionToCenter.normalized *
                    waterInset;
            }

            insetPoint.y = point.y + GetWaterHeightOffset();

            worldPoints.Add(insetPoint);

            polygon2D.Add(
                new Vector2(
                    insetPoint.x,
                    insetPoint.z
                )
            );
        }

        List<int> triangles =
            TriangulatePolygon(polygon2D);

        if (triangles.Count == 0)
        {
            Debug.LogError(
                $"{name}: 수면 삼각분할에 실패했습니다. " +
                "Spline이 서로 교차하지 않는지 확인하세요."
            );
        }

        List<Vector3> vertices =
            new List<Vector3>(worldPoints.Count);

        List<Vector2> uvs =
            new List<Vector2>(worldPoints.Count);

        GetBounds2D(
            polygon2D,
            out Vector2 min,
            out Vector2 max
        );

        Vector2 size = max - min;

        if (Mathf.Abs(size.x) < 0.0001f)
            size.x = 1f;

        if (Mathf.Abs(size.y) < 0.0001f)
            size.y = 1f;

        for (int i = 0;
             i < worldPoints.Count;
             i++)
        {
            vertices.Add(
                generatedTransform.InverseTransformPoint(
                    worldPoints[i]
                )
            );

            Vector2 point = polygon2D[i];

            uvs.Add(
                new Vector2(
                    (point.x - min.x) / size.x,
                    (point.y - min.y) / size.y
                )
            );
        }

        Mesh mesh = new Mesh
        {
            name = $"{name}_WaterSurfaceMesh"
        };

        if (vertices.Count > 65000)
        {
            mesh.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Mesh BuildLakeBankMesh(
        List<Vector3> path,
        Transform generatedTransform)
    {
        Vector3 centroid = CalculateCentroid(path);
        List<Vector3> outerPoints = new List<Vector3>(path.Count);
        List<Vector3> innerPoints = new List<Vector3>(path.Count);
        List<Vector3> waterEdgePoints = new List<Vector3>(path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 point = path[i];
            Vector3 directionToCenter = centroid - point;
            directionToCenter.y = 0f;

            if (directionToCenter.sqrMagnitude < 0.000001f)
                directionToCenter = Vector3.forward;

            directionToCenter.Normalize();

            Vector3 outer = point - directionToCenter * bankOuterWidth;
            Vector3 inner = point + directionToCenter * bankInnerWidth;
            Vector3 waterEdge = point + directionToCenter * waterInset;

            outer.y = point.y + GetBankOuterHeightOffset();
            inner.y = point.y + GetBankInnerHeightOffset();
            waterEdge.y = point.y + GetLakeBedHeightOffset();

            outerPoints.Add(outer);
            innerPoints.Add(inner);
            waterEdgePoints.Add(waterEdge);
        }

        List<Vector3> vertices =
            new List<Vector3>(path.Count * 3);

        List<int> triangles =
            new List<int>(path.Count * 12);

        List<Vector2> uvs =
            new List<Vector2>(path.Count * 3);

        for (int i = 0; i < path.Count; i++)
        {
            vertices.Add(generatedTransform.InverseTransformPoint(outerPoints[i]));
            vertices.Add(generatedTransform.InverseTransformPoint(innerPoints[i]));
            vertices.Add(generatedTransform.InverseTransformPoint(waterEdgePoints[i]));

            uvs.Add(new Vector2(0f, i / (float)path.Count));
            uvs.Add(new Vector2(0.6f, i / (float)path.Count));
            uvs.Add(new Vector2(1f, i / (float)path.Count));
        }

        for (int i = 0; i < path.Count; i++)
        {
            int next = (i + 1) % path.Count;
            int index = i * 3;
            int nextIndex = next * 3;

            AddQuadIndices(triangles, index, nextIndex, index + 1, nextIndex + 1);
            AddQuadIndices(triangles, index + 1, nextIndex + 1, index + 2, nextIndex + 2);
        }

        Mesh mesh = new Mesh
        {
            name = $"{name}_LakeBankMesh"
        };

        if (vertices.Count > 65000)
        {
            mesh.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Mesh BuildLakeBedMesh(
        List<Vector3> path,
        Transform generatedTransform)
    {
        Vector3 centroid = CalculateCentroid(path);
        List<Vector3> worldPoints = new List<Vector3>(path.Count);
        List<Vector2> polygon2D = new List<Vector2>(path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 point = path[i];
            Vector3 directionToCenter = centroid - point;
            directionToCenter.y = 0f;

            if (directionToCenter.sqrMagnitude > 0.000001f)
                point += directionToCenter.normalized * waterInset;

            point.y = path[i].y + GetLakeBedHeightOffset();
            worldPoints.Add(point);
            polygon2D.Add(new Vector2(point.x, point.z));
        }

        List<int> triangles =
            TriangulatePolygon(polygon2D);

        List<Vector3> vertices =
            new List<Vector3>(worldPoints.Count);

        List<Vector2> uvs =
            new List<Vector2>(worldPoints.Count);

        GetBounds2D(polygon2D, out Vector2 min, out Vector2 max);
        Vector2 size = max - min;

        if (Mathf.Abs(size.x) < 0.0001f)
            size.x = 1f;

        if (Mathf.Abs(size.y) < 0.0001f)
            size.y = 1f;

        for (int i = 0; i < worldPoints.Count; i++)
        {
            vertices.Add(generatedTransform.InverseTransformPoint(worldPoints[i]));
            Vector2 point = polygon2D[i];
            uvs.Add(new Vector2((point.x - min.x) / size.x, (point.y - min.y) / size.y));
        }

        Mesh mesh = new Mesh
        {
            name = $"{name}_LakeBedMaskMesh"
        };

        if (vertices.Count > 65000)
        {
            mesh.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void AddQuadIndices(
        List<int> triangles,
        int a,
        int b,
        int c,
        int d)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);

        triangles.Add(c);
        triangles.Add(b);
        triangles.Add(d);
    }

    private float GetWaterHeightOffset()
    {
        return coverExistingGround
            ? visibleWaterOffset
            : waterHeightOffset;
    }

    private float GetLakeBedHeightOffset()
    {
        return coverExistingGround
            ? visibleLakeBedOffset
            : lakeBedHeightOffset;
    }

    private float GetBankOuterHeightOffset()
    {
        return coverExistingGround
            ? visibleBankOuterOffset
            : bankOuterHeightOffset;
    }

    private float GetBankInnerHeightOffset()
    {
        return coverExistingGround
            ? visibleBankInnerOffset
            : bankInnerHeightOffset;
    }

    private void AddBox(
        List<Vector3> vertices,
        List<int> triangles,
        Transform generatedTransform,
        Vector3 center,
        Vector3 forward,
        Vector3 right)
    {
        float halfLength =
            blockLength * 0.5f;

        float halfWidth =
            curbWidth * 0.5f;

        float halfHeight =
            curbHeight * 0.5f;

        Vector3 up = Vector3.up;

        Vector3[] corners =
        {
            center
            - forward * halfLength
            - right * halfWidth
            - up * halfHeight,

            center
            + forward * halfLength
            - right * halfWidth
            - up * halfHeight,

            center
            + forward * halfLength
            + right * halfWidth
            - up * halfHeight,

            center
            - forward * halfLength
            + right * halfWidth
            - up * halfHeight,

            center
            - forward * halfLength
            - right * halfWidth
            + up * halfHeight,

            center
            + forward * halfLength
            - right * halfWidth
            + up * halfHeight,

            center
            + forward * halfLength
            + right * halfWidth
            + up * halfHeight,

            center
            - forward * halfLength
            + right * halfWidth
            + up * halfHeight
        };

        int start = vertices.Count;

        foreach (Vector3 corner in corners)
        {
            vertices.Add(
                generatedTransform.InverseTransformPoint(
                    corner
                )
            );
        }

        AddQuad(
            triangles,
            start + 0,
            start + 1,
            start + 2,
            start + 3
        );

        AddQuad(
            triangles,
            start + 4,
            start + 7,
            start + 6,
            start + 5
        );

        AddQuad(
            triangles,
            start + 0,
            start + 4,
            start + 5,
            start + 1
        );

        AddQuad(
            triangles,
            start + 1,
            start + 5,
            start + 6,
            start + 2
        );

        AddQuad(
            triangles,
            start + 2,
            start + 6,
            start + 7,
            start + 3
        );

        AddQuad(
            triangles,
            start + 3,
            start + 7,
            start + 4,
            start + 0
        );
    }

    private static void AddQuad(
        List<int> triangles,
        int a,
        int b,
        int c,
        int d)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);

        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
    }

    private static Vector3 CalculateCentroid(
        List<Vector3> points)
    {
        Vector3 total = Vector3.zero;

        foreach (Vector3 point in points)
            total += point;

        return total /
               Mathf.Max(1, points.Count);
    }

    private static List<float>
        BuildCumulativeDistances(
            List<Vector3> path)
    {
        List<float> distances =
            new List<float>(path.Count)
            {
                0f
            };

        float total = 0f;

        for (int i = 1;
             i < path.Count;
             i++)
        {
            total += Vector3.Distance(
                path[i - 1],
                path[i]
            );

            distances.Add(total);
        }

        return distances;
    }

    private static PathPoint
        EvaluatePathAtDistance(
            List<Vector3> path,
            List<float> cumulativeDistances,
            float targetDistance)
    {
        targetDistance = Mathf.Clamp(
            targetDistance,
            0f,
            cumulativeDistances[
                cumulativeDistances.Count - 1
            ]
        );

        for (int i = 1;
             i < cumulativeDistances.Count;
             i++)
        {
            if (cumulativeDistances[i] <
                targetDistance)
            {
                continue;
            }

            float previousDistance =
                cumulativeDistances[i - 1];

            float sectionLength =
                cumulativeDistances[i] -
                previousDistance;

            float ratio =
                sectionLength <= 0.000001f
                    ? 0f
                    : (targetDistance -
                       previousDistance) /
                      sectionLength;

            Vector3 position =
                Vector3.Lerp(
                    path[i - 1],
                    path[i],
                    ratio
                );

            Vector3 tangent =
                path[i] - path[i - 1];

            return new PathPoint(
                position,
                tangent
            );
        }

        return new PathPoint(
            path[path.Count - 1],
            path[path.Count - 1] -
            path[path.Count - 2]
        );
    }

    private static List<int>
        TriangulatePolygon(
            List<Vector2> polygon)
    {
        List<int> result =
            new List<int>();

        if (polygon.Count < 3)
            return result;

        List<int> indices =
            new List<int>(polygon.Count);

        bool clockwise =
            SignedArea(polygon) < 0f;

        if (clockwise)
        {
            for (int i = 0;
                 i < polygon.Count;
                 i++)
            {
                indices.Add(i);
            }
        }
        else
        {
            for (int i = polygon.Count - 1;
                 i >= 0;
                 i--)
            {
                indices.Add(i);
            }
        }

        int guard = 0;

        while (indices.Count > 3 &&
               guard <
               polygon.Count *
               polygon.Count)
        {
            bool earFound = false;

            for (int i = 0;
                 i < indices.Count;
                 i++)
            {
                int previous =
                    indices[
                        (i - 1 +
                         indices.Count) %
                        indices.Count
                    ];

                int current =
                    indices[i];

                int next =
                    indices[
                        (i + 1) %
                        indices.Count
                    ];

                Vector2 a =
                    polygon[previous];

                Vector2 b =
                    polygon[current];

                Vector2 c =
                    polygon[next];

                if (Cross(
                        b - a,
                        c - b
                    ) >= 0f)
                {
                    continue;
                }

                bool containsPoint = false;

                for (int j = 0;
                     j < indices.Count;
                     j++)
                {
                    int test =
                        indices[j];

                    if (test == previous ||
                        test == current ||
                        test == next)
                    {
                        continue;
                    }

                    if (PointInTriangle(
                            polygon[test],
                            a,
                            b,
                            c
                        ))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (containsPoint)
                    continue;

                result.Add(previous);
                result.Add(current);
                result.Add(next);

                indices.RemoveAt(i);

                earFound = true;
                break;
            }

            if (!earFound)
                break;

            guard++;
        }

        if (indices.Count == 3)
        {
            result.Add(indices[0]);
            result.Add(indices[1]);
            result.Add(indices[2]);
        }

        return result;
    }

    private static float SignedArea(
        List<Vector2> polygon)
    {
        float area = 0f;

        for (int i = 0;
             i < polygon.Count;
             i++)
        {
            Vector2 current =
                polygon[i];

            Vector2 next =
                polygon[
                    (i + 1) %
                    polygon.Count
                ];

            area +=
                current.x * next.y -
                next.x * current.y;
        }

        return area * 0.5f;
    }

    private static float Cross(
        Vector2 a,
        Vector2 b)
    {
        return
            a.x * b.y -
            a.y * b.x;
    }

    private static bool PointInTriangle(
        Vector2 point,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        float c1 =
            Cross(
                b - a,
                point - a
            );

        float c2 =
            Cross(
                c - b,
                point - b
            );

        float c3 =
            Cross(
                a - c,
                point - c
            );

        bool hasNegative =
            c1 < 0f ||
            c2 < 0f ||
            c3 < 0f;

        bool hasPositive =
            c1 > 0f ||
            c2 > 0f ||
            c3 > 0f;

        return !(hasNegative &&
                 hasPositive);
    }

    private static void GetBounds2D(
        List<Vector2> points,
        out Vector2 min,
        out Vector2 max)
    {
        min = points[0];
        max = points[0];

        for (int i = 1;
             i < points.Count;
             i++)
        {
            min = Vector2.Min(
                min,
                points[i]
            );

            max = Vector2.Max(
                max,
                points[i]
            );
        }
    }
}
