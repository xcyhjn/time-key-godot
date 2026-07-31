# Wave 02B2A Agent Prompt 审查

> 状态：通过；启动时必须按当前文件重新复核
> 负责人：主智能体
> 最后验证日期：2026-08-01

## 结论

| Prompt | 11 项内容 | 可写路径互斥 | 依赖门禁 | 结论 |
| --- | ---: | ---: | ---: | --- |
| Agent 01 Seven-card schema | 11/11 | 是 | 02B1 schema | Wave A 可启动 |
| Agent 02 Card art catalog | 11/11 | 是 | 原素材 | Wave A 可启动 |
| Agent 03 Earthquake Domain | 11/11 | 是 | Agent 01 API 冻结 | Gate A 后串行 |
| Agent 04 Two-card hand | 11/11 | 是 | Agent 01/02/03 交回 | Wave C 可启动 |
| Agent 05 Elevation presentation | 11/11 | 是 | Agent 03 result API | Wave C 可启动 |

## 路径互斥证明

- Agent 01 只写 `CardDefinition.cs`/`Domain/Cards/**`、Infrastructure、Content/Cards、Infrastructure tests 和自己的报告。
- Agent 02 只写 Resources/Cards、独占素材证据和自己的报告。
- Agent 03 只写明确列出的 Timeline/Combat Domain 根文件、`Domain/Terrain/**`、EditMode Terrain/必要旧回归和自己的报告。
- Agent 04 只写 Presentation/Cards、Cards PlayMode、独占截图和自己的报告。
- Agent 05 只写 Presentation/Terrain、Terrain PlayMode、独占截图和自己的报告。
- Agent 01/02 并行时无共同可写文件；Agent 04/05 并行时也无共同可写文件。
- Agent 03 必须在 Agent 01 交回后单独执行，避免追逐未冻结 effect schema。
- 主智能体始终独占 `VerticalSliceController.cs`、`BoardTileView.cs`、Targeting、Scene、Editor harness、asmdef、Packages/ProjectSettings、共享文档、最终证据和 Git。

## 共同禁令和资源门禁

五份 Prompt 均包含角色、必读、skill、独占路径、禁止路径、契约、步骤、非目标、测试/截图、报告、Git 和回报格式。所有 Agent 都明确不是唯一工作者，不得回退、暂存、提交、推送、切分支或修改 4 个用户脏文件。

任意时刻只允许一个 Unity Editor/batchmode。Wave A 完成后先跑 Gate A；Agent 03 完成后先跑 Gate B；Wave C 完成后主智能体才接管共享 Controller/场景。Blender、Godot 图形实例和 Unity 不并行运行。

## 审查中修正的风险

- 把立即目标从“七种效果一次实现”缩到七卡 schema + earthquake 垂直切片。
- 明确 `earthquake=+2`、真实 `0.32` 两层增量和一基/零基换算。
- 明确 `built.value=count`，不虚构等级。
- 明确 clear 是未来独立即时会话，不能进入普通 TimelineAction。
- 明确异构 JSON 使用结构化 typed DTO 视图，禁止字符串拼补；新增包只能由主智能体记录 ADR 后串行处理。
