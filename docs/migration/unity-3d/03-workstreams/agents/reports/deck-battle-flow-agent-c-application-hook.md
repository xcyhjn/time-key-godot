# Deck & Battle Flow Agent C 交付报告

> 状态：Gate B 已完成并由主智能体回收所有权

## 交付

- `BattleFlowNextTurnHook` 实现 ADR 0008 reserved hook，InitialStart 只抽 5，EndTurn 固定执行冻结手牌弃置、时间币、Era/phase、必要洗回与抽 5。
- `BattleFlowHookRequest` 冻结 lifecycle sequence、remaining `CardInstanceId`、Timeline occupied cell count 与 immutable action display snapshots。
- `CombatTurnLifecycleCoordinator` 在 resolution/clear 前冻结 request，再让既有 runner 在唯一 reserved hook 位置执行；Timeline、building、clear、status、intent 顺序未改。
- `CombatApplicationSession` 以 card-instance identity 选择/弃置实体卡，并暴露 typed outcome、reward 和 return boundary；终局后进入 `Resolved` 并拒绝后续 action。
- `DeckState.DiscardHand` 在任何移动前校验整批冻结身份，失败不产生部分移动。

## 审查修正

Agent C 初稿只在 hook 执行时读取整个 hand。主智能体审查后补齐冻结 hand instance identities 和原子批量弃置，避免重入或状态漂移时误弃新牌；随后完成既有 Coordinator/Session 共享接线。

## 验证

- Application hook 定向 EditMode：`10/10`。
- Deck/Application/Lifecycle 集成 EditMode：`60/60`。
- Full EditMode：`293/293`。
- Full graphical PlayMode：`53/53`。
- 本 Gate 未修改 Scene/Prefab/Presentation，视觉、Build 与 Player smoke 继承未受影响的 Wave 02B3 Gate D 证据；Gate D 会在最终集成态全部刷新。
