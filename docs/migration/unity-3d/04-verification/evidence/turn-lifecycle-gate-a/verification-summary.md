# Turn Lifecycle Gate A 验证总结

> 结果：PASS
> 日期：2026-08-02
> 分支：`unity_7.31`

## 实际验证

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| 定向 EditMode | `85/85`，0 失败、0 跳过 | `editmode-targeted-results.xml` |
| 全量 EditMode | `183/183`，0 失败、0 跳过 | `editmode-results.xml` |
| 全量 PlayMode | `38/38`，0 失败、0 跳过 | `playmode-results.xml` |
| Agent A 纯 C# | `20/20`，Domain/Application 编译通过 | `../../../03-workstreams/agents/reports/turn-lifecycle-agent-a-runner.md` |
| 静态检查 | `git diff --check` 通过；Domain/Application 新代码无 UnityEngine 依赖 | 本次检查输出 |

定向集合覆盖 lifecycle、snapshot、CardPlaySession、TimelineGrid、TimelineClearGrid 与 CombatApplicationSession。全量结果包含前置七卡、汉化/Silver、Scene authoring 和 Presentation 回归。

## 已关闭范围

- `TurnLifecycleRunner` 固定 InitialStart 与 EndTurn 顺序、派生输入锁、Busy 重入拒绝、连续 sequence、02B4 no-op hook 和 typed failure 锁存。
- `TimelineActionIdentity` 使用 lifecycle sequence + ordinal；Application 为同一 session 分配确定性 ID，preview、commit、resolve 与 clear snapshot 保持同一值。
- `TimelineGrid` 以 ActionId 而非对象引用检查重复、去重排序和整组清除，并可投影为 x 后 y 的 `TimelineActionPlan`。
- `TimelineActionPresentationSnapshot` 深拷贝 shape、occupied cells 和 range，并保存 actor、priority、source/target、display、validity/reason 与 resolve state。
- 既有 `CombatSessionPhase` 仍只表示卡牌交互；Controller 没有新增 gameplay 分支。

## 继承边界

Gate A 没有修改 Scene、Prefab、Presentation、字体或渲染路径，因此不生成新的截图，也不把 headless 测试当作视觉证据。简体中文阶段的 8 张实际截图、Windows build `210916374` bytes 与 Player smoke 继续作为未受影响的继承证据；Gate B 修改 UI/Scene 后必须生成新的三视口视觉证据，最终 Gate D 重新运行 build 与 Player smoke。

## 未关闭范围

敌方意图生成/重判、卡牌响应式布局、效果详情框、玩家/敌人 action frame、地图双向映射、Tower decay 与 Poison 三 pass 均未在 Gate A 实现，分别留在 Gate B/C。
