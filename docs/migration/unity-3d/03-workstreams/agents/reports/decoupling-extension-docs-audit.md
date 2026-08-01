# 解耦阶段扩展路径与维护文档只读审计

> Agent：`decoupling-extension-docs-audit`
> 审计日期：2026-08-01
> 分支：`unity_7.31`
> 代码基线：`53567eafd2ac0c3c59ee533caa230a55dc28100b`
> 范围：只读审计卡牌、效果、敌人、机制、章节边界和诊断入口；未运行 Godot/Unity

## 结论

七卡 typed schema 和 `front_image` 数据契约已经存在，但 Unity 运行链还不是可扩展架构。`VerticalSliceController` 同时持有 fixture、资源加载、卡牌目录、stable ID 路由、目标规则、时间轴文案、敌人意图、世界构建、UI 构建和表现同步。新增一张可玩的普通卡仍必须修改 Controller；新增一种效果还必须同时修改 Domain 枚举、JSON adapter、`TimelineGrid` 结算分支、目标路由和表现结果消费。

当前 Unity 没有敌人定义、敌人运行态、敌人 Prefab 或敌人效果结算。`TimelineGrid.Resolve()` 遇到 enemy action 只把 `EnemyIntentResolved` 设为 `true`，`VerticalSliceController.PlaceEnemyIntent()` 只放一个固定、零伤害的 `enemy-intent`。因此不能把当前固定占位描述成“已支持新增敌人”。

Unity 目前也没有结构化 trace sink。可观察入口只有 Controller 公共方法/只读属性、`ResolutionSnapshot`、两个 preview 的 missing-coordinate 集合/事件、Player 与 harness marker，以及异常日志。解耦阶段必须先建立可替换、只观察不改状态的 Diagnostics 边界，再写调试指南。

仓库当前不存在 `docs/migration/unity-3d/06-maintenance/`，也不存在 `unity/Assets/_Project/Prefabs/`。`CombatVerticalSlice.unity` 的 `VerticalSliceRoot` 没有子节点，只序列化了两个 TextAsset fixture；维护文档若现在声称 Scene/Prefab 可人工扩展，会与真实工程矛盾。

## 证据快照

| 事实 | 代码证据 |
| --- | --- |
| Scene 只有空根和 Controller | `CombatVerticalSlice.unity:71,89,104-105`：`VerticalSliceRoot` 的 `m_Children: []`，只绑定 `lightingFixture`/`earthquakeFixture` |
| Controller 直接建立全部稳定结构 | `VerticalSliceController.BuildSceneGraph()`、`BuildWorld()`（559）、`BuildInterface()`（636）；含 `new GameObject`、`AddComponent`、运行时材质和 UI helper |
| 卡牌运行目录硬编码为两张 | `VerticalSliceController` 的 `LightingCardId`/`EarthquakeCardId`、两个 fixture/definition/sprite 字段；`RefreshHand()`（918）手写两个 `CardViewModel` |
| 目标规则按 stable ID 分支 | `SelectTarget()`（208）只接受 lighting；`SelectEarthquakeTarget()`（285）只接受 earthquake；`TrySelectWorldAtScreenPoint()` 在两类之间分支 |
| 时间轴视觉按 stable ID 分支 | `TryPlaceSelected()`（425）用 stable ID 选择 `LIGHT`/`QUAKE` 和颜色；`ResolveTimeline()`（503）用 stable ID 选择状态文案 |
| Typed schema 已稳定 | `CardEffectKind`（`CardDefinition.cs:7`）、`CardDefinition.FrontImage`（227）、`CardJsonAdapter.ParseEffects()`（68）和 `ParseFrontImage()`（247） |
| 效果结算只实现 damage/elevation | `TimelineGrid.ApplyPlayerEffects()`（123）；damage 分支在 131，非 elevation 在 137 直接跳过 |
| 敌人是固定占位 | `VerticalSliceController.PlaceEnemyIntent()`（802）；`TimelineGrid.Resolve()` 仅记录 enemy intent 已处理，不执行 effects |
| Domain 依赖方向正确 | `TimeKey.Domain.asmdef` 的 `references: []` 与 `noEngineReferences: true` |
| Application/Diagnostics 尚不存在 | Runtime 只有 `Domain`、`Infrastructure`、`Presentation`；相应 asmdef/目录均不存在 |

## 当前真实扩展路径

### 新增一张使用既有效果的卡牌

Godot 权威路径已经接近数据驱动：

1. 在 `card_data/<stable-id>.json` 添加定义，在 `card_asset/<front_image>` 添加卡面。
2. `addons/card-framework/card_factory.tscn:10-11` 把目录固定为 `res://card_asset/` 和 `res://card_data/`。
3. `JsonCardFactory.preload_card_data()`（`json_card_factory.gd:106`）扫描全部 JSON；`create_card()`（75）按 JSON 的 `front_image`（94、129）加载图，不需要按卡名增加代码分支。
4. 若只使用 `damage/elevation/recover/built/poison/clear` 既有语义，卡片数据加载本身无需修改 Godot factory；仍需按真实效果补目标、结算和视觉回归测试。

Unity 当前路径并非目录驱动：

1. 添加/同步 `unity/Assets/_Project/Content/Cards/<id>.json`、`Resources/Art/Battle/Cards/<front_image>` 及各自 `.meta`。
2. `CardJsonAdapter` 只能解析已知 effect kind，但七种当前 fixture 已覆盖此步骤。
3. 必须给 `VerticalSliceController` 增加 serialized fixture、parse/register、sprite 生命周期和 `RefreshHand()` 项。
4. 必须给 `CombatVerticalSlice.unity` 绑定新 fixture；当前没有 catalog asset 或目录扫描入口。
5. 必须消除/扩展 `SelectCard` 后的 stable ID 文案、目标方法、raycast 分支、timeline label/color 和 resolve 文案分支。
6. 更新 `CardJsonAdapterTests` 的真实 fixture 期望；新增/更新 `CombatVerticalSliceTests` 与 `VerticalSliceAutomation` 的可玩路径和视觉证据。

就“激活仓库里已经存在的第三张卡”而言，最少仍触碰 Controller、Scene、PlayMode 集成测试和 harness 四个共享面；若是全新源卡，还要增加 Godot JSON/PNG、Unity JSON/PNG 及 `.meta`。这正是主 Prompt 要消除的耦合。

建议冻结后的最小路径（当前尚不存在）：卡牌 JSON + 原图 + 一个序列化 catalog 条目 + adapter/catalog tests + 该卡的领域/视觉验收。Controller 只能消费 catalog 结果，不再声明 fixture 字段或 stable ID 分支。`FrontImage` 必须继续是唯一卡面定位依据。

### 新增一种效果

Godot 权威实现的实际变化点至少有三处：

1. 新建 `scene/in_scene/timeline/commands/<Effect>Command.gd`，继承 `EffectCommand` 并只执行效果。
2. 在 `EffectProcessor._create_command_from_type()`（`effect_processor.gd:102-121`）注册 JSON type。
3. 在 `HexTargetRules.is_stack_valid_target()`（`HexTargetRules.gd:8-48`）补目标合法性；若需要实体过滤，还要补 `_range_has_target_for_effect()`（135 起）。
4. 若有效果专属 VFX，补 `VFXManager`/`default_tile_vfx_registry.tres` 及场景资源。

Unity 当前变化点更多且横跨层级：

1. `CardEffectKind` 和 `CardEffect` payload 验证。
2. `CardJsonAdapter.TryParseEffectKind()`（219）及 numeric/string/creation payload 分流。
3. `TimelineGrid.ApplyPlayerEffects()` 增加结算分支；当前 unknown-to-resolver 的合法 typed effect 会被静默跳过。
4. `CombatSliceState`/board/entity 状态增加真正承载效果的数据；必要时扩展不可变 `ResolutionSnapshot` result，而不是把 Unity 对象写进 Domain。
5. Controller 的目标选择、范围/时间轴状态和结果表现目前仍要同步修改。
6. Infrastructure、纯 EditMode、PlayMode 和视觉证据分别增加覆盖。

建议冻结一个按 `CardEffectKind` 注册的最小 handler 变化点，并让 Application 用例组合 target policy、timeline session 与 effect handler。不要为每张卡建立接口；接口只应对应“效果结算”和“目标策略”这两个真实变化点。必须有测试证明新增普通效果不修改 Controller stable ID 分支，未知/未注册 handler 显式失败，不能沿用当前静默 no-op。

### 新增敌人

Godot 的权威敌人/建筑模型不是单一 `EnemyBase`。实际战斗实体主要继承 `tile.gd` 的 landform 契约，包含 `HP/Max_Blood`、`take_damage()`、`heal()`、status 和以下意图方法：`get_intent_description`、`get_intent_effect_range`、`get_intent_target_center_coord`、`can_generate_intent`、`get_intent_invalid_reason`、`get_intent_target_affiliation`、`does_intent_include_self`、`get_intent_shape`、`get_intent_action`（`tile.gd:490-641`）。

真实调用链是：

```text
具体 landform/敌人
  -> EnemyIntentManager.generate_enemy_intents() / get_intent_action()
  -> TimelineManager.place_action()
  -> TimelineManager.resolve_timeline()
  -> EffectProcessor.process_action()
```

`EnemyIntentResolver.resolve_enemy_intent()`（`enemy_intent_resolver.gd:38`）另外生成 `EnemyIntentData`，供地图/时间轴表现共享，并生成 `debug_label`。注意 `EffectProcessor._parse_enemy_intent()` 当前仍返回空队列，这是 MIG-002 的源行为；Unity 不应借解耦自行发明敌人伤害语义。

Unity 当前没有可执行的“新增敌人”步骤。要形成最小真实路径，至少需要先定义并实现：

- 纯数据敌人运行态（stable ID、HP/MaxHP、坐标），不能用 GameObject 作为 Domain identity。
- 敌人意图来源及 TimelineAction 构造边界；顺序和无效原因必须可观察。
- `TimelineGrid.Resolve()` 对 enemy action 的明确结果，仍应保留 MIG-002 的“当前无命令效果”直到玩法波次确认。
- 保存的 Enemy View Prefab 和 Presenter；Prefab 只消费状态/结果。
- 敌人 fixture/catalog、EditMode 意图测试、PlayMode Prefab/表现测试和集成视觉证据。

在这些对象存在前，维护文档只能写“未支持，以及实现前置条件”，不能写成添加 Prefab 即可完成。

### 新增机制

当前应先按状态所有权分类，避免继续把机制塞进 Controller：

| 机制类型 | 当前真实落点 | 最小变化面 |
| --- | --- | --- |
| 纯规则/数值/回合状态 | `TimeKey.Domain`（目前主要是 `CombatSliceState`、`TimelineGrid`、`CombatBoardState`） | Domain 状态 + result/snapshot + 纯 EditMode characterization；零 UnityEngine |
| 选卡、选目标、预览、提交、取消、结算顺序 | 目前散在 `VerticalSliceController` 与 `CardPlaySession` | 解耦后归 Application session/use case；失败无副作用、重复调用和 seed 测试 |
| JSON/资源目录 | `CardJsonAdapter` 与 Controller 的 `Resources.Load` | Infrastructure adapter/catalog；不得决定效果结果 |
| 输入、镜头、UI、范围/时间轴颜色、Prefab | Presentation | Presenter/View + PlayMode/视觉；不得复制 Domain 合法性 |
| 观察性 | 目前几乎只有 marker/异常 | Diagnostics sink + collector test；不得改变状态或随机序列 |

新机制若跨越两层，应先以 Domain result 或 Application state snapshot 作为边界，再由 Presenter 消费；不能通过全局 event bus、singleton 或 Service Locator 让层之间互相查询。

### 未来章节边界

本阶段只应记录数据边界，不迁移局外流程。Godot 的现有事实是：

- 入场字符串由 `InSceneExternalPayloadParser.parse()` 接受，格式为 `battle_normal <map_seed>`、`battle_elite <map_seed>` 或 `boss_stage <map_seed>`，输出 `battle_tag`、`map_seed` 和原 payload。
- 返回字典由 `InSceneReturnPayloadBuilder.build()` 产生：`transition_type=return_from_combat`、`combat_result=completed`、`battle_state=settlement`、`battle_tag`、`map_seed`、`era`、`timecoins`、`deck_snapshot`、`deck_size`、`room_context`、`clear_active_room_context`（`InSceneReturnPayloadBuilder.gd:17-27`）。
- `RoomResolutionController.should_advance_tier_from_boss_payload()`（34）只有在 completed + boss room + 当前边界匹配时推进章节；Unity 当前完全没有该职责。

建议维护文档把上述字段列为“Godot 权威输入/输出事实”，再定义未来的版本化 Unity DTO 需求，但明确 DTO/bridge 尚未实现。Unity 局内只能接收一次性的战斗上下文并返回战斗结果；章节推进、地图揭示、奖励消费和 active room context 继续由 Godot 拥有。

## 现有调试与追踪入口

### Unity

| 入口 | 能观察什么 | 局限 |
| --- | --- | --- |
| `VerticalSliceController.SelectCard/SelectTarget/SelectEarthquakeTarget/PreviewTimelineSelected/TryPlaceSelected/ResolveTimeline` | 从输入意图到结算的主要断点链 | 业务、表现和组装混在同类；没有 command ID/阶段字段 |
| `CardPlaySession` transition + `CardPlayFailure` | 目标/preview/commit/cancel 的失败原因和无副作用边界 | Controller 没有结构化记录 transition |
| `TimelineGrid.Resolve()`、`ApplyPlayerEffects()` | resolution order、damage/elevation 实际写入 | 未注册 typed effect 静默跳过；enemy 不执行效果 |
| `ResolutionSnapshot`/`TileEffectResult` | 目标 HP 前后、行动顺序、地块前后层数/removed | 只覆盖现有 damage/elevation；没有通用 effect result/失败记录 |
| `BoardRangePreview`/`TimelinePlacementPreview` 的 `ActiveCoordinates`、`MissingCoordinates` 和 `MissingCoordinate` event | 范围/时间轴缺格 | 没有统一 sink；事件默认无人记录 |
| `Debug.Log("TIMEKEY_PLAYER_SMOKE_PASS")`、`TIMEKEY_EFFECTS_HARNESS_PASS`、`Debug.LogException` | 最终 smoke/harness 和未处理异常 | 不是逐命令 trace |
| `VerticalSliceAutomation.BuildValidateAndCapture()` | 固定场景、截图、build 的集成断点 | 流程写死 lighting/earthquake，无法作为通用内容调试器 |

建议最小 Diagnostics 事件字段：命令/会话 ID、阶段、card stable ID、actor kind、target ID/coord、timeline origin/shape、effect kind、before/after、success/failure reason、seed/turn。sink 必须可关闭和替换为测试 collector；发送 trace 前后应能得到字节级等价 snapshot。

### Godot 权威参考

- `scene/global/SceneLog.gd` 统一写控制台和 `user://logs/scene_flow.log`；局外入房、局内 payload、返回和 room resolution 已使用 `scene_event/error_event`。
- `EnemyIntentData.build_debug_label()`（235）输出 source、坐标、valid/invalid 和 target center。
- `EffectProcessor._create_command_from_type()` 是效果分派的首要断点；未知类型当前返回 `null` 且无日志，迁移时必须改为可观察失败。
- `HexTargetRules.is_stack_valid_target()` 是地图目标合法性的首要断点；效果命中规则不能只在表现层检查。

## 维护文档缺口

主 Prompt 要求的九份 `06-maintenance` 文档目前全部缺失：

1. `scene-and-prefab-guide.md`
2. `debugging-guide.md`
3. `add-card.md`
4. `add-effect.md`
5. `add-enemy.md`
6. `add-mechanic.md`
7. `chapter-boundary.md`
8. `testing-and-evidence.md`
9. `module-ownership.md`

`02-architecture/combat-modular-architecture.md` 也不存在。现有 `target-architecture.md` 仍把 Controller 定义为 composition root，并列出与实际目录不完全一致的 `Presentation/Battle/`；它不足以指导解耦后的 Scene/Prefab 编辑、Application 生命周期、handler 注册或 Diagnostics。

每份扩展指南在发布前必须至少包含：真实入口和文件、最小修改面、Inspector/Prefab 操作、相应 EditMode/PlayMode 测试、可复制命令、视觉检查、常见失败、断点/trace 字段和回滚方法。R1/R2/R3 尚未落地的内容必须标成“目标状态”或“未实现”，不能用推荐接口名伪装成现有 API。

## 风险与主智能体建议

| 优先级 | 风险 | 建议门禁 |
| --- | --- | --- |
| P0 | Controller stable ID 分支让第三张卡继续扩大共享文件冲突 | R2/R3 必须用测试证明新增一张既有效果卡不修改 Controller 分支 |
| P0 | typed effect 能解析但 resolver 静默跳过 Recover/Built/Poison/Clear | 未注册 handler 显式失败；clear 继续独立即时 session，不进入普通 TimelineAction |
| P0 | 文档可能把固定 enemy marker 误写成敌人系统 | `add-enemy.md` 先明确“不支持”和 MIG-002；只有实体状态/意图/Prefab/tests 均存在后才给操作教程 |
| P1 | Resources 与 runtime Sprite 生命周期仍在 Controller | 抽出只读 card catalog/artwork provider；继续严格使用 `FrontImage` |
| P1 | 目标策略同时按 card ID 和 effect type 分散 | 目标策略按真实 effect/target mode 注册；Presentation 只显示 Application 返回的合法范围 |
| P1 | 没有结构化 trace，重构失败只能靠 UI 文案和异常 | R2 先建 collector 测试，R3 接 Unity sink；验证启停 trace 不改变 snapshot |
| P1 | 章节 DTO 容易越权接管 Godot 局外 | 只写版本化数据边界和字段映射；本阶段不加 SceneManager/章节服务 |
| P2 | `CardHandHost`/`CardHandView` 自己创建子节点，Prefab 化后可能重复层级 | R1 characterization 要覆盖 Scene 预存结构、重复初始化和事件只订阅一次 |

推荐集成顺序：先冻结 characterization 与 Diagnostics 记录格式，再抽 Application 会话和效果 handler，随后抽 card catalog/artwork boundary；敌人只冻结未来所需数据/意图边界，不提前实现玩法；最后依据真实 Scene/Prefab/类名编写九份维护文档。这样可以让文档描述已验证的工程，而不是描述计划中的架构。
