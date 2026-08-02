# Wave 02B4 Deck & Battle Flow Gate D 验证总结

> 日期：2026-08-02
> Unity：6000.4.10f1
> 分支：`unity_7.31`
> 结论：PASS

## 冻结行为

- starter deck 为 12 张、固定 seed 731；stable card ID、`CardInstanceId` 与 action identity 相互独立。
- InitialStart 为 `7/5/0`；正式 EndTurn 只使用预冻结占格和手牌快照，顺序固定为弃手、时间币、phase/Era、必要回洗、抽 5，再刷新意图。
- 两轮实际路径为 `7/5/0 -> 2/5/5 -> 7/5/0`，phase `1 -> 2 -> 3`，时间币 `0 -> 31 -> 64`；第二轮发生一次确定性回洗。
- 胜利规则为 `maximumHp > 0` 且当前生命不高于最大生命 10%；Victory/Defeat 互斥，终局锁输入，胜利奖励入口与领取各一次。
- typed return boundary 携带 outcome、Era/phase/timecoins、12 张 deck snapshot、battle tag 与 seed；牌离手后 action display snapshot 仍独立存续。

## 自动化结果

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| Full EditMode | `300/300`，0 failed/skipped/inconclusive | `editmode-final.xml` |
| Full graphical PlayMode | `61/61`，0 failed/skipped/inconclusive；Direct3D12，非 NullGfx | `playmode-final.xml`、本地原始日志 |
| 视觉 Harness | 18 张 PNG，三视口、多回合、回洗、空弃牌、action 残留、胜利/失败 | `visual-summary.json`、`visual-review.md` |
| Windows build | `StandaloneWindows64` Development，`Succeeded`，`211133001` bytes；Silver attribution 存在 | `build-summary.json` |
| Actual Player smoke | exit 0，`TIMEKEY_PLAYER_SMOKE_PASS` 恰好 1 次，异常 0 | `player-smoke-summary.json` |

## 人工结论

18 张最终 PNG 已逐张打开。1280x720、1920x1080、2560x1080 均无关键裁切、UI 越界、中文缺字、错误字体、结算层穿透或 reward 残留。唯一关注项是选中卡抬升会局部遮住相邻卡上缘，但不影响关键文案或交互边界，因此不构成 Gate D 阻塞。

Build 后实际运行的是同一产物中的最新 Domain/Application/Presentation/Composition 程序集；其长度、时间与 SHA-256 记录在 `player-smoke-summary.json`。原始 `.log` 只保留为本地诊断，不纳入检查点。
