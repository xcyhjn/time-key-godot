# Turn Lifecycle Agent C2：Occupant/Status 生命周期表现

> 单一目标：在 C1 typed result 冻结后，只用结果驱动 Tower HP/退场、Poison icon/stacks 与 intent/frame 清理的 PlayMode 同步测试和必要 Prefab 调整。

## 必读

- 本阶段主 Prompt、C1 报告/result API、交互视觉审查
- 当前 `CombatOccupantPresenter`、Tower/PoisonStatus Prefab、Action/Intent Presenter API

## 独占拥有路径

- `Runtime/Presentation/Occupants/**`
- `Runtime/Presentation/Presenters/CombatOccupantPresenter.cs`
- `Prefabs/Battle/Occupants/Tower.prefab`、`Prefabs/Battle/Status/PoisonStatus.prefab` 及其现有 `.meta`（仅必要调整）
- 新 `Tests/PlayMode/Occupants/Lifecycle/**`
- 独占报告与专用非最终视觉证据目录

## 禁止路径

Domain/Application/Infrastructure/Composition、Controller、Binding、共享 Scene、Editor、asmdef、Card/Timeline/Intent Presentation 所有权、共享 docs/evidence/Git。

## 冻结契约

- Presenter 只消费 typed lifecycle result；不得计算 Tower damage、Poison 公式、death policy 或 target validity。
- Tower removal 同步销毁 View/collider/status/anchor mapping；重复 result 幂等。
- Poison stacks 0 时移除 icon；occupant removal 时 status 与相关 intent/action overlay 清理请求使用同一 runtime ID/ActionId，不留残影。
- Prefab 调整保持 Silver 字体、源素材和 Inspector 可编辑性；不新增运行时整树 fallback。

## 测试、停止与 Git

覆盖 Tower 100/50/0、Poison spread/damage/decay/new infection、death/removal、disable/enable/rebind、四 yaw anchor 一致与零重复 View/订阅。共享 Scene/Binding 接线交回主线程。不得回退、切分支、stash、暂存、commit、push。
