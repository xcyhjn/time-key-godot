# 《时之钥》Unity 回合生命周期接手 Prompt

> 状态：排队等待执行；必须在 `NEXT_STAGE_REMAINING_CARDS_PROMPT.md` 的 Gate D 完成并返回后启动
> 负责人：下一阶段接手主智能体
> 最后编写日期：2026-08-02
> 当前分支：`unity_7.31`
> 前置任务：`docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_REMAINING_CARDS_PROMPT.md`

# 主 Prompt

你是《时之钥》Godot 到 Unity 迁移的下一阶段主智能体、Unity 局内回合生命周期负责人、多智能体编排者和最终集成负责人。进入 `D:\godot\时之钥\时之钥`，确认当前分支为 `unity_7.31`，完整读取本文、Remaining Cards Gate D 最终报告、继承证据账本和当前真实代码，然后立即实施 `Wave 02B3：回合生命周期、敌人/建筑行动与状态结算`。

本文是 `NEXT_STAGE_REMAINING_CARDS_PROMPT.md` 的严格后继任务。本文“主 Prompt”是本阶段唯一执行规范；不得把旧 Prompt 中已经关闭的 Gate 当作待办重做，也不得与 02B4、局外迁移或体验重制并行。除本文定义的硬阻塞外，不要停在复述、计划或普通实现问题上；持续推进代码、Scene/Prefab、测试、维护文档、视觉证据和 Git 检查点。

## 0. 等待 Remaining Cards Gate D 完成

只有同时满足以下条件，才允许修改代码、Unity Scene/Prefab 或文档：

1. `NEXT_STAGE_REMAINING_CARDS_PROMPT.md` 的接手主智能体已经返回 Gate D 最终报告。
2. 前置阶段启动的全部子智能体均已完成、停止或明确交回所有权；没有 Agent 或 Unity 进程仍在写工作区。
3. Remaining Cards 最终集成态的完整 EditMode、PlayMode、Windows build 和 Player smoke 均为 0 失败；若实际账本数值与本文预期的 EditMode `152/152`、PlayMode `38/38` 不同，以 Gate D 最终 XML 和 `verification-summary.md` 为准，并先解释差异。
4. 七卡交互、Tower/Poison 表现、Clear 会话、保存的 Scene/Prefab、54 张 Gate D PNG 或等价完整视觉集已经通过人工检查。
5. 前置 Gate D 的提交已形成；push 成功则继承远端同步状态，push 失败则继承准确失败原因，不把网络问题误判为代码失败。

如果本文被提前发送，不要启动 Agent、不要修改文件、不要运行 Unity、不要暂存或提交；只报告仍在等待哪一个前置任务。这是排队硬门禁。

## 1. 继承 Gate D 的可信基线

完整读取：

- `04-verification/inherited-verification-ledger.md`
- `04-verification/evidence/remaining-cards-gate-d/verification-summary.md`
- `04-verification/evidence/remaining-cards-gate-d/harness-summary.json`
- `02-architecture/combat-modular-architecture.md`
- 最新 ADR、integration contracts、ownership map、test plan、parity matrix、current status、known issues 和 push status
- `06-maintenance/add-card.md`、`add-effect.md`、`add-enemy.md`、`debugging-guide.md`、`scene-and-prefab-guide.md`、`testing-and-evidence.md`
- 当前 Domain/Application/Infrastructure/Presentation/Composition API、测试、Scene、Prefab 和 Editor harness

继承未被本阶段修改覆盖的七卡 schema 与原图哈希、地图/高度/镜头、普通时间轴、Clear、Recover/Built/Poison 卡牌结算、Scene 可编辑性和视觉证据，不机械重做已经关闭的 Gate 0/A/B/C。

以下证据必须刷新：

- 回合状态机、时间轴结算、敌方意图、Tower decay、Poison turn-start 和 occupant removal 修改到的全部自动化测试。
- Enemy intent、Tower 100→50→移除、Poison 传播/伤害/衰减、死亡与 Presenter 同步的真实截图。
- 最终集成态的完整 EditMode、PlayMode、Windows build 和 Player smoke。
- 受影响的三视口、四 yaw、Scene/Prefab authoring 和重复初始化证据。

若代码、文档和证据冲突，以当前文件与实际运行结果为准；先形成独立、可复现的 Gate 0 检查点，再开始功能实现。

## 2. 本阶段边界

本阶段只关闭 `Wave 02B3`：

- 固定可观察、可测试、不可重入的回合阶段。
- 让玩家与敌方 action 在同一 12×3 时间轴中按稳定顺序结算。
- 用真实 occupant 生成并展示敌方意图，在地图变化和 Resolve 前重判。
- 在整条时间轴之后执行本回合建筑行为；Tower 在创建所在结束回合立即自损 50。
- 在新回合开始使用全图快照分三步处理 Poison：传播、伤害、衰减。
- 通过 typed result/snapshot 同步 HP、状态、死亡、tile occupant、Timeline、HUD 和 Presenter。

本阶段不实现以下 02B4 内容：

- 手牌弃置、抽牌、洗牌、牌库、弃牌堆和正式 starter deck 循环。
- 时间币产出/消费、Era/回合资源推进和玩家可见的回合计数规则。
- 胜利、失败、战斗结算、奖励入口和返回局外。
- 正式多敌人内容库、难度曲线、Boss、最终动画/VFX/音频或局外迁移。

允许为生命周期保留纯内部、不可参与玩法计算的 cycle/phase sequence number，供幂等、trace 和测试使用；不得把它伪装成 02B4 的正式回合推进。

## 3. 冻结回合阶段与执行顺序

Domain/Application 必须只有一个回合编排入口，并显式暴露当前 phase。目标状态机如下：

```text
PlayerReady
  -> EndTurnRequested / InputLocked
  -> ResolvingTimeline
  -> RunningBuildingBehaviors
  -> ClearingTimeline
  -> ProcessingTurnStartStatuses
  -> RefreshingEnemyIntents
  -> PlayerReady / InputUnlocked
```

每个阶段只能单向前进一次；结算中重复点击、重复命令、重复回调或 Presentation 重绑不得二次执行 action、Tower decay 或 Poison tick。失败必须返回结构化原因，且不得把 session 留在半完成状态。

### 3.1 时间轴阶段

- 按时间列 `x=0..11` 从左到右；同列按 `y=0..2` 从上到下。
- 同一多格 `TimelineAction` 只按稳定 action identity 执行一次，执行后移除其全部占格。
- 玩家与敌方 action 不按阵营另开第二条队列；它们只由时间轴坐标决定先后。
- 每个 action Resolve 时使用当前 Domain state 和保存的稳定 ID/`HexCoord` 重判，不使用 `GameObject`、旧 View 或选择阶段对象引用作为 identity。
- Timeline 全部 action 结束后才进入 building phase。不得让 Tower 或其他建筑在每一列之间行动。
- Clear 已移除的 action 不得复活或结算；结算期间不再允许 Clear/卡牌输入穿插。
- UI 清空属于 `ClearingTimeline`，必须在结果已经提交后执行；清 UI 不能成为 Domain mutation 的来源。

### 3.2 建筑阶段

- 在时间轴全部 Resolve 后，对此时仍存在的建筑拍稳定快照，再按 `HexCoord`、runtime ID 的确定性顺序各执行一次。
- 时间轴中刚由 Tower 卡创建的 Tower 必须进入这份快照，所以创建所在结束回合即从 HP 100 自损到 50。
- 下一次结束回合再次自损 50，HP 归 0 后立即走统一死亡/移除路径；不得留下 Domain occupant、tile 占用、Poison icon、Collider 或 Presenter 残影。
- Tower 不生成敌方意图、不额外写入 Timeline，也不因重复刷新 View 再次自损。
- 本阶段不要发明其他建筑行为。新增建筑行为必须以后通过 registry/handler 扩展，而不是在回合编排器中按 creation/stable ID 增加大 switch。

### 3.3 新回合开始阶段

- `ProcessingTurnStartStatuses` 在 building phase 和 timeline 清理之后执行。
- Poison 完成后才刷新下一轮敌方意图。02B4 将来的 phase advance、draw/shuffle 应插入 Poison 与敌方意图刷新之间的已命名扩展点；本阶段该扩展点保持显式 no-op。
- 初始进入战斗也走同一套 start-turn 编排，只是不执行 02B4 的 advance；不得维护第二套初始化意图/状态逻辑。
- 所有下一轮敌方意图生成并写入 Timeline/Presentation 后，才能回到 `PlayerReady` 并解锁输入。

## 4. MIG-002：敌方意图必须成为真实生命周期数据

当前 Godot `EffectProcessor._parse_enemy_intent()` 返回空命令，这是 MIG-002 的源缺口。Unity 不得继续用 Composition 中硬编码的 `enemy-intent`、伤害 0 marker 假装敌人系统已完成，也不得凭描述发明权威源中不存在的伤害。

本阶段必须完成：

1. 由仍存在、存活、允许生成意图的 occupant 候选生成 typed enemy intent；稳定数据至少包含 intent ID、source runtime ID/source coord、target coord、shape、priority、effect payload 或明确的 unsupported/no-effect reason。
2. 候选与放置使用显式 seed；优先级从高到低，同优先级的选择和放置结果必须可复现。生成数量、形状和 12×3 边界沿用核对后的 Godot 可观察规则。
3. Timeline 方块、地图 source/target/range 高亮、tooltip/HUD 必须消费同一个 intent snapshot；hover/cancel/clear 后不串到其他 stable ID。
4. 地块升降、occupant 死亡/替换/移除和目标拓扑变化后主动重判；Resolve 前再做最终重判。
5. 最终重判至少验证：source identity 仍存在且存活、仍允许该 intent；target tile 仍存在；需要 occupant 的 target identity/attitude/HP 仍合法；shape/Timeline action identity 仍匹配。
6. 失效 intent 必须完整移除全部占格和地图/Timeline 表现，返回稳定 invalid reason，不执行、不静默改目标。重新选目标只发生在下一次意图生成阶段。
7. 若源数据能冻结出真实 typed effect，则通过独立 enemy-intent handler 执行并输出 before/after；若权威源仍只有空 command，则以 `UnsupportedSourceCommand` 或等价显式结果完成 no-op，trace 和 UI 必须可观察，不能把伤害 0 记录成成功攻击。

Gate 0 必须重新核对至少一个真实 Godot occupant 的 `can_generate_intent()`、priority、shape、target resolver、description/range 和 action data。核心语义能够由当前源码消解时直接冻结；只有不同权威证据对实际效果或目标产生不可调和冲突时才构成硬阻塞。

Presentation 不生成意图、不选择目标、不重算合法性。保存的 Scene/Prefab 只显示 Application snapshot；`GameObject` 不得成为 source/target identity。

## 5. Tower 生命周期必须与创建当回合对齐

- Tower 保持 Remaining Cards Gate B 的中立 occupant 契约：`creation=tower`、初始 `HP=MaxHP=100`、真实 `HexCoord`、保存的 Tower Prefab、真实 `OccupantAnchor`。
- Tower 的 building behavior 固定为每次 building phase 自损 50；使用普通伤害/生命值 mutation 的统一路径，并输出 `BeforeHp`、`AfterHp`、coord、runtime ID 和 removal result。
- 创建当回合：玩家 Built action 在 Timeline Resolve 中创建 HP100 Tower；整条 Timeline 结束后的同一 building phase 必须得到 HP50。
- 后续回合：HP50 Tower 在 building phase 变为 0，并立即从 board occupant map、tile occupancy、状态注册、选择/范围查询和 Presenter 中移除。
- Resolve 前空地重判、一次最多创建一个和 Prefab authoring 继续继承，不得为实现 decay 修改 `built.value=1` 的语义。
- Tower 被 Poison 或其他伤害提前杀死时必须复用同一 removal transaction；后续 building snapshot 不再执行它。

Tower 的死亡表现可以有最小可观察退场，但 Domain removal 不得依赖动画完成。动画被禁用、对象已销毁或 Player 退出时也必须保持状态正确。

## 6. Poison 必须按全图快照做三个全局 pass

`ProcessingTurnStartStatuses` 开始时，一次性拍摄全图不可变快照。快照至少包含每个 occupant 的 runtime ID、`HexCoord`、存活/状态能力、HP/MaxHP 和 Poison 层数。随后严格执行三个全局 pass：

### Pass 1：六邻传播

- 只让快照中 `PoisonStacks>0` 且当时存活、支持状态的旧中毒 occupant 作为传播源。
- 每个传播源向六个 axial 邻居坐标各传播 1 层；只影响该邻格上当前仍存在、存活、支持状态的 occupant。
- 多个旧中毒源对同一 occupant 的贡献相加，应用时使用 checked integer 规则，不设隐藏上限。
- 传播计算只读整张 phase-entry 快照，再批量应用。新感染者和本 pass 增加的层数不得在同一回合继续传播。

### Pass 2：快照伤害

- 只对快照中的旧中毒源执行一次伤害。
- 伤害固定为 `ceil(MaxHP * 0.1 * SnapshotPoisonStacks)`；使用快照中的 MaxHP 和层数，不使用传播后的当前层数。
- 伤害钳制 HP，死亡进入统一 death/removal 结果。某 occupant 在 Pass 1 后被外部表现销毁不能改变 Domain 的确定性。
- 新感染者本回合不受 Poison 伤害。

### Pass 3：层数衰减

- 只对快照中的旧中毒源衰减一次。
- 读取完成传播后的当前层数并减 1，最低为 0；因此旧中毒者同回合收到的邻居传播会保留，仅扣一次。
- 新感染者本回合不衰减。
- 已按死亡策略被移除的 occupant 不再保留悬空状态；若源规则要求 Broken occupant 留在 tile，必须由 Gate 0 冻结的 typed death policy 明确表达，并保证 Presenter 与选择规则一致。

每个 pass 产出可审计结果：source/target ID、coord、snapshot stacks、spread delta、damage、before/after HP、before/after stacks 和 removal。Presenter 只能消费最终 snapshot/result，不得按帧自己扩散、扣血或衰减。

## 7. 死亡、tile occupant 与 Presenter 必须原子同步

- Domain 是 occupant identity、HP、status 和 tile occupancy 的唯一真相来源。
- HP 归 0 时，由冻结的 death policy 决定 `Remove` 或 `RemainBroken`；Tower 必须 `Remove`。若本阶段最小敌人 fixture 死亡语义没有权威依据，不得静默选择，先用源码消解并写 ADR。
- `Remove` 必须作为一个 Domain transaction：校验 runtime ID 与 coord 仍匹配，清 occupant registry 和 tile slot，清状态，再输出不可变 removal result。
- Application 按 phase sequence 发布结果；Presentation 先停止对应 intent/status 表现，再销毁 occupant View。动画只延迟 View 销毁，不延迟 Domain mutation。
- `CombatOccupantPresenter` 或其拆分后的 presenter 必须按 runtime ID 幂等刷新。重复应用同一 snapshot、Scene disable/enable 和 binding 重建不得复制 Tower、Poison icon、敌人 intent overlay 或订阅。
- occupant 被移除或替换后，Board range、target selection、enemy intent、Timeline tooltip 和 HUD 在同一 phase 结束前全部指向最新状态。

## 8. 使用现有模块边界与 Scene/Prefab

- 优先扩展现有 `CombatApplicationSession`、typed effect/result、occupant state、Timeline identity、registry、Composition 和 Presenter 边界；不得把 phase、enemy、Tower 或 Poison 规则塞回 `VerticalSliceController`。
- Domain/Application 保持 `noEngineReferences=true`，不得引用 UnityEngine、Resources、Prefab、MonoBehaviour 或场景路径。
- 回合阶段编排与具体行为分离：phase runner 组织顺序；enemy intent、building behavior、turn-start status 使用各自 handler/processor/registry。不要建立全局 Service Locator。
- Presentation 只订阅 Application 结果；不得复制目标重判、Tower damage、Poison 公式、death policy 或 tile removal。
- Stable Camera、Canvas、EventSystem、HUD、Timeline、CardHandHost、BoardRoot、Preview 和固定锚点继续保存于 Scene，不得在运行时重建或覆盖设计者布局。
- Tower、Poison status、CardView、TimelineCell 和 TargetView 继续来自已保存 Prefab。敌方意图若需要新 View/overlay，应新增最小、可在 Inspector 编辑的 Prefab 或保存组件，并配套 `.meta`、authoring 测试和人工检查。
- 动态对象仅限当前状态实际需要的 occupant/status/intent/action marker；重复 Build/Initialize/Bind 不得复制节点或监听。
- 默认不新增包。确需依赖时先证明当前切片需要，记录版本、用途、许可证和 ADR。

## 9. 按门禁实施

### Gate 0：接手、源语义与契约冻结

1. 确认前置 Gate D/子智能体/Unity 进程均已结束；重查分支、远端和 dirty-file inventory。
2. 读取当前 Scene/Prefab、程序集、接口、维护指南、测试和 Gate D 证据；运行最小只读冒烟。
3. 从 Godot 源冻结 end-turn、Timeline、building、start-turn status、enemy intent generation/revalidation 的真实顺序和 MIG-002 缺口。
4. 冻结本文 phase、Tower、Poison、death/removal、enemy unsupported 结果和 02B4 no-op hook 契约。
5. 更新 integration contracts、ownership、test plan、parity matrix 和 Agent prompts，形成单一目的检查点。

### Gate A：纯回合编排器

先实现 phase runner 和确定性 timeline order，不接 Tower/Poison 具体行为。证明 phase 只前进一次、输入锁定、action identity 去重、玩家/敌人同 grid 排序、失败恢复和 02B4 hook 保持 no-op。Controller 不得新增 gameplay 分支。

### Gate B：敌方意图生成、展示与重判

移除硬编码固定 marker，接入真实 occupant 候选、显式 seed、priority/shape/target、地图与 Timeline 共同展示、topology/死亡触发重判和 Resolve 最终重判。对 MIG-002 的空命令输出显式 unsupported/no-effect reason；不发明伤害。完成 intent Presenter/Prefab authoring 和视觉检查。

### Gate C：Tower 与 Poison 生命周期

先接 Tower building handler，证明创建当回合 100→50、下一回合 50→0 并完整移除；再接 Poison 三 pass 快照处理，覆盖多源传播、伤害、衰减、新感染隔离、死亡/移除和 Presenter 同步。两者必须通过同一个 phase runner，不能各自维护回合计时器。

### Gate D：集成与交付

在确定性场景完成至少两个连续生命周期，串联玩家 action、敌方 intent、Tower、Poison、clear、死亡和下一轮 intent。运行完整测试、build、Player smoke 和人工视觉检查；更新架构/ADR/维护账本，生成 02B4 后继 Prompt，精确提交并尝试 push。

## 10. 多智能体部署与所有权

先启动最多三个只读 Agent，主智能体保留一个槽：

1. `turn-lifecycle-source-semantics`：只读核对 Godot end-turn/start-turn、Timeline 排序、enemy intent、Tower、Poison 和 death/occupancy。
2. `turn-lifecycle-unity-architecture-audit`：只读审查当前 Application/Domain/Timeline/occupant/result 扩展点、测试缺口和最小修改面。
3. `turn-lifecycle-scene-visual-audit`：只读核对 Scene/Prefab、enemy intent/Tower/Poison 表现、三视口/四 yaw 风险和 harness 扩展点。

主智能体汇总后，在 `03-workstreams/agents/` 生成 Prompt、prompt review 和最终报告。实现 Agent 必须所有权互斥；建议边界：

- Gate A：一个 Agent 只拥有新 lifecycle Domain/Application 文件与对应 EditMode 测试；主智能体独占共享 session/composition 接线。
- Gate B：一个 Agent 拥有 enemy intent 纯数据/handler/test；另一个 Agent 在契约冻结后拥有 intent Presentation/Prefab/PlayMode 路径。
- Gate C：一个 Agent 拥有 building/status processor 与纯测试；另一个 Agent 在结果类型冻结后拥有 occupant/status Presenter 测试和必要 Prefab 调整。
- 主智能体始终独占共享 Scene、`VerticalSliceController`、Composition root、asmdef、registry 汇总、Editor harness、共享文档、最终证据、Git 暂存、提交和推送。

每个 Agent Prompt 必须包含单一目标、必读路径、拥有/禁止路径、冻结契约、非目标、测试与视觉命令、独占报告、停止条件和 Git 规则。所有 Agent 都不是仓库唯一工作者，不得回退他人修改，不得切分支、stash、暂存、commit 或 push。出现共享文件需求时先交回主智能体，不得抢写。

Prompt 审查通过后立即启动 Gate A，不得只交付计划或 Agent Prompt。

## 11. 自动化门禁

至少覆盖：

- Lifecycle：合法 phase 序列、重复 EndTurn 无副作用、结算中输入锁定、异常/失败不会重复结算、初始 start-turn 复用同一入口、02B4 hook 为 no-op。
- Timeline：x 后 y 排序、跨格 identity 去重、玩家/敌人交错、Clear 已移除 action 不执行、action Resolve 重判、结算后 grid 清空。
- Enemy intent：真实 occupant 候选、priority、显式 seed、shape/边界、数量上限、source/target snapshot、Scene 显示、topology 重判、source 死亡、target 消失/替换、Resolve 最终重判、unsupported source command 的显式 no-op。
- Tower：时间轴创建 HP100、同一 building phase HP50、下一次 HP0、occupant/tile/status/Presenter 清除、已提前死亡不再执行、重复 phase 不二次 damage。
- Poison：空快照、单源六邻、多源叠加、边界邻居、空格/死亡/不支持状态过滤、旧层数伤害公式、传播后层数不参与本回合伤害、新感染不传播/不受伤/不衰减、旧源当前层数减 1、checked stack、死亡与移除。
- Sync：typed before/after/removal、trace phase/action/intent/runtime ID、Presenter 幂等、disable/enable/rebind 不复制 View/监听、Domain 不引用 UnityEngine。
- Regression：七卡 schema/hand、lighting、earthquake、recover、built、poison 卡牌施加、wind/tornado clear、360° 选格、Timeline Preview、Scene authoring 和所有前置测试。

随机入口继续使用显式 seed。所有失败路径同时断言 phase、board、occupant、HP/status、Timeline 和 Presentation 无额外副作用，不能只断言返回值。

## 12. 视觉与 Player 门禁

实际运行并人工检查至少：

- 两个连续 lifecycle 的 phase/HUD/Timeline 变化，结算中输入确实锁定，下一轮 intent 完成后再解锁。
- Enemy intent 在地图 source/target/range、Timeline action 和 tooltip/HUD 中使用同一 identity；失效后所有占格和 overlay 一起消失。
- Tower 创建前、创建后 HP100、同结束回合 HP50、下一回合 HP0 后完整消失；四 yaw 下 anchor/选择/collider 不漂移。
- Poison 单源与多源传播，旧源伤害和衰减、新感染本回合不 tick；图标/层数、HP 和死亡退场不遮挡地块选择。
- Poison 杀死 occupant、Tower 自损死亡和 enemy source 失效后，tile occupancy、intent、status 和 Presenter 同步。
- 1280×720、1920×1080、2560×1080 三视口的 HUD、Timeline、七卡 hand、状态/意图 tooltip 无裁切、重叠或不可读。
- yaw 0/90/180/270 下 enemy source/target、Tower、Poison 和受影响邻格仍绑定同一逻辑坐标。

保存真实 PNG、结构化 harness summary 和人工检查总结。必须打开截图逐张检查；场景加载、退出码 0、非空像素或图像哈希变化只能证明输出存在，不能替代视觉验收。

最终必须运行完整 EditMode、完整 PlayMode、Windows build 和实际 Player smoke。XML 需解析 `total>0`、`failed=0`；build 必须 `Succeeded`；Player 必须退出码 0 并含阶段专用 smoke marker。

## 13. 文档与下一阶段

更新：

- 架构文档和一份记录 lifecycle phase、MIG-002 策略、Tower/Poison ordering、death/removal 的 ADR。
- integration contracts、ownership map、master backlog、parity matrix、test plan 和继承证据账本。
- current status、completed slices、known issues 和 push status。
- `06-maintenance/add-card.md`、`add-effect.md`、`add-enemy.md`、`debugging-guide.md`、`scene-and-prefab-guide.md`、`testing-and-evidence.md`，按真实实现说明回合 handler、enemy intent、building/status、Prefab/Presenter 和证据命令。

本阶段完成后生成 `NEXT_STAGE_DECK_AND_BATTLE_FLOW_PROMPT.md` 或等价明确命名的 02B4 Prompt，严格以后继 Gate 0 等待本文 Gate D。02B4 至少包含：

- 正式回合推进、Era/回合资源与时间币。
- 手牌弃置、抽牌、洗牌、牌库、弃牌堆和 starter deck 语义。
- 与本文已命名 hook 的顺序集成，不重写 lifecycle/Tower/Poison/intent。
- 胜负条件、战斗结算、局内奖励入口和返回边界。
- 固定 seed、多回合、空牌库/洗牌、胜负互斥、Build/Player/视觉/Git 门禁。

02B4 之后再按顺序进入局内敌人/建筑内容扩展、体验、性能与稳定性。只有用户重新排定优先级后才进入局外 Unity 迁移；否则局外继续保持 Godot。

## 14. Git 与工作区保护

开始前与每个 Gate 前重新生成 dirty-file inventory；保护用户原有、前置 Agent 和无法确认所有权的全部改动。至少不得触碰、回退或暂存仍然存在的下列工作区项：

- `default_bus_layout.tres`
- `docs/migration/unity-3d/02-architecture/migration-roadmap.md`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/timeline-invalid.png`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-0.png`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-90.png`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-180.png`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-270.png`
- `scene/in_scene/rewards/resources/default_craft_recipe_book.tres`
- `shaders/color_BG.gdshader`
- `shaders/game_over.gdshader`
- `unity/ProjectSettings/ProjectSettings.asset`
- `unity/ProjectSettings/URPProjectSettings.asset`
- 任何仍未纳入前置 Gate D 检查点的 `NEXT_STAGE_DECOUPLING_PROMPT.md`、`NEXT_STAGE_REMAINING_CARDS_PROMPT.md` 或其他不明文件

如果 Gate D 最终报告明确说明其中某项已经由其所有者合法提交，以最新 Git 状态为准；不要为匹配本文清单制造新的脏改动。Unity 测试重新写出的历史截图默认仍不属于本阶段，除非本阶段 Prompt 明确要求刷新并由主智能体审查。

不得使用 `git add -A`、`git reset --hard`、`git checkout --`、自动 stash、历史改写或清理不明确目录。Unity `.meta` 与对应资产一起审查。每个 Gate 形成单一目的提交；只用精确路径暂存，提交前检查 `git diff --cached --name-only`、`git diff --cached`、`git diff --check` 和 forbidden-file audit。

只 push 当前 `unity_7.31`。push 失败时保留本地提交并记录准确错误、ahead/behind 和远端状态；不得擅自修改用户级 Git/GCM/TLS 配置。

## 15. 只有这些情况可以停止

- Remaining Cards Gate D 或其子智能体仍在写工作区。
- 当前分支不是 `unity_7.31`，且安全切换会覆盖无法保护的改动。
- 前置 Gate D 没有可用的 ordinary/clear session、occupant state/result、Scene/Prefab 或 Presenter 扩展面，必须先修复前置验收。
- Godot 源、当前运行证据和冻结文档在 phase、Tower 立即 decay、Poison 三 pass 或 death/removal 核心语义上冲突，且只读检查无法消解。
- 必须实现 02B4、局外流程、旧存档兼容、素材授权扩张或不可恢复数据修改才能继续。
- Unity 许可证、工程锁、磁盘或编辑器故障在安全重试后仍阻止全部验证。

MIG-002 本身不是停止理由。源 enemy command 为空时按本文产出显式 unsupported/no-effect 结果并完成生成、展示、重判和生命周期；只有不同权威证据要求互斥的真实敌方效果且无法消解时才报告硬阻塞。

## 16. 最终回报

最终使用中文汇报：

1. Gate D 继承证据与本阶段刷新的测试、视觉、build、Player smoke。
2. 固定 phase、Timeline/敌人/建筑/状态的实际执行顺序和幂等保证。
3. MIG-002 的生成、展示、重判、实际 effect 或显式 unsupported 处理结果。
4. Tower 创建当回合 100→50、后续 50→0 与 occupant/tile/Presenter 移除证据。
5. Poison 全图快照、六邻传播、快照伤害、层数衰减、新感染隔离和死亡同步证据。
6. Scene/Prefab 中新增或调整的可编辑内容，以及仍动态创建的 View。
7. 多智能体所有权、报告、集成结果和 Controller/程序集边界审查。
8. 维护文档、ADR、Git 检查点、push 状态，以及明确留给 02B4 的 deck/discard/time coin/turn advance/win/loss/reward。

没有用户决策阻塞时明确写“当前无用户决策阻塞”，并继续完成当前 Gate，不要停在计划阶段。

# 主 Prompt 结束
