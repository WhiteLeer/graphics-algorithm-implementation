==========================================
SSR 实现说明（URP 版）
==========================================

目标：在 Func-3 提供一版可运行的 Screen Space Reflection
特点：通用反射（不限平面），成本高于 SSPR
文件：4个核心文件 + 1个步骤调试文件

------------------------------------------
文件列表
------------------------------------------

✅ SSR.shader - 核心SSR光线步进
✅ SSRRenderPass.cs - 渲染流程
✅ SSRRenderFeature.cs - Feature入口
✅ SSR_StepDebug.shader - 调试网格显示

------------------------------------------
Unity配置
------------------------------------------

1. URP设置
   - Depth Texture = True
   - Opaque Texture = True

2. 添加Render Feature
   - 打开你的Renderer Data
   - Add Renderer Feature -> SSR Render Feature

3. 初始参数建议
   - Intensity: 0.75
   - Max Steps: 48
   - Stride: 0.25
   - Thickness: 0.03
   - Max Distance: 12
   - Ray Start Bias: 0.01
   - Fresnel Power: 4
   - Fade Start/End: 0 / 0.12

------------------------------------------
算法流程
------------------------------------------

1. 从深度重建当前像素 worldPos
2. 用法线和视线方向得到反射方向 reflectDir
3. 沿反射方向在屏幕空间逐步步进（Ray Marching）
4. 每步投影到屏幕，采样深度，做厚度命中判断
5. 命中后采样反射颜色并做 Fresnel + Edge Fade 混合

------------------------------------------
SSPR 对比
------------------------------------------

SSR：
✅ 适用更一般的表面
❌ 每像素步进，成本更高
❌ 更易出现噪点/断裂/离屏缺失

SSPR：
✅ 平面场景成本低、稳定
❌ 强依赖平面假设

------------------------------------------
常见问题
------------------------------------------

Q: 没有反射
A: 检查 Depth/Opaque Texture 是否开启；增加 Intensity；增大 Max Steps

Q: 噪点和破碎明显
A: 增大 Thickness，减小 Stride，增加 Max Steps

Q: 性能偏高
A: 降低 Max Steps；增大 Stride；降低 Max Distance

------------------------------------------

