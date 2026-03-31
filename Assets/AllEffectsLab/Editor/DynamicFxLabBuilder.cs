#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System;

public static class DynamicFxLabBuilder
{
    private const string ScenePath = "Assets/Scenes/AllEffects-DynamicLab.unity";
    private const string CharacterScenePath = "Assets/Scenes/Character-AnimeRender-Test.unity";
    private const string MaterialRoot = "Assets/MaterialFX/Common_LitLibrary";
    private const string CharacterPrefabPath = "Assets/_Prefab/Player_Girl.prefab";
    private const string CharacterShotDir = "Assets/Screenshots/CharacterTest";
    private const string Npr0Root = "Assets/MaterialFX/NPR-0_BaseLighting";
    private const string Npr0MaterialDir = Npr0Root + "/Materials";
    private const string Npr0ConvertedDir = Npr0MaterialDir + "/CharacterConverted";
    private const string Npr0TemplatePath = Npr0MaterialDir + "/M_NPR0_ToonTemplate.mat";
    private const string Npr2Root = "Assets/MaterialFX/NPR-2_RampOutline";
    private const string Npr2MaterialDir = Npr2Root + "/Materials";
    private const string Npr2ConvertedDir = Npr2MaterialDir + "/CharacterConverted";
    private const string Npr2TemplatePath = Npr2MaterialDir + "/M_NPR2_RampOutlineTemplate.mat";
    private const string Npr2DefaultRampPath = Npr2Root + "/Textures/T_Ramp_Default.png";
    private const string Npr3Root = "Assets/MaterialFX/NPR-3_CharacterAdvanced";
    private const string Npr3MaterialDir = Npr3Root + "/Materials";
    private const string Npr3ConvertedDir = Npr3MaterialDir + "/CharacterConverted";
    private const string Npr3TemplatePath = Npr3MaterialDir + "/M_NPR3_CharacterAdvancedTemplate.mat";

    [MenuItem("Tools/All Effects/Build Dynamic Test Scene")]
    public static void BuildScene()
    {
        EnsureFolder("Assets/MaterialFX");
        EnsureFolder(MaterialRoot);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material floorMat = GetOrCreateLitMaterial(
            MaterialRoot + "/M_FloorRough.mat",
            new Color(0.30f, 0.30f, 0.32f, 1.0f),
            metallic: 0.05f,
            smoothness: 0.35f,
            emission: Color.black);

        Material mirrorMat = GetOrCreateLitMaterial(
            MaterialRoot + "/M_MirrorPlate.mat",
            new Color(0.14f, 0.16f, 0.19f, 1.0f),
            metallic: 1.0f,
            smoothness: 0.97f,
            emission: Color.black);

        Material objectMat = GetOrCreateLitMaterial(
            MaterialRoot + "/M_Object.mat",
            new Color(0.68f, 0.72f, 0.80f, 1.0f),
            metallic: 0.15f,
            smoothness: 0.45f,
            emission: Color.black);

        Material brightMat = GetOrCreateLitMaterial(
            MaterialRoot + "/M_Emissive.mat",
            new Color(0.08f, 0.08f, 0.08f, 1.0f),
            metallic: 0.0f,
            smoothness: 0.6f,
            emission: new Color(8.0f, 4.0f, 1.5f, 1.0f));

        Material accentMat = GetOrCreateLitMaterial(
            MaterialRoot + "/M_Accent.mat",
            new Color(0.20f, 0.45f, 0.75f, 1.0f),
            metallic: 0.4f,
            smoothness: 0.85f,
            emission: Color.black);

        GameObject root = new GameObject("FXLab_Root");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor_Main";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localScale = new Vector3(4.5f, 1.0f, 4.5f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        GameObject mirrorPlate = GameObject.CreatePrimitive(PrimitiveType.Plane);
        mirrorPlate.name = "Floor_ReflectivePlate";
        mirrorPlate.transform.SetParent(root.transform, false);
        mirrorPlate.transform.position = new Vector3(0.0f, 0.01f, 0.0f);
        mirrorPlate.transform.localScale = new Vector3(1.35f, 1.0f, 1.35f);
        mirrorPlate.GetComponent<Renderer>().sharedMaterial = mirrorMat;

        GameObject occluderWallA = GameObject.CreatePrimitive(PrimitiveType.Cube);
        occluderWallA.name = "AO_Wall_A";
        occluderWallA.transform.SetParent(root.transform, false);
        occluderWallA.transform.position = new Vector3(-4.8f, 1.25f, 0.0f);
        occluderWallA.transform.localScale = new Vector3(0.4f, 2.5f, 7.0f);
        occluderWallA.GetComponent<Renderer>().sharedMaterial = objectMat;

        GameObject occluderWallB = GameObject.CreatePrimitive(PrimitiveType.Cube);
        occluderWallB.name = "AO_Wall_B";
        occluderWallB.transform.SetParent(root.transform, false);
        occluderWallB.transform.position = new Vector3(0.0f, 1.25f, -4.8f);
        occluderWallB.transform.localScale = new Vector3(7.0f, 2.5f, 0.4f);
        occluderWallB.GetComponent<Renderer>().sharedMaterial = objectMat;

        for (int i = 0; i < 3; i++)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"AO_Pillar_{i}";
            pillar.transform.SetParent(root.transform, false);
            pillar.transform.position = new Vector3(-2.6f + i * 2.6f, 0.9f, 2.6f);
            pillar.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
            pillar.GetComponent<Renderer>().sharedMaterial = objectMat;
        }

        GameObject nearObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nearObject.name = "DoF_NearObject";
        nearObject.transform.SetParent(root.transform, false);
        nearObject.transform.position = new Vector3(-1.2f, 0.55f, 2.0f);
        nearObject.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
        nearObject.GetComponent<Renderer>().sharedMaterial = accentMat;
        var nearSpin = nearObject.AddComponent<FxLabSpin>();
        nearSpin.axis = new Vector3(0.3f, 1.0f, 0.0f);
        nearSpin.speed = 22.0f;

        GameObject midObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        midObject.name = "DoF_MidObject";
        midObject.transform.SetParent(root.transform, false);
        midObject.transform.position = new Vector3(0.0f, 0.65f, -0.2f);
        midObject.transform.localScale = Vector3.one * 1.3f;
        midObject.GetComponent<Renderer>().sharedMaterial = objectMat;
        var midMotion = midObject.AddComponent<FxLabOrbitMotion>();
        midMotion.center = new Vector3(0.0f, 0.65f, 0.0f);
        midMotion.axis = Vector3.up;
        midMotion.radius = 1.6f;
        midMotion.angularSpeed = 30.0f;
        midMotion.bobAmplitude = 0.15f;
        midMotion.bobFrequency = 0.35f;
        midMotion.selfSpinAxis = new Vector3(0.2f, 1.0f, 0.1f);
        midMotion.selfSpinSpeed = 28.0f;

        GameObject farObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        farObject.name = "DoF_FarObject";
        farObject.transform.SetParent(root.transform, false);
        farObject.transform.position = new Vector3(2.4f, 1.0f, -3.5f);
        farObject.transform.localScale = new Vector3(1.1f, 1.35f, 1.1f);
        farObject.GetComponent<Renderer>().sharedMaterial = accentMat;
        var farSpin = farObject.AddComponent<FxLabSpin>();
        farSpin.axis = new Vector3(0.0f, 1.0f, 0.2f);
        farSpin.speed = -25.0f;

        GameObject emissive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        emissive.name = "Bloom_Emitter";
        emissive.transform.SetParent(root.transform, false);
        emissive.transform.position = new Vector3(2.2f, 1.2f, 1.8f);
        emissive.transform.localScale = Vector3.one * 0.8f;
        emissive.GetComponent<Renderer>().sharedMaterial = brightMat;
        var emissiveMotion = emissive.AddComponent<FxLabOrbitMotion>();
        emissiveMotion.center = new Vector3(1.8f, 1.2f, 1.2f);
        emissiveMotion.axis = new Vector3(0.0f, 1.0f, 0.2f);
        emissiveMotion.radius = 0.9f;
        emissiveMotion.angularSpeed = 52.0f;
        emissiveMotion.bobAmplitude = 0.25f;
        emissiveMotion.bobFrequency = 0.9f;

        GameObject reflectiveRunner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        reflectiveRunner.name = "SSR_SSPR_Runner";
        reflectiveRunner.transform.SetParent(root.transform, false);
        reflectiveRunner.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
        reflectiveRunner.GetComponent<Renderer>().sharedMaterial = brightMat;
        var runnerMotion = reflectiveRunner.AddComponent<FxLabOrbitMotion>();
        runnerMotion.center = new Vector3(0.0f, 0.45f, 0.0f);
        runnerMotion.axis = Vector3.up;
        runnerMotion.radius = 2.4f;
        runnerMotion.angularSpeed = -68.0f;

        GameObject mainCamera = CreateMainCamera(root.transform);
        GameObject dirLight = CreateDirectionalLight(root.transform);
        CreateMovingPointLight(root.transform, emissive.transform);
        CreateReflectionProbe(root.transform);

        RenderSettings.sun = dirLight.GetComponent<Light>();
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.reflectionIntensity = 1.0f;

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("Failed to save scene at: " + ScenePath);
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Selection.activeObject = mainCamera;
        EditorGUIUtility.PingObject(mainCamera);

        Debug.Log("[AllEffects] Dynamic test scene built: " + ScenePath);
        Debug.Log("[AllEffects] Controls: Right Mouse drag = orbit, Mouse Wheel = zoom.");
        Debug.Log("[AllEffects] Ensure URP renderer enables SSAO/SSPR/SSR/Bloom/DoF features.");
    }

    [MenuItem("Tools/All Effects/Build Character Anime Test Scene")]
    public static void BuildCharacterRenderTestScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/MaterialFX");
        EnsureFolder(MaterialRoot);
        EnsureFolder(Npr0Root);
        EnsureFolder(Npr0MaterialDir);
        EnsureFolder(Npr0ConvertedDir);
        EnsureFolder(Npr2Root);
        EnsureFolder(Npr2MaterialDir);
        EnsureFolder(Npr2ConvertedDir);
        EnsureFolder(Npr3Root);
        EnsureFolder(Npr3MaterialDir);
        EnsureFolder(Npr3ConvertedDir);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material floorMat = GetOrCreateLitMaterial(
            MaterialRoot + "/M_CharacterFloor.mat",
            new Color(0.45f, 0.46f, 0.50f, 1.0f),
            metallic: 0.0f,
            smoothness: 0.22f,
            emission: Color.black);

        Material wallMat = GetOrCreateLitMaterial(
            MaterialRoot + "/M_CharacterBackWall.mat",
            new Color(0.62f, 0.64f, 0.68f, 1.0f),
            metallic: 0.0f,
            smoothness: 0.08f,
            emission: Color.black);

        GameObject root = new GameObject("CharacterRenderTest_Root");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor_Neutral";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localScale = new Vector3(3.8f, 1.0f, 3.8f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "Background_Card";
        backWall.transform.SetParent(root.transform, false);
        backWall.transform.position = new Vector3(0.0f, 2.0f, 4.0f);
        backWall.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
        backWall.transform.localScale = new Vector3(5.0f, 4.0f, 0.15f);
        backWall.GetComponent<Renderer>().sharedMaterial = wallMat;

        GameObject character = null;
        GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
        if (characterPrefab != null)
        {
            character = PrefabUtility.InstantiatePrefab(characterPrefab, scene) as GameObject;
            if (character != null)
            {
                character.name = "Player_Girl_Test";
                character.transform.SetParent(root.transform, true);
                character.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                character.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                character.transform.localScale = Vector3.one;
                NormalizeCharacterHeight(character, 1.65f);
                SnapCharacterToGround(character);
                ApplyNpr0MaterialsToCharacter(character);
            }
        }
        else
        {
            Debug.LogError("[AllEffects] Missing prefab: " + CharacterPrefabPath);
        }

        GameObject mainCamera = CreateMainCamera(root.transform);
        if (character != null)
        {
            GameObject cameraTarget = GameObject.Find("Camera_Target");
            Transform target = cameraTarget != null ? cameraTarget.transform : null;
            if (target != null)
            {
                target.position = character.transform.position + new Vector3(0.0f, 1.25f, 0.0f);
            }
        }

        GameObject dirLight = CreateStaticDirectionalLight(root.transform);
        CreateCharacterFillLight(root.transform);
        CreateCharacterRimLight(root.transform);
        EnsureCharacterPreviewHelpers(root.transform);

        RenderSettings.sun = dirLight.GetComponent<Light>();
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.reflectionIntensity = 0.35f;

        if (!EditorSceneManager.SaveScene(scene, CharacterScenePath))
        {
            Debug.LogError("Failed to save scene at: " + CharacterScenePath);
            return;
        }

        EditorSceneManager.OpenScene(CharacterScenePath, OpenSceneMode.Single);
        if (character != null)
        {
            Selection.activeObject = character;
            EditorGUIUtility.PingObject(character);
        }
        else
        {
            Selection.activeObject = mainCamera;
            EditorGUIUtility.PingObject(mainCamera);
        }

        Debug.Log("[AllEffects] Character anime render test scene built: " + CharacterScenePath);
        Debug.Log("[AllEffects] Prefab source: " + CharacterPrefabPath);
        Debug.Log("[AllEffects] Ensure URP renderer enables SSAO/SSPR/SSR/SSGI/MotionBlur/ColorGrading features.");
    }

    [MenuItem("Tools/All Effects/Finalize Character NPR Stage And Capture")]
    public static void FinalizeCharacterNprStageAndCapture()
    {
        BuildCharacterRenderTestScene();
        FinalizeCharacterNprStage();
        CaptureCharacterAnimeTestShots();
    }

    [MenuItem("Tools/All Effects/Finalize Character NPR Stage")]
    public static void FinalizeCharacterNprStage()
    {
        if (!File.Exists(CharacterScenePath))
        {
            Debug.LogWarning("[AllEffects] Character scene missing, rebuilding first.");
            BuildCharacterRenderTestScene();
        }

        Scene scene = EditorSceneManager.OpenScene(CharacterScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("[AllEffects] Failed to open scene: " + CharacterScenePath);
            return;
        }

        RemoveIfExists("SideWall");
        RemoveIfExists("Floor_ReflectivePlate");
        RemoveIfExists("Scene_ReflectionProbe");

        GameObject floor = GameObject.Find("Floor_Neutral");
        if (floor == null)
            floor = GameObject.Find("Floor_Main");
        if (floor != null)
        {
            floor.name = "Floor_Neutral";
            floor.transform.position = Vector3.zero;
            floor.transform.rotation = Quaternion.identity;
            floor.transform.localScale = new Vector3(3.8f, 1.0f, 3.8f);
        }

        GameObject card = GameObject.Find("Background_Card");
        if (card == null)
            card = GameObject.Find("BackWall");
        if (card != null)
        {
            card.name = "Background_Card";
            card.transform.position = new Vector3(0.0f, 2.0f, 4.0f);
            card.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
            card.transform.localScale = new Vector3(5.0f, 4.0f, 0.15f);
        }

        if (!EditorSceneManager.SaveScene(scene, CharacterScenePath))
            Debug.LogError("[AllEffects] Save failed after finalizing: " + CharacterScenePath);
        else
            Debug.Log("[AllEffects] Character NPR stage finalized: " + CharacterScenePath);
    }

    [MenuItem("Tools/All Effects/Apply NPR-2 Materials To Character")]
    public static void ApplyNpr2MaterialsToCharacterInScene()
    {
        GameObject character = GameObject.Find("Player_Girl_Test");
        if (character == null)
            character = GameObject.Find("Player_Girl");
        if (character == null)
        {
            Debug.LogWarning("[AllEffects] Character not found. Expected: Player_Girl_Test or Player_Girl.");
            return;
        }

        ApplyNpr2MaterialsToCharacter(character);
        EditorSceneManager.MarkSceneDirty(character.scene);
        Debug.Log("[AllEffects] Applied NPR-2 materials to character.");
    }

    [MenuItem("Tools/All Effects/Apply NPR-3 Materials To Character")]
    public static void ApplyNpr3MaterialsToCharacterInScene()
    {
        GameObject character = GameObject.Find("Player_Girl_Test");
        if (character == null)
            character = GameObject.Find("Player_Girl");
        if (character == null)
        {
            Debug.LogWarning("[AllEffects] Character not found. Expected: Player_Girl_Test or Player_Girl.");
            return;
        }

        ApplyNpr3MaterialsToCharacter(character);
        EditorSceneManager.MarkSceneDirty(character.scene);
        Debug.Log("[AllEffects] Applied NPR-3 materials to character.");
    }

    [MenuItem("Tools/All Effects/Capture Character Anime Test Shots")]
    public static void CaptureCharacterAnimeTestShots()
    {
        EnsureFolder("Assets/Screenshots");
        EnsureFolder(CharacterShotDir);

        if (!File.Exists(CharacterScenePath))
            EditorSceneManager.OpenScene(CharacterScenePath, OpenSceneMode.Single);
        else
            EditorSceneManager.OpenScene(CharacterScenePath, OpenSceneMode.Single);

        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string gamePath = $"{CharacterShotDir}/CharacterTest-Game-{timestamp}.png";
        string scenePath = $"{CharacterShotDir}/CharacterTest-Scene-{timestamp}.png";

        GameObject target = GameObject.Find("Player_Girl_Test");
        if (target == null)
            target = GameObject.Find("Player_Girl");

        Vector3 focus = target != null ? target.transform.position + Vector3.up * 1.15f : Vector3.zero;

        GameObject gameCamGo = new GameObject("Temp_GameCaptureCamera");
        Camera gameCam = gameCamGo.AddComponent<Camera>();
        gameCam.clearFlags = CameraClearFlags.Skybox;
        gameCam.fieldOfView = 42.0f;
        gameCam.nearClipPlane = 0.1f;
        gameCam.farClipPlane = 250.0f;
        gameCam.transform.position = focus + new Vector3(0.0f, 0.3f, -3.1f);
        gameCam.transform.LookAt(focus);
        CaptureFromCamera(gameCam, gamePath, 1600, 900);
        GameObject.DestroyImmediate(gameCamGo);

        GameObject tempCamGo = new GameObject("Temp_SceneCaptureCamera");
        Camera tempCam = tempCamGo.AddComponent<Camera>();
        tempCam.clearFlags = CameraClearFlags.Skybox;
        tempCam.fieldOfView = 48.0f;
        tempCam.nearClipPlane = 0.1f;
        tempCam.farClipPlane = 250.0f;
        tempCam.transform.position = focus + new Vector3(4.2f, 3.0f, -4.4f);
        tempCam.transform.LookAt(focus);
        CaptureFromCamera(tempCam, scenePath, 1600, 900);
        GameObject.DestroyImmediate(tempCamGo);

        AssetDatabase.Refresh();
        Debug.Log("[AllEffects] Capture done.");
        Debug.Log("[AllEffects] Game shot: " + gamePath);
        Debug.Log("[AllEffects] Scene shot: " + scenePath);
    }

    [MenuItem("Tools/All Effects/Apply Character Lighting Preset")]
    public static void ApplyCharacterLightingPreset()
    {
        GameObject dir = GameObject.Find("Directional Light");
        GameObject fill = GameObject.Find("Character_FillPointLight");
        GameObject rim = GameObject.Find("Character_RimPointLight");

        if (dir == null || fill == null || rim == null)
        {
            Debug.LogWarning("[AllEffects] Missing light objects for lighting preset.");
            return;
        }

        Transform dirT = dir.transform;
        Light dirL = dir.GetComponent<Light>();
        if (dirL != null)
        {
            dirT.rotation = Quaternion.Euler(48.0f, 20.0f, 0.0f);
            dirL.type = LightType.Directional;
            dirL.intensity = 0.95f;
            dirL.color = new Color(0.98f, 0.98f, 1.0f, 1.0f);
            dirL.shadows = LightShadows.Soft;
            dirL.shadowStrength = 0.58f;
            EditorUtility.SetDirty(dirL);
        }
        EditorUtility.SetDirty(dirT);

        Transform fillT = fill.transform;
        Light fillL = fill.GetComponent<Light>();
        if (fillL != null)
        {
            fillT.position = new Vector3(-1.75f, 1.6f, -0.35f);
            fillL.type = LightType.Point;
            fillL.intensity = 0.34f;
            fillL.range = 5.0f;
            fillL.color = new Color(0.77f, 0.87f, 1.0f, 1.0f);
            fillL.shadows = LightShadows.None;
            EditorUtility.SetDirty(fillL);
        }
        EditorUtility.SetDirty(fillT);

        Transform rimT = rim.transform;
        Light rimL = rim.GetComponent<Light>();
        if (rimL != null)
        {
            rimT.position = new Vector3(1.75f, 2.05f, 1.25f);
            rimL.type = LightType.Point;
            rimL.intensity = 0.46f;
            rimL.range = 4.4f;
            rimL.color = new Color(1.0f, 0.91f, 0.82f, 1.0f);
            rimL.shadows = LightShadows.None;
            EditorUtility.SetDirty(rimL);
        }
        EditorUtility.SetDirty(rimT);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.21f, 0.27f, 1.0f);
        RenderSettings.ambientIntensity = 0.55f;
        RenderSettings.fog = false;

        if (SceneManager.GetActiveScene().IsValid())
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("[AllEffects] Character lighting preset applied.");
    }

    [MenuItem("Tools/All Effects/Setup Character Preview Helpers")]
    public static void SetupCharacterPreviewHelpersInScene()
    {
        GameObject root = GameObject.Find("CharacterRenderTest_Root");
        if (root == null)
        {
            Debug.LogWarning("[AllEffects] CharacterRenderTest_Root not found.");
            return;
        }

        EnsureCharacterPreviewHelpers(root.transform);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[AllEffects] Character preview helpers are ready.");
    }

    private static GameObject CreateMainCamera(Transform parent)
    {
        GameObject cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.SetParent(parent, false);

        Camera cam = cameraGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 50.0f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200.0f;
        cam.allowHDR = true;
        cam.transform.position = new Vector3(-4.5f, 3.2f, -6.2f);
        cam.transform.rotation = Quaternion.Euler(20.0f, 32.0f, 0.0f);

        cameraGo.AddComponent<AudioListener>();

        UniversalAdditionalCameraData urpData = cameraGo.GetComponent<UniversalAdditionalCameraData>();
        if (urpData == null)
            urpData = cameraGo.AddComponent<UniversalAdditionalCameraData>();

        urpData.renderPostProcessing = true;
        urpData.requiresDepthTexture = true;
        urpData.requiresColorTexture = true;

        GameObject target = new GameObject("Camera_Target");
        target.transform.SetParent(parent, false);
        target.transform.position = new Vector3(0.0f, 0.8f, 0.0f);

        FxLabCameraOrbit orbit = cameraGo.AddComponent<FxLabCameraOrbit>();
        orbit.target = target.transform;
        orbit.targetOffset = Vector3.zero;
        orbit.distance = 10.0f;
        orbit.autoOrbit = false;
        orbit.autoOrbitSpeed = 14.0f;

        return cameraGo;
    }

    private static GameObject CreateDirectionalLight(Transform parent)
    {
        GameObject go = new GameObject("Directional Light");
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(42.0f, -35.0f, 0.0f);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.9f;

        go.AddComponent<FxLabDirectionalLightAnimator>();
        return go;
    }

    private static GameObject CreateStaticDirectionalLight(Transform parent)
    {
        GameObject go = new GameObject("Directional Light");
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(44.0f, -28.0f, 0.0f);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.85f;
        return go;
    }

    private static GameObject CreateCharacterFillLight(Transform parent)
    {
        GameObject go = new GameObject("Character_FillPointLight");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(-1.3f, 1.9f, -1.0f);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = 1.1f;
        light.range = 5.0f;
        light.color = new Color(0.86f, 0.90f, 1.0f);
        light.shadows = LightShadows.None;
        return go;
    }

    private static GameObject CreateCharacterRimLight(Transform parent)
    {
        GameObject go = new GameObject("Character_RimPointLight");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(1.5f, 2.0f, 1.7f);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = 1.3f;
        light.range = 4.2f;
        light.color = new Color(0.98f, 0.98f, 1.0f);
        light.shadows = LightShadows.None;
        return go;
    }

    private static GameObject CreateMovingPointLight(Transform parent, Transform orbitAnchor)
    {
        GameObject go = new GameObject("Bloom_MovingPointLight");
        go.transform.SetParent(parent, false);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = 9.0f;
        light.range = 8.0f;
        light.color = new Color(1.0f, 0.65f, 0.25f);
        light.shadows = LightShadows.None;

        FxLabOrbitMotion motion = go.AddComponent<FxLabOrbitMotion>();
        motion.center = orbitAnchor != null ? orbitAnchor.position : new Vector3(1.8f, 1.2f, 1.2f);
        motion.axis = Vector3.up;
        motion.radius = 0.85f;
        motion.angularSpeed = -54.0f;
        motion.bobAmplitude = 0.2f;
        motion.bobFrequency = 1.2f;

        return go;
    }

    private static void CreateReflectionProbe(Transform parent)
    {
        GameObject probeGo = new GameObject("Scene_ReflectionProbe");
        probeGo.transform.SetParent(parent, false);
        probeGo.transform.position = new Vector3(0.0f, 1.8f, 0.0f);

        ReflectionProbe probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
        probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.size = new Vector3(20.0f, 8.0f, 20.0f);
        probe.intensity = 1.0f;
    }

    private static void EnsureCharacterPreviewHelpers(Transform root)
    {
        if (root == null)
            return;

        Transform grpScene = EnsureChildGroup(root, "_00_场景");
        Transform grpCharacter = EnsureChildGroup(root, "_10_角色");
        Transform grpCamera = EnsureChildGroup(root, "_20_相机");
        Transform grpLights = EnsureChildGroup(root, "_30_灯光");
        Transform grpControl = EnsureChildGroup(root, "_90_控制");

        ReparentByName("Floor_Neutral", grpScene);
        ReparentByName("Background_Card", grpScene);
        ReparentByName("Player_Girl_Test", grpCharacter);
        ReparentByName("Player_Girl", grpCharacter);
        ReparentByName("Main Camera", grpCamera);
        ReparentByName("Camera_Target", grpCamera);
        ReparentByName("Directional Light", grpLights);
        ReparentByName("Character_FillPointLight", grpLights);
        ReparentByName("Character_RimPointLight", grpLights);

        Transform helper = root.Find("Preview_GlobalController");
        if (helper == null)
        {
            GameObject go = new GameObject("Preview_GlobalController");
            go.transform.SetParent(grpControl, false);
            helper = go.transform;
        }
        else if (helper.parent != grpControl)
        {
            helper.SetParent(grpControl, true);
        }

        FxLabGlobalMotionController controller = helper.GetComponent<FxLabGlobalMotionController>();
        if (controller == null)
            controller = helper.gameObject.AddComponent<FxLabGlobalMotionController>();

        controller.globalSpeed = 1.0f;
        controller.syncTimeScale = false;
        controller.spinMultiplier = 1.0f;
        controller.orbitMultiplier = 1.0f;
        controller.cameraOrbitMultiplier = 1.0f;
        controller.lightCycleMultiplier = 1.0f;
        controller.includeInactive = true;

        GameObject cameraGo = GameObject.Find("Main Camera");
        if (cameraGo != null)
        {
            FxLabCameraOrbit orbit = cameraGo.GetComponent<FxLabCameraOrbit>();
            if (orbit != null)
            {
                orbit.autoOrbit = false;
                orbit.distance = Mathf.Clamp(orbit.distance, 3.5f, 6.0f);
                orbit.minDistance = 2.8f;
                orbit.maxDistance = 8.5f;
                orbit.mouseSensitivity = 2.2f;
            }
        }
    }

    private static Transform EnsureChildGroup(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            return child;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void ReparentByName(string objectName, Transform newParent)
    {
        if (newParent == null)
            return;

        GameObject go = GameObject.Find(objectName);
        if (go == null)
            return;

        if (go.transform.parent == newParent)
            return;

        go.transform.SetParent(newParent, true);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string name = Path.GetFileName(folder);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            return;

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static Material GetOrCreateLitMaterial(string path, Color baseColor, float metallic, float smoothness, Color emission)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                Debug.LogError("Cannot create material, missing shader: Universal Render Pipeline/Lit");
                return null;
            }

            mat = new Material(lit);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetColor("_EmissionColor", emission);

        if (emission.maxColorComponent > 0.0001f)
            mat.EnableKeyword("_EMISSION");
        else
            mat.DisableKeyword("_EMISSION");

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void ApplyNpr0MaterialsToCharacter(GameObject character)
    {
        if (character == null)
            return;

        Shader nprShader = Shader.Find("Custom/NPR-0/ToonBasicURP");
        if (nprShader == null)
        {
            Debug.LogWarning("[AllEffects] NPR-0 shader not found: Custom/NPR-0/ToonBasicURP");
            return;
        }

        Material template = GetOrCreateNpr0TemplateMaterial(nprShader);
        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            SkinnedMeshRenderer smr = renderers[r];
            Material[] mats = smr.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                Material src = mats[i];
                Material converted = src != null ? GetOrCreateConvertedNpr0Material(src, nprShader) : template;
                if (converted != null && converted != mats[i])
                {
                    mats[i] = converted;
                    changed = true;
                }
            }

            if (changed)
            {
                smr.sharedMaterials = mats;
                EditorUtility.SetDirty(smr);
            }
        }
    }

    private static void ApplyNpr2MaterialsToCharacter(GameObject character)
    {
        if (character == null)
            return;

        Shader nprShader = Shader.Find("Custom/NPR-2/RampOutlineURP");
        if (nprShader == null)
        {
            Debug.LogWarning("[AllEffects] NPR-2 shader not found: Custom/NPR-2/RampOutlineURP");
            return;
        }

        Material template = GetOrCreateNpr2TemplateMaterial(nprShader);
        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            SkinnedMeshRenderer smr = renderers[r];
            Material[] mats = smr.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                Material src = mats[i];
                Material converted = src != null ? GetOrCreateConvertedNpr2Material(src, nprShader) : template;
                if (converted != null && converted != mats[i])
                {
                    mats[i] = converted;
                    changed = true;
                }
            }

            if (changed)
            {
                smr.sharedMaterials = mats;
                EditorUtility.SetDirty(smr);
            }
        }
    }

    private static Material GetOrCreateNpr0TemplateMaterial(Shader shader)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(Npr0TemplatePath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, Npr0TemplatePath);
        }

        mat.shader = shader;
        Texture2D rampTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Npr2DefaultRampPath);
        if (rampTex != null)
            mat.SetTexture("_RampMap", rampTex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetColor("_ShadeColor", new Color(0.72f, 0.74f, 0.80f, 1.0f));
        mat.SetFloat("_ShadeThreshold", 0.50f);
        mat.SetFloat("_ShadeSoftness", 0.06f);
        mat.SetFloat("_ShadowStrength", 0.85f);
        mat.SetColor("_SpecColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
        mat.SetFloat("_SpecThreshold", 0.78f);
        mat.SetFloat("_SpecSoftness", 0.04f);
        mat.SetColor("_RimColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
        mat.SetFloat("_RimPower", 3.8f);
        mat.SetFloat("_RimStrength", 0.25f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateConvertedNpr0Material(Material source, Shader nprShader)
    {
        string safeName = GetCanonicalBaseMaterialName(source.name);
        string targetPath = Npr0ConvertedDir + "/" + safeName + "_NPR0.mat";
        Material target = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
        if (target == null)
        {
            target = new Material(nprShader);
            AssetDatabase.CreateAsset(target, targetPath);
        }

        target.shader = nprShader;
        Texture2D rampTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Npr2DefaultRampPath);
        if (rampTex != null)
            target.SetTexture("_RampMap", rampTex);

        Texture baseMap = null;
        if (source.HasProperty("_BaseMap"))
            baseMap = source.GetTexture("_BaseMap");
        else if (source.HasProperty("_MainTex"))
            baseMap = source.GetTexture("_MainTex");
        target.SetTexture("_BaseMap", baseMap);

        if (source.HasProperty("_BaseMap") && source.HasProperty("_BaseMap_ST"))
            target.SetVector("_BaseMap_ST", source.GetVector("_BaseMap_ST"));

        Color baseColor = Color.white;
        if (source.HasProperty("_BaseColor"))
            baseColor = source.GetColor("_BaseColor");
        else if (source.HasProperty("_Color"))
            baseColor = source.GetColor("_Color");
        target.SetColor("_BaseColor", baseColor);

        if (source.HasProperty("_BumpMap"))
            target.SetTexture("_NormalMap", source.GetTexture("_BumpMap"));
        if (source.HasProperty("_BumpScale"))
            target.SetFloat("_NormalScale", source.GetFloat("_BumpScale"));

        float cutoff = source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f;
        target.SetFloat("_Cutoff", cutoff);

        float alphaClip = source.IsKeywordEnabled("_ALPHATEST_ON") ? 1.0f : 0.0f;
        target.SetFloat("_AlphaClip", alphaClip);

        target.SetColor("_ShadeColor", new Color(0.72f, 0.74f, 0.80f, 1.0f));
        target.SetFloat("_ShadeThreshold", 0.50f);
        target.SetFloat("_ShadeSoftness", 0.06f);
        target.SetFloat("_ShadowStrength", 0.85f);
        target.SetColor("_SpecColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
        target.SetFloat("_SpecThreshold", 0.78f);
        target.SetFloat("_SpecSoftness", 0.04f);
        target.SetColor("_RimColor", new Color(1.0f, 1.0f, 1.0f, 1.0f));
        target.SetFloat("_RimPower", 3.8f);
        target.SetFloat("_RimStrength", 0.25f);

        EditorUtility.SetDirty(target);
        return target;
    }

    private static Material GetOrCreateNpr2TemplateMaterial(Shader shader)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(Npr2TemplatePath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, Npr2TemplatePath);
        }

        mat.shader = shader;
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_RampOffset", 0.0f);
        mat.SetFloat("_RampContrast", 1.0f);
        mat.SetFloat("_RampStrength", 1.0f);
        mat.SetFloat("_ShadowStrength", 1.0f);
        mat.SetFloat("_AmbientStrength", 0.15f);
        mat.SetColor("_SpecColor", Color.white);
        mat.SetFloat("_SpecThreshold", 0.90f);
        mat.SetFloat("_SpecSoftness", 0.03f);
        mat.SetColor("_RimColor", Color.white);
        mat.SetFloat("_RimPower", 3.8f);
        mat.SetFloat("_RimStrength", 0.28f);
        mat.SetFloat("_AdditionalLightStrength", 0.20f);
        mat.SetColor("_OutlineColor", new Color(0.07f, 0.09f, 0.12f, 1.0f));
        mat.SetFloat("_OutlineWidth", 4.0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateConvertedNpr2Material(Material source, Shader nprShader)
    {
        string safeName = GetCanonicalBaseMaterialName(source.name);
        string targetPath = Npr2ConvertedDir + "/" + safeName + "_NPR2.mat";
        Material target = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
        if (target == null)
        {
            target = new Material(nprShader);
            AssetDatabase.CreateAsset(target, targetPath);
        }

        target.shader = nprShader;

        Texture baseMap = null;
        if (source.HasProperty("_BaseMap"))
            baseMap = source.GetTexture("_BaseMap");
        else if (source.HasProperty("_MainTex"))
            baseMap = source.GetTexture("_MainTex");
        target.SetTexture("_BaseMap", baseMap);

        if (source.HasProperty("_BaseMap_ST"))
            target.SetVector("_BaseMap_ST", source.GetVector("_BaseMap_ST"));

        Color baseColor = Color.white;
        if (source.HasProperty("_BaseColor"))
            baseColor = source.GetColor("_BaseColor");
        else if (source.HasProperty("_Color"))
            baseColor = source.GetColor("_Color");
        target.SetColor("_BaseColor", baseColor);

        if (source.HasProperty("_BumpMap"))
            target.SetTexture("_NormalMap", source.GetTexture("_BumpMap"));
        if (source.HasProperty("_BumpScale"))
            target.SetFloat("_NormalScale", source.GetFloat("_BumpScale"));

        float cutoff = source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f;
        target.SetFloat("_Cutoff", cutoff);
        target.SetFloat("_AlphaClip", source.IsKeywordEnabled("_ALPHATEST_ON") ? 1.0f : 0.0f);

        target.SetFloat("_RampOffset", 0.0f);
        target.SetFloat("_RampContrast", 1.0f);
        target.SetFloat("_RampStrength", 1.0f);
        target.SetFloat("_ShadowStrength", 1.0f);
        target.SetFloat("_AmbientStrength", 0.15f);
        target.SetColor("_SpecColor", Color.white);
        target.SetFloat("_SpecThreshold", 0.90f);
        target.SetFloat("_SpecSoftness", 0.03f);
        target.SetColor("_RimColor", Color.white);
        target.SetFloat("_RimPower", 3.8f);
        target.SetFloat("_RimStrength", 0.28f);
        target.SetFloat("_AdditionalLightStrength", 0.20f);
        target.SetColor("_OutlineColor", new Color(0.07f, 0.09f, 0.12f, 1.0f));
        target.SetFloat("_OutlineWidth", 4.0f);

        EditorUtility.SetDirty(target);
        return target;
    }

    private static void ApplyNpr3MaterialsToCharacter(GameObject character)
    {
        if (character == null)
            return;

        Shader nprShader = Shader.Find("Custom/NPR-3/CharacterAdvancedURP");
        if (nprShader == null)
        {
            Debug.LogWarning("[AllEffects] NPR-3 shader not found: Custom/NPR-3/CharacterAdvancedURP");
            return;
        }

        Material template = GetOrCreateNpr3TemplateMaterial(nprShader);
        SkinnedMeshRenderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            SkinnedMeshRenderer smr = renderers[r];
            Material[] mats = smr.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                Material src = mats[i];
                Material converted = src != null ? GetOrCreateConvertedNpr3Material(src, nprShader) : template;
                if (converted != null && converted != mats[i])
                {
                    mats[i] = converted;
                    changed = true;
                }
            }

            if (changed)
            {
                smr.sharedMaterials = mats;
                EditorUtility.SetDirty(smr);
            }
        }
    }

    private static Material GetOrCreateNpr3TemplateMaterial(Shader shader)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(Npr3TemplatePath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, Npr3TemplatePath);
        }

        mat.shader = shader;
        Texture2D rampTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Npr2DefaultRampPath);
        if (rampTex != null)
            mat.SetTexture("_RampMap", rampTex);

        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_RampOffset", 0.0f);
        mat.SetFloat("_RampContrast", 1.28f);
        mat.SetFloat("_RampStrength", 1.10f);
        mat.SetFloat("_RampBands", 4.0f);
        mat.SetFloat("_ShadowStrength", 0.92f);
        mat.SetFloat("_AmbientStrength", 0.14f);
        mat.SetColor("_ShadowTintColor", new Color(0.75f, 0.83f, 1.0f, 1.0f));
        mat.SetFloat("_ShadowTintStrength", 0.35f);
        mat.SetColor("_ShadowCoolColor", new Color(0.72f, 0.82f, 1.0f, 1.0f));
        mat.SetColor("_ShadowWarmColor", new Color(1.0f, 0.86f, 0.74f, 1.0f));
        mat.SetFloat("_ShadowStylizeStrength", 0.0f);
        mat.SetFloat("_ShadowTerminatorWidth", 0.28f);
        mat.SetFloat("_ShadowTerminatorSoftness", 0.22f);
        mat.SetColor("_SpecColor", new Color(0.38f, 0.38f, 0.38f, 1.0f));
        mat.SetFloat("_SpecThreshold", 0.994f);
        mat.SetFloat("_SpecSoftness", 0.005f);
        mat.SetFloat("_HairSpecStrength", 0.0f);
        mat.SetFloat("_HairSpecShift", 0.10f);
        mat.SetFloat("_HairSpecExponent1", 64.0f);
        mat.SetFloat("_HairSpecExponent2", 20.0f);
        mat.SetFloat("_HairSpecSecondaryStrength", 0.45f);
        mat.SetColor("_RimColor", Color.white);
        mat.SetFloat("_RimPower", 3.8f);
        mat.SetFloat("_RimStrength", 0.08f);
        mat.SetFloat("_AdditionalLightStrength", 0.10f);
        mat.SetFloat("_ColorSaturation", 1.30f);
        mat.SetFloat("_FaceRegionWeight", 0.0f);
        mat.SetFloat("_FaceShadowLift", 0.0f);
        mat.SetFloat("_FaceForwardWrap", 0.0f);
        mat.SetFloat("_FaceSpecBoost", 0.0f);
        mat.SetFloat("_FaceRimSuppress", 0.0f);
        mat.SetColor("_OutlineColor", new Color(0.08f, 0.11f, 0.16f, 1.0f));
        mat.SetFloat("_OutlineWidth", 3.0f);
        mat.SetFloat("_OutlineMinScale", 0.55f);
        mat.SetFloat("_OutlineDistanceStart", 2.0f);
        mat.SetFloat("_OutlineDistanceEnd", 10.0f);
        mat.SetFloat("_OutlineSilhouetteBoost", 0.35f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateConvertedNpr3Material(Material source, Shader nprShader)
    {
        string safeName = GetCanonicalBaseMaterialName(source.name);
        string targetPath = Npr3ConvertedDir + "/" + safeName + "_NPR3.mat";
        Material target = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
        if (target == null)
        {
            target = new Material(nprShader);
            AssetDatabase.CreateAsset(target, targetPath);
        }

        target.shader = nprShader;

        Texture baseMap = null;
        if (source.HasProperty("_BaseMap"))
            baseMap = source.GetTexture("_BaseMap");
        else if (source.HasProperty("_MainTex"))
            baseMap = source.GetTexture("_MainTex");
        target.SetTexture("_BaseMap", baseMap);

        Texture2D rampTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Npr2DefaultRampPath);
        if (rampTex != null)
            target.SetTexture("_RampMap", rampTex);

        if (source.HasProperty("_BaseMap_ST"))
            target.SetVector("_BaseMap_ST", source.GetVector("_BaseMap_ST"));

        Color baseColor = Color.white;
        if (source.HasProperty("_BaseColor"))
            baseColor = source.GetColor("_BaseColor");
        else if (source.HasProperty("_Color"))
            baseColor = source.GetColor("_Color");
        target.SetColor("_BaseColor", baseColor);

        if (source.HasProperty("_BumpMap"))
            target.SetTexture("_NormalMap", source.GetTexture("_BumpMap"));
        if (source.HasProperty("_BumpScale"))
            target.SetFloat("_NormalScale", source.GetFloat("_BumpScale"));

        float cutoff = source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f;
        target.SetFloat("_Cutoff", cutoff);
        target.SetFloat("_AlphaClip", source.IsKeywordEnabled("_ALPHATEST_ON") ? 1.0f : 0.0f);

        target.SetFloat("_RampOffset", 0.0f);
        target.SetFloat("_RampContrast", 1.28f);
        target.SetFloat("_RampStrength", 1.10f);
        target.SetFloat("_RampBands", 4.0f);
        target.SetFloat("_ShadowStrength", 0.92f);
        target.SetFloat("_AmbientStrength", 0.14f);
        target.SetColor("_ShadowTintColor", new Color(0.75f, 0.83f, 1.0f, 1.0f));
        target.SetFloat("_ShadowTintStrength", 0.35f);
        target.SetColor("_ShadowCoolColor", new Color(0.72f, 0.82f, 1.0f, 1.0f));
        target.SetColor("_ShadowWarmColor", new Color(1.0f, 0.86f, 0.74f, 1.0f));
        target.SetFloat("_ShadowStylizeStrength", 0.0f);
        target.SetFloat("_ShadowTerminatorWidth", 0.28f);
        target.SetFloat("_ShadowTerminatorSoftness", 0.22f);
        target.SetColor("_SpecColor", new Color(0.38f, 0.38f, 0.38f, 1.0f));
        target.SetFloat("_SpecThreshold", 0.994f);
        target.SetFloat("_SpecSoftness", 0.005f);
        target.SetFloat("_HairSpecStrength", 0.0f);
        target.SetFloat("_HairSpecShift", 0.10f);
        target.SetFloat("_HairSpecExponent1", 64.0f);
        target.SetFloat("_HairSpecExponent2", 20.0f);
        target.SetFloat("_HairSpecSecondaryStrength", 0.45f);
        target.SetColor("_RimColor", Color.white);
        target.SetFloat("_RimPower", 3.8f);
        target.SetFloat("_RimStrength", 0.08f);
        target.SetFloat("_AdditionalLightStrength", 0.10f);
        target.SetFloat("_ColorSaturation", 1.30f);
        target.SetFloat("_FaceRegionWeight", 0.0f);
        target.SetFloat("_FaceShadowLift", 0.0f);
        target.SetFloat("_FaceForwardWrap", 0.0f);
        target.SetFloat("_FaceSpecBoost", 0.0f);
        target.SetFloat("_FaceRimSuppress", 0.0f);
        target.SetColor("_OutlineColor", new Color(0.08f, 0.11f, 0.16f, 1.0f));
        target.SetFloat("_OutlineWidth", 3.0f);
        target.SetFloat("_OutlineMinScale", 0.55f);
        target.SetFloat("_OutlineDistanceStart", 2.0f);
        target.SetFloat("_OutlineDistanceEnd", 10.0f);
        target.SetFloat("_OutlineSilhouetteBoost", 0.35f);

        EditorUtility.SetDirty(target);
        return target;
    }

    private static void CaptureFromCamera(Camera cam, string outputPath, int width, int height)
    {
        RenderTexture prevRT = cam.targetTexture;
        RenderTexture activeRT = RenderTexture.active;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply(false, false);

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);

        cam.targetTexture = prevRT;
        RenderTexture.active = activeRT;
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void RemoveIfExists(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        if (go != null)
            UnityEngine.Object.DestroyImmediate(go);
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Material";

        char[] invalid = Path.GetInvalidFileNameChars();
        string result = name;
        for (int i = 0; i < invalid.Length; i++)
            result = result.Replace(invalid[i], '_');
        return result;
    }

    private static string GetCanonicalBaseMaterialName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return "Material";

        string name = rawName;
        string[] knownSuffixes = { "_NPR0", "_NPR2", "_NPR3", "_NPR4" };
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < knownSuffixes.Length; i++)
            {
                string suffix = knownSuffixes[i];
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                    changed = true;
                }
            }
        }

        return SanitizeFileName(name);
    }

    private static void SnapCharacterToGround(GameObject character)
    {
        if (character == null)
            return;

        Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        float deltaY = -b.min.y;
        if (Mathf.Abs(deltaY) > 0.0001f)
        {
            Vector3 pos = character.transform.position;
            pos.y += deltaY;
            character.transform.position = pos;
        }
    }

    private static void NormalizeCharacterHeight(GameObject character, float targetHeight)
    {
        if (character == null || targetHeight <= 0.01f)
            return;

        Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        float currentHeight = Mathf.Max(b.size.y, 0.0001f);
        float factor = targetHeight / currentHeight;

        if (factor > 0.001f && factor < 1000.0f)
            character.transform.localScale *= factor;
    }
}
#endif
