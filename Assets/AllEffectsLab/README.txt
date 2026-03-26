All Effects Dynamic Test Scene

用途
- 一键生成一个用于测试 SSAO / SSPR / SSR / Bloom / DoF 的动态场景。
- 场景包含：
  - 反射地板与移动高亮物体（适合观察 SSR/SSPR）
  - 墙角/柱体/接触区域（适合观察 SSAO）
  - 发光球与移动点光（适合观察 Bloom）
  - 近中远三层物体 + 运动相机（适合观察 DoF）

使用方式
1. 打开 Unity。
2. 菜单点击：Tools/All Effects/Build Dynamic Test Scene
3. 生成后会自动打开：Assets/Scenes/AllEffects-DynamicLab.unity
4. 进入 Play。

相机控制
- 按住右键拖动：手动环绕
- 鼠标滚轮：缩放距离
- 不按右键时：相机自动环绕

注意
- 请确保当前使用的 URP Renderer 已启用你的 SSAO/SSPR/SSR/Bloom/DoF Render Feature。
- 若 DoF 依赖深度，请确保 Camera/Renderer 的 Depth Texture 开启（构建器已在相机上设置 requiresDepthTexture）。
