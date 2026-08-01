# 解耦阶段写入智能体 Prompt 审查

> 状态：第二次独立复核通过；三份写入型 Agent Prompt 可按各自启动门禁使用
> 负责人：主智能体
> 独立复核：decoupling-domain-application-audit Agent
> 最后复核日期：2026-08-01
> 唯一规范：`00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md`

## 复核结论

三份 Prompt 已关闭上次复核的 7 项问题，并满足主 Prompt 对写入型 Agent 的自包含约束。角色目标、必读、独占路径、禁止路径、冻结契约、非目标、测试/截图责任、停止条件、回报格式和 Git/共享工作区禁令均已写入各自 Prompt；三份代码、测试和报告路径继续保持互斥。

结论为 **PASS**。Agent A 可在主智能体再次确认其 Prompt 所列 R1/asmdef/Unity 进程门禁后启动。Agent B 与 Agent C 仍不得提前启动；它们必须等待 Agent A 返回，以及主智能体完成并宣布 R2 测试、提交、推送和 Application API 冻结。PASS 只表示 Prompt 合格，不替代各波启动门禁。

## 当前事实核验

| 核验项 | 直接证据 | 结果 |
| --- | --- | --- |
| 分支与 R1 checkpoint | `git branch -vv` 显示 `unity_7.31` 的本地 `HEAD` 与 `origin/unity_7.31` 均为 `8c8d5b1` | 通过 |
| Application asmdef | `Runtime/Application/TimeKey.Application.asmdef` 存在，仅引用 `TimeKey.Domain`，`noEngineReferences=true` | 通过 |
| Diagnostics asmdef | `Runtime/Diagnostics/TimeKey.Diagnostics.asmdef` 存在，引用 Domain/Application，`noEngineReferences=true` | 通过 |
| 测试程序集前置 | EditMode 引用 Application/Diagnostics；Infrastructure tests 引用 Application；PlayMode 与 Presentation 引用 Application | 通过 |
| Application ports | `ICardCatalog`、`ICombatTraceSink` 与 `CombatTraceEntry` 位于 `TimeKey.Application`；`NoOpCombatTraceSink` 位于 Diagnostics | 通过 |
| Domain handler 归属 | `ICardEffectHandler` 与 `UnsupportedCardEffectException` 位于 `TimeKey.Domain`，`TimelineGrid` 消费该异常/handler 契约 | 通过 |
| Domain filter XML | `04-verification/evidence/decoupling-r2/domain-effect-contract-results.xml`：`total=13`、`passed=13`、`failed=0` | 通过 |
| 依赖方向 | Domain 无下游引用；Application -> Domain；Diagnostics -> Application/Domain；Infrastructure -> Application/Domain；Presentation 当前引用 Application/Domain/Infrastructure；未形成环 | 通过 |

## 上次 7 项问题关闭情况

| 编号 | 上次问题 | 当前修正 | 结果 |
| --- | --- | --- | --- |
| 1 | 完整 `architecture-review` 工作流会越界 | 三份 Prompt 均限定只消费已落盘审计，不启动完整工作流；Agent A 还明确不请求用户决策、不写共享架构文档 | 已关闭 |
| 2 | 缺少精确测试与截图责任 | 三份 Prompt 均引用 `command-catalog.md`，给出精确 filter、XML 路径和 `total=passed`、`failed=0` 门禁；明确 Agent 不运行 Unity/不截图以及主智能体的截图责任 | 已关闭 |
| 3 | 缺少自包含启动 Gate、停止条件和所有权交回 | 三份 Prompt 均新增“启动门禁”“立即停止并回报”，覆盖上游未冻结、禁止路径、契约冲突、路径重叠、Unity/Scene/asmdef 越界，并要求报告后声明所有权交回 | 已关闭 |
| 4 | Agent A asmdef 前置与 handler 归属未冻结 | Agent A Gate 明确要求 Application/Diagnostics/EditMode 引用先存在且可编译；冻结契约明确 catalog/trace 在 Application、handler/异常在 Domain，现有仓库事实与之吻合 | 已关闭 |
| 5 | Agent B 无法独立证明真实 Scene 接线 | Agent B 只负责可序列化依赖、生命周期和测试源；真实 Scene/Prefab 接线、Inspector、PlayMode 与视觉证据明确归主智能体 | 已关闭 |
| 6 | Agent C 可能在 Infrastructure 实现玩法规则 | Agent C 明确限定 `Infrastructure/Effects/**` 只能写 registration descriptor/装配数据，禁止目标验证、效果计算和 Domain 状态写入 | 已关闭 |
| 7 | Git 与共享工作区禁令不完整 | 三份 Prompt 均声明不是唯一工作者，并禁止 stash/stage/commit/push/切分支、revert、覆盖或清理他人与未知改动 | 已关闭 |

## 逐 Prompt 门禁

| Prompt | 单一目标与必读 | 路径互斥 | 冻结契约 | 启动/停止条件 | filter / XML / 0 failure | 截图责任 | 协作与 Git | 结论 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Agent A Application/Diagnostics | 通过 | 通过 | 通过 | 通过 | `TimeKey.Tests.EditMode.Application;TimeKey.Tests.EditMode.Diagnostics` / `decoupling-r2/agent-a-editmode-results.xml` / `failed=0` | Agent 不截图；主智能体刷新集成视图 | 通过 | PASS |
| Agent B Presentation | 通过 | 通过 | 通过 | 通过 | `TimeKey.Tests.PlayMode.Presenters;TimeKey.Tests.PlayMode.Bindings` / `decoupling-r3/agent-b-playmode-results.xml` / `failed=0` | 主智能体负责三分辨率、四 yaw、earthquake 前后 | 通过 | PASS |
| Agent C Infrastructure/Content | 通过 | 通过 | 通过 | 通过 | `TimeKey.Tests.Infrastructure.Cards;TimeKey.Tests.Infrastructure.Effects` / `decoupling-r3/agent-c-editmode-results.xml` / `failed=0` | 主智能体刷新两卡原卡面、完整手牌和资源映射 | 通过 | PASS |

## 路径与波次证明

- Agent A 独占 `Runtime/Application/**`、`Runtime/Diagnostics/**` 及对应 EditMode tests。
- Agent B 独占 `Presentation/Presenters/**`、`Presentation/Bindings/**` 及对应 PlayMode tests。
- Agent C 独占 `Infrastructure/Cards/**`、`Infrastructure/Effects/**`、`Data/Catalogs/**` 及对应 Infrastructure tests。
- 三份报告文件互异；Controller、既有 Presentation、Domain、Scene、Prefab、asmdef、harness、共享文档、最终证据和 Git 均保留给主智能体。
- Wave 2 只允许 Agent A 在 R1/asmdef Gate 后启动；Wave 3 只允许 Agent B/C 在 Agent A 返回且 R2 测试、提交、推送、API 冻结后并行启动。

## 独立复核边界

本次复核只读取三份 Prompt、当前 asmdef/端口/Domain handler、R1 Git 状态和 Domain XML，并只修改本审查文件；未修改三份 Prompt、实现、asmdef、共享迁移文档或 Git 状态，未运行 Unity。审查文件所有权现已交回主智能体。
