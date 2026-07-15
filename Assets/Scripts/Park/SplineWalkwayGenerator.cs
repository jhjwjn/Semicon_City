using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class SplineWalkwayGenerator : MonoBehaviour
{
    public enum WalkwayBuildMode
    {
        BrickBlocks,
        TexturedRibbon
    }

    [Header("Walkway")]
    public WalkwayBuildMode buildMode = WalkwayBuildMode.BrickBlocks;

    [Min(0.1f)]
    public float pathWidth = 3f;

    public float heightOffset = 0.04f;

    [Header("Concrete Base")]
    [Tooltip("벽돌 사이 틈으로 보이는 회색 콘크리트 받침을 생성합니다.")]
    public bool generateConcreteBase = true;

    [Tooltip("비워두면 Walkway Material을 복제해서 concreteBaseColor를 입힙니다.")]
    public Material concreteBaseMaterial;

    public Color concreteBaseColor = new Color(0.42f, 0.40f, 0.36f, 1f);

    [Tooltip("벽돌보다 바닥을 좌우로 조금 더 넓게 만들 거리입니다.")]
    [Min(0f)]
    public float concreteBaseExtraWidth = 0.08f;

    [Tooltip("콘크리트 받침 두께입니다.")]
    [Min(0.001f)]
    public float concreteBaseThickness = 0.05f;

    [Tooltip("벽돌 밑면보다 콘크리트 윗면을 살짝 낮춰 z-fighting을 피합니다.")]
    [Min(0f)]
    public float concreteBaseSurfaceInset = 0.003f;

    [Tooltip("풀 배치에서 보도 영역을 제외할 수 있도록 MeshCollider를 생성합니다.")]
    public bool addMeshColliders = true;

    [Header("Brick Blocks")]
    [Min(0.05f)]
    public float brickLength = 0.42f;

    [Min(0.05f)]
    public float brickWidth = 0.21f;

    [Min(0.005f)]
    public float brickHeight = 0.035f;

    [Tooltip("켜면 heightOffset 위치가 벽돌 윗면 기준이 됩니다. 바닥과 겹침을 줄이려면 끄세요.")]
    public bool alignBrickTopToHeightOffset = true;

    [Min(0f)]
    public float brickGap = 0.018f;

    [Tooltip("한 줄씩 반 칸 밀어서 실제 보도블럭 같은 엇갈림 패턴을 만듭니다.")]
    public bool staggerRows = true;

    [Tooltip("벽돌 위치/색상에 적용할 작은 무작위 변화입니다.")]
    [Range(0f, 0.08f)]
    public float positionJitter = 0.015f;

    [Tooltip("벽돌마다 높이를 살짝 다르게 만들어 너무 평평해 보이지 않게 합니다.")]
    [Range(0f, 0.02f)]
    public float heightJitter = 0.004f;

    [Tooltip("같은 Spline에서 항상 같은 무작위 패턴을 얻기 위한 값입니다.")]
    public int randomSeed = 12345;

    [Header("Brick Colors")]
    public Color brickBrown = new Color(0.48f, 0.25f, 0.12f, 1f);
    public Color brickDarkBrown = new Color(0.28f, 0.12f, 0.06f, 1f);
    public Color brickLightBrown = new Color(0.68f, 0.42f, 0.22f, 1f);

    [Tooltip("각 벽돌 색상에 추가로 섞는 밝기 변화입니다.")]
    [Range(0f, 0.4f)]
    public float colorVariation = 0.14f;

    [Header("Curve Accuracy")]
    [Range(10, 1000)]
    public int sampleCount = 200;

    [Min(0.001f)]
    public float curveTolerance = 0.03f;

    [Range(1, 12)]
    public int maxSubdivisionDepth = 8;

    [Header("Texture Ribbon")]
    [Tooltip("TexturedRibbon 모드에서 보도블록 텍스처가 몇 미터마다 반복될지 설정합니다.")]
    [Min(0.05f)]
    public float textureTileLength = 1f;

    [Header("Material")]
    [Tooltip("TexturedRibbon 모드 또는 벽돌 색상 Material의 기준 Material입니다.")]
    public Material walkwayMaterial;

    private const string GeneratedName = "Generated_Walkway";
    private const string ConcreteBaseName = "Concrete_Base";
    private const string BrickMeshNamePrefix = "Brick_Color_";
    private SplineContainer splineContainer;

    private struct PathSample
    {
        public Vector3 position;
        public Vector3 tangent;

        public PathSample(Vector3 position, Vector3 tangent)
        {
            this.position = position;
            this.tangent = tangent;
        }
    }

    private sealed class BrickMeshData
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> triangles = new List<int>();
        public readonly List<Vector2> uvs = new List<Vector2>();
    }

    [ContextMenu("Generate Walkway")]
    public void GenerateWalkway()
    {
        splineContainer = GetComponent<SplineContainer>();

        if (splineContainer == null ||
            splineContainer.Spline == null ||
            splineContainer.Spline.Count < 2)
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

        if (buildMode == WalkwayBuildMode.BrickBlocks)
        {
            BuildBrickWalkway(generated.transform);
        }
        else
        {
            Mesh mesh = BuildRibbonWalkwayMesh(generated.transform);

            MeshFilter filter = generated.AddComponent<MeshFilter>();
            MeshRenderer renderer = generated.AddComponent<MeshRenderer>();

            filter.sharedMesh = mesh;
            renderer.sharedMaterial = walkwayMaterial;

            if (addMeshColliders)
            {
                MeshCollider collider =
                    generated.AddComponent<MeshCollider>();

                collider.sharedMesh = mesh;
            }
        }

        Debug.Log(
            $"[{name}] 산책로 생성 완료 | Mode: {buildMode}, Width: {pathWidth:F2}"
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

    private void BuildBrickWalkway(Transform generatedRoot)
    {
        List<Vector3> centerPoints = BuildAdaptiveCenterline();

        if (centerPoints.Count < 2)
        {
            Debug.LogError($"{name}: Spline 샘플 점을 충분히 만들지 못했습니다.");
            return;
        }

        List<float> cumulativeDistances = BuildCumulativeDistances(centerPoints);
        float totalLength = cumulativeDistances[cumulativeDistances.Count - 1];

        if (generateConcreteBase)
        {
            CreateConcreteBaseObject(centerPoints, generatedRoot);
        }

        float lengthStep = Mathf.Max(0.01f, brickLength + brickGap);
        float widthStep = Mathf.Max(0.01f, brickWidth + brickGap);
        int columnCount = Mathf.Max(1, Mathf.FloorToInt(totalLength / lengthStep));
        int rowCount = Mathf.Max(1, Mathf.FloorToInt((pathWidth + brickGap) / widthStep));
        float usableWidth = rowCount * brickWidth + (rowCount - 1) * brickGap;
        float firstRowOffset = -usableWidth * 0.5f + brickWidth * 0.5f;

        BrickMeshData[] meshData = CreateBrickMeshData(9);

        System.Random random = new System.Random(randomSeed);

        for (int row = 0; row < rowCount; row++)
        {
            float lateralOffset = firstRowOffset + row * widthStep;
            float rowDistanceOffset =
                staggerRows && row % 2 == 1
                    ? lengthStep * 0.5f
                    : 0f;

            for (int column = 0; column < columnCount; column++)
            {
                float distance =
                    column * lengthStep + brickLength * 0.5f + rowDistanceOffset;

                if (distance + brickLength * 0.5f > totalLength)
                    continue;

                PathSample sample =
                    EvaluateByDistance(centerPoints, cumulativeDistances, distance);

                Vector3 tangent = sample.tangent;
                tangent.y = 0f;

                if (tangent.sqrMagnitude < 0.000001f)
                    tangent = Vector3.forward;

                tangent.Normalize();

                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
                float alongJitter = RandomRange(random, -positionJitter, positionJitter);
                float sideJitter = RandomRange(random, -positionJitter, positionJitter);
                float currentHeight =
                    Mathf.Max(0.001f, brickHeight + RandomRange(random, -heightJitter, heightJitter));

                Vector3 center =
                    sample.position +
                    tangent * alongJitter +
                    right * (lateralOffset + sideJitter) +
                    Vector3.up * GetBrickBaseHeightOffset(currentHeight);

                int colorIndex = PickBrickColorIndex(random);
                int shadeIndex = PickBrickShadeIndex(random);
                int materialIndex = colorIndex * 3 + shadeIndex;

                AddBrick(
                    meshData[materialIndex],
                    generatedRoot,
                    center,
                    tangent,
                    right,
                    brickLength,
                    brickWidth,
                    currentHeight
                );
            }
        }

        Material[] materials = CreateBrickMaterials();

        for (int i = 0; i < meshData.Length; i++)
        {
            if (meshData[i].vertices.Count == 0)
                continue;

            GameObject brickObject =
                new GameObject($"{BrickMeshNamePrefix}{i + 1}");

            brickObject.transform.SetParent(generatedRoot, false);
            brickObject.transform.localPosition = Vector3.zero;
            brickObject.transform.localRotation = Quaternion.identity;
            brickObject.transform.localScale = Vector3.one;

            Mesh mesh = new Mesh
            {
                name = $"{name}_BrickWalkwayMesh_{i + 1}"
            };

            if (meshData[i].vertices.Count > 65000)
            {
                mesh.indexFormat =
                    UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(meshData[i].vertices);
            mesh.SetTriangles(meshData[i].triangles, 0);
            mesh.SetUVs(0, meshData[i].uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = brickObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = brickObject.AddComponent<MeshRenderer>();

            filter.sharedMesh = mesh;
            renderer.sharedMaterial = materials[i];

            if (addMeshColliders)
            {
                MeshCollider collider =
                    brickObject.AddComponent<MeshCollider>();

                collider.sharedMesh = mesh;
            }
        }
    }

    private void CreateConcreteBaseObject(
        List<Vector3> centerPoints,
        Transform parent)
    {
        GameObject baseObject = new GameObject(ConcreteBaseName);
        baseObject.transform.SetParent(parent, false);
        baseObject.transform.localPosition = Vector3.zero;
        baseObject.transform.localRotation = Quaternion.identity;
        baseObject.transform.localScale = Vector3.one;

        MeshFilter filter = baseObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = baseObject.AddComponent<MeshRenderer>();

        filter.sharedMesh =
            BuildConcreteBaseMesh(centerPoints, baseObject.transform);

        renderer.sharedMaterial = CreateConcreteBaseMaterial();

        if (addMeshColliders)
        {
            MeshCollider collider =
                baseObject.AddComponent<MeshCollider>();

            collider.sharedMesh = filter.sharedMesh;
        }
    }

    private Mesh BuildConcreteBaseMesh(
        List<Vector3> centerPoints,
        Transform generatedTransform)
    {
        int pointCount = centerPoints.Count;
        List<Vector3> vertices = new List<Vector3>(pointCount * 4);
        List<int> triangles = new List<int>((pointCount - 1) * 24);
        List<Vector2> uvs = new List<Vector2>(pointCount * 4);

        float halfWidth = pathWidth * 0.5f + concreteBaseExtraWidth;
        float topOffset = GetConcreteBaseTopHeightOffset();
        float bottomOffset = topOffset - concreteBaseThickness;
        float accumulatedDistance = 0f;
        Vector3 previousRight = Vector3.zero;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 tangent;

            if (i == 0)
                tangent = centerPoints[1] - centerPoints[0];
            else if (i == pointCount - 1)
                tangent = centerPoints[i] - centerPoints[i - 1];
            else
                tangent = centerPoints[i + 1] - centerPoints[i - 1];

            tangent.y = 0f;

            if (tangent.sqrMagnitude < 0.000001f)
                tangent = Vector3.forward;

            tangent.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

            if (previousRight != Vector3.zero &&
                Vector3.Dot(previousRight, right) < 0f)
            {
                right = -right;
            }

            previousRight = right;

            Vector3 topCenter = centerPoints[i] + Vector3.up * topOffset;
            Vector3 bottomCenter = centerPoints[i] + Vector3.up * bottomOffset;
            Vector3 topLeft = topCenter - right * halfWidth;
            Vector3 topRight = topCenter + right * halfWidth;
            Vector3 bottomLeft = bottomCenter - right * halfWidth;
            Vector3 bottomRight = bottomCenter + right * halfWidth;

            vertices.Add(generatedTransform.InverseTransformPoint(topLeft));
            vertices.Add(generatedTransform.InverseTransformPoint(topRight));
            vertices.Add(generatedTransform.InverseTransformPoint(bottomLeft));
            vertices.Add(generatedTransform.InverseTransformPoint(bottomRight));

            if (i > 0)
                accumulatedDistance += Vector3.Distance(centerPoints[i - 1], centerPoints[i]);

            float v = accumulatedDistance / Mathf.Max(0.01f, brickLength);
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));

            if (i < pointCount - 1)
            {
                int index = i * 4;
                int next = index + 4;

                AddQuadIndices(triangles, index, next, index + 1, next + 1);
                AddQuadIndices(triangles, index + 2, index + 3, next + 2, next + 3);
                AddQuadIndices(triangles, index, index + 2, next, next + 2);
                AddQuadIndices(triangles, index + 1, next + 1, index + 3, next + 3);
            }
        }

        AddQuadIndices(triangles, 0, 1, 2, 3);

        int last = (pointCount - 1) * 4;
        AddQuadIndices(triangles, last, last + 2, last + 1, last + 3);

        Mesh mesh = new Mesh
        {
            name = $"{name}_ConcreteBaseMesh"
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

    private Mesh BuildRibbonWalkwayMesh(Transform generatedTransform)
    {
        List<Vector3> centerPoints = BuildAdaptiveCenterline();

        List<Vector3> vertices =
            new List<Vector3>(centerPoints.Count * 2);

        List<int> triangles =
            new List<int>((centerPoints.Count - 1) * 6);

        List<Vector2> uvs =
            new List<Vector2>(centerPoints.Count * 2);

        float halfWidth = pathWidth * 0.5f;
        float accumulatedDistance = 0f;
        Vector3 previousRight = Vector3.zero;

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

            Vector3 leftPoint =
                center - right * halfWidth;

            Vector3 rightPoint =
                center + right * halfWidth;

            vertices.Add(
                generatedTransform.InverseTransformPoint(leftPoint)
            );

            vertices.Add(
                generatedTransform.InverseTransformPoint(rightPoint)
            );

            if (i > 0)
            {
                accumulatedDistance += Vector3.Distance(
                    centerPoints[i - 1],
                    centerPoints[i]
                );
            }

            float v = accumulatedDistance /
                      Mathf.Max(0.01f, textureTileLength);

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
            name = $"{name}_WalkwayMesh"
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

    private BrickMeshData[] CreateBrickMeshData(int count)
    {
        BrickMeshData[] data = new BrickMeshData[count];

        for (int i = 0; i < count; i++)
        {
            data[i] = new BrickMeshData();
        }

        return data;
    }

    private Material[] CreateBrickMaterials()
    {
        return new[]
        {
            CreateTintedMaterial(brickBrown, "Brown_Dark", -colorVariation),
            CreateTintedMaterial(brickBrown, "Brown", 0f),
            CreateTintedMaterial(brickBrown, "Brown_Light", colorVariation),
            CreateTintedMaterial(brickDarkBrown, "DarkBrown_Dark", -colorVariation),
            CreateTintedMaterial(brickDarkBrown, "DarkBrown", 0f),
            CreateTintedMaterial(brickDarkBrown, "DarkBrown_Light", colorVariation),
            CreateTintedMaterial(brickLightBrown, "LightBrown_Dark", -colorVariation),
            CreateTintedMaterial(brickLightBrown, "LightBrown", 0f),
            CreateTintedMaterial(brickLightBrown, "LightBrown_Light", colorVariation)
        };
    }

    private Material CreateConcreteBaseMaterial()
    {
        if (concreteBaseMaterial != null)
            return concreteBaseMaterial;

        return CreateTintedMaterial(
            concreteBaseColor,
            "ConcreteBase",
            0f
        );
    }

    private Material CreateTintedMaterial(
        Color color,
        string suffix,
        float brightnessOffset)
    {
        Color adjustedColor = AdjustBrightness(color, brightnessOffset);

        Material material = new Material(walkwayMaterial)
        {
            name = $"{walkwayMaterial.name}_Brick_{suffix}"
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", adjustedColor);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", adjustedColor);

        return material;
    }

    private Color AdjustBrightness(Color color, float offset)
    {
        float multiplier = Mathf.Max(0.05f, 1f + offset);

        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            color.a
        );
    }

    private int PickBrickColorIndex(System.Random random)
    {
        double value = random.NextDouble();

        if (value < 0.45d)
            return 0;

        if (value < 0.75d)
            return 1;

        return 2;
    }

    private int PickBrickShadeIndex(System.Random random)
    {
        double value = random.NextDouble();

        if (value < 0.25d)
            return 0;

        if (value < 0.75d)
            return 1;

        return 2;
    }

    private float GetBrickBaseHeightOffset(float currentHeight)
    {
        if (!alignBrickTopToHeightOffset)
            return heightOffset;

        return heightOffset - currentHeight;
    }

    private float GetConcreteBaseTopHeightOffset()
    {
        if (!alignBrickTopToHeightOffset)
            return heightOffset - concreteBaseSurfaceInset;

        return heightOffset -
               brickHeight -
               heightJitter -
               concreteBaseSurfaceInset;
    }

    private void AddBrick(
        BrickMeshData meshData,
        Transform generatedTransform,
        Vector3 center,
        Vector3 tangent,
        Vector3 right,
        float length,
        float width,
        float height)
    {
        float halfLength = length * 0.5f;
        float halfWidth = width * 0.5f;
        Vector3 up = Vector3.up;

        Vector3 bottomCenter = center;
        Vector3 topCenter = center + up * height;

        Vector3[] corners =
        {
            bottomCenter - tangent * halfLength - right * halfWidth,
            bottomCenter + tangent * halfLength - right * halfWidth,
            bottomCenter + tangent * halfLength + right * halfWidth,
            bottomCenter - tangent * halfLength + right * halfWidth,
            topCenter - tangent * halfLength - right * halfWidth,
            topCenter + tangent * halfLength - right * halfWidth,
            topCenter + tangent * halfLength + right * halfWidth,
            topCenter - tangent * halfLength + right * halfWidth
        };

        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = generatedTransform.InverseTransformPoint(corners[i]);
        }

        AddQuad(meshData, corners[4], corners[5], corners[6], corners[7]);
        AddQuad(meshData, corners[0], corners[3], corners[2], corners[1]);
        AddQuad(meshData, corners[0], corners[1], corners[5], corners[4]);
        AddQuad(meshData, corners[1], corners[2], corners[6], corners[5]);
        AddQuad(meshData, corners[2], corners[3], corners[7], corners[6]);
        AddQuad(meshData, corners[3], corners[0], corners[4], corners[7]);
    }

    private void AddQuad(
        BrickMeshData meshData,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int index = meshData.vertices.Count;

        meshData.vertices.Add(a);
        meshData.vertices.Add(b);
        meshData.vertices.Add(c);
        meshData.vertices.Add(d);

        meshData.uvs.Add(new Vector2(0f, 0f));
        meshData.uvs.Add(new Vector2(1f, 0f));
        meshData.uvs.Add(new Vector2(1f, 1f));
        meshData.uvs.Add(new Vector2(0f, 1f));

        meshData.triangles.Add(index);
        meshData.triangles.Add(index + 1);
        meshData.triangles.Add(index + 2);

        meshData.triangles.Add(index);
        meshData.triangles.Add(index + 2);
        meshData.triangles.Add(index + 3);
    }

    private PathSample EvaluateByDistance(
        List<Vector3> points,
        List<float> cumulativeDistances,
        float distance)
    {
        float clampedDistance =
            Mathf.Clamp(distance, 0f, cumulativeDistances[cumulativeDistances.Count - 1]);

        int segmentIndex = 0;

        for (int i = 1; i < cumulativeDistances.Count; i++)
        {
            if (cumulativeDistances[i] >= clampedDistance)
            {
                segmentIndex = i - 1;
                break;
            }
        }

        float segmentStart = cumulativeDistances[segmentIndex];
        float segmentEnd = cumulativeDistances[segmentIndex + 1];
        float segmentLength = Mathf.Max(0.0001f, segmentEnd - segmentStart);
        float t = (clampedDistance - segmentStart) / segmentLength;

        Vector3 start = points[segmentIndex];
        Vector3 end = points[segmentIndex + 1];
        Vector3 tangent = end - start;

        if (tangent.sqrMagnitude < 0.000001f)
            tangent = Vector3.forward;

        return new PathSample(
            Vector3.Lerp(start, end, t),
            tangent.normalized
        );
    }

    private List<float> BuildCumulativeDistances(List<Vector3> points)
    {
        List<float> distances = new List<float>(points.Count)
        {
            0f
        };

        float accumulatedDistance = 0f;

        for (int i = 1; i < points.Count; i++)
        {
            accumulatedDistance += Vector3.Distance(points[i - 1], points[i]);
            distances.Add(accumulatedDistance);
        }

        return distances;
    }

    private float RandomRange(System.Random random, float min, float max)
    {
        if (Mathf.Approximately(min, max))
            return min;

        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private List<Vector3> BuildAdaptiveCenterline()
    {
        List<Vector3> points = new List<Vector3>();

        int baseSegments = Mathf.Max(10, sampleCount);

        Vector3 first = EvaluateWorldPosition(0f);
        points.Add(first);

        for (int i = 0; i < baseSegments; i++)
        {
            float t0 = i / (float)baseSegments;
            float t1 = (i + 1) / (float)baseSegments;

            Vector3 p0 = i == 0
                ? first
                : EvaluateWorldPosition(t0);

            Vector3 p1 =
                EvaluateWorldPosition(t1);

            SubdivideCurve(
                points,
                t0,
                p0,
                t1,
                p1,
                0
            );
        }

        return RemoveDuplicatePoints(points);
    }

    private void SubdivideCurve(
        List<Vector3> output,
        float t0,
        Vector3 p0,
        float t1,
        Vector3 p1,
        int depth)
    {
        float middleT = (t0 + t1) * 0.5f;

        Vector3 middlePoint =
            EvaluateWorldPosition(middleT);

        Vector3 straightMiddle =
            (p0 + p1) * 0.5f;

        float deviation = Vector3.Distance(
            middlePoint,
            straightMiddle
        );

        Vector3 firstDirection =
            middlePoint - p0;

        Vector3 secondDirection =
            p1 - middlePoint;

        float angle = 0f;

        if (firstDirection.sqrMagnitude > 0.000001f &&
            secondDirection.sqrMagnitude > 0.000001f)
        {
            angle = Vector3.Angle(
                firstDirection,
                secondDirection
            );
        }

        bool subdivide =
            deviation > curveTolerance ||
            angle > 3f;

        if (subdivide && depth < maxSubdivisionDepth)
        {
            SubdivideCurve(
                output,
                t0,
                p0,
                middleT,
                middlePoint,
                depth + 1
            );

            SubdivideCurve(
                output,
                middleT,
                middlePoint,
                t1,
                p1,
                depth + 1
            );

            return;
        }

        output.Add(p1);
    }

    private Vector3 EvaluateWorldPosition(float t)
    {
        float3 value =
            splineContainer.EvaluatePosition(
                Mathf.Clamp01(t)
            );

        return new Vector3(
            value.x,
            value.y,
            value.z
        );
    }

    private static List<Vector3> RemoveDuplicatePoints(
        List<Vector3> source)
    {
        List<Vector3> cleaned =
            new List<Vector3>();

        foreach (Vector3 point in source)
        {
            if (cleaned.Count == 0 ||
                Vector3.Distance(
                    cleaned[cleaned.Count - 1],
                    point
                ) > 0.001f)
            {
                cleaned.Add(point);
            }
        }

        return cleaned;
    }
}
