# Unity 战斗切片与解耦阶段测试计划

> 状态：Wave 02B3 Gate D 已通过
> 负责人：主智能体
> 最后验证日期：2026-08-02
> 证据来源：harness 设计、首切片契约、Unity Test Framework 1.6.0

## EditMode

- `HexCoord` 相等性与 axial→XZ 已知点。
- 时间轴接受合法单格，拒绝越界与冲突。
- 多格 action 只执行一次；顺序为 x 后 y。
- damage 100 把 10 HP 钳制为 0。
- 敌人意图记录位于玩家 action 后。
- 同 seed 731 两次得到完全相同 snapshot。
- 真实 `lighting.json` 得到稳定 ID、damage、range 与 shape。

## PlayMode

- 加载 `CombatVerticalSlice.unity` 不产生 Error/Exception。
- 场景存在透视轨道相机、19 格、目标、Canvas、36 个时间轴槽。
- elevation 1 含 `Block-0`/`Block-1`，层间距严格为 `0.32`，每层有独立 collider 和 FBX 可视对象。
- 0/90/180/270 度均能选中同一目标；俯仰与距离输入被夹紧。
- UI 覆盖坐标阻止世界 raycast，空白区域仍能选格。
- 通过控制器公共方法完成选卡、选目标、放置与结算。
- HP 文本和 3D 目标状态从 10 更新到 0；状态显示意图已处理。
- 1920×1080 与 1280×720 布局锚点不导致主要控件超出安全区。

## Batchmode 与构建

- 首次导入/编译退出码 0。
- EditMode 和 PlayMode XML 中失败数为 0。
- scene validation 退出码 0。
- Windows Player build 结果为 Succeeded；build 产物忽略，不入 Git。

## 视觉审查

- PNG 尺寸与文件名匹配，像素不是全透明/单色。
- 四个方位均有六边形实体、两层高地、目标、相机视角和 UI。
- 12×3 时间轴不越界，按钮/状态文字不重叠，1280×720 仍可读。
- 结算前/后至少一张截图能证明 HP 或目标状态变化。

## 执行结果

| 门禁 | 结果 | 结构化证据 |
| --- | --- | --- |
| 静态编译/领域反射测试 | 7 个程序集、7 个 asmdef、11 个领域用例通过 | `evidence/unity-slice-01/verification-summary.md` |
| Unity EditMode | 19/19，通过 0 失败 | `evidence/unity-slice-01/editmode-results.xml` |
| Unity PlayMode | 2/2，通过 0 失败 | `evidence/unity-slice-01/playmode-results.xml` |
| Harness/build | scene 校验、三张 PNG、Windows build 均通过 | `evidence/unity-slice-01/harness-summary.json` |
| Windows Player | 固定交互完成，`TIMEKEY_PLAYER_SMOKE_PASS`，退出码 0 | `evidence/unity-slice-01/verification-summary.md` |
| 人工视觉审查 | 1920×1080 与 1280×720 无裁切、重叠或缺失控件 | `evidence/unity-slice-01/verification-summary.md` |
| Wave 02A Unity PlayMode | 4/4，四向选择、UI 门禁、堆叠和结算均通过 | `evidence/unity-slice-02-board/playmode-results.xml` |
| Wave 02A Harness/build | 5 张 PNG、Windows build 与 Player 冒烟通过 | `evidence/unity-slice-02-board/verification-summary.md` |
| 地块几何 | 两变体各 120 三角形、0 非流形边，双层无缝 | `evidence/hex-tile-agent/model-validation.json` |
| Wave 02B1 Unity EditMode | 31/31，`CanPlace`、session 状态/失败/取消/重复 commit 与既有规则均通过 | `evidence/unity-slice-02b1/editmode-results.xml` |
| Wave 02B1 Unity PlayMode | 15/15，真实右键取消链、输入互斥、范围/时间轴预览、提交/结算与回归均通过 | `evidence/unity-slice-02b1/playmode-results.xml` |
| Wave 02B1 Harness/build | 12 张集成 PNG、场景验证与 Windows build `Succeeded` | `evidence/unity-slice-02b1/harness-summary.json` |
| Wave 02B1 Windows Player | 退出码 0，日志含 `TIMEKEY_PLAYER_SMOKE_PASS` | `evidence/unity-slice-02b1/verification-summary.md` |
| Wave 02B1 人工视觉审查 | 1920、1280、2560 与四向镜头无卡面变形/裁切/关键遮挡/预览漂移 | `evidence/unity-slice-02b1/verification-summary.md` |
| 解耦 R3 Unity EditMode | `92/92`，0 失败、0 跳过；覆盖 Composition、Infrastructure catalog/registry、Application 与 trace 回归 | `evidence/unity-decoupling-r3/editmode-results.xml` |
| 解耦 R3 Unity PlayMode | `31/31`，0 失败、0 跳过；覆盖 Binding/Presenter、七卡 hand、lighting/earthquake 与 Scene 回归 | `evidence/unity-decoupling-r3/playmode-results.xml` |
| 解耦 R3 Harness/build | 场景验证通过，14 张 PNG 生成并通过像素门禁，Windows build `Succeeded`、`206747014` bytes | `evidence/unity-decoupling-r3/harness-summary.json` |
| 解耦 R3 Windows Player | 退出码 0，运行日志包含 `TIMEKEY_PLAYER_SMOKE_PASS`；原始日志按规则不入 Git | `evidence/unity-decoupling-r3/verification-summary.md` |
| 解耦 R3 人工视觉审查 | 七卡在 1280/1920/2560 三视口完整可见；四向范围、valid/invalid 时间轴与 earthquake 前后均通过 | `evidence/unity-decoupling-r3/*.png` |
| Remaining Cards Gate A EditMode | `107/107`，0 失败、0 跳过；Recover 与完整既有领域/应用/基础设施回归 | `evidence/remaining-cards-gate-a/editmode-results.xml` |
| Remaining Cards Gate A PlayMode | Recover 真实 Scene 公共路径 `1/1`，0 失败 | `evidence/remaining-cards-gate-a/playmode-results.xml` |
| Remaining Cards Gate A 视觉 | 5 张 1280x720 PNG；卡面、目标、三格 Timeline、提交前与 10→100 结算后人工通过 | `evidence/remaining-cards-gate-a/verification-summary.md` |
| Remaining Cards Gate B EditMode | `130/130`，0 失败、0 跳过；Built/Poison 与 Scene/Prefab 全回归 | `evidence/remaining-cards-gate-b/editmode-results.xml` |
| Remaining Cards Gate B PlayMode | Tower、Poison、序列化场景 `3/3`，0 失败 | `evidence/remaining-cards-gate-b/playmode-results.xml` |
| Remaining Cards Gate B 视觉 | 8 张 1280x720 PNG；Tower HP100 与 Poison stacks2 的卡面/目标/Timeline/结算人工通过 | `evidence/remaining-cards-gate-b/verification-summary.md` |
| Remaining Cards Gate C EditMode | `152/152`，0 失败、0 跳过；Clear Domain/Application、registry 与 Scene 全回归 | `evidence/remaining-cards-gate-c/editmode-results.xml` |
| Remaining Cards Gate C PlayMode | Wind、Tornado、2×2/12×1 三态与完整 UI 清除 `4/4`，0 失败 | `evidence/remaining-cards-gate-c/playmode-results.xml` |
| Remaining Cards Gate C 视觉 | 9 张 1280x720 PNG；红 `!`、蓝 `○`、绿 `HIT`、取消恢复、Wind 移除与 Tornado 空清人工通过 | `evidence/remaining-cards-gate-c/verification-summary.md` |
| Remaining Cards Gate D full EditMode | `152/152`，0 失败、0 跳过 | `evidence/remaining-cards-gate-d/editmode-results.xml` |
| Remaining Cards Gate D full PlayMode | `38/38`，0 失败、0 跳过；包含 Binding/Presenter、七卡 Scene、四向与回归 | `evidence/remaining-cards-gate-d/playmode-results.xml` |
| Remaining Cards Gate D harness/build | 七卡完整路径、54 张 PNG，Windows build `Succeeded`、`207171486` bytes | `evidence/remaining-cards-gate-d/harness-summary.json` |
| Remaining Cards Gate D Player | 退出码 0，日志含 `TIMEKEY_PLAYER_SMOKE_PASS` | `evidence/remaining-cards-gate-d/player-smoke-summary.json` |
| Remaining Cards Gate D 人工视觉 | 1280/1920/2560、lighting/earthquake 四 yaw、Tower/Poison 四 yaw、Clear 三态与残留检查均通过 | `evidence/remaining-cards-gate-d/verification-summary.md` |
| 简体中文 EditMode / PlayMode | `161/161`、`38/38`，0 失败；集中式文案、Silver 字形、Scene/Prefab 与交互状态 | `evidence/simplified-chinese-localization/` |
| 简体中文视觉 / build / Player | 8 张三视口与关键状态 PNG 人工通过；build `Succeeded`；Player exit 0 + smoke marker | `evidence/simplified-chinese-localization/verification-summary.md` |

## Wave 02B1 计划门禁

- EditMode：`CanPlace` 无副作用；合法、越界、冲突、无目标、重复 commit、cancel 后 commit。
- PlayMode：idle/hover/select/cancel；未选牌不能选目标；未选目标不能预览/放置；UI 不泄漏世界 raycast。
- PlayMode：Selected/Targeting/Scheduling 暂停 orbit，右键优先取消；退出后恢复 orbit。
- PlayMode：有效/无效时间轴预览、确认占格、`lighting` 在敌人意图前结算、重复 Build 不复制监听。
- 视觉：`1920×1080` idle/hover/selected/target/timeline/resolved，`1280×720` placed，`2560×1080` selected。
- 四向：0/90/180/270 yaw 的相同 `HexCoord` 范围保持一致且不被卡牌 UI 遮住。
- 像素检查：卡牌区域、时间轴预览区域分别非空且状态间有差异；整图非单色不能替代局部证据。
- 回归：现有 EditMode 19/19、PlayMode 4/4、Windows build 与 Player smoke 不回退。

以上门禁已全部执行并通过；详细结果见 `evidence/unity-slice-02b1/verification-summary.md`。

## Wave 02B2A 计划门禁

### Infrastructure / EditMode

- 七张真实 JSON 的 stable/numeric ID、front_image、typed effect、range、普通 shape/clear mask 精确解析；源文件哈希一致。
- 未知 effect、错误 numeric/string value、Built 缺 creation、非法/空 clear mask 显式失败。
- `earthquake` 以中心加六邻格执行 `+2`；边缘只影响存在格，缺失格不生成。
- logical layer 1->3；上限 5->removed；ResolutionSnapshot 记录 before/after/removed；固定 seed 731 可重复。
- 两格 shape 的 CanPlace/Preview/Commit 保持单一合法性来源；旧 `lighting` Damage 和 02B1 无副作用测试全回归。

### Component PlayMode

- 两卡 hand host 显示原 `lighting/earthquake`，单选互斥、取消/drag/input gate、重复 Build 幂等。
- `HexTileColumn` 的 1/3 层分别有 1/3 个独立 mesh、renderer、collider；相邻 block 世界 Y 差严格为 `0.32`。
- Apply 后 TopBounds、occupant anchor、选中 collider 和全部层高亮同步；重复 Apply 不复制对象。

### Integration / Visual / Build

- 完成 earthquake 选卡、中心目标、7 格范围、两格 valid/invalid preview、Commit、Resolve、7 柱各新增两层的公共控制器路径。
- invalid 预览除颜色外还有边框/标记冗余，能与红色敌方意图区分，不依赖顶部英文状态文字。
- 0/90/180/270 yaw 的范围坐标、顶层选择和 collider 命中一致；1280x720 与 2560x1080 两卡 UI 无裁切/遮挡。
- 截图覆盖 selected、四向 range、timeline valid/invalid、before/after 和两种附加视口；分别检查卡牌/时间轴/升高棋盘区域像素差异，并人工检查真实层结构。
- 既有 EditMode 31/31、PlayMode 15/15 及 lighting 完整闭环不回退。
- Windows build 成功；Player 退出码 0 且含 `TIMEKEY_PLAYER_SMOKE_PASS`。

以上门禁已执行并通过：EditMode `67/67`、PlayMode `25/25`、Cards 子集 `9/9`、Terrain 子集 `3/3`；Editor harness 输出 11 张集成 PNG，Windows x64 development build 为 `Succeeded`，Player 退出码 0 且包含 smoke marker。结构化结果与人工视觉结论见 `evidence/unity-slice-02b2a/verification-summary.md`。

## 解耦 R3 最终门禁

- Infrastructure：七份真实 JSON 进入 `CardContentCatalog`，顺序、唯一 stable ID 与 `FrontImage` 资源路径受测；新增普通卡不要求 Controller 路由变更。
- Composition：`CombatCompositionRoot` 持有并释放 Application session 与运行时 sprite，统一注入 catalog、effect registry、trace sink 和 Presentation binding；程序集依赖无环。
- Presentation：`CardHandPresenter`、`BoardRangePresenter`、`TimelinePresenter`、`CombatHudPresenter` 只消费 view/result；`CombatPresentationBinding` 统一拥有输入订阅，重复 Bind/disable/enable 不复制监听。
- Diagnostics：effect trace 记录 effect kind 与 before/after；关闭或抛异常的 Unity sink 不改变战斗 snapshot。
- 回归：全量 EditMode `92/92`、PlayMode `31/31`；`lighting` 仍为 10 HP -> 0，`earthquake` 仍对七个有效柱各执行 `+2`，层距严格 `0.32`。
- 实跑：14 张 `unity-decoupling-r3` PNG 逐张人工通过，覆盖三视口七卡、两卡交互、四向 yaw、时间轴 valid/invalid 与 earthquake 前后。
- 构建：Windows build `Succeeded`，大小 `206747014` bytes；Player 退出码 0 且 smoke marker 存在。

R3 结构化证据位于 `evidence/unity-decoupling-r3/`。旧切片结果保留为历史证据，最终完成判定以本节全量门禁为准。

## Remaining Cards Gate 0 与自动化矩阵

接手最小冒烟已实跑：Application + earthquake + Scene EditMode `24/24`，`CombatVerticalSliceTests` PlayMode `10/10`，均 0 失败。结果在 Unity `Temp` 中由脚本解析后被下一次启动清理，只作为 Gate 0 门禁；最终证据必须写入新的可提交 evidence 目录。

- Recover：合法/满血/HP0、范围缺失、ID/coord 重判、消失/变满 no-op、MaxHP 钳制、shape 边界、before/after、失败纯度、Controller 零 diff。
- Built：空地、选择占用、Resolve 前占用、未知 creation/value fail-fast、value 创建上限、Tower HP/attitude/coord、本阶段无 decay。
- Poison：活体、空格/死亡/无状态能力、0->2->4、快照副本、重判、本阶段无传播/伤害/衰减。
- Clear：Wind 2x2、Tornado 12x1、边界/空清、多格 action identity 去重、完整移除、玩家/敌人、取消/重复调用、普通放置回归。
- Catalog/Hand：七 ID/原图/配置顺序、同一 CardView Prefab、单选/右键/drag、重复 Build 无复制；三视口检查中间卡 selected/drag 风险。
- Scene/Prefab：Play 前稳定对象存在；Tower/Poison/CardView/TimelineCell 来自 Prefab；Inspector 引用完整且运行时不覆盖布局。
- 最终：full EditMode、full PlayMode、harness、Windows build、Player smoke；1280x720、1920x1080、2560x1080 与 yaw 0/90/180/270 人工开图。

Gate A 已完成上述 Recover 矩阵：全量 EditMode `107/107`、公共 Scene PlayMode `1/1`、启用图形设备的 Editor Harness 与 5 张人工检查截图全部通过；Controller 零 diff。Built/Poison/Clear 仍按后续门禁执行。

Gate B 已完成 Built/Poison 矩阵：全量 EditMode `130/130`、Scene PlayMode `3/3`、定向 Scene/Prefab authoring 与 8 张人工检查截图全部通过。Tower decay、Poison tick/传播/伤害未越界实现；Clear 仍按 Gate C 执行。

Gate C 已完成 Clear 矩阵：全量 EditMode `152/152`、Scene/Presentation PlayMode `4/4`、定向 Scene authoring 与 9 张人工检查截图全部通过。Wind 2×2 命中完整移除敌方 action，Tornado 12×1 合法空清，越界/取消无副作用。

Gate D 已完成最终矩阵：full EditMode `152/152`、full PlayMode `38/38`，harness 逐卡验证 lighting、earthquake、Recover、Tower、Poison、Wind、Tornado 并生成 54 张 PNG；Windows build `Succeeded`，Player 退出码 0 且 marker 存在。三视口和四 yaw 已逐图人工检查，最终完成判定以 `evidence/remaining-cards-gate-d/verification-summary.md` 为准。

## Turn Lifecycle Gate 0 / Gate A 矩阵

Gate 0 接手冒烟已实跑：`CombatApplicationSessionTests` EditMode `25/25`、`CombatVerticalSliceTests` PlayMode `15/15`，0 失败；两份现有脏 `ProjectSettings` 的哈希在运行前后保持不变。前置简体中文证据继续继承 `161/161` EditMode、`38/38` PlayMode、8 张 PNG、Windows build 与 Player smoke，但任何受本阶段修改影响的证据必须重跑。

- Runner phase：首次启动仅执行 status/no-op/intent 尾段；结束回合严格执行七个阶段；阶段历史、输入锁与最终 ready 状态可观测。
- Re-entry/failure：运行中重复请求显式失败；每个 port 的结构化失败停止后续阶段，已冻结 snapshot 不变，状态不得停在半完成阶段。
- Timeline order：12×3 按 x 后 y；多格 action 只 resolve 一次；同格或同 action 的顺序不依赖引用地址或字典枚举。
- Action identity：同一 preview/commit/resolve/clear action 保持同一 ID；跨生命周期序列不碰撞；玩家/敌人共用 identity 类型与 snapshot schema。
- Snapshot：字段完整、集合只读、防御性复制；Presentation 不能通过 Domain collection、Label、颜色或 Prefab 实例取得规则或身份。
- Compatibility：既有普通卡、Recover/Built/Poison/Clear Application 测试全部回归；`CombatSessionPhase` 和 lifecycle phase 不混用；Controller 不新增效果或敌种分支。
- Gate A 集成门槛：定向 EditMode 全通过后再运行 full EditMode 与现有 full PlayMode。Gate A 不改变渲染时可继承前置视觉证据；一旦改 Scene/Prefab/UI，必须生成本阶段实际截图并逐张检查。
- 后续视觉门禁：1280×720、1920×1080、2560×1080 覆盖卡牌选中/响应式缩放、卡牌详情、玩家 action 框、敌人意图框，以及卡牌/敌人/Timeline/地图的双向 hover/select 高亮；不得裁切、重叠、漂移或依赖颜色作为唯一信号。

Gate A 历史结果为定向 EditMode `85/85`、full EditMode `183/183`、full PlayMode `38/38`，0 失败、0 跳过。新增测试覆盖 20 个纯 lifecycle/snapshot case、preview/commit identity、重复 ActionId 原子拒绝、Timeline plan 投影以及 Resolution/Clear snapshot identity；结构化证据位于 `evidence/turn-lifecycle-gate-a/`。Gate B/C 已在下方 Gate D 全量结果中关闭。

## Wave 02B3 Gate D 结果

- Full EditMode：`236/236`，覆盖 runner、intent seed/priority/revalidation、Tower、Poison、death policy、atomic store、session integration、Scene/Prefab 与 Silver Font/Material。
- Full graphical PlayMode：`53/53`，覆盖七卡响应状态、action frame identity、详情框、map/timeline 双向映射、lifecycle Presenter 幂等和 removal cleanup。
- Build：StandaloneWindows64 Development，`Succeeded`，`211055434` bytes，Silver attribution 已复制。
- Player：`-batchmode -nographics -timekeySmokeQuit`，exit 0，marker `TIMEKEY_PLAYER_SMOKE_PASS`。
- 视觉：Gate B 六张、Gate D 六张 PNG 已逐图检查；像素存在性不替代人工结论。

证据入口为 `evidence/turn-lifecycle-gate-b/verification-summary.md` 与 `evidence/turn-lifecycle-gate-d/verification-summary.md`。
