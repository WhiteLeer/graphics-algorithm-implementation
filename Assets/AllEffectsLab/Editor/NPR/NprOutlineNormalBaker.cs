using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class NprOutlineNormalBaker
{
    private const string HoudiniRoot = "Assets/MaterialFX/NPR/NPR-Core/Meshes/Cache_Baked";
    private const string AppliedRoot = "Assets/MaterialFX/NPR/NPR-Core/Meshes/Applied";
    private const string SourcePrefabPath = "Assets/_Prefab/Player_Girl.prefab";

    [MenuItem("Tools/NPR 风格/应用 Houdini 描边网格(选中)")]
    public static void ApplyHoudiniBakedMeshes()
    {
        if (!AssetDatabase.IsValidFolder(HoudiniRoot))
        {
            Debug.LogWarning("[NPR] 未找到 Houdini 目录: " + HoudiniRoot);
            return;
        }
        EnsureFolder("Assets/MaterialFX/NPR/NPR-Core/Meshes", "Applied");

        int applied = 0;
        int renderers = 0;
        SkinnedMeshRenderer[] allSmrs = Object.FindObjectsOfType<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer smr in allSmrs)
        {
            TryApplySingle(smr, ref renderers, ref applied);
        }
        Debug.Log("[NPR] 已按全场景执行 Houdini 法线到顶点色。");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NPR] Houdini 法线应用完成。Renderers: " + renderers + ", Applied: " + applied);
    }

    [MenuItem("Tools/NPR 风格/恢复原始骨骼网格(全场景)")]
    public static void RestoreOriginalSkinnedMeshes()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning("[NPR] 无法加载源 Prefab: " + SourcePrefabPath);
            return;
        }

        var sourceMap = new Dictionary<string, Mesh>();
        SkinnedMeshRenderer[] prefabSmrs = prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < prefabSmrs.Length; i++)
        {
            SkinnedMeshRenderer smr = prefabSmrs[i];
            if (smr != null && smr.sharedMesh != null)
            {
                sourceMap[smr.gameObject.name] = smr.sharedMesh;
            }
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);

        int restored = 0;
        SkinnedMeshRenderer[] sceneSmrs = Object.FindObjectsOfType<SkinnedMeshRenderer>(true);
        for (int i = 0; i < sceneSmrs.Length; i++)
        {
            SkinnedMeshRenderer smr = sceneSmrs[i];
            if (smr == null) continue;
            if (!sourceMap.TryGetValue(smr.gameObject.name, out Mesh src) || src == null) continue;
            smr.sharedMesh = src;
            EditorUtility.SetDirty(smr);
            restored++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NPR] 已恢复原始骨骼网格，数量: " + restored);
    }

    private static void TryApplySingle(SkinnedMeshRenderer smr, ref int renderers, ref int applied)
    {
        renderers++;
        Mesh src = smr.sharedMesh;
        if (src == null)
        {
            return;
        }

        Mesh baked = FindHoudiniMeshByName(src.name, smr.gameObject.name);
        if (baked == null)
        {
            return;
        }

        if (baked.normals == null || baked.normals.Length == 0)
        {
            return;
        }

        Mesh appliedMesh = BuildAppliedMeshFromHoudiniNormals(src, baked, smr.gameObject.name);
        if (appliedMesh == null)
        {
            return;
        }

        smr.sharedMesh = appliedMesh;
        EditorUtility.SetDirty(smr);
        applied++;

        Material[] mats = smr.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            Material mat = mats[i];
            if (mat != null && mat.HasProperty("_OutlineUseVertexColorNormal"))
            {
                mat.SetFloat("_OutlineUseVertexColorNormal", 1.0f);
                EditorUtility.SetDirty(mat);
            }
        }
    }

    private static Mesh BuildAppliedMeshFromHoudiniNormals(Mesh src, Mesh baked, string objectName)
    {
        Mesh outMesh = Object.Instantiate(src);
        outMesh.name = src.name + "_Applied";

        Vector3[] srcVertices = outMesh.vertices;
        Vector3[] bakedNormals = baked.normals;
        Color[] colors = new Color[srcVertices.Length];

        if (baked.vertexCount == srcVertices.Length)
        {
            for (int i = 0; i < srcVertices.Length; i++)
            {
                Vector3 n = bakedNormals[i].normalized;
                colors[i] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1.0f);
            }
        }
        else
        {
            Vector3[] bakedVertices = baked.vertices;
            var normalMap = new Dictionary<Vector3Int, Vector3>(bakedVertices.Length);
            var countMap = new Dictionary<Vector3Int, int>(bakedVertices.Length);

            for (int i = 0; i < bakedVertices.Length; i++)
            {
                Vector3Int key = Quantize(bakedVertices[i]);
                Vector3 n = bakedNormals[Mathf.Min(i, bakedNormals.Length - 1)];
                if (normalMap.ContainsKey(key))
                {
                    normalMap[key] += n;
                    countMap[key] += 1;
                }
                else
                {
                    normalMap[key] = n;
                    countMap[key] = 1;
                }
            }

            Vector3[] srcNormals = outMesh.normals;
            for (int i = 0; i < srcVertices.Length; i++)
            {
                Vector3 n = (srcNormals != null && srcNormals.Length == srcVertices.Length) ? srcNormals[i] : Vector3.up;
                Vector3Int key = Quantize(srcVertices[i]);
                if (normalMap.TryGetValue(key, out Vector3 accum))
                {
                    n = accum / Mathf.Max(1, countMap[key]);
                }
                n.Normalize();
                colors[i] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1.0f);
            }
        }

        outMesh.colors = colors;

        string fileName = "MESH_NPR3_" + SanitizeName(objectName) + "_Applied.asset";
        string path = AppliedRoot + "/" + fileName;
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        AssetDatabase.CreateAsset(outMesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    private static Mesh FindHoudiniMeshByName(string sourceName, string objectName)
    {
        string[] keys =
        {
            NormalizeMeshName(sourceName),
            NormalizeMeshName(objectName),
            NormalizeMeshName(sourceName).Replace(" ", string.Empty).Replace("-", "_"),
            NormalizeMeshName(objectName).Replace(" ", string.Empty).Replace("-", "_")
        };

        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            // Prefer model file name match, then resolve mesh sub-asset from that file.
            string[] modelGuids = AssetDatabase.FindAssets(key + " t:Model", new[] { HoudiniRoot });
            if (modelGuids != null && modelGuids.Length > 0)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(modelGuids[0]);
                Object[] subs = AssetDatabase.LoadAllAssetsAtPath(modelPath);
                for (int si = 0; si < subs.Length; si++)
                {
                    Mesh subMesh = subs[si] as Mesh;
                    if (subMesh != null)
                    {
                        return subMesh;
                    }
                }
            }

            string[] candidates = AssetDatabase.FindAssets(key + " t:Mesh", new[] { HoudiniRoot });
            if (candidates == null || candidates.Length == 0)
            {
                continue;
            }

            string path = AssetDatabase.GUIDToAssetPath(candidates[0]);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
            {
                return mesh;
            }
        }

        return null;
    }

    private static string NormalizeMeshName(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return string.Empty;
        }

        const string bakedSuffix = "_OutlineBaked";
        if (sourceName.EndsWith(bakedSuffix))
        {
            return sourceName.Substring(0, sourceName.Length - bakedSuffix.Length);
        }
        const string appliedSuffix = "_Applied";
        if (sourceName.EndsWith(appliedSuffix))
        {
            return sourceName.Substring(0, sourceName.Length - appliedSuffix.Length);
        }
        return sourceName;
    }

    private static Vector3Int Quantize(Vector3 v)
    {
        const float scale = 10000.0f;
        return new Vector3Int(
            Mathf.RoundToInt(v.x * scale),
            Mathf.RoundToInt(v.y * scale),
            Mathf.RoundToInt(v.z * scale));
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "mesh";
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Replace(' ', '_');
    }
}


