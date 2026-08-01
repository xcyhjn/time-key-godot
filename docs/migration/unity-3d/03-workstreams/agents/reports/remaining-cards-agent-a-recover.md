# Remaining Cards Agent A — Recover 与共享 occupant 结果契约

> 日期：2026-08-01
> 分支：`unity_7.31`
> 所有权：仅修改 reviewed prompt 授权的 Domain/Application/EditMode tests，并新增本独占报告；未触碰 Controller、Presentation、Composition、Infrastructure、Scene/Prefab、共享迁移文档、Git 状态或 Godot 源。

## 1. 结论

Gate A 通过。`recover` 已沿现有普通卡牌会话进入，不需要修改 `VerticalSliceController.cs`，也没有新增 stable-ID Controller 分支、反向 asmdef 或 UnityEngine 引用。

真实行为如下：

- 选择阶段按稳定 occupant runtime ID + `HexCoord` 重查；要求目标存在、支持生命、`HP < MaxHP` 且卡牌 range 非空。
- 仍存在的 `HP=0` occupant 合法并可恢复；满血、ID/坐标不匹配和无生命能力均非法。
- Resolve 再按 action 保存的稳定 ID + coordinate 重查；目标消失、被不同 ID 替换、变满血、缺失 coordinate/range 都是无结果、无状态副作用的 no-op。
- 成功恢复使用 `min(MaxHP, HP + value)` 的等价无溢出写法；`value=100` 从 25 钳制到 100，不借用负 Damage。
- `ResolutionSnapshot.EffectResults` 继续保持 earthquake 的 `TileEffectResult` 兼容；新增只读 `OccupantEffectResults`，Recover 输出独立的 HP before/after occupant 快照。
- Application trace 从 occupant 结果记录 Recover 的 effect kind、runtime ID、coordinate 与 HP before/after。

## 2. 冻结给 Built / Poison 的纯 Domain 契约

新增的共享契约不引用 Unity：

- `CombatOccupantState` / `CombatOccupantSnapshot`：runtime ID、coordinate、kind、creation ID、attitude、HP/MaxHP、poison stacks、supports-health/status 与 `IsAlive`。
- `CombatSliceState`：按 ID、coordinate 或两者精确查询；唯一 ID/唯一坐标添加；精确移除；受控 HP 与 poison stacks mutation。输入 occupant 会复制后再由 state 持有，查询只返回快照。
- `CardEffectResultBuffer`：分别收集 `TileEffectResult` 和 `OccupantEffectResult`；Built 后续可表达 `Before=null, After=tower`，Poison 可表达 stacks before/after。
- `ICardEffectHandler.Supports(CardEffect)`：Timeline Preview/TryPlace 在占格前执行窄 payload 预检。Recover 拒绝 `value<=0`；Built 后续可用同一接口拒绝未知 creation/value，无需建立第二套 registry 入口。
- `CombatAttitude.Neutral` 已为 Tower/Middle 语义预留；本 Agent 未实现 Built、Poison 或生命周期。

旧 public `CombatSliceState` constructors 保留。兼容 fixture 会建立 runtime target occupant，`TargetId` / `TargetHp`、Damage、lighting snapshot 与 Controller 使用的 `(1,0)` 世界目标仍保持原链路；显式 occupant constructor 用于新 typed 场景与测试。

## 3. 测试先行覆盖

新增 `TimeKey.Tests.EditMode.Effects.RecoverCardEffectHandlerTests`，并更新 Timeline/Application 旧的 “Recover unsupported” 断言。覆盖：

- 受伤目标成功、25→100 钳制、HP=0 恢复、满血 no-op。
- ID 缺失、coordinate 不匹配、不同 ID 替换、range 缺失、无生命能力的失败纯度。
- Recover 三格 timeline shape 在 x=9 合法、x=10 越界且不占格。
- Application 精确 occupant 选择、任意匹配 occupant（不只旧 primary target）、满血/ID/coord/range 非法。
- Commit 后 occupant 消失或变满血的 Resolve 重判。
- occupant before/after 结果与 Recover trace。
- `poison` 继续作为真正未注册 effect fail-fast；Recover `value=0` 作为非法 payload 在占格前 fail-fast。
- lighting、earthquake、排序、enemy marker、seed、重复 preview/commit/resolve、取消和 dispose 原回归仍在同一过滤集内。

## 4. 验证结果

### 纯 C# 编译

使用 Unity 6000.4.10f1 bundled Mono Roslyn，`/warnaserror+` 依次编译：

- 全部 `Runtime/Domain/**/*.cs`：通过。
- 全部 `Runtime/Application/**/*.cs`（引用新 Domain）：通过。
- Diagnostics 与本次三个过滤测试类：通过。

### Unity EditMode filter

唯一 Unity 实例，工程入口 `D:\timekey-unity-731`：

```text
TimeKey.Tests.EditMode.Effects.RecoverCardEffectHandlerTests;
TimeKey.Tests.EditMode.Application.CombatApplicationSessionTests;
TimeKey.Tests.EditMode.TimelineGridTests
```

最终 XML：`unity/Temp/remaining-cards-agent-a-editmode-results.xml`

```text
total=40 passed=40 failed=0 skipped=0 inconclusive=0
```

Unity 6 在退出时清理了请求写入 Temp 的即时副本，但同一次 run 同时生成了 fresh LocalLow `TestResults.xml`；验证脚本检查时间戳后把该同-run XML 复制回 reviewed prompt 要求的 Temp 路径并再次解析上述计数。对应 log 为 `unity/Temp/remaining-cards-agent-a-editmode.log`。

### 静态门禁

- `git diff --check`：通过。
- `git diff -- unity/Assets/_Project/Runtime/Presentation/VerticalSliceController.cs`：空。
- Domain/Application `UnityEngine|UnityEditor` 扫描：空。
- Unity 全工程 script compilation：通过后才执行 40 个过滤测试。

## 5. 非目标与交回

未实现 Infrastructure support registration、Composition fixture/Prefab、Presenter/UI/VFX、Built、Poison、Clear 或共享维护文档；这些继续由主智能体和后续互斥 Agent 集成。本 Agent 未切分支、stash、暂存、commit、push 或清理工作区。

当前没有 Controller/Presenter/程序集方向验收失败，也没有用户决策阻塞。上述文件所有权现交回主智能体。
