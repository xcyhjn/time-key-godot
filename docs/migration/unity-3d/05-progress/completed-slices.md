# 已完成切片

> 状态：截至 Combat Shell Gate E 已完成
> 负责人：主智能体
> 最后验证日期：2026-08-03
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

## Combat Shell Gate A：Bootstrap 与无视觉多场景往返

完成日期：2026-08-02。

- Unity-free typed SceneFlow、Combat launch/outcome 与局外一次消费状态完成。
- 六 Scene Build Settings、Bootstrap 唯一持久服务和五内容 Scene entry 完成。
- Combat 自有 EventSystem 已移除，内容根改为 bind-before-enable；旧测试迁移到测试专用 EventSystem。
- 成功链、Busy/幂等/conflict/stale、合法 route/payload 矩阵、提交前回滚/提交后保留 target、取消与真实 additive Victory/Defeat 往返通过。
- settlement/return/launch battle identity、run/room/launch correlation、state store rollback/commit、Bootstrap fault 恢复和直接轮询输入锁均有回归测试。
- Full EditMode `330/330`、Direct3D12 PlayMode `64/64`、Windows build `211747089` bytes、actual Player exit 0。

## Combat Shell Gate B：战斗顶部 UI、背景与入场

完成日期：2026-08-02。

- 保存的响应式 Top HUD 投影 Era/phase/timecoins、三牌区、目标 HP 与角色身份；暂停/设置 modal 具备输入锁、最高 UI 排序和焦点恢复。
- 保存的背景 Prefab 使用 Godot 原 BG/title/sea/shallow 图，包含海面、透明浅水和四向远景；源/副本哈希、Importer 与授权状态已记录。
- SceneFlow 等待真实 0.45 秒 Combat reveal 完成后再解锁；初始/中段/完成三帧来自 PlayMode，而非静态 harness 模拟。
- 三视口、四 yaw、pitch/zoom、卡牌/详情/敌意/Timeline/地图/Top HUD 共存和 pause modal 已逐图人工通过。
- Full EditMode `334/334`、CombatShell PlayMode `5/5`、full D3D12 PlayMode `69/69`、post-build assets `3/3`、Windows build `217436478` bytes、actual Bootstrap Player exit 0。

## Combat Shell Gate C：启动、主菜单与转场表现

完成日期：2026-08-03。

- 保存的 GameStart Prefab 复用源钥匙图，实现约 3 秒、不可跳过的 Silver 三字独立移动、黑/金/黑启动表现与显式 completion。
- 保存的 MainMenu Prefab 复用原六边形地图、中央时钟、三份字节一致时钟素材和十二张源按钮状态图，完成新游戏/种子/继续/设置/数据库/退出六项。
- Continue 无存档时明确 disabled；hover/focus/pressed/disabled、任意文本种子、设置/数据库提示、退出确认、ESC/焦点与 scoped 输入锁完成。设置实际保存并应用主音量/全屏，未接入 Music/SFX 明确禁用。
- Presentation 仅发 typed callback；Composition 创建带显式 run seed/state 的 `RunStartPayload`、递增 sequence 的 SceneFlow request，并拥有 application quit。
- 持久转场遮罩以真实 cover/reveal completion 驱动相机关闭和输入解锁，不用固定延时。
- Gate C 最终 EditMode `340/340`、D3D12 PlayMode `85/85`；51 张启动/分层入场/三视口/交互/弹层 PNG 逐图通过。

## Combat Shell Gate D：最小局外壳与完整往返

完成日期：2026-08-03。

- 保存的 OutOfBattleShell/GameOver Prefab 与正式 Scene 完成；局外复用原地图视觉与共享 Top HUD，GameOver 提供 typed defeat 和返回入口，全部中文使用 Silver。
- typed launch 保留 run/character/room/correlation/battle/deck/Era/phase/timecoins；Combat 在激活前使用该状态创建正式 composition。
- Victory 领取一次奖励后返回同一 settled room；Defeat 进入 GameOver 后返回 MainMenu 并清理 run。active launch 单次关闭、相同 outcome 幂等、冲突 outcome 拒绝。
- 三视口覆盖局外 idle/hover/selected/confirming/settled 与 GameOver defeat，共 18 张 PNG，均已人工检查。
- Full EditMode `343/343`、full graphical D3D12 PlayMode `92/92`，失败/跳过/不确定均为 0。

## Combat Shell Gate E：多场景细节与交付

完成日期：2026-08-03。

- OutOfBattle、Combat、GameOver 使用保存的分层 reveal，并以既有 completion boundary 驱动 SceneFlow 解锁；缺层、零时长、disable/destroy/replay 均确定完成。
- 局外关卡选择背景按用户要求改用原 Godot 海洋 tile；Unity 副本字节一致，三视口保持方形像素平铺。
- 三轮正式 Victory 往返保持唯一 Bootstrap/内容 entry、各轮唯一 room/launch/outcome identity、旧 Scene 卸载和最终输入解锁。
- Windows Development build 为六 Scene、`227249074` bytes，Silver attribution 存在；actual Player exit 0，`PASS=1 / PERF=3 / FAIL=0`。
- Post-review entrance `5/5` 与 stability `1/1` 关闭运行中强制完成、旧证据误用、render-counter 误命名和固定小幅内存增长门禁。
- Full EditMode `343/343`、full graphical Direct3D12 PlayMode `100/100`，失败/跳过/不确定均为 0。
- 9 张 reveal、3 张海洋三视口和 5 张 Player 共 17 张 PNG 逐图通过；Gate B 四 yaw 证据因受影响边界未变化而继续继承。

## Wave 02B3R Effect Frame Stability

- Timeline action frame 使用逐格填充和 perimeter edge，支持同实例动态 resize、Scene rebind 与异常后的刷新恢复。
- action identity index 统一 ActionId、精确 CardInstanceId、stable card id、source/target map runtime id；重复 stable id 只允许精确实例或唯一回退。
- idle/Cancelled 地块检查、右键短按/拖拽、卡牌接管和全局输入锁使用同一 overlay priority 与清理边界。
- Full EditMode `351/351`、graphical D3D12 PlayMode `107/107`、Windows Development build、actual Player smoke 均通过。
- 9 张 1280x720、1920x1080、2560x1080 与动态 resize 的 frame/Poison/Tower PNG 已逐图确认无错位、误填、裁切或 Silver 文本可读性问题。
## Wave 03 Overworld Gate B

- 单一 Gate A Domain authority 已接入 Application、typed SceneFlow 和正式壳。
- schema 2 原子 save、schema 0/1 migration、backup recovery 与真实 Continue 已完成。
- Victory/reward/settled/unlock/save 顺序、outcome replay/conflict、Defeat cleanup、
  Boss single advance、focus/input/cover rollback 均有自动化覆盖。
- Gate B 门禁：`68/68`、`100/100`、`5/5`、`17/17`、Era Clock `17/17 + 13/13`。
