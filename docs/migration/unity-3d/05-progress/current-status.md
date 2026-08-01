# Unity 3D 迁移当前状态

> 状态：解耦 R1/R2/R3 已完成；下一阶段为剩余卡牌
> 负责人：主智能体
> 最后验证日期：2026-08-01
> 证据来源：评估门禁、共享契约、Godot 基线、Git 状态

## 结论

迁移结论保持 `CONDITIONAL GO`，总体难度 4/5。解耦 R1/R2/R3 已关闭：稳定战斗层级保存为可编辑 Scene，六个实际 Prefab 可由 Inspector 调整，运行时职责已分到 Application、Presentation、Infrastructure、Diagnostics 与 Composition；局外 Godot 内容没有改动。

## 当前切片

Slice 01、Wave 02A 和 Wave 02B1 的既有契约继续成立。七张真实 fixture 现在统一解析为 `Damage/Elevation/Recover/Built/Poison/Clear` typed effects，保留 `FrontImage`、range、普通 shape 与 clear mask；官方 Newtonsoft JSON 包只用于 `JsonUtility` 无法可靠完成的 token 类型校验。

两卡手牌使用 JSON `front_image` 实际加载原 `lighting/earthquake` 卡面。`earthquake` 保存地图目标和两格时间轴 shape，结算时对当前存在的中心加六邻格各执行 `+2`：七个有效柱均从一层变为三层，每层是独立 mesh/renderer/collider，local Y 间距严格为 `0.32`；真实顶面和 occupant anchor 均上移 `0.64`，升高后 yaw 0/90/180/270 仍能选择同一格。

Wave 02B2A 历史验收为 Unity EditMode `67/67`、PlayMode `25/25`；Cards 子集 `9/9`、Terrain 子集 `3/3`；Harness 生成 11 张集成截图并成功构建 Windows Player；实际 Player 退出码 0 且日志含 `TIMEKEY_PLAYER_SMOKE_PASS`。这些结果是解耦前的可信基线，R3 最终完成判定使用下文刷新后的全量门禁。

R1 后，Camera/rig、双灯、地面、BoardRoot、TargetAnchor、EventSystem、Canvas/HUD、36 格 Timeline、CardHandHost 和 Preview 均在 Play 前存在；TimelineCell、CardView、草/土 HexBlock、HexColumn 与 TargetView 为保存的 Prefab。Controller 不再创建稳定节点，重复初始化与两轮 disable/enable 不复制棋盘、目标、监听或敌方 intent。

R2 新增纯 C# `TimeKey.Application`/`TimeKey.Diagnostics`。`CombatApplicationSession` 统一拥有选卡、entity/tile target、preview/commit/cancel/resolve、唯一 enemy intent 和结构化失败；Controller 保留旧公共面作为兼容 facade，已不再直接命令 `CardPlaySession` 或 `TimelineGrid.Resolve()`。

R3 已新增 `CardContentCatalog` 与 data-only effect registration catalog，从七份真实 TextAsset 建立完整 hand 和 `FrontImage` 资源路径。`CombatCompositionRoot` 统一拥有 Application session、运行时 sprite 与依赖装配；`CombatPresentationBinding` 统一管理输入订阅，CardHand/BoardRange/Timeline/CombatHud 四个 Presenter 只消费 Application view/result。Controller 不再解析 JSON、加载 Resources、持有 36 格时间轴列表或按 stable ID 分支，`Presentation -> Infrastructure` 依赖已移除。

结构化 trace 现包含 effect kind 与 before/after；可关闭的 Unity sink 即使异常也不改变战斗 snapshot。未注册的 Recover/Built/Poison/Clear 卡牌仍完整显示，但保持不可交互并显式报告 unsupported，不会静默成功。

R3 终验为全量 EditMode `92/92`、PlayMode `31/31`，0 失败、0 跳过；Harness 生成 14 张实际截图并成功构建 Windows Player，build 大小 `206747014` bytes；Player 退出码 0 且日志含 `TIMEKEY_PLAYER_SMOKE_PASS`。人工检查覆盖 1280×720、1920×1080、2560×1080 七卡 hand、lighting 选择/目标、earthquake 四向范围、时间轴 valid/invalid 和升高前后，没有发现裁切、关键遮挡、预览漂移或实体层结构回退。

Git 检查点与远端同步结果以 `push-status.md` 为唯一账本；本文件只记录已通过的功能和验收状态。MIG-012 的 TLS 校验警告仍保留，未修改用户级 Git/GCM 配置。

`00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md` 的 R1/R2/R3 已执行完毕。下一阶段必须完整读取 `NEXT_STAGE_REMAINING_CARDS_PROMPT.md` 后再开始，不得重跑已关闭的 Wave 00/01/02A/02B1/02B2A 或解耦阶段。

## 分支与工作区保护

- 当前集成分支：`unity_7.31`，跟踪 `origin/unity_7.31`。
- 用户原有四个未提交文件保持未暂存：`default_bus_layout.tres`、默认合成配方资源、`color_BG.gdshader`、`game_over.gdshader`。
- 检查点只精确暂存本阶段迁移文档、Unity 代码/素材/测试与结构化证据；原始日志、Library 和 build 不入库。推送状态单独记录在 `push-status.md`。

## 用户决策

当前实现没有产品或环境决策阻塞。用户已决定先完成局内战斗，局外保持原状。旧 Godot CFG 是否兼容、素材发布授权和最终平台在相关波次进入前再决策。
