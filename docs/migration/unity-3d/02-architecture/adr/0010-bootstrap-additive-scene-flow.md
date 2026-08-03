# ADR 0010：Bootstrap 与 additive SceneFlow

> 状态：已接受；Combat Shell Gate E 交付后刷新
> 日期：2026-08-02
> 取代范围：ADR 0004 中“Combat Scene 自有 EventSystem”的单一所有权约定；其余序列化 Scene/Prefab 原则继续有效。

## 决策

Unity Player 以 `Bootstrap.unity` 作为 Build index 0。Bootstrap 在整个会话中保持加载，并唯一拥有 `SceneFlowRoot`、`TransitionCanvas`、输入锁/焦点恢复、`EventSystem`、`AudioRoot` 与场景流诊断。内容 Scene 采用 additive load/unload，不得保存第二套上述持久对象。

Build Settings 顺序冻结为：

1. `Bootstrap`
2. `GameStart`
3. `MainMenu`
4. `OutOfBattleShell`
5. `CombatVerticalSlice`
6. `GameOver`

Application 层只保存 Unity-free `SceneId`、typed request/payload/outcome、phase、failure 与局外状态。Scene 名称和 Unity `Scene`/`GameObject` 只存在于 Composition 的序列化 route catalog 与运行时适配器。

合法 route/payload 矩阵固定为：`GameStart -> MainMenu`、`MainMenu -> OutOfBattleShell`、`GameOver -> MainMenu` 使用 Empty payload；`OutOfBattleShell -> Combat` 只能使用 `CombatLaunchPayload`；`Combat -> OutOfBattleShell/GameOver` 只能使用目标与 outcome 一致的 `CombatOutcome`。其余组合均在加载前 typed fail。

## 阶段与原子性

成功路径固定为：

```text
Idle -> InputLocked -> Covering -> LoadingTarget -> ActivatingTarget
-> BindingPayload -> WaitingForFirstRenderableFrame -> UnloadingSource
-> Revealing -> InputUnlocked -> Idle
```

进入 Combat 时，独立的 content entry 先接收 `CombatLaunchPayload`，随后才启用 Combat Composition/Presentation 子树。退出 Combat 时，02B4 settlement 先提交 typed return；Victory 必须在 reward claimed 后适配为 `CombatOutcome`，Defeat 不领取奖励。

提交点位于 source unload operation 成功启动之后。提交前的 load/activate/bind/first-frame/cancel 失败保持遮罩与输入锁，回滚 typed state、卸载目标并恢复来源 camera/content/focus；提交后的 reveal/unlock 失败保留 target，只继续揭罩和解锁，不得再次卸载 target。失败结果必须记录 typed reason、失败 phase、恢复后的 SceneId 与最终锁状态。

Binding 阶段的生产 state store 写入使用可回滚副本；source unload 启动后提交。取消或退出后的异步回调必须以 sequence/fingerprint 拒绝陈旧写入。

## 身份与重复请求

- request 使用严格递增 sequence 和 correlation ID。
- 同 sequence、同 fingerprint 幂等返回既有 in-flight/completed 结果。
- 同 sequence、不同 fingerprint 返回 `SequenceConflict`；旧 sequence 返回 `Stale`。
- Busy 时快速失败，本阶段不排队。
- `CombatOutcome` 组合原 launch 的 run/room/correlation identity 与 02B4 `BattleReturnPayload`，不得另建第二套战斗结算状态机。
- launch、settlement snapshot 与 return payload 的 `BattleTag + BattleSeed` 必须一致；launch fingerprint 必须包含 `BattleTag`。
- 局外状态按 outcome correlation 只消费一次；同 payload 重放幂等，不同 payload 冲突。

## 输入、焦点、相机与音频

Bootstrap 的全屏遮罩同时拦截 pointer 与 navigation；窄进程级 `SceneInputLockState` 还约束 Controller、轨道相机和手牌 View 的直接 `Input` 轮询。锁定时清空当前焦点。跨 Scene 只保存稳定 focus ID 字符串，由目标 content entry 解析默认焦点，不保存旧 Scene 的 Unity 引用。任一时刻最多一个可交互内容根、一个 enabled content camera 和一条内容 BGM；Bootstrap 不保存内容 camera。

## 不采用

- 不使用 `DontDestroyOnLoad` singleton 或全局 service locator 复制 Bootstrap 所有权。
- 不把 `object`、Dictionary/Variant 或 Unity object 作为跨 Scene payload。
- 不用 `Task.Delay` 猜首个可渲染帧或动画完成。
- 不在运行时发现重复 EventSystem 后再销毁；重复项由 Scene 结构测试阻止。

## 验证后果

Gate A 覆盖纯 coordinator phase/failure/idempotency、合法 route/payload、跨战斗 identity、防御性复制、state rollback/commit、Bootstrap 结构与可观察 initialization fault、真实 additive bind failure、最小 additive 往返和提交前/后失败恢复。移除 Combat 自有 EventSystem 后，历史 direct-load PlayMode fixture 由 Bootstrap fixture 或测试专用 EventSystem 替代；不得为旧测试保留生产重复对象。整改终验为 full EditMode `330/330`、Direct3D12 PlayMode `64/64`、Windows build 和 actual Player smoke 全通过。

Gate E 没有扩展 SceneFlow 状态机，而是在既有 `ISceneRevealPresentation` 完成边界内加入保存的多层 reveal。OutOfBattle、Combat、GameOver 的层序由 Presentation 持有；缺层、零时长、disable/destroy 和运行中强制完成都必须停止旧协程并保持终态，SceneFlow 只等待 completion。三轮实际 Player 往返保持单一 Bootstrap、单一 content entry 与 typed identity；最终门禁为 `343/343 + 100/100`、六 Scene Windows build 和实际 Player 三轮 smoke。Wave 03 必须继续复用该边界，不得用完整局外地图重写 Bootstrap 或 typed route。
