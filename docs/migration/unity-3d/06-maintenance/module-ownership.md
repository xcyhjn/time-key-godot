# 模块所有权与修改检查表

> 状态：解耦 R3 冻结

| 程序集 | 拥有 | 公开变化点 | 允许依赖 | 禁止依赖 |
| --- | --- | --- | --- | --- |
| `TimeKey.Domain` | 坐标、卡牌 typed schema、时间轴、效果、战斗状态、ActionId、回合 lifecycle runner | handler、纯模型与窄 phase port | 无 | Unity、资源路径、UI、Application |
| `TimeKey.Application` | 选卡、typed target、预览、提交、取消、结算 phase、不可变 action presentation snapshot | `CombatApplicationSession`、`ICardCatalog`、`ICombatTraceSink` | Domain | Unity、Infrastructure、View、Resources |
| `TimeKey.Infrastructure` | JSON adapter、内容目录、`front_image` 定位、效果注册清单 | catalog/adapter | Domain、Application | 玩法结果、Presenter |
| `TimeKey.Diagnostics` | no-op/collecting trace 实现 | `ICombatTraceSink` 实现 | Domain、Application | 改变状态、Unity |
| `TimeKey.Presentation` | Presenter、Binding、View、输入、镜头和 3D 同步 | 意图事件、只读刷新 | Domain、Application | Infrastructure、JSON、stable-ID 玩法分支 |
| `TimeKey.Composition` | Scene 入口、Inspector 引用、对象组装、资源加载和生命周期 | `CombatCompositionRoot` | 上述运行模块 | 领域规则、全局 locator |
| `TimeKey.Editor` | authoring、harness、截图和 build 自动化 | Editor 菜单/execute method | 运行模块 | Player 玩法规则 |

依赖方向必须保持单向且无环：Domain <- Application <- Infrastructure/Diagnostics，Presentation 只面向 Domain/Application，Composition 在最外层组装。

## 修改前检查

- 是否先从真实调用者证明需要新接口，而不是按类机械抽象？
- 规则是否仍由 Domain 唯一决定，Application 是否只编排？
- 新卡是否只增加数据/素材/注册/测试，没有 Controller stable-ID 分支？
- 稳定对象是否在 Scene/Prefab，动态对象是否来自已保存 Prefab/factory？
- Binding 生命周期是否对称，重复初始化是否幂等？
- trace sink 关闭、替换或抛错时，结果是否完全相同？
- action 是否由 lifecycle sequence + ordinal 分配，并在 preview/commit/resolve/clear/View 映射中保持同一 `TimelineActionIdentity`？
- lifecycle processor 是否只通过窄 port 接入，且没有把 Tower、Poison 或敌人规则写进 Runner？
- 受影响的 EditMode、PlayMode、harness/build/Player 和截图是否已刷新？
- staged 文件是否排除 Prompt、Godot 用户脏文件和来源不明证据？

真实场景入口为 `CombatVerticalSlice.unity`，主要组装类为 `CombatCompositionRoot`，兼容 facade 为 `VerticalSliceController`。新增敌人与章节桥接尚未实现，分别遵循 `add-enemy.md` 与 `chapter-boundary.md` 的停止边界。

依赖或职责发生长期变化时先更新架构文档和 ADR。回滚只撤销对应模块的单一目的检查点；不得用 asmdef 反向引用来绕过编译错误。
