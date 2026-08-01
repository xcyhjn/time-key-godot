# Agent A Prompt：Application 与 Diagnostics

## 角色与目标

你负责 R2 的纯 C# Application 用例和最小 Diagnostics 契约。目标是把卡牌选择、目标确认、Timeline 预览/提交/结算编排从 Controller 抽出，同时保持现有公共调用面可由主智能体转接。

## 必读

- `00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md`
- `03-workstreams/agents/reports/decoupling-domain-application-audit.md`
- `04-verification/inherited-verification-ledger.md`
- `04-verification/command-catalog.md`
- 当前 Domain、Timeline、Terrain 与 Controller 公共方法

## 使用规范

遵循 `codebase-migrate` 与 `Verification & Quality Assurance`；只消费已落盘架构审计，不启动完整 `architecture-review` 工作流，不请求用户决策，不写共享架构文档。先写 characterization/contract tests，再写最少实现。

## 启动门禁

你不是仓库中唯一工作者。仅在主智能体明确宣布 R1 commit `8c8d5b1` 已推送、无 Unity 进程，并确认下列 asmdef 已存在且可编译后启动：`TimeKey.Application`、`TimeKey.Diagnostics`、EditMode 对二者的引用。若事实不符，立即停止并回报。

## 独占可写路径

- `unity/Assets/_Project/Runtime/Application/**`
- `unity/Assets/_Project/Runtime/Diagnostics/**`
- `unity/Assets/_Project/Tests/EditMode/Application/**`
- `unity/Assets/_Project/Tests/EditMode/Diagnostics/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/decoupling-agent-a-report.md`

## 禁止路径

不得修改 Controller、Domain、Infrastructure、Presentation、Scene、Prefab、任何 asmdef、harness、共享架构/进度文档、用户保护清单或其他智能体路径。不得运行 Unity，不得 stash/stage/commit/push/切分支，不得 revert、覆盖或清理他人与未知改动；发现自身路径出现并发或未知修改时立即停止并回报。

## 冻结契约

- 一个具体 Application session；不要为每个用例制造接口。
- `ICardCatalog` 已冻结在 `TimeKey.Application`，由 Application session 消费，R3 由 Infrastructure 实现；不得改签名。
- `ICombatTraceSink` 与 `CombatTraceEntry` 已冻结在 `TimeKey.Application`，由 session 生产事件；`NoOpCombatTraceSink` 已在 Diagnostics，Agent 只补 collecting test sink/所需行为，不得改端口签名。
- `ICardEffectHandler` 已定义在 `TimeKey.Domain`，消费者是 `TimelineGrid`，实现/注册与现有 Domain 接线均归主智能体。你只把 `UnsupportedCardEffectException` 转成结构化 Application 失败，不得重定义或修改 handler。
- Application 不引用 UnityEngine、Infrastructure 或 Presentation。
- handler 未注册/不支持必须返回结构化失败；Recover/Built/Poison/Clear 不得静默 no-op。
- 初始化、重置和 dispose/bind 生命周期必须可重复且不泄漏监听。

## 步骤

1. 以现有 Controller 调用链写纯 C# characterization tests。
2. 实现最小 session、请求/结果与失败类型。
3. 加入 handler 注册验证和结构化 trace 事件。
4. 完成源码后交回所有权；Unity 测试由主智能体统一执行。
5. 写独占报告，列明 API、测试、风险和主智能体接线点。

## 非目标

不改变规则数值、Scene、UI、资源加载、卡牌目录格式、敌人能力或章节流程。

## 验收

- 用例不依赖 Unity。
- 两卡冻结路径的选择、目标、预览、提交、结算可由 Application 表达。
- 未注册效果显式失败并有 trace。
- 不新增无真实替换点的接口。

## 测试与证据责任

Agent 不运行 Unity、不截图。主智能体收回所有权后按 `command-catalog.md` 使用 Unity 6000.4.10f1、`D:\timekey-unity-731`，运行 EditMode filter `TimeKey.Tests.EditMode.Application;TimeKey.Tests.EditMode.Diagnostics`，XML 写入 `04-verification/evidence/decoupling-r2/agent-a-editmode-results.xml`，门禁为 total=passed 且 failed=0；随后主智能体跑完整 EditMode/PlayMode 并刷新集成截图。

## 立即停止并回报

上游 checkpoint/API/asmdef 未冻结；必须修改禁止路径才能编译；真实代码与冻结接口冲突；需要运行 Unity 或修改 Scene/Prefab；发现路径重叠；测试无法由主智能体复现。完成报告后明确声明独占路径所有权已交回。

## 回报格式

只报告改动文件、API 契约、测试覆盖、遗留接线点和保护清单状态。
