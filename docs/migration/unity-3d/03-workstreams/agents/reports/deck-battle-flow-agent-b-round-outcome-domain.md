# Wave 02B4 Agent B Round / Outcome Domain 报告

> 状态：已完成并返回所有权
> 分支：`unity_7.31`
> 写入范围：仅 Agent B 独占路径

## 交付

- `BattleRoundLedger`：新建状态为 Era 1 / phase 1 / 0 timecoins；`AdvanceTurn` 原子发放 `36 - occupiedCellCount` 时间币并推进 phase，phase 8 后进入下一 Era / phase 1。
- `RoundTransaction`：回合推进和时间币消费都只能通过 typed transaction 修改。同 sequence + 同 payload 返回首个结果；同 sequence + 不同 payload 返回 `SequenceConflict`。无效占格、无效消费、余额不足、时间币/Era 溢出均是 typed failure 且无副作用。
- `BattleSettlementState`：单一 authoritative transaction 维护 `Active / VictorySettlement / Defeat`。首次终局成功后输入锁定；同结果重放返回首个结果，相反结果返回 typed conflict。
- `BattleRewardEntry`：只由 Victory 产生，奖励类型固定为 `Shop / Acquire / Remove / Craft`。`TryClaimReward` 首次成功，同 sequence 幂等，新 sequence 重复领取返回 `RewardAlreadyClaimed`。Defeat 没有 reward entry。
- `BattleReturnPayload`：携带 outcome、Era/phase、timecoins、防御性复制的 deck stable-ID snapshot、battle tag 和 battle seed。Victory 显式为 `VictoryCompleted`；Defeat 显式为 `Defeat` 且 `IsCompleted == false`；Active 不能生成 return payload。

## 定向覆盖

`BattleRoundLedgerTests` 与 `BattleSettlementStateTests` 覆盖：

- 1:1 起步、phase 8 rollover 和多轮推进。
- 0 / 36 / 部分占格奖励，多格按 occupied cell 计数。
- sequence 幂等、payload 冲突、时间币/Era 溢出、消费失败无副作用。
- Victory/Defeat 双向竞争、终局输入锁定、同结果重放、奖励只领取一次。
- Victory/Defeat typed return payload、Active 返回拒绝、无效 deck snapshot 无副作用和集合防御性复制。

## 验证

- Unity 6000.4.10f1 附带 Roslyn，Domain 和两组 NUnit 测试以 warnings-as-errors 独立编译通过。
- 使用 Unity 附带 NUnit 断言程序集做纯 C# 反射执行：`32 passed / 0 failed`。
- 静态搜索确认 Agent B Runtime/Test 中没有 `UnityEngine`、`UnityEditor`、`MonoBehaviour`、`GameObject` 或 `ScriptableObject` 依赖。
- 依 Agent Prompt 未启动 Unity，因此本 Agent 不伪造 EditMode XML；定向 EditMode 与最终 XML 由主智能体在共享验证阶段运行。

## 接线约束

- Application hook 必须从 Timeline 清空前冻结的 occupied-cell 集合传入 `RoundTransaction.AdvanceTurn`，不能传 action 数，也不能在 clear 后重算。
- `InitialStart` 只读取 ledger 的 1:1 snapshot，不调用 `AdvanceTurn`。
- lifecycle sequence 应直接作为 `RoundTransaction` sequence，避免 UI/Presenter 自建幂等键。
- 进入任一终局后，Application 必须以 `BattleSettlementSnapshot.IsInputLocked` 拒绝 action 和 EndTurn；Presenter 不得反推 outcome。
- return boundary 所需 deck stable IDs 由 Deck Domain 的 authoritative snapshot 提供；Agent B 不引用或修改并行 Agent A 的牌库类型。

## 所有权

本 Agent 没有修改 asmdef、Application、Presentation、Composition、Scene/Prefab、共享文档、Godot 源或 Git 状态，也没有运行 Unity。Agent B 独占路径现已返回主智能体。
