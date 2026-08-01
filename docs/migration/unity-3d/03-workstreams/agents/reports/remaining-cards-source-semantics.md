# Remaining Cards — Godot Source Semantics Audit

> Agent：`remaining-cards-source-semantics`
> 日期：2026-08-01
> 分支：`unity_7.31`
> 范围：只读核对 Godot 的 `recover` / `built(tower)` / `poison` / `wind` / `tornado`、目标合法性与结算重判、clear mask/action identity、occupant/状态行为、源资产及现有测试夹具。未运行 Unity，未修改代码或资产。

## 1. 结论摘要

当前 Godot 权威实现足以冻结五张卡的主干行为：

| 卡 | 已确认源语义 | 关键证据 |
| --- | --- | --- |
| `recover` | `value=100`，范围 `(0,0),(1,0),(-1,1)`，普通时间轴 shape `111`；选择阶段要求范围内至少有可 `heal()` 且未满血 occupant；结算时重新读取地块当前 occupant；`set_health()` 钳制到 `[0, Max_Blood]` | `card_data/recover.json:2-11`；`scene/in_scene/hex_map_modules/rules/HexTargetRules.gd:133-152,174-182`；`scene/in_scene/timeline/commands/RecoverCommand.gd:19-37`；`scene/in_scene/tile.gd:579-593`；`scene/in_scene/tile_modules/rules/TileHealthStateRules.gd:13-18` |
| `tower` / `built` | `creation=tower,value=1`；中心单格、shape `010,111`；选择和 Resolve 都要求真实空地；`value` 是一次命令最多创建数；创建 Tower 为中立、`HP=Max_Blood=100`，注册到 `map_data` 并成为 stack occupant | `card_data/tower.json:2-11`；`scene/in_scene/hex_map_modules/rules/HexTargetRules.gd:51-78`；`scene/in_scene/timeline/commands/BuiltCommand.gd:15-49`；`scene/in_scene/enermy/tower.gd:11-31`；`scene/in_scene/hex_map_modules/registrars/RuntimeLandformRegistrar.gd:7-31,72-109,174-181` |
| `poison` | `value=2`，中心单格，shape `110,111`；只对当前仍存在、支持 `add_status()` 且（若有 HP）HP>0 的 occupant 施加；重复施加直接整数相加、无上限；状态可快照读取 | `card_data/poison.json:2-11`；`scene/in_scene/hex_map_modules/rules/HexTargetRules.gd:185-190`；`scene/in_scene/timeline/commands/PoisonCommand.gd:22-39`；`scene/in_scene/status/status_component.gd:43-60,121-122` |
| `wind` | clear mask 为 `11,11`（2x2），顶层普通 shape 为 `0`；不选地图目标，不创建 TimelineAction，合法空清也消耗卡牌 | `card_data/wind.json:2-11`；`scene/card/custom_card.gd:310-314,542-551`；`scene/in_scene/DragShapeController.gd:818-834,967-988` |
| `tornado` | clear mask 为 `111111111111`（12x1），顶层普通 shape 为 `0`；与 wind 共用即时 clear 路径 | `card_data/tornado.json:2-11`；`scene/in_scene/timeline/TimelineClearEffect.gd:45-62,65-101` |

必须由本阶段主智能体知悉的差异有四项：

1. 当前 Godot clear parser 在 `effects[].value` 缺失时会回退顶层 `shape`，空/非法 mask 又会归一化为单格；本阶段 Prompt 明确要求 typed `CardEffect.ClearMask` 为唯一来源并显式拒绝非法 mask。Unity 应遵循收紧后的 Prompt，不复制此容错。
2. 当前 Godot 普通 action 不保存稳定 HexCoord，只保存 `target_tile` 节点；Resolve 时用该节点从 `stack_nodes` 反查中心坐标。occupant 替换能正确重判，但地块节点本身销毁后即使同坐标重建也会 no-op。本阶段要求应在 Unity 中保存稳定 HexCoord 并按坐标重查。
3. Godot 的 Recover 没有 `HP>0` 门禁：仍作为 occupant 存在的 0 HP/Broken 普通 landform 可被治疗并通过 `State_Update()` 复活。阶段 Prompt 只要求“仍存在、支持生命值且 HP<MaxHP”，没有明确排除 0 HP；建议把该行为锁成测试，不要擅自加入“必须存活”过滤。
4. Godot 源回合生命周期已包含 Tower 自损和 Poison tick；本阶段 Prompt 明确将它们排除到 02B3。Unity 本阶段只实现创建/施加后的可观察状态，不提前接线生命周期。

## 2. Schema 与真实 fixture

### 2.1 五张源 JSON

| Stable ID | 数字 ID / 时代 | FrontImage | Effect | Hex range | 普通 shape / clear mask | 源位置 |
| --- | --- | --- | --- | --- | --- | --- |
| `wind` | 4 / 1 | `wind.png` | `clear`, string `"11,11"` | `(0,0)`（运行时不使用地图目标） | shape `0` / mask 2x2 | `card_data/wind.json:2-11` |
| `recover` | 5 / 1 | `recover.png` | `recover`, number `100` | `(0,0),(1,0),(-1,1)` | `111` | `card_data/recover.json:2-11` |
| `tower` | 6 / 1 | `tower_card.png` | `built`, `creation="tower"`, number `1` | `(0,0)` | `010,111` | `card_data/tower.json:2-11` |
| `poison` | 7 / 1 | `poison_card.png` | `poison`, number `2` | `(0,0)` | `110,111` | `card_data/poison.json:2-11` |
| `tornado` | 8 / 2 | `tornado.png` | `clear`, string `"111111111111"` | `(0,0)`（运行时不使用地图目标） | shape `0` / mask 12x1 | `card_data/tornado.json:2-11` |

普通 shape parser 只把字符 `1` 变成占格坐标并裁切到最小包围盒；Recover 得到 `(0,0),(1,0),(2,0)`，Tower 得到 `(1,0),(0,1),(1,1),(2,1)`，Poison 得到 `(0,0),(1,0),(0,1),(1,1),(2,1)`。证据：`scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd:8-28,31-54,92-103`。

Godot 的 JSON 层是 permissive Dictionary：`JsonCardFactory` 只验证 `front_image`，其余 effect 字段到结算时才被 `int()` / `str()` 转换，未知 effect 被忽略；它不是 Unity 应复制的 typed validation。证据：`addons/card-framework/json_card_factory.gd:75-100,145-165`；`scene/in_scene/effect/effect_processor.gd:101-121`。

Unity 当前五份内容副本 `unity/Assets/_Project/Content/Cards/{recover,tower,poison,wind,tornado}.json` 与对应 `card_data/*.json` SHA-256 完全一致。既有 schema fixture 已锁定五卡 typed effect、range、普通 shape 和 clear mask：`unity/Assets/_Project/Tests/Infrastructure/CardJsonAdapterTests.cs:44-131,134-149`；错误类型、Built 缺 creation、非法 clear mask 也已有拒绝测试：同文件 `187-259`。这些测试目前只证明 schema，不证明玩法结算。

### 2.2 源 JSON SHA-256

| 文件 | SHA-256 |
| --- | --- |
| `card_data/recover.json` | `55AAF6397840C8C6F09D85959EAB3E2C6C91D81D7B754458387CE1F21A43C279` |
| `card_data/tower.json` | `1BF3079D050C61EF385D28CF84A900F9A154D0201451821E1DE32827483772E3` |
| `card_data/poison.json` | `011138F0CDD2C4D54CCF204F4C49E633FB77B009F24D7D1BAA05BCF088427202` |
| `card_data/wind.json` | `A8124F671144EDD6CFD756BDA4F3C8DEFA5BA6083A5E285459441F93085A90D8` |
| `card_data/tornado.json` | `78775DABE8A883DB053812948D49E1F5150EE9F2E148160A0BFC1E7C30981023` |

## 3. 普通 action 的选择、保存与 Resolve 重判

### 3.1 选择阶段

- `HexTargetRules.is_stack_valid_target()` 按 effect 类型选择规则；Recover/Poison 会扫描以玩家所点中心为基准的整个 `effect_range`，因此中心格可为空，只要范围内至少有一个合法 occupant 即可。Built 只接受 `occupant` meta 与 `map_data.landform/landform_in` 都为空的真实 stack。证据：`scene/in_scene/hex_map_modules/rules/HexTargetRules.gd:8-48,51-78,81-96,133-152`。
- Recover 合法 occupant：实例有效、有 `heal()`；若同时暴露 `HP` 与 `Max_Blood`，则必须 `HP<Max_Blood`。若自定义对象只有 `heal()` 而没有血量字段，当前 Godot 仍判合法。证据：同文件 `174-182`。
- Poison 合法 occupant：实例有效、有 `add_status()`；若暴露 HP，则必须 `HP>0`。无 HP 的自定义状态对象仍可合法。证据：同文件 `185-190`。
- clear 虽然在通用规则中返回 true（同文件 `26-30`），实际 `CustomCard` 选中后 deferred 进入 no-target 时间轴路径，不制造地图点击：`scene/card/custom_card.gd:310-314,542-551`。

### 3.2 action 保存与结算

- `TimelineAction` 保存的是 `source_node` 和 `target_tile` 节点引用，没有独立 target HexCoord：`scene/in_scene/timeline/TimelineAction.gd:6-24`。
- 回合结束按时间轴从左到右、同列从上到下结算；多格 action 用对象引用去重，只执行一次：`scene/in_scene/timeline/TimelineManager.gd:157-194`。
- Resolve 时 `EffectProcessor` 才从 `action.target_tile` 反查中心坐标、重新解析 `effect_range`，并从当前 `stack_nodes` 收集仍存在的 stack：`scene/in_scene/effect/effect_processor.gd:35-51,62-99`。
- 各命令执行时再次从 stack `occupant` meta 取当前实体，而不是使用选择时实体引用：`scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd:5-13`。因此同一 stack 上 occupant 在选择后被替换，Recover/Poison 会作用于新 occupant；变为空或失效则 no-op。
- 局限：如果原 `target_tile` 节点被释放或从 `stack_nodes` 移除，`EffectProcessor` 在 `64-68` 直接返回空范围；它不会凭旧坐标找到同坐标的新 stack。这正是 Unity 应采用稳定 HexCoord 的理由。

## 4. Recover

### 已确认

- 命令构造把负数 amount 压到 0；0 或无有效 map/range 直接 no-op：`scene/in_scene/timeline/commands/RecoverCommand.gd:11-21`。
- 每个当前 stack 重新取 occupant 并复用最终 `can_recover_entity()` 校验；空格、对象消失、已满血均 no-op：同文件 `23-34`；`scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd:68-78`。
- 真正治疗调用 `entity.heal(amount)`，不是负伤害；`heal()` 调用 `set_health(HP+amount)`，纯规则使用 `clampf(new_hp,0,Max_Blood)`：`scene/in_scene/tile.gd:579-593`；`scene/in_scene/tile_modules/rules/TileHealthStateRules.gd:13-18`。
- Recover VFX 只在合法 occupant 上触发，之后治疗；至少一人被治疗才等待 0.3 秒：`scene/in_scene/timeline/commands/RecoverCommand.gd:23-37`。

### 状态/occupant 边界

- 普通 landform 的 `die()` 将状态设为 Broken、清空状态并切破损贴图，但保留 occupant，不 `queue_free()`：`scene/in_scene/tile.gd:610-629`。
- Recover 没有 HP>0 或 Broken 过滤；HP=0 且仍占位的 landform 满足 `HP<Max_Blood`，`heal()` 后 `State_Update()` 可进入 Revived。因此源语义允许对“仍存在的 0 HP 废墟 occupant”复活。
- Tower 是例外：Tower 死亡会移除 occupant/map_data 并释放自身，之后 Recover 无目标：`scene/in_scene/enermy/tower.gd:47-64,72-91`。

## 5. Built / Tower

### 已确认

- 选择阶段与 Resolve 均同时检查 stack occupant 和 `map_data.landform/landform_in`；不存在的 coord、非 Dictionary map entry、当前被占用均失败：`scene/in_scene/hex_map_modules/rules/HexTargetRules.gd:51-78`；`scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd:26-56`。
- `build_count` 是遍历范围时的成功创建上限，命中上限立即停止，不是 Tower 等级：`scene/in_scene/timeline/commands/BuiltCommand.gd:15-19,28-46`。当前 fixture range 只有中心单格，`value=1` 最多创建一个。
- creation registry 当前只注册 `tower`；未知 ID `push_warning` 并 no-op，不返回可供上层观察的显式错误结果：同文件 `7-9,52-66`。
- Tower 在调用基类初始化前设置 `Max_Blood=100`；基类 `_init()` 把 `HP=Max_Blood`。Tower `Attitude=Middle`，目标/位置为创建坐标，不生成敌方意图：`scene/in_scene/enermy/tower.gd:11-35`；`scene/in_scene/tile.gd:163-179`。
- 注册器把 `owner_battle/location/target` 写成真实 coord，将 entity 作为 stack child，写 `stack.meta.occupant`，并同步写入 `map_data.landform`、`landform_in`、`landform_type`；Middle Tower 进入 `Middle` group：`scene/in_scene/hex_map_modules/registrars/RuntimeLandformRegistrar.gd:72-109,174-181`。

### 需要收紧/确认

- `BuiltCommand._init()` 使用 `maxi(1,new_build_count)`，所以源运行时的 0/负数 value 会被改成 1，而不是失败：`scene/in_scene/timeline/commands/BuiltCommand.gd:17-20`。真实 fixture 固定为 1；Unity typed schema 已更严格，建议保留 `value>=1` 显式验证，不复制此容错。
- 注册器本身在写入前不再检查占用；最终防线位于 `BuiltCommand`。Unity 应把最终原子占用检查放在 Domain/Application mutation 边界，避免 handler 与 board mutation 之间出现竞态。

### 02B3 非目标但必须记录的源行为

Tower `Behavior()` 每次 `step_next` 自损 50：`scene/in_scene/enermy/tower.gd:39-45`。真实回合顺序是先 resolve timeline，再 emit `step_next`：`scene/in_scene/in_scene.gd:551-573`；HexMap 随即遍历建筑调用 `Behavior()`：`scene/in_scene/hex_map.gd:1714-1716`；`scene/in_scene/hex_map_modules/turn/TileTurnBehaviorRunner.gd:29-39,60-67`。因此 Godot 中当回合创建的 Tower 会在同一次结束回合从 100 立即降到 50。本阶段按 Prompt 只创建 HP100 occupant，不执行这一步；02B3 再一次性接入。

## 6. Poison

### 已确认

- 命令把负 stack 压 0；执行时重新读取当前 occupant，通过 `add_status()` 与 HP>0 门禁后添加 `poison`：`scene/in_scene/timeline/commands/PoisonCommand.gd:14-39`；`scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd:81-87`。
- `landform.add_status()` 对 `stacks<=0` 或 Broken 状态 no-op；每个 landform 拥有独立 `StatusComponent`：`scene/in_scene/tile.gd:182-224`。
- `StatusComponent.add_status()` 使用 `old + stacks`，不覆盖旧值、不设上限；未知状态拒绝。`get_status_snapshot()` 返回独立 Dictionary 副本：`scene/in_scene/status/status_component.gd:43-60,121-122`。
- 状态 icon 使用 `res://image/effect/poison_icon.png`，新增后延迟 1 秒显示；施加 VFX 使用 `poison` registry：`scene/global/StatusDB.gd:9-22`；`scene/in_scene/status/status_component.gd:125-166,169-189`；`scene/in_scene/tile.gd:290-305`。

### 02B3 非目标但必须记录的源行为

- 新回合开始，HexMap 先为所有 occupant 拍状态快照，再逐个处理，避免新传播对象同回合连锁：`scene/in_scene/hex_map.gd:1987-2014`。
- 每个旧中毒 occupant 先向六邻 occupant 各加 1，再按 `ceil(Max_Blood*0.1*快照层数)` 扣血，最后当前层数减 1：`scene/in_scene/tile.gd:243-287`；`scene/global/StatusDB.gd:11-22,57-66`；`scene/in_scene/status/status_component.gd:89-96`。
- 时间轴结算与 Tower behavior 后立刻进入 `start_new_turn()`，其中先处理状态再抽牌：`scene/in_scene/in_scene.gd:551-599`。所以本回合刚施加的 poison 会在紧接着的新回合开始触发一次；本阶段仍不得提前接线。

## 7. Wind / Tornado Clear

### 即时会话与合法性

- clear 卡选中后以 null 地图目标直接进入 DragShapeController；clear mode 使用 effect value 解析的 mask，并保持卡牌停在手牌原位：`scene/card/custom_card.gd:310-314,542-551`；`scene/in_scene/DragShapeController.gd:314-346`；`scene/in_scene/drag_modules/rules/DragCardShapeResolver.gd:11-17`。
- 合法性只检查 mask 非空以及所有格都在 12x3 边界内，不检查占用：`scene/in_scene/timeline/TimelineClearEffect.gd:104-115`；`scene/in_scene/drag_modules/rules/DragPlacementQueryService.gd:11-25`。
- 因此 wind 合法原点为 x=0..10、y=0..1；tornado 合法原点为 x=0、y=0..2。空清合法。
- 确认 clear 不调用 `TimelineManager.place_action()`；执行后无论移除 0 个还是多个 action，都走成功结束并尝试移入弃牌堆：`scene/in_scene/DragShapeController.gd:801-834,967-988`。
- 右键取消只清预览/选择并回手，不执行 clear：同文件 `653-673`；强制打断同样只 `_end_dragging()`：`1043-1057`。

### action identity 与完整移除

- 普通 action 放置时同一 `TimelineAction` 实例写入��所有占格：`scene/in_scene/timeline/TimelineManager.gd:133-152`。
- clear 扫 mask 时以 `action.get_instance_id()` 去重，同一多格 action 最多加入 removed list 一次：`scene/in_scene/timeline/TimelineClearEffect.gd:190-226`。
- 删除按 action 的全部 `get_absolute_coords()` 擦除，并只擦除仍引用该 action 的格子，因此任一格命中即整 action 移除，不会误删后来替换的格子：`scene/in_scene/timeline/TimelineManager.gd:424-440`；无动画 fallback 同样按引用保护：`scene/in_scene/timeline/TimelineClearEffect.gd:363-376`。
- clear execute 不检查 `TimelineAction.Type`、source 或阵营，只要 grid cell 持有有效 action 就删除。因此当前可观察规则是玩家 action 与敌人 action 均可 clear；未来若建筑 action 进入同一 grid，也应默认可 clear，除非后续产品契约明确新增过滤。

### 当前源实现与阶段收紧规则的冲突

1. `get_clear_shape_coords()` 在 clear `value` 缺失时回退顶层 `shape`：`scene/in_scene/timeline/TimelineClearEffect.gd:45-62`。本阶段必须禁止回退。
2. `parse_shape_coords()` 对无任何 `1` 的 mask 经 `_normalize_coords()` 变成 `(0,0)` 单格：同文件 `65-101,244-261`。本阶段 typed parser 应继续显式拒绝空/非法 mask。
3. Preview 当前用红/蓝/绿区分越界/空格/命中，占用格只由颜色变化；所有状态统一加 2px 同色边框，没有状态特异的 icon、线型或形状：同文件 `20-25,128-177,274-284`。这不满足本阶段“除颜色外再有冗余”的视觉门禁，Unity 必须另加状态特异标记。

## 8. 资产与牌组语义

| 资产 | 尺寸/格式 | SHA-256 | 使用点 |
| --- | --- | --- | --- |
| `card_asset/recover.png` | 1135x1590, RGB24 | `F9065BC4FD646D8EEFC463FBC35798EDDDDDE489C4214F6333ACCC6E54A23D99` | `card_data/recover.json:3` |
| `card_asset/tower_card.png` | 1135x1590, RGB24 | `73BFFC6162D4D0D09D196835E3AC1BE5D40FEE19BC6C5BC391008386471AF05B` | `card_data/tower.json:3` |
| `card_asset/poison_card.png` | 1135x1590, RGB24 | `936B2BE26879462D0A18E1F0F5867E3681461A744E73E56823E3B1E8AEDCB879` | `card_data/poison.json:3` |
| `card_asset/wind.png` | 1135x1590, RGB24 | `BFCDA4C0A872FE327EFDFB2FF6880E4A343CC176484CCE5BEE0BC13C39DDB498` | `card_data/wind.json:3` |
| `card_asset/tornado.png` | 1135x1590, RGB24 | `B369DCC027670C4AA28E003FC61B97DCE5DB077B982AEE6BAFDC84766B00E564` | `card_data/tornado.json:3` |
| `image/enermy/tower/tower.png` | 256x256, ARGB32 | `41DD24EF1670355906BA80247233BDC422E6022AB0A7AE42B8ACEB962E18F014` | `scene/in_scene/enermy/tower.gd:18-25` |
| `image/effect/poison_icon.png` | 160x160, ARGB32 | `4445561C52BBD9DC4206CA475D72E1D6178361281E558459739F28CB72013812` | `scene/global/StatusDB.gd:11-22` |
| `image/effect/poison.png` | 1024x749, ARGB32 | `B4777B5C0E0A489B1803C0FE97FDEF57D1FE7FBDE4763835FF8321F1AEEAE011` | `scene/in_scene/effect/tile_poison_vfx.tscn:4,30,52-62` |
| `image/effect/recover.png` | 1024x774, ARGB32 | `5DE64118080E072188BC7A6130630ED2586F6702AAB273D484B8483869A68BD7` | `scene/in_scene/effect/tile_recover_vfx.tscn:4,30,52-62` |

Godot 正式 starter deck 包含 Recover×2、Wind×2、Tower×2、Poison×2，不包含 Tornado：`scene/global/global_db.gd:6-8`。Tornado 是时代 2 合成结果，配方 `wind_tower -> tornado`：`scene/in_scene/rewards/CraftReward.gd:42-44`。Unity 的全卡调试手牌可以包含 Tornado，但不得据此更改正式初始牌组。

Godot 的 Tower 是运行时脚本实例，不是保存的 `.tscn`；Unity 本阶段要求创建可维护 Prefab 是迁移架构要求，不应误解为源项目已有 Tower Prefab。

## 9. 冲突、未知点与建议决策

| 项 | 当前证据 | 建议 |
| --- | --- | --- |
| Recover 的 0 HP occupant | Godot 可 heal 并 Revived；Prompt 未明确排除 | 作为已确认源语义实现并加测试：存在且 HP=0 可恢复；已移除 occupant 仍 no-op |
| “支持生命值”判断 | Godot 对仅有 `heal()`、无 HP/Max 的自定义 Node 也判合法 | Unity typed occupant 模型应要求明确 HP/Max；这是架构收紧，不影响真实 Godot landform |
| 稳定 HexCoord | Godot action 只存 target tile Node | Unity 按 Prompt 保存 HexCoord 并 Resolve 重查，覆盖 tile/occupant replacement 测试 |
| Built 非法 value / 未知 creation | Godot 把 value<=0 压为 1；未知 creation warning+no-op | Unity schema/registry 显式失败；真实 fixture value=1 不受影响 |
| clear mask fallback | Godot 回退普通 shape，空 mask 变单格 | Unity 只接受 typed ClearMask 并拒绝缺失/空/非法 mask |
| clear 的阵营过滤 | 源码无过滤，玩家/敌人均删除 | 不按文案猜测过滤；Domain 测试至少分别清玩家与敌人 action |
| clear 非颜色冗余 | Godot 只有统一边框，状态差异仍只靠颜色 | Unity Preview 为越界/空格/命中提供不同 icon/纹理/边框线型之一 |
| Godot 自动化测试 | 排除 addons/Unity 后未发现相关 GDScript 测试 | 不能把源实现当已被测试证明；Unity 行为测试必须完整补齐 |

上述差异都可由当前文件消解，不构成需要用户决策的硬阻塞。

## 10. 本阶段非目标

- Tower 的 `step_next` 自损 50、0 HP occupant 清除。
- Poison 的回合开始快照、六邻传播、10% MaxHP×层数伤害与层数衰减。
- 完整敌人意图/行为、抽弃牌、时间币、胜负、奖励与局外流程。
- 复制 Godot permissive JSON 容错、clear mask fallback、运行时 Tower 脚本实例化架构。
- 最终 VFX/营销式视觉重做；本阶段只需源风格的最小可观察表现与真实视觉证据。

## 11. 建议自动化测试矩阵

### Recover

1. fixture 精确值/range/shape；三格 shape 在 12x3 边界的合法/非法原点。
2. 选择：范围内受伤 occupant 合法；全空、全满血、无 HP 接口非法；中心空但侧翼受伤 occupant 合法。
3. Resolve：occupant 消失 no-op；同 coord 换新受伤 occupant 时治疗新对象；变满血 no-op；地块节点被替换仍按稳定 HexCoord 找到新 stack。
4. HP=0 且 occupant 仍存在时恢复/复活；结果钳制 MaxHP；失败路径 board、HP、timeline 无副作用。

### Built/Tower

1. 真实空地成功；选择时占用失败；Resolve 前被占用 no-op；不存在的 coord no-op。
2. `value=1` 最多创建一个；未知 creation、0/负 value 显式失败且无 mutation。
3. Tower snapshot：stable type `tower`、Middle/neutral、HP=MaxHP=100、真实 HexCoord、唯一 occupant；Presentation 实例挂到对应 OccupantAnchor/Prefab。
4. 本阶段创建后不会自行降到 50；02B3 再单独证明同一结束回合立即自损。

### Poison

1. fixture 精确值 2/range/shape；活体成功、空格/0 HP/Broken/无状态接口 no-op。
2. 0→2、2→4、任意大整数继续相加；before/after snapshot 可观察且副本不别名内部字典。
3. Resolve 前 occupant 消失 no-op；同 coord 替换后施加到新 occupant；失败无副作用。
4. 本阶段多次推进普通 effect handler 不触发传播/伤害/衰减；状态 icon 使用源资产且不遮挡 occupant/选择。

### Clear

1. typed mask 精确为 wind 2x2、tornado 12x1；禁止回退普通 shape；缺失/空/非法 mask 显式失败。
2. wind 边界 x=0..10,y=0..1；tornado 仅 x=0,y=0..2；越界不消耗、不改 grid。
3. 合法空清成功、消耗会话但不创建 action/不占格；取消完全无副作用。
4. 多格 action 命中一格即全删；同 action 被多格命中只返回一次；同时命中多个不同 action 各删一次。
5. 玩家 action、敌人 action、未来建筑 action 分别可清；引用/稳定 action ID 保护不误删已替换格。
6. 普通 `CanPlace/TryPlace/Commit` 仍禁止重叠且不受 clear 合法性污染；Preview 的越界/空格/命中同时具备颜色与非颜色状态冗余。

### Fixture/牌组/资产

1. 五份 Unity JSON 与 `card_data` 源哈希继续一致；七卡 schema fixture 保留现有 `CardJsonAdapterTests`。
2. starter deck 不出现 Tornado；全卡 debug hand 可出现七 ID，但不回写正式牌组。
3. 五张卡面文件名来自 `front_image`，Tower/Poison 不从 stable ID 猜测特殊文件名。

## 12. 审计边界

- 本报告基于当前 `unity_7.31` HEAD 与工作区文件；相关 Godot 源文件均为已跟踪、非 dirty 文件。
- 未运行 Godot 或 Unity；因此本报告是源码/fixture/资产静态证据，不替代本阶段要求的 Unity EditMode、PlayMode、Build、Player smoke 与视觉检查。
- 本 Agent 只新增本报告，未触碰任何现有 dirty 文件，未切分支、stash、暂存、commit 或 push。
