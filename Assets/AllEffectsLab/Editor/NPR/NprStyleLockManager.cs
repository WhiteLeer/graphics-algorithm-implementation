using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class NprStyleLockManager
{
    private const string ProfileRoot = "Assets/MaterialFX/NPR/NPR-Core/Materials/StyleProfiles";
    private const string LockedRoot = "Assets/MaterialFX/NPR/NPR-Core/Materials/StyleProfilesLocked";
    private const string SnapshotDocPath = "Assets/MaterialFX/NPR/NPR-Core/README_NPR3_LockedParams.md";

    private static readonly string[] Styles = { "Genshin", "HSR", "ZZZ" };

    private static readonly string[] SnapshotProps =
    {
        "_ColorSaturation",
        "_ExposureCompensation",
        "_ToonContrast",
        "_ShadowStrength",
        "_AmbientStrength",
        "_RampContrast",
        "_RampBands",
        "_SpecThreshold",
        "_SpecSoftness",
        "_HairSpecStrength",
        "_HairSpecExponent1",
        "_HairSpecExponent2",
        "_FaceOrientationStrength",
        "_OutlineWidth",
        "_OutlineWidthMapStrength",
        "_OutlineUseVertexColorNormal",
    };

    [MenuItem("Tools/NPR 风格/锁定当前三风格参数(保存快照)")]
    public static void SaveLockedProfiles()
    {
        EnsureFolder("Assets/MaterialFX/NPR/NPR-Core/Materials", "StyleProfilesLocked");

        var md = new StringBuilder();
        md.AppendLine("# NPR-3 Locked Parameters");
        md.AppendLine();
        md.AppendLine("- Generated At: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        md.AppendLine("- Source: `Assets/MaterialFX/NPR/NPR-Core/Materials/StyleProfiles`");
        md.AppendLine("- Purpose: Lock current approved style params and allow one-click restore.");
        md.AppendLine();

        int copied = 0;
        foreach (string style in Styles)
        {
            string srcDir = $"{ProfileRoot}/{style}/Character";
            string dstStyle = $"{LockedRoot}/{style}";
            EnsureFolder(LockedRoot, style);
            EnsureFolder(dstStyle, "Character");
            string dstDir = $"{dstStyle}/Character";

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { srcDir });
            md.AppendLine($"## {style}");
            md.AppendLine();

            foreach (string guid in guids)
            {
                string srcPath = AssetDatabase.GUIDToAssetPath(guid);
                Material src = AssetDatabase.LoadAssetAtPath<Material>(srcPath);
                if (src == null)
                {
                    continue;
                }

                string dstPath = $"{dstDir}/{Path.GetFileName(srcPath)}";
                Material dst = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
                if (dst == null)
                {
                    if (AssetDatabase.CopyAsset(srcPath, dstPath))
                    {
                        copied++;
                    }
                }
                else
                {
                    EditorUtility.CopySerialized(src, dst);
                    EditorUtility.SetDirty(dst);
                    copied++;
                }

                md.AppendLine($"### {src.name}");
                foreach (string prop in SnapshotProps)
                {
                    if (!src.HasProperty(prop))
                    {
                        continue;
                    }
                    md.AppendLine($"- `{prop}`: {ReadPropValue(src, prop)}");
                }
                md.AppendLine();
            }
        }

        WriteTextAsset(SnapshotDocPath, md.ToString());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[NPR] 锁定完成，已更新锁定材质条目: {copied}。");
    }

    [MenuItem("Tools/NPR 风格/恢复锁定参数(覆盖当前)")]
    public static void RestoreLockedProfiles()
    {
        int restored = 0;
        foreach (string style in Styles)
        {
            string srcDir = $"{LockedRoot}/{style}/Character";
            string dstDir = $"{ProfileRoot}/{style}/Character";
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { srcDir });
            foreach (string guid in guids)
            {
                string srcPath = AssetDatabase.GUIDToAssetPath(guid);
                Material src = AssetDatabase.LoadAssetAtPath<Material>(srcPath);
                if (src == null)
                {
                    continue;
                }

                string dstPath = $"{dstDir}/{Path.GetFileName(srcPath)}";
                Material dst = AssetDatabase.LoadAssetAtPath<Material>(dstPath);
                if (dst == null)
                {
                    if (AssetDatabase.CopyAsset(srcPath, dstPath))
                    {
                        restored++;
                    }
                    continue;
                }

                EditorUtility.CopySerialized(src, dst);
                EditorUtility.SetDirty(dst);
                restored++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        NprTextureChannelValidator.Validate();
        Debug.Log($"[NPR] 已恢复锁定参数，覆盖材质条目: {restored}。");
    }

    private static string ReadPropValue(Material mat, string prop)
    {
        Shader shader = mat.shader;
        int index = shader != null ? shader.FindPropertyIndex(prop) : -1;
        if (index >= 0)
        {
            var type = shader.GetPropertyType(index);
            if (type == UnityEngine.Rendering.ShaderPropertyType.Float ||
                type == UnityEngine.Rendering.ShaderPropertyType.Range)
            {
                return mat.GetFloat(prop).ToString("0.###");
            }
            if (type == UnityEngine.Rendering.ShaderPropertyType.Color)
            {
                Color c = mat.GetColor(prop);
                return $"({c.r:0.###}, {c.g:0.###}, {c.b:0.###}, {c.a:0.###})";
            }
            if (type == UnityEngine.Rendering.ShaderPropertyType.Vector)
            {
                Vector4 v = mat.GetVector(prop);
                return $"({v.x:0.###}, {v.y:0.###}, {v.z:0.###}, {v.w:0.###})";
            }
            if (type == UnityEngine.Rendering.ShaderPropertyType.Texture)
            {
                Texture t = mat.GetTexture(prop);
                return t != null ? t.name : "null";
            }
        }

        // Safe fallback for older Unity API/property edge cases.
        return mat.GetFloat(prop).ToString("0.###");
    }

    private static void WriteTextAsset(string assetPath, string content)
    {
        string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
        string dir = Path.GetDirectoryName(full);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(full, content, Encoding.UTF8);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}



