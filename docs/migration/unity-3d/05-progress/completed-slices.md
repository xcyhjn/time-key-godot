# 已完成切片

> 状态：截至 Wave 02B4 Gate D 已完成
> 负责人：主智能体
> 最后验证日期：2026-08-02
> 证据来源：Slice Definition of Done

## Wave 00：迁移评估基线

完成日期：2026-07-31。

- 确认 `unity_7.31`、Godot 4.6.2 与 Unity 6000.4.10f1。
- 运行 Godot headless/import 与图形流程，保存日志和 8 组关键截图。
- 完成可行性 4/5 评估、依赖/数据/风险盘点和 3D 边界。
- 冻结首切片契约、路线图、ownership 与 agent prompts。

这是一项迁移门禁成果，不计作 Unity 功能切片。Slice 01 只有在测试、batchmode、build、视觉证据和 parity 全部通过后才会登记到这里。

## Wave 01：战斗垂直切片 01

完成日期：2026-07-31。

- Unity 6000.4.10f1、URP 17.4.0、uGUI 2.0.0、Test Framework 1.6.0 工程可导入和构建。
- 真实 `lighting.json` 哈希与 Godot 源文件一致，适配到纯 C# 卡牌/时间轴/结算领域模型。
- 固定 seed 731 的 3D flat-top axial 白盒场景完成选卡→选目标→放置→解析；目标从 10 HP 变为 0，敌方意图按冻结顺序记录。
- Unity EditMode 19/19、PlayMode 2/2、Windows Player 运行时冒烟全部通过。
- 初始/解析后 1920×1080 及解析后 1280×720 截图通过像素门禁与人工布局审查。
- 回合推进、胜负奖励、局外闭环、完整卡牌效果和授权素材仍按契约留给后续波次。

## Wave 02A：局内 3D 战斗棋盘

完成日期：2026-07-31。

- 19 格 flat-top 棋盘使用透视轨道镜头，可 360° 旋转、俯仰、缩放和平移。
- UI 覆盖区域阻止世界选择；同一目标在 0/90/180/270 度均可 raycast 选中。
- elevation 改为每层 `0.32` 的独立 mesh/collider 实体堆叠，两处高地均为真实两层。
- Blender 草地/裸土模型各 120 三角形、0 非流形边，可复现源文件、FBX 和三张模型证据已落盘。
- 原 `center_altar.png` 作为完整面向相机的世界 billboard，保留原项目视觉语言。
- Unity EditMode 19/19、PlayMode 4/4、四向 1920×1080、1280×720、Windows build 与 Player 冒烟全部通过。

## Wave 02B1：原版 lighting 卡牌交互

完成日期：2026-07-31。

- 原 `lighting.png` 与 `behide.png` 逐字节接入；卡面保持 `1135×1590` 原尺寸与底部 `125×175` 交互基准。
- 手牌完成 idle、hover、selected/targeting/scheduling、右键/Escape 取消与输入隔离；选牌期间轨道镜头暂停，取消或提交后恢复。
- `TimelineGrid.CanPlace` 与 `CardPlaySession` 提供无副作用的目标/时间轴预览，失败、取消与重复 commit 不改变占格。
- `effect_range` 直接投影到 3D 棋盘实体；时间轴 valid/invalid 颜色仅消费 Domain 合法性结果，确认后才写入 `LIGHTING`。
- 原有结算闭环保持 seed 731、10 HP -> 0、敌方意图后处理；局外 Godot 流程未修改。
- Unity EditMode 31/31、PlayMode 15/15、12 张集成截图、Windows build 与 Player 冒烟全部通过。

## Wave 02B2A：七卡 Schema 与 earthquake 垂直切片

完成日期：2026-08-01。

- 七张真实 JSON 无损进入 typed effect schema，保留 stable/numeric ID、`front_image`、range、普通 shape 与 clear mask；错误 token 类型显式失败。
- 七张 `1135×1590` 原卡面与 Godot 源哈希一致；两卡手牌实际用 `FrontImage` 加载 lighting 与 earthquake，并保持单选互斥、取消和拖拽事件。
- `earthquake` 使用保存的地图坐标和两格时间轴 shape，结算中心加六邻格；边缘缺失坐标不创建幽灵 tile。
- 七个有效柱各新增两个独立 FBX mesh/renderer/collider block，层距严格 `0.32`；顶面与 occupant anchor 同步上移 `0.64`，四向仍能选择抬高后的同一格。
- Unity EditMode `67/67`、PlayMode `25/25`、11 张集成截图、Windows build 和 Player smoke 全部通过。

## 解耦 R1：可编辑 Scene 与 Prefab

完成日期：2026-08-01。

- 稳定 Camera、灯光、地面、BoardRoot、TargetAnchor、EventSystem、Canvas/HUD、36 格 Timeline、CardHandHost 与 Preview 已序列化到 Scene。
- TimelineCell、CardView、两种 HexBlock、HexColumn 和 TargetView 六个 Prefab 已保存并由 Inspector 引用。
- `BuildSceneGraph()` 仅保留验证/初始化兼容面；Controller 不再运行时创建稳定节点。
- 对称事件绑定覆盖重复初始化和 disable/enable；动态棋盘、目标与 intent 不重复。
- Unity EditMode `70/70`、PlayMode `26/26`、11 张刷新截图、Windows build 和 Player smoke 全部通过。

## 解耦 R2：Application 与 Diagnostics

完成日期：2026-08-01。

- 新增无 Unity 引用的 Application/Diagnostics asmdef，依赖方向为 `Application -> Domain`、`Diagnostics -> Application/Domain`。
- `CombatApplicationSession` 统一组织选卡、typed entity/tile target、preview、commit、cancel、resolve 和唯一初始 enemy intent。
- `ICardCatalog`、`ICombatTraceSink`、不可变会话结果与 collecting/no-op sink 已冻结；diagnostics 异常不影响战斗快照。
- Controller 原公共方法/属性保持，但选中、目标、时间轴和结算顺序已委托给 Application。
- 未注册 Recover/Built/Poison/Clear 不再静默 no-op；失败在玩家 action 占格前返回并记录 trace。
- Application/Diagnostics `14/14`、全量 EditMode `86/86`、PlayMode `26/26`、11 张刷新截图、Windows build 和 Player smoke 全部通过。

## 解耦 R3：Presentation、Infrastructure 与 Composition

完成日期：2026-08-01。

- `CardContentCatalog` 从七份真实 fixture 建立有序内容目录并精确映射 `FrontImage`；data-only effect registry 显式区分已支持与未支持效果，新增普通卡不再要求 Controller 路由改动。
- `CombatCompositionRoot` 统一装配并释放 Application session、catalog、trace sink、Presenter binding 与运行时 sprite；新增 Composition asmdef 后依赖方向保持无环。
- CardHand、BoardRange、Timeline 与 CombatHud 四个 Presenter 只消费 Application view/result，`CombatPresentationBinding` 统一拥有输入订阅；Presentation 不再依赖 Infrastructure。
- Controller 不再解析 JSON、调用 Resources、持有 36 格 Timeline Inspector 列表或按 stable ID 决定视觉；场景 Inspector 引用完整且可编辑。
- trace 新增 effect kind 与 before/after，Unity sink 可关闭；sink 失败不改变战斗结算结果。
- 全量 EditMode `92/92`、PlayMode `31/31`，Windows build `Succeeded`、`206747014` bytes，Player smoke 退出码 0；14 张三视口/四向/时间轴/前后实际渲染证据人工通过。

## Wave 02B2B/02B2C：剩余五卡

完成日期：2026-08-02。

- Recover 沿普通公共路径在 Resolve 重判 runtime ID + `HexCoord`，把目标 10 HP 恢复并钳制到 100；满血、消失和不同 ID 替换保持无副作用。
- Built 在 Resolve 时对空 tile 创建 Neutral Tower occupant（HP/MaxHP 100）；Tower 使用保存的 billboard Prefab，本阶段不提前实现 decay。
- Poison 对存活且支持状态的 occupant 每次累加 2，并产生不可变 before/after；保存的 PoisonStatus Prefab 显示原图标与整数层数，本阶段不提前实现 tick。
- Wind 2×2、Tornado 12×1 使用独立 Clear session；越界拒绝、空清成功，命中后按 action identity 去重并完整移除玩家或敌方 action。
- Application 以交互模式通用路由 ordinary/clear；`VerticalSliceController` 没有 Recover 等 stable-ID 分支。Scene 现保存八个 Prefab 和五个 Presenter 的完整 Inspector 接线。
- Gate A/B/C 小门禁均关闭；Gate D 刷新 full EditMode `152/152`、full PlayMode `38/38`、54 张实际 PNG、Windows build `Succeeded`（`207171486` bytes）和 Player smoke 退出码 0。

## 当前 Unity 战斗切片：简体中文

完成日期：2026-08-02。

- 集中式中文目录覆盖 HUD、目标、生命/中毒、时间轴、清除和七张卡牌显示名，内部 stable ID 与数据契约未改。
- Scene 默认文本和 TimelineCell Prefab 使用 Silver；字体署名、CC BY 4.0 及 10 万美元预算/收入门槛已记录。
- EditMode `161/161`、PlayMode `38/38`，汉化 Harness 生成 8 张 PNG，Windows build `Succeeded`（`210916374` bytes），Player smoke 退出码 0。
- 1280×720、1920×1080、2560×1080 与雷击/台风关键状态逐图检查通过，无方框字、裁切、重叠或宽屏错位。

## Wave 02B3：回合生命周期与战斗交互表现

完成日期：2026-08-02。

- 单一 Application runner 固定 Timeline→building→clear→Poison/status→02B4 hook→intent refresh。
- 确定性 enemy intent 使用 source/target/shape/effect 最终重判；空 command 明确 no-effect。
- Tower 创建周期 100→50、下一周期 Remove；Poison 三 pass 与新感染隔离、死亡事务通过。
- action identity 贯通卡牌、玩家/敌人 frame、地图 range 和详情框双向 hover。
- 七卡响应式状态、两个保存 UI Prefab、Scene host、Silver Text/TextMesh 完成。
- Full EditMode `236/236`、graphical PlayMode `53/53`、12 张人工检查 PNG、Windows build 与 actual Player smoke 通过。

## Wave 02B4：牌库、正式回合与终局

完成日期：2026-08-02。

- 12 张 starter deck 使用固定 seed 与唯一 `CardInstanceId`；稳定卡 ID、实体卡 ID、action identity 保持分离。
- 正式抽弃/回洗路径为 `7/5/0 -> 2/5/5 -> 7/5/0`，phase `1 -> 2 -> 3`，时间币 `0 -> 31 -> 64`。
- 02B3 runner 顺序未重写；battle-flow transaction 只占用保留 hook，并消费 EndTurn 开始时冻结的 occupancy/hand/action snapshots。
- 最大生命 10% 胜利谓词、胜负互斥、输入锁、一次奖励入口/领取和 typed return boundary 完成。
- BattleFlow HUD/结算 Prefab、动态 5 手牌、简体中文与 Silver 完成；18 张三视口/终局 PNG 逐图通过。
- Full EditMode `300/300`、Direct3D12 PlayMode `61/61`、Windows build `Succeeded`（`211133001` bytes）和 actual Player smoke exit 0。
