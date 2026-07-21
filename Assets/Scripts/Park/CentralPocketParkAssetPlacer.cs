using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class CentralPocketParkAssetPlacer : MonoBehaviour
{
    [Serializable]
    public sealed class PrefabGroup
    {
        public GameObject[] prefabs;
        public float heightOffset;
        public Vector3 rotationOffset;
        [Min(0.01f)] public float minimumScale = 1f;
        [Min(0.01f)] public float maximumScale = 1f;
        public bool randomizeYaw = true;
    }

    [Header("Socket Search")]
    [Tooltip("Leave empty to search below this GameObject.")]
    public Transform socketSearchRoot;

    [Header("Main Landmark Tree")]
    public GameObject mainTreePrefab;
    public float mainTreeHeightOffset;
    [Min(0.01f)] public float mainTreeScale = 1f;
    public Vector3 mainTreeRotationOffset;

    [Header("Optional Accent Trees")]
    public PrefabGroup accentTrees = new PrefabGroup
    {
        minimumScale = 0.65f,
        maximumScale = 0.80f,
    };

    [Header("Shrubs")]
    public PrefabGroup shrubs = new PrefabGroup
    {
        minimumScale = 0.75f,
        maximumScale = 1.05f,
    };

    [Header("Flowers / Ground Cover")]
    public PrefabGroup flowers = new PrefabGroup
    {
        minimumScale = 0.65f,
        maximumScale = 0.95f,
    };

    [Header("Generation")]
    public int randomSeed = 240715;

    private const string GeneratedRootName = "_Generated_Park_Assets";

    [ContextMenu("Rebuild Park Assets")]
    public void RebuildParkAssets()
    {
        ClearGeneratedParkAssets();

        Transform searchRoot = socketSearchRoot != null ? socketSearchRoot : transform;
        GameObject generatedObject = new GameObject(GeneratedRootName);
        generatedObject.transform.SetParent(transform, false);

        Transform mainSocket = FindSocket(searchRoot, "SOCKET_Main_Tree");
        if (mainSocket != null && mainTreePrefab != null)
        {
            CreateInstance(
                mainTreePrefab,
                mainSocket,
                generatedObject.transform,
                mainTreeHeightOffset,
                mainTreeScale,
                mainTreeRotationOffset,
                0f,
                "Main_Tree"
            );
        }

        System.Random random = new System.Random(randomSeed);
        GenerateGroup(searchRoot, "SOCKET_Accent_Tree_", "Accent_Tree",
            accentTrees, generatedObject.transform, random);
        GenerateGroup(searchRoot, "SOCKET_Shrub_", "Shrub",
            shrubs, generatedObject.transform, random);
        GenerateGroup(searchRoot, "SOCKET_Flower_", "Flower",
            flowers, generatedObject.transform, random);

        Debug.Log($"[{name}] Park prefab assets rebuilt.", this);
    }

    [ContextMenu("Clear Generated Park Assets")]
    public void ClearGeneratedParkAssets()
    {
        Transform existing = transform.Find(GeneratedRootName);
        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    private static void GenerateGroup(
        Transform searchRoot,
        string socketPrefix,
        string instancePrefix,
        PrefabGroup settings,
        Transform generatedRoot,
        System.Random random)
    {
        if (settings == null || settings.prefabs == null || settings.prefabs.Length == 0)
            return;

        List<Transform> sockets = FindSockets(searchRoot, socketPrefix);
        for (int index = 0; index < sockets.Count; index++)
        {
            GameObject prefab = PickPrefab(settings.prefabs, random);
            if (prefab == null)
                continue;

            float minimum = Mathf.Max(0.01f, settings.minimumScale);
            float maximum = Mathf.Max(minimum, settings.maximumScale);
            float scale = Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
            float yaw = settings.randomizeYaw
                ? Mathf.Lerp(0f, 360f, (float)random.NextDouble())
                : 0f;

            CreateInstance(
                prefab,
                sockets[index],
                generatedRoot,
                settings.heightOffset,
                scale,
                settings.rotationOffset,
                yaw,
                $"{instancePrefix}_{index + 1:00}"
            );
        }
    }

    private static GameObject PickPrefab(GameObject[] prefabs, System.Random random)
    {
        var valid = new List<GameObject>();
        for (int index = 0; index < prefabs.Length; index++)
        {
            if (prefabs[index] != null)
                valid.Add(prefabs[index]);
        }
        return valid.Count == 0 ? null : valid[random.Next(valid.Count)];
    }

    private static void CreateInstance(
        GameObject prefab,
        Transform socket,
        Transform generatedRoot,
        float heightOffset,
        float scale,
        Vector3 rotationOffset,
        float randomYaw,
        string instanceName)
    {
        GameObject instance;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, generatedRoot);
        else
            instance = Instantiate(prefab, generatedRoot);
#else
        instance = Instantiate(prefab, generatedRoot);
#endif
        instance.name = instanceName;
        instance.transform.position = socket.position + socket.up * heightOffset;
        instance.transform.rotation = socket.rotation *
            Quaternion.Euler(rotationOffset) * Quaternion.Euler(0f, randomYaw, 0f);
        instance.transform.localScale = Vector3.Scale(
            prefab.transform.localScale,
            Vector3.one * Mathf.Max(0.01f, scale)
        );
    }

    private static Transform FindSocket(Transform root, string exactName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < transforms.Length; index++)
        {
            if (transforms[index].name == exactName)
                return transforms[index];
        }
        return null;
    }

    private static List<Transform> FindSockets(Transform root, string prefix)
    {
        var result = new List<Transform>();
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int index = 0; index < transforms.Length; index++)
        {
            if (transforms[index].name.StartsWith(prefix, StringComparison.Ordinal))
                result.Add(transforms[index]);
        }
        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CentralPocketParkAssetPlacer))]
public sealed class CentralPocketParkAssetPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8f);

        CentralPocketParkAssetPlacer placer =
            (CentralPocketParkAssetPlacer)target;

        if (GUILayout.Button("Rebuild Park Assets", GUILayout.Height(34f)))
        {
            placer.RebuildParkAssets();
            EditorUtility.SetDirty(placer);
        }

        if (GUILayout.Button("Clear Generated Park Assets", GUILayout.Height(25f)))
        {
            placer.ClearGeneratedParkAssets();
            EditorUtility.SetDirty(placer);
        }
    }
}
#endif
