using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

/// <summary>
/// 通用Debug数据捕获器
/// 捕获：截图 + Console日志 + 系统诊断 + Feature配置 + 中间步骤可视化
/// </summary>
public class DebugDataCapture : MonoBehaviour
{
    private string effectName;
    private System.Action onComplete;
    private string featureSettings;
    private Material debugMaterial;
    private int[] debugSteps;
    private string completionMarkerPath;

    public static void Capture(string effectName, string featureSettings = null, Material debugMaterial = null, int[] debugSteps = null, System.Action onComplete = null)
    {
        Capture(effectName, featureSettings, debugMaterial, debugSteps, null, onComplete);
    }

    public static void Capture(string effectName, string featureSettings = null, Material debugMaterial = null, int[] debugSteps = null, string completionMarkerPath = null, System.Action onComplete = null)
    {
        GameObject obj = new GameObject("[DebugCapture]");
        obj.hideFlags = HideFlags.HideAndDontSave;
        DebugDataCapture capture = obj.AddComponent<DebugDataCapture>();
        capture.effectName = effectName;
        capture.featureSettings = featureSettings;
        capture.debugMaterial = debugMaterial;
        capture.debugSteps = debugSteps ?? new int[] { 3, 2, 4, 7 }; // WorldPosition, DistanceToPlane, ReflectedUV, FinalResult
        capture.completionMarkerPath = completionMarkerPath;
        capture.onComplete = onComplete;
        capture.StartCoroutine(capture.CaptureCoroutine());
    }

    private IEnumerator CaptureCoroutine()
    {
        Debug.Log($"[Debug] 开始捕获 {effectName}...");
        bool lightweightCapture = !string.IsNullOrEmpty(effectName) && effectName.Contains("SSR");

        // Give the camera a couple of frames to settle after entering play mode.
        for (int i = 0; i < 4; i++)
            yield return null;
        Debug.Log("[DebugCapture] Warm-up completed.");

        // Capture the final composited frame.
        yield return new WaitForEndOfFrame();
        Debug.Log("[DebugCapture] EndOfFrame reached.");

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string dir = GetOutputDirectory();

        // 捕获GameView截图
        Texture2D screenshot = CaptureScreen();

        // 捕获中间步骤可视化
        Texture2D stepDebug = null;
        if (!lightweightCapture && debugMaterial != null)
        {
            stepDebug = CaptureStepDebug(screenshot);
        }

        // 创建统一的可视化面板（GameView + 2x2步骤 + 文本标注）
        Texture2D unifiedPanel = CreateUnifiedVisualization(screenshot, stepDebug);
        string imagePath = Path.Combine(dir, $"{effectName}_{timestamp}.png");
        SaveTexture(unifiedPanel, imagePath);

        // 生成完整报告（包含Console日志）
        string reportPath = Path.Combine(dir, $"{effectName}_{timestamp}_Report.txt");
        GenerateReport(reportPath, timestamp);

        WriteCompletionMarker(imagePath, reportPath);

        Debug.Log($"[Debug] ✅ 捕获完成！\n" +
                  $"面板: {Path.GetFileName(imagePath)}\n" +
                  $"报告: {Path.GetFileName(reportPath)}\n" +
                  $"位置: {dir}");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        onComplete?.Invoke();

        // 清理临时纹理
        Destroy(screenshot);
        if (stepDebug != null) Destroy(stepDebug);
        Destroy(unifiedPanel);
        Destroy(gameObject);
    }

    private void WriteCompletionMarker(string imagePath, string reportPath)
    {
        if (string.IsNullOrEmpty(completionMarkerPath))
            return;

        string markerDir = Path.GetDirectoryName(completionMarkerPath);
        if (!string.IsNullOrEmpty(markerDir) && !Directory.Exists(markerDir))
            Directory.CreateDirectory(markerDir);

        File.WriteAllText(completionMarkerPath, imagePath + "\n" + reportPath, Encoding.UTF8);
    }

    private string GetOutputDirectory()
    {
        string dir = Path.Combine(Application.dataPath, "DebugCaptures");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    private Texture2D CaptureScreen()
    {
        Texture2D screenTex = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenTex != null)
        {
            Debug.Log($"[DebugCapture] 使用 ScreenCapture 捕获最终画面: {screenTex.width}x{screenTex.height}");
            return screenTex;
        }

        // 回退：使用Camera的实际渲染尺寸，而不是Screen（避免Game View尺寸不匹配）
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[DebugCapture] 未找到Main Camera！");
            return new Texture2D(1, 1);
        }

        int width = cam.pixelWidth;
        int height = cam.pixelHeight;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        return tex;
    }

    private Texture2D CaptureStepDebug(Texture2D screenSource)
    {
        Camera cam = Camera.main;
        if (cam == null || debugMaterial == null)
            return null;

        try
        {
            // ⚠️ 关键：手动绑定深度纹理（Graphics.Blit不会自动传递）
            Texture depthTex = Shader.GetGlobalTexture("_CameraDepthTexture");
            Texture normalsTex = Shader.GetGlobalTexture("_CameraNormalsTexture");
            if (depthTex != null)
            {
                Debug.Log($"[DebugCapture] 深度纹理尺寸: {depthTex.width}x{depthTex.height}");
                debugMaterial.SetTexture("_CameraDepthTexture", depthTex);
            }
            else
            {
                Debug.LogError("[DebugCapture] ⚠️ 深度纹理为null！请在URP Renderer Asset中勾选 'Depth Texture'");
                Debug.LogError("[DebugCapture] 路径: Project Settings > Graphics > Scriptable Render Pipeline Settings");
            }
            if (normalsTex != null)
            {
                debugMaterial.SetTexture("_CameraNormalsTexture", normalsTex);
            }

            // ⚠️ 关键：重新设置VP矩阵（必须在运行时设置）
            UnityEngine.Matrix4x4 view = cam.worldToCameraMatrix;
            UnityEngine.Matrix4x4 proj = UnityEngine.GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            UnityEngine.Matrix4x4 vp = proj * view;
            UnityEngine.Matrix4x4 invVp = vp.inverse;
            debugMaterial.SetMatrix("_CustomMatrixVP", vp);
            debugMaterial.SetMatrix("_SSPRViewProj", vp);
            debugMaterial.SetMatrix("_SSPRInvViewProj", invVp);
            debugMaterial.SetMatrix("_SSRView", view);
            debugMaterial.SetMatrix("_SSRProj", proj);
            debugMaterial.SetMatrix("_SSRViewProj", vp);
            debugMaterial.SetMatrix("_SSRInvViewProj", invVp);

            // ⚠️ 关键：设置worldPos重建参数
            UnityEngine.Matrix4x4 cview = view;
            cview.SetColumn(3, new UnityEngine.Vector4(0, 0, 0, 1));

            float tanHalfFOV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float aspect = cam.aspect;

            UnityEngine.Vector3 topLeftView = new UnityEngine.Vector3(-tanHalfFOV * aspect, tanHalfFOV, -1);
            UnityEngine.Vector3 topRightView = new UnityEngine.Vector3(tanHalfFOV * aspect, tanHalfFOV, -1);
            UnityEngine.Vector3 bottomLeftView = new UnityEngine.Vector3(-tanHalfFOV * aspect, -tanHalfFOV, -1);

            UnityEngine.Matrix4x4 cviewInv = cview.inverse;
            UnityEngine.Vector3 topLeft = cviewInv.MultiplyPoint3x4(topLeftView);
            UnityEngine.Vector3 topRight = cviewInv.MultiplyPoint3x4(topRightView);
            UnityEngine.Vector3 bottomLeft = cviewInv.MultiplyPoint3x4(bottomLeftView);

            UnityEngine.Vector4 xExtent = topRight - topLeft;
            UnityEngine.Vector4 yExtent = bottomLeft - topLeft;

            debugMaterial.SetVector("_CameraViewTopLeftCorner", topLeft);
            debugMaterial.SetVector("_CameraViewXExtent", xExtent);
            debugMaterial.SetVector("_CameraViewYExtent", yExtent);
            debugMaterial.SetVector("_ProjectionParams2", new UnityEngine.Vector4(1.0f / cam.nearClipPlane, 0, 0, 0));

            // 手动计算ZBufferParams（替代Unity builtin）
            float near = cam.nearClipPlane;
            float far = cam.farClipPlane;
            float x = 1.0f - far / near;
            float y = far / near;
            float z = x / far;
            float w = y / far;
            debugMaterial.SetVector("_ManualZBufferParams", new UnityEngine.Vector4(x, y, z, w));
            debugMaterial.SetVector("_ManualCameraPos", cam.transform.position);

            Debug.Log($"[DebugCapture] WorldPos重建参数:\n" +
                      $"  TopLeft: {topLeft}\n" +
                      $"  XExtent: {xExtent}\n" +
                      $"  YExtent: {yExtent}\n" +
                      $"  ProjectionParams2.x: {1.0f / cam.nearClipPlane}");

            Debug.Log($"[DebugCapture] View矩阵:\n{view}");
            Debug.Log($"[DebugCapture] Proj矩阵:\n{proj}");
            Debug.Log($"[DebugCapture] VP矩阵:\n{vp}");
            Debug.Log($"[DebugCapture] 相机Pos:{cam.transform.position} Rot:{cam.transform.eulerAngles}");

            int stepSize = effectName != null && effectName.Contains("SSR") ? 512 : 1024;
            int gridSize = 2;
            int totalSize = stepSize * gridSize;

            // Reuse the already captured final screen instead of reading the back buffer again.
            Texture2D screenCopy = screenSource;
            int width = screenCopy != null ? screenCopy.width : cam.pixelWidth;
            int height = screenCopy != null ? screenCopy.height : cam.pixelHeight;
            if (screenCopy == null)
            {
                Debug.LogError("[DebugCapture] 缺少主截图，无法生成步骤调试图。");
                return null;
            }

            // 设置到材质
            debugMaterial.SetTexture("_BaseMap", screenCopy);
            debugMaterial.SetVector("_SSRScreenSize", new Vector4(width, height, 1.0f / width, 1.0f / height));

            // 绑定SSPR全局资源（Offset和屏幕尺寸）
            Texture ssprOffset = Shader.GetGlobalTexture("_SSPR_Offset");
            if (ssprOffset != null)
            {
                debugMaterial.SetTexture("_SSPR_Offset", ssprOffset);
                debugMaterial.SetVector("_SSPRScreenSize", new Vector4(ssprOffset.width, ssprOffset.height, 1.0f / ssprOffset.width, 1.0f / ssprOffset.height));
            }

            // 创建临时RT用于各个步骤
            RenderTexture[] stepRTs = new RenderTexture[4];
            for (int i = 0; i < 4; i++)
            {
                stepRTs[i] = RenderTexture.GetTemporary(stepSize, stepSize, 0, RenderTextureFormat.ARGB32);
            }

            // 创建全屏源RT
            RenderTexture fullscreenRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(screenCopy, fullscreenRT);

            // ⚠️ 在循环前记录材质中的所有向量参数（Feature特有参数，如SSPR的_ReflectionPlane）
            // 使用HasProperty检查以保证通用性
            UnityEngine.Vector4 sspr_params = UnityEngine.Vector4.zero;
            UnityEngine.Vector4 sspr_params2 = UnityEngine.Vector4.zero;
            UnityEngine.Vector4 sspr_params3 = UnityEngine.Vector4.zero;
            UnityEngine.Vector4 reflection_plane = UnityEngine.Vector4.zero;
            UnityEngine.Vector4 ssr_params = UnityEngine.Vector4.zero;
            UnityEngine.Vector4 ssr_params2 = UnityEngine.Vector4.zero;
            UnityEngine.Vector4 ssr_params3 = UnityEngine.Vector4.zero;
            UnityEngine.Vector4 ssr_receiver_plane = UnityEngine.Vector4.zero;
            UnityEngine.Vector4 ssr_receiver_params = UnityEngine.Vector4.zero;
            bool hasSSPRParams = false;
            bool hasSSPRParams2 = false;
            bool hasSSPRParams3 = false;
            bool hasReflectionPlane = false;
            bool hasSSRParams = false;
            bool hasSSRParams2 = false;
            bool hasSSRParams3 = false;
            bool hasSSRReceiverPlane = false;
            bool hasSSRReceiverParams = false;

            if (debugMaterial.HasProperty("_SSPRParams"))
            {
                sspr_params = debugMaterial.GetVector("_SSPRParams");
                hasSSPRParams = true;
            }
            if (debugMaterial.HasProperty("_SSPRParams2"))
            {
                sspr_params2 = debugMaterial.GetVector("_SSPRParams2");
                hasSSPRParams2 = true;
            }
            if (debugMaterial.HasProperty("_SSPRParams3"))
            {
                sspr_params3 = debugMaterial.GetVector("_SSPRParams3");
                hasSSPRParams3 = true;
            }
            if (debugMaterial.HasProperty("_ReflectionPlane"))
            {
                reflection_plane = debugMaterial.GetVector("_ReflectionPlane");
                hasReflectionPlane = true;
                Debug.Log($"[DebugCapture] 记录_ReflectionPlane = {reflection_plane}");
            }
            if (debugMaterial.HasProperty("_SSRParams"))
            {
                ssr_params = debugMaterial.GetVector("_SSRParams");
                hasSSRParams = true;
            }
            if (debugMaterial.HasProperty("_SSRParams2"))
            {
                ssr_params2 = debugMaterial.GetVector("_SSRParams2");
                hasSSRParams2 = true;
            }
            if (debugMaterial.HasProperty("_SSRParams3"))
            {
                ssr_params3 = debugMaterial.GetVector("_SSRParams3");
                hasSSRParams3 = true;
            }
            if (debugMaterial.HasProperty("_SSRReceiverPlane"))
            {
                ssr_receiver_plane = debugMaterial.GetVector("_SSRReceiverPlane");
                hasSSRReceiverPlane = true;
            }
            if (debugMaterial.HasProperty("_SSRReceiverParams"))
            {
                ssr_receiver_params = debugMaterial.GetVector("_SSRReceiverParams");
                hasSSRReceiverParams = true;
            }

            // 渲染各个步骤（根据debugSteps指定的Pass索引）
            for (int i = 0; i < 4; i++)
            {
                int passIndex = debugSteps[i];

                // ⚠️ 每次Blit前重新设置所有参数（包括SSPR特有参数）
                if (depthTex != null)
                {
                    debugMaterial.SetTexture("_CameraDepthTexture", depthTex);
                }
                if (normalsTex != null)
                {
                    debugMaterial.SetTexture("_CameraNormalsTexture", normalsTex);
                }
                debugMaterial.SetMatrix("_CustomMatrixVP", vp);
                debugMaterial.SetMatrix("_SSPRViewProj", vp);
                debugMaterial.SetMatrix("_SSPRInvViewProj", invVp);
                debugMaterial.SetMatrix("_SSRView", view);
                debugMaterial.SetMatrix("_SSRProj", proj);
                debugMaterial.SetMatrix("_SSRViewProj", vp);
                debugMaterial.SetMatrix("_SSRInvViewProj", invVp);
                debugMaterial.SetVector("_CameraViewTopLeftCorner", topLeft);
                debugMaterial.SetVector("_CameraViewXExtent", xExtent);
                debugMaterial.SetVector("_CameraViewYExtent", yExtent);
                debugMaterial.SetVector("_ProjectionParams2", new UnityEngine.Vector4(1.0f / cam.nearClipPlane, 0, 0, 0));
                debugMaterial.SetVector("_SSRScreenSize", new Vector4(width, height, 1.0f / width, 1.0f / height));

                // 手动参数（每次都设置）
                UnityEngine.Vector4 zBufferParams = new UnityEngine.Vector4(x, y, z, w);
                debugMaterial.SetVector("_ManualZBufferParams", zBufferParams);
                debugMaterial.SetVector("_ManualCameraPos", cam.transform.position);

                // Feature特有参数（每次都重新设置，防止Blit清空）
                if (hasSSPRParams)
                    debugMaterial.SetVector("_SSPRParams", sspr_params);
                if (hasSSPRParams2)
                    debugMaterial.SetVector("_SSPRParams2", sspr_params2);
                if (hasSSPRParams3)
                    debugMaterial.SetVector("_SSPRParams3", sspr_params3);
                if (hasReflectionPlane)
                    debugMaterial.SetVector("_ReflectionPlane", reflection_plane);
                if (hasSSRParams)
                    debugMaterial.SetVector("_SSRParams", ssr_params);
                if (hasSSRParams2)
                    debugMaterial.SetVector("_SSRParams2", ssr_params2);
                if (hasSSRParams3)
                    debugMaterial.SetVector("_SSRParams3", ssr_params3);
                if (hasSSRReceiverPlane)
                    debugMaterial.SetVector("_SSRReceiverPlane", ssr_receiver_plane);
                if (hasSSRReceiverParams)
                    debugMaterial.SetVector("_SSRReceiverParams", ssr_receiver_params);

                Graphics.Blit(fullscreenRT, stepRTs[i], debugMaterial, passIndex);
            }

            // 创建最终合并RT
            RenderTexture finalRT = RenderTexture.GetTemporary(totalSize, totalSize, 0, RenderTextureFormat.ARGB32);

            // 设置步骤纹理到材质
            debugMaterial.SetTexture("_Step1", stepRTs[0]);
            debugMaterial.SetTexture("_Step2", stepRTs[1]);
            debugMaterial.SetTexture("_Step3", stepRTs[2]);
            debugMaterial.SetTexture("_Step4", stepRTs[3]);

            // 使用Pass 8合并（SSPR_StepDebug.shader的最后一个Pass）
            Graphics.Blit(fullscreenRT, finalRT, debugMaterial, 8);

            // 读取到Texture2D
            RenderTexture.active = finalRT;
            Texture2D result = new Texture2D(totalSize, totalSize, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, totalSize, totalSize), 0, 0);
            result.Apply();
            RenderTexture.active = null;

            // 清理
            Destroy(screenCopy);
            RenderTexture.ReleaseTemporary(fullscreenRT);
            RenderTexture.ReleaseTemporary(finalRT);
            for (int i = 0; i < 4; i++)
            {
                RenderTexture.ReleaseTemporary(stepRTs[i]);
            }

            Debug.Log($"[DebugCapture] 成功生成2x2步骤可视化 ({totalSize}x{totalSize})");
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DebugCapture] 步骤可视化生成失败: {e.Message}");
            return null;
        }
    }

    private Texture2D CreateUnifiedVisualization(Texture2D gameView, Texture2D stepDebug)
    {
        int textHeight = 140;
        int padding = 20;

        int finalWidth, finalHeight;
        int gameViewDisplayWidth, gameViewDisplayHeight;
        int stepDebugSize = 0;

        // 计算布局
        if (stepDebug != null)
        {
            stepDebugSize = stepDebug.width; // 2048x2048
            // GameView缩放到与步骤图等高
            float scale = (float)stepDebugSize / gameView.height;
            gameViewDisplayWidth = Mathf.RoundToInt(gameView.width * scale);
            gameViewDisplayHeight = stepDebugSize;

            finalWidth = gameViewDisplayWidth + stepDebugSize + padding * 3;
            finalHeight = stepDebugSize + textHeight + padding * 2;
        }
        else
        {
            // 只有GameView
            gameViewDisplayWidth = gameView.width;
            gameViewDisplayHeight = gameView.height;
            finalWidth = gameViewDisplayWidth + padding * 2;
            finalHeight = gameViewDisplayHeight + textHeight + padding * 2;
        }

        // 创建画布
        Texture2D panel = new Texture2D(finalWidth, finalHeight, TextureFormat.RGB24, false);
        Color[] bgPixels = new Color[finalWidth * finalHeight];
        Color bgColor = new Color(0.15f, 0.15f, 0.15f); // 深灰背景
        for (int i = 0; i < bgPixels.Length; i++) bgPixels[i] = bgColor;
        panel.SetPixels(bgPixels);

        // 绘制GameView（左侧）
        BlitScaled(panel, gameView, padding, padding + textHeight, gameViewDisplayWidth, gameViewDisplayHeight);

        // 绘制步骤图（右侧）
        if (stepDebug != null)
        {
            int stepX = padding * 2 + gameViewDisplayWidth;
            int stepY = padding + textHeight;
            Blit(panel, stepDebug, stepX, stepY);
        }

        // 绘制文本区域
        DrawTextOverlay(panel, padding, padding);

        panel.Apply();
        Debug.Log($"[DebugCapture] 生成统一面板 ({finalWidth}x{finalHeight})");
        return panel;
    }

    private void BlitScaled(Texture2D dest, Texture2D src, int x, int y, int width, int height)
    {
        // 简单的最近邻缩放
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                float u = (float)dx / width;
                float v = (float)dy / height;
                int sx = Mathf.FloorToInt(u * src.width);
                int sy = Mathf.FloorToInt(v * src.height);
                sx = Mathf.Clamp(sx, 0, src.width - 1);
                sy = Mathf.Clamp(sy, 0, src.height - 1);
                Color pixel = src.GetPixel(sx, sy);
                dest.SetPixel(x + dx, y + dy, pixel);
            }
        }
    }

    private void Blit(Texture2D dest, Texture2D src, int x, int y)
    {
        Color[] pixels = src.GetPixels();
        dest.SetPixels(x, y, src.width, src.height, pixels);
    }

    private void DrawTextOverlay(Texture2D tex, int x, int y)
    {
        // 绘制文本背景框
        int boxWidth = tex.width - x * 2;
        int boxHeight = 120;
        Color boxColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        for (int dy = 0; dy < boxHeight; dy++)
        {
            for (int dx = 0; dx < boxWidth; dx++)
            {
                tex.SetPixel(x + dx, y + dy, boxColor);
            }
        }

        // 生成文本内容
        Camera cam = Camera.main;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[{effectName}] {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        if (cam != null)
        {
            sb.Append($"Camera: ({cam.transform.position.x:F1}, {cam.transform.position.y:F1}, {cam.transform.position.z:F1}) ");
            sb.AppendLine($"Rot: ({cam.transform.eulerAngles.x:F0}, {cam.transform.eulerAngles.y:F0}, {cam.transform.eulerAngles.z:F0})");
        }

        // 从featureSettings提取关键参数
        if (!string.IsNullOrEmpty(featureSettings))
        {
            ParseAndDisplaySettings(sb);
        }

        var stats = ConsoleLogger.GetStats();
        sb.Append($"FPS: {(int)(1.0f / Time.deltaTime)} | ");
        sb.Append($"Console: {stats.errors}E {stats.warnings}W {stats.logs}L");

        // 注意：Unity运行时没有GUI.DrawTexture，我们只能绘制简单的像素
        // 用小像素块表示文字（ASCII art风格），或在报告中提供完整文本
        string text = sb.ToString();
        Debug.Log($"[DebugCapture] 面板文本:\n{text}");

        // 在底部绘制边框线
        Color lineColor = new Color(0.3f, 0.6f, 0.9f);
        for (int dx = 0; dx < boxWidth; dx++)
        {
            tex.SetPixel(x + dx, y + boxHeight - 1, lineColor);
        }
    }

    private void ParseAndDisplaySettings(StringBuilder sb)
    {
        // 解析Settings文本，提取关键参数
        string[] lines = featureSettings.Split('\n');
        foreach (string line in lines)
        {
            if (line.Contains("planeDistance:"))
            {
                sb.Append($"PlaneDistance: {ExtractValue(line)} | ");
            }
            else if (line.Contains("intensity:"))
            {
                sb.Append($"Intensity: {ExtractValue(line)} | ");
            }
            else if (line.Contains("fresnelPower:"))
            {
                sb.Append($"Fresnel: {ExtractValue(line)} | ");
            }
        }
    }

    private string ExtractValue(string line)
    {
        int colonIndex = line.IndexOf(':');
        if (colonIndex >= 0 && colonIndex < line.Length - 1)
        {
            return line.Substring(colonIndex + 1).Trim();
        }
        return "";
    }

    private void SaveTexture(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    private void GenerateReport(string path, string timestamp)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{effectName} - Complete Debug Report");
        sb.AppendLine("=" + new string('=', 78));
        sb.AppendLine($"Timestamp: {timestamp}");
        sb.AppendLine($"Time: {System.DateTime.Now}");
        sb.AppendLine();

        sb.AppendLine("=== System ===");
        sb.AppendLine($"Unity: {Application.unityVersion}");
        sb.AppendLine($"Platform: {Application.platform}");
        sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
        sb.AppendLine($"API: {SystemInfo.graphicsDeviceType}");
        sb.AppendLine($"VRAM: {SystemInfo.graphicsMemorySize} MB");
        sb.AppendLine();

        sb.AppendLine("=== Screen ===");
        sb.AppendLine($"Resolution: {Screen.width}x{Screen.height}");
        sb.AppendLine($"DPI: {Screen.dpi}");
        sb.AppendLine();

        if (Camera.main != null)
        {
            Camera cam = Camera.main;
            sb.AppendLine("=== Camera ===");
            sb.AppendLine($"Position: {cam.transform.position}");
            sb.AppendLine($"Rotation: {cam.transform.eulerAngles}");
            sb.AppendLine($"FOV: {cam.fieldOfView}");
            sb.AppendLine($"Near: {cam.nearClipPlane} | Far: {cam.farClipPlane}");
            sb.AppendLine();
        }

        var stats = ConsoleLogger.GetStats();
        sb.AppendLine("=== Console Stats ===");
        sb.AppendLine($"Errors: {stats.errors}");
        sb.AppendLine($"Warnings: {stats.warnings}");
        sb.AppendLine($"Logs: {stats.logs}");
        sb.AppendLine();

        sb.AppendLine("=== Performance ===");
        sb.AppendLine($"FPS: {(int)(1.0f / Time.deltaTime)}");
        sb.AppendLine($"Frame Time: {Time.deltaTime * 1000:F2} ms");
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

        // 整合Console日志到报告中
        sb.AppendLine("=== Console Logs (Last 100) ===");
        var logs = ConsoleLogger.GetLogs(100);
        if (logs != null && logs.Count > 0)
        {
            foreach (var log in logs)
            {
                sb.AppendLine($"[{log.timestamp}] [{log.type}]");
                sb.AppendLine(log.message);
                sb.AppendLine("-" + new string('-', 77));
            }
        }
        else
        {
            sb.AppendLine("(No logs captured)");
        }
        sb.AppendLine();

        File.WriteAllText(path, sb.ToString());
    }
}
