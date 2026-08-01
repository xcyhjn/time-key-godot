# ADR 0007：Occupant 效果状态与独立 Clear 会话

> 状态：Accepted
> 日期：2026-08-02

## 背景

ADR 0005/0006 已把战斗命令收口到 `CombatApplicationSession`，并以 typed target、效果 handler/registry、Presenter 和保存到仓库的 Scene/Prefab 建立扩展边界。剩余五卡带来两类不同问题：`recover`、`built`、`poison` 要修改或创建地块 occupant；`wind`、`tornado` 则不选地图目标，也不应伪装成普通 `TimelineAction`。若把这些规则写回共享 Controller，或让表现层推导生命值、状态层数与 clear 命中关系，就会重新引入 stable ID 分支和双份状态。

## 决策

### 1. Occupant 是无 Unity 依赖的领域状态

- `CombatOccupantState` 以稳定运行时身份和 `HexCoord` 索引 occupant，保存 kind、creation ID、attitude、HP/Max HP、poison stacks 以及 health/status capability。
- `CombatSliceState` 是 occupant 查询、创建与占位校验的权威；普通效果在 Resolve 时按稳定坐标重新查找当前 occupant，选择阶段的 View 引用不能替代最终重判。
- 对 occupant 的有效修改由 `OccupantEffectResult` 返回不可变 `Before`/`After` 快照。创建使用 `Before=null`；无效、目标消失或已不满足条件的结算返回 no-op，不制造虚假结果。
- 本阶段只冻结创建、治疗和叠层状态，不执行 Tower 自损或 Poison 回合 tick；这些行为必须由 Wave 02B3 的统一回合生命周期驱动。

### 2. 普通效果继续通过 handler/registry 扩展

- `TimelineGrid` 的普通结算 handler 集合包含 `Damage`、`Elevation`、`Recover`、`Built`、`Poison`。每个 handler 自行声明是否支持 typed effect，并写入统一结果缓冲。
- `recover` 只治疗存在、支持生命值且未满血的 occupant，并钳制到 Max HP；`built` 只在 Resolve 时仍为空的真实地块创建一个中立、HP 100 的 Tower；`poison` 只对仍存活且支持状态的 occupant 增加整数层数。
- 内容可交互登记与 Domain handler 仍是两项显式契约：前者决定卡牌能否进入交互，后者决定结算。新增普通效果必须同时登记并覆盖测试，不得在 Controller 或 Presenter 中按 stable ID 路由。
- 不增加第三方包；现有 Domain/Application/Composition/Presentation 程序集方向保持不变。

### 3. Clear 是独立会话，不是普通时间轴 action

- typed `CardEffect.ClearMask` 是 clear 范围的唯一来源，不回退到普通 `CardDefinition.Shape`。
- `TimelineClearSession` 独立维护 Selected、Preview、Committed、Cancelled 状态；Application 只暴露通用 `OrdinaryTimeline` 与 `TimelineClear` interaction mode，用模式约束命令顺序。
- Clear 不选地图目标、不调用普通 `CanPlace`/`TryPlace`、不创建 `TimelineAction`、不占新时间轴格，也不进入普通效果 Resolve。
- 合法性只取决于整个 mask 是否位于 12x3 时间轴边界内。合法空清可以提交；越界 preview 不可提交；取消清除临时状态且不修改时间轴。
- 命中判断使用时间轴中真实 `TimelineAction` 对象身份去重。同一个 action 即使被 mask 命中多格，也只返回一次，并移除该 action 的全部占格；不按 actor kind、卡牌 ID 或文案添加隐藏过滤。

### 4. Presentation 只消费结果与快照

- `CombatOccupantPresenter` 消费 `OccupantEffectResult`：按 `CreationId` 从 Inspector 登记的 Prefab 创建动态 occupant，将其挂到目标地块的 `OccupantAnchor`，并从 `After.PoisonStacks` 更新状态 Prefab。它不重复治疗、建造、叠层或目标合法性规则。
- `ClearTimelinePreview` 消费 typed clear preview，以颜色、边框和符号冗余区分越界 `!`、合法空格 `○` 与命中 `HIT`；`TimelinePresenter` 只按 `TimelineClearResult.RemovedActions` 删除完整 action 表现并恢复 preview 前外观。
- `VerticalSliceController` 仅保留通用 interaction mode 的输入协调与兼容 facade；不得新增 `recover`、`tower`、`poison`、`wind`、`tornado` stable ID 分支或 effect-type 大型 switch。

## 取舍

- Occupant 快照和结果对象增加了少量复制成本，但把状态修改、诊断和表现同步放在同一可审计事实链上，也为 02B3 的全图回合开始快照留出明确入口。
- 内容登记与 handler registry 需要同步维护；这是用显式测试换取“可交互”与“可结算”不被隐式混同。
- Clear 维护独立状态机比复用普通卡牌会话多一个类型，但避免虚假 target、虚假 action 和普通放置规则污染，也能表达合法空清与 action identity 去重。
- Presenter 依赖保存的 Tower/Poison Prefab 和 Inspector 注册；配置缺失会显式失败，而不是运行时静默生成不可维护的替代物。

## 后果

五个普通效果可沿同一 typed handler 链路结算，occupant 的创建、生命值和状态变化都能通过 before/after 快照测试和渲染；Wind/Tornado 则在独立 clear 会话中完成边界预览、空清、去重和完整 action 移除。共享 Controller 不承担具体卡牌语义，Scene 中保存稳定锚点与 Presenter 配置，动态对象只从已保存 Prefab 实例化。

Wave 02B3 必须复用这里冻结的 occupant 状态和结果边界实现统一回合阶段：Tower 在创建所在结束回合及后续回合自损 50，Poison 以回合开始全图快照传播、伤害和衰减，死亡时移除 occupant 并同步 Presenter。不得把这些生命周期规则回填到卡牌 handler 或表现层。
