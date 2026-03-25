using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Automated post-process capture entrypoints for CLI and menu usage.
/// </summary>
[InitializeOnLoad]
public static class PostProcessAutoCapture
{
    private const string SessionKeyPending = "PostProcessAutoCapture.Pending";
    private const string SessionKeyFeatureType = "PostProcessAutoCapture.FeatureType";
    private const string SessionKeyQuit = "PostProcessAutoCapture.QuitOnFinish";
    private const string SessionKeyDeadline = "PostProcessAutoCapture.Deadline";
    private const string SessionKeyError = "PostProcessAutoCapture.Error";
    private const string SessionKeyMarkerPath = "PostProcessAutoCapture.MarkerPath";

    private static string s_TargetFeatureTypeName;
    private static bool s_QuitOnFinish;
    private static bool s_CaptureStarted;
    private static double s_Deadline;
    private static string s_Error;
    private static string s_CompletionMarkerPath;

    static PostProcessAutoCapture()
    {
        RestorePendingState();
    }

    [MenuItem("Tools/Post Process/Auto Capture SSPR (Play Mode)")]
    public static void CaptureSSPRPlayMenu()
    {
        Debug.Log("[AutoCapture] CaptureSSPRPlayMenu invoked.");
        StartCaptureForFeature("SSPRRenderFeature", false);
    }

    [MenuItem("Tools/Post Process/Auto Capture SSR (Play Mode)")]
    public static void CaptureSSRPlayMenu()
    {
        Debug.Log("[AutoCapture] CaptureSSRPlayMenu invoked.");
        StartCaptureForFeature("SSRRenderFeature", false);
    }

    // CLI:
    // "Unity.exe" -projectPath "<project>" -batchmode -executeMethod PostProcessAutoCapture.CaptureSSPRPlayBatch -logFile -
    public static void CaptureSSPRPlayBatch()
    {
        Debug.Log("[AutoCapture] CaptureSSPRPlayBatch invoked.");
        StartCaptureForFeature("SSPRRenderFeature", true);
    }

    // CLI:
    // "Unity.exe" -projectPath "<project>" -batchmode -executeMethod PostProcessAutoCapture.CaptureSSRPlayBatch -logFile -
    public static void CaptureSSRPlayBatch()
    {
        Debug.Log("[AutoCapture] CaptureSSRPlayBatch invoked.");
        StartCaptureForFeature("SSRRenderFeature", true);
    }

    private static void StartCaptureForFeature(string featureTypeName, bool quitOnFinish)
    {
        if (s_CaptureStarted)
        {
            Debug.Log("[AutoCapture] Capture already started, skipping duplicate request.");
            return;
        }

        if (!TryFindFeature(featureTypeName, out ScriptableRendererFeature feature))
        {
            FailAndMaybeExit($"[AutoCapture] Feature not found: {featureTypeName}");
            return;
        }

        s_TargetFeatureTypeName = featureTypeName;
        s_QuitOnFinish = quitOnFinish;
        s_CaptureStarted = true;
        s_Error = null;
        s_Deadline = EditorApplication.timeSinceStartup + 240.0;
        s_CompletionMarkerPath = CreateCompletionMarkerPath(featureTypeName);
        Debug.Log($"[AutoCapture] StartCaptureForFeature -> {featureTypeName}, quitOnFinish={quitOnFinish}");
        PersistState();

        feature.SetActive(true);
        EditorUtility.SetDirty(feature);
        SetDebugVisualization(feature, true);

        RegisterCallbacks();
        EditorApplication.delayCall += EnsureEnterPlayMode;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!s_CaptureStarted || string.IsNullOrEmpty(s_TargetFeatureTypeName))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
#if UNITY_EDITOR
            EditorApplication.isPaused = false;
#endif
            if (!TryFindFeature(s_TargetFeatureTypeName, out ScriptableRendererFeature feature))
            {
                s_Error = $"[AutoCapture] Feature not found after domain reload: {s_TargetFeatureTypeName}";
                PersistState();
                EditorApplication.isPlaying = false;
                return;
            }

            Debug.Log("[AutoCapture] EnteredPlayMode, scheduling EditorPlayModeCapture.");
            string settingsInfo = SerializeFeatureSettings(feature);
            EditorPlayModeCapture.Schedule(
                feature.name,
                settingsInfo,
                s_CompletionMarkerPath,
                () =>
                {
                    EditorApplication.isPlaying = false;
                });
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Debug.Log("[AutoCapture] Returned to EditMode.");
            if (IsCaptureCompleted())
            {
                FinishCapture();
                return;
            }

            if (EditorApplication.timeSinceStartup > s_Deadline)
            {
                s_Error = "[AutoCapture] Capture timeout (>240s).";
                PersistState();
                FinishCapture();
                return;
            }

            Debug.LogWarning("[AutoCapture] Returned to EditMode before completion marker was written. Will retry PlayMode.");
            PersistState();
            EditorApplication.delayCall += EnsureEnterPlayMode;
        }
    }

    private static void OnEditorUpdate()
    {
        if (!s_CaptureStarted)
            return;

        if (IsCaptureCompleted())
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            else
                FinishCapture();
            return;
        }

        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EnsureEnterPlayMode();
        }

        if (EditorApplication.timeSinceStartup <= s_Deadline)
            return;

        s_Error = "[AutoCapture] Capture timeout (>240s).";
        PersistState();
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
        else
            FinishCapture();
    }

    private static void FinishCapture()
    {
        EditorPlayModeCapture.Cancel();

        if (!string.IsNullOrEmpty(s_TargetFeatureTypeName) &&
            TryFindFeature(s_TargetFeatureTypeName, out ScriptableRendererFeature feature))
        {
            SetDebugVisualization(feature, false);
        }

        UnregisterCallbacks();

        bool ok = string.IsNullOrEmpty(s_Error);
        if (ok)
            Debug.Log("[AutoCapture] Capture finished.");
        else
            Debug.LogError(s_Error);

        bool quitOnFinish = s_QuitOnFinish;
        ClearState();

        if (quitOnFinish)
            EditorApplication.Exit(ok ? 0 : 1);
    }

    private static void FailAndMaybeExit(string message)
    {
        Debug.LogError(message);
        if (s_QuitOnFinish)
            EditorApplication.Exit(1);
    }

    private static void RegisterCallbacks()
    {
        UnregisterCallbacks();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    private static void UnregisterCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    private static void PersistState()
    {
        SessionState.SetBool(SessionKeyPending, s_CaptureStarted);
        SessionState.SetString(SessionKeyFeatureType, s_TargetFeatureTypeName ?? string.Empty);
        SessionState.SetBool(SessionKeyQuit, s_QuitOnFinish);
        SessionState.SetFloat(SessionKeyDeadline, (float)s_Deadline);
        SessionState.SetString(SessionKeyError, s_Error ?? string.Empty);
        SessionState.SetString(SessionKeyMarkerPath, s_CompletionMarkerPath ?? string.Empty);
    }

    private static void RestorePendingState()
    {
        bool pending = SessionState.GetBool(SessionKeyPending, false);
        if (!pending)
            return;

        s_CaptureStarted = true;
        s_TargetFeatureTypeName = SessionState.GetString(SessionKeyFeatureType, string.Empty);
        s_QuitOnFinish = SessionState.GetBool(SessionKeyQuit, false);
        s_Deadline = SessionState.GetFloat(SessionKeyDeadline, (float)(EditorApplication.timeSinceStartup + 90.0));
        string savedError = SessionState.GetString(SessionKeyError, string.Empty);
        s_Error = string.IsNullOrEmpty(savedError) ? null : savedError;
        s_CompletionMarkerPath = SessionState.GetString(SessionKeyMarkerPath, string.Empty);

        if (IsCaptureCompleted())
        {
            ClearState();
            return;
        }

        if (s_Deadline <= EditorApplication.timeSinceStartup)
        {
            Debug.LogWarning("[AutoCapture] Pending capture deadline already expired on restore. Clearing stale state.");
            ClearState();
            return;
        }

        if (s_Deadline < EditorApplication.timeSinceStartup + 30.0)
            s_Deadline = EditorApplication.timeSinceStartup + 120.0;

        RegisterCallbacks();
        Debug.Log($"[AutoCapture] Restored pending capture for {s_TargetFeatureTypeName}");
        EditorApplication.delayCall += EnsureEnterPlayMode;
    }

    private static void ClearState()
    {
        s_TargetFeatureTypeName = null;
        s_QuitOnFinish = false;
        s_CaptureStarted = false;
        s_Deadline = 0.0;
        s_Error = null;
        DeleteCompletionMarker();
        s_CompletionMarkerPath = null;

        SessionState.SetBool(SessionKeyPending, false);
        SessionState.SetString(SessionKeyFeatureType, string.Empty);
        SessionState.SetBool(SessionKeyQuit, false);
        SessionState.SetFloat(SessionKeyDeadline, 0.0f);
        SessionState.SetString(SessionKeyError, string.Empty);
        SessionState.SetString(SessionKeyMarkerPath, string.Empty);
    }

    private static bool IsCaptureCompleted()
    {
        return !string.IsNullOrEmpty(s_CompletionMarkerPath) && System.IO.File.Exists(s_CompletionMarkerPath);
    }

    private static string CreateCompletionMarkerPath(string featureTypeName)
    {
        string dir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Temp", "PostProcessAutoCapture");
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        string safeName = string.IsNullOrEmpty(featureTypeName) ? "Capture" : featureTypeName;
        string fileName = $"{safeName}_{System.DateTime.Now:yyyyMMdd_HHmmss_fff}.done";
        string path = System.IO.Path.Combine(dir, fileName);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
        return path;
    }

    private static void DeleteCompletionMarker()
    {
        if (string.IsNullOrEmpty(s_CompletionMarkerPath))
            return;

        if (System.IO.File.Exists(s_CompletionMarkerPath))
            System.IO.File.Delete(s_CompletionMarkerPath);
    }

    private static void EnsureEnterPlayMode()
    {
        if (!s_CaptureStarted)
            return;

#if UNITY_EDITOR
        EditorApplication.isPaused = false;
#endif
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Debug.Log("[AutoCapture] Triggering PlayMode.");
        EditorApplication.isPlaying = true;
    }

    private static bool TryFindFeature(string featureTypeName, out ScriptableRendererFeature feature)
    {
        feature = null;
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rendererData == null)
                continue;

            SerializedObject so = new SerializedObject(rendererData);
            SerializedProperty featuresProp = so.FindProperty("m_RendererFeatures");
            if (featuresProp == null || !featuresProp.isArray)
                continue;

            for (int i = 0; i < featuresProp.arraySize; i++)
            {
                SerializedProperty item = featuresProp.GetArrayElementAtIndex(i);
                ScriptableRendererFeature f = item.objectReferenceValue as ScriptableRendererFeature;
                if (f == null)
                    continue;
                if (f.GetType().Name == featureTypeName)
                {
                    feature = f;
                    return true;
                }
            }
        }

        return false;
    }

    private static void SetDebugVisualization(ScriptableRendererFeature feature, bool enabled)
    {
        FieldInfo settingsField = feature.GetType().GetField("settings", BindingFlags.Public | BindingFlags.Instance);
        if (settingsField == null)
            return;
        object settings = settingsField.GetValue(feature);
        if (settings == null)
            return;
        FieldInfo debugField = settings.GetType().GetField("enableDebugVisualization", BindingFlags.Public | BindingFlags.Instance);
        if (debugField == null)
            return;
        debugField.SetValue(settings, enabled);
    }

    private static string SerializeFeatureSettings(ScriptableRendererFeature feature)
    {
        FieldInfo settingsField = feature.GetType().GetField("settings", BindingFlags.Public | BindingFlags.Instance);
        if (settingsField == null)
            return "No settings field.";
        object settings = settingsField.GetValue(feature);
        if (settings == null)
            return "Settings is null.";

        StringBuilder sb = new StringBuilder();
        FieldInfo[] fields = settings.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(settings);
            if (value == null)
                continue;
            if (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType.IsEnum || field.FieldType == typeof(Vector2) || field.FieldType == typeof(Vector3) || field.FieldType == typeof(Vector4))
                sb.AppendLine($"{field.Name}: {value}");
        }
        return sb.ToString().TrimEnd();
    }

    private static int[] GetDebugSteps(ScriptableRendererFeature feature)
    {
        int[] steps = new[] { 1, 2, 3, 4 };
        FieldInfo settingsField = feature.GetType().GetField("settings", BindingFlags.Public | BindingFlags.Instance);
        if (settingsField == null)
            return steps;
        object settings = settingsField.GetValue(feature);
        if (settings == null)
            return steps;

        FieldInfo s1 = settings.GetType().GetField("debugStep1");
        FieldInfo s2 = settings.GetType().GetField("debugStep2");
        FieldInfo s3 = settings.GetType().GetField("debugStep3");
        FieldInfo s4 = settings.GetType().GetField("debugStep4");

        if (s1 != null) steps[0] = (int)s1.GetValue(settings);
        if (s2 != null) steps[1] = (int)s2.GetValue(settings);
        if (s3 != null) steps[2] = (int)s3.GetValue(settings);
        if (s4 != null) steps[3] = (int)s4.GetValue(settings);

        return steps;
    }

    private static Material CreateStepDebugMaterial(ScriptableRendererFeature feature)
    {
        string baseName = feature.name.Replace("RenderFeature", string.Empty);
        Shader shader = Shader.Find($"Hidden/{baseName}_StepDebug");
        if (shader == null)
            return null;

        Material mat = new Material(shader);

        FieldInfo settingsField = feature.GetType().GetField("settings", BindingFlags.Public | BindingFlags.Instance);
        if (settingsField == null)
            return mat;
        object settings = settingsField.GetValue(feature);
        if (settings == null)
            return mat;

        if (feature.GetType().Name == "SSPRRenderFeature")
        {
            float intensity = GetFloat(settings, "intensity", 1.0f);
            float fresnel = GetFloat(settings, "fresnelPower", 5.0f);
            float fadeStart = GetFloat(settings, "fadeStart", 0.0f);
            float fadeEnd = GetFloat(settings, "fadeEnd", 0.1f);
            Vector3 planeNormal = GetVector3(settings, "planeNormal", Vector3.up);
            float planeDistance = GetFloat(settings, "planeDistance", 0.0f);

            mat.SetVector("_SSPRParams", new Vector4(intensity, fresnel, fadeStart, fadeEnd));
            mat.SetVector("_ReflectionPlane", new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, planeDistance));
        }
        else if (feature.GetType().Name == "SSRRenderFeature")
        {
            float intensity = GetFloat(settings, "intensity", 0.75f);
            float maxSteps = GetIntAsFloat(settings, "maxSteps", 48);
            float stride = GetFloat(settings, "stride", 0.25f);
            float thickness = GetFloat(settings, "thickness", 0.03f);
            float maxDistance = GetFloat(settings, "maxDistance", 12.0f);
            float rayStartBias = GetFloat(settings, "rayStartBias", 0.03f);
            float fresnel = GetFloat(settings, "fresnelPower", 4.0f);
            float fadeStart = GetFloat(settings, "fadeStart", 0.0f);
            float fadeEnd = GetFloat(settings, "fadeEnd", 0.12f);
            Vector3 receiverPlaneNormal = GetVector3(settings, "receiverPlaneNormal", Vector3.up);
            float receiverPlaneDistance = GetFloat(settings, "receiverPlaneDistance", 0.0f);
            float receiverNormalThreshold = GetFloat(settings, "receiverNormalThreshold", 0.8f);
            float receiverMaxDistance = GetFloat(settings, "receiverMaxDistance", 0.08f);
            bool useReceiverMask = GetBool(settings, "useReceiverPlaneMask", false);

            mat.SetVector("_SSRParams", new Vector4(intensity, maxSteps, stride, thickness));
            mat.SetVector("_SSRParams2", new Vector4(maxDistance, rayStartBias, fresnel, 0.0f));
            mat.SetVector("_SSRParams3", new Vector4(fadeStart, fadeEnd, 0.0f, 0.0f));
            mat.SetVector("_SSRReceiverPlane", new Vector4(
                receiverPlaneNormal.x,
                receiverPlaneNormal.y,
                receiverPlaneNormal.z,
                receiverPlaneDistance));
            mat.SetVector("_SSRReceiverParams", new Vector4(
                receiverNormalThreshold,
                receiverMaxDistance,
                useReceiverMask ? 1.0f : 0.0f,
                0.0f));
        }

        return mat;
    }

    private static float GetFloat(object settings, string fieldName, float fallback)
    {
        FieldInfo f = settings.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null)
            return fallback;
        object v = f.GetValue(settings);
        return v is float fv ? fv : fallback;
    }

    private static Vector3 GetVector3(object settings, string fieldName, Vector3 fallback)
    {
        FieldInfo f = settings.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null)
            return fallback;
        object v = f.GetValue(settings);
        return v is Vector3 vv ? vv : fallback;
    }

    private static float GetIntAsFloat(object settings, string fieldName, int fallback)
    {
        FieldInfo f = settings.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null)
            return fallback;
        object v = f.GetValue(settings);
        return v is int iv ? iv : fallback;
    }

    private static bool GetBool(object settings, string fieldName, bool fallback)
    {
        FieldInfo f = settings.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (f == null)
            return fallback;
        object v = f.GetValue(settings);
        return v is bool bv ? bv : fallback;
    }
}
