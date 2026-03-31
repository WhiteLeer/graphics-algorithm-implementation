#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Npr4StylePresets
{
    [MenuItem("Tools/All Effects/NPR-4/Apply Clean Anime Style")]
    public static void ApplyCleanAnimeStyle()
    {
        ApplyPreset(vibrant: false);
    }

    [MenuItem("Tools/All Effects/NPR-4/Apply Vibrant Anime Style")]
    public static void ApplyVibrantAnimeStyle()
    {
        ApplyPreset(vibrant: true);
    }

    [MenuItem("Tools/All Effects/NPR-5/Apply Hair Specular Boost")]
    public static void ApplyHairSpecularBoost()
    {
        ApplyHairSpecPreset();
    }

    [MenuItem("Tools/All Effects/NPR-6/Apply Face Lighting Tuning")]
    public static void ApplyFaceLightingTuning()
    {
        ApplyFacePreset();
    }

    [MenuItem("Tools/All Effects/NPR-7/Apply Stylized Shadow")]
    public static void ApplyStylizedShadow()
    {
        ApplyShadowStylizePreset();
    }

    [MenuItem("Tools/All Effects/NPR-8/Apply Adaptive Outline")]
    public static void ApplyAdaptiveOutline()
    {
        ApplyAdaptiveOutlinePreset();
    }

    [MenuItem("Tools/All Effects/NPR-9/Apply Material Region Profiles")]
    public static void ApplyMaterialRegionProfiles()
    {
        ApplyMaterialRegionProfilesPreset();
    }

    private static void ApplyPreset(bool vibrant)
    {
        DynamicFxLabBuilder.ApplyCharacterLightingPreset();

        GameObject character = GameObject.Find("Player_Girl_Test");
        if (character == null)
            character = GameObject.Find("Player_Girl");
        if (character == null)
        {
            Debug.LogWarning("[NPR-4] Character not found.");
            return;
        }

        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        HashSet<Material> unique = new HashSet<Material>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].sharedMaterials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null)
                    unique.Add(mats[j]);
            }
        }

        foreach (Material mat in unique)
        {
            if (mat.shader == null || !mat.shader.name.Contains("NPR-"))
                continue;

            bool isHair = mat.name.Contains("2_");
            bool isAccent = mat.name.Contains("1_");

            float sat = vibrant ? 1.55f : 1.35f;
            float contrast = vibrant ? 1.42f : 1.28f;
            float rim = vibrant ? 0.11f : 0.08f;
            float addLight = vibrant ? 0.12f : 0.09f;

            if (isHair)
            {
                sat += vibrant ? 0.20f : 0.12f;
                rim += vibrant ? 0.10f : 0.08f;
            }
            else if (isAccent)
            {
                sat += vibrant ? 0.10f : 0.06f;
                contrast += 0.05f;
            }

            if (mat.HasProperty("_ColorSaturation")) mat.SetFloat("_ColorSaturation", sat);
            if (mat.HasProperty("_RampContrast")) mat.SetFloat("_RampContrast", contrast);
            if (mat.HasProperty("_RimStrength")) mat.SetFloat("_RimStrength", rim);
            if (mat.HasProperty("_AdditionalLightStrength")) mat.SetFloat("_AdditionalLightStrength", addLight);
            if (mat.HasProperty("_ShadowStrength")) mat.SetFloat("_ShadowStrength", vibrant ? 0.92f : 0.88f);
            if (mat.HasProperty("_AmbientStrength")) mat.SetFloat("_AmbientStrength", vibrant ? 0.12f : 0.15f);
            if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", isHair ? 3.2f : 2.9f);
            if (mat.HasProperty("_HairSpecStrength")) mat.SetFloat("_HairSpecStrength", isHair ? (vibrant ? 1.45f : 1.10f) : 0.0f);
            if (mat.HasProperty("_HairSpecShift")) mat.SetFloat("_HairSpecShift", isHair ? 0.14f : 0.10f);
            if (mat.HasProperty("_HairSpecExponent1")) mat.SetFloat("_HairSpecExponent1", isHair ? 72.0f : 64.0f);
            if (mat.HasProperty("_HairSpecExponent2")) mat.SetFloat("_HairSpecExponent2", isHair ? 18.0f : 20.0f);
            if (mat.HasProperty("_HairSpecSecondaryStrength")) mat.SetFloat("_HairSpecSecondaryStrength", isHair ? 0.62f : 0.45f);

            EditorUtility.SetDirty(mat);
        }

        Material floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/MaterialFX/Common_LitLibrary/M_CharacterFloor.mat");
        Material wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/MaterialFX/Common_LitLibrary/M_CharacterBackWall.mat");
        if (floor != null)
        {
            floor.SetColor("_BaseColor", vibrant ? new Color(0.34f, 0.38f, 0.45f, 1f) : new Color(0.40f, 0.43f, 0.49f, 1f));
            EditorUtility.SetDirty(floor);
        }
        if (wall != null)
        {
            wall.SetColor("_BaseColor", vibrant ? new Color(0.52f, 0.58f, 0.70f, 1f) : new Color(0.58f, 0.62f, 0.72f, 1f));
            EditorUtility.SetDirty(wall);
        }

        AssetDatabase.SaveAssets();
        Debug.Log(vibrant ? "[NPR-4] Applied Vibrant Anime Style." : "[NPR-4] Applied Clean Anime Style.");
    }

    private static void ApplyHairSpecPreset()
    {
        GameObject character = GameObject.Find("Player_Girl_Test");
        if (character == null)
            character = GameObject.Find("Player_Girl");
        if (character == null)
        {
            Debug.LogWarning("[NPR-5] Character not found.");
            return;
        }

        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        HashSet<Material> unique = new HashSet<Material>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].sharedMaterials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null)
                    unique.Add(mats[j]);
            }
        }

        foreach (Material mat in unique)
        {
            if (mat.shader == null || !mat.shader.name.Contains("NPR-"))
                continue;

            bool isHair = mat.name.Contains("2_");
            if (!mat.HasProperty("_HairSpecStrength"))
                continue;

            mat.SetFloat("_HairSpecStrength", isHair ? 1.45f : 0.0f);
            if (mat.HasProperty("_HairSpecShift")) mat.SetFloat("_HairSpecShift", 0.14f);
            if (mat.HasProperty("_HairSpecExponent1")) mat.SetFloat("_HairSpecExponent1", 72.0f);
            if (mat.HasProperty("_HairSpecExponent2")) mat.SetFloat("_HairSpecExponent2", 18.0f);
            if (mat.HasProperty("_HairSpecSecondaryStrength")) mat.SetFloat("_HairSpecSecondaryStrength", 0.62f);
            if (mat.HasProperty("_SpecColor")) mat.SetColor("_SpecColor", new Color(0.52f, 0.58f, 0.70f, 1.0f));
            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[NPR-5] Hair specular boost applied.");
    }

    private static void ApplyFacePreset()
    {
        GameObject character = GameObject.Find("Player_Girl_Test");
        if (character == null)
            character = GameObject.Find("Player_Girl");
        if (character == null)
        {
            Debug.LogWarning("[NPR-6] Character not found.");
            return;
        }

        SkinnedMeshRenderer head = character.transform.Find("Model_Char_Main_Head")?.GetComponent<SkinnedMeshRenderer>();
        if (head == null || head.sharedMaterials == null || head.sharedMaterials.Length == 0 || head.sharedMaterials[0] == null)
        {
            Debug.LogWarning("[NPR-6] Head renderer/material not found.");
            return;
        }

        Material headBase = head.sharedMaterials[0];
        Material headFace = GetOrCreateFaceMaterial(headBase);
        if (headFace == null)
            return;

        Material[] mats = head.sharedMaterials;
        mats[0] = headFace;
        head.sharedMaterials = mats;
        EditorUtility.SetDirty(head);

        if (headFace.HasProperty("_FaceRegionWeight")) headFace.SetFloat("_FaceRegionWeight", 1.0f);
        if (headFace.HasProperty("_FaceShadowLift")) headFace.SetFloat("_FaceShadowLift", 0.34f);
        if (headFace.HasProperty("_FaceForwardWrap")) headFace.SetFloat("_FaceForwardWrap", 0.36f);
        if (headFace.HasProperty("_FaceSpecBoost")) headFace.SetFloat("_FaceSpecBoost", 0.30f);
        if (headFace.HasProperty("_FaceRimSuppress")) headFace.SetFloat("_FaceRimSuppress", 0.70f);
        if (headFace.HasProperty("_ShadowTintStrength")) headFace.SetFloat("_ShadowTintStrength", 0.10f);
        if (headFace.HasProperty("_RimStrength")) headFace.SetFloat("_RimStrength", 0.035f);
        if (headFace.HasProperty("_OutlineWidth")) headFace.SetFloat("_OutlineWidth", 2.6f);
        EditorUtility.SetDirty(headFace);

        AssetDatabase.SaveAssets();
        Debug.Log("[NPR-6] Face lighting tuning applied.");
    }

    private static Material GetOrCreateFaceMaterial(Material source)
    {
        if (source == null)
            return null;

        const string dir = "Assets/MaterialFX/NPR-3_CharacterAdvanced/Materials/CharacterConverted";
        if (!AssetDatabase.IsValidFolder(dir))
            return null;

        string srcPath = AssetDatabase.GetAssetPath(source);
        string sourceName = Path.GetFileNameWithoutExtension(srcPath);
        if (string.IsNullOrEmpty(sourceName))
            sourceName = source.name;
        string targetPath = dir + "/" + sourceName + "_Face_NPR6.mat";

        Material target = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
        if (target == null)
        {
            target = new Material(source);
            AssetDatabase.CreateAsset(target, targetPath);
        }
        else
        {
            EditorUtility.CopySerialized(source, target);
        }

        target.name = Path.GetFileNameWithoutExtension(targetPath);
        return target;
    }

    private static void ApplyShadowStylizePreset()
    {
        GameObject character = GameObject.Find("Player_Girl_Test");
        if (character == null)
            character = GameObject.Find("Player_Girl");
        if (character == null)
        {
            Debug.LogWarning("[NPR-7] Character not found.");
            return;
        }

        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        HashSet<Material> unique = new HashSet<Material>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].sharedMaterials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null)
                    unique.Add(mats[j]);
            }
        }

        foreach (Material mat in unique)
        {
            if (mat.shader == null || !mat.shader.name.Contains("NPR-"))
                continue;

            bool isHair = mat.name.Contains("2_");
            bool isFace = mat.name.Contains("_Face_NPR6");

            if (mat.HasProperty("_ShadowStylizeStrength")) mat.SetFloat("_ShadowStylizeStrength", isFace ? 0.45f : 0.62f);
            if (mat.HasProperty("_ShadowCoolColor")) mat.SetColor("_ShadowCoolColor", isHair ? new Color(0.70f, 0.82f, 1.0f, 1.0f) : new Color(0.74f, 0.84f, 1.0f, 1.0f));
            if (mat.HasProperty("_ShadowWarmColor")) mat.SetColor("_ShadowWarmColor", isHair ? new Color(0.96f, 0.86f, 0.74f, 1.0f) : new Color(1.0f, 0.88f, 0.78f, 1.0f));
            if (mat.HasProperty("_ShadowTerminatorWidth")) mat.SetFloat("_ShadowTerminatorWidth", isFace ? 0.36f : 0.28f);
            if (mat.HasProperty("_ShadowTerminatorSoftness")) mat.SetFloat("_ShadowTerminatorSoftness", isFace ? 0.34f : 0.22f);
            if (mat.HasProperty("_ShadowTintStrength")) mat.SetFloat("_ShadowTintStrength", isFace ? 0.09f : 0.21f);
            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[NPR-7] Stylized shadow preset applied.");
    }

    private static void ApplyAdaptiveOutlinePreset()
    {
        GameObject character = GameObject.Find("Player_Girl_Test");
        if (character == null)
            character = GameObject.Find("Player_Girl");
        if (character == null)
        {
            Debug.LogWarning("[NPR-8] Character not found.");
            return;
        }

        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        HashSet<Material> unique = new HashSet<Material>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].sharedMaterials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null)
                    unique.Add(mats[j]);
            }
        }

        foreach (Material mat in unique)
        {
            if (mat.shader == null || !mat.shader.name.Contains("NPR-"))
                continue;

            bool isHair = mat.name.Contains("2_");
            bool isFace = mat.name.Contains("_Face_NPR6");

            if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", isHair ? 3.4f : (isFace ? 2.4f : 2.9f));
            if (mat.HasProperty("_OutlineMinScale")) mat.SetFloat("_OutlineMinScale", isHair ? 0.62f : 0.52f);
            if (mat.HasProperty("_OutlineDistanceStart")) mat.SetFloat("_OutlineDistanceStart", 1.8f);
            if (mat.HasProperty("_OutlineDistanceEnd")) mat.SetFloat("_OutlineDistanceEnd", 9.0f);
            if (mat.HasProperty("_OutlineSilhouetteBoost")) mat.SetFloat("_OutlineSilhouetteBoost", isFace ? 0.18f : 0.45f);
            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[NPR-8] Adaptive outline preset applied.");
    }

    private static void ApplyMaterialRegionProfilesPreset()
    {
        GameObject character = GameObject.Find("Player_Girl_Test");
        if (character == null)
            character = GameObject.Find("Player_Girl");
        if (character == null)
        {
            Debug.LogWarning("[NPR-9] Character not found.");
            return;
        }

        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        HashSet<Material> unique = new HashSet<Material>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].sharedMaterials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] != null)
                    unique.Add(mats[j]);
            }
        }

        foreach (Material mat in unique)
        {
            if (mat.shader == null || !mat.shader.name.Contains("NPR-"))
                continue;

            string lower = mat.name.ToLowerInvariant();
            bool isFace = lower.Contains("_face_npr6");
            bool isHair = lower.Contains("2_npr3");
            bool isCloth = lower.Contains("1_npr3");
            bool isSkin = !isFace && !isHair && !isCloth;

            if (isFace)
                ApplyFaceGroup(mat);
            else if (isHair)
                ApplyHairGroup(mat);
            else if (isCloth)
                ApplyClothGroup(mat);
            else if (isSkin)
                ApplySkinGroup(mat);

            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[NPR-9] Material region profiles applied.");
    }

    private static void ApplySkinGroup(Material mat)
    {
        SetIf(mat, "_ColorSaturation", 1.48f);
        SetIf(mat, "_RampContrast", 1.34f);
        SetIf(mat, "_ShadowStrength", 0.90f);
        SetIf(mat, "_AmbientStrength", 0.13f);
        SetIf(mat, "_RimStrength", 0.07f);
        SetIf(mat, "_SpecThreshold", 0.992f);
        SetIf(mat, "_SpecSoftness", 0.009f);
        SetIf(mat, "_OutlineWidth", 2.75f);
        SetIf(mat, "_ShadowStylizeStrength", 0.50f);
        SetIf(mat, "_ShadowTerminatorWidth", 0.30f);
        SetIf(mat, "_ShadowTerminatorSoftness", 0.24f);
    }

    private static void ApplyClothGroup(Material mat)
    {
        SetIf(mat, "_ColorSaturation", 1.64f);
        SetIf(mat, "_RampContrast", 1.52f);
        SetIf(mat, "_ShadowStrength", 0.95f);
        SetIf(mat, "_AmbientStrength", 0.10f);
        SetIf(mat, "_RimStrength", 0.11f);
        SetIf(mat, "_AdditionalLightStrength", 0.14f);
        SetIf(mat, "_SpecThreshold", 0.994f);
        SetIf(mat, "_SpecSoftness", 0.006f);
        SetIf(mat, "_OutlineWidth", 2.95f);
        SetIf(mat, "_ShadowStylizeStrength", 0.68f);
        SetIf(mat, "_ShadowTerminatorWidth", 0.27f);
        SetIf(mat, "_ShadowTerminatorSoftness", 0.20f);
    }

    private static void ApplyHairGroup(Material mat)
    {
        SetIf(mat, "_ColorSaturation", 1.70f);
        SetIf(mat, "_RampContrast", 1.58f);
        SetIf(mat, "_ShadowStrength", 0.93f);
        SetIf(mat, "_AmbientStrength", 0.10f);
        SetIf(mat, "_RimStrength", 0.16f);
        SetIf(mat, "_HairSpecStrength", 1.62f);
        SetIf(mat, "_HairSpecShift", 0.15f);
        SetIf(mat, "_HairSpecExponent1", 68.0f);
        SetIf(mat, "_HairSpecExponent2", 16.0f);
        SetIf(mat, "_HairSpecSecondaryStrength", 0.70f);
        SetIf(mat, "_OutlineWidth", 3.45f);
        SetIf(mat, "_OutlineMinScale", 0.64f);
        SetIf(mat, "_OutlineSilhouetteBoost", 0.55f);
        SetIf(mat, "_ShadowStylizeStrength", 0.66f);
    }

    private static void ApplyFaceGroup(Material mat)
    {
        SetIf(mat, "_ColorSaturation", 1.42f);
        SetIf(mat, "_RampContrast", 1.24f);
        SetIf(mat, "_ShadowStrength", 0.84f);
        SetIf(mat, "_AmbientStrength", 0.16f);
        SetIf(mat, "_RimStrength", 0.03f);
        SetIf(mat, "_OutlineWidth", 2.35f);
        SetIf(mat, "_OutlineSilhouetteBoost", 0.14f);
        SetIf(mat, "_FaceRegionWeight", 1.0f);
        SetIf(mat, "_FaceShadowLift", 0.36f);
        SetIf(mat, "_FaceForwardWrap", 0.40f);
        SetIf(mat, "_FaceSpecBoost", 0.32f);
        SetIf(mat, "_FaceRimSuppress", 0.72f);
        SetIf(mat, "_ShadowStylizeStrength", 0.42f);
        SetIf(mat, "_ShadowTerminatorWidth", 0.40f);
        SetIf(mat, "_ShadowTerminatorSoftness", 0.36f);
    }

    private static void SetIf(Material mat, string prop, float value)
    {
        if (mat.HasProperty(prop))
            mat.SetFloat(prop, value);
    }
}
#endif
