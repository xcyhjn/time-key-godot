# Wave 02B4 Player smoke 与 Build/Run 证据审查

> 审查范围：只读检查 `VerticalSliceController.Start(-timekeySmokeQuit)`、BattleFlow public API、Gate D build/player 产物。未运行 Unity，未修改共享代码、Scene、Composition、harness 或维护文档。

## 结论

> 后续执行结果：审计提出的 02B4 缺口已关闭。增强后的 Player 于 2026-08-02 16:02 构建，实际运行 `exit 0`，`TIMEKEY_PLAYER_SMOKE_PASS` 恰好 1 次，常见异常 0 次。结构化结果和项目程序集 SHA-256 见 `04-verification/evidence/deck-battle-flow-gate-d/player-smoke-summary.json`。

### 审计时基线（已被最终证据替代）

当前 `player-smoke.log` 证明 Windows Player 确实完成 Recover 与 Lighting 两次旧切片交互，marker `TIMEKEY_PLAYER_SMOKE_PASS` 恰好出现 1 次，且未发现 `TIMEKEY_PLAYER_SMOKE_FAIL`、`NullReferenceException`、`InvalidOperationException`、`Exception:` 或 `Unhandled`。但 `VerticalSliceController.Start()` 在 marker 前只断言第二轮已进入、目标 HP 为 0、enemy intent 未执行；它没有断言牌区、正式 phase、时间币、洗牌、终局互斥、输入锁或 typed return，因此这份日志还不能单独证明 02B4 Player smoke。

审计时 Gate D `build-summary.json` 记录 `StandaloneWindows64`、Development、Silver、`Succeeded`、`211129141` bytes 和 `D:\timekey-unity-731\Builds\Windows\TimeKeySlice.exe`。当时构建目录内四个项目程序集时间均为 `2026-08-02 15:02:23`，smoke 日志时间为 `15:03:00`。Unity launcher EXE 自身时间仍为 `2026-07-31 14:33:56`，这是可复用 launcher，不能仅凭 EXE 时间戳证明脚本新鲜度。该批次现已由最终 build `211133001` bytes、程序集 SHA-256 和明确 Player exit code 的结构化证据替代。

## Marker 前必须断言的最小 02B4 路径

以下断言应全部位于 `Debug.Log("TIMEKEY_PLAYER_SMOKE_PASS")` 之前，任一失败都进入现有 catch 并 `Application.Quit(1)`：

1. **初始正式手牌**：`BattleFlow` 非 null，`Outcome == Active`、`IsInputLocked == false`；`Era/Phase/Timecoins == 1/1/0`；Draw/Hand/Discard 为 `7/5/0`，三区总数为 12，12 个 `CardInstanceId` 唯一。记录初始手牌实例 ID，用于证明 stable card ID 与 instance ID 未混用。
2. **第一轮 Recover**：选择成功后 `Current.SelectedCardInstanceId` 有值；提交前记录 action identity，提交后该实例已不在 Hand，但 timeline/action display snapshot 仍保留同一 action identity 和 `recover` 显示负载。玩家动作 3 格与既有敌人意图 2 格共同形成冻结占用 5 格；Resolve 成功后 `LastBattleFlowResult.Succeeded`，`Phase == 2`、`Timecoins == 31`，牌区为 `2/5/5`，`DrawResult.Shuffle.Occurred == false`。
3. **第二轮 Lighting**：同样记录并核对独立实例 ID 与 action identity；玩家动作 1 格与敌人意图 2 格形成冻结占用 3 格。Resolve 后目标 HP 为 0，`LastBattleFlowResult.Succeeded`，`Era/Phase/Timecoins == 1/3/64`，牌区为 `7/5/0`，三区总数仍为 12，且 `DrawResult.Shuffle.Occurred == true`。这一步同时证明空抽牌堆从弃牌堆确定性回洗。
4. **authoritative 终局**：`Settlement.Outcome == VictorySettlement`、reward entry 非 null 且尚未领取、`IsInputLocked == true`；再次 `SelectCard(...)` 必须失败。随后提交相反的 `ResolveBattleOutcome(Defeat)`，结果必须失败且 failure 为 `OutcomeConflict`，最终 outcome 仍为 Victory。
5. **typed return boundary**：`CreateBattleReturnBoundary()` 成功；payload 为 `VictorySettlement/VictoryCompleted`，`Era/Phase/Timecoins == 1/3/64`，deck stable ID snapshot 为 12 张，`BattleTag == "combat-vertical-slice"`，`BattleSeed == 731`。

奖励按钮的实际点击与“只能领取一次”已有图形 PlayMode/Harness 覆盖；无图形 Player smoke 不必模拟 UI 点击，但 smoke summary 必须明确本路径只断言 reward entry 存在，claim-once 由哪份 XML/视觉证据承担。

## 可能失败点

- **手牌确定性漂移**：smoke 直接按 stable ID 选 `recover`、`lighting`。starter order、seed、shuffle 或 draw 方向一旦变化，卡不在对应手牌时会在选择处失败；这是应保留的确定性回归信号。
- **只断言 snapshot 终值会掩盖顺序错误**：必须分别检查第一次无洗牌、第二次发生洗牌，以及各次冻结 occupied-cell 数；否则错误的弃置/回洗顺序也可能碰巧得到 12 张总数。
- **action identity 被 View 销毁带走**：打出牌会立即离开 Hand。只检查 card stable ID 不能证明已提交 action frame/snapshot 存活，需在第一或第二次 commit 后核对 action identity 与保存的 presentation snapshot。
- **结算序列与相反命令**：自动胜利使用生命周期 sequence，Controller 的显式 settlement sequence 独立从 1 开始。相反 outcome 应得到 `OutcomeConflict`；若意外复用同一 sequence，可能得到 `SequenceConflict`，两者含义不同，smoke 应固定期待前者。
- **终局后输入漏锁**：只检查 outcome 不够；还要主动尝试选牌并确认失败，防止 Presenter 显示胜利但 Application 仍接受命令。
- **return 只看 `Succeeded` 不足**：payload 可能漏牌、回合资源或错误 battle tag/seed；必须逐字段断言。
- **退出语义**：marker 必须只在所有断言完成后输出一次；外层 runner 同时要求进程 exit code 0、marker count 1、fail marker 0、无未处理异常。仅 marker 或仅 exit 0 均不能通过。
- **构建证据绑定不足**：修改 `VerticalSliceController.Start()` 后，现有 15:02 build 和 15:03 log 立即失效，必须重新 Build 再运行实际 Player。不要把 Unity launcher 的旧时间戳误判为 stale build；应记录 `TimeKey.Presentation.dll`、`TimeKey.Application.dll`、`TimeKey.Domain.dll`、`TimeKey.Composition.dll` 的 SHA-256/时间，或记录完整可执行构建清单哈希。

## 最终结构化证据最低字段

`build-summary.json` 应增加实际 player path、build finish time、build result/bytes、Silver attribution 状态，以及 EXE 和项目程序集 SHA-256。`player-smoke-summary.json` 应增加相同构建哈希引用、实际 arguments（`-batchmode -nographics -timekeySmokeQuit -logFile ...`）、start/end time、exit code、pass/fail marker count、异常计数、raw log path/hash，并列出上述初始/第一轮/第二轮/终局/return 的实际观测值。

原 `player-smoke.log` 仅作为增强前基线；最终 Gate D 使用 `player-smoke-final.log` 与 `player-smoke-summary.json`。
