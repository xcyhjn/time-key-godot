# Turn Lifecycle Agent C2：Occupant/Status 生命周期表现交付报告

> 状态：C2 独占实现与 PlayMode 测试完成，等待主线程共享 Binding、Controller、Editor authoring 与最终视觉集成
> 日期：2026-08-02
> 所有权：仅 C2 Prompt 指定的 Occupant Presentation、PoisonStatus Prefab、PlayMode tests 与本报告

## 交付内容

- `CombatOccupantPresenter.ApplyLifecycleChanges()` 直接消费 C1 的 `LifecycleOccupantChangeResult`，不重算 Tower damage、Poison 传播/伤害/衰减或 death policy。
- Presenter 按 `runtimeId + sequence + phase` 拒绝重复与过期结果；同 sequence 的 building 结果仍可被更晚的 turn-start status 结果覆盖。
- Tower/其他有 HP occupant 的 View 保存 typed `AfterHp`；Remove 先移除 Poison status，再从 runtime ID 映射移除并停用/销毁 occupant View。
- Poison `AfterPoisonStacks>0` 创建或更新唯一 status View；降到 0、RemainBroken death 或 Remove 时立即移除，不留下映射或可见残影。
- `RegisterExisting()` 重复绑定同一 View 保持幂等；同 runtime ID 替换 View 时清理旧 View/status，disable/enable/rebind 不复制节点。
- 新增 `OccupantRemoved(string runtimeId)` 通知。它只发布 typed identity，不在 Presentation 反查 action、intent 或目标合法性，供主线程统一清理相关 overlay/frame。
- `PoisonStatus.prefab` 的层数 `TextMesh` 改用现有 Silver 字体 `35b5b371d76876b4f8b26cd2376eb08c`。

## 专属测试

新增 `Tests/PlayMode/Occupants/Lifecycle/CombatOccupantLifecyclePresentationTests.cs`，共 9 case：

- Tower `100 -> 50 -> 0/Remove`，status/View 清理和重复 removal 通知幂等。
- old Poison source 的 typed HP/stacks 更新、新感染 typed stacks 显示、0 层图标移除。
- Poison death `RemainBroken` 保留 occupant View、HP0、清 status。
- 过期 sequence 不得回写 HP/status。
- disable/enable/rebind 零重复 View/status。
- yaw `0/90/180/270` 下 occupant 与 status 保持同一 anchor。

Unity 6000.4.10f1 专属 PlayMode 结果：`9/9 passed`，XML 为 `D:/timekey-unity-731/Logs/turn-lifecycle-c2-playmode.xml`。

并发工作区的全量 PlayMode 结果为 `44/53`，9 个失败均不在 C2 路径：1 个 CardHand 旧位移断言、6 个 VerticalSlice 初始 action 数/新 intent 生命周期旧断言、2 个旧 ClearTimeline rig 缺 action-frame 引用。C2 的 9 个用例在同次全量运行中全部通过；这些共享测试适配已交回主线程。

## 主线程集成交接

1. 在每次 lifecycle 结果发布后，把 building 与 status 的 `LifecycleOccupantChangeResult` 按 phase 顺序传给 `CombatOccupantPresenter.ApplyLifecycleChanges()`；不要重新构造 HP/Poison 公式。
2. 在共享 Binding/overlay 所有权内订阅 `OccupantRemoved(runtimeId)`，使用 Application snapshot 的 source runtime ID 映射清理 intent frame、tooltip 和地图 overlay。C1 result 没有 ActionId，Presentation 不应猜测。
3. `VerticalSliceSceneAuthoring.CreatePoisonStatusPrefab()` 当前创建 `TextMesh` 后未设置 Silver font；主线程需在其 Editor 所有权内同步设置字体，否则再次 authoring 会覆盖已保存 Prefab 的 Silver 引用。
4. `CombatOccupantView.healthLabel` 是可选保存引用。若最终视觉要求 Tower 本体直接显示 HP，主线程应由 Tower Prefab authoring 创建 Silver TextMesh 并赋值；不应在运行时创建 fallback UI。即使不配置标签，typed HP、退场与 status 同步仍可由 Presenter/测试观察。

## 边界与工作区

- 未修改 Domain/Application/Infrastructure/Composition、Controller、Binding、Scene、Editor、asmdef、Card/Timeline/Intent Presentation 或共享证据。
- 未有意修改当前 dirty 的 `Tower.prefab`；该文件只有主线程先前 authoring 产生的空白序列化差异。
- 未切分支、stash、暂存、commit 或 push。
- 未生成截图；C2 专属测试证明结构与映射，最终 Tower/Poison 画面仍必须由主线程在集成 Scene 中实际截图并人工检查。
- 当前无用户决策阻塞。
