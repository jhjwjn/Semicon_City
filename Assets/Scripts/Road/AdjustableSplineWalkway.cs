using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SplineContainer), typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class AdjustableSplineWalkway : MonoBehaviour
{
    public enum PlacementMode
    {
        BothSides,
        LeftOnly,
        RightOnly,
        Alternating,
    }

    [Header("Spline Accuracy")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField, Min(0)] private int splineIndex;
    [Tooltip("Every knot-to-knot curve is divided by at least this amount.")]
    [SerializeField, Range(4, 48)] private int minimumSegmentsPerCurve = 16;
    [Tooltip("Long curves receive more subdivisions so this length is never exceeded.")]
    [SerializeField, Range(0.10f, 2.0f)] private float maximumSegmentLength = 0.35f;
    [SerializeField, Range(4, 32)] private int curveLengthProbeSteps = 12;
    [SerializeField] private bool useSplineUpVector;
    [SerializeField] private float heightOffset = 0.03f;
    [SerializeField] private float lateralOffset;

    [Header("Plain Walkway")]
    [SerializeField, Min(1.0f)] private float width = 4.0f;
    [SerializeField, Min(0.02f)] private float thickness = 0.12f;
    [InspectorName("Walkway Surface Material (Assign Here)")]
    [Tooltip("Drag the actual Unity material for the walkway surface into this slot.")]
    [SerializeField] private Material walkwayMaterial;
    [SerializeField, Min(0.1f)] private float uvMetersPerTile = 1.0f;
    [SerializeField] private bool generateMeshCollider = true;

    [Header("Tree Prefab Repetition")]
    [SerializeField] private bool generateTrees = true;
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private PlacementMode treePlacement = PlacementMode.BothSides;
    [SerializeField, Range(2.0f, 50.0f)] private float treeSpacing = 10.0f;
    [SerializeField, Range(0.0f, 10.0f)] private float treeOffsetFromWalkway = 1.2f;
    [Tooltip("Positive values move trees upward along the walkway normal.")]
    [SerializeField, Range(-10.0f, 10.0f)] private float treeHeightOffset;
    [SerializeField, Range(0.1f, 5.0f)] private float treeScale = 1.0f;
    [SerializeField] private Vector3 treeRotationOffset;
    [InspectorName("Tree Leaves Material")]
    [SerializeField] private Material treeLeavesMaterial;
    [InspectorName("Tree Trunk Material")]
    [SerializeField] private Material treeTrunkMaterial;
    [Tooltip("Used only when the original FBX material names do not identify bark or leaves.")]
    [SerializeField, Min(0)] private int treeTrunkFallbackSlotIndex;

    [Header("Bench Prefab Repetition")]
    [SerializeField] private bool generateBenches = true;
    [SerializeField] private GameObject benchPrefab;
    [SerializeField] private PlacementMode benchPlacement = PlacementMode.Alternating;
    [SerializeField, Range(2.0f, 80.0f)] private float benchSpacing = 24.0f;
    [SerializeField, Range(0.0f, 10.0f)] private float benchOffsetFromWalkway = 1.0f;
    [Tooltip("Positive values move benches upward along the walkway normal.")]
    [SerializeField, Range(-10.0f, 10.0f)] private float benchHeightOffset;
    [SerializeField, Range(0.1f, 5.0f)] private float benchScale = 1.0f;
    [SerializeField] private Vector3 benchRotationOffset;
    [SerializeField] private bool benchesFaceWalkway = true;
    [InspectorName("Bench Wood Material")]
    [SerializeField] private Material benchWoodMaterial;
    [InspectorName("Bench Metal Material")]
    [SerializeField] private Material benchMetalMaterial;

    [Header("Shared Placement")]
    [SerializeField, Min(0.0f)] private float startClearance = 3.0f;
    [SerializeField, Min(0.0f)] private float endClearance = 3.0f;
    [SerializeField] private bool rebuildAutomatically = true;

    private Mesh generatedMesh;
    private Transform generatedInstancesRoot;
    private int lastSplineHash;

    private struct Sample
    {
        public Vector3 position;
        public Vector3 tangent;
        public Vector3 side;
        public Vector3 up;
        public float distance;
    }

    public float Width
    {
        get => width;
        set
        {
            width = Mathf.Max(1.0f, value);
            Rebuild();
        }
    }

    private void Reset()
    {
        splineContainer = GetComponent<SplineContainer>();
        Rebuild();
    }

    private void OnEnable()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
        Rebuild();
    }

    private void OnDisable()
    {
        DestroyGeneratedContent();
    }

    private void Update()
    {
        if (Application.isPlaying || !rebuildAutomatically || !isActiveAndEnabled)
            return;
        if (!TryGetSpline(out Spline spline))
            return;

        int currentHash = spline.GetHashCode();
        if (currentHash != lastSplineHash)
            Rebuild();
    }

    private void OnValidate()
    {
        ValidateSettings();
        if (!rebuildAutomatically || !isActiveAndEnabled)
            return;

#if UNITY_EDITOR
        EditorApplication.delayCall -= DelayedRebuild;
        EditorApplication.delayCall += DelayedRebuild;
#else
        Rebuild();
#endif
    }

#if UNITY_EDITOR
    private void DelayedRebuild()
    {
        if (this != null && isActiveAndEnabled)
            Rebuild();
    }
#endif

    private void ValidateSettings()
    {
        width = Mathf.Max(1.0f, width);
        thickness = Mathf.Max(0.02f, thickness);
        minimumSegmentsPerCurve = Mathf.Clamp(minimumSegmentsPerCurve, 4, 48);
        maximumSegmentLength = Mathf.Clamp(maximumSegmentLength, 0.10f, 2.0f);
        curveLengthProbeSteps = Mathf.Clamp(curveLengthProbeSteps, 4, 32);
        uvMetersPerTile = Mathf.Max(0.1f, uvMetersPerTile);
        treeSpacing = Mathf.Max(2.0f, treeSpacing);
        benchSpacing = Mathf.Max(2.0f, benchSpacing);
        startClearance = Mathf.Max(0.0f, startClearance);
        endClearance = Mathf.Max(0.0f, endClearance);
    }

    [ContextMenu("Rebuild Walkway, Trees and Benches")]
    public void Rebuild()
    {
        ValidateSettings();
        if (!TryGetSpline(out Spline spline))
            return;

        lastSplineHash = spline.GetHashCode();
        List<float> parameters = BuildCurveFaithfulParameters(spline);
        List<Sample> samples = BuildSamples(spline, parameters);
        if (samples.Count < 2 || samples[^1].distance <= 0.01f)
            return;

        DestroyGeneratedContent();
        BuildPlainWalkway(samples);
        CreateInstancesRoot();

        if (generateTrees && treePrefab != null)
            GeneratePrefabSeries(samples, treePrefab, "Tree", treePlacement,
                treeSpacing, treeOffsetFromWalkway, treeHeightOffset, treeScale,
                treeRotationOffset, false);

        if (generateBenches && benchPrefab != null)
            GeneratePrefabSeries(samples, benchPrefab, "Bench", benchPlacement,
                benchSpacing, benchOffsetFromWalkway, benchHeightOffset, benchScale,
                benchRotationOffset, benchesFaceWalkway);
    }

    private bool TryGetSpline(out Spline spline)
    {
        spline = null;
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null || splineContainer.Splines == null ||
            splineContainer.Splines.Count == 0)
            return false;

        splineIndex = Mathf.Clamp(splineIndex, 0, splineContainer.Splines.Count - 1);
        spline = splineContainer.Splines[splineIndex];
        return spline != null && spline.Count >= 2;
    }

    private List<float> BuildCurveFaithfulParameters(Spline spline)
    {
        int curveCount = spline.Closed ? spline.Count : spline.Count - 1;
        var parameters = new List<float>(curveCount * minimumSegmentsPerCurve + 1)
        {
            0.0f,
        };

        for (int curveIndex = 0; curveIndex < curveCount; curveIndex++)
        {
            float curveStart = curveIndex / (float)curveCount;
            float curveEnd = (curveIndex + 1) / (float)curveCount;
            float curveLength = EstimateCurveLength(spline, curveStart, curveEnd);
            int subdivisions = Mathf.Max(
                minimumSegmentsPerCurve,
                Mathf.CeilToInt(curveLength / maximumSegmentLength));

            for (int step = 1; step <= subdivisions; step++)
            {
                float localT = step / (float)subdivisions;
                parameters.Add(Mathf.Lerp(curveStart, curveEnd, localT));
            }
        }

        return parameters;
    }

    private float EstimateCurveLength(Spline spline, float startT, float endT)
    {
        float length = 0.0f;
        Vector3 previous = EvaluateWorldPosition(spline, startT);
        for (int i = 1; i <= curveLengthProbeSteps; i++)
        {
            float t = Mathf.Lerp(startT, endT, i / (float)curveLengthProbeSteps);
            Vector3 current = EvaluateWorldPosition(spline, t);
            length += Vector3.Distance(previous, current);
            previous = current;
        }
        return length;
    }

    private List<Sample> BuildSamples(Spline spline, List<float> parameters)
    {
        var samples = new List<Sample>(parameters.Count);
        var worldPositions = new List<Vector3>(parameters.Count);
        var worldUps = new List<Vector3>(parameters.Count);
        Vector3 previousWorldPosition = Vector3.zero;
        Vector3 previousWorldSide = Vector3.zero;
        float distance = 0.0f;

        for (int i = 0; i < parameters.Count; i++)
        {
            float t = parameters[i];
            worldPositions.Add(EvaluateWorldPosition(spline, t));
            worldUps.Add(useSplineUpVector ? EvaluateWorldUp(spline, t) : Vector3.up);
        }

        for (int i = 0; i < parameters.Count; i++)
        {
            Vector3 worldPosition = worldPositions[i];
            Vector3 worldUp = worldUps[i];
            Vector3 worldTangent;

            // Match the stable corner-frame method used by the previous walkway:
            // derive direction from neighbouring sampled positions instead of
            // trusting a knot tangent that can be discontinuous at sharp corners.
            if (spline.Closed && parameters.Count > 3)
            {
                int previousIndex = i == 0 ? parameters.Count - 2 : i - 1;
                int nextIndex = i == parameters.Count - 1 ? 1 : i + 1;
                worldTangent = worldPositions[nextIndex] - worldPositions[previousIndex];
            }
            else if (i == 0)
            {
                worldTangent = worldPositions[1] - worldPositions[0];
            }
            else if (i == parameters.Count - 1)
            {
                worldTangent = worldPositions[i] - worldPositions[i - 1];
            }
            else
            {
                worldTangent = worldPositions[i + 1] - worldPositions[i - 1];
            }

            if (!useSplineUpVector)
                worldTangent.y = 0.0f;
            if (worldTangent.sqrMagnitude < 0.000001f)
                worldTangent = i > 0
                    ? worldPositions[i] - worldPositions[i - 1]
                    : Vector3.forward;
            worldTangent.Normalize();
            worldUp = Vector3.ProjectOnPlane(worldUp, worldTangent).normalized;
            if (worldUp.sqrMagnitude < 0.000001f)
                worldUp = Vector3.up;

            Vector3 worldSide = Vector3.Cross(worldUp, worldTangent).normalized;
            if (worldSide.sqrMagnitude < 0.000001f)
                worldSide = Vector3.right;

            // Keep the ribbon frame continuous. Spline tangents can flip at a knot,
            // which otherwise swaps left/right and creates long triangular spikes.
            if (i > 0 && Vector3.Dot(previousWorldSide, worldSide) < 0.0f)
                worldSide = -worldSide;

            worldPosition += worldUp * heightOffset + worldSide * lateralOffset;
            if (i > 0)
                distance += Vector3.Distance(previousWorldPosition, worldPosition);

            samples.Add(new Sample
            {
                position = transform.InverseTransformPoint(worldPosition),
                tangent = transform.InverseTransformDirection(worldTangent).normalized,
                side = transform.InverseTransformDirection(worldSide).normalized,
                up = transform.InverseTransformDirection(worldUp).normalized,
                distance = distance,
            });
            previousWorldPosition = worldPosition;
            previousWorldSide = worldSide;
        }

        return samples;
    }

    private void BuildPlainWalkway(List<Sample> samples)
    {
        int sampleCount = samples.Count;
        var vertices = new List<Vector3>(sampleCount * 8);
        var normals = new List<Vector3>(sampleCount * 8);
        var uvs = new List<Vector2>(sampleCount * 8);
        var triangles = new List<int>((sampleCount - 1) * 24);
        float halfWidth = width * 0.5f;

        // Four independent vertex strips keep top, sides and bottom sharp while
        // sharing vertices along the length, so adjacent curves cannot crack.
        for (int strip = 0; strip < 4; strip++)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                Sample sample = samples[i];
                Vector3 left = sample.position - sample.side * halfWidth;
                Vector3 right = sample.position + sample.side * halfWidth;
                Vector3 bottomLeft = left - sample.up * thickness;
                Vector3 bottomRight = right - sample.up * thickness;
                float v = sample.distance / uvMetersPerTile;

                switch (strip)
                {
                    case 0: // top: left, right
                        AddStripPair(vertices, normals, uvs, left, right,
                            sample.up, WorldPlanarUv(left), WorldPlanarUv(right));
                        break;
                    case 1: // left side: bottom, top
                        AddStripPair(vertices, normals, uvs, bottomLeft, left,
                            -sample.side, new Vector2(0.0f, v),
                            new Vector2(thickness / uvMetersPerTile, v));
                        break;
                    case 2: // right side: top, bottom
                        AddStripPair(vertices, normals, uvs, right, bottomRight,
                            sample.side, new Vector2(0.0f, v),
                            new Vector2(thickness / uvMetersPerTile, v));
                        break;
                    default: // bottom: right, left
                        AddStripPair(vertices, normals, uvs, bottomRight, bottomLeft,
                            -sample.up, new Vector2(0.0f, v),
                            new Vector2(width / uvMetersPerTile, v));
                        break;
                }
            }
        }

        for (int strip = 0; strip < 4; strip++)
        {
            int stripStart = strip * sampleCount * 2;
            for (int i = 0; i < sampleCount - 1; i++)
            {
                int a = stripStart + i * 2;
                int b = a + 1;
                int c = stripStart + (i + 1) * 2 + 1;
                int d = stripStart + (i + 1) * 2;

                // Correct outward-facing winding: a-d-c and a-c-b.
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(c);
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
            }
        }

        generatedMesh = new Mesh
        {
            name = $"{name}_PlainSplineWalkway",
            indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
        };
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetNormals(normals);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(triangles, 0, true);
        generatedMesh.RecalculateBounds();
        generatedMesh.RecalculateTangents();

        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterials = walkwayMaterial != null
            ? new[] { walkwayMaterial }
            : System.Array.Empty<Material>();
        if (walkwayMaterial == null)
        {
            Debug.LogError(
                "AdjustableSplineWalkway: Assign a material to " +
                "'Walkway Surface Material (Assign Here)'.",
                this);
        }
        UpdateCollider();
    }

    private void CreateInstancesRoot()
    {
        var root = new GameObject("_Generated_Trees_And_Benches");
        generatedInstancesRoot = root.transform;
        generatedInstancesRoot.SetParent(transform, false);
    }

    private void GeneratePrefabSeries(List<Sample> samples, GameObject prefab,
        string prefix, PlacementMode placementMode, float spacing, float offset,
        float height, float scale, Vector3 rotationOffset, bool faceWalkway)
    {
        float totalLength = samples[^1].distance;
        float sideDistance = width * 0.5f + offset;
        int instanceIndex = 0;

        for (float distance = startClearance + spacing * 0.5f;
             distance < totalLength - endClearance;
             distance += spacing)
        {
            Sample sample = SampleAtDistance(samples, distance);
            foreach (int sideSign in GetPlacementSides(placementMode, instanceIndex))
            {
                Vector3 position = sample.position + sample.side * sideDistance * sideSign +
                                   sample.up * height;
                Quaternion baseRotation;
                if (faceWalkway)
                {
                    Vector3 towardWalkway = sideSign < 0 ? sample.side : -sample.side;
                    baseRotation = Quaternion.LookRotation(towardWalkway, sample.up);
                }
                else
                {
                    baseRotation = Quaternion.LookRotation(sample.tangent, sample.up);
                }

                Quaternion rotation = baseRotation * Quaternion.Euler(rotationOffset);
                InstantiatePrefab(prefab, $"{prefix}_{instanceIndex:000}_{sideSign}",
                    position, rotation, scale);
            }
            instanceIndex++;
        }
    }

    private static IEnumerable<int> GetPlacementSides(PlacementMode mode, int index)
    {
        switch (mode)
        {
            case PlacementMode.BothSides:
                return new[] { -1, 1 };
            case PlacementMode.LeftOnly:
                return new[] { -1 };
            case PlacementMode.RightOnly:
                return new[] { 1 };
            default:
                return new[] { index % 2 == 0 ? -1 : 1 };
        }
    }

    private void InstantiatePrefab(GameObject prefab, string objectName,
        Vector3 localPosition, Quaternion localRotation, float uniformScale)
    {
        GameObject instance;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, generatedInstancesRoot);
        else
            instance = Instantiate(prefab, generatedInstancesRoot);
#else
        instance = Instantiate(prefab, generatedInstancesRoot);
#endif
        instance.name = objectName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = Vector3.one * uniformScale;

        if (objectName.StartsWith("Tree_"))
            ApplyTreeMaterials(instance);
        else if (objectName.StartsWith("Bench_"))
            ApplyBenchMaterials(instance);
    }

    private void ApplyTreeMaterials(GameObject treeInstance)
    {
        Renderer[] renderers = treeInstance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] original = renderer.sharedMaterials;
            if (original == null || original.Length == 0)
                continue;

            var replacement = new Material[original.Length];
            for (int slot = 0; slot < original.Length; slot++)
            {
                Material originalMaterial = original[slot];
                string slotIdentity = BuildMaterialIdentity(
                    renderer, originalMaterial, original.Length == 1);

                bool explicitlyLeaves = IsLeafMaterial(originalMaterial) || ContainsAny(
                    slotIdentity, "leaf", "leaves", "foliage", "green", "broadleaf", "crown");
                bool explicitlyTrunk = ContainsAny(
                    slotIdentity, "trunk", "bark", "branch", "stem", "wood");

                if (explicitlyLeaves && !explicitlyTrunk)
                    replacement[slot] = treeLeavesMaterial;
                else if (explicitlyTrunk && !explicitlyLeaves)
                    replacement[slot] = treeTrunkMaterial;
                else if (original.Length == 1)
                    replacement[slot] = explicitlyTrunk ? treeTrunkMaterial : treeLeavesMaterial;
                else
                    replacement[slot] = slot == treeTrunkFallbackSlotIndex
                        ? treeTrunkMaterial
                        : treeLeavesMaterial;

                if (replacement[slot] == null)
                    replacement[slot] = originalMaterial;
            }
            renderer.sharedMaterials = replacement;
        }
    }

    private static bool IsLeafMaterial(Material material)
    {
        if (material == null)
            return false;
        if (material.IsKeywordEnabled("_ALPHATEST_ON"))
            return true;
        if (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f)
            return true;
        if (material.HasProperty("_Cutoff") && material.GetFloat("_Cutoff") > 0.0f &&
            material.renderQueue >= 2400 && material.renderQueue <= 2500)
            return true;
        return false;
    }

    private void ApplyBenchMaterials(GameObject benchInstance)
    {
        Renderer[] renderers = benchInstance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] current = renderer.sharedMaterials;
            if (current == null || current.Length == 0)
                continue;

            // A combined OBJ can arrive as one Renderer with 15 submesh slots.
            if (renderers.Length == 1 && current.Length >= 15)
            {
                var replacement = new Material[current.Length];
                for (int slot = 0; slot < replacement.Length; slot++)
                    replacement[slot] = IsBenchWoodSlot(slot)
                        ? benchWoodMaterial
                        : benchMetalMaterial;
                renderer.sharedMaterials = FillMissingWithExisting(replacement, current);
                continue;
            }

            bool isWood = IsBenchWoodRendererName(renderer.gameObject.name);
            ReplaceEveryMaterialSlot(renderer, isWood ? benchWoodMaterial : benchMetalMaterial);
        }
    }

    private static bool IsBenchWoodRendererName(string rendererName)
    {
        // These are the six brown plank groups found in the supplied Bench.obj.
        return rendererName == "___01" ||
               rendererName == "___01_(2)" ||
               rendererName == "___01_(2)_(2)" ||
               rendererName == "___01_(3)" ||
               rendererName == "___01_(1)_(2)" ||
               rendererName == "___01_(3)_(2)";
    }

    private static bool IsBenchWoodSlot(int slot)
    {
        // OBJ group order: 0, 4, 5, 6, 8 and 12 are the six wooden planks.
        return slot == 0 || slot == 4 || slot == 5 ||
               slot == 6 || slot == 8 || slot == 12;
    }

    private static string BuildMaterialIdentity(
        Renderer renderer, Material material, bool includeRendererName)
    {
        string identity = includeRendererName
            ? renderer.gameObject.name.ToLowerInvariant()
            : string.Empty;
        if (material != null)
            identity += " " + material.name.ToLowerInvariant();
        return identity;
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        foreach (string term in terms)
            if (text.Contains(term))
                return true;
        return false;
    }

    private static void ReplaceEveryMaterialSlot(Renderer renderer, Material target)
    {
        if (renderer == null || target == null)
            return;
        Material[] current = renderer.sharedMaterials;
        int slotCount = Mathf.Max(1, current == null ? 0 : current.Length);
        var replacement = new Material[slotCount];
        for (int i = 0; i < replacement.Length; i++)
            replacement[i] = target;
        renderer.sharedMaterials = replacement;
    }

    private static Material[] FillMissingWithExisting(Material[] replacement, Material[] existing)
    {
        for (int i = 0; i < replacement.Length; i++)
        {
            if (replacement[i] == null && i < existing.Length)
                replacement[i] = existing[i];
        }
        return replacement;
    }

    private Sample SampleAtDistance(List<Sample> samples, float targetDistance)
    {
        int low = 0;
        int high = samples.Count - 1;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (samples[mid].distance < targetDistance)
                low = mid + 1;
            else
                high = mid;
        }

        int nextIndex = Mathf.Clamp(low, 1, samples.Count - 1);
        Sample a = samples[nextIndex - 1];
        Sample b = samples[nextIndex];
        float range = Mathf.Max(0.0001f, b.distance - a.distance);
        float t = Mathf.Clamp01((targetDistance - a.distance) / range);
        return new Sample
        {
            position = Vector3.Lerp(a.position, b.position, t),
            tangent = Vector3.Slerp(a.tangent, b.tangent, t).normalized,
            side = Vector3.Slerp(a.side, b.side, t).normalized,
            up = Vector3.Slerp(a.up, b.up, t).normalized,
            distance = targetDistance,
        };
    }

    private void UpdateCollider()
    {
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (!generateMeshCollider)
        {
            if (meshCollider != null)
                DestroyGeneratedObject(meshCollider);
            return;
        }

        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = generatedMesh;
    }

    private Vector3 EvaluateWorldPosition(Spline spline, float t)
    {
        float3 local = SplineUtility.EvaluatePosition(spline, Mathf.Clamp01(t));
        return splineContainer.transform.TransformPoint(new Vector3(local.x, local.y, local.z));
    }

    private Vector3 EvaluateWorldTangent(Spline spline, float t)
    {
        float3 local = SplineUtility.EvaluateTangent(spline, Mathf.Clamp01(t));
        return splineContainer.transform.TransformDirection(new Vector3(local.x, local.y, local.z));
    }

    private Vector3 EvaluateWorldUp(Spline spline, float t)
    {
        float3 local = SplineUtility.EvaluateUpVector(spline, Mathf.Clamp01(t));
        return splineContainer.transform.TransformDirection(new Vector3(local.x, local.y, local.z));
    }

    private void DestroyGeneratedContent()
    {
        if (generatedInstancesRoot != null)
            DestroyGeneratedObject(generatedInstancesRoot.gameObject);
        generatedInstancesRoot = null;

        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh == generatedMesh)
            filter.sharedMesh = null;
        if (generatedMesh != null)
            DestroyGeneratedObject(generatedMesh);
        generatedMesh = null;

    }

    private static void DestroyGeneratedObject(Object obj)
    {
        if (obj == null)
            return;
        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }

    private static void AddStripPair(List<Vector3> vertices, List<Vector3> normals,
        List<Vector2> uvs, Vector3 first, Vector3 second, Vector3 normal,
        Vector2 firstUv, Vector2 secondUv)
    {
        vertices.Add(first);
        vertices.Add(second);
        normals.Add(normal.normalized);
        normals.Add(normal.normalized);
        uvs.Add(firstUv);
        uvs.Add(secondUv);
    }

    private Vector2 WorldPlanarUv(Vector3 localPosition)
    {
        Vector3 worldPosition = transform.TransformPoint(localPosition);
        float tileSize = Mathf.Max(0.1f, uvMetersPerTile);
        return new Vector2(worldPosition.x / tileSize, worldPosition.z / tileSize);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AdjustableSplineWalkway))]
public sealed class AdjustableSplineWalkwayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8.0f);
        if (GUILayout.Button("Rebuild Walkway, Trees and Benches", GUILayout.Height(32.0f)))
        {
            var walkway = (AdjustableSplineWalkway)target;
            walkway.Rebuild();
            EditorUtility.SetDirty(walkway);
        }
    }
}
#endif
