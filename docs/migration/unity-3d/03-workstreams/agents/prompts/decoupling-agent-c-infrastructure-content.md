# Agent C Prompt：Infrastructure Catalog 与 Content 边界

## 角色与目标

你负责 R3 的卡牌 catalog、资源定位和 handler 组装所需 Infrastructure/Content 边界。目标是新增卡牌或替换卡面时不再修改 Controller，也不把 Unity Resources 细节泄漏到 Application。

## 必读

- `00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md`
- `03-workstreams/agents/reports/decoupling-extension-docs-audit.md`
- `03-workstreams/agents/reports/decoupling-domain-application-audit.md`
- `04-verification/inherited-verification-ledger.md`
- `04-verification/command-catalog.md`
- R2 的 `ICardCatalog`/handler 契约与当前 CardJsonAdapter/七卡 fixtures

## 使用规范

遵循 `codebase-migrate` 与 `Verification & Quality Assurance`；只消费落盘审计，不启动完整 `architecture-review` 工作流。使用结构化 JSON/API，不进行字符串拼补。

## 启动门禁

你不是仓库中唯一工作者。仅在 Agent A 已返回、主智能体明确宣布 R2 Gate 已测试/提交/推送且 `ICardCatalog` API 冻结，并确认 Infrastructure/Infrastructure tests 已引用 Application 后启动。否则立即停止并回报。

## 独占可写路径

- `unity/Assets/_Project/Runtime/Infrastructure/Cards/**`
- `unity/Assets/_Project/Runtime/Infrastructure/Effects/**`
- `unity/Assets/_Project/Tests/Infrastructure/Cards/**`
- `unity/Assets/_Project/Tests/Infrastructure/Effects/**`
- `unity/Assets/_Project/Data/Catalogs/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/decoupling-agent-c-report.md`

## 禁止路径

不得修改根级 CardJsonAdapter、现有七卡 JSON、Controller、Application、Domain、Presentation、Scene、Prefab、asmdef、harness、共享文档、用户保护清单或其他智能体路径。不得运行 Unity，不得 stash/stage/commit/push/切分支，不得 revert、覆盖或清理他人与未知改动；发现自身路径重叠时立即停止并回报。

## 冻结契约

- 实现 R2 冻结的 `ICardCatalog`，输入是七张现有结构化 fixture/目录描述，不把 `TextAsset`/`Sprite` 类型暴露给 Application。
- catalog 以 stable ID 驱动数据与 front-image 资源描述；Controller 不再维护两卡 fixture switch。
- 七卡均可被枚举、校验、定位；重复 stable ID、缺失 front_image 和未知 effect 必须显式失败。
- `Infrastructure/Effects/**` 只允许 handler registration descriptor/装配数据；不得验证目标、计算效果或写 Domain 状态。实际玩法 handler 属于 Domain。
- 实际 Unity 对象加载留在 Composition/Presentation 适配层；此路径只提供内容和结构化定位。

## 步骤

1. 写 catalog 正常与失败测试。
2. 实现最小 catalog/资源描述与 effect handler registration 数据。
3. 证明新增一张 fixture/目录项不需修改现有路由代码。
4. 写独占报告，列明主智能体接线与资源验证步骤。

## 非目标

不实现剩余五卡真实效果，不新增敌人/章节能力，不移动或改写现有资源，不改场景。

## 验收

- 七卡 catalog 唯一且完整。
- front_image 映射是数据驱动并有失败测试。
- 新增卡/效果注册无需 Controller stable-ID 分支。

## 测试与证据责任

Agent 不运行 Unity、不截图。主智能体收回所有权后运行 EditMode filter `TimeKey.Tests.Infrastructure.Cards;TimeKey.Tests.Infrastructure.Effects`，XML 写入 `04-verification/evidence/decoupling-r3/agent-c-editmode-results.xml`，门禁为 total=passed 且 failed=0。两卡原卡面、完整手牌与资源映射截图由主智能体集成接线后刷新。

## 立即停止并回报

R2 未推送/API 未冻结；必须修改禁止路径、现有 JSON 或 asmdef；需要实现玩法结果/写 Domain；需要运行 Unity/修改 Scene；自身路径有并发修改。完成报告后明确所有权已交回。

## 回报格式

只报告改动文件、catalog 契约、测试覆盖、主智能体接线点和保护清单状态。
