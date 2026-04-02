using System.IO;
using UnityEditor;
using UnityEngine;

public static class FbxMaterialSplitter
{
    private const string FurinaRendererPath = "CharacterRenderTest_Root/_10_角色/芙宁/网格";
    private const string SplitRootName = "网格_单材质组";

    [MenuItem("Tools/NPR 风格/网格拆分/拆分选中SkinnedMesh(单材质)")]
    public static void SplitSelectedSkinnedMesh()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogWarning("[Split] 请先选中一个带 SkinnedMeshRenderer 的对象。");
            return;
        }

        SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogWarning("[Split] 当前选中对象没有 SkinnedMeshRenderer: " + go.name);
            return;
        }

        SplitSkinnedRenderer(go, smr);
    }

    [MenuItem("Tools/NPR 风格/网格拆分/拆分芙宁娜SkinnedMesh(单材质)")]
    public static void SplitFurinaSkinnedMesh()
    {
        GameObject go = GameObject.Find(FurinaRendererPath);
        if (go == null)
        {
            Debug.LogError("[Split] 未找到对象: " + FurinaRendererPath);
            return;
        }

        SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogError("[Split] 对象缺少 SkinnedMeshRenderer: " + FurinaRendererPath);
            return;
        }

        SplitSkinnedRenderer(go, smr);
    }

    private static void SplitSkinnedRenderer(GameObject sourceGo, SkinnedMeshRenderer sourceSmr)
    {
        Mesh sourceMesh = sourceSmr.sharedMesh;
        if (sourceMesh == null)
        {
            Debug.LogError("[Split] 源网格为空: " + sourceGo.name);
            return;
        }

        int subMeshCount = sourceMesh.subMeshCount;
        Material[] mats = sourceSmr.sharedMaterials;
        if (subMeshCount <= 1)
        {
            Debug.Log("[Split] 已是单材质网格，无需拆分: " + sourceGo.name);
            return;
        }

        string sourceMeshPath = AssetDatabase.GetAssetPath(sourceMesh);
        if (string.IsNullOrEmpty(sourceMeshPath))
        {
            sourceMeshPath = "Assets/_Model";
        }

        string meshFolder = EnsureSplitFolder(sourceMeshPath);
        Transform parent = sourceGo.transform.parent;

        Transform splitRoot = sourceGo.transform.Find(SplitRootName);
        if (splitRoot != null)
        {
            Undo.DestroyObjectImmediate(splitRoot.gameObject);
        }

        GameObject splitRootGo = new GameObject(SplitRootName);
        Undo.RegisterCreatedObjectUndo(splitRootGo, "Create Split Root");
        splitRootGo.transform.SetParent(sourceGo.transform, false);
        splitRootGo.transform.localPosition = Vector3.zero;
        splitRootGo.transform.localRotation = Quaternion.identity;
        splitRootGo.transform.localScale = Vector3.one;

        for (int i = 0; i < subMeshCount; i++)
        {
            string partName = GetPartName(i, mats);
            string meshName = sourceMesh.name + "_" + partName + "_SM" + i.ToString("D2");
            string meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(meshFolder, meshName + ".asset").Replace("\\", "/"));

            Mesh splitMesh = BuildSingleSubmeshMesh(sourceMesh, i);
            AssetDatabase.CreateAsset(splitMesh, meshAssetPath);

            GameObject partGo = new GameObject(partName);
            Undo.RegisterCreatedObjectUndo(partGo, "Create Split Part");
            partGo.transform.SetParent(splitRootGo.transform, false);
            partGo.transform.localPosition = Vector3.zero;
            partGo.transform.localRotation = Quaternion.identity;
            partGo.transform.localScale = Vector3.one;

            SkinnedMeshRenderer partSmr = partGo.AddComponent<SkinnedMeshRenderer>();
            partSmr.sharedMesh = splitMesh;
            partSmr.rootBone = sourceSmr.rootBone;
            partSmr.bones = sourceSmr.bones;
            partSmr.quality = sourceSmr.quality;
            partSmr.updateWhenOffscreen = sourceSmr.updateWhenOffscreen;
            partSmr.skinnedMotionVectors = sourceSmr.skinnedMotionVectors;
            partSmr.localBounds = sourceSmr.localBounds;
            partSmr.lightProbeUsage = sourceSmr.lightProbeUsage;
            partSmr.reflectionProbeUsage = sourceSmr.reflectionProbeUsage;
            partSmr.shadowCastingMode = sourceSmr.shadowCastingMode;
            partSmr.receiveShadows = sourceSmr.receiveShadows;
            partSmr.allowOcclusionWhenDynamic = sourceSmr.allowOcclusionWhenDynamic;
            partSmr.motionVectorGenerationMode = sourceSmr.motionVectorGenerationMode;
            partSmr.probeAnchor = sourceSmr.probeAnchor;

            Material mat = i < mats.Length ? mats[i] : null;
            if (mat != null)
            {
                partSmr.sharedMaterial = mat;
            }
        }

        sourceSmr.enabled = false;
        EditorUtility.SetDirty(sourceSmr);
        EditorUtility.SetDirty(splitRootGo);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Split] 完成: " + sourceGo.name + " 子网格数=" + subMeshCount + "，已生成单材质子组件。");
    }

    private static Mesh BuildSingleSubmeshMesh(Mesh source, int subMeshIndex)
    {
        Mesh m = Object.Instantiate(source);
        m.name = source.name + "_Split_" + subMeshIndex.ToString("D2");
        int[] indices = source.GetIndices(subMeshIndex);
        MeshTopology topology = source.GetTopology(subMeshIndex);
        m.subMeshCount = 1;
        m.SetIndices(indices, topology, 0, false);
        m.UploadMeshData(false);
        return m;
    }

    private static string EnsureSplitFolder(string sourceMeshPath)
    {
        string folder = Path.GetDirectoryName(sourceMeshPath);
        if (string.IsNullOrEmpty(folder))
        {
            folder = "Assets";
        }

        folder = folder.Replace("\\", "/");
        string splitFolder = folder + "/SplitMeshes";
        if (!AssetDatabase.IsValidFolder(splitFolder))
        {
            AssetDatabase.CreateFolder(folder, "SplitMeshes");
        }

        return splitFolder;
    }

    private static string GetPartName(int index, Material[] mats)
    {
        if (mats != null && index < mats.Length && mats[index] != null)
        {
            return "部件_" + mats[index].name;
        }

        return "部件_" + index.ToString("D2");
    }
}
