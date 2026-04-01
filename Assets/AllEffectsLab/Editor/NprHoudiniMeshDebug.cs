using UnityEditor;
using UnityEngine;

public static class NprHoudiniMeshDebug
{
    [MenuItem("Tools/NPR 风味/调试/打印 HoudiniBaked Mesh 名")]
    public static void DumpMeshes()
    {
        string root = "Assets/MaterialFX/NPR/NPR-Core/Meshes/HoudiniBaked";
        string[] guids = AssetDatabase.FindAssets("t:Mesh", new[] { root });
        Debug.Log("[NPR][Debug] HoudiniBaked Mesh Count: " + guids.Length);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            Debug.Log("[NPR][Debug] Mesh: " + (mesh != null ? mesh.name : "<null>") + " | Path: " + path);
        }
    }
}

