using UnityEditor;
using UnityEngine;

public static class NprPackedLightMapGenerator
{
    private const string SrcRoot = "Assets/MaterialFX/NPR/NPR-Core/Textures/Generated";
    private const string DstRoot = "Assets/MaterialFX/NPR/NPR-Core/Textures/Profiles";

    [MenuItem("Tools/NPR 风味/生成三风味 Packed LightMap")]
    public static void GenerateAll()
    {
        GenerateForRole("Face");
        GenerateForRole("Body");
        GenerateForRole("Hair");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NPR][Packed] 三风味 LightMap 生成完成。");
    }

    private static void GenerateForRole(string role)
    {
        string srcPath = $"{SrcRoot}/T_{role}_LightMap.png";
        Texture2D src = LoadReadable(srcPath);
        if (src == null)
        {
            Debug.LogWarning("[NPR][Packed] 找不到源贴图: " + srcPath);
            return;
        }

        WriteStyle(src, role, "Genshin", 0.72f, 1.35f, 0.58f);
        WriteStyle(src, role, "HSR",     0.78f, 1.55f, 0.46f);
        WriteStyle(src, role, "ZZZ",     0.62f, 1.15f, 0.70f);
    }

    private static void WriteStyle(Texture2D src, string role, string style, float aoPivot, float aoContrast, float specBoost)
    {
        int w = src.width;
        int h = src.height;
        Color[] inPixels = src.GetPixels();
        Color[] outPixels = new Color[inPixels.Length];

        for (int i = 0; i < inPixels.Length; i++)
        {
            Color c = inPixels[i];
            float lum = Mathf.Clamp01(c.grayscale);
            float ao = Mathf.Clamp01((lum - aoPivot) * aoContrast + aoPivot);
            float detail = Mathf.Clamp01(Mathf.Abs(c.r - c.g) + Mathf.Abs(c.g - c.b));
            float spec = Mathf.Clamp01(Mathf.Pow(1.0f - lum, 1.4f) * 0.85f + detail * 0.35f + specBoost * 0.08f);

            // materialId: body/face/hair split for slight per-region tuning in shader.
            float materialId = role == "Face" ? 0.15f : role == "Body" ? 0.45f : 0.80f;

            // Keep style-specific channel semantics aligned with shaders.
            Color packed = Color.black;
            if (style == "Genshin")
            {
                packed.r = spec;
                packed.g = ao;
                packed.b = spec * 0.9f;
                packed.a = materialId;
            }
            else if (style == "HSR")
            {
                packed.r = ao;
                packed.g = 0.0f;
                packed.b = spec;
                packed.a = materialId;
            }
            else // ZZZ
            {
                packed.r = spec;
                packed.g = ao;
                packed.b = Mathf.Clamp01(spec * 0.45f + (1.0f - ao) * 0.35f);
                packed.a = materialId;
            }

            outPixels[i] = packed;
        }

        Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        outTex.SetPixels(outPixels);
        outTex.Apply(false, false);

        string dstPath = $"{DstRoot}/{style}/T_{role}_LightMap.png";
        byte[] png = outTex.EncodeToPNG();
        System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(dstPath), png);
        Object.DestroyImmediate(outTex);

        FixImporter(dstPath);
    }

    private static Texture2D LoadReadable(string path)
    {
        FixImporter(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void FixImporter(string path)
    {
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        bool changed = false;
        if (ti.sRGBTexture) { ti.sRGBTexture = false; changed = true; }
        if (!ti.isReadable) { ti.isReadable = true; changed = true; }
        if (ti.textureCompression != TextureImporterCompression.Uncompressed)
        {
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }
        if (changed) ti.SaveAndReimport();
    }
}

