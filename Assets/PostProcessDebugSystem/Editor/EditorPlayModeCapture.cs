using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingGraphics = System.Drawing.Graphics;

/// <summary>
/// Captures the GameView directly from the editor while Play Mode is running.
/// This avoids runtime coroutines and WaitForEndOfFrame dependencies.
/// </summary>
public static class EditorPlayModeCapture
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RectIntInterop rect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    private const int SwRestore = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct RectIntInterop
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private static string s_EffectName;
    private static string s_FeatureSettings;
    private static string s_CompletionMarkerPath;
    private static Action s_OnComplete;
    private static double s_CaptureAt;
    private static bool s_IsScheduled;
    private static int s_Attempts;

    public static void Schedule(string effectName, string featureSettings, string completionMarkerPath, Action onComplete)
    {
        Cancel();

        s_EffectName = effectName;
        s_FeatureSettings = featureSettings;
        s_CompletionMarkerPath = completionMarkerPath;
        s_OnComplete = onComplete;
        s_CaptureAt = EditorApplication.timeSinceStartup + 3.0;
        s_Attempts = 0;
        s_IsScheduled = true;

        FocusGameView();
        EditorApplication.update += OnEditorUpdate;
        Debug.Log($"[EditorPlayModeCapture] Scheduled GameView capture for {effectName}.");
    }

    public static void Cancel()
    {
        if (!s_IsScheduled)
            return;

        s_IsScheduled = false;
        EditorApplication.update -= OnEditorUpdate;
        s_EffectName = null;
        s_FeatureSettings = null;
        s_CompletionMarkerPath = null;
        s_OnComplete = null;
        s_CaptureAt = 0.0;
        s_Attempts = 0;
    }

    private static void OnEditorUpdate()
    {
        if (!s_IsScheduled)
            return;

        if (!EditorApplication.isPlaying)
            return;

        if (EditorApplication.timeSinceStartup < s_CaptureAt)
            return;

        FocusGameView();
        Texture2D screenshot = TryCaptureGameView();
        s_Attempts++;

        if (screenshot == null || screenshot.width <= 4 || screenshot.height <= 4)
        {
            if (screenshot != null)
                UnityEngine.Object.DestroyImmediate(screenshot);

            if (s_Attempts < 5)
            {
                s_CaptureAt = EditorApplication.timeSinceStartup + 0.5;
                Debug.LogWarning("[EditorPlayModeCapture] GameView capture returned no usable image, retrying.");
                return;
            }

            Debug.LogError("[EditorPlayModeCapture] Failed to capture GameView after multiple attempts.");
            Complete();
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string dir = GetOutputDirectory();
        string imagePath = Path.Combine(dir, $"{s_EffectName}_{timestamp}.png");
        string logPath = Path.Combine(dir, $"{s_EffectName}_{timestamp}_Console.txt");
        string reportPath = Path.Combine(dir, $"{s_EffectName}_{timestamp}_Report.txt");

        SaveTexture(screenshot, imagePath);
        UnityEngine.Object.DestroyImmediate(screenshot);

        if (Application.isPlaying)
            ConsoleLogger.ExportLogs(logPath, 100);
        else
            EditorConsoleLogger.ExportLogs(logPath, 100);

        GenerateReport(reportPath, timestamp, imagePath, logPath);
        WriteCompletionMarker(imagePath, reportPath);

        Debug.Log($"[EditorPlayModeCapture] Capture completed: {Path.GetFileName(imagePath)}");
        Complete();
    }

    private static void Complete()
    {
        Action onComplete = s_OnComplete;
        Cancel();
        onComplete?.Invoke();
    }

    private static void FocusGameView()
    {
        Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType == null)
            return;

        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        if (gameView == null)
            return;

        gameView.Show();
        gameView.Focus();
        gameView.Repaint();
        InternalEditorUtility.RepaintAllViews();
        BringUnityMainWindowToFront();
    }

    private static Texture2D TryCaptureGameView()
    {
        Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType == null)
            return null;

        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        if (gameView == null)
            return null;

        BringUnityMainWindowToFront();

        Texture2D fromScreen = TryCaptureFromUnityWindow();
        if (fromScreen != null)
            return fromScreen;

        fromScreen = TryCaptureFromScreen(gameViewType, gameView);
        if (fromScreen != null)
            return fromScreen;

        return TryCaptureFromRenderTexture(gameViewType, gameView);
    }

    private static void BringUnityMainWindowToFront()
    {
        try
        {
            IntPtr handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (handle == IntPtr.Zero)
                return;

            ShowWindow(handle, SwRestore);
            SetForegroundWindow(handle);
        }
        catch
        {
            // Best effort only. Screen capture will fall back to render texture if this fails.
        }
    }

    private static Texture2D TryCaptureFromUnityWindow()
    {
        IntPtr handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        if (handle == IntPtr.Zero)
        {
            Debug.Log("[EditorPlayModeCapture] Unity window capture skipped: MainWindowHandle is zero.");
            return null;
        }

        if (!GetWindowRect(handle, out RectIntInterop rect))
        {
            Debug.Log("[EditorPlayModeCapture] Unity window capture skipped: GetWindowRect failed.");
            return null;
        }

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 4 || height <= 4)
        {
            Debug.Log($"[EditorPlayModeCapture] Unity window capture skipped: invalid size {width}x{height}.");
            return null;
        }

        Texture2D screenshot = CaptureWindowWithGdi(rect.Left, rect.Top, width, height);
        if (screenshot == null)
            return null;

        Debug.Log($"[EditorPlayModeCapture] Captured from Unity window: {width}x{height}");
        return screenshot;
    }

    private static Texture2D CaptureWindowWithGdi(int x, int y, int width, int height)
    {
        try
        {
            using (DrawingBitmap bitmap = new DrawingBitmap(width, height))
            using (DrawingGraphics graphics = DrawingGraphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                bool printed = false;
                try
                {
                    IntPtr handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                    if (handle != IntPtr.Zero)
                        printed = PrintWindow(handle, hdc, 0);
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }

                if (!printed)
                    graphics.CopyFromScreen(x, y, 0, 0, bitmap.Size);

                Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

                for (int py = 0; py < height; py++)
                {
                    int flippedY = height - 1 - py;
                    for (int px = 0; px < width; px++)
                    {
                        System.Drawing.Color c = bitmap.GetPixel(px, py);
                        texture.SetPixel(px, flippedY, new UnityEngine.Color32(c.R, c.G, c.B, c.A));
                    }
                }

                texture.Apply();
                return texture;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EditorPlayModeCapture] Unity window GDI capture failed: {ex.Message}");
            return null;
        }
    }

    private static Texture2D TryCaptureFromRenderTexture(Type gameViewType, EditorWindow gameView)
    {
        FieldInfo renderTextureField = gameViewType.GetField("m_RenderTexture", BindingFlags.Instance | BindingFlags.NonPublic);
        if (renderTextureField == null)
            return null;

        RenderTexture renderTexture = renderTextureField.GetValue(gameView) as RenderTexture;
        if (renderTexture == null || renderTexture.width <= 4 || renderTexture.height <= 4)
            return null;

        RenderTexture previousActive = RenderTexture.active;
        try
        {
            RenderTexture.active = renderTexture;
            Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            screenshot.Apply();
            FlipVertical(screenshot);
            Debug.Log($"[EditorPlayModeCapture] Captured from GameView RenderTexture: {renderTexture.width}x{renderTexture.height}");
            return screenshot;
        }
        finally
        {
            RenderTexture.active = previousActive;
        }
    }

    private static Texture2D TryCaptureFromScreen(Type gameViewType, EditorWindow gameView)
    {
        Rect pixelRect = GetGameViewPixelRect(gameViewType, gameView);
        int width = Mathf.RoundToInt(pixelRect.width);
        int height = Mathf.RoundToInt(pixelRect.height);
        if (width <= 4 || height <= 4)
            return null;

        float screenHeight = Screen.currentResolution.height > 0
            ? Screen.currentResolution.height
            : Display.main.systemHeight;
        Vector2 topLeft = new Vector2(pixelRect.x, screenHeight - pixelRect.y - height);
        MethodInfo readScreenPixel = typeof(InternalEditorUtility).GetMethod(
            "ReadScreenPixel",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { typeof(Vector2), typeof(int), typeof(int) },
            null);
        if (readScreenPixel == null)
            return null;

        object result = readScreenPixel.Invoke(null, new object[] { topLeft, width, height });
        Texture2D screenshot = ConvertCaptureResult(result, width, height);
        if (screenshot == null)
            return null;

        FlipVertical(screenshot);
        Debug.Log($"[EditorPlayModeCapture] Captured from on-screen GameView: {width}x{height}");
        return screenshot;
    }

    private static Texture2D ConvertCaptureResult(object result, int width, int height)
    {
        if (result is Texture2D texture)
            return texture;

        if (result is UnityEngine.Color[] pixels && pixels.Length > 0)
        {
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.SetPixels(pixels);
            screenshot.Apply();
            return screenshot;
        }

        return null;
    }

    private static Rect GetGameViewPixelRect(Type gameViewType, EditorWindow gameView)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo targetInViewProperty = gameViewType.GetProperty("targetInView", flags);
        PropertyInfo viewInWindowProperty = gameViewType.GetProperty("viewInWindow", flags);
        if (targetInViewProperty != null)
        {
            Rect targetInView = (Rect)targetInViewProperty.GetValue(gameView, null);
            Rect viewInWindow = viewInWindowProperty != null
                ? (Rect)viewInWindowProperty.GetValue(gameView, null)
                : new Rect(0f, 0f, 0f, 0f);

            if (targetInView.width > 4f && targetInView.height > 4f)
            {
                float pixelScale = EditorGUIUtility.pixelsPerPoint;
                Rect windowPos = gameView.position;
                return new Rect(
                    (windowPos.x + viewInWindow.x + targetInView.x) * pixelScale,
                    (windowPos.y + viewInWindow.y + targetInView.y) * pixelScale,
                    targetInView.width * pixelScale,
                    targetInView.height * pixelScale);
            }
        }

        MethodInfo getViewInWindow = gameViewType.GetMethod("GetViewInWindow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo getViewPixelRect = gameViewType.GetMethod("GetViewPixelRect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (getViewInWindow != null && getViewPixelRect != null)
        {
            Rect viewInWindow = (Rect)getViewInWindow.Invoke(gameView, new object[] { gameView.position });
            Rect pixelRect = (Rect)getViewPixelRect.Invoke(gameView, new object[] { viewInWindow });
            if (pixelRect.width > 4 && pixelRect.height > 4)
                return pixelRect;
        }

        float scale = EditorGUIUtility.pixelsPerPoint;
        Rect pos = gameView.position;
        return new Rect(pos.x * scale, pos.y * scale, pos.width * scale, pos.height * scale);
    }

    private static void FlipVertical(Texture2D texture)
    {
        UnityEngine.Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;

        for (int y = 0; y < height / 2; y++)
        {
            int oppositeY = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                int a = y * width + x;
                int b = oppositeY * width + x;
                UnityEngine.Color temp = pixels[a];
                pixels[a] = pixels[b];
                pixels[b] = temp;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    private static string GetOutputDirectory()
    {
        string dir = Path.Combine(Application.dataPath, "DebugCaptures");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    private static void SaveTexture(Texture2D texture, string path)
    {
        File.WriteAllBytes(path, texture.EncodeToPNG());
    }

    private static void WriteCompletionMarker(string imagePath, string reportPath)
    {
        if (string.IsNullOrEmpty(s_CompletionMarkerPath))
            return;

        string markerDir = Path.GetDirectoryName(s_CompletionMarkerPath);
        if (!string.IsNullOrEmpty(markerDir) && !Directory.Exists(markerDir))
            Directory.CreateDirectory(markerDir);

        File.WriteAllText(s_CompletionMarkerPath, imagePath + "\n" + reportPath, Encoding.UTF8);
    }

    private static void GenerateReport(string reportPath, string timestamp, string imagePath, string logPath)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{s_EffectName} - Debug Report (Editor GameView Capture)");
        sb.AppendLine("=" + new string('=', 78));
        sb.AppendLine($"Timestamp: {timestamp}");
        sb.AppendLine($"Time: {DateTime.Now}");
        sb.AppendLine($"Mode: Play Mode / Editor-side GameView capture");
        sb.AppendLine($"Image: {Path.GetFileName(imagePath)}");
        sb.AppendLine($"Console: {Path.GetFileName(logPath)}");
        sb.AppendLine();

        sb.AppendLine("=== System ===");
        sb.AppendLine($"Unity: {Application.unityVersion}");
        sb.AppendLine($"Platform: {Application.platform}");
        sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
        sb.AppendLine($"API: {SystemInfo.graphicsDeviceType}");
        sb.AppendLine();

        Camera cam = Camera.main;
        if (cam != null)
        {
            sb.AppendLine("=== Main Camera ===");
            sb.AppendLine($"Name: {cam.name}");
            sb.AppendLine($"Position: {cam.transform.position}");
            sb.AppendLine($"Rotation: {cam.transform.eulerAngles}");
            sb.AppendLine($"FOV: {cam.fieldOfView}");
            sb.AppendLine($"Near: {cam.nearClipPlane} | Far: {cam.farClipPlane}");
            sb.AppendLine($"Pixel Size: {cam.pixelWidth}x{cam.pixelHeight}");
            sb.AppendLine();
        }

        var stats = Application.isPlaying ? ConsoleLogger.GetStats() : EditorConsoleLogger.GetStats();
        sb.AppendLine("=== Console Stats ===");
        sb.AppendLine($"Errors: {stats.errors}");
        sb.AppendLine($"Warnings: {stats.warnings}");
        sb.AppendLine($"Logs: {stats.logs}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(s_FeatureSettings))
        {
            sb.AppendLine("=== Feature Settings ===");
            sb.AppendLine(s_FeatureSettings);
            sb.AppendLine();
        }

        File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
    }
}

