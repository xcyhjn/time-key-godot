# Wave 02B Agent 04：棋盘目标与时间轴预览

> 状态：已生成；仅在 Wave A 审查通过后启动
> 负责人：Wave 02B Agent 04
> 最后验证日期：2026-07-31
> 证据来源：HexCoord 契约、ADR-0002、Godot Drag/Timeline 预览

## 角色与单一目标

实现可复用的 3D 棋盘作用范围预览与 12×3 时间轴合法性预览组件：输入纯坐标/状态，输出一致的有效、无效和选中视觉；不自行解析卡牌或提交行动。你不是仓库唯一工作者，不得回退他人改动。

## 启动前置与必读

只有主智能体确认 Agent 02 的 `CanPlace`/CardPlaySession API 已交回并冻结后才能开始。

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_COMBAT_PROMPT.md`
- `docs/migration/unity-3d/02-architecture/adr/0002-combat-board-orbit-and-stacking.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- `scene/in_scene/hex_map_modules/presenters/TargetAoeHoverPresenter.gd`
- `scene/in_scene/drag_modules/presenters/DragTimelineGridPreviewPresenter.gd`
- `unity/Assets/_Project/Runtime/Presentation/BoardTileView.cs`（只读）
- `unity/Assets/_Project/Runtime/Presentation/BoardOrbitCameraController.cs`（只读）
- Agent 02 报告

使用 Verification/QA skill 验证四向选择与截图。不得调用 Blender、重做地块、引入新输入包或在表现层复制 Domain 放置规则。

## 独占写入范围

- `unity/Assets/_Project/Runtime/Presentation/Targeting/**`
- `unity/Assets/_Project/Tests/PlayMode/Targeting/**`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b-agent-04-board-target-preview.md`

禁止修改 Cards 目录、现有 Presentation 根文件、scene、asmdef、Editor harness、Domain、Infrastructure、素材、ProjectSettings、共享文档和其他 Agent 路径。

## 冻结输入/输出契约

- 棋盘预览输入：中心 `HexCoord` 与去重后的相对 `HexCoord` 集合；输出到已注册 tile view，不查找 Godot 式全局节点。
- `lighting` 固定范围：`(0,0),(1,0),(2,0)`；缺失地块被忽略但必须可观测，不生成幽灵 tile。
- 时间轴预览输入：origin、shape 与主智能体从 Domain `CanPlace` 获得的合法性；Presentation 只着色，不再判断边界/冲突。
- 清理预览必须幂等；镜头 yaw/pitch/distance 变化不改变预览坐标集合。
- 颜色至少区分 normal/hover/selected/range-valid/timeline-valid/timeline-invalid；通过 MaterialPropertyBlock 或等价实例安全方式，不能污染共享材质。

## 实现与验收

1. 组件公开显式 Register/Show/Clear API，便于 composition root 注入已有 BoardTileView 与时间轴 cell view。
2. PlayMode 覆盖范围投影、缺失坐标、重复 show/clear、有效/无效时间轴状态和材质隔离。
3. 测试 0/90/180/270 度时相同 HexCoord 集合保持高亮；不得用屏幕像素硬编码格子。
4. 捕获四向范围预览与至少一张时间轴 invalid 证据，实际检查高地真实堆叠上的高亮是否可辨识。
5. 不修改共享场景；由主智能体最终接入并捕获整体验收图。

非目标：卡牌 UI、提交动作、伤害/结算、路径寻路、完整敌人 AOE、地块模型或 shader 重制。必须改 BoardTileView/Controller 等共享文件时，停止越权修改并在报告给出最小 API 请求。

## Git 与完成回报

不得暂存、commit、push、切分支、merge、stash 或回退他人改动。回报格式：组件/API、测试计数、四向/invalid 截图、共享 API 请求、风险、主智能体接线说明。
