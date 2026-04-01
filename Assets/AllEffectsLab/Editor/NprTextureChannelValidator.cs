using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class NprTextureChannelValidator
{
    private const string Root = "Assets/MaterialFX/NPR/NPR-Core/Textures/Profiles";

    [MenuItem("Tools/NPR 风味/校验贴图通道规范")]
    public static void Validate()
    {
        var rules = new Dictionary<string, ChannelRule>
        {
            { "Genshin", new ChannelRule("G", "max(R,B)", "A") },
            { "HSR", new ChannelRule("R", "B", "A") },
            { "ZZZ", new ChannelRule("G", "R", "A") }
        };

        int checkedCount = 0;
        foreach (var kv in rules)
        {
            string style = kv.Key;
            ChannelRule rule = kv.Value;
            string[] paths =
            {
                $"{Root}/{style}/T_Face_LightMap.png",
                $"{Root}/{style}/T_Body_LightMap.png",
                $"{Root}/{style}/T_Hair_LightMap.png"
            };

            foreach (string p in paths)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (tex == null)
                {
                    Debug.LogWarning("[NPR][Channel] 缺少贴图: " + p);
                    continue;
                }

                ValidateImporter(p);
                ValidateTextureChannels(tex, p, style, rule);
                checkedCount++;
            }
        }

        Debug.Log("[NPR][Channel] 校验完成，已检查贴图: " + checkedCount);
    }

    private static void ValidateImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        if (importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            changed = true;
        }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
            Debug.Log("[NPR][Channel] 已修正导入设置: " + path);
        }
    }

    private static void ValidateTextureChannels(Texture2D tex, string path, string style, ChannelRule rule)
    {
        Color32[] pixels;
        try
        {
            pixels = tex.GetPixels32();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[NPR][Channel] 无法读取贴图像素: " + path + " | " + ex.Message);
            return;
        }
        if (pixels == null || pixels.Length == 0)
        {
            Debug.LogWarning("[NPR][Channel] 贴图为空: " + path);
            return;
        }

        ChannelStat r = new ChannelStat();
        ChannelStat g = new ChannelStat();
        ChannelStat b = new ChannelStat();
        ChannelStat a = new ChannelStat();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            r.Push(c.r / 255f);
            g.Push(c.g / 255f);
            b.Push(c.b / 255f);
            a.Push(c.a / 255f);
        }

        bool aoValid = IsMeaningful(rule.AO, r, g, b);
        bool specValid = IsMeaningful(rule.Spec, r, g, b);
        bool idValid = IsMeaningful(rule.MaterialId, r, g, b, a);

        if (!aoValid || !specValid || !idValid)
        {
            Debug.LogWarning($"[NPR][Channel] {style} 贴图通道可能不符合约定: {path} | AO={rule.AO} Spec={rule.Spec} ID={rule.MaterialId}");
        }
        else
        {
            Debug.Log($"[NPR][Channel] {style} OK: {path}");
        }
    }

    private static bool IsMeaningful(string key, ChannelStat r, ChannelStat g, ChannelStat b)
    {
        if (key == "R") return r.Range > 0.02f;
        if (key == "G") return g.Range > 0.02f;
        if (key == "B") return b.Range > 0.02f;
        if (key == "max(R,B)") return Mathf.Max(r.Range, b.Range) > 0.02f;
        return true;
    }

    private static bool IsMeaningful(string key, ChannelStat r, ChannelStat g, ChannelStat b, ChannelStat a)
    {
        if (key == "A") return a.Range > 0.001f;
        return IsMeaningful(key, r, g, b);
    }

    private readonly struct ChannelRule
    {
        public readonly string AO;
        public readonly string Spec;
        public readonly string MaterialId;

        public ChannelRule(string ao, string spec, string materialId)
        {
            AO = ao;
            Spec = spec;
            MaterialId = materialId;
        }
    }

    private struct ChannelStat
    {
        public float Min;
        public float Max;
        private bool _inited;

        public float Range => Max - Min;

        public void Push(float v)
        {
            if (!_inited)
            {
                Min = v;
                Max = v;
                _inited = true;
                return;
            }

            if (v < Min) Min = v;
            if (v > Max) Max = v;
        }
    }
}
