# Wave 01 Agent Prompt 审查

> 状态：通过
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Prompt 内容检查、所有权路径枚举、共享契约

## 审查结果

| Prompt | 11 项必备内容 | 写入路径互斥 | 依赖可执行 | 结论 |
| --- | ---: | ---: | ---: | --- |
| Agent 01 Domain | 11/11 | 是 | 已执行 | 通过并完成 |
| Agent 02 Data adapter | 11/11 | 是 | Domain API 已落盘后执行 | 通过并完成 |
| Agent 03 Battle view | 11/11 | 是 | Domain + adapter 已落盘 | 通过；未启动，所有权交回主智能体 |

## 互斥证明

- Agent 01 只写 `Runtime/Domain`、`Tests/EditMode` 和自己的报告。
- Agent 02 只写 `Runtime/Infrastructure`、`Content/Cards`、独立 `Tests/Infrastructure` 和自己的报告。
- Agent 03 只写 `Runtime/Presentation`、`Scenes/VerticalSlice`、`Tests/PlayMode` 和自己的报告。
- 主智能体只写工程/包/Editor harness/共享文档，并在代理运行时避开其独占路径。

三个角色仅共享只读契约和共同祖先目录，没有共享可写文件。所有 Prompt 都明确了非唯一工作者、不得回退他人改动、越权停止、测试责任、Git 禁令与完成回报格式。

## 启动与交接决策

先启动 Agent 01，再在 Domain API 落盘后启动 Agent 02。Agent 03 Prompt 保留为可复用工作说明，但未实际启动；主智能体在 Agent 01/02 均完成、所有代理路径没有并发写入后接管视图集成。该变化只减少交接次数，没有改变冻结的 Domain、adapter 或玩法契约。
