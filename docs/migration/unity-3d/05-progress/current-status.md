# Unity 3D 迁移当前状态

> 状态：Combat Shell Gate A 已完成并推送；Gate B 正在启动
> 负责人：主智能体
> 最后验证日期：2026-08-02
> 证据来源：评估门禁、共享契约、Godot 基线、Git 状态

## 结论

迁移结论保持 `CONDITIONAL GO`，总体难度 4/5。解耦、剩余五卡、02B3 与 02B4 已关闭：稳定战斗层级保存为可编辑 Scene，十一个实际 Prefab 可由 Inspector 调整，运行时职责已分到 Domain、Application、Presentation、Infrastructure、Diagnostics 与 Composition；局外 Godot 内容没有改动。

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

`00-bootstrap/NEXT_STAGE_DECK_AND_BATTLE_FLOW_PROMPT.md` 的 Gate 0-D 已全部关闭。下一阶段规范为 `NEXT_STAGE_COMBAT_SHELL_AND_SCENE_FLOW_PROMPT.md`，先核验 02B3/02B4 Gate D 再启动。

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

Application typed SceneFlow、launch/outcome/shell state、成功/回滚 phase、sequence 幂等与失败语义已实现。Combat 在 payload bind 前保持 inactive；历史直接加载测试用测试专用 EventSystem。最终门禁为 EditMode `320/320`、Direct3D12 PlayMode `62/62`、build `211736305` bytes 和实际 Player exit 0/marker 一次。Gate B 下一步补 TopHUD 与 3D 背景。

Gate A 功能、测试与结构化证据提交为 `86cdc05`，已推送至 `origin/unity_7.31`；推送后 ahead/behind 为 `0/0`。
