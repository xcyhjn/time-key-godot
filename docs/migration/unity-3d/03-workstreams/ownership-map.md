# 多智能体所有权图

> 状态：Wave 02B2A 五个 Agent 已交回；共享集成完成
> 负责人：主智能体
> 最后验证日期：2026-08-01
> 证据来源：目标架构、首切片依赖图、Prompt 路径审查

## 独占写入范围

| 角色 | 独占路径 | 依赖 | 当前状态 |
| --- | --- | --- | --- |
| 主智能体 / 集成 | 共享文档、工程配置、Editor harness；Agent 03 未启动后接管 `Runtime/Presentation/**`、`Scenes/VerticalSlice/**`、`Tests/PlayMode/**` | Domain + adapter | 已完成 |
| Agent 01 / Domain | `unity/Assets/_Project/Runtime/Domain/**`、`unity/Assets/_Project/Tests/EditMode/**`、自己的报告 | 冻结 schema | 已完成并交回 |
| Agent 02 / Data adapter | `unity/Assets/_Project/Runtime/Infrastructure/**`、`unity/Assets/_Project/Content/Cards/**`、`unity/Assets/_Project/Tests/Infrastructure/**`、自己的报告 | Domain API | 已完成并交回 |
| Agent 03 / Battle view | Prompt 中定义的 Presentation/Scene/PlayMode 路径 | Domain + adapter | 未启动；所有权在无并行写入时显式交回主智能体 |
| Wave 02 Agent 01 / Hex tile model | `ArtSource/HexTiles/**`、`Resources/Art/Battle/Models/**`、独占证据与报告 | 冻结几何契约 | 已完成并交回；主智能体只在 Presentation 中接入 FBX |
| Wave 02B Agent 01 / Card art | `Resources/Art/Battle/Cards/**`、独占证据与报告 | 原素材哈希 | Wave A 完成并交回；原卡面/牌背哈希通过 |
| Wave 02B Agent 02 / Card Domain | `Runtime/Domain/**`、`Tests/EditMode/**`、独占报告 | Wave 02B1 语义契约 | Wave A 完成并交回；主智能体审查后冻结实际 API |
| Wave 02B Agent 03 / Card hand UI | `Runtime/Presentation/Cards/**`、`Prefabs/Battle/Cards/**`、`Tests/PlayMode/Cards/**`、独占证据与报告 | Agent 01 + Agent 02 | Wave B 完成并交回；主智能体已接线共享 Controller |
| Wave 02B Agent 04 / Target preview | `Runtime/Presentation/Targeting/**`、`Tests/PlayMode/Targeting/**`、独占证据与报告 | Agent 02 | Wave B 完成并交回；主智能体已接线共享棋盘/时间轴 |

Agent 01 完成后启动 Agent 02。两项依赖落盘并经主智能体审查后，没有再启动 Agent 03；主智能体在确认该路径从未被代理写入后接管 Presentation、场景和 PlayMode 集成，避免新增一次接口交接。全程没有并发写同一路径。

## 禁止范围

- 所有代理不得修改 Godot 源码、用户原有脏文件、根级 Git 配置或其他代理路径。
- 所有代理不得提交、推送、切分支、合并或回退他人改动。
- 共享架构、backlog、parity 和进度账本只由主智能体更新。
- 发现契约冲突时停止写共享文件，在独占报告中记录并通知主智能体。

## 路径互斥审查

Domain、Infrastructure、Presentation 的运行时代码和各自测试/报告没有路径交集。Agent 01 已完成后，Agent 02 使用独立的 `Tests/Infrastructure` 测试程序集，不改 Agent 01 的 `Tests/EditMode` 文件。主智能体不在代理执行期间修改其独占路径；集成前先等待代理完成。`Assets/_Project` 仅是共同祖先，不是可写所有权授权。

## Wave 02B1 并发波次

- Wave A：Agent 01 与 Agent 02 可并行；没有共同可写文件。
- 主智能体等待两者完成，审查素材哈希、Domain API 和 EditMode 后明确收回所有权。
- Wave B：Agent 03 与 Agent 04 可并行；`Cards/**` 与 `Targeting/**`、各自测试/证据/报告完全互斥。
- 主智能体始终独占 `VerticalSliceController.cs`、`BoardTileView.cs`、场景、Editor harness、asmdef、ProjectSettings、共享文档、最终证据和 Git。
- 任何时刻只允许一个 Unity Editor/batchmode 实例，防止工程锁和低内存互相污染。

实际执行严格遵循上述两波：Agent 01/02 并行完成后，主智能体审查原素材哈希、`TimelineGrid.CanPlace` 与 `CardPlaySession` 并收回所有权；随后 Agent 03/04 并行。四个 Agent 均只写各自 Prompt 的互斥路径，最后由主智能体独占共享 Controller、Harness、测试集成、迁移账本与 Git。

## Wave 02B2A 实际所有权

| 角色 | 独占路径摘要 | 启动条件 | 当前状态 |
| --- | --- | --- | --- |
| 02B2A Agent 01 / Schema | `CardDefinition.cs`、`Domain/Cards/**`、Infrastructure、Content/Cards、Infrastructure tests、独占报告 | 02B1 已冻结 | Wave A 完成并交回；七卡 typed schema `58/58` |
| 02B2A Agent 02 / Card art | Resources/Cards、独占素材证据/报告 | 原素材存在 | Wave A 完成并交回；七张卡面尺寸/哈希核对通过 |
| 02B2A Agent 03 / Earthquake Domain | 明确列出的 Timeline/Combat Domain 文件、`Domain/Terrain/**`、EditMode Terrain/必要回归、独占报告 | Agent 01 API 冻结 | Wave B 完成并交回；earthquake 领域用例 `9/9` |
| 02B2A Agent 04 / Two-card hand | Presentation/Cards、Cards PlayMode、独占视觉证据/报告 | Agent 01/02/03 交回 | Wave C 完成并交回；Cards PlayMode `9/9` |
| 02B2A Agent 05 / Elevation view | Presentation/Terrain、Terrain PlayMode、独占视觉证据/报告 | Agent 03 result API 冻结 | Wave C 完成并交回；Terrain PlayMode `3/3` |
| 主智能体 / Integration | Controller、BoardTileView、Targeting、scene、Editor、asmdef/Packages、共享文档、最终证据和 Git | 每波 Agent 交回 | 完成；最终 EditMode `67/67`、PlayMode `25/25` |

执行顺序固定为 `(Agent 01 || Agent 02) -> Gate A -> Agent 03 -> Gate B -> (Agent 04 || Agent 05) -> 主集成`。路径集合审查见 `agents/prompt-review-wave-02b2a.md`。并发只表示独占文件可同时编写；Unity/Godot/Blender 图形或 batchmode 仍不得并行启动。

实际执行保持三波依赖和互斥路径；各 Agent 不提交、不推送、不修改共享 Controller/Scene/Harness。主智能体只在依赖 Gate 通过并交回所有权后做共享接线和串行 Unity 验证。五份独占报告位于 `agents/reports/wave-02b2a-agent-*.md`。
