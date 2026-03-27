using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// MCP-friendly stage bridge:
/// - External tool writes a JSON request file.
/// - Unity applies stage/debug params to the target renderer feature.
/// - Bridge exports per-stage console log and writes a response file.
///
/// This keeps screenshot capture on MCP side (manage_camera.screenshot),
/// while preserving multi-stage preview and per-stage logs.
/// </summary>
[InitializeOnLoad]
public static class McpStageCaptureBridge
{
    [Serializable]
    private class StageRequest
    {
        public string command;
        public string featureType;
        public string sessionId;
        public string stageLabel;
        public int runtimeDebugStep = -1;
        public bool enableDebugVisualization = true;
        public bool exportConsole = true;
    }

    [Serializable]
    private class StageResponse
    {
        public bool success;
        public string message;
        public string command;
        public string featureType;
        public string sessionId;
        public string stageLabel;
        public int runtimeDebugStep;
        public string sessionDirectory;
        public string logPath;
        public string reportPath;
        public string settingsSummary;
        public string timestamp;
    }

    private const double PollInterval = 0.2;
    private const string RequestFileName = ".mcp_stage_request.json";
    private const string ResponseFileName = ".mcp_stage_response.json";

    private static readonly string DebugCapturesRoot = Path.Combine(Application.dataPath, "DebugCaptures");
    private static readonly string RequestPath = Path.Combine(DebugCapturesRoot, RequestFileName);
    private static readonly string ResponsePath = Path.Combine(DebugCapturesRoot, ResponseFileName);

    private static double s_LastPollTime;

    static McpStageCaptureBridge()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - s_LastPollTime < PollInterval)
            return;
        s_LastPollTime = now;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        if (!File.Exists(RequestPath))
            return;

        ProcessRequest();
    }

    private static void ProcessRequest()
    {
        StageResponse response = new StageResponse
        {
            success = false,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        try
        {
            EnsureDebugCaptureRoot();
            string json = File.ReadAllText(RequestPath, Encoding.UTF8);
            StageRequest request = JsonUtility.FromJson<StageRequest>(json);

            if (request == null)
            {
                response.message = "Request JSON parse failed.";
                WriteResponse(response);
                return;
            }

            response.command = Safe(request.command);
            response.featureType = Safe(request.featureType);
            response.sessionId = Safe(request.sessionId);
            response.stageLabel = Safe(request.stageLabel);
            response.runtimeDebugStep = request.runtimeDebugStep;

            string command = string.IsNullOrWhiteSpace(request.command) ? "set_stage" : request.command;
            if (command == "begin_session")
            {
                string sessionDir = EnsureSessionDirectory(request);
                string reportPath = Path.Combine(sessionDir, "MCP_Capture_Report.txt");
                File.WriteAllText(reportPath, BuildSessionHeader(request, sessionDir), Encoding.UTF8);

                response.success = true;
                response.message = "Session initialized.";
                response.sessionDirectory = sessionDir;
                response.reportPath = reportPath;
                WriteResponse(response);
                return;
            }

            if (command == "end_session")
            {
                string sessionDir = EnsureSessionDirectory(request);
                string reportPath = Path.Combine(sessionDir, "MCP_Capture_Report.txt");
                AppendReportLine(reportPath, $"[END] {response.timestamp} feature={request.featureType}");

                response.success = true;
                response.message = "Session finalized.";
                response.sessionDirectory = sessionDir;
                response.reportPath = reportPath;
                WriteResponse(response);
                return;
            }

            if (!TryFindFeature(request.featureType, out ScriptableRendererFeature feature))
            {
                response.message = $"Feature not found: {request.featureType}";
                WriteResponse(response);
                return;
            }

            feature.SetActive(true);
            ApplyStage(feature, request.enableDebugVisualization, request.runtimeDebugStep);
            EditorUtility.SetDirty(feature);
            AssetDatabase.SaveAssets();

            string sessionDirectory = EnsureSessionDirectory(request);
            string report = Path.Combine(sessionDirectory, "MCP_Capture_Report.txt");
            string summary = SerializeFeatureSettings(feature);
            string logPath = string.Empty;
            if (request.exportConsole)
            {
                string safeLabel = SanitizeFileName(string.IsNullOrWhiteSpace(request.stageLabel) ? "Stage" : request.stageLabel);
                string fileName = $"{DateTime.Now:HHmmss}_{safeLabel}_Console.txt";
                logPath = Path.Combine(sessionDirectory, fileName);
                EditorConsoleLogger.ExportLogs(logPath, 200);
            }

            AppendReportLine(report, BuildStageLine(request, logPath, summary));

            response.success = true;
            response.message = "Stage applied.";
            response.sessionDirectory = sessionDirectory;
            response.logPath = logPath;
            response.reportPath = report;
            response.settingsSummary = summary;
            WriteResponse(response);
        }
        catch (Exception ex)
        {
            response.success = false;
            response.message = $"Exception: {ex.Message}";
            WriteResponse(response);
        }
        finally
        {
            try
            {
                File.Delete(RequestPath);
            }
            catch
            {
                // Ignore cleanup failure; next poll will retry.
            }
        }
    }

    private static void ApplyStage(ScriptableRendererFeature feature, bool enableDebugVisualization, int runtimeDebugStep)
    {
        FieldInfo settingsField = feature.GetType().GetField("settings", BindingFlags.Public | BindingFlags.Instance);
        if (settingsField == null)
            return;

        object settings = settingsField.GetValue(feature);
        if (settings == null)
            return;

        FieldInfo debugVisualizationField = settings.GetType().GetField("enableDebugVisualization", BindingFlags.Public | BindingFlags.Instance);
        if (debugVisualizationField != null && debugVisualizationField.FieldType == typeof(bool))
            debugVisualizationField.SetValue(settings, enableDebugVisualization);

        if (runtimeDebugStep < 0)
            return;

        FieldInfo runtimeStepField = settings.GetType().GetField("runtimeDebugStep", BindingFlags.Public | BindingFlags.Instance);
        if (runtimeStepField != null && runtimeStepField.FieldType == typeof(int))
        {
            runtimeStepField.SetValue(settings, runtimeDebugStep);
            return;
        }

        FieldInfo fallbackStepField = settings.GetType().GetField("debugStep", BindingFlags.Public | BindingFlags.Instance);
        if (fallbackStepField != null && fallbackStepField.FieldType == typeof(int))
            fallbackStepField.SetValue(settings, runtimeDebugStep);
    }

    private static bool TryFindFeature(string featureTypeName, out ScriptableRendererFeature feature)
    {
        feature = null;
        if (string.IsNullOrWhiteSpace(featureTypeName))
            return false;

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

    private static string EnsureSessionDirectory(StageRequest request)
    {
        EnsureDebugCaptureRoot();
        string root = Path.Combine(DebugCapturesRoot, "MCP");
        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);

        string session = string.IsNullOrWhiteSpace(request.sessionId)
            ? DateTime.Now.ToString("yyyyMMdd_HHmmss")
            : SanitizeFileName(request.sessionId);

        string dir = Path.Combine(root, session);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return dir;
    }

    private static void EnsureDebugCaptureRoot()
    {
        if (!Directory.Exists(DebugCapturesRoot))
            Directory.CreateDirectory(DebugCapturesRoot);
    }

    private static string BuildSessionHeader(StageRequest request, string sessionDir)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("MCP Stage Capture Report");
        sb.AppendLine("========================");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Session: {request.sessionId}");
        sb.AppendLine($"Feature: {request.featureType}");
        sb.AppendLine($"Directory: {sessionDir}");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string BuildStageLine(StageRequest request, string logPath, string summary)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[STAGE] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"stageLabel={request.stageLabel}");
        sb.AppendLine($"runtimeDebugStep={request.runtimeDebugStep}");
        sb.AppendLine($"log={logPath}");
        if (!string.IsNullOrWhiteSpace(summary))
        {
            sb.AppendLine("settings:");
            sb.AppendLine(summary);
        }
        sb.AppendLine();
        return sb.ToString();
    }

    private static void AppendReportLine(string path, string content)
    {
        File.AppendAllText(path, content + Environment.NewLine, Encoding.UTF8);
    }

    private static string SerializeFeatureSettings(ScriptableRendererFeature feature)
    {
        FieldInfo settingsField = feature.GetType().GetField("settings", BindingFlags.Public | BindingFlags.Instance);
        if (settingsField == null)
            return string.Empty;

        object settings = settingsField.GetValue(feature);
        if (settings == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        FieldInfo[] fields = settings.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(settings);
            if (value == null)
                continue;

            Type t = field.FieldType;
            if (t.IsPrimitive || t.IsEnum || t == typeof(string) ||
                t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) || t == typeof(Color))
            {
                sb.AppendLine($"{field.Name}: {value}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static void WriteResponse(StageResponse response)
    {
        try
        {
            EnsureDebugCaptureRoot();
            string json = JsonUtility.ToJson(response, true);
            File.WriteAllText(ResponsePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[McpStageCaptureBridge] Failed to write response: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Session";

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static string Safe(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
    }
}
