# Wave 02B3R Interaction Agent Prompt

> 状态：Gate 0 后启动；只拥有新增交互仲裁代码与纯测试
> 负责人：effect-frame-interaction-agent
> 最后验证日期：2026-08-03

## 单一目标

建立 Presentation-only 的交互 owner 仲裁、`IdleTileInspect` typed 状态端口和右键短按/旋转阈值策略，覆盖选择退出与全状态清理契约。不得接线既有 Controller/Binding/Scene；主智能体串行接线。

## 必读资料

- `00-bootstrap/START_HERE_PROMPT.md`
- `00-bootstrap/NEXT_STAGE_EFFECT_FRAME_STABILITY_PROMPT.md`
- `03-workstreams/integration-contracts.md`
- `03-workstreams/agents/reports/turn-lifecycle-interaction-visual-audit.md`
- `unity/Assets/_Project/Runtime/Application/CombatApplicationModels.cs`
- `unity/Assets/_Project/Runtime/Application/SceneFlow/SceneFlowContracts.cs`

## 独占写入范围

- `unity/Assets/_Project/Runtime/Presentation/Interaction/**`（仅新增文件）
- `unity/Assets/_Project/Tests/EditMode/Interaction/**`（仅新增文件）
- `docs/migration/unity-3d/03-workstreams/agents/reports/effect-frame-interaction-agent.md`

## 禁止写入

不得修改既有 `VerticalSliceController.cs`、`BoardOrbitCameraController.cs`、`BoardTileView.cs`、`CombatPresentationBinding.cs`、任何正式 Scene/Prefab、asmdef、Build Settings、共享文档或历史证据。不得运行 Unity/Godot，不得 stage/commit/push、stash、切分支或回退他人改动。你不是仓库唯一工作者，必须适配其他在途改动。

## 冻结契约

- owner 优先级：`Resolving/Disabled > CardTargeting > Scheduling/Drag/Clear > IdleActionHover > None`，任意时刻只有一个 owner。
- `IdleTileInspect` 只保留最多一个 `HexCoord`，Set/Toggle/Clear 是唯一写入口；同格 toggle、另一格原子替换、空世界/Escape/短右键清除。
- `CardSelected` 清除 tile inspect 后接管；Committed/Resolving/Resolved/Disposed/GlobalInputLock 拒绝新的 inspect，并清所有表现 owner。
- 右键短按只有未越过明确像素阈值才取消 inspect；越阈值只代表相机旋转，不触发世界选择或取消。
- 所有清理 API 必须幂等并返回最终唯一 owner/inspect 状态。

## 实现与验证

1. 添加 Unity-free `OverlayPriorityCoordinator`、`IdleTileInspectState/Port`、`RightClickGesturePolicy`，不可引用 `UnityEngine`。
2. 为优先级抢占、同格 toggle、替换、空地、Escape、短右键/拖拽、锁定拒绝和全量清理写 NUnit EditMode 测试。
3. 运行仓库配置的纯 EditMode filter；Gate 0 测试必须先在当前代码上复现缺口，之后在本 Prompt 范围内变绿。

## 完成回报

报告列出改动文件、契约状态、测试命令/结果、任何失败与主集成所需的最小接线点。发现共享契约冲突立即停止写入并报告。
