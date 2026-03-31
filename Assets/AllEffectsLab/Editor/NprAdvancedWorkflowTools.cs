#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class NprAdvancedWorkflowTools
{
    private const string FusionShadowMaterialPath = "Assets/MaterialFX/Common_LitLibrary/M_Fusion_FootShadow.mat";
    private const string TurntableDir = "Assets/Screenshots/Turntable";

    [MenuItem("Tools/All Effects/NPR-11/Apply Eye Face Accent")]
    public static void ApplyEyeFaceAccent()
    {
        Material face = GetFaceMaterial();
        if (face == null)
        {
            Debug.LogWarning("[NPR-11] Face material not found. Run NPR-6 first.");
            return;
        }

        SetIf(face, "_ColorSaturation", 1.50f);
        SetIf(face, "_RampContrast", 1.20f);
        SetIf(face, "_FaceRegionWeight", 1.0f);
        SetIf(face, "_FaceShadowLift", 0.42f);
        SetIf(face, "_FaceForwardWrap", 0.46f);
        SetIf(face, "_FaceSpecBoost", 0.42f);
        SetIf(face, "_FaceRimSuppress", 0.80f);
        SetIf(face, "_ShadowTintStrength", 0.06f);
        SetIf(face, "_SpecThreshold", 0.986f);
        SetIf(face, "_SpecSoftness", 0.014f);
        SetIf(face, "_OutlineWidth", 2.2f);
        SetIf(face, "_OutlineSilhouetteBoost", 0.10f);
        if (face.HasProperty("_SpecColor")) face.SetColor("_SpecColor", new Color(0.95f, 0.98f, 1.0f, 1.0f));

        EditorUtility.SetDirty(face);
        AssetDatabase.SaveAssets();
        Debug.Log("[NPR-11] Eye/face accent applied.");
    }

    [MenuItem("Tools/All Effects/NPR-12/Apply Layered Hair Highlights")]
    public static void ApplyLayeredHairHighlights()
    {
        foreach (Material mat in GetCharacterNprMaterials())
        {
            if (!IsHair(mat))
                continue;

            SetIf(mat, "_ColorSaturation", 1.78f);
            SetIf(mat, "_RampContrast", 1.62f);
            SetIf(mat, "_RimStrength", 0.18f);
            SetIf(mat, "_HairSpecStrength", 1.78f);
            SetIf(mat, "_HairSpecShift", 0.16f);
            SetIf(mat, "_HairSpecExponent1", 58.0f);
            SetIf(mat, "_HairSpecExponent2", 14.0f);
            SetIf(mat, "_HairSpecSecondaryStrength", 0.76f);
            SetIf(mat, "_OutlineWidth", 3.5f);
            SetIf(mat, "_OutlineMinScale", 0.66f);
            if (mat.HasProperty("_ShadowCoolColor")) mat.SetColor("_ShadowCoolColor", new Color(0.66f, 0.80f, 1.0f, 1.0f));
            if (mat.HasProperty("_ShadowWarmColor")) mat.SetColor("_ShadowWarmColor", new Color(0.96f, 0.84f, 0.72f, 1.0f));

            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[NPR-12] Layered hair highlights applied.");
    }

    [MenuItem("Tools/All Effects/NPR-13/Apply Character Scene Fusion")]
    public static void ApplyCharacterSceneFusion()
    {
        GameObject character = GetCharacter();
        if (character == null)
        {
            Debug.LogWarning("[NPR-13] Character not found.");
            return;
        }

        Material shadowMat = GetOrCreateFusionShadowMaterial();
        if (shadowMat == null)
        {
            Debug.LogWarning("[NPR-13] Cannot create fusion shadow material.");
            return;
        }

        GameObject go = GameObject.Find("Fusion_FootShadow");
        if (go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Fusion_FootShadow";
            if (go.GetComponent<Collider>() != null)
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = shadowMat;

        Bounds b = GetCharacterBounds(character);
        Vector3 pos = b.center;
        pos.y = b.min.y + 0.006f;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        float radius = Mathf.Max(b.extents.x, b.extents.z) * 1.2f;
        go.transform.localScale = new Vector3(radius, radius, 1.0f);

        Material floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/MaterialFX/Common_LitLibrary/M_CharacterFloor.mat");
        if (floor != null && floor.HasProperty("_BaseColor"))
        {
            floor.SetColor("_BaseColor", new Color(0.30f, 0.35f, 0.42f, 1.0f));
            EditorUtility.SetDirty(floor);
        }

        EditorUtility.SetDirty(go);
        AssetDatabase.SaveAssets();
        Debug.Log("[NPR-13] Character scene fusion applied.");
    }

    [MenuItem("Tools/All Effects/NPR-14/Lighting Preset Daylight")]
    public static void ApplyLightingDaylight() => ApplyLightingPreset(new Color(1.0f, 0.99f, 0.96f, 1.0f), 1.15f, 0.56f, 52.0f, 18.0f, 0.42f, 0.44f);

    [MenuItem("Tools/All Effects/NPR-14/Lighting Preset Evening")]
    public static void ApplyLightingEvening() => ApplyLightingPreset(new Color(1.0f, 0.86f, 0.74f, 1.0f), 1.08f, 0.62f, 34.0f, -18.0f, 0.30f, 0.56f);

    [MenuItem("Tools/All Effects/NPR-14/Lighting Preset Stage")]
    public static void ApplyLightingStage() => ApplyLightingPreset(new Color(0.88f, 0.94f, 1.0f, 1.0f), 1.30f, 0.50f, 62.0f, 36.0f, 0.20f, 0.68f);

    [MenuItem("Tools/All Effects/NPR-15/Optimize Character Texture Imports")]
    public static void OptimizeCharacterTextureImports()
    {
        var materials = GetCharacterNprMaterials();
        var texturePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Material mat in materials)
        {
            if (mat == null) continue;
            Texture baseMap = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            Texture normalMap = mat.HasProperty("_NormalMap") ? mat.GetTexture("_NormalMap") : null;
            AddTexturePath(texturePaths, baseMap);
            AddTexturePath(texturePaths, normalMap);
        }

        int updated = 0;
        foreach (string path in texturePaths)
        {
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            bool changed = false;
            if (ti.textureCompression != TextureImporterCompression.Uncompressed)
            {
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (ti.crunchedCompression)
            {
                ti.crunchedCompression = false;
                changed = true;
            }
            if (ti.anisoLevel < 8)
            {
                ti.anisoLevel = 8;
                changed = true;
            }
            if (ti.maxTextureSize < 2048)
            {
                ti.maxTextureSize = 2048;
                changed = true;
            }
            if (ti.filterMode != FilterMode.Bilinear)
            {
                ti.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            if (changed)
            {
                ti.SaveAndReimport();
                updated++;
            }
        }

        Debug.Log($"[NPR-15] Texture import optimization complete. Updated: {updated}");
    }

    [MenuItem("Tools/All Effects/NPR-16/Capture Turntable 12 Shots")]
    public static void CaptureTurntable12Shots()
    {
        GameObject character = GetCharacter();
        if (character == null)
        {
            Debug.LogWarning("[NPR-16] Character not found.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Screenshots"))
            AssetDatabase.CreateFolder("Assets", "Screenshots");
        if (!AssetDatabase.IsValidFolder(TurntableDir))
            AssetDatabase.CreateFolder("Assets/Screenshots", "Turntable");

        Bounds b = GetCharacterBounds(character);
        Vector3 focus = b.center + new Vector3(0.0f, b.extents.y * 0.30f, 0.0f);
        float dist = Mathf.Max(b.extents.x, b.extents.z) * 3.1f + 0.9f;
        float height = focus.y + b.extents.y * 0.30f;

        GameObject camGo = new GameObject("Temp_TurntableCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 42.0f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200.0f;

        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30.0f;
            Vector3 dir = Quaternion.Euler(0.0f, angle, 0.0f) * Vector3.back;
            cam.transform.position = focus + dir * dist;
            cam.transform.position = new Vector3(cam.transform.position.x, height, cam.transform.position.z);
            cam.transform.LookAt(focus);

            string path = $"{TurntableDir}/NPR16-Turntable-{stamp}-{i:00}.png";
            CaptureFromCamera(cam, path, 1600, 900);
        }

        UnityEngine.Object.DestroyImmediate(camGo);
        AssetDatabase.Refresh();
        Debug.Log("[NPR-16] Turntable capture complete (12 shots).");
    }

    private static void ApplyLightingPreset(Color dirColor, float dirIntensity, float dirShadow, float pitch, float yaw, float fillIntensity, float rimIntensity)
    {
        Light dir = FindLight("Directional Light");
        Light fill = FindLight("Character_FillPointLight");
        Light rim = FindLight("Character_RimPointLight");
        if (dir == null || fill == null || rim == null)
        {
            Debug.LogWarning("[NPR-14] Missing lights.");
            return;
        }

        dir.transform.rotation = Quaternion.Euler(pitch, yaw, 0.0f);
        dir.intensity = dirIntensity;
        dir.color = dirColor;
        dir.shadowStrength = dirShadow;
        fill.intensity = fillIntensity;
        rim.intensity = rimIntensity;

        EditorUtility.SetDirty(dir.transform);
        EditorUtility.SetDirty(dir);
        EditorUtility.SetDirty(fill);
        EditorUtility.SetDirty(rim);
        Debug.Log("[NPR-14] Lighting preset applied.");
    }

    private static Material GetOrCreateFusionShadowMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(FusionShadowMaterialPath);
        if (mat == null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) return null;
            mat = new Material(unlit);
            AssetDatabase.CreateAsset(mat, FusionShadowMaterialPath);
        }

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.0f, 0.0f, 0.0f, 0.30f));
        mat.SetFloat("_Surface", 1.0f);
        mat.SetFloat("_Blend", 0.0f);
        mat.SetFloat("_ZWrite", 0.0f);
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static List<Material> GetCharacterNprMaterials()
    {
        var result = new List<Material>();
        GameObject character = GetCharacter();
        if (character == null)
            return result;

        var unique = new HashSet<Material>();
        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].sharedMaterials;
            for (int j = 0; j < mats.Length; j++)
            {
                Material m = mats[j];
                if (m != null && m.shader != null && m.shader.name.Contains("NPR-") && unique.Add(m))
                    result.Add(m);
            }
        }

        return result;
    }

    private static Material GetFaceMaterial()
    {
        GameObject character = GetCharacter();
        if (character == null)
            return null;

        SkinnedMeshRenderer head = character.transform.Find("Model_Char_Main_Head")?.GetComponent<SkinnedMeshRenderer>();
        if (head != null && head.sharedMaterials != null && head.sharedMaterials.Length > 0)
        {
            Material m = head.sharedMaterials[0];
            if (m != null && m.name.Contains("_Face_NPR6"))
                return m;
        }

        foreach (Material mat in GetCharacterNprMaterials())
        {
            if (mat.name.Contains("_Face_NPR6"))
                return mat;
        }

        return null;
    }

    private static GameObject GetCharacter()
    {
        GameObject character = GameObject.Find("Player_Girl_Test");
        if (character == null)
            character = GameObject.Find("Player_Girl");
        return character;
    }

    private static bool IsHair(Material mat)
    {
        return mat != null && mat.name.ToLowerInvariant().Contains("2_npr3");
    }

    private static void SetIf(Material mat, string prop, float value)
    {
        if (mat != null && mat.HasProperty(prop))
            mat.SetFloat(prop, value);
    }

    private static Light FindLight(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Light>() : null;
    }

    private static void AddTexturePath(HashSet<string> set, Texture tex)
    {
        if (tex == null) return;
        string path = AssetDatabase.GetAssetPath(tex);
        if (!string.IsNullOrEmpty(path))
            set.Add(path);
    }

    private static Bounds GetCharacterBounds(GameObject character)
    {
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return new Bounds(character.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    private static void CaptureFromCamera(Camera cam, string outputPath, int width, int height)
    {
        RenderTexture prevRT = cam.targetTexture;
        RenderTexture activeRT = RenderTexture.active;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply(false, false);

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);

        cam.targetTexture = prevRT;
        RenderTexture.active = activeRT;
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
    }
}
#endif
