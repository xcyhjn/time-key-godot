# Wave 02B4 Godot 源语义审计

> 状态：Gate 0 只读审计完成，无硬阻塞
> 日期：2026-08-02
> 写入范围：仅本报告；未修改 Godot/Unity 源码、共享文档、Scene/Prefab、证据或 Git 状态

## 结论

当前 Godot 源足以冻结 Wave 02B4 的牌库、时间币、Era/phase、正式回合、胜负、奖励入口与返回边界。存在两处必须显式裁决、但不构成硬阻塞的迁移差异：

1. Godot 在 Timeline 结算前移动剩余手牌并发放时间币，而 ADR 0008 的唯一 02B4 reserved hook 位于 Timeline、建筑、清空和回合开始状态之后。
2. Godot 只有胜利的自动判定；失败入口当前来自外部信号/调试按钮，且胜败不共享互斥终局事务。

最小裁决是不修改 ADR 0008 或既有 lifecycle runner：结束回合请求先冻结 Timeline 占格、剩余手牌实例和 action display snapshot；hook 再以冻结输入执行弃手、时间币、phase、必要洗回与抽牌。胜败则迁移为一个 typed、互斥、幂等的 Domain outcome；失败由显式命令进入，不伪造 Godot 中不存在的生命值规则。

## 冻结常量

| 项目 | 冻结值 | 权威依据 |
| --- | --- | --- |
| Starter deck | `lighting, lighting, earthquake, earthquake, recover, wind, wind, recover, tower, tower, poison, poison` | `scene/global/global_db.gd:7-8` |
| 重复计数 | `lighting` 2、`earthquake` 2、`recover` 2、`wind` 2、`tower` 2、`poison` 2，共 12 张 | `scene/global/global_db.gd:7` |
| Timeline | 12 列 x 3 层，共 36 格，内部坐标 `x=0..11, y=0..2` | `scene/in_scene/timeline/TimelineManager.gd:8-13` |
| 牌堆顶 | `_held_cards` 数组尾端，即 `back()`；Pile 文档也定义高索引为 top | `scene/in_scene/in_scene_modules/cards/CardDrawFlowController.gd:34-35`、`addons/card-framework/pile.gd:59-72,121-124` |
| 正式每轮抽牌请求 | 5 | `scene/in_scene/in_scene.gd:596-599` |
| 手牌上限配置 | 7 | `scene/in_scene/in_scene.gd:694-708` |
| Era/phase | Era 从 1 开始；phase 从 1 开始，正常范围 1..8；8 后推进为下一 Era 的 phase 1 | `scene/global/global_clock.gd:79-104` |
| 时间币比例 | 默认 `1.0`，增量为 `floor(emptySlots * ratio)` | `scene/global/global_timecoin.gd:10-12,42-57` |
| 自动胜利阈值 | 已登记敌方累计最大生命大于 0，当前/累计最大生命 `<= 0.1` | `scene/in_scene/enermy/total_enemy_health_bar.gd:19-20,489-499` |

Starter deck 先按上述数组顺序逐张创建，再对 `_held_cards` 调用无 seed 的全局 `shuffle()`；因此列表顺序是确定的构造输入，不是 Godot 的实际开局抽牌顺序。Unity 应从该有序输入建立 card instances，再使用显式 seed 洗牌，不继承全局随机状态（`scene/in_scene/in_scene_modules/cards/CardSystemBootstrap.gd:132-140`）。

## 牌库、手牌、弃牌与洗牌

### 牌实例和容器

- 每个 `GlobalDB.player_deck` 内容 ID 都由 factory 实例化为一个独立 `Card` Node，并追加进目标容器；`card_name` 是内容 ID，`card_info` 是内容数据（`addons/card-framework/json_card_factory.gd:185-207`）。
- 容器用 `Array[Card] _held_cards` 保存 Node 引用，移动卡牌时从旧容器移除同一个对象再加入新容器，不创建新卡（`addons/card-framework/card_container.gd:54-56,276-299,310-318`）。
- `move_cards()` 的批量实现按输入数组末端到开头移动；在不指定 index 时逐张追加。因此强制弃置一批手牌会反转其容器数组顺序，但弃牌洗回后立即 shuffle，此顺序不会成为正式抽牌顺序（`addons/card-framework/card_container.gd:310-318`）。
- 成功提交玩家 action 后，该实体卡立即进入弃牌堆；结束回合的“弃手”只处理仍留在 hand 的卡（`scene/in_scene/DragShapeController.gd:903-937,967-987`、`scene/in_scene/drag_modules/cards/DragSuccessDiscardMover.gd:8-18`）。

### 抽牌和空堆

Godot 的准确抽牌循环如下（`scene/in_scene/in_scene_modules/cards/CardDrawFlowController.gd:9-44`）：

1. 任一 hand/deck/discard 引用无效，返回 `drawn_count=0`。
2. 请求数 `<=0`，或进入函数时 hand 已达到/超过 7，返回 `drawn_count=0`。
3. 每次迭代若 deck 为空且 discard 非空，先把全部 discard 移回 deck 并 shuffle。
4. 若 deck 与 discard 都为空，立即停止；不会死循环或凭空造牌。
5. 从 deck 数组尾端取一张并把同一 Card Node 移入 hand。
6. 可用牌少于请求数时只抽实际可用数量。

手牌上限的 Godot 实现有一个可观察缺口：它只在循环前检查一次，没有在每次抽牌前重新检查。因此“手里 6 张时调试请求抽 3”可以得到 9 张；正式流程先强制弃掉剩余手牌，再抽 5，不会触发这个缺口（`scene/in_scene/in_scene_modules/cards/CardDrawFlowController.gd:16-24`、`scene/in_scene/in_scene.gd:558-559,596-597`）。Unity Domain 应把 7 作为真实不变量，返回 typed `HandLimitReached/PartialDraw`，而不是复制调试路径的越界行为。

洗牌的准确边界如下（`scene/in_scene/in_scene_modules/cards/CardDrawFlowController.gd:47-68`）：

- 无效 deck/discard：`shuffled=false, card_count=0`。
- discard 为空：相同失败结果；deck 不受影响。
- discard 非空：移动全部卡到 deck，对 deck 数组调用全局 `shuffle()`，返回实际移动张数。
- deck 与 discard 双空时 draw 返回 0，shuffle 返回 false；Unity 必须给出 typed 原因且保持三堆不变。

## 正式回合与 reserved hook 裁决

### Godot 可观察顺序

正式 End Turn 的源顺序是：

1. 仅 `COMBAT` 状态接受请求并立即锁输入（`scene/in_scene/in_scene.gd:551-556`）。
2. 强制弃掉全部剩余手牌。调用方没有 `await`，但实际容器移动发生在 controller 第一个 `await` 之前；tooltip/UI 尾部刷新可能与 Timeline 启动交叠（`scene/in_scene/in_scene.gd:558-562,634-641`、`scene/in_scene/in_scene_modules/cards/HandDiscardFlowController.gd:9-23`）。
3. Timeline 在任何 action 执行前，以当前 36 格占用计算并发放时间币；随后按 x 从小到大、同列 y 从小到大执行，同一多格 action 只执行一次，并清空网格（`scene/in_scene/timeline/TimelineManager.gd:157-209`）。
4. Timeline 完成后发出 `step_next(currentEra, 0)` 执行建筑，再清空 Timeline UI（`scene/in_scene/in_scene.gd:561-573`）。
5. 新回合先发 `new_turn_starting`，再处理回合开始状态；随后推进 phase、刷新顶部进度、抽 5、刷新 intent、发 `turn_started` 并解锁（`scene/in_scene/in_scene.gd:579-605`）。

首次进入战斗复用 `_start_turn(false)`：锁输入并等待卡牌系统/Timeline intro，处理状态但不推进 phase、不发放时间币，抽 5 后生成 intent（`scene/in_scene/in_scene.gd:405-421,579-605`）。

### ADR 0008 固定顺序

ADR 0008 冻结 `Timeline -> building -> clear -> statuses -> 02B4 hook -> intents`，且只允许 02B4 替换 reserved hook（`docs/migration/unity-3d/02-architecture/adr/0008-turn-lifecycle-intents-and-action-presentation.md:12-28,50-52`）。现有 runner 也在 statuses 后调用 `INextTurnHook`，再刷新 intent（`unity/Assets/_Project/Runtime/Domain/Lifecycle/TurnLifecycleRunner.cs:98-185`）。

### 最小裁决

不得为了逐行复制 Godot 而在 runner 前后新增第二套 lifecycle。采用以下单一事务：

1. `EndTurnRequested/InputLocked` 时冻结 request：`lifecycle sequence`、Timeline 完整 plan/occupied cells、剩余 hand 的 card-instance identities、当前 Era/phase/timecoins/outcome，以及所有已提交 action 的 presentation-safe snapshots。
2. runner 继续依 ADR 0008 执行 Timeline、建筑、清空和状态；这些处理器不得读取/修改 deck、hand、discard 或 timecoins。当前 Godot Timeline effect 路径也没有牌区/时间币依赖。
3. EndTurn hook 严格执行：弃置冻结 hand 中仍位于 hand 的实例 -> 按冻结占格计算 `floor((36 - occupiedCells) * ratio)` -> phase 推进/8 后 Era rollover -> deck 为空时把 discard 洗回 -> 抽至正式请求 5（受 hand limit 7 约束）。
4. InitialStart hook 只初始化/确定性洗牌并抽 5；不弃牌、不推进 phase、不发 Timeline 时间币。
5. hook 成功后才允许既有 intent refresh；重复 lifecycle sequence 返回第一次的同一 typed result，不再移动卡牌、加币或推进 phase。

这会把 Godot 的两次内部数据写入延后到 reserved hook，但玩家从 EndTurn 到 PlayerReady 全程输入锁定；通过冻结 pre-clear occupancy、hand identities 和 action display snapshot，最终可观察牌区、时间币、Era/phase、抽牌与 intent 顺序保持一致，同时不改 lifecycle。

## 时间币与 Era/phase

- 空位数按 36 减去 Timeline `grid.size()`；多格 action 的每个占格都占一个 slot，而 action 自身只执行一次（`scene/in_scene/timeline/TimelineManager.gd:159-168,181-194`）。Unity 必须从冻结 plan 的 occupied-cell 集合计算，不能在 clearing 后读取空 Timeline，否则会错误发 36。
- `add_from_timeline()` 对空位 `<=0` 或向下取整结果 `<=0` 保持余额不变；正增量会同步 MapState 后发信号（`scene/global/global_timecoin.gd:42-80`）。
- 消费数量 `<=0`、余额不足均返回 false 且不改余额；余额不足额外发信号。成功才扣款、同步并返回 true（`scene/global/global_timecoin.gd:83-110`）。Unity 应返回 typed result，失败无副作用。
- GlobalClock reset 为 Era 1/phase 1；每次正式新回合 phase 加 1，超过 8 时 Era 加 1 且 phase 置 1（`scene/global/global_clock.gd:79-104`）。首次开局不推进（`scene/in_scene/in_scene.gd:591-594`）。
- 时间币、Era 和 phase 会同步到 MapState，属于跨场景进度数据，不是 HUD 所有（`scene/global/global_timecoin.gd:180-196`、`scene/global/global_clock.gd:124-139`）。

## Identity 与 action snapshot

Godot 通过 Card Node/数组对象身份维持同一实体卡；Card 本身只有内容 `card_name` 和 `card_info`，没有独立、可序列化的 card-instance identity（`addons/card-framework/card.gd:19-54`）。TimelineAction 又直接保存 card Node 为 `source_node`，并直接引用 `card.card_info`；Timeline/UI 以 `TimelineAction.get_instance_id()` 去重或映射（`scene/in_scene/drag_modules/rules/DragPlayerActionFactory.gd:8-16`、`scene/in_scene/timeline/TimelineAction.gd:6-24`、`scene/in_scene/timeline/TimelineManager.gd:389-396`）。

Wave 02B4 必须加强为三个不混用的身份：

- card stable ID：内容类型，例如 `lighting`；重复卡可相同。
- card-instance identity：一场战斗内每张实体卡唯一，跨 deck/hand/discard 移动保持不变。
- action identity：每次提交 action 唯一；同一 card instance 的不同提交也不能复用。

提交 action 时必须复制卡名、效果、source/target、shape、occupied cells 和 identity 到不可变 display payload。牌进入 discard、洗回 deck、重新抽到 hand、CardView 销毁或重绑均不得改变已经提交的 action frame。该裁决也符合 ADR 0008 的统一只读 snapshot 规则（`docs/migration/unity-3d/02-architecture/adr/0008-turn-lifecycle-intents-and-action-presentation.md:30-34`）。

## 胜利、失败与互斥终局

### Godot 当前边界

- 自动战斗胜利由累计敌方总血条触发：最大值必须大于 0，当前/累计最大值 `<= 10%`，并以本地 bool 保证信号只发一次（`scene/in_scene/enermy/total_enemy_health_bar.gd:489-499`）。
- Main 只在 `COMBAT` 状态接收胜利；它先把状态改成 `SETTLEMENT`，再进入结算编排（`scene/in_scene/in_scene.gd:1216-1235`）。因此重复胜利信号会被拒绝。
- 失败没有自动 Domain 判定。仓库中 `defeat_triggered` 的局内生产者只有 lose/debug 按钮；失败 handler 也没有把 `current_battle_state` 改成终局状态（`scene/in_scene/in_scene.gd:280-287,1144-1158`）。失败流程停止音乐、隐藏 UI、显示统计并删除 slot 0 存档（`scene/in_scene/in_scene_modules/settlement/CombatDefeatFlowController.gd:9-44`）。
- 最终游戏胜利页是另一个按钮入口，只负责声音和 Win Screen，不是“本场战斗胜利进入结算”的同一边界（`scene/in_scene/in_scene.gd:1164-1170`、`scene/in_scene/in_scene_modules/settlement/GameWinFlowController.gd:5-28`）。

### Unity 加强项

使用一个 authoritative outcome transaction，至少包含 `InProgress/Victory/Defeat`。`TryResolve(sequence, requestedOutcome)` 只有第一次能成功；重复同结果幂等返回原结果，相反结果返回 typed conflict，且任何终局后都拒绝 EndTurn/action/reward 重发。Victory 和 Defeat 必须在同一事务内互斥；不要根据 UI、动画完成或 Presenter 状态推导 outcome。

由于 Godot 没有正式失败谓词，Unity 02B4 只提供显式 typed defeat command/boundary 与测试/Player harness 入口，不自行发明玩家 HP、回合数或时间币归零规则。Victory 可以由现有战斗状态输入触发，但判断写入 Domain，不由 Presenter 决定。

## 战斗结算、奖励与返回边界

战斗胜利后的 Godot 顺序是：停止 BGM、隐藏调试/战斗 UI、锁输入、保存 `GlobalDB.player_deck` 快照并把所有 runtime Card Nodes 回收到 deck、清 Timeline、锁地图并进入奖励模式、隐藏敌方血条、播放“战斗胜利”横幅，最后显示结算按钮（`scene/in_scene/in_scene_modules/settlement/CombatVictorySettlementController.gd:9-25`、`scene/in_scene/in_scene.gd:1185-1213`）。回收后的 deck 仍使用无 seed `shuffle()`（`scene/in_scene/in_scene_modules/settlement/SettlementDeckReclaimService.gd:13-37`）。

奖励入口来自结算地图上的敌方建筑，按 dead/alive bind state 过滤；payload 当前含 `stack/coord/landform/reward_type/reward_label`，可用类型是 `shop/acquire/remove/craft`（`scene/in_scene/hex_map_modules/rewards/SettlementRewardController.gd:40-53,92-133`、`scene/in_scene/in_scene.gd:50-53`）。奖励页实际以 child scene 打开并直接读写 GlobalDB/GlobalTimecoin；提交或 shop 退出后才标记对应建筑已消费（`scene/in_scene/in_scene_modules/settlement/SettlementRewardSceneController.gd:9-50,75-84`、`scene/in_scene/in_scene_modules/settlement/SettlementRewardExitController.gd:9-28`、`scene/in_scene/in_scene_modules/settlement/SettlementRewardConsumer.gd:9-20`）。

这些 shop/acquire/remove/craft 具体页面及局外地图消费仍是 Godot 流程，不属于 Unity 02B4 的全量迁移范围。Unity 本阶段只需交付：

- Victory 后恰好一次的 typed reward-entry snapshot，不含 Godot Node/stack 引用。
- 奖励选择/确认的最小局内入口，任何重复领取返回 typed no-op/conflict。
- typed return boundary，携带 outcome、Era/phase、timecoins、持久 deck 内容快照和来源上下文；不切换真实 Godot 局外地图。

Godot 的返回 payload 当前固定为：`transition_type=return_from_combat`、`combat_result=completed`、`battle_state=settlement`、battle tag、map seed、Era、timecoins、deck snapshot/size、room context 和 `clear_active_room_context=true`（`scene/in_scene/in_scene_modules/scene_flow/InSceneReturnPayloadBuilder.gd:9-28`）。返回时先同步 Era、构建/记录并保存 pending payload，再锁输入、隐藏 UI、播放 Dim，最后切 OutScene（`scene/in_scene/in_scene_modules/scene_flow/InSceneReturnFlowController.gd:9-23`）。局外仅把 `combat_result=completed` 视为 boss 章节推进候选（`scene/out_scene/out_scene_modules/RoomResolutionController.gd:34-61`）。Unity 的 typed boundary 应把模糊的 `completed` 明确为 Victory outcome；Defeat 不伪装成 completed return。

## Unity 可测试断言

Gate A/B 至少覆盖以下断言：

1. 12 张 starter deck 的有序输入和六种重复计数准确；相同 seed 产生相同 card-instance 顺序，不同实例即使 stable ID 相同也不相等。
2. 数组尾端为 top；draw 在 deck 空时只洗回 discard；双空返回 typed empty 且状态完全不变。
3. hand limit 永不超过 7；正式 InitialStart 抽 5 且 Era/phase 保持 1/1、时间币不变。
4. EndTurn 冻结占格后恰好一次执行：discard remaining hand -> `floor((36-occupied)*ratio)` timecoins -> phase/rollover -> shuffle if needed -> draw 5 -> intent refresh。
5. 36 格全占时增量 0；0 格占用时按默认 ratio 增 36；多格 action 按 occupied cells 计数而不是 action 数。
6. phase 8 的下一正式回合变为下一 Era/phase 1；重复 sequence 不再加币、推进或移动卡。
7. 任一步 typed 失败不产生部分重复副作用；终局 outcome 拒绝 hook 和 action submission。
8. action display snapshot 在源 card 从 hand/discard/deck 移动、洗回、重抽或 View 销毁后仍保持原 card name/effect/source/target/action identity。
9. Victory/Defeat 首次成功后互斥；重复同 outcome 幂等，相反 outcome typed conflict；reward entry 只随 Victory 发一次。
10. return boundary 的 deck/timecoins/Era/phase 来自 Domain snapshot；Presenter、动画和 Scene 对象不可决定或修改这些字段。

## 已消解差异与硬阻塞判断

- Godot 初始/洗回/结算回收都使用全局无 seed shuffle：按 Wave 02B4 显式 seed 契约加强。
- Godot hand limit 只做入口检查：Unity 把 7 固化为 Domain 不变量，正式流程结果不变。
- Godot 空状态只返回 bool/计数或静默 return：Unity 改为 typed result，失败无副作用。
- Godot 的 Card/TimelineAction Node 身份不能跨层稳定表达：Unity 分离 card stable ID、card-instance identity 和 action identity。
- Godot 的弃手/时间币写入早于 Timeline，而 reserved hook 晚于 statuses：使用 pre-resolution 冻结输入和单一 hook transaction 消解，不增改 lifecycle phase。
- Godot 缺少正式失败谓词和统一胜败互斥：迁移为显式 typed outcome command，不发明新失败规则。
- Godot reward scene 和 OutScene 是局外流程：Unity 02B4 仅实现 reward-entry/return boundary，不移植完整局外系统。

以上差异均可由当前主 Prompt、ADR 0008 与迁移加强项裁决，无需用户决策，没有权威语义不可调和冲突。
