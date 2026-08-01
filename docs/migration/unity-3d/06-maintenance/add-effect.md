# 添加卡牌效果

> 状态：普通时间轴已注册 `Damage`、`Elevation`、`Recover`、`Built`、`Poison`；`Clear` 使用已验证的独立会话

## 最小修改面

1. 先在 `Runtime/Domain/CardDefinition.cs` 确认 `CardEffectKind` 和 payload 是否已经表达该效果。只有源 schema 无法表达时才扩展 typed 数据，并同步 `CardJsonAdapter` 的结构化 token 校验。
2. 在 Domain 增加或注册实际 handler；handler 只接收纯状态/动作和 `CardEffectResultBuffer`，产出 tile 或 occupant before/after 结果，不得引用 Unity、Prefab 或资源路径。`Supports(CardEffect)` 必须在占格前拒绝错误 payload。
3. 在 `CombatApplicationSession.TryGetRequiredTargetKind` 定义 entity/tile 目标策略。这里按 effect kind 分支是用例策略；禁止在 Controller/Presenter 按 stable ID 分支。
4. 在 `CardEffectRegistrationCatalog` 注册已真正支持的 kind。只更新枚举或 JSON 而没有 handler 时必须保持 `UnsupportedEffect`。
5. 让 Composition 注入所需 handler/状态；Presenter 只消费 session view 与结果。若现有通用颜色/标签足够，不新增卡牌专属 View 代码。

Clear 不走普通 handler/target/Resolve 步骤：在 `TimelineGrid.PreviewClear/TryClear` 增加纯 mask 规则，由 `TimelineClearSession` 管理 Preview/Commit/Cancel，再由 Application 暴露 `TimelineClearPreview/Result`。View 只能读取逐格状态与 `RemovedActions[].OccupiedCells`，不得扫描 UI label 推断占用或 action identity。

## 测试与调试

Domain EditMode 覆盖正常结果、边界、无效输入、确定性和失败无副作用；Application 测试覆盖目标类型、commit 前 fail-fast、resolve 顺序和 trace；Infrastructure 测试覆盖 token/payload 与注册；需要世界表现时增加 PlayMode 和实际截图。

Trace 的 `resolve-effect` 条目应包含 kind、before、after。逻辑错误在 handler/`TimelineGrid.Resolve` 断点定位；目标错误在 `TryGetRequiredTargetKind`；卡牌可见但不可执行先查 registration catalog；视觉不同步再查 Presenter/Controller 对快照的应用。

例如 `Elevation +2` 的验收不是只看数值：七个有效柱各新增两个独立 mesh/renderer/collider，块间 `0.32`，顶面与 occupant anchor 上移 `0.64`，范围和四向选择继续正确。

`Recover +100` 的已验证最小切片是：handler 精确重查 runtime ID + coordinate，只接受存在、支持生命且未满血的 occupant；HP=0 可恢复，结果钳制 `MaxHP`，满血/消失/不同 ID 替换/缺 range 均无副作用。Infrastructure 注册后真实卡立即沿公共 Scene 路径可用；`VerticalSliceController.cs` 不应产生差异。对应测试与截图位于 `04-verification/evidence/remaining-cards-gate-a/`。

`Built tower,1` 只在真实空 tile Resolve 时创建一个 Neutral HP100 occupant，结果为 `Before=null, After=tower`；`Poison +2` 只对仍存活且支持状态的 stable occupant 累加 stacks。两者共用 `CombatOccupantState` 和 `CardEffectResultBuffer`；Presentation 只消费 snapshot。Tower 生命周期和 Poison tick 不属于“添加效果”步骤，必须由回合/意图阶段另行实现。

Inspector 只应新增真实需要的 Prefab/Presenter 引用，不把 handler 做成场景对象。回滚按 adapter、Domain handler、Application target policy、registration、测试/表现的单一切片撤销；不得留下已注册但无实现的效果。

Clear 三态表现的已验证约定是：空格天蓝 `○`、命中绿色 `HIT`、越界红色 `!`；颜色外必须保留 marker/边框冗余。`TimelineCellView` 默认文字/颜色由 Scene authoring 显式保存，Clear/Cancel 后恢复，不依赖 `Awake/OnEnable` 执行顺序。

当前五个普通 handler 与一个独立 Clear session 已在 full EditMode `152/152`、full PlayMode `38/38`、build/Player 和 54 张 PNG 中共同回归；最终证据见 `04-verification/evidence/remaining-cards-gate-d/`，架构决策见 `02-architecture/adr/0007-occupant-effects-and-independent-clear-session.md`。

## 回合型效果

需要在回合阶段执行的效果不能放回卡牌 Resolve handler 或 Presenter。building behavior 登记到 `RunningBuildingBehaviors`；全局状态登记到 `ProcessingTurnStartStatuses`；enemy typed effect 由 intent handler 在 `ResolvingTimeline` 执行。三者都复用 `CombatTurnLifecycleCoordinator`，不得创建第二个计时器。

每个 handler 消费稳定 occupant snapshot，输出 typed before/after/removal result。死亡按 `LifecycleDeathPolicyResolver`，变更通过 `ILifecycleOccupantStore.TryApply` 原子提交。新增显示文案通过 `IActionDisplayCatalog`，frame/tooltip 只消费 snapshot。参考 ADR 0008 和 Tower/Poison 专属测试。
