# 解耦阶段继承验收账本

> 阶段：`NEXT_STAGE_DECOUPLING_PROMPT.md`
> 基线：Wave 02B2A 完成态
> 建立日期：2026-08-01
> 规则：证据只能按未受影响的责任边界继承；触及边界后必须在同一波重新验证。

## 当前门禁

| 项目 | 事实 | 状态 |
| --- | --- | --- |
| Git | `unity_7.31`，开始时 `HEAD...origin/unity_7.31 = 0/0` | 通过 |
| 前序智能体 | Wave 02B2A 的主任务及五个子任务均已完成并返回 | 通过 |
| Unity 独占 | 开始时无 Unity/UnityHub 进程、无 Editor lock | 通过 |
| 用户改动 | 4 个已修改文件与 2 个未跟踪阶段 Prompt 已列入保护清单 | 保护中 |
| 只读审计 | Domain/Application、Scene/Prefab、扩展维护三份报告均已返回 | 通过 |

## 保护清单

本阶段不得修改、暂存或提交下列既有用户改动：

- `default_bus_layout.tres`
- `scene/in_scene/rewards/resources/default_craft_recipe_book.tres`
- `shaders/color_BG.gdshader`
- `shaders/game_over.gdshader`
- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md`
- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_REMAINING_CARDS_PROMPT.md`

## 可继承证据

| 能力 | 证据 | 继承条件 | 当前状态 |
| --- | --- | --- | --- |
| Godot 权威行为 | `evidence/godot-baseline/**` | Godot 权威文件未变 | 可继承 |
| 七卡 typed schema 与 JSON 解析 | `evidence/wave-02b2a-gate-a/editmode-results.xml`、七卡 manifest/contact sheet | Domain card schema、JSON fixture、CardJsonAdapter 未变 | 可继承至相关边界首次修改 |
| earthquake Domain `+2` 与 `0.32` 规则 | `evidence/wave-02b2a-gate-b/editmode-results.xml` | Timeline/Terrain Domain 未变 | 可继承至 R2 首次修改 |
| 卡面源素材与 front-image 映射 | `evidence/wave-02b2a-card-art-agent/**` | 源图片、fixture front_image、资源映射未变 | 可继承至 R3 资源边界修改 |
| Domain 零 UnityEngine | `TimeKey.Domain.asmdef` 与 Wave 02B2A EditMode | Domain/asmdef 未变 | 可继承；最终仍需静态复核 |

## 必须刷新证据

| 证据 | 失效原因 | 最迟刷新门禁 |
| --- | --- | --- |
| Scene/Prefab 层级与 Inspector 引用 | R1 将稳定对象从运行时生成迁入序列化资产 | R1 checkpoint 前 |
| PlayMode 集成结果 | 初始化、监听和场景对象来源改变 | 每个 R1/R2/R3 checkpoint 前 |
| harness 与 Windows build/player smoke | Composition 与依赖图改变 | 每个波次按影响刷新，最终全量刷新 |
| 所有 Unity 视觉截图 | Scene、Prefab、Presenter 或资源边界改变即失效 | R1 后初刷，R3 后终刷 |
| asmdef 无环与层级依赖 | R2/R3 新增 Application/Diagnostics 和依赖 | R2、R3、最终门禁 |

## 波次失效矩阵

| 波次 | 允许继承 | 当波必须重跑 |
| --- | --- | --- |
| R1 Scene/Prefab | Domain、schema、Godot 基线 | Scene EditMode、完整 PlayMode、harness、build/player、实际截图 |
| R2 Application | 未改资源与卡面映射 | Application EditMode、完整 EditMode/PlayMode、asmdef/Domain 静态检查、harness |
| R3 Presentation/Infrastructure/Diagnostics | Godot 基线 | 全量测试、build/player、三视口、四向 yaw、earthquake 前后、诊断与扩展测试 |

## 强制行为断言

- `lighting` 仍按冻结目标链造成伤害并按合法形状进入 Timeline。
- `earthquake` 范围仍为中心加六邻格；每个有效柱新增两个真实 mesh/renderer/collider 块。
- 两个新增块的中心间距严格为 `0.32`，顶面、单位锚点、范围预览与四向选择同步抬升 `0.64`。
- 稳定层级在进入 Play 前已存在，Inspector 引用完整且人工调整可持久化。
- `BuildSceneGraph()` 仅保留兼容 facade；重复初始化、禁用/启用或重载不得重复节点与监听。
- 未注册 typed effect 必须显式失败，不得静默成功或静默 no-op。

## 证据登记规则

每个 checkpoint 都记录测试总数、失败数、Unity 版本、命令、日志/XML/截图路径、视觉检查结论和 `git status --short`。只引用本账本标记为“可继承”且条件仍成立的旧证据；其余一律生成新证据。

## R1 刷新记录

R1 已刷新：EditMode `70/70`、PlayMode `26/26`、Scene authoring marker、Windows build、Player smoke、11 张截图与人工视觉检查。证据位于 `evidence/decoupling-r1/**` 和 `evidence/unity-decoupling-r1/**`。Domain、七卡 schema 和 Godot 基线在 R1 未修改，继续按本账本条件继承。
