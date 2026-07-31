# Wave 02B Agent 02：卡牌交互 Domain

> 状态：已生成，待接手主智能体审查后启动
> 负责人：Wave 02B Agent 02
> 最后验证日期：2026-07-31
> 证据来源：玩法等价契约、TimelineGrid、Godot CustomCard/DragShape 行为

## 角色与单一目标

在无 UnityEngine 依赖的 Domain 中建立 `lighting` 出牌会话与非变异时间轴预览，使“选卡 -> 选目标 -> 预览位置 -> 确认/取消”成为可测试状态机，并保证最终放置仍复用 TimelineGrid 的唯一合法性规则。你不是仓库唯一工作者，不得回退他人改动。

## 必读与能力

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_COMBAT_PROMPT.md`
- `docs/migration/unity-3d/01-assessment/gameplay-parity-contract.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- `unity/Assets/_Project/Runtime/Domain/*.cs`
- `unity/Assets/_Project/Tests/EditMode/TimelineGridTests.cs`
- `scene/card/custom_card.gd`
- `scene/in_scene/drag_modules/rules/DragPlacementQueryService.gd`

使用 `codebase-migrate` 保持规则等价，使用 Verification/QA 类 skill 设计失败路径测试。不要使用 Godot 实现 skill 编写 Unity C#。

## 独占写入范围

- `unity/Assets/_Project/Runtime/Domain/**`
- `unity/Assets/_Project/Tests/EditMode/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b-agent-02-card-interaction-domain.md`

Agent 01 不写这些路径，因此 Wave A 可并行。禁止修改 Infrastructure、Presentation、scene、asmdef、Editor harness、ProjectSettings、共享文档、Godot 文件和其他 Agent 报告。

## 冻结 API 语义

主智能体允许具体命名在审查时微调，但以下语义必须存在：

```text
TimelineGrid.CanPlace(TimelineAction) -> bool       # 不改变 occupied/action 集合
CardPlaySession(card)
SelectTarget(targetId, HexCoord) -> transition result
PreviewTimeline(TimelineGrid, TimelineCell) -> valid/invalid result
Commit(TimelineGrid) -> success/failure             # 仅这里最终占格
Cancel() -> idle/cancelled snapshot                 # 不改变 grid
```

- `CanPlace` 与 `TryPlace` 必须调用同一内部校验，禁止复制两套边界/冲突算法。
- 会话状态至少区分 Idle/TargetSelected/TimelinePreview/Committed/Cancelled；非法顺序返回显式结果，不抛含糊空引用。
- 目标保存稳定字符串 ID 与 `HexCoord`，不保存 Unity GameObject。
- Preview 必须无副作用；重复 Preview、Cancel 和失败 Commit 都不改变 `OccupiedCellCount`。
- Commit 使用 `TimelineAction.FromCard`，保留 `lighting`、damage 100、shape 单格与目标 ID。

## 实现、测试与非目标

1. 先写 EditMode 测试：合法路径、无目标预览、越界、冲突、重复 commit、cancel 后 commit、preview 无副作用。
2. 最小修改现有 Domain；不为未来七卡建立推测性框架。
3. 保持现有 19 项 EditMode 全部通过；新增测试数量和名称写入报告。
4. 运行 Domain/EditMode 可用门禁和 `git diff --check`。

非目标：UI、输入、JSON、其他卡牌效果、敌人 AI、撤销历史、网络同步。若需要 UnityEngine、修改 asmdef 或无法在现有 TimelineGrid 语义下实现，应停止越权写入并报告契约问题。

## Git 与完成回报

不得暂存、commit、push、切分支、merge、stash 或回退他人改动。回报格式：API 差异、测试计数、行为证据、失败项、兼容风险、主智能体集成建议。
