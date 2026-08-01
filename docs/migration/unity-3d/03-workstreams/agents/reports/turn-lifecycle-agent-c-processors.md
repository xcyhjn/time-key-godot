# Turn Lifecycle Agent C1：Building、Poison 与 Death Processor 交付报告

> 状态：纯 C# 实现与独占测试完成，等待主线程共享状态适配和 Unity 集成
> 日期：2026-08-02
> 所有权：仅 C1 Prompt 指定的新 lifecycle 子目录、对应 EditMode tests、`.meta` 与本报告

## 交付内容

- 新增 `ILifecycleOccupantStore` 事务端口：入口一次性返回 `CombatOccupantSnapshot` 全图快照；提交必须先校验所有 expected HP/Poison 值，再全有或全无地应用 mutation。Remove 同时清理 occupant registry、tile slot 和 status registration。
- 新增 typed death/removal 契约：`LifecycleDeathPolicy`、`LifecycleMutationReason`、`LifecycleOccupantMutation` 与 `LifecycleOccupantChangeResult` 均携带 sequence、phase、runtime ID、coord、before/after HP、before/after Poison、removal 和 reason。
- 冻结默认死亡策略：Tower 与 canonical typed `radar-underling` 为 `Remove`；其他普通 occupant 为 `RemainBroken`。死亡后 Poison 归零，不保留悬空状态。
- 新增 `TowerBuildingBehaviorProcessor`，实现 `IBuildingBehaviorProcessor`。它在调用时拍摄包含本轮新建 Tower 的快照，按 `HexCoord(Q,R)`、runtime ID 排序，并固定执行 50 点 decay。
- 新增 `PoisonTurnStartProcessor`，实现 `ITurnStartStatusProcessor`。它按入口不可变快照严格执行六邻传播、旧来源伤害、旧来源衰减三个全局 pass，并只进行一次原子提交。

## 冻结行为

### Tower

- HP `100 -> 50`，同一创建 cycle 即可执行；下一 lifecycle sequence `50 -> 0` 后按 `Remove` 原子移除。
- HP0、已移除或非 Tower occupant 不执行。
- 同一 sequence 重复调用返回 successful/already-processed，不重复 snapshot、damage 或 mutation。
- store 拒绝 mutation 时不发布 change result，也不锁存 sequence；修复前置状态后可用同一 sequence 安全重试。

### Poison

- 入口 old source 仅限 `IsAlive && SupportsStatus && PoisonStacks>0`。
- Pass 1 使用 axial 六邻 `(1,0),(1,-1),(0,-1),(-1,0),(-1,1),(0,1)`；多源贡献 checked 聚合。目标必须在入口快照中仍存活并支持状态。
- Pass 2 只对 old source 使用 `ceil(MaxHP * snapshotStacks / 10)`；结果同时记录 requested/applied damage 与 before/after HP。
- Pass 3 只对 old source 从传播后的当前层数减 1；新感染本轮不传播、不受 Poison 伤害、不衰减。
- old source 死亡时跳过常规 decay 并清零状态；Tower/Radar underling Remove，普通敌人保留 HP0 Broken 占位。
- overflow 或 store precondition 失败发生在提交前；不发布 spread/damage/decay/change 结果，不留下部分 mutation。
- 同一 sequence 幂等；Tower 与 Poison 已通过真实 `TurnLifecycleRunner` 证明 `RunningBuildingBehaviors` 先于 `ProcessingTurnStartStatuses`。

## 测试与非 Unity 验证

新增 25 个纯 C# NUnit case，覆盖：

- 空快照、HexCoord/runtime ID 稳定排序、非法 sequence、事务拒绝与可重试。
- Tower 创建 cycle 100→50、下一 cycle 50→0、提前死亡过滤、普通 occupant 过滤和重复 sequence 幂等。
- Tower/Radar underling Remove、普通 enemy RemainBroken、Remove 强制 HP/status 归零。
- Poison 单源六邻、多源叠加、空边界、死亡/不支持状态目标过滤、精确 ceil 伤害、old source 收到传播后只衰减一次。
- 新感染隔离、checked overflow 零副作用、Tower Poison death Remove、普通 enemy Poison death Broken/status clear、结果与 mutation 稳定排序。
- `TurnLifecycleRunner` 端到端顺序：Tower `100 -> 50` 后 Poison 按 MaxHP100 扣 10，最终 HP40。

验证结果：

- Unity Mono C# 编译完整 `Runtime/Domain/**/*.cs`：通过。
- 同编译器编译 C1 独占 NUnit tests：通过。
- PowerShell 反射执行 C1 tests：`passed=25, failed=0`。
- Unity 6000.4.10f1 Roslyn 使用当前 Bee `TimeKey.Domain.rsp` 与 `TimeKey.Tests.EditMode.rsp` 编译：通过。
- C1 runtime 静态扫描无 `UnityEngine`、`UnityEditor`、`GameObject`、`MonoBehaviour`、`Resources`、`Sprite` 或 `Color`。

按主线程明确禁令，本 Agent 未启动 Unity、未运行 Unity Test Runner、未生成 XML 或视觉证据。

## 主线程集成交接

当前 `CombatSliceState` 已有按 runtime ID + coord 查询、HP 设置、正向增加 Poison 与 Remove，但缺少以下共享能力；C1 未越界修改：

1. 稳定枚举全部 `CombatOccupantSnapshot`。
2. 绝对设置 Poison stacks，以支持衰减与死亡清零。
3. 以 expected HP/Poison 预检后一次性提交全部 mutation；Remove 同步清理 occupant 双索引、tile occupancy/status 权威注册。

主线程应在共享状态或 Application adapter 中实现 `ILifecycleOccupantStore`，不要复制 `CombatSliceState`。然后把同一个 adapter 注入 `TowerBuildingBehaviorProcessor` 与 `PoisonTurnStartProcessor`，再注入已存在的 `TurnLifecycleRunner`。Application/Presentation 只投影 `LastResult` 的 typed results，不重新计算伤害、传播、衰减或死亡策略。

当前 canonical Radar underling stable kind 冻结为 `radar-underling`；若主线程未来为 Radar occupant 引入独立 enum/flag，应在 adapter 投影为该 canonical typed kind，而不是在 processor 中增加 Godot 字符串分支。

## 工作区与 Git

- 未修改任何既有共享 `.cs`、asmdef、Scene、Prefab、Controller、Composition、Editor、共享文档或 Godot 源。
- 未切分支、stash、暂存、commit 或 push。
- 当前无用户决策阻塞；共享 adapter 需求已明确交回主线程。
