# Godot / Unity 等价矩阵

> 状态：Wave 02B2A 与解耦 R1/R2/R3 已验证
> 负责人：主智能体
> 最后验证日期：2026-08-01
> 证据来源：Godot 实跑截图/日志、玩法等价契约、Unity 全量测试、构建、Player smoke 与实际截图

| 行为 | Godot 基线 | Unity 当前实现 | 判定 | 证据 |
| --- | --- | --- | --- | --- |
| 启动/场景加载 | 主场景 headless 退出码 0；GUI 可进入战斗 | batchmode 导入、scene validate、Player build/启动均为 0 | 等价 | Godot logs；`evidence/unity-slice-01/verification-summary.md` |
| 六边形战场 | 2D axial 地图，目标可选择 | 3D flat-top axial XZ，19 格且四向可选 | 允许差异 | `evidence/unity-slice-02-board/board-yaw-*.png` |
| 高度 | 2D 绘制偏移表现高度 | 每层 0.32 的独立 FBX mesh 与 collider 实体堆叠 | 允许差异 | PlayMode + `hex-tile-agent/two-layer-stack.png` |
| 镜头检视 | 局内主要缩放，无 3D 环绕 | 透视 360° 旋转、俯仰、缩放、平移 | 允许差异 | 0/90/180/270 PNG + PlayMode |
| 卡牌数据 | `lighting.json`，稳定 ID `lighting` | 原样 fixture，SHA-256 相同 | 等价 | hash + EditMode XML |
| 七卡 schema | 七份 JSON 使用 number/string 异构 effect value，并由 `front_image` 指向原图 | 七份 fixture 解析为 Damage/Elevation/Recover/Built/Poison/Clear typed effect；原文件名映射保留 | 等价 | `wave-02b2a-integration/editmode-results.xml` + ADR-0003 |
| 卡牌素材 | `lighting.png` 与 `behide.png` 原卡面/牌背 | 原文件逐字节复制；运行时保持 `1135×1590` 卡面比例 | 等价 | `evidence/wave-02b-card-art-agent/manifest.md` + PlayMode |
| 七张卡面 | 七张 JSON 的 `front_image` 指向对应原卡面 | `CardContentCatalog` 从七份 fixture 建立有序内容目录，并按 `Art/Battle/Cards/<file stem>` 加载七张原卡面 | 等价 | `evidence/unity-decoupling-r3/seven-card-hand-*.png` + EditMode |
| 手牌状态 | 底部卡牌 idle/hover/selected；右键取消 | 七卡完整进入 hand；`lighting`/`earthquake` 可交互，尚未注册的 typed effect 保持可见但禁用；取消语义不回退 | 等价 | `evidence/unity-decoupling-r3/seven-card-hand-*.png` + PlayMode |
| 交互顺序 | 选卡→世界目标→时间轴格 | 相同语义，可用点击/公共控制器路径完成 | 等价 | PlayMode XML + 截图 |
| 世界范围预览 | `effect_range` axial offset 投影到地图 | 相同 offset 投影到 3D 实体格；四向镜头不改变坐标集合，缺失格不生成幽灵格 | 等价 | `target-range-yaw-*.png` + PlayMode |
| 时间轴合法性预览 | hover/拖放时显示可放置性，确认后才占格 | `CanPlace`/preview 无副作用；valid 绿、invalid 红，Commit 后才写入 | 等价 | `timeline-valid/invalid-1920x1080.png` + EditMode/PlayMode |
| 时间轴 | 12×3，按列再按行 | 相同，冲突/边界受测 | 等价 | EditMode XML |
| 雷击效果 | damage 100 | 10 HP 目标钳制为 0 | 等价 | snapshot 测试 + resolved PNG |
| 地震效果 | 目标中心与六邻格 `elevation +2`；Godot 层高间隔为 `0.32` | 七个有效柱各新增两个独立 mesh/renderer/collider block，顶面与 occupant anchor 上移 `0.64` | 等价 | `evidence/unity-decoupling-r3/earthquake-before-1920x1080.png`、`earthquake-after-1920x1080.png` + PlayMode |
| Recover | `+100`，Resolve 重查当前 occupant，钳制 MaxHP | Gate A 实现中；冻结为稳定 ID+HexCoord 重判和 typed before/after | 待验证 | `agents/reports/remaining-cards-source-semantics.md` |
| Built/Tower | 空地创建 Middle Tower，HP100；同结束回合 decay 属于下一波 | Gate B 待实现原 Tower billboard Prefab 与 Neutral HP100 occupant；本阶段不 decay | 待验证 | `agents/reports/remaining-cards-asset-ui-audit.md` |
| Poison | 活体 +2，无上限累加；回合开始 tick 属于下一波 | Gate B 待实现 typed stacks 与原 poison icon+层数；本阶段不 tick | 待验证 | `agents/reports/remaining-cards-source-semantics.md` |
| Wind/Tornado clear | 无地图目标；2x2/12x1 mask；空清；任一格命中整 action 删除 | Gate C 待实现独立 clear session、identity 去重和三态预览 | 待验证 | `agents/reports/remaining-cards-unity-extension-audit.md` |
| 场景与组合边界 | Godot 场景保存稳定节点，脚本在运行时组织玩法 | Unity 稳定层级与六个 Prefab 可在 Inspector 编辑；`CombatCompositionRoot` 只在组合层装配 session、catalog、presenter 与 trace sink | 允许差异 | `evidence/unity-decoupling-r3/harness-summary.json` + Scene/Prefab EditMode |
| 表现层输入与刷新 | Godot 节点信号驱动卡牌、范围、时间轴和 HUD | `CombatPresentationBinding` 统一订阅输入，四个 Presenter 只消费 Application view/result；Controller 不再加载 JSON/Resources 或按 stable ID 分支 | 允许差异 | `evidence/unity-decoupling-r3/editmode-results.xml` + PlayMode |
| 结构化诊断 | Godot 以运行日志与截图定位结算 | trace 包含 phase、card、target、timeline、effect kind 与 before/after；Unity sink 可关闭且 sink 异常不改变战斗结果 | 允许差异 | `evidence/unity-decoupling-r3/editmode-results.xml` |
| 敌人意图 | 可见占位；当前命令解析为空，建筑在时间轴后行动 | 固定意图可见，并在玩家 action 后记录“已处理”，不新增伤害 | 允许差异 | contract + snapshot 测试 |
| 回合变化 | 空结束回合 1/8→2/8，时间币变化 | 非首切片目标 | 未实现 | `battle-turn-2.png` |
| 胜负/奖励/返回 | 可进入奖励与局外返回路径 | 非首切片目标 | 未实现 | source map |
| 视觉 | 2D 像素/UI；1920 基线且 1280 菜单可读 | Blender 低多边形地块 + 原建筑 billboard + uGUI；1280/1920/2560 七卡、四向范围和结算前后均可读 | 允许差异 | `evidence/unity-decoupling-r3/` 的 14 张 PNG + 3 张 Blender PNG |

判定词只使用：`等价`、`允许差异`、`未实现`、`已知缺陷`、`待验证`。Slice 01、Wave 02A、Wave 02B1、Wave 02B2A 与解耦 R1/R2/R3 已关闭；Remaining Cards Gate 0 已通过，Gate A 实现中。Tower decay、Poison tick 与敌方/建筑行动留给 02B3，牌库/回合资源/胜负留给 02B4，局外保持原状。
