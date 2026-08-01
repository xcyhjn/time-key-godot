# Turn Lifecycle Agent B2：Card/Timeline/Intent Presentation

> 单一目标：在 Gate A snapshot 与 B1 intent 契约冻结后，实现响应式七卡、卡牌详情框、按 ActionId 分组的玩家/敌人 action frame、地图双向 hover 与统一清理；只负责指定 Presentation/Prefab/PlayMode 路径。

## 必读

- 本阶段主 Prompt、交互视觉审查、Gate A/B1 报告与冻结 snapshot
- Godot card presenters、`TimelineActionShapeVisual.gd`、timeline UI presenters、enemy intent presentation controller
- 当前 CardHand、Timeline/Board Presenter、Binding 和 PlayMode tests

## 独占拥有路径

- `Runtime/Presentation/Cards/**`
- 新目录 `Runtime/Presentation/Actions/**`、`Runtime/Presentation/Tooltips/**` 及 `.meta`
- `Runtime/Presentation/Presenters/CardHandPresenter.cs`
- `Runtime/Presentation/Presenters/TimelinePresenter.cs`
- 新的 action/detail/map overlay Presenter 文件及 `.meta`
- 新 `Prefabs/Battle/UI/CardEffectFrame.prefab`、`TimelineActionFrame.prefab` 及 `.meta`
- `Tests/PlayMode/Cards/**` 与新 `Tests/PlayMode/Actions/**`、`Tooltips/**`
- 独占报告与专用非最终视觉证据目录

## 禁止路径

Domain、Application、Infrastructure、Composition、`CombatPresentationBinding.cs`、`VerticalSliceController.cs`、共享 Scene、Editor authoring/harness、asmdef、共享 docs/evidence/Git。Scene 接线需求交回主线程。

## 冻结契约

- 所有规则与显示数据来自同一 immutable snapshot；Presenter 只维护 `ActionId -> View` 和 `cell -> ActionId`。
- 七卡所有状态互斥；选中最高层；取消/失败/commit/disable/rebind 恢复 parent/sibling/scale/rotation/raycast，不漂移或复制。
- 1280 全部卡可点且邻卡关键区不被吞；1920/2560 围绕既定中心并使用可序列化 min/max width、spacing、selected reserved extent。
- 详情框与 action frame 必须来自保存 Prefab；一 ActionId 一整体底板/外轮廓/弱 seam，任一格 hover 整组。
- enemy 复用容器但必须有 stripe/pulse/source badge 等非纯颜色编码；unsupported/invalid/resolving 可辨。
- overlay 优先级与清理由单一 coordinator 执行，clear/death/revalidation/rebind 幂等清所有映射和 tooltip。

## 非目标、测试与视觉

不生成 intent、不重算目标/伤害/合法性、不修改 Scene/Composition。测试三视口七卡首中末 hover/selected/drag/cancel/disable、详情边界、1x1/多格/相邻同色 action、任一格 hover、clear/death 清理、enemy valid/unsupported/invalid、双入口同 ActionId。专用截图需人工打开；最终 Scene/harness 由主线程完成。

## 停止与 Git

若冻结 snapshot 缺字段或必须写共享 Scene/Binding，停止该点并报告主线程，不在 Presentation 反推。不得回退、切分支、stash、暂存、commit、push。
