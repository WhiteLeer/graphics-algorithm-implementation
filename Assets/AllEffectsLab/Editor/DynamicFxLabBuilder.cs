#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class DynamicFxLabBuilder
{
    private const string ScenePath = "Assets/Scenes/AllEffects-DynamicLab.unity";
    private const string MaterialRoot = "Assets/MaterialFX/Common_LitLibrary";

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
        orbit.autoOrbit = true;
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
}
#endif
