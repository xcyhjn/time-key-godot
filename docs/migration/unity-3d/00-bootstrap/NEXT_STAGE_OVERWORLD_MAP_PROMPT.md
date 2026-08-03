# Wave 03：完整局外地图、房间流程与存档迁移下一阶段 Prompt

> 目标分支：`unity_7.31`
> 前置基线：Combat Shell Gate E 已完成
> 生成日期：2026-08-03

## 主 Prompt

进入 `D:\godot\时之钥\时之钥`，确认当前位于 `unity_7.31` 分支。完整读取本文件，并将“主 Prompt”作为 Wave 03 唯一执行规范立即开始。保护全部现有未提交改动；继承 Combat Shell Gate E 仍可信且未受影响的 Bootstrap、SceneFlow、typed payload/outcome、Silver 汉化、顶部 UI、战斗背景、海洋局外背景、转场、Build 和 Player 证据。除本文定义的硬阻塞外，不得停在复述、计划或提问阶段；持续推进 Gate 0 至 Gate D，并更新 Scene/Prefab、代码、测试、视觉证据、维护文档与精确 Git 检查点。push 失败或不可用时直接记录并跳过，不修改用户级 Git/GCM/TLS 配置。

### 1. 本阶段目标

把当前单房间 `OutOfBattleShell` 扩展为正式程序化局外地图：同一固定 seed 生成稳定章节图，玩家只能沿相邻边移动；支持战斗、事件、商店、Boss 和章节推进；支持 Continue/save migration；所有进入战斗与返回结算继续使用既有 `OutOfBattleShellState`、`CombatLaunchPayload`、`CombatOutcome` 和 SceneFlow，不创建第二套跨 Scene 状态机。

本阶段必须保持：

- Bootstrap、SceneFlow phase、typed payload/outcome、sequence/idempotency、input/focus/transition 所有权不变。
- `MainMenu -> OutOfBattleShell -> Combat -> Reward -> OutOfBattleShell` 与 `Combat -> GameOver -> MainMenu` 既有路径继续通过。
- 局外背景继续使用当前海洋 tile 和 `OutOfBattleOceanBackground` 响应式规则。
- 玩家可见中文继续使用 Silver Font/Material；无可信 Godot 原文时不得臆造剧情、卡牌或敌人规则。
- Combat camera、战斗背景、Top HUD、02B3 action identity 和 02B4 deck/round/outcome 契约不因局外扩展而重写。

### 2. Gate 0：保护、冻结与决策

1. 记录分支、HEAD、ahead/behind、dirty/untracked inventory、Unity/Godot 进程与可写路径所有者；不得 stash、reset、checkout 或清理不明文件。
2. 解析 Gate E 权威 XML/JSON/PNG/文档，确认 `343/343 + 99/99`、六 Scene build、actual Player 三轮、海洋三视口和 Silver attribution 可继承；受影响边界必须在本阶段重跑。
3. 完整读取 Godot 局外地图生成、CFG/房间连接、移动、事件、商店、Boss、章节推进、Continue/save 和 return payload 源码/资源。冻结可观察语义、默认数据、随机顺序、失败路径和现有素材授权。
4. 形成并接受一份 ADR，明确 Godot CFG 兼容等级：优先选择“相同 seed 产生同一拓扑与房间类型序列”；若 Godot RNG/CFG 无法逐位复现，必须记录可验证的兼容层级与理由，不能静默改算法。
5. 冻结 save schema/version、迁移策略、原子写入与损坏存档恢复。Continue 必须以真实可恢复存档决定 enabled，不再是永久禁用占位。
6. 审查 `OutOfBattleShellState`、`RunStartPayload`、`CombatLaunchPayload`、`CombatOutcome` 的扩展点。地图节点身份、chapter、seed、当前位置、已结算集合和商店/事件状态使用 Unity-free immutable/defensive-copy contracts；不得跨 Scene 保存 Unity object。

Gate 0 交付：源语义报告、CFG/seed ADR、save migration 设计、共享契约更新、测试矩阵、dirty 保护清单、多智能体 Prompt 与 Prompt review。审查通过后立即进入 Gate A。

### 3. 多智能体所有权

在 `docs/migration/unity-3d/03-workstreams/agents/` 生成所有权互斥 Prompt，最多并行三个子智能体，主智能体保留共享集成：

- Map Domain Agent：只拥有新增 Unity-free `Domain/Overworld/**` 与对应 EditMode tests，负责确定性拓扑、邻接、房间状态和章节规则。
- OutOfBattle Presentation Agent：只拥有新增 `Presentation/Overworld/**`、局部 Prefab/PlayMode tests，负责地图节点/边、选中/可达/已结算状态和响应式布局；不得写 SceneFlow/save。
- Save Migration Agent：只拥有新增 `Infrastructure/Persistence/**`、对应 EditMode tests 和迁移报告；不得写 Domain 规则或共享 Scene。
- 主智能体独占既有 Application/SceneFlow contracts、正式 Scene/Prefab、Composition、Build Settings、MainMenu Continue 接线、共享 asmdef、Editor harness、证据、共享文档、Git 暂存/提交/push。

所有 Agent 都不是仓库唯一工作者，不得切分支、stash、回退他人修改、暂存、commit 或 push。不得并行运行 Unity。Prompt review 必须证明白名单无交集、依赖方向正确、共享文件全部由主智能体独占。

### 4. Gate A：地图 Domain 与确定性

实现纯 C# 地图模型与生成：stable `MapNodeId`、章节/层级、节点类型、邻接边、入口/出口/Boss、当前位置、visited/settled/available 状态和 fixed-seed generator。至少断言：

- 相同 seed/config 产生同一拓扑、节点 ID、房间类型和邻接；不同 seed 能产生可观察差异。
- 图从入口可达 Boss，无孤立节点、自环、重复边或跨非法层跳转；每个非入口节点至少一个前驱，非 Boss 路径有合法后继。
- 只能移动到当前节点的相邻可用节点；重复命令幂等，stale/conflict 明确失败，非法移动无部分副作用。
- 战斗、事件、商店、Boss 的进入/结算互斥；Boss 胜利只推进一次章节，Defeat 不伪装成完成。
- 生成和状态快照不引用 Unity API/Object，集合防御性复制，整数/seed 边界有测试。

Gate A 需要定向与完整 EditMode 通过并形成单一目的本地检查点；未获用户指示不得扩大为完整剧情或经济系统。

### 5. Gate B：Application、SceneFlow 与房间流程

在既有 typed boundary 上接入地图状态：

- MainMenu 新游戏/种子游戏创建 map/run；Continue 从迁移后的存档恢复同一 run/map/current node。
- 地图只为相邻可用节点发 typed room selection；战斗/Boss 继续构造既有 `CombatLaunchPayload`，返回继续消费既有 `CombatOutcome` 并按 identity 结算一次。
- 事件与商店使用明确的 typed room outcome，不借用 Combat outcome，不把 Dictionary/object/Unity object 塞进 payload。
- 奖励领取、房间 settled、地图解锁、存档提交的顺序固定且可回滚；崩溃/写入失败不能留下“奖励已领但房间未结算”或相反的部分状态。
- Boss Victory 推进章节并生成/载入下一章节地图；最终章节结束使用明确 typed 状态。Defeat 保持现有 GameOver 清理语义。
- SceneFlow loading/binding/reveal 失败恢复原地图节点、焦点、遮罩和输入锁；相同 outcome replay 幂等，不同 outcome conflict。

运行纯 Application 测试、真实 additive PlayMode 路径与前置 SceneFlow/02B4 回归。

### 6. Gate C：正式地图 UI、事件/商店与存档

把正式 OutOfBattleShell Scene/Prefab authoring 为可编辑地图：

- 节点与边由运行时数据实例化，但稳定 host、Scroll/zoom/focus、详情/确认层、Top HUD、海洋背景和布局参数在 Play 前保存。
- 明确显示 current、available、locked、hover/focus、selected、confirming、visited、settled、Boss 状态；pointer、键盘/手柄导航与 ESC/focus restore 一致。
- 三视口 1280x720、1920x1080、2560x1080 不裁切、不重叠，海洋 tile 不拉伸；长中文、最大节点数和分支路径仍可读。
- 事件/商店先实现 Godot 已有且可冻结的最小正式交互；不可用功能必须明确 disabled/提示，不做假按钮。
- Continue 真实反映存档：无存档 disabled，合法存档 enabled，旧版本可迁移，损坏/未来版本有中文可恢复提示。
- 所有玩家可见文字、动态节点标签、弹层与按钮使用 Silver；新增源素材记录源路径、Unity 路径、尺寸、SHA-256 和授权。

保存 Scene/Prefab/Material/配置并加入 EditMode 结构测试、PlayMode 交互测试和真实渲染证据。不得只用 headless load、非空像素或退出码代替人工视觉检查。

### 7. Gate D：章节、端到端与交付

至少完成以下真实路线：

1. 新游戏 -> 固定 seed 地图 -> 相邻战斗 -> Victory/reward -> 同图结算并解锁后继。
2. 事件 -> 选择/结果 -> 保存 -> 重启 Player -> Continue -> 同一节点/状态/资源。
3. 商店 -> 一次交易/退出 -> 保存恢复，余额与库存不重复消费。
4. 多房间 -> Boss -> Victory -> 章节只推进一次并生成稳定下一图。
5. Combat Defeat -> GameOver -> MainMenu；损坏存档与 SceneFlow failure 可恢复且无重复 Bootstrap/content entry。

连续至少三房间与一次 Player 重启；检查唯一 Bootstrap/EventSystem/Audio/Transition、旧 Scene 卸载、payload/outcome 一次消费、input/focus、内存和存档文件句柄。运行最终完整 EditMode、完整 graphical D3D12 PlayMode、Windows build 与实际可见 Player smoke。

视觉证据至少覆盖三视口地图全景、最密分支、节点全部状态、事件、商店、Boss、章节转换、Continue、损坏存档提示、进入/返回动画和海洋背景；逐张人工检查裁切、重叠、文字、焦点、可达性表达和交互锁。动画必须有帧序列或结构化时间点。

### 8. 文档与 Git

持续更新 ADR、integration contracts、ownership map、test plan、parity matrix、inherited ledger、current status、completed slices、known issues、push status、Scene/Prefab authoring、存档 schema/migration、调试与证据索引。每个 Gate 前后刷新 dirty inventory。

不得使用 `git add -A`、`git reset --hard`、`git checkout --`、自动 stash、历史改写或清理不明确目录。测试重写的历史 PNG、raw logs、build binaries、Library、用户 Godot 源改动和来源不明文件不得进入检查点。只精确暂存本阶段白名单，提交前执行 cached name audit、cached diff、`git diff --check`、JSON/XML parse、Scene YAML/Build Settings 和 forbidden-file audit。

### 9. 只有这些情况可以停止

- Gate E 权威证据不成立，或前置 Agent/Unity 进程仍在写共享文件。
- 当前分支不是 `unity_7.31`，且安全切换会覆盖无法保护的改动。
- Godot 局外地图/CFG/save 源缺失到无法冻结最小语义，且仓库没有已批准替代契约。
- 旧存档迁移涉及不可恢复覆盖，但没有可验证备份/版本策略。
- 必须发明新剧情、经济、敌人规则、外部服务或进行不可恢复数据修改才能继续。
- Unity 许可证、工程锁、磁盘或编辑器故障在安全重试后仍阻止全部验证。

节点间距、连线样式、动画时长、海洋 tile 数、UI 锚点或兼容等级在证据范围内可做最低风险选择，不是停止理由。当前无用户决策阻塞时明确写出并继续执行。

### 10. 最终回报

最终用中文汇报 Gate E 继承证据、CFG/seed 兼容决定、地图/房间/章节 Domain、typed SceneFlow 集成、事件/商店/Boss、Continue/save migration、三视口/Silver/海洋视觉、完整测试、build、实际 Player、性能稳定性、Scene/Prefab 动态边界、多智能体所有权、维护文档、本地 Git 检查点与 push 状态。

## 主 Prompt 结束
