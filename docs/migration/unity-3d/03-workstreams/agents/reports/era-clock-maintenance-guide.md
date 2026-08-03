# Era Clock Animation Maintenance Guide

## 数据流

EraClock 是单向 Presentation projection：authoritative `OutOfBattleShellState` 或 `BattleFlowPresentationSnapshot` -> `EraClockSnapshotAdapter` -> immutable `EraClockPresentationSnapshot` -> `EraClockPresenter`。不要让 Presenter 递增 Era/Phase、消费 outcome 或写 SceneFlow。

每个可见 owner 使用单调 `sequence`。`ApplySnapshot` 的结果可为 `Accepted`、`Repeated`、`Stale` 或 `SequenceConflict`；只有 Accepted 会改变当前投影。非相邻 authoritative jump 默认 snap，不伪造中间 Domain 事件。

## Inspector 调整

`EraClockAnimationSettings` 集中保存：

- phase/rollover/anchor duration
- center/HUD scale
- degrees per phase 和 clockwise
- rollover endpoint/pulse end fraction
- jump mode
- `AnimationCurve` easing

修改曲线时保留 `0 -> 0`、`1 -> 1`，随后重跑相邻 phase、rollover、anchor、快速 Apply 和 zero-duration。不要把持续时间复制到 SceneFlow 固定 delay；等待 `TransitionFinished`。

pointer 映射为 `(phase - 1) * 45 degrees`，clockwise 由配置转换方向。progress 为 `phase / 8f`。标签固定为 `第{Era}时代` 和 `{Phase} / 8`。如果三者不一致，先检查送入 snapshot，而不是在视图上各自修补。

## Rollover 排查

正确顺序是 Phase 8 端点 -> progress/pointer reset + pulse -> 新 Era Phase 1 终态。检查：

1. adapter 输入是否严格为 `(era, 8) -> (era + 1, 1)`；其他跳跃会分类为 Jump。
2. sequence 是否递增；stale/conflict 不会执行动画。
3. `rolloverPulse` 是否为独立 CanvasGroup，初始 alpha 0，Graphic non-raycast。
4. endpoint fraction 小于 pulse end fraction。
5. `animation-timeline.json` 的 owner、from/to、completion、取消原因和中帧 alpha。

## 取消、旧 routine 与生命周期

快速连续 Apply、rebind 和显式 cancel 都会增加 transition generation，并停止旧 coroutine。判断旧 routine 回写时，订阅 `TransitionFinished`，检查最终事件的 snapshot sequence 与 `CurrentSnapshot.Sequence` 相同，并确认 `LastCompletionReason`。

- 显式取消：`CancelAndSnap()`，reason `Cancelled`
- rebind：`Rebind(...)`，reason `Rebound`
- disable：自动 snap，reason `Disabled`
- zero-duration：同帧 terminal，reason `Immediate`
- 正常动画：reason `Completed`

pause 会 cancel-and-snap，resume 与尺寸变化会 refresh anchor。destroy 只停止 owner，不创建 fallback 实例。

## Anchor 与 resize

Center/HUD 位置必须来自保存的 `RectTransform` 引用。Presenter 使用同一个 `clockRoot` 移动和缩放；禁止创建中央/HUD 两份可见时钟。resize 后用 `RefreshLayout()` 把 settled snapshot 对齐到当前 anchor。检查 root、EraLabel、PhaseLabel 四角都在 viewport 内，并保留实际 raster margin。

HUD 顶部建议以 1920x1080 reference space 内缩约 220 pixels 起步，再按正式 HUD 邻接元素调整。不要写死屏幕中心或具体 viewport 像素坐标。

## 证据维护

行为变更后至少重跑：

- `TimeKey.Tests.EditMode.EraClock`
- `TimeKey.Tests.PlayMode.EraClock`
- 完整 EditMode
- 完整 graphical PlayMode
- `EraClockEvidenceAutomation.BuildPlayerEvidence`
- 实际 Player smoke 并检查 `ERA_CLOCK_PLAYER_SMOKE_PASS`

canonical 输出只写 `era-clock-animation-gate-*` 新目录。失败迭代用不同文件名，修复后不要覆盖 Gate 0 红测，也不要覆盖其他 wave 的历史 PNG/XML/JSON/log。截图必须逐张人工打开；同时检查 XML `total > 0`、`failed = 0`、JSON renderer/viewport、pixel margin、raycast、单 owner 和材质增长。

注意：仓库中的完整 PlayMode suite 会运行其他 wave 的视觉测试，而其中部分测试会重写固定的历史证据路径。请在 disposable worktree 重放完整 suite；若必须在 dirty worktree 运行，先保存逐文件 inventory，且只可恢复运行前明确干净、运行后才变化的文件。不要把其他 wave 的历史证据纳入 Era Clock staging。

独立 evidence automation 在 Editor 中创建并保存专用 Scene，Player 运行时不会动态搭正式 UI 树。正式 Scene/Prefab 的 serialized binding 仍由主智能体维护。
