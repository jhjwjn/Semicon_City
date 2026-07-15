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

    [Tooltip("Raycast를 쓰지 않을 때 풀을 생성할 높이입니다.")]
    public float groundHeight = 0f;

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
        for (int i = 0; i < blockers.Count; i++)
        {
            Collider blocker = blockers[i];

            if (blocker == null || !blocker.enabled)
                continue;

            if (IsInsideBlockerBoundsXZ(position, blocker.bounds))
                return true;
        }

        return false;
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

    private Material CreateGrassMaterial(Color color, string suffix)
    {
        if (grassMaterial != null)
        {
            Material clonedMaterial = new Material(grassMaterial)
            {
                name = $"{grassMaterial.name}_{suffix}"
            };

            ApplyMaterialColor(clonedMaterial, color);
            return clonedMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material createdMaterial = new Material(shader)
        {
            name = $"Generated_Grass_Material_{suffix}"
        };

        ApplyMaterialColor(createdMaterial, color);
        return createdMaterial;
    }

    private void ApplyMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
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
