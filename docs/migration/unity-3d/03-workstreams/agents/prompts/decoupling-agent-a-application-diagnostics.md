# Agent A Prompt：Application 与 Diagnostics

## 角色与目标

你负责 R2 的纯 C# Application 用例和最小 Diagnostics 契约。目标是把卡牌选择、目标确认、Timeline 预览/提交/结算编排从 Controller 抽出，同时保持现有公共调用面可由主智能体转接。

## 必读

- `00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md`
- `03-workstreams/agents/reports/decoupling-domain-application-audit.md`
- `04-verification/inherited-verification-ledger.md`
- 当前 Domain、Timeline、Terrain 与 Controller 公共方法

## 使用规范

遵循 `codebase-migrate`、`architecture-review` 与 `Verification & Quality Assurance`；先写 characterization/contract tests，再写最少实现。

## 独占可写路径

- `unity/Assets/_Project/Runtime/Application/**`
- `unity/Assets/_Project/Runtime/Diagnostics/**`
- `unity/Assets/_Project/Tests/EditMode/Application/**`
- `unity/Assets/_Project/Tests/EditMode/Diagnostics/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/decoupling-agent-a-report.md`

## 禁止路径

不得修改 Controller、Domain、Infrastructure、Presentation、Scene、Prefab、任何 asmdef、harness、共享架构/进度文档、用户保护清单或其他智能体路径。不得运行 Unity、暂存、提交、推送或切分支。

## 冻结契约

- 一个具体 Application session；不要为每个用例制造接口。
- 仅允许真实变化点：`ICardCatalog`、`ICardEffectHandler`、`ICombatTraceSink`。
- Application 不引用 UnityEngine、Infrastructure 或 Presentation。
- handler 未注册/不支持必须返回结构化失败；Recover/Built/Poison/Clear 不得静默 no-op。
- 初始化、重置和 dispose/bind 生命周期必须可重复且不泄漏监听。

## 步骤

1. 以现有 Controller 调用链写纯 C# characterization tests。
2. 实现最小 session、请求/结果与失败类型。
3. 加入 handler 注册验证和结构化 trace 事件。
4. 运行可在当前程序集上下文运行的静态/纯 C# 检查；Unity 测试由主智能体统一执行。
5. 写独占报告，列明 API、测试、风险和主智能体接线点。

## 非目标

不改变规则数值、Scene、UI、资源加载、卡牌目录格式、敌人能力或章节流程。

## 验收

- 用例不依赖 Unity。
- 两卡冻结路径的选择、目标、预览、提交、结算可由 Application 表达。
- 未注册效果显式失败并有 trace。
- 不新增无真实替换点的接口。

## 回报格式

只报告改动文件、API 契约、测试覆盖、遗留接线点和保护清单状态。
