# 多智能体所有权图

> 状态：Wave 02A 所有权已完成交接
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：目标架构、首切片依赖图、Prompt 路径审查

## 独占写入范围

| 角色 | 独占路径 | 依赖 | 当前状态 |
| --- | --- | --- | --- |
| 主智能体 / 集成 | 共享文档、工程配置、Editor harness；Agent 03 未启动后接管 `Runtime/Presentation/**`、`Scenes/VerticalSlice/**`、`Tests/PlayMode/**` | Domain + adapter | 已完成 |
| Agent 01 / Domain | `unity/Assets/_Project/Runtime/Domain/**`、`unity/Assets/_Project/Tests/EditMode/**`、自己的报告 | 冻结 schema | 已完成并交回 |
| Agent 02 / Data adapter | `unity/Assets/_Project/Runtime/Infrastructure/**`、`unity/Assets/_Project/Content/Cards/**`、`unity/Assets/_Project/Tests/Infrastructure/**`、自己的报告 | Domain API | 已完成并交回 |
| Agent 03 / Battle view | Prompt 中定义的 Presentation/Scene/PlayMode 路径 | Domain + adapter | 未启动；所有权在无并行写入时显式交回主智能体 |
| Wave 02 Agent 01 / Hex tile model | `ArtSource/HexTiles/**`、`Resources/Art/Battle/Models/**`、独占证据与报告 | 冻结几何契约 | 已完成并交回；主智能体只在 Presentation 中接入 FBX |

Agent 01 完成后启动 Agent 02。两项依赖落盘并经主智能体审查后，没有再启动 Agent 03；主智能体在确认该路径从未被代理写入后接管 Presentation、场景和 PlayMode 集成，避免新增一次接口交接。全程没有并发写同一路径。

## 禁止范围

- 所有代理不得修改 Godot 源码、用户原有脏文件、根级 Git 配置或其他代理路径。
- 所有代理不得提交、推送、切分支、合并或回退他人改动。
- 共享架构、backlog、parity 和进度账本只由主智能体更新。
- 发现契约冲突时停止写共享文件，在独占报告中记录并通知主智能体。

## 路径互斥审查

Domain、Infrastructure、Presentation 的运行时代码和各自测试/报告没有路径交集。Agent 01 已完成后，Agent 02 使用独立的 `Tests/Infrastructure` 测试程序集，不改 Agent 01 的 `Tests/EditMode` 文件。主智能体不在代理执行期间修改其独占路径；集成前先等待代理完成。`Assets/_Project` 仅是共同祖先，不是可写所有权授权。
