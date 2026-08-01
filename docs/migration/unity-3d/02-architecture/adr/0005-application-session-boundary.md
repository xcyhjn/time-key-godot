# ADR 0005：Application 会话边界

> 状态：Accepted
> 日期：2026-08-01

## 背景

R1 已将稳定场景与 Prefab 序列化，但 `VerticalSliceController` 仍直接持有 `CardPlaySession`、`TimelineGrid`命令顺序、选中卡/目标/时间轴原点和结算调用。用例无法脱离 Unity View 测试，资源、交互与规则状态也容易分叉。

## 决策

- 新增无 Unity 引用的 `TimeKey.Application` asmdef，仅依赖 `TimeKey.Domain`。
- 用一个具体 `CombatApplicationSession` 组织选卡、typed target、preview、commit、cancel 和 resolve；不为每个用例制造接口。
- `ICardCatalog` 和 `ICombatTraceSink` 由 Application 拥有，Infrastructure/Diagnostics 实现。
- `CombatTarget` 显式区分 entity/tile；目标类型从 effect kind 推导，不从 stable ID 分支。
- Application 返回不可变 `CombatCommandResult`/`CombatSessionView`，Presentation 只消费结果同步 View。
- `UnsupportedCardEffectException` 在用例边界转成 `UnsupportedEffect`，不支持效果在玩家 action 占格前失败。
- 初始敌方 intent 由 session 构造时一次性放置；Controller 只渲染已组装的 intent。
- `VerticalSliceController` 保留原公共方法/属性作为兼容 facade，不再直接创建或命令 `CardPlaySession`。

## 后果

Application/Diagnostics 可在纯 EditMode 环境测试，sink 抛异常也不改变战斗快照。Controller 暂时仍负责两卡 fixture 解析、卡图加载、UI 文案/颜色和世界表现同步；R3 必须用 catalog/composition 与窄 Presenter 移除这些职责，并删除临时 `Presentation -> Infrastructure` 依赖。
