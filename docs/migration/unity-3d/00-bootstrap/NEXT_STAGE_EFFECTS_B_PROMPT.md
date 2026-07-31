# 《时之钥》Unity Wave 02B2B 接手 Prompt

> 状态：02B2B 聚焦参考；不得绕过 `NEXT_STAGE_DECOUPLING_PROMPT.md`，解耦后优先执行 `NEXT_STAGE_REMAINING_CARDS_PROMPT.md`
> 负责人：下一阶段主智能体
> 最后编写日期：2026-08-01
> 当前分支：`unity_7.31`
> 前置任务：`docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md`
> 前置证据：`docs/migration/unity-3d/04-verification/evidence/unity-slice-02b2a/verification-summary.md`

# 主 Prompt

本文是 Wave 02B2A 按原阶段要求准备的 02B2B 聚焦参考。当前工作区已存在更晚冻结的严格队列：先完成 `NEXT_STAGE_DECOUPLING_PROMPT.md`，再以 `NEXT_STAGE_REMAINING_CARDS_PROMPT.md` 为权威执行剩余卡牌。前置解耦未完成时，不得按本文修改代码、启动 Agent、运行 Unity 或创建提交。

前置门禁通过后，你是《时之钥》Godot 到 Unity 迁移的下一阶段主智能体、Unity 局内战斗玩法负责人和最终集成负责人。进入 `D:\godot\时之钥\时之钥`，确认当前分支为 `unity_7.31`，完整读取本文、`NEXT_STAGE_REMAINING_CARDS_PROMPT.md` 和最新迁移账本；若接口、所有权或范围有冲突，以后者和解耦后的真实工程为准。

本阶段只实现 `recover`、`poison`、`tower/built` 三张卡的领域结果和最小可观察表现。除本文定义的硬阻塞外，不要停在复述、计划或普通实现问题上；持续推进测试、实际渲染证据、账本、Git 检查点和 `origin/unity_7.31` 推送。

## 1. 接手与保护

1. 搜索 Mem0 的项目约定，以当前文件和用户最新要求为准。
2. 完整读取 `NEXT_STAGE_EFFECTS_PROMPT.md`、Wave 02B2A 五份 Agent 报告、ADR-0003、integration contracts、ownership map、test plan、parity matrix、current status 和 known issues。
3. 重新检查 `git status --short --branch`、远端、Unity 版本和是否有运行中的 Unity 实例。
4. 保护所有既有未提交改动。至少不得修改、暂存或提交：
   - `default_bus_layout.tres`
   - `scene/in_scene/rewards/resources/default_craft_recipe_book.tres`
   - `shaders/color_BG.gdshader`
   - `shaders/game_over.gdshader`
5. 不得改写、删除或暂存来源不明的未跟踪文件。不得使用 `git add -A`、自动 stash、历史改写或破坏性回退。
6. Unity 仍使用 6000.4.10f1 和现有 ASCII junction；受 MIG-006 约束，任意时刻只运行一个 Unity/Godot/Blender 图形或 batchmode 进程。

如果 02B2A 的实现、测试、证据和账本相互矛盾，先用最小复现确认真实基线；只修复阻塞本阶段的前置回归，不做相邻重构。

## 2. 继承基线

以下能力是冻结输入，不重复实现：

- 七张真实 fixture 的 typed schema、stable/numeric ID、`FrontImage`、range、普通 shape 和 clear mask。
- `lighting` damage 100 闭环。
- `earthquake` 中心加六邻格各 `+2`；真实 mesh/renderer/collider 每层间隔 `0.32`，顶面/occupant anchor 同步，四向可选。
- 普通卡的地图目标、时间轴 Preview/Commit/Cancel 无副作用契约。
- CardHandHost 的多卡稳定顺序、单选互斥、取消、drag 和镜头输入隔离。

未被本阶段修改覆盖的 02B2A 证据不机械重做；最终仍必须运行完整 EditMode、PlayMode、build 和 Player smoke，确认没有回归。

## 3. 精确范围

### Recover

- fixture：`recover.value=100`，range 为 `(0,0),(1,0),(-1,1)`，普通 shape 为 `111`。
- 只允许选择仍存在、存活、支持生命值且 `HP < MaxHP` 的 occupant。
- Resolve 时按稳定 `HexCoord` 重新查询当前 occupant；空格、目标消失、死亡或已满血均为 no-op。
- 结果严格为 `min(MaxHP, HP + 100)`；不得把治疗实现成负 damage，不得超过 MaxHP。
- 快照必须能观察目标坐标、before HP、after HP 和是否应用。

### Poison

- fixture：`poison.value=2`，range 为中心单格，普通 shape 为 `110,111`。
- 只允许选择仍存在、存活且支持状态的 occupant。
- Resolve 时重新查询；空格、死亡或目标消失为 no-op。
- 成功时整数层数直接 `+2`；重复施加继续累加，不覆盖旧值，不增加隐藏上限。
- 快照必须能观察 before/after poison stacks。
- Presentation 提供最小可辨识层数或图标反馈，并保持地块和 occupant 可选。

### Tower / Built

- fixture：`built.creation=tower`、`value=1`；含义是本次最多创建一个 occupant，不是塔等级。
- 只能选择真实存在且当前没有 occupant 的地块；选择阶段和 Resolve 阶段都必须校验。
- Resolve 前被其他对象占用时为 no-op，不替换、不叠加、不创建幽灵对象。
- 成功结果为中立 Tower，初始 `HP=100`，保存稳定 `HexCoord`，Presentation 挂到真实地块 `OccupantAnchor`。
- 使用仓库内可人工维护的 Tower Prefab 和原项目 Tower 素材；不要在 Controller 中程序化拼装永久结构。

## 4. 明确非目标

- 不实现 Tower 创建所在回合或后续回合的自损 50。
- 不实现 poison 的回合开始传播、伤害、衰减或新感染连锁。
- 不实现 `wind/tornado` clear 会话。
- 不实现完整敌人行动、抽弃牌、时间币、胜负、奖励、局外流程、最终 VFX/音频或旧档转换。
- 不为了三张卡建立通用脚本语言、反射式效果框架或新的资源系统。

这些边界属于后续 02B2C/02B3；本阶段测试应明确证明 Tower 不自损、Poison 不自动 tick。

## 5. Godot 权威参考

至少完整读取并对照：

- `scene/in_scene/effect/effect_processor.gd`
- `scene/in_scene/timeline/commands/RecoverCommand.gd`
- `scene/in_scene/timeline/commands/BuiltCommand.gd`
- `scene/in_scene/timeline/commands/PoisonCommand.gd`
- `scene/in_scene/hex_map_modules/rules/HexTargetRules.gd`
- `scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd`
- `scene/in_scene/enermy/tower.gd`
- `scene/in_scene/status/status_component.gd`
- `scene/global/StatusDB.gd`
- 七张源 JSON 和 `image/effect/poison_icon.png`、Tower 原素材。

若注释、文档和可执行 Godot 行为冲突，以可复现行为为当前基线，并把差异写入 known issues；不得凭文案猜测扩展语义。

## 6. 依赖与多智能体波次

先生成本阶段互斥 Prompt 和 prompt review，再按下列依赖执行。主智能体独占共享 Controller、BoardTileView、Scene、Editor harness、程序集/Packages、共享账本、最终证据与 Git。

- Wave A：Agent 01 负责 occupant/HP/status 的纯 Domain 模型与测试；Agent 02 负责 Tower/Poison 素材审计、导入和独占报告。两者路径必须互斥。
- Gate A：主智能体审查稳定坐标重判、HP/MaxHP、poison stacks、occupant 空闲判定和素材哈希；运行 EditMode/导入门禁。
- Wave B：Agent 03 负责 Recover effect/result 与 EditMode；Agent 04 负责 Poison + Built effect/result 与 EditMode。共享基类或 result union 由主智能体在两者启动前冻结，禁止并发写同一文件。
- Gate B：主智能体合并并运行完整 EditMode；确认 no-op 不改变状态、重复 poison 可累加、built 不覆盖 occupant。
- Wave C：Agent 05 负责 Tower Prefab、poison 最小状态表现和独占 PlayMode/截图；主智能体随后接入共享手牌、目标、时间轴、Scene 与 harness。

每个 Agent 都必须被告知：它不是仓库唯一工作者，不得回退他人修改，不得修改共享路径，不得切分支、stash、暂存、commit 或 push；完成后报告改动、证据、失败项和风险并交回所有权。

## 7. 实现原则

- Domain 保存稳定 ID/坐标/数值，不保存 Unity GameObject、Component 或 Transform。
- Presentation 只消费 Domain 结果，不复制治疗钳制、目标合法性、poison 累加或 built 重判规则。
- 普通卡继续使用现有 `CardPlaySession` 与 `TimelineAction.FromCard`；不得按 stable ID 在 UI 中硬编码第二套 effect 语义。
- 如果现有 effect dispatch 必须扩展，使用最小显式分支或与仓库一致的窄 handler；不要为三个 effect 引入抽象工厂或依赖注入框架。
- `FrontImage` 仍是卡面资源的唯一内容映射来源。
- 动态 occupant 必须使用 Prefab；稳定 Canvas、Timeline、BoardRoot 和预览对象不由运行时重复重建。
- 重复 `BuildSceneGraph`、重复 Preview、取消、失败 Commit、失败 Resolve 均不得复制监听、占格、状态或 GameObject。

## 8. 自动化门禁

### EditMode

- Recover：合法目标、满血非法、目标消失、死亡、范围缺失、100 点钳制 MaxHP、三格 shape 边界、no-op 状态不变。
- Poison：活体成功、空格/死亡/目标消失 no-op、首次 `0->2`、重复 `2->4`、快照 before/after、本阶段不 tick。
- Built：空地成功、已占用非法、Resolve 前被占用、未知 creation 显式失败、value 只创建一个、Tower HP 100/中立/坐标正确。
- 既有七卡 adapter、lighting、earthquake、timeline purity、seed 731 和 EffectResults 全部回归。

### PlayMode

- 三张新卡从真实 catalog/front_image 建立手牌，单选互斥、取消、drag 和重复 Build 不回退。
- Recover 合法/非法范围反馈、三格时间轴 Preview、Resolve 后 HUD/世界 HP 同步。
- Poison 两次 Resolve 后层数 4，可观察表现更新但不遮挡目标选择。
- Tower 只在空地创建一个 Prefab occupant，挂到真实 `OccupantAnchor`，四向可见/可选；占用冲突不创建对象。
- Terrain 升高后创建 Tower 或既有 occupant 时仍使用最新 anchor。
- lighting/earthquake 完整 PlayMode 回归。

### 集成

- 完整 EditMode/PlayMode 均为 0 失败。
- Editor harness/scene validation 为 0，Windows x64 build `Succeeded`。
- Player 退出码 0 且日志包含 `TIMEKEY_PLAYER_SMOKE_PASS`。
- 原始日志留本地并忽略；提交 NUnit XML、结构化 JSON、PNG 和人工视觉总结。

## 9. 视觉门禁

实际渲染并打开检查：

- 1280x720 与 2560x1080 的多卡手牌，无裁切、关键遮挡或不可辨识卡面。
- Recover 合法/非法目标、结算前后 HP。
- Poison 0/2/4 层状态表现，不遮挡 occupant、范围或地块选择。
- Tower 创建前后、occupant anchor、碰撞和 yaw 0/90/180/270。
- 每张新卡至少一张 valid/invalid 时间轴证据；invalid 继续使用颜色外的冗余标记。

截图必须检查真实渲染，不得用编译成功、场景加载或全图非空像素代替。检查裁切、重叠、文字、卡面比例、浮空、穿模、过期高亮和四向选择。

## 10. 文档、Git 与完成定义

完成后更新 integration contracts、ownership map、master backlog、test plan、parity matrix、current status、known issues、completed slices、push status 和 command catalog。保存各 Agent 报告、Gate XML、最终 evidence summary，并生成下一阶段 `Wave 02B2C wind/tornado clear` Prompt。

提交前：

1. 运行 `git diff --check` 和完整验证。
2. 使用精确路径暂存，核对 `git diff --cached --name-only`。
3. 确认四个用户脏文件、来源不明文件、Unity Library/Logs/Build 和原始日志不在暂存区。
4. 形成单一目的提交并推送 `origin unity_7.31`；push 失败则保留提交并记录准确错误，不修改用户级 TLS/GCM 配置。
5. 收工前按 AGENTS.md 搜索同主题 Mem0，更新短小、可复用的项目记忆，不存提交哈希和瞬时输出。

只有测试/实际渲染/build/Player smoke/账本/Git 检查点全部完成，或者遇到本文硬阻塞，Wave 02B2B 才可结束。

## 11. 硬阻塞

只在以下情况停止并请求用户：

- 分支不正确且切换会覆盖无法保护的用户改动。
- Godot 真实语义与冻结契约在 HP/occupant/poison 核心模型上冲突，且本地证据无法消解。
- 必须修改局外流程、旧存档或素材授权范围才能继续。
- Unity 许可证、工程锁、磁盘或编辑器故障在安全串行重试后仍阻止全部验证。
- 必须删除、覆盖用户文件或执行不可恢复操作。

Tower 自损、poison tick、clear 和完整敌人行为不是本阶段阻塞，必须按非目标留给后续。

# 主 Prompt 结束
