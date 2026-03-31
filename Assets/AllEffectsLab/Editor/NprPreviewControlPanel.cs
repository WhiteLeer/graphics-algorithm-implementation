#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class NprPreviewControlPanel : EditorWindow
{
    private const string CharacterScenePath = "Assets/Scenes/Character-AnimeRender-Test.unity";

    [MenuItem("Tools/All Effects/NPR Preview Control Panel")]
    public static void ShowWindow()
    {
        GetWindow<NprPreviewControlPanel>("NPR Preview");
    }

    private void OnGUI()
    {
        GUILayout.Label("NPR 预览快捷面板", EditorStyles.boldLabel);

        if (GUILayout.Button("打开角色测试场景"))
            OpenCharacterScene();

        if (GUILayout.Button("设置预览辅助对象"))
            DynamicFxLabBuilder.SetupCharacterPreviewHelpersInScene();

        EditorGUILayout.Space(6);
        GUILayout.Label("风格切换", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("应用 NPR-3 材质"))
            DynamicFxLabBuilder.ApplyNpr3MaterialsToCharacterInScene();
        if (GUILayout.Button("NPR-4 清爽风格"))
            Npr4StylePresets.ApplyCleanAnimeStyle();
        if (GUILayout.Button("NPR-4 高饱和风格"))
            Npr4StylePresets.ApplyVibrantAnimeStyle();
        if (GUILayout.Button("NPR-5 头发高光"))
            Npr4StylePresets.ApplyHairSpecularBoost();
        if (GUILayout.Button("NPR-6 面部光照"))
            Npr4StylePresets.ApplyFaceLightingTuning();
        if (GUILayout.Button("NPR-7 阴影风格化"))
            Npr4StylePresets.ApplyStylizedShadow();
        if (GUILayout.Button("NPR-8 自适应轮廓"))
            Npr4StylePresets.ApplyAdaptiveOutline();
        if (GUILayout.Button("NPR-9 分区材质参数"))
            Npr4StylePresets.ApplyMaterialRegionProfiles();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("NPR-11 眼脸强化"))
            NprAdvancedWorkflowTools.ApplyEyeFaceAccent();
        if (GUILayout.Button("NPR-12 头发分层"))
            NprAdvancedWorkflowTools.ApplyLayeredHairHighlights();
        if (GUILayout.Button("NPR-13 场景融合"))
            NprAdvancedWorkflowTools.ApplyCharacterSceneFusion();
        if (GUILayout.Button("NPR-14 日景灯光"))
            NprAdvancedWorkflowTools.ApplyLightingDaylight();
        if (GUILayout.Button("NPR-14 黄昏灯光"))
            NprAdvancedWorkflowTools.ApplyLightingEvening();
        if (GUILayout.Button("NPR-14 舞台灯光"))
            NprAdvancedWorkflowTools.ApplyLightingStage();
        if (GUILayout.Button("NPR-15 贴图优化"))
            NprAdvancedWorkflowTools.OptimizeCharacterTextureImports();
        if (GUILayout.Button("NPR-16 12角截图"))
            NprAdvancedWorkflowTools.CaptureTurntable12Shots();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        GUILayout.Label("相机与灯光", EditorStyles.boldLabel);
        DrawCameraSection();
        DrawLightSection();

        EditorGUILayout.Space(6);
        GUILayout.Label("预览与输出", EditorStyles.boldLabel);
        if (GUILayout.Button("抓图（Game + Scene）"))
            DynamicFxLabBuilder.CaptureCharacterAnimeTestShots();

        if (GUILayout.Button("聚焦 Scene 到角色"))
            FocusSceneViewOnCharacter();

        if (GUILayout.Button("保存当前场景"))
            SaveScene();
    }

    private static void DrawCameraSection()
    {
        var orbit = FindObjectOfType<FxLabCameraOrbit>();
        if (orbit == null)
        {
            EditorGUILayout.HelpBox("未找到 FxLabCameraOrbit（Main Camera）", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        bool autoOrbit = EditorGUILayout.Toggle("自动环绕", orbit.autoOrbit);
        float distance = EditorGUILayout.Slider("相机距离", orbit.distance, 2.8f, 8.5f);
        float speed = EditorGUILayout.Slider("环绕速度", orbit.autoOrbitSpeed, 0.0f, 60.0f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(orbit, "Adjust NPR Camera");
            orbit.autoOrbit = autoOrbit;
            orbit.distance = distance;
            orbit.autoOrbitSpeed = speed;
            EditorUtility.SetDirty(orbit);
            MarkDirtyScene(orbit.gameObject.scene);
        }
    }

    private static void DrawLightSection()
    {
        Light directional = FindLight("Directional Light");
        Light fill = FindLight("Character_FillPointLight");
        Light rim = FindLight("Character_RimPointLight");

        if (directional == null || fill == null || rim == null)
        {
            EditorGUILayout.HelpBox("未找到角色灯光（Directional/Fill/Rim）", MessageType.Info);
            return;
        }

        EditorGUI.BeginChangeCheck();
        Vector3 dirEuler = directional.transform.rotation.eulerAngles;
        float dirPitch = EditorGUILayout.Slider("主光俯仰", dirEuler.x, 10.0f, 80.0f);
        float dirYaw = EditorGUILayout.Slider("主光方位", dirEuler.y, -180.0f, 180.0f);
        float dirIntensity = EditorGUILayout.Slider("主光强度", directional.intensity, 0.1f, 2.0f);
        float dirShadow = EditorGUILayout.Slider("主光阴影", directional.shadowStrength, 0.0f, 1.0f);
        float fillIntensity = EditorGUILayout.Slider("补光强度", fill.intensity, 0.0f, 2.0f);
        float rimIntensity = EditorGUILayout.Slider("轮廓光强度", rim.intensity, 0.0f, 2.0f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObjects(new Object[] { directional.transform, directional, fill, rim }, "Adjust NPR Lights");
            directional.transform.rotation = Quaternion.Euler(dirPitch, dirYaw, 0.0f);
            directional.intensity = dirIntensity;
            directional.shadowStrength = dirShadow;
            fill.intensity = fillIntensity;
            rim.intensity = rimIntensity;

            EditorUtility.SetDirty(directional.transform);
            EditorUtility.SetDirty(directional);
            EditorUtility.SetDirty(fill);
            EditorUtility.SetDirty(rim);
            MarkDirtyScene(directional.gameObject.scene);
        }
    }

    private static Light FindLight(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Light>() : null;
    }

    private static void OpenCharacterScene()
    {
        if (!System.IO.File.Exists(CharacterScenePath))
        {
            DynamicFxLabBuilder.BuildCharacterRenderTestScene();
            return;
        }

        EditorSceneManager.OpenScene(CharacterScenePath, OpenSceneMode.Single);
    }

    private static void FocusSceneViewOnCharacter()
    {
        GameObject target = GameObject.Find("Player_Girl_Test");
        if (target == null)
            target = GameObject.Find("Player_Girl");

        if (target == null)
            return;

        Selection.activeObject = target;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private static void SaveScene()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.SaveScene(scene);
    }

    private static void MarkDirtyScene(UnityEngine.SceneManagement.Scene scene)
    {
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);
    }
}
#endif
