using UnityEditor;
using UnityEngine;

public static class NprMaskMapGenerator
{
    private const string SrcRoot = "Assets/MaterialFX/NPR/NPR-Core/Textures/SourceMaps";
    private const string DstRoot = "Assets/MaterialFX/NPR/NPR-Core/Textures/StyleMaps";
    private const string TexturePrefix = "TEX_NPR3";

    [MenuItem("Tools/NPR 风格/生成三风格 Mask(FaceShadow/OutlineWidth)")]
    public static void GenerateAllMasks()
    {
        GenerateRole("Face");
        GenerateRole("Body");
        GenerateRole("Hair");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NPR][Mask] 三风格 Mask 生成完成。");
    }

    private static void GenerateRole(string role)
    {
        Texture2D srcFace = LoadReadable($"{SrcRoot}/{TexturePrefix}_{role}_FaceShadow.png");
        Texture2D srcOutline = LoadReadable($"{SrcRoot}/{TexturePrefix}_{role}_OutlineWidth.png");
        Texture2D srcLight = LoadReadable($"{SrcRoot}/{TexturePrefix}_{role}_LightMap.png");
        if (srcFace == null || srcOutline == null || srcLight == null)
        {
            Debug.LogWarning("[NPR][Mask] 缺源贴图: " + role);
            return;
        }

        GenerateStyle(role, "Genshin", srcFace, srcOutline, srcLight, 0.90f, 0.85f, 0.30f);
        GenerateStyle(role, "HSR", srcFace, srcOutline, srcLight, 0.78f, 0.72f, 0.20f);
        GenerateStyle(role, "ZZZ", srcFace, srcOutline, srcLight, 1.05f, 1.10f, 0.45f);
    }

    private static void GenerateStyle(string role, string style, Texture2D srcFace, Texture2D srcOutline, Texture2D srcLight, float faceContrast, float outlineContrast, float edgeBoost)
    {
        int w = srcFace.width;
        int h = srcFace.height;
        Color[] faceSrc = srcFace.GetPixels();
        Color[] outlineSrc = srcOutline.GetPixels();
        Color[] lightSrc = srcLight.GetPixels();

        Color[] outFace = new Color[faceSrc.Length];
        Color[] outOutline = new Color[outlineSrc.Length];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float f = faceSrc[i].grayscale;
                float o = outlineSrc[i].grayscale;
                float l = lightSrc[i].grayscale;

                float grad = Sobel(l, lightSrc, x, y, w, h);

                // FaceShadow: preserve facial area soft bands, style-specific contrast.
                float face = Mathf.Pow(Mathf.Clamp01((f - 0.5f) * faceContrast + 0.5f), 1.0f + edgeBoost * 0.25f);
                face = Mathf.Clamp01(face + grad * edgeBoost * 0.12f);

                // OutlineWidth: narrow interior, stronger silhouette boundaries.
                float outline = Mathf.Clamp01((o - 0.5f) * outlineContrast + 0.5f);
                outline = Mathf.Clamp01(outline + grad * edgeBoost * 0.30f);
                outline = Mathf.Lerp(outline, Mathf.Clamp01(outline * 0.85f), role == "Face" ? 0.0f : 0.25f);

                if (role == "Face")
                {
                    // Keep center-face cleaner, emphasize contour around cheeks/jawline.
                    float nx = (x + 0.5f) / w;
                    float ny = (y + 0.5f) / h;
                    float centerMask = 1.0f - Mathf.SmoothStep(0.08f, 0.42f, Mathf.Abs(nx - 0.5f));
                    float lowerFace = Mathf.SmoothStep(0.35f, 0.90f, ny);
                    float cheekBand = centerMask * lowerFace;

                    face = Mathf.Lerp(face, Mathf.Clamp01(face * 0.88f + (1.0f - cheekBand) * 0.10f), 0.55f);
                    outline = Mathf.Lerp(outline, Mathf.Clamp01(outline * 0.75f + (1.0f - centerMask) * 0.22f), 0.60f);
                }
                else if (role == "Hair")
                {
                    // Hair benefits from stronger strand boundary response.
                    outline = Mathf.Clamp01(outline + grad * 0.18f);
                }

                outFace[i] = new Color(face, face, face, 1f);
                outOutline[i] = new Color(outline, outline, outline, 1f);
            }
        }

        SaveGray($"{DstRoot}/{style}/{TexturePrefix}_{role}_FaceShadow.png", w, h, outFace);
        SaveGray($"{DstRoot}/{style}/{TexturePrefix}_{role}_OutlineWidth.png", w, h, outOutline);
    }

    private static float Sobel(float centerL, Color[] src, int x, int y, int w, int h)
    {
        int x0 = Mathf.Max(0, x - 1);
        int x1 = x;
        int x2 = Mathf.Min(w - 1, x + 1);
        int y0 = Mathf.Max(0, y - 1);
        int y1 = y;
        int y2 = Mathf.Min(h - 1, y + 1);

        float l00 = src[y0 * w + x0].grayscale;
        float l10 = src[y0 * w + x1].grayscale;
        float l20 = src[y0 * w + x2].grayscale;
        float l01 = src[y1 * w + x0].grayscale;
        float l21 = src[y1 * w + x2].grayscale;
        float l02 = src[y2 * w + x0].grayscale;
        float l12 = src[y2 * w + x1].grayscale;
        float l22 = src[y2 * w + x2].grayscale;

        float gx = -l00 - 2f * l01 - l02 + l20 + 2f * l21 + l22;
        float gy = -l00 - 2f * l10 - l20 + l02 + 2f * l12 + l22;
        float g = Mathf.Sqrt(gx * gx + gy * gy);
        return Mathf.Clamp01(g * 0.5f + Mathf.Abs(centerL - l10) * 0.5f);
    }

    private static void SaveGray(string path, int w, int h, Color[] pixels)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(path), png);
        FixImporter(path);
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


