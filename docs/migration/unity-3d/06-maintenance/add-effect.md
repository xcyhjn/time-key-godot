# 添加卡牌效果

> 状态：当前运行时已注册 `Damage`、`Elevation` 与 `Recover`；其他 typed schema 仍 fail-fast

## 最小修改面

1. 先在 `Runtime/Domain/CardDefinition.cs` 确认 `CardEffectKind` 和 payload 是否已经表达该效果。只有源 schema 无法表达时才扩展 typed 数据，并同步 `CardJsonAdapter` 的结构化 token 校验。
2. 在 Domain 增加或注册实际 handler；handler 只接收纯状态/动作和 `CardEffectResultBuffer`，产出 tile 或 occupant before/after 结果，不得引用 Unity、Prefab 或资源路径。`Supports(CardEffect)` 必须在占格前拒绝错误 payload。
3. 在 `CombatApplicationSession.TryGetRequiredTargetKind` 定义 entity/tile 目标策略。这里按 effect kind 分支是用例策略；禁止在 Controller/Presenter 按 stable ID 分支。
4. 在 `CardEffectRegistrationCatalog` 注册已真正支持的 kind。只更新枚举或 JSON 而没有 handler 时必须保持 `UnsupportedEffect`。
5. 让 Composition 注入所需 handler/状态；Presenter 只消费 session view 与结果。若现有通用颜色/标签足够，不新增卡牌专属 View 代码。

## 测试与调试

Domain EditMode 覆盖正常结果、边界、无效输入、确定性和失败无副作用；Application 测试覆盖目标类型、commit 前 fail-fast、resolve 顺序和 trace；Infrastructure 测试覆盖 token/payload 与注册；需要世界表现时增加 PlayMode 和实际截图。

Trace 的 `resolve-effect` 条目应包含 kind、before、after。逻辑错误在 handler/`TimelineGrid.Resolve` 断点定位；目标错误在 `TryGetRequiredTargetKind`；卡牌可见但不可执行先查 registration catalog；视觉不同步再查 Presenter/Controller 对快照的应用。

例如 `Elevation +2` 的验收不是只看数值：七个有效柱各新增两个独立 mesh/renderer/collider，块间 `0.32`，顶面与 occupant anchor 上移 `0.64`，范围和四向选择继续正确。

`Recover +100` 的已验证最小切片是：handler 精确重查 runtime ID + coordinate，只接受存在、支持生命且未满血的 occupant；HP=0 可恢复，结果钳制 `MaxHP`，满血/消失/不同 ID 替换/缺 range 均无副作用。Infrastructure 注册后真实卡立即沿公共 Scene 路径可用；`VerticalSliceController.cs` 不应产生差异。对应测试与截图位于 `04-verification/evidence/remaining-cards-gate-a/`。

Inspector 只应新增真实需要的 Prefab/Presenter 引用，不把 handler 做成场景对象。回滚按 adapter、Domain handler、Application target policy、registration、测试/表现的单一切片撤销；不得留下已注册但无实现的效果。
