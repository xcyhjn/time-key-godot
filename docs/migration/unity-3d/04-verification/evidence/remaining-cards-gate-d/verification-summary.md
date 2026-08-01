# Remaining Cards Gate D 验证总结

> 结果：PASS
> 日期：2026-08-02
> 场景：`RemainingCardsSevenCardVerticalSlice`
> 自动化种子：`731`

## 证据范围

本目录是 Wave 02B2B/02B2C 最终集成态的刷新证据。未被本阶段覆盖的 Godot 源语义、七卡 schema/原卡图哈希及解耦阶段的稳定 Scene/Prefab 基线继续由继承账本负责；本次重新验证了剩余五卡、普通卡回归、Scene/Prefab 引用、完整测试、Windows build、Player smoke 和实际渲染。

自动化证据与人工视觉判断分开记录：XML、JSON、进程退出码和 marker 证明结构与行为断言通过；下方“人工视觉检查”来自逐张打开 54 张真实 PNG，不能由非空像素或退出码替代。

## 自动化结果

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| EditMode | 152/152 passed，0 failed，0 skipped | `editmode-results.xml` |
| PlayMode | 38/38 passed，0 failed，0 skipped | `playmode-results.xml` |
| 最终 harness | PASS；marker=`TIMEKEY_REMAINING_CARDS_GATE_D_HARNESS_PASS` | `harness-summary.json`、`harness.log` |
| Windows build | `Succeeded`，207171486 bytes | `harness-summary.json` |
| Player smoke | exit code 0；`TIMEKEY_PLAYER_SMOKE_PASS` 已找到 | `player-smoke-summary.json`、`player-smoke.log` |
| 渲染采集 | 54 张 PNG；三种 viewport、四个 cardinal yaw 与七卡路径均在 manifest | `harness-summary.json` 的 `screenshots` |

最终场景在进入 Play 前已有稳定 Hierarchy，七张 fixture、36 个时间轴格、Board/HUD/Presenter 引用与以下 8 个可维护 Prefab 由测试验证：`CardView`、`Tower`、`PoisonStatus`、`TargetView`、`HexBlockDirt`、`HexBlockGrass`、`HexColumn`、`TimelineCell`。

## 七卡交互与结算核对

| 卡牌 | 交互路径 | 最终观察 |
| --- | --- | --- |
| `lighting` | 地图目标 -> 普通时间轴 | HP 10 -> 0；敌方 intent 同轮被 Resolve；目标与时间轴预览回归通过 |
| `earthquake` | 地图目标 -> 普通时间轴 | 七个范围坐标；逻辑层数 +2；顶部高度 +0.64 |
| `recover` | 地图目标 -> 普通时间轴 | HP 10 -> 100；选中、目标、合法时间轴、结算前后均留图 |
| `tower` | 空地目标 -> 普通时间轴 | 创建 1 个中立 Tower；HP 100；坐标 `(0,0)`；挂到真实 `OccupantAnchor` |
| `poison` | 活体目标 -> 普通时间轴 | stacks 0 -> 2；状态图标及层数来自 occupant `After` 快照 |
| `wind` | 不选地图目标 -> 即时 clear | 2x2 mask；越界、合法空清、命中、取消和清除后均留图；命中移除 1 个完整 action |
| `tornado` | 不选地图目标 -> 即时 clear | 12x1 mask；合法空清移除 0，命中移除 1 个完整 action；两条提交路径均留图 |

普通 handler 最终集合为 `Damage`、`Elevation`、`Recover`、`Built`、`Poison`。Clear 使用独立 `TimelineClearSession` 与 typed `ClearMask`，不创建普通 `TimelineAction`；多格命中按 action identity 去重。自动化还覆盖 Resolve 重判、no-op、重复 poison 累加、空清、越界、取消纯度、完整 action 移除及普通放置回归。

## 人工视觉检查

### 七卡手牌与视口

- `seven-card-hand-1280x720.png`：七张卡均完整可见，卡面与 stable ID 可辨；下缘触及屏幕底部，但没有实际裁切，也未遮住关键棋盘与时间轴交互区域。
- `seven-card-hand-1920x1080.png`：卡牌比例、重叠和棋盘留白正常，七卡均可识别。
- `seven-card-hand-2560x1080.png`：卡牌相对画面较小，但图像、顺序和选中目标仍可辨；未发现重叠错位或超宽裁切。

### 棋盘与四向 yaw

- `lighting-targeted-yaw-*` 与 `earthquake-range-yaw-*`：yaw 0/90/180/270 下目标、范围与同一逻辑坐标保持绑定；Earthquake 七格黄色范围完整，没有漂移到错误地块。
- `tower-after-resolve-yaw-*`：四向都能看到并选择位于 `(0,0)` 的 Tower；HP 100 可读，实例位于真实地块锚点，没有悬空或跟随相机漂移。
- `poison-after-resolve-yaw-*`：四向都保持同一 occupant/逻辑坐标；poison 图标和层数 `2` 可读，未关键性遮挡 occupant 或地块选择反馈。

### Clear 与状态恢复

- Wind/Tornado 的越界态使用红色 `!`，合法空格使用蓝色 `○`，命中使用绿色 `HIT`，边框和符号提供了不依赖颜色的冗余区分。
- Wind 的空清、命中、取消后与清除后截图，以及 Tornado 的空清提交、命中提交和两种提交后截图，均显示 preview 外观被恢复；没有残留边框、符号或被部分留下的多格 action。
- Recover、Tower、Poison 的选中/目标/合法时间轴/结算后状态和 Lighting、Earthquake 回归图均已开图检查；未见缺图、关键文字不可读、对象重叠、范围残留或结算前后状态相反。

## 结论与阶段边界

Gate D 达成：七张真实卡都从同一内容目录进入正确 interaction mode；五张剩余卡完成领域结果、表现和证据闭环；保存的 Scene/8 Prefab 与动态 CardView、Tower、Poison 状态、action 标记之间的所有权可检查；完整 EditMode、PlayMode、build、Player smoke 和人工视觉门禁均通过。

本结论不包含 Wave 02B3：Tower 创建所在结束回合及后续回合自损 50、Poison 回合开始全图快照传播/伤害/衰减、完整敌人行为与死亡移除仍是下一阶段工作。1280 手牌触底和 2560 卡牌偏小是本次视觉观察，不构成当前交互阻塞；若后续 HUD 内容增加，应在对应视口重新做实际渲染检查。
