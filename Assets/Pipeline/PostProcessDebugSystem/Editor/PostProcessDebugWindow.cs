using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// 统一的后处理Debug窗口
/// 自动扫描所有Render Feature，一键Debug
/// </summary>
public class PostProcessDebugWindow : EditorWindow
{
    private Vector2 scrollPos;
    private List<FeatureInfo> features = new List<FeatureInfo>();
    private bool autoRefresh = true;
    private double lastRefreshTime = 0;

    private class FeatureInfo
    {
        public ScriptableRendererFeature feature;
        public string name;
        public string assetPath;
        public bool hasDebugVisualization;
    }

    [MenuItem("Window/Post Process Debug Center")]
    public static void ShowWindow()
    {
        var window = GetWindow<PostProcessDebugWindow>("PP Debug Center");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshFeatures();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawToolbar();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawFeatureList();
        EditorGUILayout.EndScrollView();

        DrawFooter();

        // 自动刷新
        if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > 2.0)
        {
            RefreshFeatures();
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("🛠️ Post Process Debug Center", titleStyle);
        EditorGUILayout.Space(5);

        GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10
        };
        EditorGUILayout.LabelField("统一管理所有后处理效果的Debug", subtitleStyle);
        EditorGUILayout.Space(10);
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("🔄 刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshFeatures();
        }

        autoRefresh = GUILayout.Toggle(autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("📂 打开文件夹", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            OpenOutputFolder();
        }

        if (GUILayout.Button("🗑️ 清理旧文件", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            ClearOldFiles();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFeatureList()
    {
        if (features.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "未找到任何后处理效果。\n\n" +
                "请确保：\n" +
                "1. 项目使用URP渲染管线\n" +
                "2. 在Renderer Data中添加了Render Features\n" +
                "3. 点击\"🔄 刷新\"按钮\n\n" +
                "位置：Project → Settings → Graphics → URP Asset → Renderer Data",
                MessageType.Info);

            // 显示调试信息
            EditorGUILayout.Space(5);
            if (GUILayout.Button("🔍 显示详细扫描信息"))
            {
                ShowScanDetails();
            }

            return;
        }

        EditorGUILayout.LabelField($"找到 {features.Count} 个后处理效果：", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        foreach (var info in features)
        {
            DrawFeatureItem(info);
        }
    }

    private void DrawFeatureItem(FeatureInfo info)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 标题行
        EditorGUILayout.BeginHorizontal();

        // 图标 + 名称
        GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"📦 {info.name}", nameStyle);

        GUILayout.FlexibleSpace();

        // Debug可视化标签
        if (info.hasDebugVisualization)
        {
            GUIStyle badgeStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { background = MakeTex(2, 2, new Color(0.2f, 0.8f, 0.4f, 0.3f)) },
                padding = new RectOffset(5, 5, 2, 2),
                fontSize = 9
            };
            GUILayout.Label("✓ Debug可视化", badgeStyle);
        }

        EditorGUILayout.EndHorizontal();

        // 资产路径
        EditorGUILayout.LabelField("资产:", info.assetPath, EditorStyles.miniLabel);

        EditorGUILayout.Space(3);

        // 按钮行
        EditorGUILayout.BeginHorizontal();

        // 主捕获按钮（Play模式）
        GUI.backgroundColor = Application.isPlaying ? new Color(0.3f, 0.8f, 0.5f) : new Color(0.6f, 0.6f, 0.6f);

        if (GUILayout.Button(Application.isPlaying ? "📷 Capture (Play)" : "📷 Capture (Editor)", GUILayout.Height(30)))
        {
            CaptureFeature(info);
        }

        GUI.backgroundColor = Color.white;

        // 选择按钮
        if (GUILayout.Button("→ 选择", GUILayout.Width(60), GUILayout.Height(30)))
        {
            Selection.activeObject = info.feature;
            EditorGUIUtility.PingObject(info.feature);
        }

        EditorGUILayout.EndHorizontal();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Editor模式：捕获SceneView（后处理效果可能不完整）\nPlay模式：捕获GameView（完整效果）", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawFooter()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"📊 状态: {(Application.isPlaying ? "运行中 ✓" : "已停止")}");

        GUILayout.FlexibleSpace();

        // 根据模式选择对应的Logger
        var stats = Application.isPlaying ? ConsoleLogger.GetStats() : EditorConsoleLogger.GetStats();
        string statsText = $"Console: {stats.errors} 错误 | {stats.warnings} 警告 | {stats.logs} 日志";
        EditorGUILayout.LabelField(statsText);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "💡 使用提示：\n" +
            "1. 运行游戏（Play Mode）\n" +
            "2. 点击任意效果的 [📷 Capture Debug Data]\n" +
            "3. 对Claude说：\"请分析Debug数据\"\n" +
            "4. Claude会自动读取并分析所有文件",
            MessageType.Info);
    }

    private void RefreshFeatures()
    {
        features.Clear();

        // 查找所有UniversalRendererData
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);

            if (rendererData != null)
            {
                // 使用SerializedObject访问内部字段（更可靠）
                SerializedObject serializedObject = new SerializedObject(rendererData);
                SerializedProperty rendererFeaturesProperty = serializedObject.FindProperty("m_RendererFeatures");

                if (rendererFeaturesProperty != null && rendererFeaturesProperty.isArray)
                {
                    for (int i = 0; i < rendererFeaturesProperty.arraySize; i++)
                    {
                        SerializedProperty featureProperty = rendererFeaturesProperty.GetArrayElementAtIndex(i);
                        ScriptableRendererFeature feature = featureProperty.objectReferenceValue as ScriptableRendererFeature;

                        if (feature != null)
                        {
                            features.Add(new FeatureInfo
                            {
                                feature = feature,
                                name = feature.name,
                                assetPath = path,
                                hasDebugVisualization = HasDebugVisualization(feature)
                            });
                        }
                    }
                }
            }
        }

        Repaint();
    }

    private bool HasDebugVisualization(ScriptableRendererFeature feature)
    {
        // 检查是否有enableDebugVisualization字段
        var type = feature.GetType();
        var settingsField = type.GetField("settings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (settingsField != null)
        {
            var settings = settingsField.GetValue(feature);
            if (settings != null)
            {
                var debugField = settings.GetType().GetField("enableDebugVisualization",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                return debugField != null;
            }
        }

        return false;
    }

    private void CaptureFeature(FeatureInfo info)
    {
        // 提取Feature配置信息
        string settingsInfo = SerializeFeatureSettings(info.feature);

        if (Application.isPlaying)
        {
            // Play模式：运行时捕获（完整效果）
            EnableDebugVisualization(info.feature, true);

            // 尝试查找StepDebug shader（用于中间步骤可视化）
            Material debugMaterial = CreateStepDebugMaterial(info.name, info.feature);
            int[] debugSteps = GetDebugSteps(info.feature);

            DebugDataCapture.Capture(info.name, settingsInfo, debugMaterial, debugSteps, () =>
            {
                EnableDebugVisualization(info.feature, false);

                if (debugMaterial != null)
                    UnityEngine.Object.DestroyImmediate(debugMaterial);

                EditorUtility.DisplayDialog("捕获完成",
                    $"{info.name} Debug数据已保存！\n\n" +
                    $"现在对Claude说：\n\"请分析Debug数据\"",
                    "好的");
            });
        }
        else
        {
            // Editor模式：SceneView捕获（效果可能不完整）
            if (!EditorUtility.DisplayDialog("Editor模式捕获",
                $"即将捕获 {info.name} 的SceneView截图。\n\n" +
                "⚠️ 注意：Editor模式下后处理效果可能不完整。\n" +
                "建议在Play Mode下捕获以获得完整效果。\n\n" +
                "是否继续？",
                "继续", "取消"))
            {
                return;
            }

            EnableDebugVisualization(info.feature, true);

            EditorDebugCapture.Capture(info.name, settingsInfo, () =>
            {
                EnableDebugVisualization(info.feature, false);

                EditorUtility.DisplayDialog("捕获完成",
                    $"{info.name} Debug数据已保存！\n\n" +
                    $"现在对Claude说：\n\"请分析Debug数据\"",
                    "好的");
            });
        }
    }

    private void EnableDebugVisualization(ScriptableRendererFeature feature, bool enable)
    {
        var type = feature.GetType();
        var settingsField = type.GetField("settings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (settingsField != null)
        {
            var settings = settingsField.GetValue(feature);
            if (settings != null)
            {
                var debugField = settings.GetType().GetField("enableDebugVisualization",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (debugField != null)
                {
                    debugField.SetValue(settings, enable);
                }
            }
        }
    }

    private void OpenOutputFolder()
    {
        string dir = Path.Combine(Application.dataPath, "DebugCaptures");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        EditorUtility.RevealInFinder(dir);
    }

    private void ClearOldFiles()
    {
        string dir = Path.Combine(Application.dataPath, "DebugCaptures");
        if (!Directory.Exists(dir))
            return;

        var files = Directory.GetFiles(dir).Where(f => !f.EndsWith(".meta")).ToArray();

        if (files.Length == 0)
        {
            EditorUtility.DisplayDialog("没有文件", "DebugCaptures文件夹是空的。", "确定");
            return;
        }

        if (EditorUtility.DisplayDialog("清理确认",
            $"确定要删除 {files.Length} 个Debug文件吗？\n\n此操作无法撤销！",
            "删除", "取消"))
        {
            foreach (var file in files)
                File.Delete(file);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", $"已删除 {files.Length} 个文件", "确定");
        }
    }

    private void ShowScanDetails()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Renderer Data 扫描详情 ===\n");

        // 查找所有UniversalRendererData
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        sb.AppendLine($"找到 {guids.Length} 个 UniversalRendererData 资产：\n");

        if (guids.Length == 0)
        {
            sb.AppendLine("⚠️ 没有找到任何 UniversalRendererData！");
            sb.AppendLine("\n这意味着：");
            sb.AppendLine("• 项目可能没有正确配置URP");
            sb.AppendLine("• 或者Renderer Asset还没有创建");
            sb.AppendLine("\n解决方法：");
            sb.AppendLine("1. 检查 Edit → Project Settings → Graphics");
            sb.AppendLine("2. 确保 Scriptable Render Pipeline Settings 设置了URP Asset");
            sb.AppendLine("3. 创建/配置 Forward Renderer 或 Universal Renderer");
        }
        else
        {
            int totalFeatures = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);

                sb.AppendLine($"📁 {path}");

                if (rendererData != null)
                {
                    // 使用SerializedObject访问内部字段
                    SerializedObject serializedObject = new SerializedObject(rendererData);
                    SerializedProperty rendererFeaturesProperty = serializedObject.FindProperty("m_RendererFeatures");

                    if (rendererFeaturesProperty != null && rendererFeaturesProperty.isArray)
                    {
                        int featureCount = rendererFeaturesProperty.arraySize;

                        if (featureCount > 0)
                        {
                            sb.AppendLine($"   ✓ 包含 {featureCount} 个 Render Features：");

                            for (int i = 0; i < featureCount; i++)
                            {
                                SerializedProperty featureProperty = rendererFeaturesProperty.GetArrayElementAtIndex(i);
                                ScriptableRendererFeature feature = featureProperty.objectReferenceValue as ScriptableRendererFeature;

                                if (feature != null)
                                {
                                    sb.AppendLine($"      • {feature.name} ({feature.GetType().Name})");
                                    totalFeatures++;
                                }
                                else
                                {
                                    sb.AppendLine("      • [null] (可能是缺失的引用)");
                                }
                            }
                        }
                        else
                        {
                            sb.AppendLine("   ⚠️ 没有Render Features（列表为空）");
                        }
                    }
                    else
                    {
                        sb.AppendLine("   ❌ 无法找到m_RendererFeatures字段");
                    }
                }
                else
                {
                    sb.AppendLine("   ❌ 无法加载资产");
                }

                sb.AppendLine();
            }

            sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"总计：{totalFeatures} 个有效的 Render Features");

            if (totalFeatures == 0)
            {
                sb.AppendLine("\n⚠️ 找到了Renderer Data但没有Render Features！");
                sb.AppendLine("\n解决方法：");
                sb.AppendLine("1. 在Project窗口中找到上述的Renderer Data资产");
                sb.AppendLine("2. 点击选中它");
                sb.AppendLine("3. 在Inspector中点击 \"Add Renderer Feature\"");
                sb.AppendLine("4. 添加你的后处理效果（如SSPR）");
            }
        }

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("扫描详情",
            sb.ToString() + "\n\n完整信息已输出到Console窗口。",
            "确定");
    }

    private int[] GetDebugSteps(ScriptableRendererFeature feature)
    {
        var type = feature.GetType();
        var settingsField = type.GetField("settings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (settingsField == null)
            return new int[] { 1, 2, 3, 4 }; // 默认

        var settings = settingsField.GetValue(feature);
        if (settings == null)
            return new int[] { 1, 2, 3, 4 };

        // 尝试读取debugStep1-4字段
        var step1Field = settings.GetType().GetField("debugStep1");
        var step2Field = settings.GetType().GetField("debugStep2");
        var step3Field = settings.GetType().GetField("debugStep3");
        var step4Field = settings.GetType().GetField("debugStep4");

        int[] steps = new int[4];
        steps[0] = step1Field != null ? (int)step1Field.GetValue(settings) : 1;
        steps[1] = step2Field != null ? (int)step2Field.GetValue(settings) : 2;
        steps[2] = step3Field != null ? (int)step3Field.GetValue(settings) : 3;
        steps[3] = step4Field != null ? (int)step4Field.GetValue(settings) : 4;

        return steps;
    }

    private Material CreateStepDebugMaterial(string featureName, ScriptableRendererFeature feature)
    {
        // 尝试查找对应的StepDebug shader
        // 如: "SSPRRenderFeature" -> "Hidden/SSPR_StepDebug"
        // "SSAORenderFeature" -> "Hidden/SSAO_StepDebug"

        string baseName = featureName.Replace("RenderFeature", "");
        string shaderName = $"Hidden/{baseName}_StepDebug";

        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.Log($"[DebugWindow] 未找到StepDebug shader: {shaderName}，跳过中间步骤可视化");
            return null;
        }

        Material mat = new Material(shader);

        // 设置Camera参数（从Main Camera）
        Camera cam = Camera.main;
        if (cam != null)
        {
            SetupCameraParams(mat, cam);
        }

        // 从Feature拷贝Settings参数到材质
        CopyFeatureSettingsToMaterial(feature, mat);

        Debug.Log($"[DebugWindow] 创建StepDebug材质: {shaderName}");
        return mat;
    }

    private void CopyFeatureSettingsToMaterial(ScriptableRendererFeature feature, Material mat)
    {
        var type = feature.GetType();
        var settingsField = type.GetField("settings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (settingsField == null)
            return;

        var settings = settingsField.GetValue(feature);
        if (settings == null)
            return;

        // 根据Feature类型设置不同的参数
        if (type.Name == "SSPRRenderFeature")
        {
            // SSPR参数
            var intensity = settings.GetType().GetField("intensity")?.GetValue(settings);
            var fresnelPower = settings.GetType().GetField("fresnelPower")?.GetValue(settings);
            var fadeStart = settings.GetType().GetField("fadeStart")?.GetValue(settings);
            var fadeEnd = settings.GetType().GetField("fadeEnd")?.GetValue(settings);
            var planeNormal = settings.GetType().GetField("planeNormal")?.GetValue(settings);
            var planeDistance = settings.GetType().GetField("planeDistance")?.GetValue(settings);

            if (intensity != null && fresnelPower != null && fadeStart != null && fadeEnd != null)
            {
                mat.SetVector("_SSPRParams", new Vector4(
                    (float)intensity,
                    (float)fresnelPower,
                    (float)fadeStart,
                    (float)fadeEnd
                ));
            }

            if (planeNormal != null && planeDistance != null)
            {
                Vector3 normal = (Vector3)planeNormal;
                Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, (float)planeDistance);
                mat.SetVector("_ReflectionPlane", reflectionPlane);
                Debug.Log($"[DebugWindow] 设置_ReflectionPlane = {reflectionPlane}");
            }
            else
            {
                Debug.LogWarning($"[DebugWindow] 无法读取planeNormal或planeDistance: normal={planeNormal}, dist={planeDistance}");
            }
        }
        else if (type.Name == "SSAORenderFeature")
        {
            // SSAO参数（如果将来需要SSAO的StepDebug）
            var intensity = settings.GetType().GetField("intensity")?.GetValue(settings);
            var radius = settings.GetType().GetField("radius")?.GetValue(settings);
            var beta = settings.GetType().GetField("beta")?.GetValue(settings);
            var sampleCount = settings.GetType().GetField("sampleCount")?.GetValue(settings);

            if (intensity != null && radius != null && beta != null && sampleCount != null)
            {
                mat.SetVector("_SSAOParams", new Vector4(
                    (float)intensity,
                    (float)radius,
                    (float)beta,
                    (int)sampleCount
                ));
            }
        }
        else if (type.Name == "SSRRenderFeature")
        {
            var intensity = settings.GetType().GetField("intensity")?.GetValue(settings);
            var maxSteps = settings.GetType().GetField("maxSteps")?.GetValue(settings);
            var stride = settings.GetType().GetField("stride")?.GetValue(settings);
            var thickness = settings.GetType().GetField("thickness")?.GetValue(settings);
            var maxDistance = settings.GetType().GetField("maxDistance")?.GetValue(settings);
            var rayStartBias = settings.GetType().GetField("rayStartBias")?.GetValue(settings);
            var fresnelPower = settings.GetType().GetField("fresnelPower")?.GetValue(settings);
            var fadeStart = settings.GetType().GetField("fadeStart")?.GetValue(settings);
            var fadeEnd = settings.GetType().GetField("fadeEnd")?.GetValue(settings);
            var receiverPlaneNormal = settings.GetType().GetField("receiverPlaneNormal")?.GetValue(settings);
            var receiverPlaneDistance = settings.GetType().GetField("receiverPlaneDistance")?.GetValue(settings);
            var receiverNormalThreshold = settings.GetType().GetField("receiverNormalThreshold")?.GetValue(settings);
            var receiverMaxDistance = settings.GetType().GetField("receiverMaxDistance")?.GetValue(settings);
            var useReceiverPlaneMask = settings.GetType().GetField("useReceiverPlaneMask")?.GetValue(settings);

            if (intensity != null && maxSteps != null && stride != null && thickness != null)
            {
                mat.SetVector("_SSRParams", new Vector4(
                    (float)intensity,
                    (int)maxSteps,
                    (float)stride,
                    (float)thickness
                ));
            }

            if (maxDistance != null && rayStartBias != null && fresnelPower != null)
            {
                mat.SetVector("_SSRParams2", new Vector4(
                    (float)maxDistance,
                    (float)rayStartBias,
                    (float)fresnelPower,
                    0.0f
                ));
            }

            if (fadeStart != null && fadeEnd != null)
            {
                mat.SetVector("_SSRParams3", new Vector4(
                    (float)fadeStart,
                    (float)fadeEnd,
                    0.0f,
                    0.0f
                ));
            }

            if (receiverPlaneNormal != null && receiverPlaneDistance != null)
            {
                Vector3 normal = ((Vector3)receiverPlaneNormal).normalized;
                mat.SetVector("_SSRReceiverPlane", new Vector4(
                    normal.x, normal.y, normal.z, (float)receiverPlaneDistance));
            }

            if (receiverNormalThreshold != null && receiverMaxDistance != null && useReceiverPlaneMask != null)
            {
                mat.SetVector("_SSRReceiverParams", new Vector4(
                    (float)receiverNormalThreshold,
                    (float)receiverMaxDistance,
                    (bool)useReceiverPlaneMask ? 1.0f : 0.0f,
                    0.0f
                ));
            }
        }
    }

    private void SetupCameraParams(Material mat, Camera camera)
    {
        Matrix4x4 view = camera.worldToCameraMatrix;
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vp = proj * view;
        Matrix4x4 invVp = vp.inverse;

        // ⚠️ 关键：手动设置VP矩阵（Graphics.Blit不会自动传递）
        // 注意：不能用builtin名称，使用自定义名称
        mat.SetMatrix("_CustomMatrixVP", vp);
        mat.SetMatrix("_SSRView", view);
        mat.SetMatrix("_SSRProj", proj);
        mat.SetMatrix("_SSRViewProj", vp);
        mat.SetMatrix("_SSRInvViewProj", invVp);

        // 计算cview（移除平移）
        Matrix4x4 cview = view;
        cview.SetColumn(3, new Vector4(0, 0, 0, 1));

        // 计算View空间的frustum corners
        float tanHalfFOV = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float aspect = camera.aspect;

        Vector3 topLeftView = new Vector3(-tanHalfFOV * aspect, tanHalfFOV, -1);
        Vector3 topRightView = new Vector3(tanHalfFOV * aspect, tanHalfFOV, -1);
        Vector3 bottomLeftView = new Vector3(-tanHalfFOV * aspect, -tanHalfFOV, -1);

        // 转换到世界空间
        Matrix4x4 cviewInv = cview.inverse;
        Vector3 topLeft = cviewInv.MultiplyPoint3x4(topLeftView);
        Vector3 topRight = cviewInv.MultiplyPoint3x4(topRightView);
        Vector3 bottomLeft = cviewInv.MultiplyPoint3x4(bottomLeftView);

        Vector4 xExtent = topRight - topLeft;
        Vector4 yExtent = bottomLeft - topLeft;

        mat.SetVector("_CameraViewTopLeftCorner", topLeft);
        mat.SetVector("_CameraViewXExtent", xExtent);
        mat.SetVector("_CameraViewYExtent", yExtent);
        mat.SetVector("_ProjectionParams2", new Vector4(1.0f / camera.nearClipPlane, 0, 0, 0));
    }

    private string SerializeFeatureSettings(ScriptableRendererFeature feature)
    {
        var type = feature.GetType();
        var settingsField = type.GetField("settings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (settingsField == null)
            return "无法获取Settings";

        var settings = settingsField.GetValue(feature);
        if (settings == null)
            return "Settings为空";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        var settingsType = settings.GetType();
        var fields = settingsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var field in fields)
        {
            // 跳过Unity特殊类型（避免输出过多信息）
            if (field.FieldType == typeof(RenderPassEvent))
            {
                var value = field.GetValue(settings);
                sb.AppendLine($"{field.Name}: {value}");
            }
            else if (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType.IsEnum)
            {
                var value = field.GetValue(settings);
                sb.AppendLine($"{field.Name}: {value}");
            }
            else if (field.FieldType == typeof(Vector3))
            {
                var value = (Vector3)field.GetValue(settings);
                sb.AppendLine($"{field.Name}: ({value.x:F3}, {value.y:F3}, {value.z:F3})");
            }
            else if (field.FieldType == typeof(Vector4))
            {
                var value = (Vector4)field.GetValue(settings);
                sb.AppendLine($"{field.Name}: ({value.x:F3}, {value.y:F3}, {value.z:F3}, {value.w:F3})");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
