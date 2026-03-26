═══════════════════════════════════════════════════════════════════════════════
                      通用后处理Debug系统 v2.0
═══════════════════════════════════════════════════════════════════════════════

🎯 核心特性

✓ 通用性 - 支持所有后处理效果（SSPR、SSAO、SSR等）
✓ 自动扫描 - 自动发现所有Render Features
✓ 统一管理 - 一个窗口管理所有效果
✓ Console监听 - 自动收集运行时日志
✓ AI集成 - 一键生成数据，Claude自动分析
✓ MCP桥接 - 支持Unity MCP多阶段截图流程（阶段切换 + 每阶段日志）

═══════════════════════════════════════════════════════════════════════════════

🚀 快速开始（3步）

1. 打开Debug窗口
   Window → Post Process Debug Center

2. 点击任意效果的 [📷 Capture Debug Data]
   • Play模式：捕获GameView（完整后处理效果）✓ 推荐
   • Editor模式：捕获SceneView（效果可能不完整）

3. 对Claude说："请分析Debug数据"

完成！Claude会自动读取并分析。

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

💡 捕获模式对比

Play模式（推荐）：
✓ 完整的后处理效果
✓ 运行时性能数据（FPS等）
✓ GameView截图
✓ Debug可视化正常工作

Editor模式：
• 无需运行游戏
• SceneView截图
⚠️ 后处理效果可能不完整
⚠️ 无法获取运行时性能数据
⚠️ Debug可视化可能无法正常显示

═══════════════════════════════════════════════════════════════════════════════

📁 系统文件结构

PostProcessDebugSystem/
├─ ConsoleLogger.cs                      ← Console日志收集（Play模式自动启动）
├─ DebugDataCapture.cs                   ← 运行时捕获器（Play模式）
├─ Editor/
│  ├─ PostProcessDebugWindow.cs          ← 统一Debug窗口 ⭐核心⭐
│  ├─ EditorConsoleLogger.cs             ← Editor模式日志收集（自动启动）
│  ├─ EditorDebugCapture.cs              ← Editor模式捕获器
│  ├─ McpStageCaptureBridge.cs           ← MCP多阶段桥接（请求/响应 + 日志）
│  └─ DebugDataAnalyzer.cs               ← 数据分析工具
└─ README.txt                            ← 本文件

═══════════════════════════════════════════════════════════════════════════════

🔌 Unity MCP 多阶段截图流程（推荐）

目标：
• 截图由 Unity MCP `manage_camera.screenshot` 负责（更稳定）
• 阶段切换/参数快照/每阶段日志由 `McpStageCaptureBridge` 负责

请求文件（由外部工具写入）：
Assets/DebugCaptures/.mcp_stage_request.json

响应文件（Unity写回）：
Assets/DebugCaptures/.mcp_stage_response.json

支持命令：
1. `begin_session`
2. `set_stage`
3. `end_session`

示例请求（set_stage）：
{
  "command": "set_stage",
  "featureType": "SSRRenderFeature",
  "sessionId": "SSR_20260326_A",
  "stageLabel": "Step1",
  "runtimeDebugStep": 16,
  "enableDebugVisualization": true,
  "exportConsole": true
}

产物目录：
Assets/DebugCaptures/MCP/<sessionId>/
• `MCP_Capture_Report.txt`（阶段记录 + 参数快照）
• `*_Console.txt`（每阶段日志）
• 截图文件（由MCP截图指令保存）

推荐循环：
1. 写入 `begin_session`
2. 依次写入多个 `set_stage`
3. 每次 `set_stage` 成功后调用 `manage_camera.screenshot`
4. 写入 `end_session`

═══════════════════════════════════════════════════════════════════════════════

🎨 Debug窗口功能

[Window → Post Process Debug Center]

┌────────────────────────────────────────┐
│  🛠️ Post Process Debug Center          │
├────────────────────────────────────────┤
│  🔄 刷新 | ☑ 自动刷新 | 📂 打开 | 🗑️ 清理 │
├────────────────────────────────────────┤
│  找到 N 个后处理效果：                  │
│                                        │
│  ┌──────────────────────────────────┐ │
│  │ 📦 SSPR              ✓ Debug可视化│ │
│  │ 资产: ...Renderer.asset           │ │
│  │ [📷 Capture Debug Data]  [→ 选择]│ │
│  └──────────────────────────────────┘ │
│                                        │
│  ┌──────────────────────────────────┐ │
│  │ 📦 SSAO                           │ │
│  │ 资产: ...Renderer.asset           │ │
│  │ [📷 Capture Debug Data]  [→ 选择]│ │
│  └──────────────────────────────────┘ │
│                                        │
├────────────────────────────────────────┤
│  📊 状态: 运行中 ✓                     │
│  Console: 0 错误 | 2 警告 | 15 日志   │
└────────────────────────────────────────┘

功能说明：
• 自动刷新：每2秒自动扫描Render Features
• [📷 Capture]: 捕获截图+日志+报告
• [→ 选择]: 在Inspector中选中该Feature
• [📂 打开]: 打开DebugCaptures文件夹
• [🗑️ 清理]: 删除所有旧Debug文件

═══════════════════════════════════════════════════════════════════════════════

📊 生成的数据

每次捕获生成2个文件（已整合，无冗余）：

Assets/DebugCaptures/
├─ SSPR_20260316_143025.png              ← 统一可视化面板
└─ SSPR_20260316_143025_Report.txt       ← 完整报告（含Console日志）

可视化面板布局：
┌─────────────────────────────────────────┐
│ [GameView]           │ [2x2 Debug步骤]  │
│                      │ ┌──────┬──────┐ │
│ 实时渲染结果         │ │Step1 │Step2 │ │
│                      │ ├──────┼──────┤ │
│                      │ │Step3 │Step4 │ │
│                      │ └──────┴──────┘ │
├─────────────────────────────────────────┤
│ [关键参数] Camera、Settings、FPS...    │
└─────────────────────────────────────────┘

完整报告内容：
• 系统信息（GPU、Unity版本、平台）
• 屏幕信息（分辨率、DPI）
• 相机信息（位置、FOV、裁剪面）
• Console统计（错误/警告/日志数量）
• 性能信息（FPS、帧时间）
• 场景对象（测试物体的详细信息）
• Feature配置（所有参数）
• Console日志（最近100条，完整记录）

═══════════════════════════════════════════════════════════════════════════════

🤖 AI自动分析

传统方式（麻烦）：
  您: "有个Bug..."
  Claude: "能截图看看吗？"
  您: "这是截图"
  Claude: "能看看Console吗？"
  您: "这是Console"
  Claude: "能再截个XX图吗？"
  ...（10+轮）

整合后（极简）：
  您: "请分析Debug数据"
  Claude: [读取统一面板.png]
         [读取完整报告.txt]
         [一次性获取所有信息]
         [精确定位问题]
         [自动修复代码]
  完成！（1轮）

效率提升：10x+
文件数量：减少50%（4个→2个）
信息密度：提升200%（一张图包含所有可视化）

═══════════════════════════════════════════════════════════════════════════════

💡 为新效果启用Debug可视化（可选）

如果你的后处理效果支持Debug可视化（如SSPR的3x2网格），
在Settings类中添加：

    [System.Serializable]
    public class Settings
    {
        public bool enableDebugVisualization = false;  // ← 就这一行
        // ... 其他参数
    }

系统会自动识别并：
• 在窗口中显示"✓ Debug可视化"标签
• 捕获时自动启用/恢复

不添加也完全没问题，系统正常捕获截图。

═══════════════════════════════════════════════════════════════════════════════

❓ 常见问题

Q: 窗口中看不到任何效果？
A: 确保：
   1. URP项目（不是Built-in RP）
   2. Renderer Asset中添加了Render Features
   3. 点击"🔄 刷新"按钮

Q: 捕获按钮灰色无法点击？
A: 必须进入Play Mode才能捕获。

Q: Console日志收集器没启动？
A: 系统在游戏启动时自动初始化，无需手动操作。

Q: Claude说找不到文件？
A: 确保文件在：C:\Users\wepie\My project\Assets\DebugCaptures\
   然后说："请分析Debug数据"（无需提供路径）

Q: 如何知道是否有新的Debug数据？
A: 查看DebugCaptures文件夹中最新的文件日期
   或在Debug Center窗口中点击 [📂 打开文件夹]

═══════════════════════════════════════════════════════════════════════════════

📈 系统演进

v1.0（分散式）：
  ❌ 需要为每个效果手动创建Editor
  ❌ 文件混乱（通用+专用混在一起）
  ❌ 多个独立按钮（分散）
  ❌ 生成4个文件（截图、步骤、日志、报告）

v2.0（统一式）：
  ✅ 自动扫描所有效果
  ✅ 清晰的文件结构（独立文件夹）
  ✅ 统一管理窗口（集中）
  ✅ 无需任何手动配置
  ❌ 仍然生成4个文件（信息冗余）

v2.1（整合式 - 当前版本）：
  ✅ 所有v2.0的优势
  ✅ 统一可视化面板（一图看全）
  ✅ 完整报告（含Console日志）
  ✅ 只生成2个文件（无冗余）
  ✅ 关键参数自动标注
  ✅ AI一次性读取完毕

═══════════════════════════════════════════════════════════════════════════════

🎉 立即体验

1. Window → Post Process Debug Center
2. 运行游戏
3. 点击 [📷 Capture Debug Data]
4. 对Claude说："请分析Debug数据"

就这么简单！

═══════════════════════════════════════════════════════════════════════════════
