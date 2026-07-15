using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GrassScatterGenerator : MonoBehaviour
{
    [Header("Area")]
    [Tooltip("공원/연구지구 바닥 Collider입니다. 지정하면 이 Collider 위에만 풀이 생성됩니다.")]
    public Collider grassAreaCollider;

    [Tooltip("비워두면 이 오브젝트 위치를 중심으로 사용합니다.")]
    public Transform areaCenter;

    [Tooltip("grassAreaCollider가 없을 때만 사용하는 사각형 생성 영역입니다.")]
    public Vector2 areaSize = new Vector2(30f, 30f);

    [Tooltip("풀 뿌리의 기준 월드 Y 높이입니다.")]
    public float groundHeight = 0f;

    [Tooltip("켜면 모든 풀의 시작점 Y를 groundHeight로 고정합니다.")]
    public bool lockGrassRootToGroundHeight = true;

    [Header("Ground")]
    [Tooltip("켜면 위에서 아래로 Raycast를 쏴서 실제 바닥 높이에 풀을 붙입니다. grassAreaCollider가 있으면 자동으로 켜진 것처럼 동작합니다.")]
    public bool useGroundRaycast = false;

    public LayerMask groundLayerMask = ~0;
    public float raycastStartHeight = 50f;
    public float raycastDistance = 120f;

    [Header("Blockers")]
    [Tooltip("건물, 보도, 호수처럼 풀이 생기면 안 되는 오브젝트들의 부모를 넣습니다.")]
    public Transform blockerRoot;

    [Tooltip("Blocker Root 아래 MeshFilter에 Collider가 없으면 자동으로 MeshCollider를 붙입니다.")]
    public bool autoCreateMissingBlockerColliders = true;

    [Tooltip("추가로 직접 제외할 Collider들입니다.")]
    public Collider[] extraBlockers;

    [Tooltip("금지 영역을 Collider보다 조금 더 크게 잡는 거리입니다.")]
    [Min(0f)]
    public float blockerPadding = 0.25f;

    [Tooltip("보도/호수 Mesh를 위에서 아래로 검사할 때 시작 높이입니다.")]
    [Min(1f)]
    public float blockerRaycastStartHeight = 40f;

    [Tooltip("보도/호수 Mesh를 위에서 아래로 검사할 때 거리입니다.")]
    [Min(1f)]
    public float blockerRaycastDistance = 100f;

    [Header("Grass")]
    [Min(1)]
    public int grassCount = 2500;

    [Min(1)]
    public int maxAttempts = 20000;

    [Min(0.02f)]
    public float bladeWidth = 0.12f;

    [Min(0.02f)]
    public float bladeHeight = 0.45f;

    [Range(0f, 1f)]
    public float sizeVariation = 0.45f;

    [Range(1, 4)]
    public int crossedPlanesPerClump = 3;

    [Tooltip("켜면 잔디 알파 텍스처가 입혀진 billboard 방식으로 생성합니다.")]
    public bool useTexturedGrassBillboards = true;

    public Color grassDark = new Color(0.13f, 0.35f, 0.08f, 1f);
    public Color grassMid = new Color(0.27f, 0.55f, 0.12f, 1f);
    public Color grassLight = new Color(0.46f, 0.68f, 0.18f, 1f);

    public Material grassMaterial;

    [Header("Random")]
    public int randomSeed = 2468;

    private const string GeneratedName = "Generated_Grass";

    private sealed class GrassMeshData
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> triangles = new List<int>();
        public readonly List<Vector2> uvs = new List<Vector2>();
    }

    [ContextMenu("Generate Grass")]
    public void GenerateGrass()
    {
        ClearGrass();

        List<Collider> blockers = CollectBlockers();
        System.Random random = new System.Random(randomSeed);

        GrassMeshData[] grassMeshes =
        {
            new GrassMeshData(),
            new GrassMeshData(),
            new GrassMeshData()
        };

        int created = 0;
        int attempts = 0;

        while (created < grassCount && attempts < maxAttempts)
        {
            attempts++;

            Vector3 position = PickRandomAreaPosition(random);

            if (!TryProjectToGround(ref position, out Collider groundCollider))
                continue;

            if (!IsAllowedGround(groundCollider))
                continue;

            if (IsBlocked(position, blockers))
                continue;

            ApplyGrassRootHeight(ref position);

            AddGrassClump(
                grassMeshes,
                position,
                random
            );

            created++;
        }

        if (CountVertices(grassMeshes) == 0)
        {
            Debug.LogWarning($"{name}: 생성 가능한 풀 위치를 찾지 못했습니다.");
            return;
        }

        GameObject generated = new GameObject(GeneratedName);
        generated.transform.SetParent(transform, false);
        generated.transform.localPosition = Vector3.zero;
        generated.transform.localRotation = Quaternion.identity;
        generated.transform.localScale = Vector3.one;

        CreateGrassMeshObjects(generated.transform, grassMeshes);

        Debug.Log(
            $"[{name}] 풀 생성 완료 | Created: {created}, Attempts: {attempts}, Blockers: {blockers.Count}"
        );
    }

    private void CreateGrassMeshObjects(
        Transform parent,
        GrassMeshData[] grassMeshes)
    {
        Material[] materials =
        {
            CreateGrassMaterial(grassDark, "Dark"),
            CreateGrassMaterial(grassMid, "Mid"),
            CreateGrassMaterial(grassLight, "Light")
        };

        for (int i = 0; i < grassMeshes.Length; i++)
        {
            if (grassMeshes[i].vertices.Count == 0)
                continue;

            GameObject grassObject =
                new GameObject($"Grass_{i + 1}");

            grassObject.transform.SetParent(parent, false);
            grassObject.transform.localPosition = Vector3.zero;
            grassObject.transform.localRotation = Quaternion.identity;
            grassObject.transform.localScale = Vector3.one;

            Mesh mesh = new Mesh
            {
                name = $"{name}_GrassMesh_{i + 1}"
            };

            if (grassMeshes[i].vertices.Count > 65000)
            {
                mesh.indexFormat =
                    UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(grassMeshes[i].vertices);
            mesh.SetTriangles(grassMeshes[i].triangles, 0);
            mesh.SetUVs(0, grassMeshes[i].uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = grassObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = grassObject.AddComponent<MeshRenderer>();

            filter.sharedMesh = mesh;
            renderer.sharedMaterial = materials[i];
        }
    }

    private int CountVertices(GrassMeshData[] grassMeshes)
    {
        int count = 0;

        for (int i = 0; i < grassMeshes.Length; i++)
        {
            count += grassMeshes[i].vertices.Count;
        }

        return count;
    }

    [ContextMenu("Clear Grass")]
    public void ClearGrass()
    {
        Transform existing = transform.Find(GeneratedName);

        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    private Vector3 PickRandomAreaPosition(System.Random random)
    {
        if (grassAreaCollider != null)
        {
            Bounds bounds = grassAreaCollider.bounds;

            float areaX = Mathf.Lerp(
                bounds.min.x,
                bounds.max.x,
                (float)random.NextDouble()
            );

            float areaZ = Mathf.Lerp(
                bounds.min.z,
                bounds.max.z,
                (float)random.NextDouble()
            );

            return new Vector3(areaX, bounds.center.y, areaZ);
        }

        Transform center = areaCenter != null ? areaCenter : transform;

        float x =
            ((float)random.NextDouble() - 0.5f) * areaSize.x;

        float z =
            ((float)random.NextDouble() - 0.5f) * areaSize.y;

        return center.position + new Vector3(x, groundHeight, z);
    }

    private bool TryProjectToGround(
        ref Vector3 position,
        out Collider groundCollider)
    {
        groundCollider = null;

        if (!useGroundRaycast && grassAreaCollider == null)
        {
            position.y = groundHeight;
            return true;
        }

        Vector3 rayStart =
            position + Vector3.up * raycastStartHeight;

        if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                raycastDistance,
                groundLayerMask))
        {
            position = hit.point;
            groundCollider = hit.collider;
            return true;
        }

        return false;
    }

    private void ApplyGrassRootHeight(ref Vector3 position)
    {
        if (!lockGrassRootToGroundHeight)
            return;

        position.y = groundHeight;
    }

    private bool IsAllowedGround(Collider groundCollider)
    {
        if (grassAreaCollider == null)
            return true;

        if (groundCollider == null)
            return false;

        return groundCollider == grassAreaCollider ||
               groundCollider.transform.IsChildOf(grassAreaCollider.transform);
    }

    private List<Collider> CollectBlockers()
    {
        List<Collider> blockers = new List<Collider>();

        if (blockerRoot != null)
        {
            if (autoCreateMissingBlockerColliders)
                CreateMissingMeshColliders(blockerRoot);

            blockers.AddRange(
                blockerRoot.GetComponentsInChildren<Collider>()
            );
        }

        if (extraBlockers != null)
        {
            for (int i = 0; i < extraBlockers.Length; i++)
            {
                if (extraBlockers[i] != null &&
                    !blockers.Contains(extraBlockers[i]))
                {
                    blockers.Add(extraBlockers[i]);
                }
            }
        }

        return blockers;
    }

    private void CreateMissingMeshColliders(Transform root)
    {
        MeshFilter[] meshFilters =
            root.GetComponentsInChildren<MeshFilter>(true);

        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter filter = meshFilters[i];

            if (filter.sharedMesh == null)
                continue;

            if (filter.GetComponent<Collider>() != null)
                continue;

            MeshCollider collider =
                filter.gameObject.AddComponent<MeshCollider>();

            collider.sharedMesh = filter.sharedMesh;
        }
    }

    private bool IsBlocked(
        Vector3 position,
        List<Collider> blockers)
    {
        if (IsBlockedByRaycast(position, blockers))
            return true;

        for (int i = 0; i < blockers.Count; i++)
        {
            Collider blocker = blockers[i];

            if (blocker == null || !blocker.enabled)
                continue;

            if (ShouldUseBoundsBlocker(blocker) &&
                IsInsideBlockerBoundsXZ(position, blocker.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBlockedByRaycast(
        Vector3 position,
        List<Collider> blockers)
    {
        Vector3 rayStart =
            position + Vector3.up * blockerRaycastStartHeight;

        RaycastHit[] hits = Physics.RaycastAll(
            rayStart,
            Vector3.down,
            blockerRaycastDistance
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null)
                continue;

            for (int j = 0; j < blockers.Count; j++)
            {
                Collider blocker = blockers[j];

                if (blocker == null || !blocker.enabled)
                    continue;

                if (hitCollider == blocker)
                    return true;
            }
        }

        return false;
    }

    private bool ShouldUseBoundsBlocker(Collider blocker)
    {
        return blocker is BoxCollider ||
               blocker is SphereCollider ||
               blocker is CapsuleCollider;
    }

    private bool IsInsideBlockerBoundsXZ(
        Vector3 position,
        Bounds bounds)
    {
        return position.x >= bounds.min.x - blockerPadding &&
               position.x <= bounds.max.x + blockerPadding &&
               position.z >= bounds.min.z - blockerPadding &&
               position.z <= bounds.max.z + blockerPadding;
    }

    private void AddGrassClump(
        GrassMeshData[] grassMeshes,
        Vector3 position,
        System.Random random)
    {
        if (useTexturedGrassBillboards)
        {
            AddTexturedGrassClump(grassMeshes, position, random);
            return;
        }

        int bladeCount = Mathf.Max(1, crossedPlanesPerClump);
        float randomScale = Mathf.Lerp(
            1f - sizeVariation,
            1f + sizeVariation,
            (float)random.NextDouble()
        );

        float width = bladeWidth * randomScale;
        float height = bladeHeight * randomScale;
        float baseAngle = (float)random.NextDouble() * 360f;
        int colorIndex = PickGrassColorIndex(random);
        GrassMeshData meshData = grassMeshes[colorIndex];

        for (int i = 0; i < bladeCount; i++)
        {
            float angle = baseAngle + 360f / bladeCount * i;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            float lean = Mathf.Lerp(0.03f, 0.12f, (float)random.NextDouble());
            float bladeWidthScale = Mathf.Lerp(0.45f, 1.1f, (float)random.NextDouble());
            float bladeHeightScale = Mathf.Lerp(0.75f, 1.25f, (float)random.NextDouble());

            Vector3 root = position +
                           forward * RandomRange(random, -0.025f, 0.025f) +
                           right * RandomRange(random, -0.025f, 0.025f);
            Vector3 baseLeft =
                root - right * width * bladeWidthScale * 0.5f;
            Vector3 baseRight =
                root + right * width * bladeWidthScale * 0.5f;
            Vector3 tip =
                root +
                Vector3.up * height * bladeHeightScale +
                forward * lean;

            AddBladeTriangle(meshData, baseLeft, baseRight, tip);
            AddBladeTriangle(meshData, baseRight, baseLeft, tip);
        }
    }

    private void AddTexturedGrassClump(
        GrassMeshData[] grassMeshes,
        Vector3 position,
        System.Random random)
    {
        int planeCount = Mathf.Max(1, crossedPlanesPerClump);
        float randomScale = Mathf.Lerp(
            1f - sizeVariation,
            1f + sizeVariation,
            (float)random.NextDouble()
        );

        float width = bladeWidth * 2.8f * randomScale;
        float height = bladeHeight * randomScale;
        float baseAngle = (float)random.NextDouble() * 360f;
        int colorIndex = PickGrassColorIndex(random);
        GrassMeshData meshData = grassMeshes[colorIndex];

        for (int i = 0; i < planeCount; i++)
        {
            float angle = baseAngle + 180f / planeCount * i;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 center =
                position +
                forward * RandomRange(random, -0.035f, 0.035f) +
                right * RandomRange(random, -0.035f, 0.035f);

            float planeWidth = width * RandomRange(random, 0.8f, 1.25f);
            float planeHeight = height * RandomRange(random, 0.85f, 1.3f);
            Vector3 topOffset =
                Vector3.up * planeHeight +
                forward * RandomRange(random, -0.05f, 0.08f);

            Vector3 a = center - right * planeWidth * 0.5f;
            Vector3 b = center + right * planeWidth * 0.5f;
            Vector3 c = center + right * planeWidth * 0.5f + topOffset;
            Vector3 d = center - right * planeWidth * 0.5f + topOffset;

            AddGrassQuad(meshData, a, b, c, d);
            AddGrassQuad(meshData, b, a, d, c);
        }
    }

    private int PickGrassColorIndex(System.Random random)
    {
        double value = random.NextDouble();

        if (value < 0.35d)
            return 0;

        if (value < 0.8d)
            return 1;

        return 2;
    }

    private float RandomRange(
        System.Random random,
        float min,
        float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private void AddBladeTriangle(
        GrassMeshData meshData,
        Vector3 baseLeft,
        Vector3 baseRight,
        Vector3 tip)
    {
        int index = meshData.vertices.Count;

        meshData.vertices.Add(transform.InverseTransformPoint(baseLeft));
        meshData.vertices.Add(transform.InverseTransformPoint(baseRight));
        meshData.vertices.Add(transform.InverseTransformPoint(tip));

        meshData.uvs.Add(new Vector2(0f, 0f));
        meshData.uvs.Add(new Vector2(1f, 0f));
        meshData.uvs.Add(new Vector2(0.5f, 1f));

        meshData.triangles.Add(index);
        meshData.triangles.Add(index + 1);
        meshData.triangles.Add(index + 2);
    }

    private void AddGrassQuad(
        GrassMeshData meshData,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d)
    {
        int index = meshData.vertices.Count;

        meshData.vertices.Add(transform.InverseTransformPoint(a));
        meshData.vertices.Add(transform.InverseTransformPoint(b));
        meshData.vertices.Add(transform.InverseTransformPoint(c));
        meshData.vertices.Add(transform.InverseTransformPoint(d));

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

    private Material CreateGrassMaterial(Color color, string suffix)
    {
        if (grassMaterial != null)
        {
            Material clonedMaterial = new Material(grassMaterial)
            {
                name = $"{grassMaterial.name}_{suffix}"
            };

            ApplyMaterialColor(clonedMaterial, color);
            ApplyGrassTexture(clonedMaterial);
            return clonedMaterial;
        }

        Shader shader = Shader.Find("Semicon/Grass Billboard");

        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material createdMaterial = new Material(shader)
        {
            name = $"Generated_Grass_Material_{suffix}"
        };

        ApplyMaterialColor(createdMaterial, color);
        ApplyGrassTexture(createdMaterial);
        return createdMaterial;
    }

    private void ApplyMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private void ApplyGrassTexture(Material material)
    {
        Texture2D texture = CreateProceduralGrassTexture();

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 1f);

        if (material.HasProperty("_Cutoff"))
            material.SetFloat("_Cutoff", 0.25f);

        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        material.EnableKeyword("_ALPHATEST_ON");
        material.renderQueue = 2450;
    }

    private Texture2D CreateProceduralGrassTexture()
    {
        const int width = 64;
        const int height = 128;

        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        )
        {
            name = "Generated_Grass_Blade_Texture",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color transparent = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, transparent);
            }
        }

        DrawProceduralBlade(texture, 18f, 0f, 30f, height - 3f, 9f);
        DrawProceduralBlade(texture, 32f, 0f, 34f, height - 1f, 12f);
        DrawProceduralBlade(texture, 46f, 0f, 37f, height - 10f, 8f);
        DrawProceduralBlade(texture, 26f, 0f, 17f, height - 24f, 6f);
        DrawProceduralBlade(texture, 40f, 0f, 54f, height - 26f, 6f);

        texture.Apply();
        return texture;
    }

    private void DrawProceduralBlade(
        Texture2D texture,
        float rootX,
        float rootY,
        float tipX,
        float tipY,
        float maxWidth)
    {
        int width = texture.width;
        int height = texture.height;

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            float centerX = Mathf.Lerp(rootX, tipX, v);
            float bladeHalfWidth = Mathf.Lerp(maxWidth, 0.4f, v) * 0.5f;

            for (int x = 0; x < width; x++)
            {
                float distance = Mathf.Abs(x - centerX);

                if (distance > bladeHalfWidth)
                    continue;

                float edge = 1f - distance / Mathf.Max(0.001f, bladeHalfWidth);
                float alpha = Mathf.Clamp01(edge * Mathf.SmoothStep(0f, 1f, v));
                Color current = texture.GetPixel(x, y);
                float newAlpha = Mathf.Max(current.a, alpha);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, newAlpha));
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (grassAreaCollider != null)
        {
            DrawGrassAreaColliderGizmo();
            return;
        }

        Transform center = areaCenter != null ? areaCenter : transform;

        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.25f);
        Gizmos.DrawCube(
            center.position + Vector3.up * 0.05f,
            new Vector3(areaSize.x, 0.1f, areaSize.y)
        );

        Gizmos.color = new Color(0.1f, 0.6f, 0.1f, 1f);
        Gizmos.DrawWireCube(
            center.position + Vector3.up * 0.05f,
            new Vector3(areaSize.x, 0.1f, areaSize.y)
        );
    }

    private void DrawGrassAreaColliderGizmo()
    {
        Gizmos.color = new Color(0.15f, 0.9f, 0.2f, 0.9f);

        MeshCollider meshCollider = grassAreaCollider as MeshCollider;

        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = meshCollider.transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(meshCollider.sharedMesh);
            Gizmos.matrix = previousMatrix;
            return;
        }

        Bounds bounds = grassAreaCollider.bounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
