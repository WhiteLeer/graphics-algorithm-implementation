# NPR 工具目录说明

路径：`Assets/AllEffectsLab/Editor/NPR`

## 保留的核心工具
- `NprStyleSwitcherWindow.cs`
  - 作用：三套风格（原神/崩铁/绝区零）一键构建与切换（选中/全场景）。
- `NprPackedLightMapGenerator.cs`
  - 作用：生成三风格 `LightMap` 打包贴图。
- `NprMaskMapGenerator.cs`
  - 作用：生成三风格 `FaceShadow/OutlineWidth` 掩码贴图。
- `NprTextureChannelValidator.cs`
  - 作用：校验贴图通道规范与导入设置。
- `NprStyleLockManager.cs`
  - 作用：锁定当前三风格参数与一键恢复锁定版本。
- `NprOutlineNormalBaker.cs`
  - 作用：应用 Houdini 平滑法线到顶点色（描边流程）。

## 已清理的一次性工具
- `NprHoudiniMeshDebug.cs`
- `NprHoudiniObjExporter.cs`

## 菜单规范
- 菜单统一前缀：`Tools/NPR 风格/...`
- 菜单与文档术语统一为“风格”。

