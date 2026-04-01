using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class NprHoudiniObjExporter
{
    private const string OutDir = "Assets/MaterialFX/NPR/NPR-Core/Meshes/HoudiniInput";

    [MenuItem("Tools/NPR 风味/Houdini/导出 SkinnedMesh 为 OBJ(全场景)")]
    public static void ExportAllSkinnedMeshToObj()
    {
        EnsureFolder("Assets/MaterialFX/NPR/NPR-Core/Meshes", "HoudiniInput");

        SkinnedMeshRenderer[] smrs = Object.FindObjectsOfType<SkinnedMeshRenderer>(true);
        int count = 0;
        for (int i = 0; i < smrs.Length; i++)
        {
            SkinnedMeshRenderer smr = smrs[i];
            if (smr == null || smr.sharedMesh == null)
            {
                continue;
            }

            string fileName = SanitizeName(smr.gameObject.name) + ".obj";
            string assetPath = OutDir + "/" + fileName;
            string absPath = Path.GetFullPath(assetPath);
            WriteMeshObj(smr.sharedMesh, absPath);
            count++;
        }

        AssetDatabase.Refresh();
        Debug.Log("[NPR][Houdini] OBJ 导出完成: " + count + " 个 -> " + OutDir);
    }

    private static void WriteMeshObj(Mesh mesh, string path)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;

        var sb = new StringBuilder(1024 * 16);
        sb.AppendLine("# Exported by NprHoudiniObjExporter");
        sb.AppendLine("o " + mesh.name);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            sb.AppendLine($"v {v.x} {v.y} {v.z}");
        }

        if (normals != null && normals.Length == vertices.Length)
        {
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 n = normals[i];
                sb.AppendLine($"vn {n.x} {n.y} {n.z}");
            }
        }

        if (uvs != null && uvs.Length == vertices.Length)
        {
            for (int i = 0; i < uvs.Length; i++)
            {
                Vector2 uv = uvs[i];
                sb.AppendLine($"vt {uv.x} {uv.y}");
            }
        }

        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            int[] tris = mesh.GetTriangles(sub);
            for (int i = 0; i < tris.Length; i += 3)
            {
                int a = tris[i] + 1;
                int b = tris[i + 1] + 1;
                int c = tris[i + 2] + 1;
                sb.AppendLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
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

