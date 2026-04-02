using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FurinaGenshinMaterialSetup
{
    private const string CharacterTextureRoot = "Assets/_Model/Genshin_Furina_MMD/Converted/Textures";
    private const string GeneratedMapRoot = "Assets/_Model/Genshin_Furina_MMD/Converted/GeneratedMaps/Genshin";

    [MenuItem("Tools/NPR 风格/芙宁娜-应用原神材质")]
    public static void ApplyGenshinLook()
    {
        const string rendererPath = "CharacterRenderTest_Root/_10_角色/芙宁/网格";
        GameObject go = GameObject.Find(rendererPath);
        if (go == null)
        {
            Debug.LogError("[Furina] 未找到网格对象: " + rendererPath);
            return;
        }

        SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogError("[Furina] 网格对象缺少 SkinnedMeshRenderer");
            return;
        }

        Shader shader = Shader.Find("Custom/NPR-3/GenshinURP");
        if (shader == null)
        {
            Debug.LogError("[Furina] 未找到 Shader: Custom/NPR-3/GenshinURP");
            return;
        }

        Texture2D ramp = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MaterialFX/NPR/NPR-Core/Textures/TEX_NPR3_Default_Ramp.png");
        EnsureFolder("Assets/_Model/Genshin_Furina_MMD/Converted", "GeneratedMaps");
        EnsureFolder("Assets/_Model/Genshin_Furina_MMD/Converted/GeneratedMaps", "Genshin");
        EnsureCharacterMaps();

        Texture2D faceLM = LoadTextureWithFallback(
            GeneratedMapRoot + "/TEX_Furina_GI_Face_LightMap.png",
            "Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps/Genshin/TEX_NPR3_Face_LightMap.png");
        Texture2D bodyLM = LoadTextureWithFallback(
            GeneratedMapRoot + "/TEX_Furina_GI_Body_LightMap.png",
            "Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps/Genshin/TEX_NPR3_Body_LightMap.png");
        Texture2D hairLM = LoadTextureWithFallback(
            GeneratedMapRoot + "/TEX_Furina_GI_Hair_LightMap.png",
            "Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps/Genshin/TEX_NPR3_Hair_LightMap.png");
        Texture2D faceS = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps/Genshin/TEX_NPR3_Face_FaceShadow.png");
        Texture2D bodyS = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps/Genshin/TEX_NPR3_Body_FaceShadow.png");
        Texture2D hairS = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps/Genshin/TEX_NPR3_Hair_FaceShadow.png");
        Texture2D faceO = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps/Genshin/TEX_NPR3_Face_OutlineWidth.png");
        Texture2D bodyO = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps/Genshin/TEX_NPR3_Body_OutlineWidth.png");
        Texture2D hairO = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps/Genshin/TEX_NPR3_Hair_OutlineWidth.png");

        EnsureFolder("Assets/_Model/Genshin_Furina_MMD/Converted", "MaterialsCN");
        string matDir = "Assets/_Model/Genshin_Furina_MMD/Converted/MaterialsCN";

        Material[] src = smr.sharedMaterials;
        Material[] dst = new Material[src.Length];

        string[] cnNames = {
            "颜面","口舌","发丝","发梢","肤体","衣饰","裙摆","装饰",
            "高光","透层","细节","配件","鞋袜","眼瞳","眉睫","帽饰",
            "领结","纹理","边饰","底衬","附饰","流苏","神眼","袖口",
            "衣摆","暗纹","亮纹","珠饰","花边","扣饰","渐层","描边"
        };

        for (int i = 0; i < src.Length; i++)
        {
            Material sm = src[i];
            if (sm == null)
            {
                continue;
            }

            string matName = i < cnNames.Length ? cnNames[i] : ("材" + i.ToString("D2"));
            string path = matDir + "/" + matName + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sm);
                m.name = matName;
                AssetDatabase.CreateAsset(m, path);
            }

            // Preserve imported base texture if available
            Texture baseTex = sm.HasProperty("_MainTex") ? sm.GetTexture("_MainTex") : null;
            if (baseTex == null && sm.HasProperty("_BaseMap"))
            {
                baseTex = sm.GetTexture("_BaseMap");
            }

            m.shader = shader;
            if (m.HasProperty("_BaseMap") && baseTex != null) m.SetTexture("_BaseMap", baseTex);
            if (m.HasProperty("_RampMap") && ramp != null) m.SetTexture("_RampMap", ramp);

            Role role = ClassifyRole(matName);
            Texture2D lm = role == Role.Face ? faceLM : role == Role.Hair ? hairLM : bodyLM;
            Texture2D fs = role == Role.Face ? faceS : role == Role.Hair ? hairS : bodyS;
            Texture2D ow = role == Role.Face ? faceO : role == Role.Hair ? hairO : bodyO;
            if (m.HasProperty("_LightMap") && lm != null) m.SetTexture("_LightMap", lm);
            if (m.HasProperty("_FaceShadowMap") && fs != null) m.SetTexture("_FaceShadowMap", fs);
            if (m.HasProperty("_OutlineWidthMap") && ow != null) m.SetTexture("_OutlineWidthMap", ow);

            // Genshin-ish baseline tuning
            SetIf(m, "_ColorSaturation", 1.52f);
            SetIf(m, "_RampContrast", 1.32f);
            SetIf(m, "_RampBands", 4.0f);
            SetIf(m, "_RampStrength", 0.98f);
            SetIf(m, "_ShadowStrength", 0.82f);
            SetIf(m, "_AmbientStrength", 0.20f);
            SetIf(m, "_RimStrength", 0.09f);
            SetIf(m, "_RimPower", 3.4f);
            SetIf(m, "_ToonContrast", 1.04f);
            SetIf(m, "_ExposureCompensation", 1.00f);
            SetIf(m, "_ShadowStylizeStrength", 0.03f);
            SetIf(m, "_ShadowTerminatorWidth", 0.30f);
            SetIf(m, "_ShadowTerminatorSoftness", 0.26f);
            SetIf(m, "_ShadowTintStrength", 0.10f);
            SetColorIf(m, "_ShadowTintColor", new Color(0.88f, 0.91f, 0.97f, 1f));
            SetColorIf(m, "_ShadowCoolColor", new Color(0.82f, 0.88f, 0.97f, 1f));
            SetColorIf(m, "_ShadowWarmColor", new Color(0.98f, 0.90f, 0.82f, 1f));
            SetIf(m, "_SpecThreshold", role == Role.Hair ? 0.985f : 0.994f);
            SetIf(m, "_SpecSoftness", role == Role.Hair ? 0.020f : 0.010f);
            SetIf(m, "_HairSpecStrength", role == Role.Hair ? 0.55f : 0.0f);
            SetIf(m, "_HairSpecExponent1", role == Role.Hair ? 72f : 64f);
            SetIf(m, "_HairSpecExponent2", role == Role.Hair ? 18f : 16f);
            SetIf(m, "_HairSpecSecondaryStrength", role == Role.Hair ? 0.42f : 0.35f);
            SetIf(m, "_OutlineWidth", role == Role.Face ? 1.9f : role == Role.Hair ? 2.2f : 2.05f);
            SetIf(m, "_OutlineWidthMapStrength", role == Role.Face ? 0.72f : 0.62f);
            SetIf(m, "_OutlineUseVertexColorNormal", 0.0f); // imported model has no baked vcol normals
            SetIf(m, "_FaceRegionWeight", role == Role.Face ? 1.0f : 0.0f);
            SetIf(m, "_UseFaceShadowMap", role == Role.Face ? 1.0f : 0.0f);
            SetIf(m, "_FaceShadowMapStrength", role == Role.Face ? 1.0f : 0.0f);
            SetIf(m, "_FaceOrientationStrength", role == Role.Face ? 0.52f : 0.0f);

            EditorUtility.SetDirty(m);
            dst[i] = m;
        }

        smr.sharedMaterials = dst;
        EditorUtility.SetDirty(smr);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Furina] 已应用原神材质流程，材质槽: " + dst.Length);
    }

    private enum Role { Face, Body, Hair }

    private static Role ClassifyRole(string n)
    {
        if (n.Contains("发")) return Role.Hair;
        if (n.Contains("颜") || n.Contains("眼") || n.Contains("眉") || n.Contains("口")) return Role.Face;
        return Role.Body;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void SetIf(Material m, string p, float v)
    {
        if (m.HasProperty(p)) m.SetFloat(p, v);
    }

    private static void SetColorIf(Material m, string p, Color c)
    {
        if (m.HasProperty(p)) m.SetColor(p, c);
    }

    [MenuItem("Tools/NPR 风格/芙宁娜-贴图与槽位校验报告")]
    public static void ValidateFurinaBindings()
    {
        string[] mats = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Model/Genshin_Furina_MMD/Converted/MaterialsCN" });
        int ok = 0;
        int warn = 0;
        foreach (string guid in mats)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                continue;
            }

            bool hasBase = m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null;
            bool hasLight = m.HasProperty("_LightMap") && m.GetTexture("_LightMap") != null;
            bool hasRamp = m.HasProperty("_RampMap") && m.GetTexture("_RampMap") != null;
            bool hasOutline = m.HasProperty("_OutlineWidthMap") && m.GetTexture("_OutlineWidthMap") != null;
            bool pass = hasBase && hasLight && hasRamp && hasOutline;

            if (pass)
            {
                ok++;
            }
            else
            {
                warn++;
                Debug.LogWarning("[Furina][校验] 贴图槽可能缺失: " + path);
            }
        }

        Debug.Log("[Furina][校验] 完成: OK=" + ok + " WARN=" + warn);
    }

    private static Texture2D LoadTextureWithFallback(string preferredPath, string fallbackPath)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(preferredPath);
        if (tex != null)
        {
            return tex;
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(fallbackPath);
    }

    private static void EnsureCharacterMaps()
    {
        BuildPackedLightMap(
            "TEX_Furina_GI_Face_LightMap.png",
            CharacterTextureRoot + "/颜.png",
            CharacterTextureRoot + "/spa_h.png",
            0.15f,
            0.92f,
            1.20f);
        BuildPackedLightMap(
            "TEX_Furina_GI_Body_LightMap.png",
            CharacterTextureRoot + "/体.png",
            CharacterTextureRoot + "/spa_h.png",
            0.45f,
            0.82f,
            1.05f);
        BuildPackedLightMap(
            "TEX_Furina_GI_Hair_LightMap.png",
            CharacterTextureRoot + "/髮.png",
            CharacterTextureRoot + "/hair_s.bmp",
            0.80f,
            0.70f,
            1.35f);
    }

    private static void BuildPackedLightMap(
        string outName,
        string aoPath,
        string specPath,
        float materialId,
        float aoStrength,
        float specStrength)
    {
        string outPath = GeneratedMapRoot + "/" + outName;
        Texture2D aoTex = LoadReadableSource(aoPath);
        Texture2D specTex = LoadReadableSource(specPath);
        if (aoTex == null || specTex == null)
        {
            Debug.LogWarning("[Furina] 控制贴图源缺失，跳过生成: " + outName);
            return;
        }

        int w = aoTex.width;
        int h = aoTex.height;
        Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        Color[] outPixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                Color aoC = aoTex.GetPixelBilinear(u, v);
                Color spC = specTex.GetPixelBilinear(u, v);

                float lum = Mathf.Clamp01(aoC.grayscale);
                float ao = Mathf.Clamp01(0.55f + lum * 0.45f * aoStrength);
                float spec = Mathf.Clamp01(Mathf.Pow(spC.grayscale, 1.15f) * specStrength);

                outPixels[y * w + x] = new Color(spec, ao, spec * 0.92f, materialId);
            }
        }

        outTex.SetPixels(outPixels);
        outTex.Apply(false, false);
        System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(outPath), outTex.EncodeToPNG());
        Object.DestroyImmediate(outTex);
        AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceSynchronousImport);
        FixImporterAsLinear(outPath);
    }

    private static Texture2D LoadReadableSource(string path)
    {
        EnsureReadable(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void FixImporterAsLinear(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        if (importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            changed = true;
        }
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            changed = true;
        }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static void EnsureReadable(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }
}
