# 多智能体所有权图

> 状态：Combat Shell Gate 0 Prompt 审查通过；Gate A SceneFlow 所有权已开放
> 负责人：主智能体
> 最后验证日期：2026-08-02
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

## 解耦 R2/R3 所有权

| 角色 | 独占路径 | 状态 |
| --- | --- | --- |
| Agent A / Application + Diagnostics | `Runtime/Application/**`、`Runtime/Diagnostics/**`、`Tests/EditMode/Application/**`、`Tests/EditMode/Diagnostics/**`、独占报告 | 已完成交回；正式 Unity filter `14/14` |
| Agent B / Presenter + Binding | `Runtime/Presentation/Presenters/**`、`Bindings/**`、对应 PlayMode tests/报告 | 已完成交回；Binding/Presenter 集成测试通过 |
| Agent C / Catalog + Content | `Runtime/Infrastructure/Cards/**`、`Effects/**`、对应 Infrastructure tests、`Data/Catalogs/**`/报告 | 按冻结的 Agent-C 所有权由主集成串行落地；filter `6/6` |
| 主智能体 | Controller、Scene/Prefab/Composition、asmdef、harness、共享 docs/evidence/Git | 已完成 R3 组装、终验、证据与检查点 |

Agent B 与 C 的冻结路径保持互斥，并且都不修改 Controller、Scene、Prefab 或 asmdef。Agent B 已交回独占实现；Agent C 未作为并行写入者启动，其所有权等价范围由主智能体在无并发冲突时串行实现。最终另有只读 Domain/Application 审计、Scene/Prefab 审计、扩展文档审计和集成复核；报告位于 `agents/reports/decoupling-*.md`。所有权现已全部交回主智能体。

## Remaining Cards 实际所有权

| 波次 | 角色 | 独占路径摘要 | 启动条件 | 状态 |
| --- | --- | --- | --- | --- |
| Gate 0 | 三个只读审计 | 各自 `agents/reports/remaining-cards-*.md` | 前置解耦关闭 | 已完成交回 |
| Gate A | Agent A Recover | 共享 occupant/result、Recover handler、TimelineGrid、Application 与对应 EditMode tests | Prompt review PASS | 已完成并交回；40/40 Agent filter、Gate A 全量 107/107 |
| Gate B | Agent B1 Built | 新 Built handler/test/report | Gate A 交回 | 已完成交回；10 个独占 case 静态通过，纳入全量 130/130 |
| Gate B | Agent B2 Poison | 新 Poison handler/test/report | Gate A 交回 | 外部 Agent 鉴权失败后主智能体按 reviewed Prompt 接管完成；纳入全量 130/130 |
| Gate C | Agent C1 Clear Domain | TimelineGrid clear API、clear session、Application 与 EditMode tests | Gate B 集成 | 已完成交回；纯 C# 0 warning/error、定向 EditMode 56/56 |
| Gate C | C2 Clear Presentation 所有权切片 | Timeline preview/presenter/cell 与对应 PlayMode tests | C1 契约冻结 | C1 交回后由主智能体按 reviewed C2 路径完成；Gate C 全量 EditMode 152/152、PlayMode 4/4 |
| 全程 | 主智能体 | Controller/Binding/Composition/Infrastructure registry、Scene/Prefab/资产、Editor、asmdef、共享文档/证据/Git | 每波交回 | 已完成 Gate D 集成、终验与检查点 |

路径审查位于 `agents/prompt-review-remaining-cards.md`。Agent A 运行期间主智能体不修改其独占路径；B1/B2 只新增互斥文件；C1 完成后才开放 C2。所有 Agent 都不是仓库唯一工作者，不得回退、stash、暂存、commit 或 push。

Gate D 的文档与 harness 两个只读审计智能体均已完成返回；它们没有写工作区。Scene、Prefab、代码、测试、证据、维护文档与 Git 所有权现已全部回收，当前无运行中的写入 Agent。B2 鉴权失败后由主智能体接管、C2 在 C1 交回后串行接管的历史保持不变。

## Turn Lifecycle Gate 0 所有权

| 波次 | 角色 | 独占路径摘要 | 启动条件 | 当前状态 |
| --- | --- | --- | --- | --- |
| Gate 0 | 三个只读审计 | 各自 `agents/reports/turn-lifecycle-*.md` | 当前工作区与主 Prompt 冻结 | 已完成返回；无代码写入 |
| Gate A | Agent A / 生命周期 Runner | 新增 lifecycle/identity/snapshot 文件、新增对应 EditMode tests、独占报告 | Prompt review PASS | 已完成并交回；纯 C# `20/20`，集成全量 `183/183` |
| Gate B | Agent B1 / Enemy intent Domain | 新增 intent Domain/Application 文件与独占 tests/report | Gate A snapshot API 冻结 | 待启动 |
| Gate B | Agent B2 / Intent Presentation | `Presentation/Intent/**`、对应 Prefab/PlayMode tests/report | B1 返回 | 待启动 |
| Gate C | Agent C1 / Building + Poison processors | 新增 processors 与独占 EditMode tests/report | Gate A runner API 冻结 | 待启动 |
| Gate C | Agent C2 / Lifecycle Presentation | `Presentation/TurnLifecycle/**`、对应 Prefab/PlayMode tests/report | C1 返回 | 待启动 |
| 全程 | 主智能体 / 集成 | 所有既有共享代码、Controller、Binding、Composition、Scene、asmdef、Editor harness、共享文档、最终 evidence 与 Git | 每波交回 | 独占 |

详细文件白名单以 `agents/prompts/turn-lifecycle-agent-*.md` 为准。五份写入 Prompt 通过交集审查：A、B1、B2、C1、C2 不共享可写文件；Agent 不得暂存、提交、推送、切分支、运行 Unity 或覆盖其他工作者改动。任何新增共享依赖先写独占报告并交回主智能体处理。

执行顺序固定为 `Gate A -> (Gate B1 || Gate C1) -> (Gate B2 || Gate C2) -> 主集成`。Unity Editor、PlayMode、harness、build 与 Player smoke 始终由主智能体串行执行，避免工程锁与证据污染。

Gate A 实际执行保持互斥：Agent A 只新增 Prompt 白名单内文件，主智能体只修改既有 Timeline/CardPlay/Application 共享文件。Agent 交回后主智能体独占运行 Unity 定向与全量测试并回收全部 Gate A 路径；当前没有 Gate A 写入智能体。

## Wave 02B3 所有权回收

| Agent | 独占交付 | 最终状态 |
| --- | --- | --- |
| A runner | 新 lifecycle Domain/Application 与 EditMode | 已完成并交回 |
| B intent domain | `Domain/Application/Intents` 与专属 tests/report | 已完成，`23/23` |
| B presentation tests | `PlayMode/Actions`、`Tooltips` 专属 tests | 已完成，`6/6` |
| C processors | building/status/death 与专属 tests/report | 已完成，`25/25` |
| C presentation | occupant lifecycle Presenter tests/report | 已完成，`9/9` |
| 主智能体 | 共享 session/composition/controller/Scene/Prefab/asmdef/harness/docs/Git | 已完成并回收全部路径 |

所有 Agent 均未切分支、stash、stage、commit 或 push；当前无活跃写入所有者。

## Wave 02B4 所有权

| 波次 | 角色 | 独占路径摘要 | 当前状态 |
| --- | --- | --- | --- |
| Gate 0 | Agent 0 / Godot source | 单一 `deck-battle-flow-source-semantics.md` 报告 | 已完成并交回；无硬阻塞 |
| Gate A | Agent A / Deck Domain | 新 `Runtime/Domain/Deck/**`、`Tests/EditMode/Deck/**`、报告 | 已完成并交回；独占 `12/12`，Unity Gate A 纳入 `44/44` |
| Gate A | Agent B / Round + Outcome Domain | 新 `Runtime/Domain/BattleFlow/**`、`Tests/EditMode/BattleFlow/**`、报告 | 已完成并交回；独占 `32/32`，Unity Gate A 纳入 `44/44` |
| Gate B | Agent C / Application hook | 新 `Runtime/Application/BattleFlow/**`、对应 tests/report | 已完成并交回；定向 `10/10`，主集成 `60/60`，full EditMode `293/293` |
| Gate C | Agent D / Presentation | 新 `Presentation/BattleFlow/**`、新 BattleFlow Prefab/tests/report | 已完成并交回；主智能体已集成 Scene/Binding/Composition |
| 全程 | 主智能体 | 所有既有/共享代码、Controller、Composition、Binding、Scene、asmdef、Editor、证据、维护文档和 Git | Gate D 已完成并回收全部路径 |

路径白名单见 `agents/prompts/deck-battle-flow-agent-*.md`，交集审查见 `agents/prompt-review-deck-battle-flow.md`。执行顺序为 `Agent 0 -> (A || B) -> C -> D -> 主集成`；任何时刻仅主智能体可串行启动 Unity。

Gate A 实际保持互斥：Agent A/B 只新增各自 Domain/test/report，均未运行 Unity 或 Git。主智能体在两者交回后完成定向 EditMode `44/44` 与完整 EditMode `280/280`，并回收 Gate A 全部路径。

Gate B 初稿保持 Agent C 新目录所有权；主智能体审查后补齐 frozen hand identity/atomic discard，并独占修改既有 Coordinator、Session 与共享测试。Agent C 未运行 Unity 或 Git；主智能体串行完成 Application `10/10`、集成 `60/60`、full EditMode `293/293` 与 graphical PlayMode `53/53`，现已回收 Gate B 全部路径。

Gate C/D 中 Agent D 只写独占 Presentation/Prefab/tests/report，主智能体负责共享 Scene、Binding、Composition、Controller、harness、终验与 Git。最终 full EditMode `300/300`、graphical PlayMode `61/61`，18 张 PNG、Windows build 和 actual Player smoke 均通过；所有 02B4 所有权现已交回。

## Combat Shell 所有权

| Gate | 角色 | 独占路径摘要 | 当前状态 |
| --- | --- | --- | --- |
| Gate 0 | source/visual 与 scene architecture 两个只读 Agent | 只读审计结果；不写工作区 | 已完成返回；均为 PASS WITH CONCERNS，无硬阻塞 |
| Gate A | Agent A / SceneFlow 白名单 | 仅新增 `Application/SceneFlow/**`、`Composition/SceneFlow/**`、对应 EditMode/PlayMode tests 与自己的报告 | 主智能体完成并回收；两轮独立审查整改后 `330/330 + 64/64`、build/Player 通过 |
| Gate B | Agent B / Combat Shell | 仅新增 `Presentation/CombatShell/**`、局部 Prefab/Material/tests/report | Prompt 已审查；Gate A 后启动 |
| Gate C | Agent C / Menu/Transition | 仅新增 `Presentation/GameStart/MainMenu/TransitionVisuals/**`、Shell Prefab/Animation/tests/report | Prompt 已审查；Gate B 后启动 |
| 全程 | 主智能体 | 所有既有文件、正式 Scene、Build Settings、asmdef、route/payload 接线、Editor harness、共享 docs/evidence/Git | 独占 |

三份实现白名单无交集，详见 `agents/prompt-review-combat-shell.md`。任何 Agent 都不得运行 Unity/Godot、修改共享 Scene/Build Settings、暂存或提交；主智能体在每个 Agent 返回后回收路径并串行验证。

Gate A 实际未启动并行写入 Agent；主智能体独占 SceneFlow 白名单和全部共享文件完成实现、Scene authoring、Unity 验证、build/Player 与 Git。随后只读审查 Agent 两轮提出原子性、typed boundary、新 run、真实 failure、captured drag 与 timeout 问题，主智能体完成整改并以 `330/330 + 64/64`、build/Player 关闭。Agent B/C 路径未写入，继续保持 Gate B/C 的独占所有权。
