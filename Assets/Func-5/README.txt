Func-5 DoF 使用说明

1. 在 Renderer Data 中 Add Renderer Feature -> DoFRenderFeature。
2. Shader 使用 Hidden/Func5/DoF。
3. 推荐起始参数：
   - Focal Distance: 8
   - Focal Range: 2.5
   - Max Blur Radius: 1.5
   - Intensity: 1.0
   - Near Blur Strength: 1.0
   - Far Blur Strength: 1.0
4. 调整建议：
   - 想让焦外更明显：提高 Intensity 或 Max Blur Radius。
   - 焦平面太窄：增大 Focal Range。
   - 背景虚化过重：降低 Far Blur Strength。
