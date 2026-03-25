==========================================
SSPR 实现说明（PPR 路线）
==========================================

基于：SIGGRAPH 2017 PPR / SSPR 思路（projection hash）
参考：Optimized pixel-projected reflections for planar reflectors
文件：5个核心文件

------------------------------------------
文件列表
------------------------------------------

✅ SSPR.compute - Projection pass（写入中间缓冲）
✅ SSPR.shader - Resolve pass + 反射混合
✅ SSPRRenderPass.cs - 渲染流程与参数传递
✅ SSPRRenderFeature.cs - Feature入口
✅ SSPR_StepDebug.shader - 步骤调试可视化

------------------------------------------
Unity配置
------------------------------------------

1. 确保URP设置
   Edit → Project Settings → Quality
   → 打开你的URP Asset
   → Depth Texture = True
   → Opaque Texture = True

2. 添加Render Feature
   URP Renderer Data
   → Add Renderer Feature
   → 选择 "SSPR Render Feature"

3. 建议初始参数
   Plane Normal: (0,1,0)
   Plane Distance: 0
   Intensity: 1.0
   Fresnel Power: 1.0~2.0
   Normal Threshold: 0.5
   Max Plane Distance: 0
   Occlusion Thickness: 0
   Depth Consistency: 0.001
   Enable Hole Fill: False（先关，定位问题时更直观）

------------------------------------------
核心技术（当前实现）
------------------------------------------

1. Projection pass（Compute，数据散射）
   - 从深度重建 worldPos
   - 对 worldPos 做平面镜像，得到 mirroredWorldPos
   - 将 mirroredWorldPos 投影到屏幕 targetPixel
   - 向中间缓冲写入 source pixel（offset映射）
   - 通过原子操作保证并发写稳定

2. Resolve pass（Pixel，数据收集）
   - 当前像素读取 _SSPR_Offset（可选洞填充）
   - 解码得到 source pixel，采样 _BaseMap 反射色
   - 基于法线阈值、平面距离、深度一致性做过滤
   - Fresnel + EdgeFade 计算权重并混合回场景

3. 辅助缓冲
   - _SSPR_Offset: R32_UInt，存 source pixel 编码
   - _SSPR_Depth:  R32_UInt，存投影命中深度（用于一致性/遮挡）

------------------------------------------
渲染流程
------------------------------------------

Step A: 复制相机颜色到 _BaseMap
Step B: Compute Clear（清空中间缓冲）
Step C: Compute Project（构建 receiver→source 映射）
Step D: 全屏Resolve（采样反射 + 混合）
Step E: 输出回相机颜色

------------------------------------------
调试模式
------------------------------------------

Debug步骤（推荐）：
1. SSPR_OffsetValid
2. SSPR_OffsetCoverage
3. SSPR_SampledColor
4. WorldPosition

判定标准：
- OffsetValid 出现稳定有效区（非全黑/非全红）
- OffsetCoverage 与反射区域几何关系一致
- SampledColor 有可解释的反射内容
- 最终画面反射在平面下方且随视角连续变化

------------------------------------------
SSPR 与 SSR 对比
------------------------------------------

SSPR（本实现）：
✅ 只针对平面，约束强，成本低
✅ 无长距离ray marching，易做高分辨率
✅ 在水面/地板这类场景性价比高
❌ 仅适合平面，泛化能力弱

SSR（传统）：
✅ 可用于更一般反射场景
❌ 依赖ray marching，步数越高成本越高
❌ 离屏缺失、噪点和断裂处理成本高

------------------------------------------
常见问题
------------------------------------------

Q: 完全无反射
A: 1. 检查Depth/Opaque Texture
   2. 看OffsetValid是否全无效
   3. 先把OcclusionThickness=0, DepthConsistency=0.001
   4. 检查planeNormal/planeDistance是否匹配场景

Q: 反射过浅
A: 提高 Intensity，降低 Fresnel Power

Q: 边缘破洞明显
A: 开启 Hole Fill，并适当增加 HoleFillRadius

Q: 闪烁/跳动
A: 先收紧反射区域（ReflectorBounds），再微调阈值

------------------------------------------
