# Unity 局内战斗模块架构

> 状态：R2 已实现；R3 Composition/Presenter 执行前基线
> 最后验证日期：2026-08-01

## 依赖方向

```text
TimeKey.Domain          -> []
TimeKey.Application     -> Domain
TimeKey.Infrastructure  -> Domain, Application
TimeKey.Diagnostics     -> Domain, Application
TimeKey.Presentation    -> Domain, Application, Infrastructure  # R3 移除 Infrastructure
TimeKey.Editor          -> Domain, Infrastructure, Presentation
```

`Domain` 和 `Application` 的 `noEngineReferences=true`，两者对 `UnityEngine`、Presentation 和 Infrastructure 引用均为 0。禁止 `Domain -> Application`、`Application -> Infrastructure`、`Presentation -> Infrastructure` 的最终形态，以及任何反向查询 View/Prefab 的依赖。

## 运行调用链

```text
serialized Scene / input intent
  -> VerticalSliceController compatibility facade
  -> CombatApplicationSession command
  -> CardPlaySession + TimelineGrid + ICardEffectHandler
  -> CombatCommandResult / ResolutionSnapshot
  -> Controller (R2) or Presenter (R3) updates views
  -> optional ICombatTraceSink
```

`CombatApplicationSession` 是用例状态的唯一所有者。它消费 `ICardCatalog`，持有选卡、typed target、timeline origin、commit/resolve phase，并返回不可变 view/result。`TimelineGrid` 仍是占格和结算顺序的唯一来源；`ICardEffectHandler` 是效果结算变化点。

## 模块所有权

| 模块 | 拥有 | 不得拥有 |
| --- | --- | --- |
| Domain | 格坐标、卡牌数据、时间轴合法性、效果处理、战斗快照 | Unity 类型、资源路径、UI |
| Application | 命令顺序、typed target、会话 phase、ports、结构化失败 | JSON/Resources、Prefab、规则重算 |
| Infrastructure | JSON adapter、卡牌 catalog 和结构化资源定位 | 玩法结果、View |
| Diagnostics | no-op/collecting/Unity trace sink | 影响 seed、状态或命令成败 |
| Presentation | 输入映射、View state、相机、范围/时间轴表现 | JSON 解析、stable ID 玩法分支、占格规则 |
| Composition | Scene Inspector 引用、catalog/trace/session 组装和生命周期 | Domain 规则 |

## 扩展变化点

新普通卡牌应只增加 fixture/原图/catalog 条目和针对性测试，不修改 Controller。新效果修改 typed payload/adapter、Domain handler 及目标策略，不在 Presenter 写结算。新敌人尚未实现；必须先有纯数据实体与权威 Godot 意图契约。

## R3 必须关闭的临时债

- 将七卡 catalog/front-image 定位移出 Controller。
- 增加 Composition 组装边界，移除 `Presentation -> Infrastructure`。
- 将 CardHand、Timeline、Board/HUD 重复的表现协调交给窄 Presenter/Binding。
- 把 trace 扩展为包含 effect kind 和 before/after，并接入可关闭 Unity sink。
- 用场景、Prefab、PlayMode 和实际截图证明组装与生命周期。
