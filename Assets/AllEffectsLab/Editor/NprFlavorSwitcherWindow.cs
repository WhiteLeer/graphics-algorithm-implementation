using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NprFlavorSwitcherWindow : EditorWindow
{
    private const string BaseMaterialRoot = "Assets/MaterialFX/NPR/NPR-Core/Materials/CharacterConverted";
    private const string ProfileMaterialRoot = "Assets/MaterialFX/NPR/NPR-Core/Materials/Profiles";
    private const string ProfileTextureRoot = "Assets/MaterialFX/NPR/NPR-Core/Textures/Profiles";
    private const string GeneratedTextureRoot = "Assets/MaterialFX/NPR/NPR-Core/Textures/Generated";

    private static readonly StyleDef[] Styles =
    {
        new StyleDef("原神", "Genshin", "Assets/MaterialFX/NPR/NPR-Core/Shaders/Profiles/NPR3_GenshinURP.shader", 1.60f, 0.58f, 0.20f, 0.10f, new Color(0.74f, 0.84f, 1f, 1f), new Color(1f, 0.88f, 0.78f, 1f), new Color(0.08f, 0.11f, 0.16f, 1f)),
        new StyleDef("崩铁", "HSR", "Assets/MaterialFX/NPR/NPR-Core/Shaders/Profiles/NPR3_HSRURP.shader", 1.48f, 0.50f, 0.16f, 0.08f, new Color(0.68f, 0.78f, 0.95f, 1f), new Color(0.90f, 0.82f, 0.72f, 1f), new Color(0.10f, 0.12f, 0.18f, 1f)),
        new StyleDef("绝区零", "ZZZ", "Assets/MaterialFX/NPR/NPR-Core/Shaders/Profiles/NPR3_ZZZURP.shader", 1.55f, 0.55f, 0.20f, 0.12f, new Color(0.62f, 0.75f, 1f, 1f), new Color(1f, 0.80f, 0.62f, 1f), new Color(0.03f, 0.04f, 0.06f, 1f)),
    };

    private int _styleIndex;
    private bool _applyToSelection = true;

    [MenuItem("Tools/NPR 风味切换器")]
    public static void Open()
    {
        GetWindow<NprFlavorSwitcherWindow>("NPR 风味切换");
    }

    [MenuItem("Tools/NPR 风味/切换为原神(选中)")]
    public static void ApplyGenshinSelection()
    {
        SwitchFlavor(Styles[0], true);
    }

    [MenuItem("Tools/NPR 风味/切换为崩铁(选中)")]
    public static void ApplyHsrSelection()
    {
        SwitchFlavor(Styles[1], true);
    }

    [MenuItem("Tools/NPR 风味/切换为绝区零(选中)")]
    public static void ApplyZzzSelection()
    {
        SwitchFlavor(Styles[2], true);
    }

    [MenuItem("Tools/NPR 风味/切换为原神(全场景)")]
    public static void ApplyGenshinAll()
    {
        SwitchFlavor(Styles[0], false);
    }

    [MenuItem("Tools/NPR 风味/切换为崩铁(全场景)")]
    public static void ApplyHsrAll()
    {
        SwitchFlavor(Styles[1], false);
    }

    [MenuItem("Tools/NPR 风味/切换为绝区零(全场景)")]
    public static void ApplyZzzAll()
    {
        SwitchFlavor(Styles[2], false);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("整体渲染风味", EditorStyles.boldLabel);
        _styleIndex = EditorGUILayout.Popup("目标风味", _styleIndex, new[] { "原神", "崩铁", "绝区零" });
        _applyToSelection = EditorGUILayout.Toggle("仅作用于当前选中根节点", _applyToSelection);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("一键构建并切换"))
        {
            SwitchFlavor(Styles[_styleIndex], _applyToSelection);
        }

        EditorGUILayout.HelpBox("会自动构建该风味独立材质/贴图并替换 Renderer 材质。\n风味是独立 Shader + 独立材质目录。", MessageType.Info);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("描边法线流程", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("仅使用 Houdini 流程: 先在 Houdini 处理平滑法线并导回，再一键应用到选中角色。", MessageType.None);

        if (GUILayout.Button("应用 Houdini 描边网格(选中)", GUILayout.Height(26)))
        {
            NprOutlineNormalBaker.ApplyHoudiniBakedMeshes();
        }
    }

    private static void SwitchFlavor(StyleDef style, bool onlySelection)
    {
        EnsureFolder("Assets/MaterialFX/NPR/NPR-Core/Materials", "Profiles");
        EnsureFolder(ProfileMaterialRoot, style.Key);
        EnsureFolder(ProfileMaterialRoot + "/" + style.Key, "CharacterConverted");
        EnsureFolder("Assets/MaterialFX/NPR/NPR-Core/Textures", "Profiles");
        EnsureFolder(ProfileTextureRoot, style.Key);

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(style.ShaderPath);
        if (shader == null)
        {
            Debug.LogError("找不到风味 Shader: " + style.ShaderPath);
            return;
        }

        BuildStyleTextures(style.Key);
        BuildStyleMaterials(style, shader);

        int rendererCount;
        int slotCount;
        ApplyStyleToRenderers(style.Key, onlySelection, out rendererCount, out slotCount);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        NprTextureChannelValidator.Validate();
        Debug.Log($"[NPR] 已切换到 {style.DisplayName}，渲染器 {rendererCount} 个，材质槽 {slotCount} 个。");
    }

    private static void BuildStyleTextures(string styleKey)
    {
        string styleDir = ProfileTextureRoot + "/" + styleKey;
        string[] roles = { "Face", "Body", "Hair" };
        string[] kinds = { "LightMap", "FaceShadow", "OutlineWidth" };

        foreach (string role in roles)
        {
            foreach (string kind in kinds)
            {
                string src = GeneratedTextureRoot + $"/T_{role}_{kind}.png";
                string dst = styleDir + $"/T_{role}_{kind}.png";
                if (!AssetDatabase.LoadAssetAtPath<Texture2D>(dst))
                {
                    if (!AssetDatabase.CopyAsset(src, dst))
                    {
                        Debug.LogWarning("贴图拷贝失败: " + src + " -> " + dst);
                    }
                }

                TextureImporter importer = AssetImporter.GetAtPath(dst) as TextureImporter;
                if (importer != null && importer.sRGBTexture)
                {
                    importer.sRGBTexture = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }
    }

    private static void BuildStyleMaterials(StyleDef style, Shader shader)
    {
        string styleMatRoot = ProfileMaterialRoot + "/" + style.Key + "/CharacterConverted";
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { BaseMaterialRoot });

        foreach (string guid in guids)
        {
            string srcPath = AssetDatabase.GUIDToAssetPath(guid);
            Material src = AssetDatabase.LoadAssetAtPath<Material>(srcPath);
            if (src == null)
            {
                continue;
            }

            string baseName = NormalizeMaterialKey(src.name);
            string dstPath = styleMatRoot + "/" + baseName + "_" + style.Key + ".mat";

            Material dst = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
            if (dst == null)
            {
                if (!AssetDatabase.CopyAsset(srcPath, dstPath))
                {
                    Debug.LogWarning("材质拷贝失败: " + srcPath + " -> " + dstPath);
                    continue;
                }
                dst = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
            }
            else
            {
                EditorUtility.CopySerialized(src, dst);
            }

            if (dst == null)
            {
                continue;
            }

            dst.shader = shader;
            ApplyStyleParams(dst, style, ClassifyRole(src));
            ApplyStyleTexturesToMaterial(dst, style.Key, ClassifyRole(src));
            EditorUtility.SetDirty(dst);
        }
    }

    private static void ApplyStyleParams(Material mat, StyleDef style, MaterialRole role)
    {
        bool isFace = role == MaterialRole.Face;
        bool isHair = role == MaterialRole.Hair;

        SetFloatIfExists(mat, "_ColorSaturation", style.ColorSaturation);
        SetFloatIfExists(mat, "_ShadowStylizeStrength", style.ShadowStylizeStrength);
        SetFloatIfExists(mat, "_ShadowTintStrength", style.ShadowTintStrength);
        SetFloatIfExists(mat, "_RimStrength", style.RimStrength);
        SetColorIfExists(mat, "_ShadowCoolColor", style.ShadowCoolColor);
        SetColorIfExists(mat, "_ShadowWarmColor", style.ShadowWarmColor);
        SetColorIfExists(mat, "_OutlineColor", style.OutlineColor);
        SetVectorIfExists(mat, "_HeadForwardWS", new Vector4(0f, 0f, 1f, 0f));
        SetVectorIfExists(mat, "_HeadRightWS", new Vector4(1f, 0f, 0f, 0f));

        // Role-driven baseline: ensure face and hair pipelines are actually enabled.
        SetFloatIfExists(mat, "_FaceRegionWeight", isFace ? 1.0f : 0.0f);
        SetFloatIfExists(mat, "_FaceRimSuppress", isFace ? 0.45f : 0.0f);
        SetFloatIfExists(mat, "_FaceSpecBoost", isFace ? 0.15f : 0.0f);
        SetFloatIfExists(mat, "_FaceForwardWrap", isFace ? 0.18f : 0.0f);
        SetFloatIfExists(mat, "_FaceShadowLift", isFace ? 0.04f : 0.0f);
        SetFloatIfExists(mat, "_HairSpecStrength", isHair ? 0.85f : 0.0f);
        SetFloatIfExists(mat, "_HairSpecShift", isHair ? 0.18f : 0.10f);
        SetFloatIfExists(mat, "_HairSpecExponent1", isHair ? 96f : 72f);
        SetFloatIfExists(mat, "_HairSpecExponent2", isHair ? 24f : 18f);
        SetFloatIfExists(mat, "_HairSpecSecondaryStrength", isHair ? 0.62f : 0.40f);
        if (style.Key == "Genshin")
        {
            SetColorIfExists(mat, "_BaseColor", new Color(1.08f, 1.03f, 0.98f, 1f));
            SetFloatIfExists(mat, "_ShadowStrength", 0.90f);
            SetFloatIfExists(mat, "_AmbientStrength", 0.15f);
            SetFloatIfExists(mat, "_RampContrast", 1.36f);
            SetFloatIfExists(mat, "_RampBands", 4f);
            SetFloatIfExists(mat, "_SpecThreshold", 0.994f);
            SetFloatIfExists(mat, "_SpecSoftness", 0.012f);
            SetColorIfExists(mat, "_SpecColor", new Color(0.55f, 0.62f, 0.74f, 1f));
            SetFloatIfExists(mat, "_ShadowTerminatorWidth", 0.34f);
            SetFloatIfExists(mat, "_ShadowTerminatorSoftness", 0.28f);
            SetFloatIfExists(mat, "_RimPower", 3.2f);
            SetColorIfExists(mat, "_RimColor", new Color(1.0f, 0.97f, 0.90f, 1f));
            SetFloatIfExists(mat, "_AdditionalLightStrength", 0.18f);
            SetFloatIfExists(mat, "_HairAnisoStabilize", 0.85f);
            SetFloatIfExists(mat, "_HairSpecViewFade", 1.35f);
            SetFloatIfExists(mat, "_FaceOrientationStrength", role == MaterialRole.Face ? 0.35f : 0f);
            SetFloatIfExists(mat, "_FaceShadowSoftness", 0.08f);
            SetFloatIfExists(mat, "_FaceShadowHorizontalBias", 0.0f);
            SetFloatIfExists(mat, "_ExposureCompensation", 1.04f);
            SetFloatIfExists(mat, "_ToonContrast", 1.08f);
            SetFloatIfExists(mat, "_OutlineWidth", role == MaterialRole.Face ? 1.85f : 2.15f);
            SetFloatIfExists(mat, "_OutlineInnerSuppress", 0.30f);
            SetFloatIfExists(mat, "_OutlineSilhouetteBoost", 0.28f);
            SetFloatIfExists(mat, "_OutlineZOffset", 0.0014f);
        }
        else if (style.Key == "HSR")
        {
            SetColorIfExists(mat, "_BaseColor", new Color(0.99f, 1.02f, 1.06f, 1f));
            SetFloatIfExists(mat, "_ShadowStrength", 0.83f);
            SetFloatIfExists(mat, "_AmbientStrength", 0.19f);
            SetFloatIfExists(mat, "_RampContrast", 1.16f);
            SetFloatIfExists(mat, "_RampBands", 3f);
            SetFloatIfExists(mat, "_SpecThreshold", 0.987f);
            SetFloatIfExists(mat, "_SpecSoftness", 0.030f);
            SetColorIfExists(mat, "_SpecColor", new Color(0.64f, 0.72f, 0.88f, 1f));
            SetFloatIfExists(mat, "_ShadowTerminatorWidth", 0.44f);
            SetFloatIfExists(mat, "_ShadowTerminatorSoftness", 0.42f);
            SetFloatIfExists(mat, "_RimPower", 2.2f);
            SetColorIfExists(mat, "_RimColor", new Color(0.72f, 0.84f, 1.0f, 1f));
            SetFloatIfExists(mat, "_AdditionalLightStrength", 0.22f);
            SetFloatIfExists(mat, "_HairAnisoStabilize", 0.90f);
            SetFloatIfExists(mat, "_HairSpecViewFade", 1.65f);
            SetFloatIfExists(mat, "_FaceOrientationStrength", role == MaterialRole.Face ? 0.30f : 0f);
            SetFloatIfExists(mat, "_FaceShadowSoftness", 0.12f);
            SetFloatIfExists(mat, "_FaceShadowHorizontalBias", -0.03f);
            SetFloatIfExists(mat, "_ExposureCompensation", 1.04f);
            SetFloatIfExists(mat, "_ToonContrast", 1.00f);
            SetFloatIfExists(mat, "_OutlineWidth", role == MaterialRole.Face ? 1.75f : 2.05f);
            SetFloatIfExists(mat, "_OutlineInnerSuppress", 0.24f);
            SetFloatIfExists(mat, "_OutlineSilhouetteBoost", 0.24f);
            SetFloatIfExists(mat, "_OutlineZOffset", 0.0018f);
        }
        else
        {
            SetColorIfExists(mat, "_BaseColor", new Color(1.10f, 1.00f, 0.93f, 1f));
            SetFloatIfExists(mat, "_ShadowStrength", 0.93f);
            SetFloatIfExists(mat, "_AmbientStrength", 0.12f);
            SetFloatIfExists(mat, "_RampContrast", 1.60f);
            SetFloatIfExists(mat, "_RampBands", 5f);
            SetFloatIfExists(mat, "_SpecThreshold", 0.982f);
            SetFloatIfExists(mat, "_SpecSoftness", 0.018f);
            SetColorIfExists(mat, "_SpecColor", new Color(0.92f, 0.78f, 0.56f, 1f));
            SetFloatIfExists(mat, "_ShadowTerminatorWidth", 0.22f);
            SetFloatIfExists(mat, "_ShadowTerminatorSoftness", 0.16f);
            SetFloatIfExists(mat, "_RimPower", 4.3f);
            SetColorIfExists(mat, "_RimColor", new Color(1.0f, 0.86f, 0.70f, 1f));
            SetFloatIfExists(mat, "_AdditionalLightStrength", 0.10f);
            SetFloatIfExists(mat, "_HairAnisoStabilize", 0.82f);
            SetFloatIfExists(mat, "_HairSpecViewFade", 1.20f);
            SetFloatIfExists(mat, "_FaceOrientationStrength", role == MaterialRole.Face ? 0.12f : 0f);
            SetFloatIfExists(mat, "_FaceShadowSoftness", 0.06f);
            SetFloatIfExists(mat, "_FaceShadowHorizontalBias", 0.04f);
            SetFloatIfExists(mat, "_ExposureCompensation", 1.00f);
            SetFloatIfExists(mat, "_ToonContrast", 1.02f);
            SetFloatIfExists(mat, "_OutlineWidth", role == MaterialRole.Face ? 2.45f : 2.90f);
            SetFloatIfExists(mat, "_OutlineInnerSuppress", 0.05f);
            SetFloatIfExists(mat, "_OutlineSilhouetteBoost", 0.38f);
            SetFloatIfExists(mat, "_OutlineZOffset", 0.0008f);
        }
        SetFloatIfExists(mat, "_UseFaceShadowMap", isFace ? 1f : 0f);
        SetFloatIfExists(mat, "_FaceShadowMapStrength", isFace ? 1f : 0.0f);
        SetFloatIfExists(mat, "_OutlineWidthMapStrength", isFace ? 0.72f : 0.60f);
        SetFloatIfExists(mat, "_OutlineUseVertexColorNormal", 1f);
    }

    private static void ApplyStyleTexturesToMaterial(Material mat, string styleKey, MaterialRole role)
    {
        string roleName = role == MaterialRole.Face ? "Face" : role == MaterialRole.Body ? "Body" : "Hair";
        Texture2D light = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ProfileTextureRoot}/{styleKey}/T_{roleName}_LightMap.png");
        Texture2D face = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ProfileTextureRoot}/{styleKey}/T_{roleName}_FaceShadow.png");
        Texture2D outline = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ProfileTextureRoot}/{styleKey}/T_{roleName}_OutlineWidth.png");
        if (light != null) mat.SetTexture("_LightMap", light);
        if (face != null) mat.SetTexture("_FaceShadowMap", face);
        if (outline != null) mat.SetTexture("_OutlineWidthMap", outline);
    }

    private static void ApplyStyleToRenderers(string styleKey, bool onlySelection, out int rendererCount, out int slotCount)
    {
        Dictionary<string, Material> map = LoadStyleMap(styleKey);
        rendererCount = 0;
        slotCount = 0;

        List<Renderer> renderers = new List<Renderer>();
        if (onlySelection && Selection.gameObjects != null && Selection.gameObjects.Length > 0)
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                renderers.AddRange(go.GetComponentsInChildren<Renderer>(true));
            }
        }
        else
        {
            renderers.AddRange(Object.FindObjectsOfType<Renderer>(true));
        }

        foreach (Renderer renderer in renderers)
        {
            Material[] mats = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material current = mats[i];
                if (current == null)
                {
                    continue;
                }

                string key = NormalizeMaterialKey(current.name);
                if (map.TryGetValue(key, out Material replacement) && replacement != null && replacement != current)
                {
                    mats[i] = replacement;
                    changed = true;
                    slotCount++;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = mats;
                EditorUtility.SetDirty(renderer);
                EnsureFaceDirectionBinder(renderer);
                rendererCount++;
            }
        }
    }

    private static void EnsureFaceDirectionBinder(Renderer renderer)
    {
        if (renderer == null || renderer.transform == null)
        {
            return;
        }

        Transform root = renderer.transform.root;
        if (root == null)
        {
            return;
        }

        if (root.GetComponent<NprFaceDirectionBinder>() == null)
        {
            root.gameObject.AddComponent<NprFaceDirectionBinder>();
            EditorUtility.SetDirty(root.gameObject);
        }
    }

    private static Dictionary<string, Material> LoadStyleMap(string styleKey)
    {
        var map = new Dictionary<string, Material>();
        string root = ProfileMaterialRoot + "/" + styleKey + "/CharacterConverted";
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { root });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                continue;
            }
            map[NormalizeMaterialKey(mat.name)] = mat;
        }
        return map;
    }

    private static MaterialRole ClassifyRole(Material mat)
    {
        Texture baseTex = mat.GetTexture("_BaseMap");
        string name = ((baseTex != null ? baseTex.name : mat.name) ?? string.Empty).ToLowerInvariant();
        string matName = (mat.name ?? string.Empty).ToLowerInvariant();

        if (name.Contains("hair") || matName.Contains("hair") || matName.Contains("2_npr3"))
        {
            return MaterialRole.Hair;
        }

        if (name.Contains("face") || name.Contains("head") || name.Contains("skin") ||
            matName.Contains("face") || matName.Contains("head"))
        {
            return MaterialRole.Face;
        }

        if (name.Contains("body") || name.Contains("cloth") || name.Contains("shoe") ||
            matName.Contains("1_npr3") || matName.Contains("0_npr3"))
        {
            return MaterialRole.Body;
        }

        // Safe fallback for generic ids like "0_NPR3": treat as body to avoid face-map pollution.
        return MaterialRole.Body;
    }

    private static string NormalizeMaterialKey(string name)
    {
        string[] suffixes = { "_Genshin", "_HSR", "_ZZZ" };
        foreach (string suffix in suffixes)
        {
            if (name.EndsWith(suffix))
            {
                name = name.Substring(0, name.Length - suffix.Length);
            }
        }
        return name;
    }

    private static void SetFloatIfExists(Material mat, string prop, float value)
    {
        if (mat.HasProperty(prop))
        {
            mat.SetFloat(prop, value);
        }
    }

    private static void SetColorIfExists(Material mat, string prop, Color value)
    {
        if (mat.HasProperty(prop))
        {
            mat.SetColor(prop, value);
        }
    }

    private static void SetVectorIfExists(Material mat, string prop, Vector4 value)
    {
        if (mat.HasProperty(prop))
        {
            mat.SetVector(prop, value);
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private enum MaterialRole
    {
        Face,
        Body,
        Hair
    }

    private readonly struct StyleDef
    {
        public readonly string DisplayName;
        public readonly string Key;
        public readonly string ShaderPath;
        public readonly float ColorSaturation;
        public readonly float ShadowStylizeStrength;
        public readonly float ShadowTintStrength;
        public readonly float RimStrength;
        public readonly Color ShadowCoolColor;
        public readonly Color ShadowWarmColor;
        public readonly Color OutlineColor;

        public StyleDef(string displayName, string key, string shaderPath, float colorSaturation, float shadowStylizeStrength, float shadowTintStrength, float rimStrength, Color shadowCoolColor, Color shadowWarmColor, Color outlineColor)
        {
            DisplayName = displayName;
            Key = key;
            ShaderPath = shaderPath;
            ColorSaturation = colorSaturation;
            ShadowStylizeStrength = shadowStylizeStrength;
            ShadowTintStrength = shadowTintStrength;
            RimStrength = rimStrength;
            ShadowCoolColor = shadowCoolColor;
            ShadowWarmColor = shadowWarmColor;
            OutlineColor = outlineColor;
        }
    }
}
