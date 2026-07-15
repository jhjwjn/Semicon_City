using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Terrain))]
public class TerrainGrassDetailPainter : MonoBehaviour
{
    [Header("Terrain")]
    public Terrain targetTerrain;

    [Tooltip("공원/연구지구 바닥 MeshCollider입니다. 예: City.002")]
    public Collider grassAreaCollider;

    [Tooltip("켜면 Terrain 크기와 위치를 Grass Area Collider의 bounds에 맞춥니다.")]
    public bool fitTerrainToGrassArea = true;

    [Tooltip("Terrain을 기존 바닥보다 살짝 아래로 둡니다.")]
    public float terrainYOffset = -0.02f;

    [Tooltip("켜면 Grass Area Collider 높이에 맞춰 Terrain 높이맵을 굽습니다.")]
    public bool conformTerrainHeightToGrassArea = true;

    [Tooltip("Terrain 높이맵 해상도입니다. 513 또는 1025를 권장합니다.")]
    [Range(33, 2049)]
    public int heightmapResolution = 513;

    [Tooltip("잔디가 지면에 파묻히거나 뜰 때 미세 조정하는 값입니다.")]
    public float surfaceYOffset = 0f;

    [Tooltip("켜면 Terrain의 사각형 바닥 표면은 숨기고 잔디 Detail만 보이게 합니다.")]
    public bool hideTerrainSurface = true;

    [Tooltip("Terrain Detail이 보이는 거리입니다.")]
    [Min(1f)]
    public float detailObjectDistance = 120f;

    [Tooltip("Terrain Detail 전체 밀도 배율입니다.")]
    [Range(0f, 1f)]
    public float detailObjectDensity = 1f;

    [Header("Blockers")]
    [Tooltip("보도, 호수, 건물처럼 풀이 생기면 안 되는 오브젝트들의 부모입니다.")]
    public Transform blockerRoot;

    public Collider[] extraBlockers;

    [Tooltip("Blocker Root 아래 MeshFilter에 Collider가 없으면 자동으로 MeshCollider를 붙입니다.")]
    public bool autoCreateMissingBlockerColliders = true;

    [Tooltip("Box/Sphere/Capsule 건물 Collider를 약간 크게 제외할 거리입니다.")]
    [Min(0f)]
    public float blockerPadding = 0.25f;

    [Header("Detail Map")]
    [Range(128, 2048)]
    public int detailResolution = 512;

    [Range(8, 64)]
    public int detailResolutionPerPatch = 16;

    [Range(0, 16)]
    public int grassDensity = 8;

    [Range(0, 16)]
    public int flowerDensity = 1;

    [Range(0, 16)]
    public int plantDensity = 1;

    [Range(0f, 1f)]
    public float densityNoise = 0.35f;

    [Header("Idyllic Nature Details")]
    [Tooltip("Idyllic Fantasy Nature의 Grass_01~03 prefab을 넣으세요.")]
    public GameObject[] grassDetailPrefabs;

    [Tooltip("FlowerMeadow_* prefab을 2~4개 정도 넣으세요.")]
    public GameObject[] flowerMeadowPrefabs;

    [Tooltip("Plant_01~08 중 공원에 어울리는 작은 식물을 넣으세요.")]
    public GameObject[] plantDetailPrefabs;

    [Header("Fallback Grass Texture")]
    public Texture2D grassTexture;

    public Color healthyColor = new Color(0.32f, 0.62f, 0.16f, 1f);
    public Color dryColor = new Color(0.18f, 0.36f, 0.08f, 1f);

    [Min(0.01f)]
    public float minWidth = 0.25f;

    [Min(0.01f)]
    public float maxWidth = 0.55f;

    [Min(0.01f)]
    public float minHeight = 0.35f;

    [Min(0.01f)]
    public float maxHeight = 0.85f;

    [Min(0.01f)]
    public float noiseSpread = 0.45f;

    [Header("Raycast")]
    public float raycastStartHeight = 60f;
    public float raycastDistance = 140f;

    [Header("Random")]
    public int randomSeed = 9876;

    [ContextMenu("Setup Terrain Grass")]
    public void SetupTerrainGrass()
    {
        EnsureTerrain();

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogError($"{name}: Terrain 또는 TerrainData가 필요합니다.");
            return;
        }

        if (grassAreaCollider == null)
        {
            Debug.LogError($"{name}: Grass Area Collider를 지정하세요.");
            return;
        }

        if (fitTerrainToGrassArea)
            FitTerrainToGrassArea();

        if (conformTerrainHeightToGrassArea)
            ConformTerrainHeightToGrassArea();

        ApplyTerrainVisibility();
        SetupDetailPrototype();
        PaintGrassDetails();
    }

    [ContextMenu("Paint Grass Details")]
    public void PaintGrassDetails()
    {
        EnsureTerrain();

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogError($"{name}: Terrain 또는 TerrainData가 필요합니다.");
            return;
        }

        if (grassAreaCollider == null)
        {
            Debug.LogError($"{name}: Grass Area Collider를 지정하세요.");
            return;
        }

        TerrainData data = targetTerrain.terrainData;
        ApplyTerrainVisibility();

        if (data.detailResolution != detailResolution)
        {
            data.SetDetailResolution(
                detailResolution,
                detailResolutionPerPatch
            );
        }

        if (data.detailPrototypes == null ||
            data.detailPrototypes.Length == 0)
            SetupDetailPrototype();

        int prototypeCount = data.detailPrototypes.Length;
        int[] prototypeDensities = BuildPrototypeDensities();

        List<Collider> blockers = CollectBlockers();
        int[][,] detailLayers = new int[prototypeCount][,];

        for (int i = 0; i < prototypeCount; i++)
            detailLayers[i] = new int[detailResolution, detailResolution];

        System.Random random = new System.Random(randomSeed);
        int paintedCells = 0;

        for (int z = 0; z < detailResolution; z++)
        {
            for (int x = 0; x < detailResolution; x++)
            {
                Vector3 worldPosition =
                    DetailCellToWorldPosition(x, z, data);

                if (!IsInsideGrassArea(worldPosition))
                    continue;

                if (IsBlocked(worldPosition, blockers))
                    continue;

                PaintPrototypeDensities(
                    detailLayers,
                    prototypeDensities,
                    x,
                    z,
                    random
                );

                paintedCells++;
            }
        }

        for (int i = 0; i < prototypeCount; i++)
            data.SetDetailLayer(0, 0, i, detailLayers[i]);

        Debug.Log(
            $"[{name}] Terrain 디테일 칠하기 완료 | Cells: {paintedCells}, Prototypes: {prototypeCount}, Blockers: {blockers.Count}"
        );
    }

    [ContextMenu("Clear Grass Details")]
    public void ClearGrassDetails()
    {
        EnsureTerrain();

        if (targetTerrain == null || targetTerrain.terrainData == null)
            return;

        ApplyTerrainVisibility();

        TerrainData data = targetTerrain.terrainData;
        int resolution = data.detailResolution;

        if (resolution <= 0 ||
            data.detailPrototypes == null ||
            data.detailPrototypes.Length == 0)
        {
            return;
        }

        for (int i = 0; i < data.detailPrototypes.Length; i++)
        {
            int[,] emptyLayer = new int[resolution, resolution];
            data.SetDetailLayer(0, 0, i, emptyLayer);
        }
    }

    private void EnsureTerrain()
    {
        if (targetTerrain == null)
            targetTerrain = GetComponent<Terrain>();
    }

    private void ApplyTerrainVisibility()
    {
        targetTerrain.drawHeightmap = !hideTerrainSurface;
        targetTerrain.drawTreesAndFoliage = true;
        targetTerrain.detailObjectDistance = detailObjectDistance;
        targetTerrain.detailObjectDensity = detailObjectDensity;
    }

    private void FitTerrainToGrassArea()
    {
        Bounds bounds = grassAreaCollider.bounds;
        TerrainData data = targetTerrain.terrainData;

        data.size = new Vector3(
            bounds.size.x,
            Mathf.Max(1f, bounds.size.y + 1f),
            bounds.size.z
        );

        targetTerrain.transform.position = new Vector3(
            bounds.min.x,
            bounds.min.y + terrainYOffset,
            bounds.min.z
        );
    }

    [ContextMenu("Conform Terrain Height To Grass Area")]
    public void ConformTerrainHeightToGrassArea()
    {
        EnsureTerrain();

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogError($"{name}: Terrain 또는 TerrainData가 필요합니다.");
            return;
        }

        if (grassAreaCollider == null)
        {
            Debug.LogError($"{name}: Grass Area Collider를 지정하세요.");
            return;
        }

        TerrainData data = targetTerrain.terrainData;
        int resolution = Mathf.ClosestPowerOfTwo(
            Mathf.Max(33, heightmapResolution - 1)
        ) + 1;

        data.heightmapResolution = resolution;

        float[,] heights = new float[resolution, resolution];
        Vector3 terrainPosition = targetTerrain.transform.position;
        Vector3 terrainSize = data.size;
        int sampled = 0;

        for (int z = 0; z < resolution; z++)
        {
            float normalizedZ = z / (float)(resolution - 1);

            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = x / (float)(resolution - 1);

                Vector3 rayStart = new Vector3(
                    terrainPosition.x + normalizedX * terrainSize.x,
                    terrainPosition.y + terrainSize.y + raycastStartHeight,
                    terrainPosition.z + normalizedZ * terrainSize.z
                );

                if (TryRaycastGrassArea(rayStart, out RaycastHit hit))
                {
                    float height =
                        (hit.point.y + surfaceYOffset - terrainPosition.y) /
                        Mathf.Max(0.001f, terrainSize.y);

                    heights[z, x] = Mathf.Clamp01(height);
                    sampled++;
                }
                else
                {
                    heights[z, x] = 0f;
                }
            }
        }

        data.SetHeights(0, 0, heights);

        Debug.Log(
            $"[{name}] Terrain 높이 맞춤 완료 | Resolution: {resolution}, Samples: {sampled}"
        );
    }

    private void SetupDetailPrototype()
    {
        TerrainData data = targetTerrain.terrainData;
        List<DetailPrototype> prototypes = new List<DetailPrototype>();

        AddMeshDetailPrototypes(
            prototypes,
            grassDetailPrefabs,
            DetailRenderMode.VertexLit,
            minWidth,
            maxWidth,
            minHeight,
            maxHeight
        );

        AddMeshDetailPrototypes(
            prototypes,
            flowerMeadowPrefabs,
            DetailRenderMode.VertexLit,
            minWidth,
            maxWidth,
            minHeight,
            maxHeight
        );

        AddMeshDetailPrototypes(
            prototypes,
            plantDetailPrefabs,
            DetailRenderMode.VertexLit,
            minWidth,
            maxWidth,
            minHeight,
            maxHeight
        );

        if (prototypes.Count == 0)
            prototypes.Add(CreateFallbackGrassPrototype());

        data.detailPrototypes = prototypes.ToArray();

        data.SetDetailResolution(
            detailResolution,
            detailResolutionPerPatch
        );
    }

    private void AddMeshDetailPrototypes(
        List<DetailPrototype> prototypes,
        GameObject[] prefabs,
        DetailRenderMode renderMode,
        float prototypeMinWidth,
        float prototypeMaxWidth,
        float prototypeMinHeight,
        float prototypeMaxHeight)
    {
        if (prefabs == null)
            return;

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] == null)
                continue;

            DetailPrototype prototype = new DetailPrototype
            {
                prototype = prefabs[i],
                renderMode = renderMode,
                usePrototypeMesh = true,
                useInstancing = true,
                healthyColor = Color.white,
                dryColor = Color.white,
                minWidth = prototypeMinWidth,
                maxWidth = prototypeMaxWidth,
                minHeight = prototypeMinHeight,
                maxHeight = prototypeMaxHeight,
                noiseSpread = noiseSpread
            };

            prototypes.Add(prototype);
        }
    }

    private DetailPrototype CreateFallbackGrassPrototype()
    {
        return new DetailPrototype
        {
            prototypeTexture =
                grassTexture != null
                    ? grassTexture
                    : CreateFallbackGrassTexture(),
            renderMode = DetailRenderMode.GrassBillboard,
            healthyColor = healthyColor,
            dryColor = dryColor,
            minWidth = minWidth,
            maxWidth = maxWidth,
            minHeight = minHeight,
            maxHeight = maxHeight,
            noiseSpread = noiseSpread
        };
    }

    private int[] BuildPrototypeDensities()
    {
        List<int> densities = new List<int>();

        AddDensities(densities, grassDetailPrefabs, grassDensity);
        AddDensities(densities, flowerMeadowPrefabs, flowerDensity);
        AddDensities(densities, plantDetailPrefabs, plantDensity);

        if (densities.Count == 0)
            densities.Add(grassDensity);

        return densities.ToArray();
    }

    private void AddDensities(
        List<int> densities,
        GameObject[] prefabs,
        int density)
    {
        if (prefabs == null)
            return;

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
                densities.Add(density);
        }
    }

    private void PaintPrototypeDensities(
        int[][,] detailLayers,
        int[] prototypeDensities,
        int x,
        int z,
        System.Random random)
    {
        for (int i = 0; i < detailLayers.Length; i++)
        {
            int baseDensity = i < prototypeDensities.Length
                ? prototypeDensities[i]
                : 0;

            int density = PickDensity(random, baseDensity);

            if (density <= 0)
                continue;

            detailLayers[i][z, x] = density;
        }
    }

    private Vector3 DetailCellToWorldPosition(
        int x,
        int z,
        TerrainData data)
    {
        float normalizedX =
            (x + 0.5f) / detailResolution;

        float normalizedZ =
            (z + 0.5f) / detailResolution;

        Vector3 terrainPosition =
            targetTerrain.transform.position;

        return new Vector3(
            terrainPosition.x + normalizedX * data.size.x,
            terrainPosition.y + data.size.y + 0.01f,
            terrainPosition.z + normalizedZ * data.size.z
        );
    }

    private bool IsInsideGrassArea(Vector3 worldPosition)
    {
        Vector3 rayStart =
            worldPosition + Vector3.up * raycastStartHeight;

        return TryRaycastGrassArea(rayStart, out _);
    }

    private bool TryRaycastGrassArea(
        Vector3 rayStart,
        out RaycastHit grassAreaHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            rayStart,
            Vector3.down,
            raycastDistance + raycastStartHeight
        );

        grassAreaHit = new RaycastHit();

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == grassAreaCollider)
            {
                grassAreaHit = hits[i];
                return true;
            }
        }

        return false;
    }

    private bool IsBlocked(
        Vector3 worldPosition,
        List<Collider> blockers)
    {
        Vector3 rayStart =
            worldPosition + Vector3.up * raycastStartHeight;

        RaycastHit[] hits = Physics.RaycastAll(
            rayStart,
            Vector3.down,
            raycastDistance
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

        for (int i = 0; i < blockers.Count; i++)
        {
            Collider blocker = blockers[i];

            if (blocker == null || !blocker.enabled)
                continue;

            if (!ShouldUseBoundsBlocker(blocker))
                continue;

            if (IsInsideBlockerBoundsXZ(worldPosition, blocker.bounds))
                return true;
        }

        return false;
    }

    private List<Collider> CollectBlockers()
    {
        List<Collider> blockers = new List<Collider>();

        if (blockerRoot != null)
        {
            if (autoCreateMissingBlockerColliders)
                CreateMissingMeshColliders(blockerRoot);

            blockers.AddRange(
                blockerRoot.GetComponentsInChildren<Collider>(true)
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

    private int PickDensity(System.Random random, int baseDensity)
    {
        if (baseDensity <= 0)
            return 0;

        float multiplier = Mathf.Lerp(
            1f - densityNoise,
            1f + densityNoise,
            (float)random.NextDouble()
        );

        return Mathf.Clamp(
            Mathf.RoundToInt(baseDensity * multiplier),
            0,
            16
        );
    }

    private Texture2D CreateFallbackGrassTexture()
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
            name = "Generated_Terrain_Grass_Texture",
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

        DrawFallbackBlade(texture, 18f, 0f, 30f, height - 3f, 9f);
        DrawFallbackBlade(texture, 32f, 0f, 34f, height - 1f, 12f);
        DrawFallbackBlade(texture, 46f, 0f, 37f, height - 10f, 8f);
        DrawFallbackBlade(texture, 26f, 0f, 17f, height - 24f, 6f);
        DrawFallbackBlade(texture, 40f, 0f, 54f, height - 26f, 6f);

        texture.Apply();
        return texture;
    }

    private void DrawFallbackBlade(
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

                float edge =
                    1f - distance / Mathf.Max(0.001f, bladeHalfWidth);

                float alpha =
                    Mathf.Clamp01(edge * Mathf.SmoothStep(0f, 1f, v));

                Color current = texture.GetPixel(x, y);
                texture.SetPixel(
                    x,
                    y,
                    new Color(1f, 1f, 1f, Mathf.Max(current.a, alpha))
                );
            }
        }
    }
}
