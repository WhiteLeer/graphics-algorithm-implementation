using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class SSRRestoreBaselineOnce
{
    private const string SessionKey = "SSRRestoreBaselineOnce.Done";

    static SSRRestoreBaselineOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += ApplyBaseline;
    }

    [MenuItem("Tools/Post Process/Restore SSR Baseline (Func-3)")]
    public static void ApplyBaseline()
    {
        bool changed = false;
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rendererData == null)
                continue;

            SerializedObject so = new SerializedObject(rendererData);
            SerializedProperty featuresProp = so.FindProperty("m_RendererFeatures");
            if (featuresProp == null || !featuresProp.isArray)
                continue;

            for (int i = 0; i < featuresProp.arraySize; i++)
            {
                SerializedProperty item = featuresProp.GetArrayElementAtIndex(i);
                ScriptableRendererFeature feature = item.objectReferenceValue as ScriptableRendererFeature;
                if (feature == null || feature.GetType().Name != "SSRRenderFeature")
                    continue;

                SerializedObject featureSo = new SerializedObject(feature);
                SerializedProperty settings = featureSo.FindProperty("settings");
                if (settings == null)
                    continue;

                SetFloat(settings, "intensity", 1.9f);
                SetInt(settings, "maxSteps", 128);
                SetFloat(settings, "stride", 0.012f);
                SetFloat(settings, "thickness", 0.04f);
                SetFloat(settings, "maxDistance", 12.0f);
                SetFloat(settings, "rayStartBias", 0.03f);
                SetFloat(settings, "fresnelPower", 1.2f);
                SetFloat(settings, "fadeStart", 0.0f);
                SetFloat(settings, "fadeEnd", 0.12f);
                SetBool(settings, "useReceiverPlaneMask", false);
                SetVector3(settings, "receiverPlaneNormal", Vector3.up);
                SetFloat(settings, "receiverPlaneDistance", 0.0f);
                SetFloat(settings, "receiverNormalThreshold", 0.8f);
                SetFloat(settings, "receiverMaxDistance", 0.12f);
                SetBool(settings, "enableDebugVisualization", false);
                SetEnum(settings, "debugStep1", 16);
                SetEnum(settings, "debugStep2", 10);
                SetEnum(settings, "debugStep3", 17);
                SetEnum(settings, "debugStep4", 18);
                SetEnum(settings, "runtimeDebugStep", 0);
                SetEnum(settings, "renderPassEvent", (int)RenderPassEvent.AfterRenderingOpaques);

                featureSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(feature);
                changed = true;
            }
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SSRRestoreBaselineOnce] Restored SSR baseline settings.");
        }
    }

    private static void SetFloat(SerializedProperty settings, string name, float value)
    {
        SerializedProperty p = settings.FindPropertyRelative(name);
        if (p != null) p.floatValue = value;
    }

    private static void SetInt(SerializedProperty settings, string name, int value)
    {
        SerializedProperty p = settings.FindPropertyRelative(name);
        if (p != null) p.intValue = value;
    }

    private static void SetBool(SerializedProperty settings, string name, bool value)
    {
        SerializedProperty p = settings.FindPropertyRelative(name);
        if (p != null) p.boolValue = value;
    }

    private static void SetVector3(SerializedProperty settings, string name, Vector3 value)
    {
        SerializedProperty p = settings.FindPropertyRelative(name);
        if (p != null) p.vector3Value = value;
    }

    private static void SetEnum(SerializedProperty settings, string name, int value)
    {
        SerializedProperty p = settings.FindPropertyRelative(name);
        if (p != null) p.enumValueIndex = value;
    }
}
