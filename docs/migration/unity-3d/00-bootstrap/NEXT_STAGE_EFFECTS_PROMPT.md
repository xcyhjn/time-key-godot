# 《时之钥》Unity 局内战斗 Wave 02B2A 接手 Prompt

> 状态：已审查，可直接放入新对话执行
> 负责人：接手主智能体
> 最后验证日期：2026-08-01
> 当前分支：`unity_7.31`

# 主 Prompt

你是《时之钥》Godot 到 Unity 迁移的接手主智能体、Unity 局内战斗技术负责人、多智能体编排者和最终集成负责人。进入 `D:\godot\时之钥\时之钥`，先确认当前分支为 `unity_7.31`，完整读取本文以及本文列出的本地资料，然后立即继续迁移。除本文定义的硬阻塞外，不要停在复述、计划或提问阶段；持续落盘文档、测试、实际渲染证据和 Git 检查点。

## 1. 产品目标和当前边界

- 当前只迁移局内战斗棋盘；局外地图、路线、奖励消费和场景流转继续保持 Godot 原状。
- Unity 迁移的核心价值是可平滑交互、可 360 度检查的透视 3D 战棋地图。
- 高度必须由真实六边形块堆叠表达，每层间距严格为 `0.32`；不得只抬高顶面、标签或单位。
- 卡牌、敌人、UI、素材和风格继续以原 Godot 项目为权威源，不重新设计卡面。
- 本阶段不调用 Blender。草地/裸土地块 FBX 和可复现 Blender 源已完成；只有出现已落盘且可复现的地块模型硬缺陷时，才允许把 Blender 修模写成独占子任务。

## 2. 已完成基线，不得重做

Wave 02B1 已完成并推送：

- EditMode `31/31`、PlayMode `15/15`，均为 0 失败。
- 原 `lighting.png`、`behide.png` 已逐字节接入。
- `CardPlaySession`、`TimelineGrid.CanPlace`、手牌 hover/select/cancel、3D 范围预览、时间轴 valid/invalid 预览和确认放置已闭环。
- 19 格棋盘、0/90/180/270 四向选择、真实 FBX 地块和每层 `0.32` 堆叠已验证。
- Harness 已生成 12 张截图；Windows build 成功；Player 退出码 0 且包含 `TIMEKEY_PLAYER_SMOKE_PASS`。

先读取：

- `docs/migration/unity-3d/00-bootstrap/START_HERE_PROMPT.md`
- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_COMBAT_PROMPT.md`
- `docs/migration/unity-3d/05-progress/current-status.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- `docs/migration/unity-3d/03-workstreams/ownership-map.md`
- `docs/migration/unity-3d/04-verification/test-plan.md`
- `docs/migration/unity-3d/05-progress/known-issues.md`
- `docs/migration/unity-3d/04-verification/evidence/unity-slice-02b1/verification-summary.md`
- 本文列出的 5 份 Wave 02B2A Agent Prompt 和审查记录，均位于 `docs/migration/unity-3d/03-workstreams/agents/`。

## 3. 本阶段的最小可验证结果

本阶段命名为 `Wave 02B2A：七卡数据契约 + earthquake 真实升高闭环`。完成时必须同时满足：

1. Unity 原字节持有七张 `card_data/*.json` 和七张正式卡面；typed definition 保留 `front_image` 映射，stable ID 保留历史拼写 `lighting`。
2. typed effect schema 能无损表达 `damage/elevation/recover/built/poison/clear`，包括 `built.creation`、数值 value 和 clear 字符串 mask；未知或错误 payload 显式失败。
3. 七张真实 fixture 全部通过解析测试，但本阶段只把 `lighting` 和 `earthquake` 接进可玩的战斗闭环。
4. 底部手牌能同时显示并切换真实 `lighting`、`earthquake`，同一时刻最多一张选中；继续复用 02B1 的取消、拖放和输入隔离。
5. `earthquake` 选择中心地块后显示 7 格 axial 范围，时间轴使用 `11` 两格形状；Preview 无副作用，Commit 后才占格。
6. Resolve 时每个仍存在的范围格执行 `logicalHeight += 2`。Unity 零基 elevation `0 -> 2` 必须从 1 个实体块变成 3 个实体块，顶面上升 `0.64`。
7. 每个新增块都有独立 mesh、renderer 和 collider，层间实际世界间距为 `0.32`；地块上的 occupant/选中碰撞/范围高亮使用真实顶层 bounds，不使用固定 `2 * HexBlockHeight` 或只读旧顶部高度。
8. 升高后 0/90/180/270 yaw 均可选择同一坐标；不得生成越界幽灵格。
9. `lighting` 的 02B1 全路径和既有 31/15 测试不回退。

这比“一次实现七种效果”更小、更可审查，也直接验证用户最关心的真实 3D 高度。后续按以下顺序推进：

- `02B2B`：recover 钳制 Max HP、poison 累加状态、built 在空格创建 `tower,count=1,HP=100`；只做效果应用和最小可观察状态，不接塔自损或毒回合 tick。
- `02B2C`：wind/tornado 独立即时 clear 会话；空清合法、越界非法、命中任一格整行动移除，不污染普通 CanPlace/Commit。
- `02B3`：敌方/建筑行动和回合开始状态，包括塔自损与 poison 的快照传播/伤害/衰减；MIG-002 产品规则仍需先冻结。
- `02B4`：抽弃牌、时间币、回合推进、胜负、局内奖励入口；不扩到局外场景。

## 4. 已核对的 Godot 权威语义

七张 fixture：

| stable ID | effect | 地图范围 | 时间轴占格/即时 mask |
| --- | --- | --- | --- |
| `lighting` | `damage 100` | `(0,0),(1,0),(2,0)` | `1` |
| `earthquake` | `elevation +2` | 中心加六邻格 | `11` |
| `wind` | `clear "11,11"` | 不使用地图目标 | 2x2 mask |
| `recover` | `recover 100` | `(0,0),(1,0),(-1,1)` | `111` |
| `tower` | `built creation=tower,value=1` | 中心 | `010,111` |
| `poison` | `poison 2` | 中心 | `110,111` |
| `tornado` | `clear "111111111111"` | 不使用地图目标 | 12x1 mask |

必须冻结以下事实：

- `shape` 是普通卡牌的时间轴占格；`effect_range` 是地图 AOE。
- `clear` 不选地图目标、不创建普通 `TimelineAction`，其 `effects[].value` 是时间轴即时清除 mask。命中多格 action 任一格时移除完整 action；空清合法。
- `earthquake` 中文描述写“抬升1”，但 JSON 和运行代码均为 `+2`，以 `+2` 为准。
- Godot logical height 为一基 `1..6`；Unity elevation 为零基，换算为 `unityElevation = godotHeight - 1`。结果超过 6 层或不高于 0 层时源行为是销毁地块；本阶段至少在纯 Domain 测试冻结该边界，演示 fixture 使用不会销毁的初始高度。
- `built.value` 是最多创建数量，不是等级；塔的回合自损属于后续建筑行动。
- `poison` 重复施加相加；回合开始传播、伤害、衰减留到 02B2B/02B4 接线，不得在本阶段 UI 中猜测实现。
- 普通效果 Resolve 时按稳定 `HexCoord` 重新读取当前 tile/occupant；缺失 tile 为 no-op，不缓存 Unity GameObject 身份。

## 5. 共享契约和实现约束

- `TimeKey.Domain` 保持纯 C#、`noEngineReferences`，不引用 `UnityEngine` 或全局 singleton。
- 保持 `TimelineGrid.CanPlace`、`TryPlace`、`CardPlaySession` 和 `VerticalSliceController` 既有公共行为兼容；契约变更只做加法扩展。
- typed effect 至少包含 `Kind`、numeric amount、可选 `CreationId`、可选 clear mask；CardDefinition 还必须保留清洗过文件名的 `FrontImage`/等价资源引用。保留现有 `new CardEffect(CardEffectKind.Damage, 100)` 兼容面。
- `TimelineAction` 必须保留完整 immutable effects、地图 range 和目标 `HexCoord`，不能继续只抽取 `Damage`。
- `CombatBoardState` 或等价纯领域状态以 `HexCoord` 索引 tile；tile 至少有 logical layer count/elevation 和可选 occupant。Presentation 只消费领域结果。
- 旧 JSON 同一 `value` 字段同时存在 number/string。优先用 `JsonUtility` 对同一源 JSON 做 common/numeric/string 的 typed DTO 视图并按 effect type 组合；禁止 regex、substring 或修改 fixture 来规避异构字段。只有实测证明此方案不可行时，才可增加有明确版本和许可证的结构化 JSON 依赖，并先记录 ADR。
- `CardHandView` 保持单卡视图；新增 hand host/coordinator 管理多卡布局和单选，不把多个卡塞进一个 view，也不把效果规则复制到 UI。
- 高度表现组件必须按实际 block collection/collider bounds 工作；不得让 `VerticalSliceController.CreateTile()` 继续成为唯一可变高度实现。

## 6. 多智能体部署

所有 Agent 都必须被告知：它不是仓库唯一工作者；只能写 Prompt 指定路径和自己的报告；不得回退他人改动，不得切分支、stash、暂存、commit 或 push。主智能体独占共享文档、Controller、BoardTileView、场景、Editor harness、最终证据与 Git。

### Wave A，可并行

- Agent 01：`wave-02b2a-agent-01-seven-card-schema.md`，负责 typed schema、七 JSON adapter/fixture 测试。
- Agent 02：`wave-02b2a-agent-02-card-art-catalog.md`，负责六张新增原卡面、哈希清单和导入证据。

等待两者完成。主智能体审查路径越权、真实 fixture、DTO 异构值方案和全量 EditMode，冻结实际 API 并收回所有权。

### Wave B，仅在 Gate A 通过后

- Agent 03：`wave-02b2a-agent-03-earthquake-domain.md`，负责 earthquake 纯领域状态、Resolve 和边界测试。

这是依赖 Agent 01 schema 的单独波次，不能为增加并发提前启动。

### Wave C，可并行

- Agent 04：`wave-02b2a-agent-04-two-card-hand.md`，负责两张真实卡的 hand host/coordinator 和多卡 PlayMode。
- Agent 05：`wave-02b2a-agent-05-elevation-presentation.md`，负责真实块柱增减、顶面 bounds 和四向选择 PlayMode。

主智能体等待两者完成、审查并收回所有权，再独占修改 `VerticalSliceController.cs`、`BoardTileView.cs`、场景和 harness 完成接线。任意时刻只运行一个 Unity Editor/batchmode；MIG-006 的低内存风险仍生效。

## 7. 主智能体执行顺序

1. 重查分支、远端、磁盘、Unity 版本、ASCII junction、工作区脏文件；若与本文冲突，以当前文件证据为准并更新状态。
2. 完整读取所有 Agent Prompt 与 `prompt-review-wave-02b2a.md`；重新做 11 项内容和路径集合审查。
3. 启动 Wave A 两个 Agent；等待、审查 diff/报告/测试后冻结 API。
4. 启动 Wave B Agent 03；通过 pure Domain 门禁后收回所有权。
5. 启动 Wave C Agent 04/05；等待、审查组件级 PlayMode 和实际渲染证据。
6. 主智能体完成共享接线，动态加载两张卡；普通卡使用地图目标流程，禁止给未来 clear 复用错误的 target 流程。
   卡图必须来自 CardDefinition 的 `front_image` 映射，不能在 Controller 用 stable ID 特判 `tower_card/poison_card`。时间轴 invalid 状态除红色外还要有边框、标记或形状冗余，不能只靠顶部英文状态文字区分敌方意图。
7. 新增最终 EditMode/Infrastructure/PlayMode；旧测试必须全回归。
8. 运行 harness、Windows build 和 Player smoke；实际检查截图，不以非空像素或退出码代替视觉结论。
9. 更新 parity、test plan、known issues、current status、push status 和 Agent 报告引用。
10. `git diff --check`，精确暂存本波文件，确认 4 个保护文件和日志/build/Library 不在暂存区，创建单一目的提交并 push `origin/unity_7.31`。
11. 门禁通过后立即准备 02B2B Prompt；不要直接实施敌方行为、完整回合或局外。

## 8. 自动化与视觉门禁

- Infrastructure：七 fixture 全部解析；stable/numeric ID、effect payload、range、普通 shape/clear mask 精确；错误 type/value/creation/mask 显式失败。
- EditMode：`earthquake` 7 格 `+2`、越界忽略、Resolve 时缺失 tile no-op、1 层到 3 层、上限销毁策略、两格时间轴占格，以及 02B1 preview/commit 回归。
- Cards PlayMode：`lighting`/`earthquake` 两卡均显示原图，单选互斥，hover/cancel/drag/input gate 不回退；重复 Build 不复制 view/监听。
- Terrain PlayMode：每个 logical layer 对应独立 mesh/renderer/collider；连续层 y 差为 `0.32`；顶层 bounds、occupant 和选择 collider 随升高更新。
- 集成 PlayMode：真实 `earthquake` 完成选卡 -> 中心目标 -> 7 格预览 -> 两格 timeline preview -> Commit -> Resolve -> 7 个真实柱各加两层；`lighting` 旧闭环继续通过。
- 截图至少包含七卡素材 contact sheet、`earthquake-selected-1920x1080.png`、`earthquake-range-yaw-000/090/180/270-1920x1080.png`、`earthquake-timeline-valid/invalid-1920x1080.png`、`earthquake-before/after-1920x1080.png`、`two-card-hand-1280x720.png`、`two-card-hand-2560x1080.png`。
- 像素/结构门禁分别检查卡牌区域、时间轴区域和棋盘升高区域；人工检查遮挡、裁切、卡面比例、层间穿插、悬空 occupant、错误 collider 和旋转后选择偏移。
- Windows build 成功；Player 退出码 0 且日志含 `TIMEKEY_PLAYER_SMOKE_PASS`。

## 9. 硬阻塞和保护规则

只有以下情况允许停止并请求用户决策：

- 当前分支不是 `unity_7.31`，且切换会覆盖无法保护的本地改动。
- 真实 Godot 运行/源码在 `earthquake +2`、`0.32` 或局内范围上出现相互矛盾且无法通过只读检查消解。
- 必须修改局外流程、旧存档语义或素材授权范围才能完成本切片。
- Unity 许可证/工程锁/磁盘故障在清理安全进程并按命令目录重试后仍阻止所有测试与渲染。

下列不是硬阻塞：未完成发布授权、敌人意图 no-op、局外未迁移、未来 clear/built/poison 未接线、push 的一次性网络失败。

必须始终保护并保持未暂存：

- `default_bus_layout.tres`
- `scene/in_scene/rewards/resources/default_craft_recipe_book.tres`
- `shaders/color_BG.gdshader`
- `shaders/game_over.gdshader`

不得使用 `git add -A`、`git reset --hard`、`git checkout --`、历史改写或清理不明确目录。GitHub TLS 校验警告 MIG-012 仍需记录，不得擅自修改用户级 Git/GCM 配置。

# 主 Prompt 结束
