# 解耦阶段写入智能体 Prompt 审查

> 状态：独立复核未通过；修正前三个写入型 Agent 均不得启动
> 负责人：主智能体
> 独立复核：decoupling-domain-application-audit Agent
> 最后复核日期：2026-08-01
> 唯一规范：`00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md`

## 复核结论

三份 Prompt 的目标和代码路径互斥成立，主智能体共享文件所有权也没有冲突；预期的 R1 -> R2 -> R3 三波顺序正确。但是，Prompt 仍缺少主 Prompt 强制要求的可复制测试/截图命令、停止条件和自包含依赖门禁，且共同技能/Git 说明存在越权风险。Agent A 另外缺少可编译的 asmdef 前置条件，并且 `ICardEffectHandler` 的程序集归属与消费者方向未冻结。

结论为 **FAIL**。主智能体必须先修正三份 Prompt，再做一次独立复核；审查通过前不得启动写入型 Agent。

## 逐 Prompt 结果

| Prompt | 11 类结构 | 路径互斥 | 三波门禁 | 测试/截图命令 | 停止条件 | Unity/Git/协作禁令 | 结论 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Agent A Application/Diagnostics | 结构存在，内容不完整 | 通过 | 未写入 Agent 自身 Prompt | 缺失 | 缺失 | 部分通过 | 不通过 |
| Agent B Presentation | 结构存在，内容不完整 | 通过 | 未写入 Agent 自身 Prompt | 缺失 | 缺失 | 部分通过 | 不通过 |
| Agent C Infrastructure/Content | 结构存在，内容不完整 | 通过 | 未写入 Agent 自身 Prompt | 缺失 | 缺失 | 部分通过 | 不通过 |

## 11 类内容逐项复核

| 类别 | Agent A | Agent B | Agent C | 结论 |
| --- | --- | --- | --- | --- |
| 1. 单一角色与目标 | 通过 | 通过 | 通过 | 三者目标边界清楚 |
| 2. 必读文件 | 部分 | 部分 | 部分 | 缺 `command-catalog.md`；上游 API/asmdef 检查点没有给出精确路径/状态 |
| 3. 使用规范 | 不通过 | 不通过 | 不通过 | 直接要求执行完整 `architecture-review` 工作流会与独占路径和不得改共享文档冲突 |
| 4. 独占可写路径 | 通过 | 通过 | 通过 | 三者代码、测试、报告路径无交叉 |
| 5. 禁止路径 | 通过 | 通过 | 通过 | Controller、Scene/Prefab、asmdef、harness 和共享文档均保留给主智能体 |
| 6. 冻结契约 | 部分 | 通过 | 部分 | Agent A 的 handler 归属未冻结；Agent C 的 Effects 目录职责可能越入玩法规则 |
| 7. 实施步骤 | 部分 | 部分 | 部分 | 没有红/绿门禁、上游检查和交回所有权的精确步骤 |
| 8. 非目标 | 通过 | 通过 | 通过 | 均未提前实现剩余卡、敌人或章节 |
| 9. 测试/截图命令与可验证性 | 不通过 | 不通过 | 不通过 | 三份 Prompt 都没有可复制命令、test filter、XML 位置或截图责任 |
| 10. 独占报告与回报 | 通过 | 通过 | 通过 | 报告路径唯一，回报字段明确 |
| 11. 停止条件、Git 与共享工作区规则 | 不通过 | 不通过 | 不通过 | 没有停止条件；未明确禁止 stash/回退他人；Agent 自身 Prompt 未声明“不是唯一工作者” |

## 路径互斥证明

- Agent A 只写新 `Runtime/Application/**`、`Runtime/Diagnostics/**` 与对应 EditMode tests。
- Agent B 只写新 `Presentation/Presenters/**`、`Presentation/Bindings/**` 与对应 PlayMode tests。
- Agent C 只写新 `Infrastructure/Cards/**`、`Infrastructure/Effects/**`、`Data/Catalogs/**` 与对应 Infrastructure tests。
- 三者报告文件独占，代码与测试目录无交集。
- 主智能体独占 Controller、既有 Presentation 组件、Scene、Prefab、全部 asmdef、Editor harness、共享架构/维护/进度/验收文档、最终证据与 Git。

此项通过。禁止把 Agent A 的 handler 契约暗中放入既有 Domain 文件，也禁止 Agent C 在 Infrastructure 中实现玩法结算；否则会破坏上述互斥和依赖方向。

## 三波依赖门禁

预期顺序正确，但必须写入每份 Agent Prompt，而不能只存在于本审查文档：

1. **Wave 1 / R1（主智能体）**：Scene/Prefab checkpoint、序列化 binding 契约、必要 asmdef 与测试程序集引用已经提交并推送；没有 Unity 写入进程或不明锁。
2. **Wave 2 / R2（Agent A）**：只在主智能体明确宣布 R1 Gate 通过后启动。Agent A 返回后，主智能体完成 Application/Controller/asmdef 接线、运行完整受影响测试、提交并推送，冻结 R2 API。
3. **Wave 3 / R3（Agent B + Agent C）**：只在 Agent A 已返回且主智能体明确宣布 R2 Gate 通过后并行启动。主智能体最终独占 Composition、Scene/Prefab、Controller、asmdef、harness、视觉证据与 Git。

任何上游 Gate 缺少 commit/push 状态、结构化测试 XML 或冻结 API 路径时，下游 Agent 必须停止并回报，不得自行补改共享文件。

## 问题记录

**Issue 1: 使用 `architecture-review` 会授权越界工作流**

三份 Prompt 的“使用规范”均要求遵循 `architecture-review`。该技能的完整流程要求读取/更新广泛架构资料、写 traceability/registry、请求用户批准并可能启动额外 specialist，这与 Agent 的独占写路径、不得改共享文档和固定任务边界冲突。

所需修正：删除该技能要求，或明确写成“只消费已落盘的独立架构审计报告，不启动 `/architecture-review` 工作流、不请求用户决策、不写共享文档”。保留 `codebase-migrate` 和 `Verification & Quality Assurance` 即可。

**Issue 2: 三份 Prompt 均缺测试与截图命令**

主 Prompt 要求每份 Agent Prompt 写明测试/截图命令。当前只有“写测试”“由主智能体运行”等概述，没有 Unity executable、project path、test filter、XML 路径、预期结果或截图责任，因此无法形成可复制验收。

所需修正：每份 Prompt 必须引用 `04-verification/command-catalog.md` 并列出精确命令。由于 Agent 被禁止运行 Unity，应明确标记“Agent 不执行，主智能体在收回所有权后串行执行”：

- Agent A：Application/Diagnostics EditMode filter、XML 目标、0 failure 门禁；无独立截图，并明确由主智能体刷新集成视图。
- Agent B：Presenters/Bindings PlayMode filter、XML 目标；主智能体接入 Scene 后执行 1280x720、1920x1080、2560x1080 和四向截图。
- Agent C：Infrastructure Catalog/Effects EditMode filter、XML 目标；无独立截图，主智能体用两卡/原卡面集成截图验证资源映射未回退。

**Issue 3: 三份 Prompt 缺少停止条件与自包含上游 Gate**

依赖顺序只写在本审查文档，Agent 自身 Prompt 没有声明启动前置、硬停止条件和交回所有权时机。Agent 可能在 R1/R2 未冻结或需要修改禁止文件时继续工作。

所需修正：每份 Prompt 增加“启动门禁”和“立即停止并回报”章节。至少覆盖：上游 checkpoint/API/asmdef 未冻结；所需修改落入禁止路径；发现并发改动与自身路径重叠；需要运行 Unity或修改 Scene/Prefab/asmdef 才能继续；测试无法由主智能体复现；契约与真实代码冲突。交付报告后必须明确所有权已交回。

**Issue 4: Agent A 缺少可编译 asmdef 前置，handler 依赖方向未冻结**

Agent A 只能新增 `Runtime/Application/**` 和 EditMode tests，同时禁止修改任何 asmdef。当前基线没有 `TimeKey.Application.asmdef`，现有 EditMode asmdef也不引用 Application；若主智能体不在 Agent A 启动前创建这些引用，Agent A 的代码/测试不能进入目标程序集并编译。

另外，Prompt 同时允许 `ICardEffectHandler`，却禁止修改 Domain。若该接口由 `TimelineGrid`/Domain resolver 消费，把它定义在 Application 会迫使 `Domain -> Application`，违反目标依赖方向；若只由 Application 消费，则必须先冻结谁负责现有 `TimelineGrid.ApplyPlayerEffects` 集成。

所需修正：Agent A 启动前由主智能体创建/验证 `TimeKey.Application`、Diagnostics（如独立）和对应 test asmdef 引用。Prompt 必须给出三个接口的准确程序集、namespace、消费者与实现者。若 handler 由 Domain 消费，其契约和 `TimelineGrid` 接线必须归主智能体/Domain 独占任务，不能授权 Agent A 越界。

**Issue 5: Agent B 的 Scene 接线验收在禁令下不可独立验证**

Agent B 被禁止修改 Scene/Prefab、asmdef并禁止运行 Unity，但验收要求“组件能由 Scene 序列化引用接线”。仅写字段或 PlayMode test 源码不能证明真实 Scene 引用、Hierarchy、Prefab 连接或 enable/disable 后的实际行为。

所需修正：把验收拆成两级。Agent B 只证明组件公开可序列化依赖、无 `GameObject.Find`/service locator、bind/unbind 契约和测试源完整；主智能体负责实际 Scene/Prefab 接线、运行 PlayMode、打开 Inspector 检查并生成视觉证据。R2 Gate 还必须预先确保 Presentation/PlayMode asmdef 已引用 Application。

**Issue 6: Agent C 的 Infrastructure/Effects 责任可能侵入玩法规则**

Agent C 的目标包含“handler 组装”且拥有 `Infrastructure/Effects/**`，但主 Prompt 明确 Infrastructure 不决定玩法结果。当前 Prompt 没有区分 catalog registration descriptor 与 `ICardEffectHandler` 玩法实现，可能把 Damage/Elevation/未来效果结算放入 Infrastructure。

所需修正：明确 Agent C 只能实现 catalog、结构化资源定位和 handler registration descriptor/工厂装配数据，不得实现 target validation、effect calculation 或修改 Domain 状态。R2 Gate 需预先让 Infrastructure/test asmdef 引用 Application port；七卡输入来源、catalog 构造入口和重复/缺失失败行为也应给出精确 API，而不是让 Agent 猜测。

**Issue 7: Git 与共享工作区禁令不完整**

三份 Prompt 禁止暂存、提交、推送和切分支，但没有明确禁止 stash、回退/覆盖他人修改，也没有在 Agent 自身 Prompt 中声明它不是仓库唯一工作者。只在本审查文档写共同禁令不够，因为三份 Prompt 并未要求必读本文件。

所需修正：在每份 Prompt 原文写明“你不是唯一工作者；不得 revert/覆盖他人改动；不得 stash、stage、commit、push、切分支；遇到未知或重叠改动立即停止并回报；不得修改或清理用户/未知脏文件”。

## 修正后复核门禁

重新复核必须逐项确认：

- 三份 Prompt 的 11 类内容不仅有标题，而且具备可执行信息。
- 精确测试 filter、XML 位置、截图责任和 0 failure 标准已写入。
- 启动 Gate、停止条件、交回所有权和共享工作区规则均为自包含文本。
- Agent A/Application、Agent B/Presentation、Agent C/Infrastructure 的 asmdef 前置已经由主智能体完成，且依赖图无环。
- `ICardCatalog`、`ICardEffectHandler`、`ICombatTraceSink` 的程序集、消费者、实现者和禁止方向已冻结。
- Agent B 的组件级验收与主智能体 Scene/Prefab 集成验收已分开。
- Agent C 不拥有任何玩法结果或 Domain 状态写入职责。

完成上述修正前，**禁止启动 Agent A/B/C**。
