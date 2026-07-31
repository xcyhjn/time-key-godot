# Wave 01 Agent Prompt 审查

> 状态：通过
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Prompt 内容检查、所有权路径枚举、共享契约

## 审查结果

| Prompt | 11 项必备内容 | 写入路径互斥 | 依赖可执行 | 结论 |
| --- | ---: | ---: | ---: | --- |
| Agent 01 Domain | 11/11 | 是 | 可立即启动 | 通过 |
| Agent 02 Data adapter | 11/11 | 是 | 等待 Domain API | 通过，暂不启动 |
| Agent 03 Battle view | 11/11 | 是 | 等待 Domain + adapter | 通过，暂不启动 |

## 互斥证明

- Agent 01 只写 `Runtime/Domain`、`Tests/EditMode` 和自己的报告。
- Agent 02 只写 `Runtime/Infrastructure`、`Content/Cards` 和自己的报告。
- Agent 03 只写 `Runtime/Presentation`、`Scenes/VerticalSlice`、`Tests/PlayMode` 和自己的报告。
- 主智能体只写工程/包/Editor harness/共享文档，并在代理运行时避开其独占路径。

三个角色仅共享只读契约和共同祖先目录，没有共享可写文件。所有 Prompt 都明确了非唯一工作者、不得回退他人改动、越权停止、测试责任、Git 禁令与完成回报格式。

## 启动决策

只启动 Agent 01。Agent 02 与 Agent 03 的输入 API 尚未落盘，并行启动会迫使它们猜测契约实现或越权，因此按依赖分波执行。主智能体继续保留集成槽。
