# Unity 3D 迁移当前状态

> 状态：Wave 03 Overworld Gate A-D 已关闭；Wave 04 Gate 0 已冻结，等待 Gate A
> 负责人：主智能体
> 最后验证日期：2026-08-04
> 证据来源：评估门禁、共享契约、Godot 基线、Git 状态

## Wave 04 Gate 0 intake

Wave 03 Overworld Gate A-D 已从权威 XML/JSON/PNG/build/Player 证据重新复核并继承；没有重新实现
任何 Wave 03 Gate A-D。当前分支、进程、锁、dirty/untracked inventory、目标文件 hash、包/asmdef
状态和资产许可分类记录于 `04-verification/evidence/wave-04-gate-0/intake.md`。

Gate 0 已冻结 `AudioCue`、`CombatFeedbackEvent` 和 `TutorialStep` typed contract；三个 Agent Prompt
已写入 `03-workstreams/agents/prompts/` 并通过互斥路径审查。Unity Hub/许可进程已按用户要求终止，
当前没有 Unity/Godot/Player/Agent 写入者。下一步是运行 Gate 0 定向红测并保存独立 XML，再串行
审查 Audio/Feedback/Tutorial 回报后由主智能体集成正式 Scene/Prefab 和设置。

## 结论

迁移结论保持 `CONDITIONAL GO`，总体难度 4/5。解耦、剩余五卡、02B3 与 02B4 已关闭：稳定战斗层级保存为可编辑 Scene，十一个实际 Prefab 可由 Inspector 调整，运行时职责已分到 Domain、Application、Presentation、Infrastructure、Diagnostics 与 Composition；局外 Godot 内容没有改动。

当前队列置顶为 `00-bootstrap/NEXT_STAGE_PLUGINIZATION_AND_ASSET_TOOLING_PROMPT.md`。该阶段只在当前写入任务完成检查点并交回所有权后进入实现；它优先建立 Editor-only 场景/Prefab 校验、卡牌/资产导入审计和联网候选登记，再由主智能体串行决定是否安装任何新包。现有解耦、剩余卡牌和局外地图 Prompt 的功能顺序不变。

## 当前切片

Slice 01、Wave 02A 和 Wave 02B1 的既有契约继续成立。七张真实 fixture 现在统一解析为 `Damage/Elevation/Recover/Built/Poison/Clear` typed effects，保留 `FrontImage`、range、普通 shape 与 clear mask；官方 Newtonsoft JSON 包只用于 `JsonUtility` 无法可靠完成的 token 类型校验。

七卡手牌使用 JSON `front_image` 实际加载原卡面。普通时间轴现支持 Damage、Elevation、Recover、Built 与 Poison：Recover 在 Resolve 重判稳定 occupant 后把 10 HP 钳制到 100；Tower 在空 tile 创建 Neutral HP100 occupant；Poison 对活体状态目标累加 2。Wind/Tornado 进入独立 `TimelineClearSession`，按 2×2/12×1 typed mask 空清或完整移除被命中的 action，不选地图目标、不创建普通 action。

Wave 02B2A 历史验收为 Unity EditMode `67/67`、PlayMode `25/25`；Cards 子集 `9/9`、Terrain 子集 `3/3`；Harness 生成 11 张集成截图并成功构建 Windows Player；实际 Player 退出码 0 且日志含 `TIMEKEY_PLAYER_SMOKE_PASS`。这些结果是解耦前的可信基线，R3 最终完成判定使用下文刷新后的全量门禁。

R1 后，Camera/rig、双灯、地面、BoardRoot、TargetAnchor、EventSystem、Canvas/HUD、36 格 Timeline、CardHandHost 和普通/Clear Preview 均在 Play 前存在；TimelineCell、CardView、草/土 HexBlock、HexColumn、TargetView、Tower 与 PoisonStatus 为八个保存 Prefab。Controller 不再创建稳定节点，重复初始化与两轮 disable/enable 不复制棋盘、目标、监听或敌方 intent。

R2 新增纯 C# `TimeKey.Application`/`TimeKey.Diagnostics`。`CombatApplicationSession` 统一拥有选卡、entity/tile target、preview/commit/cancel/resolve、唯一 enemy intent 和结构化失败；Controller 保留旧公共面作为兼容 facade，已不再直接命令 `CardPlaySession` 或 `TimelineGrid.Resolve()`。

R3 已新增 `CardContentCatalog` 与 data-only effect registration catalog，从七份真实 TextAsset 建立完整 hand 和 `FrontImage` 资源路径。`CombatCompositionRoot` 统一拥有 Application session、运行时 sprite 与依赖装配；`CombatPresentationBinding` 统一管理输入订阅，CardHand/BoardRange/Timeline/CombatHud/CombatOccupant 五个 Presenter 只消费 Application view/result。Controller 不再解析 JSON、加载 Resources、持有 36 格时间轴列表或按 stable ID 分支，`Presentation -> Infrastructure` 依赖已移除。

结构化 trace 现包含 effect kind 与 before/after；可关闭的 Unity sink 即使异常也不改变战斗 snapshot。六种 kind 均已登记：五个普通 handler 加一个独立 Clear session；新增未知效果仍会在改变状态前显式报告 unsupported，不会静默成功。

Gate D 终验为全量 EditMode `152/152`、PlayMode `38/38`，0 失败、0 跳过；Harness 生成 54 张实际截图并成功构建 Windows Player，build 大小 `207171486` bytes；Player 退出码 0 且日志含 `TIMEKEY_PLAYER_SMOKE_PASS`。人工检查覆盖 1280×720、1920×1080、2560×1080 七卡 hand，lighting/earthquake 四向目标与范围，Tower/Poison 四向绑定，以及 Wind/Tornado 越界、空清、命中、取消和提交后的残留，没有发现关键裁切、遮挡、预览漂移或 occupant 错位。最终证据位于 `../04-verification/evidence/remaining-cards-gate-d/`。

当前 Unity 战斗切片玩家可见文本已统一为简体中文，并使用 Silver 像素字体；stable ID、数据字段和开发者日志保持不变。刷新后的 EditMode 为 `161/161`、PlayMode 为 `38/38`，汉化 Harness 生成 8 张实际截图并成功构建 Windows Player（`210916374` bytes），Player smoke 退出码 0。三视口及雷击/台风关键状态已人工确认无缺字、裁切、重叠或宽屏错位，证据位于 `../04-verification/evidence/simplified-chinese-localization/`。

Wave 02B3 已在 Gate A identity/runner 基础上完成 B-D 集成；02B4 只通过保留 hook 加入牌区、回合资源和终局。当前完成判定使用 `300/300 + 61/61` 及 02B4 Gate D build/Player/视觉证据，早期结果只保留为历史局部门禁。

Git 检查点与远端同步结果以 `push-status.md` 为唯一账本；本文件只记录已通过的功能和验收状态。MIG-012 的 TLS 校验警告仍保留，未修改用户级 Git/GCM 配置。

`00-bootstrap/NEXT_STAGE_DECK_AND_BATTLE_FLOW_PROMPT.md` 的 Gate 0-D 已全部关闭。当前唯一阶段规范为 `NEXT_STAGE_COMBAT_SHELL_AND_SCENE_FLOW_PROMPT.md`；其 Gate 0-C 已关闭，Gate D 下一步实现正式局外壳、奖励与 GameOver 往返。

## 分支与工作区保护

- 当前集成分支：`unity_7.31`，跟踪 `origin/unity_7.31`。
- 用户与前置阶段的既有未提交文件继续保持未暂存；完整保护清单以阶段 Prompt 和继承账本为准。
- 检查点只精确暂存本阶段迁移文档、Unity 代码/素材/测试与结构化证据；原始日志、Library 和 build 不入库。推送状态单独记录在 `push-status.md`。

## 用户决策

当前实现没有产品或环境决策阻塞。用户已决定先完成局内战斗，局外保持原状。旧 Godot CFG 是否兼容、素材发布授权和最终平台在相关波次进入前再决策。

## Wave 02B3 Gate D 完成态

统一 lifecycle runner 已接入实际 session/composition。Timeline、Tower building、clear、Poison、新回合 hook 和 enemy intent refresh 按固定顺序执行；Tower 创建周期 HP100→50，下一周期移除；Poison 使用全图快照三 pass，新感染不会在同周期受伤。enemy 空 command 是中文可见的 `UnsupportedSourceCommand` no-effect。

七卡在三视口保持可选，选中/hover/drag 响应式缩放；CardEffectFrame、玩家/敌人 TimelineActionFrame、地图 source/target/range 和 tooltip 通过同一 action identity snapshot 双向映射。Scene 保存稳定 host，动态 frame 只从 Prefab 创建。Tower HP 与 Poison 层数字体/材质均为 Silver。

最终门禁为 EditMode `236/236`、graphical PlayMode `53/53`、Windows build `Succeeded`（`211055434` bytes）、Player exit 0/`TIMEKEY_PLAYER_SMOKE_PASS`。实现提交 `e70988c` 已推送至 `origin/unity_7.31`。02B4 入口为 `NEXT_STAGE_DECK_AND_BATTLE_FLOW_PROMPT.md`。

## Wave 02B4 Gate D 完成态

12 张 starter deck 以固定 seed 731 建立唯一 `CardInstanceId`，正式手牌与三堆守恒路径为 `7/5/0 -> 2/5/5 -> 7/5/0`。EndTurn 只使用 pre-clear occupancy/hand/action snapshots，在既有 lifecycle hook 内依次弃手、结算时间币、推进 phase/Era、必要回洗并抽 5；实际 phase 为 `1 -> 2 -> 3`、时间币为 `0 -> 31 -> 64`。

胜利条件为最大生命 10% 的纯 Domain 规则。Victory/Defeat 互斥，终局锁输入，胜利产生一次 reward entry/claim；typed return 携带 12 张 deck snapshot、Era/phase/timecoins、battle tag 与 seed。动态实体卡离手后，既有 action frame 仍使用独立 action identity/display snapshot。

最终门禁为 EditMode `300/300`、Direct3D12 PlayMode `61/61`、18 张逐图复核 PNG、Windows build `Succeeded`（`211133001` bytes）和 actual Player exit 0/marker 一次。证据入口为 `../04-verification/evidence/deck-battle-flow-gate-d/verification-summary.md`。

Gate C `1a98db0` 与 Gate D `bbd040c` 均已推送到 `origin/unity_7.31`；该次推送后 ahead/behind 为 `0/0`。

## Combat Shell Gate A 完成态

Build index 0 现为持久 Bootstrap，后续依次为 GameStart、MainMenu、OutOfBattleShell、CombatVerticalSlice、GameOver。Bootstrap 唯一拥有 SceneFlow、TransitionCanvas、输入/焦点 gate、EventSystem、AudioRoot 与 state store；内容 Scene 各一个 typed entry 且无持久副本。

Application typed SceneFlow、launch/outcome/shell state、成功/回滚 phase、sequence 幂等与失败语义已实现。两轮独立审查整改进一步封闭合法 route/payload 矩阵、battle/room/correlation identity、state store rollback/commit、post-commit 恢复、Bootstrap fault、同会话多 run、captured drag 和 async timeout。最终刷新门禁为 EditMode `330/330`、Direct3D12 PlayMode `64/64`、build `211747089` bytes 和实际 Player exit 0/marker 一次/异常 0。Gate B 下一步补 TopHUD 与 3D 背景。

Gate A 初次功能与证据提交已推送；独立审查整改已形成本地检查点 `9a42a3d`。三次 push 均因 GitHub 443 connect/reset 失败，用户明确指示跳过 push；当前本地相对远端 ahead 1。受保护未提交文件与原始日志继续排除在暂存范围外。

## Combat Shell Gate B complete

正式 Combat Scene 已接入保存的 `CombatTopHUD` 与 `CombatBattleBackground`。Top HUD 直接投影 Application snapshot 的 Era/phase/timecoins、draw/hand/discard、目标 HP 和角色身份；暂停/设置使用独立输入锁 lease 与焦点恢复，不清除 SceneFlow 锁。所有玩家文字继续使用 Silver。

背景由海面、透明浅水和四块远景组成；旧近黑地面只关闭 renderer，board collider 不变。四 yaw、pitch/zoom 边界、三视口和卡牌详情/目标/时间轴/敌意/Top HUD 共存截图已人工检查。入口 reveal 现由 SceneFlow 显式启动并等待 0.45 秒完成后才解锁，真实 PlayMode 初/中/末帧已保存。

最终门禁为 full EditMode `334/334`、CombatShell PlayMode `5/5`、full D3D12 PlayMode `69/69`、post-build Gate B assets `3/3`、Windows build `Succeeded`（`217436478` bytes）和 actual Bootstrap Player exit 0/marker 一次/异常 0。独立复核补充的 Application snapshot→TopHUD 直接测试已关闭。证据入口为 `../04-verification/evidence/combat-shell-gate-b/verification-summary.md`。原背景图片授权仍为 MIG-005 的“本地验证可用、公开发布未放行”；Gate C 下一步实现 GameStart/MainMenu。

用户明确指示 push 不可用时直接跳过。本阶段不再重试 push，所有检查点只保留本地并继续保护既有未提交改动。

## Combat Shell Gate C complete

GameStart 与 MainMenu 已使用保存的响应式 Prefab。GameStart 复用源钥匙图，以 Silver 三字完成冻结的约三秒不可跳过黑/金/黑序列。MainMenu 复用原六边形地图、字节一致时钟和十二张左右不对称按钮状态图，提供新游戏、种子游戏、继续、设置、数据库和退出六项命令。

无存档时继续按钮明确禁用；pointer hover、键盘焦点、pressed 与 disabled 状态可区分。种子接受任意文本；设置面板实际保存并应用主音量/全屏，未接入的 Music/SFX 明确禁用。种子、设置、数据库和退出弹层使用 scoped lease 阻断底层输入并恢复焦点，Presentation 不持有 SceneFlow、payload 或 application 行为。

持久转场遮罩现有真实 cover/reveal completion。SceneFlow 在关闭来源相机前等待 cover，并在解锁输入前同时等待遮罩与内容 reveal；来源 Scene 正常卸载不再取消 post-commit reveal。最终全量门禁为 EditMode `340/340`、D3D12 PlayMode `85/85`；51 张 GameStart、分层入场、三视口、交互与弹层 PNG 已逐图复核。Gate D 下一步；按用户指示不尝试 push。

## Combat Shell Gate D complete

正式局外壳现使用保存的响应式 Prefab、原六边形地图背景、共享顶部 HUD、单一战斗房间与确认/已结算反馈；GameOver 使用保存 Prefab 显示 typed defeat 并返回 MainMenu。全部玩家可见文字使用 Silver。

`RunStartPayload` 现在显式携带角色身份；局外 launch factory 保留 run/room/correlation/battle/deck/Era/phase/timecoins。Combat composition 在内容启用前消费正式 launch，而首个 authoritative outcome 会关闭 active launch；相同重放幂等，不同或相反重放显式拒绝。Victory 领取奖励后只返回并结算同一房间一次，Defeat 进入 GameOver 后清理 run。

最终 Gate D 门禁为 full EditMode `343/343`、full graphical D3D12 PlayMode `92/92`，以及 18 张三视口局外/GameOver PNG 人工复核；失败、跳过和不确定均为 0。Gate E 接续 Windows build、actual Player smoke、连续三轮、性能与扩展动画；按用户指示不尝试 push。

## Combat Shell Gate E complete

OutOfBattle、Combat 与 GameOver 已接入保存的分层 reveal。自动化时间线验证背景/上下文/房间、状态/时间轴/手牌、背景/面板的顺序和最终可交互状态；缺层、零时长、disable、destroy 与重复播放由定向 `7/7` 覆盖。三轮 Victory 稳定性用例最终 `1/1`，验证唯一 Bootstrap、唯一内容 entry、typed room/launch/outcome identity、Scene 卸载、内存预算和 resize 后输入恢复。

六 Scene Windows Development build 已成功，`227249074` bytes，Silver attribution 存在；实际 Player smoke exit 0，日志为 `PASS=1 / PERF=3 / FAIL=0`。三轮 transition 约 `1674 / 1513 / 1513 ms`，最终输入未锁定；三次内存原始样本总增量 `412086` bytes，material monotonic 与 sustained-slope 均为 false。实际渲染 recorder 为 `SetPass Calls Count`，三轮值均为 18；这些短样本不宣称长期无泄漏或 profiler-grade GPU 结论。

局外关卡选择界面已按用户要求改用原 Godot 海洋 tile，并通过 1280x720、1920x1080、2560x1080 响应式复核。最终 full EditMode `343/343`、full graphical D3D12 PlayMode `100/100`；9 张 reveal、3 张海洋三视口和 5 张 Player 路径图共 17 张均逐图通过。Gate B 的四 yaw 证据因 combat camera/background 边界未变化而继续继承。Gate E 已关闭，正式后继为 `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md`；用户已指示 push 不可用时直接跳过，本阶段不尝试 push。

## Wave 02B3R Effect Frame Stability complete

效果框已改为逐占用格填充并只绘制真实外轮廓，Tower 与 Poison 的非矩形缺口不会被根矩形误填；同一 frame 实例在 1280x720、1920x1080、2560x1080 与动态 resize 后会按 layout signature 重新吸附。CardInstanceId、ActionId、stable card id 和地图 runtime id 通过统一 identity index 建立双向映射，重复 stable id 不再造成错误高亮。

空手 idle/Cancelled 地块检查已接入统一清理路径：同格复点、空地、Escape、短右键、卡牌接管、取消、提交、结算、Scene rebind、disable 和全局输入锁均清理；右键拖拽继续只旋转相机。最终 full EditMode `351/351`、graphical D3D12 PlayMode `107/107`、Windows Development build 和实际 Player smoke 全部通过；9 张三视口/动态 resize/non-rect PNG 已逐图复核。Combat Shell Gate A-E 经审查均已关闭，因此没有可继续执行的未完成 Combat Shell Gate，恢复位置保持当前正式后继。当前无用户决策阻塞；按用户指示不执行 push。

## Wave 03R Era Clock formal integration complete

原 Godot `clock_noring/ring/point` 与 Silver 已通过 typed snapshot、adapter、planner、state machine 和单一 Presenter 接入正式 MainMenu、OutOfBattleShell 与 CombatTopHUD。MainMenu 使用 Center；局外等待真实 reveal completion 后进入 HUD；Combat 从 authoritative BattleFlow 投影 HUD。共享 `HudAnchor` 保存于 `TopBar/ClockPlate`，settled Presenter 自动跟随首帧 Canvas/Layout 重排和动态 resize，不持有 input lease。

Wave 03R-F 以正式 Bootstrap 路由红测复现并关闭了局外 reveal Center 中间态时钟与中央房间重叠：共享 Top HUD 的 Center anchor 固定为 `(0.20, 0.74)`、scale `0.50`，不改变 HUD anchor、SceneFlow completion 或唯一 Presenter 状态机。最终定向门禁为 EditMode `17/17`、graphical PlayMode `13/13`、正式三次 Victory 往返 `1/1`；全量为 EditMode `413/413`、graphical PlayMode `121/121`。六 Scene Windows Development build 成功（`227421290` bytes）；D3D12 Player exit 0、PASS 1、FAIL/异常 0，三轮均只有一个 EraClockPresenter，最终输入未锁定。正式五张 Player 图、三视口四阶段与动态 resize 证据均已逐张复核。

P0、Wave 03P 和地图 Domain Gate A 已在当前提交历史中完成。下一恢复点为 `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md` Gate B；不得重新执行地图 Gate A。当前无用户决策阻塞。

## Wave 03 Overworld Gate B complete

Gate A 的单一 `OverworldChapterState` 已接入 `OverworldRunApplication`、既有 typed
SceneFlow、正式 MainMenu/OutOfBattle/Combat/GameOver 路由和 schema 2 原子存档。
`OutOfBattleShellState` 仅作为既有 Presenter 的兼容投影；正式中央入口使用当前
available 的真实 Gate A 节点 identity，不存在第二套地图、节点翻译表或跨 Scene
状态机。

Continue 现在由真实可恢复存档决定 enabled，恢复同一 run/map fingerprint/current
node/resources；schema 0/1 可迁移，损坏/future/I/O failure 有 typed 状态。SceneFlow
在源卸载前完成候选存档，失败会恢复旧地图、旧文件、源 Scene、焦点、遮罩和输入。
最终定向门禁为 EditMode `68/68`，SceneFlow/02B4 EditMode `100/100`，真实 additive
PlayMode `5/5`，全 SceneFlow D3D12 PlayMode `17/17`，Era Clock `17/17 + 13/13`。
证据入口为 `../04-verification/evidence/overworld-map-gate-b/verification-summary.md`。

下一恢复点为 Gate C：正式动态地图 UI、Event/Shop、Continue 错误提示和三视口视觉。
当前无用户决策阻塞。

## Wave 03 Overworld Gate C complete

正式局外壳现从唯一 Gate A map snapshot 动态生成节点与连线，使用 exact `MapNodeId`、
layer/slot/room type，并覆盖 Current、Available、Locked、Visited、Settled、Selected、
Confirming、Moving、Arrived 等表现态。顶部 Era Clock、中央地图和底部房间详情带在
1280x720、1920x1080、2560x1080 与 1600x900 动态 resize 中保持分区，无节点重叠或
文字裁切。

Event 在源资源缺失时提供显式安全跳过，不伪造剧情/奖励；Shop 使用确定性七卡 offer，
以 50 时间币完成扣款、deck add、房间结算和 schema 2 存档的原子提交。有效 Continue
恢复同一地图/资源；corrupt/future/I/O failure 保持 typed fail 并显示可恢复 Silver 中文
提示。

最终门禁为 targeted EditMode `39/39`、additive regression `10/10`、visual PlayMode
`2/2`、full graphical EditMode `446/446`、full graphical D3D12 PlayMode `128/128`。
十五张 PNG 已逐图复核；六 Scene Windows build 成功，Player launcher 为 `667648`
bytes、SHA-256 `FE5E81292DF0F6591DCEEC172141B6F0F22D7CBB853DE83786B725E1BC68BEEE`，
Silver attribution 存在。实际可见 Player 已从 MainMenu 进入正式 OutOfBattle，确认
Era Clock、动态地图/连线和底部详情无重叠。证据入口为
`../04-verification/evidence/overworld-map-gate-c/verification-summary.md`。

下一恢复点为 Gate D：多房间路线、Boss/章节推进、失败返回和重启 Continue 的端到端
闭环。当前无用户决策阻塞。

## Wave 03 Overworld Gate D complete

Gate D 已用两个独立可见 Player 进程关闭真实重启边界。第一段在 Event 房结算后写入
schema 2；第二段 fresh Bootstrap 从 Continue 恢复完全相同的 run、seed、map fingerprint、
current node、100 时间币、12 张牌和 settled identity。Continue 在预留 transition 前先
用持久 operation cursor 提升 sequence floor，避免新进程序列重用。

同一 seed 4 路线随后完成 50 时间币 Shop、两次普通战斗和 Boss，购买 `earthquake`
只发生一次，Boss victory 只生成 chapter 2 一次，相同 outcome replay 为 already applied。
chapter 2 的实际 Defeat 进入 GameOver；返回 MainMenu 后 run、save、launch、outcome 和
input lock 全部清除。

最终门禁为 additive `10/10`、full graphical EditMode `446/446`、full graphical D3D12
PlayMode `130/130`。六 Scene Windows build 成功；Player launcher 仍为 `667648` bytes、
SHA-256 `FE5E81292DF0F6591DCEEC172141B6F0F22D7CBB853DE83786B725E1BC68BEEE`，
Silver attribution 已随 Player 放置。prepare/resume 两段 Player 均 exit 0，10 张三视口
Player PNG 已逐张通过。证据入口为
`../04-verification/evidence/overworld-map-gate-d/verification-summary.md`。

Wave 03 Overworld Gate A-D 已关闭；当前主 Prompt 没有剩余地图 Gate，且无用户决策硬
阻塞。
