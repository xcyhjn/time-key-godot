# Wave 02B Agent 03：原版手牌 UI

> 状态：已生成；仅在 Wave A 审查通过后启动
> 负责人：Wave 02B Agent 03
> 最后验证日期：2026-07-31
> 证据来源：Godot 战斗截图、原卡面、Wave 02B1 Domain 契约

## 角色与单一目标

用 uGUI 实现可复用的底部 `lighting` 手牌视图：原卡面、稳定布局、悬停抬升/放大、选中、取消和拖动视觉状态，并通过事件向 composition root 发出意图。不得在 UI 中实现伤害、范围或时间轴合法性。你不是仓库唯一工作者，不得回退他人改动。

## 启动前置与必读

只有主智能体确认 Agent 01 素材与 Agent 02 Domain 已交回、实际 API 已冻结后才能开始。

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_COMBAT_PROMPT.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- `docs/migration/unity-3d/04-verification/test-plan.md`
- `docs/migration/unity-3d/04-verification/evidence/godot-baseline/battle-initial.png`
- `scene/card/custom_card.gd`
- `unity/Assets/_Project/Runtime/Presentation/VerticalSliceController.cs`（只读）
- Agent 01/02 报告

使用 Verification/QA skill 做实际渲染和交互验证；若有 Unity UI/design-system skill，可用于还原现有风格，但不得重新设计卡面。禁止 Blender、图像生成和第三方 UI 包。

## 独占写入范围

- `unity/Assets/_Project/Runtime/Presentation/Cards/**`
- `unity/Assets/_Project/Prefabs/Battle/Cards/**`
- `unity/Assets/_Project/Tests/PlayMode/Cards/**`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b-agent-03-card-hand-ui.md`

禁止修改 `VerticalSliceController.cs`、现有 Presentation 根文件、Targeting 目录、scene、asmdef、Editor harness、素材源、Domain、Infrastructure、ProjectSettings、共享文档和其他 Agent 路径。

## 冻结输入/输出契约

- 输入 view model：稳定卡牌 ID、Sprite、selected/interactable 状态。
- 输出事件：`CardSelected(stableId)`、`CardCancelRequested(stableId)`；拖动只输出屏幕位置/阶段，不自行 raycast 世界或时间轴。
- 同一时刻最多一张选中卡；取消后恢复原父级、位置、缩放与旋转。
- UI 必须消耗自己的 pointer 输入，不能泄漏到棋盘或轨道镜头。
- Idle 时右键归轨道镜头；Selected/Targeting/Scheduling 时卡牌取消优先，并由事件请求主 composition root 暂停/恢复 `BoardCamera.InputEnabled`。
- 卡面保持原始纵横比，使用完整 `lighting.png`，不把中文描述拆成另一个仿制面板。

## 实现与验收

1. 用容器/锚点组织底部手牌，不用只适配 1920×1080 的魔法绝对坐标。
2. 悬停抬升/放大应平滑且不导致布局永久重排；选中状态在鼠标移开后仍可辨识。
3. 右键或主智能体调用取消入口后，卡牌恢复且事件只触发一次。
4. PlayMode 覆盖 hover/selection/cancel、事件次数、pointer 拦截和重复 build 不复制监听器。
5. 捕获 1920×1080、1280×720 与 2560×1080 的 idle/hover/selected 视觉证据并实际检查裁切、变形和棋盘遮挡。
6. 不修改共享场景；用测试中构建的最小 Canvas/Prefab 验证，由主智能体最终接入场景。

非目标：世界目标范围、时间轴合法性、结算、完整扇形多卡算法、tooltip 关键词、其他六卡、音频/VFX。必须写共享文件或 Domain API 不足时停止越权修改并报告。

## Git 与完成回报

不得暂存、commit、push、切分支、merge、stash 或回退他人改动。回报格式：组件/事件、测试计数、两视口截图、未满足差异、风险、主智能体接线说明。
