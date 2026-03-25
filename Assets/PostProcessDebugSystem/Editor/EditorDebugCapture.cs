using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

/// <summary>
/// Editor模式专用捕获器
/// 支持在不运行游戏的情况下捕获SceneView
/// </summary>
public class EditorDebugCapture
{
    public static void Capture(string effectName, string featureSettings = null, System.Action onComplete = null)
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string dir = GetOutputDirectory();
        string imagePath = Path.Combine(dir, $"{effectName}_{timestamp}.png");

        // 捕获SceneView截图
        Texture2D screenshot = CaptureSceneView();
        if (screenshot != null)
        {
            SaveTexture(screenshot, imagePath);
            Object.DestroyImmediate(screenshot);
        }
        else
        {
            EditorUtility.DisplayDialog("捕获失败",
                "无法捕获SceneView。\n请确保SceneView窗口打开且可见。",
                "确定");
            return;
        }

        // 导出日志（Editor模式使用EditorConsoleLogger）
        string logPath = Path.Combine(dir, $"{effectName}_{timestamp}_Console.txt");
        EditorConsoleLogger.ExportLogs(logPath, 100);

        // 生成报告
        string reportPath = Path.Combine(dir, $"{effectName}_{timestamp}_Report.txt");
        GenerateReport(reportPath, timestamp, effectName, featureSettings);

        AssetDatabase.Refresh();

        Debug.Log($"[Debug] ✅ Editor模式捕获完成！\n" +
                  $"截图: {Path.GetFileName(imagePath)}\n" +
                  $"位置: {dir}");

        onComplete?.Invoke();
    }

    private static Texture2D CaptureSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            // 尝试查找任意打开的SceneView
            SceneView[] views = Resources.FindObjectsOfTypeAll<SceneView>();
            if (views.Length > 0)
                sceneView = views[0];
        }

        if (sceneView == null)
            return null;

        // 获取SceneView的相机和尺寸
        Camera cam = sceneView.camera;
        int width = (int)sceneView.position.width;
        int height = (int)sceneView.position.height;

        // 创建RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture previousRT = cam.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            // 渲染到RenderTexture
            cam.targetTexture = rt;
            cam.Render();

            // 读取像素
            RenderTexture.active = rt;
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            return screenshot;
        }
        finally
        {
            // 恢复原始状态
            cam.targetTexture = previousRT;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(rt);
        }
    }

    private static string GetOutputDirectory()
    {
        string dir = Path.Combine(Application.dataPath, "DebugCaptures");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    private static void SaveTexture(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    private static void GenerateReport(string path, string timestamp, string effectName, string featureSettings = null)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{effectName} - Debug Report (Editor Mode)");
        sb.AppendLine("=" + new string('=', 78));
        sb.AppendLine($"Timestamp: {timestamp}");
        sb.AppendLine($"Time: {System.DateTime.Now}");
        sb.AppendLine($"Mode: Editor (非运行时)");
        sb.AppendLine();

        sb.AppendLine("=== System ===");
        sb.AppendLine($"Unity: {Application.unityVersion}");
        sb.AppendLine($"Platform: {Application.platform}");
        sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
        sb.AppendLine($"API: {SystemInfo.graphicsDeviceType}");
        sb.AppendLine($"VRAM: {SystemInfo.graphicsMemorySize} MB");
        sb.AppendLine();

        // SceneView相机信息
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.camera != null)
        {
            Camera cam = sceneView.camera;
            sb.AppendLine("=== SceneView Camera ===");
            sb.AppendLine($"Position: {cam.transform.position}");
            sb.AppendLine($"Rotation: {cam.transform.eulerAngles}");
            sb.AppendLine($"FOV: {cam.fieldOfView}");
            sb.AppendLine($"Near: {cam.nearClipPlane} | Far: {cam.farClipPlane}");
            sb.AppendLine();
        }

        var stats = EditorConsoleLogger.GetStats();
        sb.AppendLine("=== Console Stats ===");
        sb.AppendLine($"Errors: {stats.errors}");
        sb.AppendLine($"Warnings: {stats.warnings}");
        sb.AppendLine($"Logs: {stats.logs}");
        sb.AppendLine();

        sb.AppendLine("⚠️ 注意：Editor模式捕获限制");
        sb.AppendLine("• 无法获取运行时性能数据（FPS等）");
        sb.AppendLine("• 后处理效果可能未完全渲染");
        sb.AppendLine("• 建议在Play Mode下捕获以获得完整信息");
        sb.AppendLine();

        // 自动收集场景对象信息
        GameObject testObject = GameObject.Find("测试物体");
        if (testObject != null)
        {
            sb.AppendLine("=== Scene Objects ===");
            sb.AppendLine($"Parent: {testObject.name}");
            sb.AppendLine($"  Position: {testObject.transform.position}");
            sb.AppendLine($"  Rotation: {testObject.transform.eulerAngles}");
            sb.AppendLine($"  Scale: {testObject.transform.localScale}");
            sb.AppendLine();

            // 遍历所有子对象
            foreach (Transform child in testObject.transform)
            {
                sb.AppendLine($"Child: {child.name}");
                sb.AppendLine($"  Position: {child.position}");
                sb.AppendLine($"  Local Position: {child.localPosition}");
                sb.AppendLine($"  Rotation: {child.eulerAngles}");
                sb.AppendLine($"  Scale: {child.localScale}");

                // 获取Renderer信息（如果有）
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    sb.AppendLine($"  Bounds Center: {renderer.bounds.center}");
                    sb.AppendLine($"  Bounds Size: {renderer.bounds.size}");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("=== Scene Objects ===");
            sb.AppendLine("⚠️ 未找到名为'测试物体'的GameObject");
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(featureSettings))
        {
            sb.AppendLine("=== Feature Settings ===");
            sb.AppendLine(featureSettings);
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }
}
