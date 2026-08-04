# Unity 3D 迁移路线图

> 状态：Wave 03 Overworld Gate A-D 已完成；当前阶段为 Wave 04 内容、反馈、音频与教程
> 负责人：主智能体
> 最后验证日期：2026-08-04
> 证据来源：可行性报告、依赖矩阵、风险登记、共享契约

## 波次

| 波次 | 结果 | 进入条件 | 完成证据 |
| --- | --- | --- | --- |
| 00 评估 | Godot 基线、难度、3D 边界、风险 | 分支与工具可用 | 评估文档、日志、截图 |
| 01 战斗垂直切片 | 真实卡牌→时间轴→结算→3D 目标 | 共享契约冻结 | EditMode、PlayMode、build、双视口截图 |
| 02A 局内 3D 棋盘 | 360° 轨道镜头、实体堆叠、稳定选格、原美术语言 | Wave 01 无架构阻断 | 4 向截图、PlayMode、FBX 几何证据 |
| 02B 战斗规则扩展 | 全卡牌效果、形状、敌人/建筑、胜负 | 02A 棋盘空间契约稳定 | parity fixtures、战斗闭环 |
| 03 局外闭环 | 单一地图 authority、schema 2 Continue、多房间/Boss/Defeat/重启 | Wave 02B4 与 SceneFlow 已关闭 | `446/446 + 130/130`、两段 Player、Gate D summary |
| 04 内容与体验 | 教程、音频、战斗反馈、地图/UI 产品化 | Gate 0 资产/typed 契约冻结 | Gate A-E 定向/全量回归、三视口/resize、Player |
| 05 发布加固 | 存档升级、平台、构建与回归 | 目标平台冻结 | 发布构建、回归矩阵 |

Wave 00、Wave 01、Wave 02A、Wave 02B1、Wave 02B2A、Wave 02B2B/C、Wave 02B3/4、Combat Shell A-E 与 Wave 03 Overworld Gate A-D 已关闭。Wave 04 以本地 `wave-04-gate-0/intake.md`、`asset-license-ledger.md` 和 `integration-contracts-wave-04.md` 为入口；Audio/Feedback/Tutorial 仍只能消费既有 typed result/trace/snapshot，不能建立第二套 Domain、SceneFlow、save、Theme 或 identity。MIG-002/MIG-003/MIG-005 继续透明记录。

## Wave 01 依赖顺序

1. 工程/包锁与 batchmode harness。
2. Domain 规则与真实 `lighting` fixture。
3. 3D 战场、uGUI 输入和领域适配。
4. PlayMode、截图、构建与 parity 审查。

只允许第 1、2 项在所有权互斥时并行；Presentation 必须在领域契约可编译后集成。主智能体独占共享文档、工程集成、Git 暂存、提交和推送。

## 重新评估触发器

- 要求强兼容现有 Godot CFG。
- 首发平台不再是 Windows 桌面验证基线。
- 单位改为自由移动或高度不再是整数层，破坏 axial 格语义。
- 需要复制授权不明素材或 vendored 插件内容。
- 首切片证明 uGUI 无法满足卡牌/时间轴交互。

## 2026-08-03 Wave 03 恢复点

P0 Editor-only 工具链、Wave 03P 局外移动/Theme 核心、地图 Domain Gate A 与 Wave 03R EraClock 正式接线已经完成。Wave 03R-F 已关闭局外 reveal Center 中间态与中央房间重叠；EraClock 继续使用单一 typed owner 接入 MainMenu、OutOfBattle 和 CombatTopHUD，并通过全量 `413/413 + 121/121`、六 Scene build 与 D3D12 Bootstrap Player。

Wave 03 的当前入口是 `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md` Gate B：在既有地图 Domain contract 上接入 Application、SceneFlow 和房间流程。不得重新生成 Gate A 模型，也不得建立第二套 Theme、移动或时钟状态机。
