# Wave 02B Agent Prompt 审查

> 状态：通过，待执行时复核
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Wave 02B1 契约、四份 Prompt、路径集合人工审查

## 审查结果

| Prompt | 11 项必备内容 | 写入路径互斥 | 依赖门禁 | 结论 |
| --- | ---: | ---: | ---: | --- |
| Agent 01 Card art | 11/11 | 是 | 原素材存在/哈希可验 | 通过 |
| Agent 02 Card Domain | 11/11 | 是 | 现有 Domain 基线 | 通过 |
| Agent 03 Card hand UI | 11/11 | 是 | Agent 01/02 审查完成 | 通过，延后启动 |
| Agent 04 Target preview | 11/11 | 是 | Agent 02 API 冻结 | 通过，延后启动 |

## 互斥证明

- Agent 01 仅写 `Resources/Art/Battle/Cards/**`、自己的证据和报告。
- Agent 02 仅写 `Runtime/Domain/**`、`Tests/EditMode/**` 和自己的报告。
- Agent 03 仅写 `Runtime/Presentation/Cards/**`、`Prefabs/Battle/Cards/**`、`Tests/PlayMode/Cards/**`、自己的证据和报告。
- Agent 04 仅写 `Runtime/Presentation/Targeting/**`、`Tests/PlayMode/Targeting/**`、自己的证据和报告。
- 四者仅共享只读契约和祖先目录，没有共同可写文件。
- 主智能体独占 Controller、BoardTileView、scene、Editor harness、asmdef、工程设置、共享文档、最终证据和 Git。

## 启动顺序与资源门禁

1. Wave A 并行启动 Agent 01/02。
2. 两者完成后，主智能体审查路径越权、哈希、API、EditMode 并收回所有权。
3. Wave B 并行启动 Agent 03/04。
4. 代理可以并行写独占文件，但 Unity Editor/batchmode 任何时刻只允许运行一个实例。
5. 每波先集成、测试、视觉审查、提交，再启动后续依赖；不以增加并发为理由绕过未冻结 API。

## 共同禁令

所有 Prompt 均明确：非唯一工作者、不得回退他人改动、越权停止、不得暂存/提交/推送/切分支、测试与证据责任、独占报告和完成回报格式。四个用户脏文件、Godot 源、共享配置不在任何 Agent 写入范围。
