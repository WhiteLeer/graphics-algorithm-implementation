using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FurinaImportPostProcessor
{
    [MenuItem("Tools/NPR 风格/修正芙宁娜导入(中文命名+材质外置)")]
    public static void Process()
    {
        const string rootPath = "CharacterRenderTest_Root/_10_角色/Furina_MMD_Textured";
        GameObject root = GameObject.Find(rootPath);
        if (root == null)
        {
            Debug.LogError("[Furina] 未找到对象: " + rootPath);
            return;
        }

        RenameHierarchy(root.transform);
        ExternalizeRendererMaterials(root);
        MarkSceneDirtyNow();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Furina] 导入后处理完成。");
    }

    private static void RenameHierarchy(Transform root)
    {
        root.name = "芙宁";

        Dictionary<string, string> map = new Dictionary<string, string>
        {
            { "【芙宁娜】", "骨架" },
            { "joints", "关节" },
            { "rigidbodies", "刚体" },
            { "【芙宁娜】_arm", "骨臂" },
            { "【芙宁娜】_mesh", "网格" },
        };

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (map.TryGetValue(t.name, out string newName))
            {
                t.name = newName;
            }
        }
    }

    private static void ExternalizeRendererMaterials(GameObject root)
    {
        SkinnedMeshRenderer renderer = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (renderer == null)
        {
            Debug.LogWarning("[Furina] 未找到 SkinnedMeshRenderer");
            return;
        }

        Material[] src = renderer.sharedMaterials;
        if (src == null || src.Length == 0)
        {
            Debug.LogWarning("[Furina] 未找到材质槽");
            return;
        }

        const string baseDir = "Assets/_Model/Genshin_Furina_MMD/Converted";
        const string matDir = baseDir + "/Materials";
        EnsureFolder(baseDir, "Materials");

        string[] names =
        {
            "颜面", "口舌", "发丝", "发梢", "肤体", "衣饰", "裙摆", "装饰", "高光", "透层",
            "细节", "配件", "鞋袜", "眼瞳", "眉睫", "帽饰", "领结", "纹理", "边饰", "底衬",
            "附饰", "流苏", "神眼", "袖口", "衣摆", "暗纹", "亮纹", "珠饰", "花边", "扣饰",
            "渐层", "描边", "备用", "辅材", "材料", "表面", "外层", "内层", "主材", "次材"
        };

        Material[] dst = new Material[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            Material m = src[i];
            if (m == null)
            {
                continue;
            }

            string zh = i < names.Length ? names[i] : "材" + i.ToString("D2");
            string assetPath = matDir + "/" + zh + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing == null)
            {
                Material created = new Material(m);
                created.name = zh;
                AssetDatabase.CreateAsset(created, assetPath);
                existing = created;
            }
            dst[i] = existing;
        }

        renderer.sharedMaterials = dst;
        EditorUtility.SetDirty(renderer);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void MarkSceneDirtyNow()
    {
        UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }
}
