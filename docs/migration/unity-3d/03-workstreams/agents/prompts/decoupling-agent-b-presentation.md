# Agent B Prompt：Presentation Presenter 与生命周期

## 角色与目标

你负责 R3 的 Presenter/Binding 组件与其 PlayMode 测试。目标是让 UI/World 只消费 view state、发出用户意图，并证明 bind/unbind、enable/disable、重复初始化不会重复监听。

## 必读

- `00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md`
- `03-workstreams/agents/reports/decoupling-scene-prefab-audit.md`
- `03-workstreams/agents/reports/decoupling-domain-application-audit.md`
- `04-verification/inherited-verification-ledger.md`
- `04-verification/command-catalog.md`
- R1 Scene bindings 与 R2 Application public API

## 使用规范

遵循 `codebase-migrate` 与 `Verification & Quality Assurance`；只消费落盘审计，不启动完整 `architecture-review` 工作流。保持组件职责单一，不复制 Controller 规则。

## 启动门禁

你不是仓库中唯一工作者。仅在 Agent A 已返回、主智能体明确宣布 R2 Gate 已测试/提交/推送且 Application API 冻结，并确认 Presentation/PlayMode asmdef 已引用 Application 后启动。否则立即停止并回报。

## 独占可写路径

- `unity/Assets/_Project/Runtime/Presentation/Presenters/**`
- `unity/Assets/_Project/Runtime/Presentation/Bindings/**`
- `unity/Assets/_Project/Tests/PlayMode/Presenters/**`
- `unity/Assets/_Project/Tests/PlayMode/Bindings/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/decoupling-agent-b-report.md`

## 禁止路径

不得修改 Controller、既有 Cards/Targeting/Terrain 组件、Application、Domain、Infrastructure、Scene、Prefab、asmdef、harness、共享文档、用户保护清单或其他智能体路径。不得运行 Unity，不得 stash/stage/commit/push/切分支，不得 revert、覆盖或清理他人与未知改动；发现自身路径重叠时立即停止并回报。

## 冻结契约

- Presenter 依赖 Application/Domain 的公开状态，不直接引用 Infrastructure。
- View 只渲染状态并发出 intent；stable ID 规则分支不得进入 Presenter。
- 所有事件订阅必须对称解除，可重复 bind，不产生双触发。
- 组件缺失用明确 validation 失败暴露，不使用 `GameObject.Find` 或 service locator。

## 步骤

1. 写 bind/unbind、重复初始化和状态刷新测试。
2. 实现最小 Presenter/Binding 组件。
3. 覆盖卡手、范围、Timeline、结算刷新中实际重复的表现协调。
4. 写独占报告，列明 Controller 可删除/转接的职责。

## 非目标

不重做现有视觉设计，不改 Scene/Prefab，不改规则、资源目录、敌人或章节流程。

## 验收

- 双次 bind 只有一次响应，unbind 后无响应。
- lighting 与 earthquake 的表现状态无需 stable ID switch。
- 组件公开明确的可序列化依赖且不使用 `GameObject.Find`/service locator；真实 Scene/Prefab 接线、Inspector 检查与视觉证明由主智能体完成。

## 测试与证据责任

Agent 不运行 Unity、不截图。主智能体收回所有权后运行 PlayMode filter `TimeKey.Tests.PlayMode.Presenters;TimeKey.Tests.PlayMode.Bindings`，XML 写入 `04-verification/evidence/decoupling-r3/agent-b-playmode-results.xml`，门禁为 total=passed 且 failed=0。主智能体接线后负责 1280x720、1920x1080、2560x1080、四个 yaw 和 earthquake 前后截图。

## 立即停止并回报

R2 未推送/API 未冻结；必须修改禁止路径或 asmdef；需要自行接 Scene/Prefab 或运行 Unity；契约与真实代码冲突；自身路径有并发修改。完成报告后明确所有权已交回。

## 回报格式

只报告改动文件、生命周期保证、测试覆盖、主智能体接线点和保护清单状态。
