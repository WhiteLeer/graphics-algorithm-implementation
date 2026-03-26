Func-4 Bloom 使用说明

1. 在 Renderer Data 中 Add Renderer Feature -> BloomRenderFeature。
2. 该 Feature 会自动使用 Shader: Hidden/Func4/Bloom。
3. 推荐初始参数：
   - Intensity: 1.0
   - Threshold: 1.0
   - Soft Knee: 0.5
   - Clamp Value: 10.0
   - Iterations: 4
   - Tint: White
4. 如果画面过糊：降低 Iterations 或 Intensity。
5. 如果亮部不够明显：降低 Threshold 或提高 Intensity。
