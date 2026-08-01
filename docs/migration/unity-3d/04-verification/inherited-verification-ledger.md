# 解耦阶段继承验收账本

> 阶段：`NEXT_STAGE_DECOUPLING_PROMPT.md`
> 基线：Wave 02B2A 完成态
> 建立日期：2026-08-01
> 规则：证据只能按未受影响的责任边界继承；触及边界后必须在同一波重新验证。

## 阶段进入门禁（历史）

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

## R2 刷新记录

R2 已刷新：Application/Diagnostics `14/14`、全量 EditMode `86/86`、PlayMode `26/26`、11 张截图、Windows build 与 Player smoke 全部通过。`CombatApplicationSession` 成为唯一用例编排入口，Controller 只保留兼容 facade；sink 中立性与程序集依赖方向已重新验证。

## R3 最终刷新记录

R3 已完成并使 Presentation、Infrastructure、Composition、资源映射与 trace 边界的旧证据失效后重建。最终证据位于 `evidence/unity-decoupling-r3/`：

| 门禁 | 最终事实 | 状态 |
| --- | --- | --- |
| 全量 EditMode | `92/92`，0 失败、0 跳过 | 通过 |
| 全量 PlayMode | `31/31`，0 失败、0 跳过 | 通过 |
| Composition/依赖 | `CombatCompositionRoot` 统一装配；Presentation 不依赖 Infrastructure；程序集无环 | 通过 |
| 七卡内容 | `CardContentCatalog` 载入七份真实 fixture 与对应 `FrontImage`；effect registry 显式区分已注册/未注册效果 | 通过 |
| 输入与刷新 | `CombatPresentationBinding` 统一订阅；四个 Presenter 消费 Application view/result；重复绑定不复制监听 | 通过 |
| 诊断 | effect kind 与 before/after 已进入 trace；Unity sink 可关闭且保持中立 | 通过 |
| Harness/build | 14 张 PNG；Windows build `Succeeded`、`206747014` bytes | 通过 |
| Player smoke | 退出码 0，包含 `TIMEKEY_PLAYER_SMOKE_PASS` | 通过 |
| 人工视觉 | 三视口七卡、四向范围、valid/invalid 时间轴、lighting 与 earthquake 前后逐张检查 | 通过 |

最终仍可继承的旧证据只剩未受 Unity 解耦影响的 Godot 权威基线、源素材哈希与历史切片记录。当前 Unity 完成态一律以 R3 全量 XML、harness JSON、Player 日志和 14 张实际渲染 PNG 为准。

## Remaining Cards 接手记录

2026-08-01 接手时 `HEAD` 与 `origin/unity_7.31` 同步在 `da00914`，前置所有权全部交回；无 Unity Editor 写入进程后运行最小冒烟，EditMode `24/24`、PlayMode `10/10`，0 失败。三份只读审计确认五卡 JSON/卡图哈希、Godot 语义、扩展接口与 Tower/Poison 真源无冲突。

| R3 证据 | 本阶段开始时 | 继承条件 |
| --- | --- | --- |
| 七卡 typed schema 与五张卡图哈希 | 可继承 | fixture、adapter、原图未改 |
| 3D 棋盘、镜头、0.32 层高、四 yaw | 可继承 | Terrain/Camera/Controller/Scene 未改；Gate B/C 后按受影响面复查 |
| `lighting` / `earthquake` Domain | 可继承为回归基线 | handler/result/state 变更后必须刷新完整测试 |
| Scene/Prefab 可编辑性 | 当前可继承 | 新增 Tower/Poison Prefab 和 Scene 引用后必须刷新 Scene tests/视觉 |
| R3 Windows build / Player smoke | Gate A 修改运行程序集前仅作接手基线 | Gate D 必须从最终集成态重建并重跑 |
| R3 七卡 idle 三视口 | 风险参考 | CardHand 未改前可继承；中间卡 selected/drag 仍须补拍 |
| R3 Timeline 普通 valid/invalid | 普通路径回归基线 | Clear preview/Timeline Presenter 改动后必须刷新 |

本阶段最终不能用 R3 build/Player 或旧 idle 截图证明新 Recover/Tower/Poison/Clear；只继承未受改动影响的源证据和历史契约。

## Remaining Cards Gate A 刷新记录

Recover 修改了共享 Domain/Application effect 与 occupant 边界，因此该边界的 R3 测试证据已经刷新：全量 EditMode `107/107`、Recover 公共 Scene PlayMode `1/1`，0 失败。启用图形设备的 Editor Harness 生成 5 张 1280x720 状态图并断言目标 10→100；人工确认原卡面、目标反馈、三格 Timeline 和结算 HUD 可读。`VerticalSliceController.cs`、Scene/Prefab、Terrain/Camera 与卡图资源未变，因此 R3 的三视口 idle、四向镜头、0.32 层高和 Scene/Prefab 证据仍按原条件可继承到 Gate B 首次触及这些边界。Gate A 未重建 Player，最终 Build/Player 仍必须在 Gate D 刷新。
