using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
public class ParkVegetationGenerator : MonoBehaviour
{
    [Header("Area")]
    public Collider grassAreaCollider;
    public Transform blockerRoot;
    public Collider[] extraBlockers;
    public bool autoCreateMissingBlockerColliders = true;
    public float raycastStartHeight = 60f;
    public float raycastDistance = 140f;
    public float groundHeight = 0f;

    [Header("Open Grass Objects")]
    public GameObject[] treePrefabs;
    public int treeCount = 12;
    public Vector2 treeScaleRange = new Vector2(0.75f, 1.25f);

    public GameObject[] bushPrefabs;
    public int bushCount = 24;
    public Vector2 bushScaleRange = new Vector2(0.75f, 1.35f);

    [Header("Lake Edge")]
    public SplineContainer lakeSpline;
    public GameObject[] shorePrefabs;
    public int shoreCount = 80;
    public Vector2 shoreOffsetRange = new Vector2(-0.35f, 0.9f);
    public Vector2 shoreScaleRange = new Vector2(0.8f, 1.25f);

    [Header("Water Surface")]
    public GameObject[] waterPlantPrefabs;
    public int waterPlantCount = 24;
    public Vector2 waterPlantInsetRange = new Vector2(0.7f, 2.5f);
    public Vector2 waterPlantScaleRange = new Vector2(0.65f, 1.1f);
    public float waterPlantHeight = 0.02f;

    [Header("Random")]
    public int randomSeed = 13579;
    public int maxAttempts = 20000;

    private const string GeneratedName = "Generated_Park_Vegetation";

    [ContextMenu("Generate Park Vegetation")]
    public void GenerateParkVegetation()
    {
        ClearParkVegetation();

        if (grassAreaCollider == null)
        {
            Debug.LogError($"{name}: Grass Area Collider를 지정하세요.");
            return;
        }

        GameObject root = new GameObject(GeneratedName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        List<Collider> blockers = CollectBlockers();
        System.Random random = new System.Random(randomSeed);

        int trees = ScatterInGrassArea(
            root.transform,
            treePrefabs,
            treeCount,
            treeScaleRange,
            blockers,
            random
        );

        int bushes = ScatterInGrassArea(
            root.transform,
            bushPrefabs,
            bushCount,
            bushScaleRange,
            blockers,
            random
        );

        int shore = ScatterAlongLakeEdge(
            root.transform,
            shorePrefabs,
            shoreCount,
            shoreOffsetRange,
            shoreScaleRange,
            random
        );

        int waterPlants = ScatterInsideLake(
            root.transform,
            waterPlantPrefabs,
            waterPlantCount,
            waterPlantInsetRange,
            waterPlantScaleRange,
            random
        );

        Debug.Log(
            $"[{name}] 공원 식생 생성 완료 | Trees: {trees}, Bushes: {bushes}, Shore: {shore}, Water: {waterPlants}, Blockers: {blockers.Count}"
        );
    }

    [ContextMenu("Clear Park Vegetation")]
    public void ClearParkVegetation()
    {
        Transform existing = transform.Find(GeneratedName);

        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    private int ScatterInGrassArea(
        Transform parent,
        GameObject[] prefabs,
        int count,
        Vector2 scaleRange,
        List<Collider> blockers,
        System.Random random)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0)
            return 0;

        Bounds bounds = grassAreaCollider.bounds;
        int created = 0;
        int attempts = 0;

        while (created < count && attempts < maxAttempts)
        {
            attempts++;

            Vector3 candidate = new Vector3(
                Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble()),
                bounds.center.y,
                Mathf.Lerp(bounds.min.z, bounds.max.z, (float)random.NextDouble())
            );

            if (!IsInsideGrassArea(candidate))
                continue;

            if (IsBlocked(candidate, blockers))
                continue;

            candidate.y = groundHeight;

            GameObject prefab = PickPrefab(prefabs, random);

            if (prefab == null)
                continue;

            CreateInstance(
                parent,
                prefab,
                candidate,
                RandomRotation(random),
                RandomScale(scaleRange, random)
            );

            created++;
        }

        return created;
    }

    private int ScatterAlongLakeEdge(
        Transform parent,
        GameObject[] prefabs,
        int count,
        Vector2 offsetRange,
        Vector2 scaleRange,
        System.Random random)
    {
        if (lakeSpline == null ||
            prefabs == null ||
            prefabs.Length == 0 ||
            count <= 0)
        {
            return 0;
        }

        int created = 0;

        for (int i = 0; i < count; i++)
        {
            float t = (i + (float)random.NextDouble()) / count;
            Vector3 position = EvaluateSplinePosition(t);
            Vector3 tangent = EvaluateSplineTangent(t);
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

            if (right.sqrMagnitude < 0.000001f)
                right = Vector3.right;

            float offset = RandomRange(offsetRange, random);
            position += right * offset;
            position.y = groundHeight;

            GameObject prefab = PickPrefab(prefabs, random);

            if (prefab == null)
                continue;

            CreateInstance(
                parent,
                prefab,
                position,
                RandomRotation(random),
                RandomScale(scaleRange, random)
            );

            created++;
        }

        return created;
    }

    private int ScatterInsideLake(
        Transform parent,
        GameObject[] prefabs,
        int count,
        Vector2 insetRange,
        Vector2 scaleRange,
        System.Random random)
    {
        if (lakeSpline == null ||
            prefabs == null ||
            prefabs.Length == 0 ||
            count <= 0)
        {
            return 0;
        }

        Vector3 center = EstimateLakeCenter();
        int created = 0;

        for (int i = 0; i < count; i++)
        {
            float t = (i + (float)random.NextDouble()) / count;
            Vector3 edge = EvaluateSplinePosition(t);
            Vector3 directionToCenter = center - edge;
            directionToCenter.y = 0f;

            if (directionToCenter.sqrMagnitude < 0.000001f)
                continue;

            directionToCenter.Normalize();

            Vector3 position =
                edge + directionToCenter * RandomRange(insetRange, random);

            position.y = waterPlantHeight;
            GameObject prefab = PickPrefab(prefabs, random);

            if (prefab == null)
                continue;

            CreateInstance(
                parent,
                prefab,
                position,
                RandomRotation(random),
                RandomScale(scaleRange, random)
            );

            created++;
        }

        return created;
    }

    private void CreateInstance(
        Transform parent,
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        float scale)
    {
        GameObject instance = Instantiate(prefab, position, rotation, parent);
        instance.name = prefab.name;
        instance.transform.localScale = Vector3.one * scale;
    }

    private bool IsInsideGrassArea(Vector3 worldPosition)
    {
        Vector3 rayStart = worldPosition + Vector3.up * raycastStartHeight;

        RaycastHit[] hits = Physics.RaycastAll(
            rayStart,
            Vector3.down,
            raycastDistance
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == grassAreaCollider)
                return true;
        }

        return false;
    }

    private bool IsBlocked(
        Vector3 worldPosition,
        List<Collider> blockers)
    {
        Vector3 rayStart = worldPosition + Vector3.up * raycastStartHeight;

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
                if (hitCollider == blockers[j])
                    return true;
            }
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

    private Vector3 EvaluateSplinePosition(float t)
    {
        float3 value = lakeSpline.EvaluatePosition(Mathf.Repeat(t, 1f));
        return new Vector3(value.x, value.y, value.z);
    }

    private Vector3 EvaluateSplineTangent(float t)
    {
        float3 value = lakeSpline.EvaluateTangent(Mathf.Repeat(t, 1f));
        Vector3 tangent = new Vector3(value.x, value.y, value.z);
        tangent.y = 0f;

        if (tangent.sqrMagnitude < 0.000001f)
            tangent = Vector3.forward;

        return tangent.normalized;
    }

    private Vector3 EstimateLakeCenter()
    {
        Vector3 center = Vector3.zero;
        int count = 64;

        for (int i = 0; i < count; i++)
        {
            center += EvaluateSplinePosition(i / (float)count);
        }

        return center / count;
    }

    private GameObject PickPrefab(GameObject[] prefabs, System.Random random)
    {
        if (prefabs == null || prefabs.Length == 0)
            return null;

        return prefabs[random.Next(0, prefabs.Length)];
    }

    private Quaternion RandomRotation(System.Random random)
    {
        return Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
    }

    private float RandomScale(Vector2 range, System.Random random)
    {
        return RandomRange(range, random);
    }

    private float RandomRange(Vector2 range, System.Random random)
    {
        return Mathf.Lerp(range.x, range.y, (float)random.NextDouble());
    }
}
