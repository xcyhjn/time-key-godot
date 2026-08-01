# 添加局内机制

> 原则：先确定规则归属，再扩大最小修改面

## 归属判断

| 问题 | 模块 |
| --- | --- |
| 固定输入应得到什么战斗结果？ | Domain |
| 玩家命令在什么阶段允许、失败如何返回？ | Application |
| JSON、资源、存储或外部格式如何进入纯模型？ | Infrastructure |
| 输入、布局、动画、高亮或 3D 同步如何显示？ | Presentation |
| Scene 中如何组装并管理生命周期？ | Composition |
| 如何记录而不改变结果？ | Diagnostics |

不要为每个类增加接口。只有真实外部依赖、变化点或测试替身才定义 port；现有入口优先复用 `CombatApplicationSession`、`ICardCatalog` 和 `ICombatTraceSink`。

## 最小实施流程

1. 从 Godot 权威代码和可观察行为冻结契约、非目标和边界值。
2. 在 Domain 写纯 EditMode 特征测试与规则，保持零 `UnityEngine`。
3. 仅在需要新命令/phase 时扩展 Application 的不可变 result/view；失败必须无副作用。
4. 外部数据通过 Infrastructure 适配；Presenter 只映射意图和结果，不复制合法性。
5. 在 `CombatCompositionRoot` 增加显式构造/Inspector 接线，绑定和解除绑定必须对称。
6. 更新 `combat-modular-architecture.md`、必要 ADR、维护指南和 parity；再跑受影响 filter 与完整终验。

## Inspector、视觉与故障

稳定控件/锚点进入 Scene 或 Prefab；运行数量才动态的对象来自已保存 Prefab/factory。缺失引用应在启动或 `OnValidate` 给出定位错误。UI/世界表现变化至少刷新三视口；涉及选取或地形时刷新四向 yaw；不能用测试绿替代实际渲染证据。

常见失败包括把 Domain identity 绑到 `GameObject`、在 Presenter 重算规则、Composition 变成 Service Locator、事件只订阅不解除、trace 影响命令结果，以及提前实现局外/敌人规则。按调用链分层断点，先修最小失败层。

回滚使用单一机制检查点，从 Composition 接线向内撤销，保证旧 Application 公共面和既有 Scene 引用仍可运行；不要覆盖其他代理或用户改动。
