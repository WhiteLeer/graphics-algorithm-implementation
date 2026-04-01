# NPR-Core 贴图与通道规范 (NPR-3)

本文档用于固定三套风味 (`Genshin/HSR/ZZZ`) 的贴图通道规范，避免材质迁移、贴图重烘焙后出现错通道问题。

## 1. 输入贴图约定
- `_BaseMap`: sRGB
- `_NormalMap`: Normal 导入
- `_RampMap`: sRGB
- `_LightMap`: 线性 (sRGB 关闭)
- `_FaceShadowMap`: 线性 (sRGB 关闭)
- `_OutlineWidthMap`: 线性 (sRGB 关闭)

## 2. 三风味 LightMap 严格通道语义
### Genshin (`Custom/NPR-3/GenshinURP`)
- `R`: Spec 参与 (与 B 取 max)
- `G`: AO
- `B`: Spec 参与 (与 R 取 max)
- `A`: MaterialId / 分区控制
- 规则表达: `AO = G`, `Spec = max(R, B)`, `ID = A`

### HSR (`Custom/NPR-3/HSRURP`)
- `R`: AO
- `G`: 预留
- `B`: Spec
- `A`: MaterialId / 分区控制
- 规则表达: `AO = R`, `Spec = B`, `ID = A`

### ZZZ (`Custom/NPR-3/ZZZURP`)
- `R`: Spec
- `G`: AO
- `B`: EdgeAccent (风格化边缘强化)
- `A`: MaterialId / 分区控制
- 规则表达: `AO = G`, `Spec = R`, `ID = A`

## 3. Face/Outline 掩码语义
- `_FaceShadowMap`: 脸部朝向阴影权重图，白 = 更受脸部阴影控制，黑 = 更少参与。
- `_OutlineWidthMap`: 描边宽度权重图，白 = 更粗，黑 = 更细。
- 三分区输出文件:
  - `T_Face_FaceShadow.png` / `T_Body_FaceShadow.png` / `T_Hair_FaceShadow.png`
  - `T_Face_OutlineWidth.png` / `T_Body_OutlineWidth.png` / `T_Hair_OutlineWidth.png`

## 4. 目录与资产分层
- 生成源: `Assets/MaterialFX/NPR/NPR-Core/Textures/Generated`
- 风味成品: `Assets/MaterialFX/NPR/NPR-Core/Textures/Profiles/{Genshin|HSR|ZZZ}`
- 当前风味材质: `Assets/MaterialFX/NPR/NPR-Core/Materials/Profiles/{Style}/CharacterConverted`
- 锁定参数材质: `Assets/MaterialFX/NPR/NPR-Core/Materials/LockedProfiles/{Style}/CharacterConverted`

## 5. 自动校验与触发点
- 手动校验: `Tools/NPR 风味/校验贴图通道规范`
- 切换风味时会自动触发一次校验。
- 恢复锁定参数时会自动触发一次校验。

校验项:
- 导入设置: `isReadable=true`, `sRGBTexture=false`, `Uncompressed`
- 通道有效性: 目标通道有足够动态范围，避免全黑/常量误贴图

## 6. 参数锁定流程
### 固化当前版本
- 菜单: `Tools/NPR 风味/锁定当前三风味参数(保存快照)`
- 输出:
  - 锁定材质快照到 `LockedProfiles`
  - 参数快照文档 `README_NPR3_LockedParams.md`

### 回滚到锁定版本
- 菜单: `Tools/NPR 风味/恢复锁定参数(覆盖当前)`
- 作用: 用 `LockedProfiles` 覆盖当前 `Profiles` 材质参数，保持风味稳定。

