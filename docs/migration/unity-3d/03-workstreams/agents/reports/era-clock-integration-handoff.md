# Wave 03R Era Clock Integration Handoff

## 交接状态

白名单内 Contract、Presenter、测试和独立 evidence 已完成，主智能体也已完成正式 `MainMenu`、`OutOfBattleShell`、`CombatTopHUD` 与 SceneFlow/Composition 串行接线。正式 Bootstrap 路由、三次 Victory 往返、全量测试、六 Scene build 和 D3D12 Player 均已通过；本文件现作为已关闭阶段的维护交接。

正式接线结论和精确证据见 `era-clock-formal-integration-report.md`。下一恢复点是 `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md` Gate B，不得重做地图 Domain Gate A、P0 工具链或 Wave 03P 移动/Theme。

## 三类互斥模块

| 模块 | 路径 | 职责 |
| --- | --- | --- |
| Contract | `Runtime/Application/EraClock/**` | 不可变 typed snapshot、现有 BattleFlow/OutOfBattle adapter、transition plan、唯一状态机 |
| Presenter | `Runtime/Presentation/EraClock/**` | pointer/progress/label/anchor transition、取消、rebind、resize、zero-duration；Player driver 只服务独立证据 Scene |
| Evidence | `Tests/**/EraClock/**`、`Editor/EraClock/**`、`era-clock-animation-gate-*` | 红测、行为测试、三视口截图、结构化时间线、独立 Windows Player build/smoke |

三个模块没有修改 asmdef；它们由现有程序集目录自然收录。

## 正式 Scene 树和 Inspector 绑定

主智能体应在正式 Prefab 中保存一棵稳定 UI 树，不要在运行时 `new GameObject` 创建正式时钟：

```text
EraClock (RectTransform + CanvasGroup + EraClockPresenter)
|-- ClockFace (RawImage: clock_noring)
|-- ClockRing (RawImage: ring)
|-- PointerPivot (RectTransform)
|   `-- Pointer (RawImage: point)
|-- EraProgress (Image, Filled/Horizontal)
|-- EraLabel (Text, Silver)
|-- PhaseLabel (Text, Silver)
`-- RolloverPulse (RectTransform + CanvasGroup + RawImage: ring)

EraClockAnchors
`-- CenterAnchor (RectTransform)

TopBar/ClockPlate
`-- HudAnchor (RectTransform)
```

`EraClockViewBindings` 的 12 个引用必须全部保存：`clockRoot`、`clockFace`、`clockRing`、`pointerPivot`、`pointerImage`、`progressFill`、`eraLabel`、`phaseLabel`、`centerAnchor`、`hudAnchor`、`rootGroup`、`rolloverPulse`。缺任一引用时 `ApplySnapshot` 会在启动前抛出明确字段错误。

所有 RawImage/Image/Text 的 `raycastTarget` 均设为 false；`rootGroup.blocksRaycasts` 和 `interactable` 均为 false。`MainMenu/ClockEntrance` 是 EraClock 外层 CanvasGroup，主智能体还需把该外层设为 non-raycast，不能只依赖 `rootGroup`。

素材使用现有 Resources：

- `Art/Shell/MainMenu/clock_noring`
- `Art/Shell/MainMenu/ring`
- `Art/Shell/MainMenu/point`
- `Fonts/Silver`

生产配置为 phase `0.24s`、rollover `0.64s`、anchor `1.0s`；MainMenu center scale 为 `1.0`、HUD scale 为 `0.46`。共享 Top HUD 的 Center anchor 为 `(0.20, 0.74)`、center scale 为 `0.50`、HUD scale 为 `0.32`，用于避开局外 reveal 中央房间；不得把它改回正中心。每 phase `45` 度、clockwise、rollover endpoint `0.35`、pulse end `0.65`、jump `Snap`。这些值和 easing curve 都是 Inspector 序列化配置，不要复制为调用点 magic number。证据 timeline 为了取得稳定中帧，独立 harness 将 rollover 配为 `2.0s`，不是生产默认值。

视觉证据表明顶部 HUD anchor 至少需要约 `220` 个 1920x1080 reference pixels 的安全内缩，避免 Silver 标签越过画面上缘。正式 Prefab 仍应按其相邻地图节点、按钮和 HUD 做三视口人工复核。

## Typed 调用边界

局外 authoritative 状态：

```csharp
var snapshot = EraClockSnapshotAdapter.FromOutOfBattle(
    stateStore.OutOfBattleState,
    eraClockSequence,
    EraClockAnchorTarget.Center);
eraClockPresenter.ApplySnapshot(snapshot);
```

战斗 authoritative 状态：

```csharp
var snapshot = EraClockSnapshotAdapter.FromBattleFlow(
    combatState.BattleFlow,
    eraClockSequence,
    EraClockAnchorTarget.Hud);
eraClockPresenter.ApplySnapshot(snapshot);
```

`eraClockSequence` 必须由正式 composition owner 单调分配。相同 sequence 只可重放完全相同 snapshot；同 sequence 不同内容会返回 `SequenceConflict`，更小 sequence 会返回 `Stale`。Presenter 不写回 Era/Phase、不消费 round outcome、不持有 input lease。

推荐接线位置如下：

| 路由 | 最小串行改动 | 调用顺序 |
| --- | --- | --- |
| MainMenu | 在正式 Prefab 保存 EraClock 树；由已有 run/bootstrap authority 提供 Era/Phase，不在 `MainMenuPresenter` 推导 | authoritative run state -> Center snapshot -> `ApplySnapshot`; `ClockEntrance` 只控制外层 alpha |
| OutOfBattle | `OutOfBattleShellSceneNavigation` 增加序列化 `EraClockPresenter`，与当前 `presenter.Apply(stateStore.OutOfBattleState)` 相邻投影 | state store apply -> Center snapshot -> reveal/实际 completion -> 同一 presenter 的 Hud snapshot |
| Combat | `CombatPresentationBinding` 增加序列化 `EraClockPresenter`，在 `state.BattleFlow != null` 时与 `battleFlowPresenter.Apply` 同源投影 | BattleFlow snapshot -> Hud adapter -> `ApplySnapshot` |
| 返回局外/Continue | 新 Scene owner 初始绑定最新 authoritative snapshot；同 Scene 的 center/HUD 移动只使用同一个 presenter | cancel/unload 旧 owner -> bind latest -> snap/animate by actual route semantics |

MainMenu 如果没有 authoritative run state，不得由 Presenter 猜 Era/Phase。由新游戏初始化或 Continue state 明确提供；缺数据时保持时钟未绑定或使用已有 Domain 初始化结果。

## Completion、取消与输入

- 用 `TransitionFinished(snapshot, reason)` 协调实际完成，不用固定延时模拟完成。
- 新 snapshot 会以 generation token 停止旧 coroutine，旧 routine 不能回写终态。
- Scene unload/disable/destroy 由生命周期清理；显式 transition cancel 或 pause 调用 `CancelAndSnap()`。
- rebind 使用 `Rebind(bindings, settings)`，它停止旧动画并把最新 snapshot snap 到新引用。
- Presenter 在 settled 状态的 `LateUpdate` 自动跟随当前 anchor，覆盖首帧 Canvas/Layout 重排和运行中 resize；外部仍可显式调用 `RefreshLayout()` 做即时同步。
- `IsInputLocked` 是透传元数据。EraClock 不获取或释放 `SceneInputLockState`，也不修改按钮、地图、卡牌选择或 transition cover。

## 受保护文件的最小 diff

以下文件本任务未修改。主智能体接线前必须重新读取当前 dirty diff，并只追加上述序列化引用/调用：

- `unity/Assets/_Project/Prefabs/Shell/MainMenu.prefab`
- `unity/Assets/_Project/Prefabs/Shell/OutOfBattleShell.prefab`
- `unity/Assets/_Project/Prefabs/Battle/CombatShell/CombatTopHUD.prefab`
- 正式 MainMenu、OutOfBattle、Combat Scene
- `OutOfBattleShellSceneNavigation.cs`
- `CombatPresentationBinding.cs`
- 如确有必要才修改三个既有 Presenter；推荐 composition 并列投影，避免扩大职责

接线前只读 SHA-256：MainMenu Prefab `EC4175AA...CF42E6`，OutOfBattle Prefab `6A3A5C...337988A`，CombatTopHUD Prefab `5FD1E2...77861FD`。若哈希不同，说明用户/其他任务已有新改动，必须以新内容为基线，不要覆盖。

## 后续 Gate

正式 Gate B-E 与 Wave 03R-F reveal overlap closeout 已关闭。后续只允许维护性修复，并必须保留单 owner、typed snapshot、`ClockPlate/HudAnchor`、共享 Top HUD Center anchor `(0.20, 0.74)`、non-raycast 和 settled anchor follow 契约。当前队列恢复 `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md` Gate B；Era Clock 完成不代表局外地图完成。
