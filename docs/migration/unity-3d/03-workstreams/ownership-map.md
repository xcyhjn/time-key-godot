# 多智能体所有权图

> 状态：Wave 01 所有权已冻结
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：目标架构、首切片依赖图、Prompt 路径审查

## 独占写入范围

| 角色 | 独占路径 | 依赖 | 当前状态 |
| --- | --- | --- | --- |
| 主智能体 / 集成 | `docs/migration/unity-3d/**`（代理独占报告除外）、`.gitignore`、`unity/Packages/**`、`unity/ProjectSettings/**`、`unity/Assets/_Project/Editor/**` | 无 | 执行中 |
| Agent 01 / Domain | `unity/Assets/_Project/Runtime/Domain/**`、`unity/Assets/_Project/Tests/EditMode/**`、`docs/migration/unity-3d/03-workstreams/agents/reports/wave-01-agent-01-domain.md` | 冻结 schema | 待启动 |
| Agent 02 / Data adapter | `unity/Assets/_Project/Runtime/Infrastructure/**`、`unity/Assets/_Project/Content/Cards/**`、`docs/migration/unity-3d/03-workstreams/agents/reports/wave-01-agent-02-data.md` | Domain API | 待启动，Agent 01 后 |
| Agent 03 / Battle view | `unity/Assets/_Project/Runtime/Presentation/**`、`unity/Assets/_Project/Scenes/VerticalSlice/**`、`unity/Assets/_Project/Tests/PlayMode/**`、`docs/migration/unity-3d/03-workstreams/agents/reports/wave-01-agent-03-battle-view.md` | Domain + adapter | 待启动，Agent 02 后 |

主智能体保留集成槽，只在依赖已落盘时启动下游。三个代理 Prompt 已生成，但 Wave 01 首步只启动 Agent 01；这是有意的串行依赖，不是并行度不足。

## 禁止范围

- 所有代理不得修改 Godot 源码、用户原有脏文件、根级 Git 配置或其他代理路径。
- 所有代理不得提交、推送、切分支、合并或回退他人改动。
- 共享架构、backlog、parity 和进度账本只由主智能体更新。
- 发现契约冲突时停止写共享文件，在独占报告中记录并通知主智能体。

## 路径互斥审查

Domain、Infrastructure、Presentation 的运行时代码和各自测试/报告没有路径交集。主智能体不在代理执行期间修改其独占路径；集成前先等待代理完成。`Assets/_Project` 仅是共同祖先，不是可写所有权授权。
