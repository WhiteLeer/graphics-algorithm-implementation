==========================================
SSAO实现说明
==========================================

基于：Unity URP 12.1.7官方实现
算法：Morgan 2011 - Alchemy Screen-Space Ambient Obscurance
文件：只有3个核心文件

------------------------------------------
文件列表
------------------------------------------

✅ SSAO.shader - 完整SSAO算法（5个Pass）
✅ SSAORenderPass.cs - 渲染逻辑
✅ SSAORenderFeature.cs - Feature入口

✅ SSAO_Debug.shader - 调试用
❌ 其他文件已删除

------------------------------------------
Unity配置
------------------------------------------

1. 确保URP设置
   Edit → Project Settings → Quality
   → 打开你的URP Asset
   → Depth Texture = True ✅

2. 添加Render Feature
   URP-HighFidelity-Renderer
   → Add Renderer Feature
   → 选择"SSAO Render Feature"

3. 参数设置（Unity官方默认值）
   Intensity: 3.0
   Radius: 0.035
   Sample Count: 12
   Enable Blur: True
   Render Pass Event: After Rendering Opaques

4. 运行测试

------------------------------------------
核心技术
------------------------------------------

1. View Space重建（Unity方法）
   - 使用frustum corner插值
   - viewPos = topLeftCorner + xExtent*u + yExtent*v
   - 乘以深度缩放因子

2. Morgan 2011 Alchemy AO公式
   - ao = Σ(max(dot(v,n) - β*d, 0) / (|v|² + ε))
   - β = 0.002（自阴影抑制）
   - ε = 0.0001（防止除零）
   - kContrast = 0.6（对比度）

3. 半球采样
   - 使用Unity的随机数表
   - InterleavedGradientNoise去除图案
   - 球面坐标生成采样点

4. 双边模糊
   - 5-tap高斯核（Unity权重）
   - 法线感知（保留边缘）
   - 水平+垂直两次模糊

------------------------------------------
渲染流程
------------------------------------------

Pass 0: AO计算
  输入：深度 + 法线
  输出：RT1（ARGB32：R=AO, GBA=Normal）

Pass 1: 水平模糊
  输入：RT1
  输出：RT2（双边滤波，水平方向）

Pass 2: 垂直模糊
  输入：RT2
  输出：RT3（双边滤波，垂直方向）

Pass 3: 最终输出
  输入：RT3
  输出：Final（R8：只保留AO通道，反转）

Pass 4: 应用到场景
  输入：相机颜色 + Final
  输出：相机颜色（乘法混合）

------------------------------------------
参数说明
------------------------------------------

Intensity（强度）: 0.5 - 4.0
  Unity默认：3.0
  调高：AO更明显，但可能过暗
  调低：AO更微妙

Radius（半径）: 0.01 - 1.0
  Unity默认：0.035
  调高：影响范围更广（大物体）
  调低：更局部（小细节）

Sample Count（采样数）: 4 - 32
  Unity默认：12
  4：快速但有噪点
  12：质量与性能平衡
  24+：最高质量但慢

Enable Blur（模糊）:
  Unity默认：True
  必须开启，否则噪点严重

------------------------------------------
调试模式
------------------------------------------

Debug Mode选项：
  None: 正常SSAO
  UV: 显示UV坐标
  Depth: 显示原始深度
  Linear Depth: 显示线性深度
  Normal: 显示法线
  World Pos: 显示世界坐标

如果效果不对，用Debug Mode逐步检查：
1. UV正常吗？
2. Depth有值吗？
3. Normal彩色吗？
4. 如果都正常，问题在AO计算

------------------------------------------
Frame Debugger
------------------------------------------

RT1/RT2/RT3显示暗红色是正常的！
  原因：ARGB32格式，R通道存AO
  重点：看红色有没有明暗变化

如果纯红色无变化：
  → AO计算失败
  → 检查深度纹理

如果有明暗变化：
  → AO计算正常
  → 检查应用Pass

------------------------------------------
性能
------------------------------------------

1080p, SampleCount=12:
  - AO计算：2-3ms
  - 双边模糊：0.5ms
  - 总计：3-4ms

优化建议：
  - 降低SampleCount到8
  - 关闭模糊（不推荐）

------------------------------------------
与Unity官方对比
------------------------------------------

我们的实现：
  ✅ 核心算法一致
  ✅ 代码清晰易懂
  ✅ 质量接近官方
  ❌ 缺少降采样优化
  ❌ 缺少法线重建
  ❌ 不支持XR/VR

Unity官方：
  ✅ 完整功能
  ✅ 高度优化
  ❌ 代码复杂

建议：
  - 学习用我们的
  - 生产用Unity官方

------------------------------------------
常见问题
------------------------------------------

Q: 完全没有效果
A: 1. 检查Depth Texture是否开启
   2. 检查Render Pass Event时机
   3. 用Debug Mode检查深度法线

Q: 效果太弱
A: 增大Intensity到3.0 - 4.0

Q: 效果太强/过暗
A: 降低Intensity到1.0 - 2.0

Q: 有噪点
A: 1. 确保Enable Blur = True
   2. 增加Sample Count

Q: RT显示红色
A: 正常！ARGB32格式的R通道

------------------------------------------
