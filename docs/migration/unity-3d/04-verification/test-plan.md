# Unity 战斗切片与解耦阶段测试计划

> 状态：解耦 R3 全部门禁已通过
> 负责人：主智能体
> 最后验证日期：2026-08-01
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
