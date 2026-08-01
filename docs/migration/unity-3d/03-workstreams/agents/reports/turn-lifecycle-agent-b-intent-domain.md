# Turn Lifecycle Agent B1：Enemy Intent 纯数据与 Handler 交付报告

> 状态：实现完成，等待主智能体集成
> 日期：2026-08-02
> 所有权：仅新增 B1 Prompt 授予的 runtime、test、`.meta` 与本报告

## 交付内容

- 新增 Unity-free `EnemyIntentSourceSnapshot`、target/effect/display 声明、world/candidate/schedule/rejection/removal/resolve typed models。
- target occupant 重判直接复用现有 `CombatOccupantSnapshot`；没有新增第二套 Timeline action 或 presentation snapshot。
- `EnemyIntentScheduler` 使用显式 seed、稳定 Ordinal 字符串 hash 和 canonical source ID 排序，不依赖集合枚举顺序或全局 RNG。
- 调度固定高优先级先放，同级候选与 origin 均由 seed 决定，接受现有 Timeline occupied cells，默认 `12x3`、最多 5 个。
- candidate 直接生成现有 `TimelineAction`，保留同一 `TimelineActionIdentity`、source/target identity、literal shape 和 effect range。
- Village `011` 原样表达为偏移 `(1,0),(2,0)`；origin 本身仍须在 grid 内，但不会被补成占格。
- `EnemyIntentResolver` 在 Resolve 前重判 source identity/coord/alive/capability、保存目标、目标 occupant identity/attitude/HP、literal shape、effect definition 和当前拓扑展开后的 effect range。
- 每次 Resolve 均返回完整 `EnemyIntentRemovalSnapshot`，包含 ActionId 与全部 occupied cells；失效不重选目标。
- 空 source command 返回 `Succeeded=true`、`EffectApplied=false`、`UnsupportedSourceCommand`；不会构造 damage 0。非空但未注册 command 返回显式 `UnsupportedEffect` no-effect。
- `EnemyIntentApplicationService` 只把上述结果投影为 Gate A 既有 `TimelineActionPresentationSnapshot`，scheduled/resolved/removed 使用同一 ActionId。

## 验证

- Unity `6000.4.10f1` 随附 Roslyn，以 C# 9、`warnaserror` 编译完整 `Runtime/Domain/**/*.cs`：通过。
- 同一编译器引用 Domain 后编译完整 `Runtime/Application/**/*.cs`：通过。
- 编译本 Agent 新增 EditMode tests：通过。
- 使用仓库 `ReflectionTestRunner.cs` 执行专属 NUnit 测试：`passed=23, failed=0`。
- 覆盖 candidate filtering、priority、seed 可复现和输入顺序无关、不同 seed、Village literal shape、边界、Timeline conflict、数量上限、ActionId 深拷贝、source replacement/death/capability、target retarget/removal/replacement/death/attitude、shape/effect/topology 重判、完整 removal snapshot、unsupported/no-effect 与现有 presentation snapshot 投影。
- runtime 新文件静态扫描无 `UnityEngine`、`UnityEditor`、`GameObject`、`MonoBehaviour`、`Resources`、`Sprite` 或 `Color`。
- 所有新增 Unity asset 均有配套 `.meta`，且专属路径无 trailing whitespace。
- 遵循任务禁令，本 Agent 未启动 Unity，因此 Unity EditMode filter 与全量回归由主智能体集成后执行。

## 主线程集成需求

1. 从当前 `CombatOccupantSnapshot` 与正式 occupant intent capability/catalog 投影 `EnemyIntentSourceSnapshot`；Tower 必须传 `CanGenerateIntent=false`，Village 传 literal `(1,0),(2,0)`，不得在 Presentation 反推。
2. 在刷新意图时从当前 lifecycle sequence 的下一个空闲 ordinal 开始分配，传入 Timeline 当前全部 occupied cells；将 `Scheduled[i].Action` 放入现有 `TimelineGrid`，不要重建 ActionId。
3. 同一 scheduled action 使用 `CreateScheduledSnapshot()` 发布 Timeline/地图/tooltip 数据；空 Godot command 将自然显示为 `UnsupportedSourceCommand`。
4. topology 改变时对现有 scheduled action 调用 `Resolve`/重判；invalid 时按 `Removal.ActionId` 原子移除完整 frame、格子与地图 overlay，不在原 action 上静默改 target。
5. Timeline resolve 端口在真正 effect handler 前执行最终重判，并发布 `CreateResolvedSnapshot()`；本阶段没有权威 enemy command，故正常结果是显式 unsupported no-effect。
6. 集成后运行 B1 EditMode filter、完整 EditMode/PlayMode，并由主线程完成 Scene/Prefab 与视觉证据；本交付不拥有这些路径。

## 工作区与 Git

- 未修改任何既有共享文件、Scene、Prefab、ProjectSettings、asmdef 或共享维护文档。
- 未切分支、stash、暂存、commit 或 push。
- 当前无用户决策阻塞。
