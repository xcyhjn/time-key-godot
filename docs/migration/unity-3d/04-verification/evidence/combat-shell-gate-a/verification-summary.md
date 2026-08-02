# Combat Shell Gate A verification summary

> 结论：PASS
> 日期：2026-08-02
> Unity：6000.4.10f1

## 实现

- Build index 0 为持久 `Bootstrap`；Build Settings 固定六 Scene 顺序。
- Bootstrap 唯一拥有 SceneFlow、TransitionCanvas、全局输入/焦点 gate、EventSystem、AudioRoot 与 state store。
- 五个内容 Scene 各有且仅有一个 `SceneContentEntry`，没有 EventSystem/AudioRoot/Transition 副本。
- Combat `VerticalSliceRoot` 在 typed payload 绑定前保持 inactive；绑定后才启用既有 Composition/Presentation。历史 direct-load 测试使用测试专用 EventSystem，不把副本保存回生产 Scene。
- Application 新增 Unity-free request/phase/failure/result、launch/outcome/shell state 与 coordinator；成功链和回滚链按 ADR 0010 固定。
- Victory outcome 要求 reward claimed；Defeat 不走 reward。run/room/correlation/battle/deck identity 进入 fingerprint 与一次性局外消费。

## 最终门禁

| 门禁 | 结果 |
| --- | --- |
| full EditMode | `320/320`，0 failed/skipped/inconclusive |
| full graphical PlayMode | `62/62`，Direct3D 12，0 failed/skipped/inconclusive |
| additive roundtrip | `1/1`；GameStart -> MainMenu -> OutOfBattle -> Combat -> Victory return；第二次 Combat -> GameOver -> MainMenu |
| Scene 结构 | Bootstrap 持久对象各 1；内容 Scene entry 各 1、持久对象副本 0 |
| Windows build | Development `Succeeded`，六 Scene，`211736305` bytes |
| actual Player | 960x540 可见 Player，exit 0，marker 1，异常 0 |
| Git/进程 | 验证后 Unity 进程 0；受保护改动未纳入 |

正式证据为 `editmode-final.xml`、`playmode-final.xml`、`build-summary.json` 和 `player-smoke-summary.json`。原始 Unity/Player 日志以及启动失败、超时和 renderer-crash 诊断日志不提交；它们只用于定位，不替代最终通过证据。

## 关注项

- Gate A 内容 Scene 为无视觉壳；GameStart/MainMenu/OutOfBattle/GameOver 的正式视觉、交互、动画和三视口证据分别留给 Gate C/D/E。
- Combat Composition 仍使用 02B4 fixture 构造战斗状态；Gate D 接入时必须从已绑定 `CombatLaunchPayload` 建立 deck/round/settlement，不能先生成 fixture 再覆盖。
- 首个可渲染帧在 Gate A 由 enabled camera + 下一 Unity player-loop frame 冻结；最终真实像素仍必须由图形 harness/Player 截图验证。
