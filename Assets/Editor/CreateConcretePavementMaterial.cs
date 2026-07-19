using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CreateConcretePavementMaterial
{
    private const string Folder = "Assets/Materials/Walkway/ConcretePavement02";
    private const string MaterialPath = Folder + "/MAT_ConcretePavement02_URP.mat";

    static CreateConcretePavementMaterial()
    {
        EditorApplication.delayCall += CreateIfMissing;
    }

    [MenuItem("Tools/Semicon City/Create Concrete Pavement Material")]
    public static void CreateOrRefresh()
    {
        ConfigureTexture(Folder + "/ConcretePavement02_BaseColor_2K.jpg", false, true);
        ConfigureTexture(Folder + "/ConcretePavement02_NormalGL_2K.jpg", true, false);
        ConfigureTexture(Folder + "/ConcretePavement02_AO_2K.jpg", false, false);
        ConfigureTexture(Folder + "/ConcretePavement02_Roughness_2K.jpg", false, false);
        ConfigureTexture(Folder + "/ConcretePavement02_Height_2K.jpg", false, false);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("URP/Lit 셰이더를 찾지 못했습니다. URP 패키지와 Render Pipeline Asset을 확인하세요.");
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "MAT_ConcretePavement02_URP" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture2D baseMap = Load("ConcretePavement02_BaseColor_2K.jpg");
        Texture2D normalMap = Load("ConcretePavement02_NormalGL_2K.jpg");
        Texture2D aoMap = Load("ConcretePavement02_AO_2K.jpg");

        material.SetTexture("_BaseMap", baseMap);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.07f);

        material.SetTexture("_BumpMap", normalMap);
        material.SetFloat("_BumpScale", 0.45f);
        material.EnableKeyword("_NORMALMAP");

        material.SetTexture("_OcclusionMap", aoMap);
        material.SetFloat("_OcclusionStrength", 0.8f);
        material.EnableKeyword("_OCCLUSIONMAP");

        // Parallax produces unstable streaks at grazing angles on a long spline.
        // Keep the height texture in the project, but do not use it in URP/Lit.
        material.SetTexture("_ParallaxMap", null);
        material.SetFloat("_Parallax", 0.0f);
        material.DisableKeyword("_PARALLAXMAP");

        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = material;
        EditorGUIUtility.PingObject(material);
        Debug.Log("보도 재질 생성 완료: " + MaterialPath);
    }

    private static void CreateIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) == null &&
            Load("ConcretePavement02_BaseColor_2K.jpg") != null)
        {
            CreateOrRefresh();
        }
    }

    private static Texture2D Load(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + fileName);
    }

    private static void ConfigureTexture(string path, bool normalMap, bool sRgb)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool changed = false;
        TextureImporterType desiredType = normalMap
            ? TextureImporterType.NormalMap
            : TextureImporterType.Default;

        if (importer.textureType != desiredType)
        {
            importer.textureType = desiredType;
            changed = true;
        }

        if (importer.sRGBTexture != sRgb)
        {
            importer.sRGBTexture = sRgb;
            changed = true;
        }

        if (importer.wrapMode != TextureWrapMode.Repeat)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            changed = true;
        }

        if (importer.maxTextureSize != 2048)
        {
            importer.maxTextureSize = 2048;
            changed = true;
        }

        if (changed) importer.SaveAndReimport();
    }
}
