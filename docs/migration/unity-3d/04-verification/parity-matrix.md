# Godot / Unity 等价矩阵

> 状态：Wave 02B3 Gate D 已验证
> 负责人：主智能体
> 最后验证日期：2026-08-02
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
| 七张卡面 | 七张 JSON 的 `front_image` 指向对应原卡面 | `CardContentCatalog` 从七份 fixture 建立有序内容目录，并按 `Art/Battle/Cards/<file stem>` 加载七张原卡面 | 等价 | `evidence/remaining-cards-gate-d/seven-card-hand-*.png` + EditMode |
| 手牌状态 | 底部卡牌 idle/hover/selected；右键取消 | 七卡完整进入 hand 并进入 ordinary/clear 正确交互模式；取消恢复预览、action 与镜头输入 | 等价 | Gate A/B/C PNG + PlayMode |
| 交互顺序 | 选卡→世界目标→时间轴格 | 相同语义，可用点击/公共控制器路径完成 | 等价 | PlayMode XML + 截图 |
| 世界范围预览 | `effect_range` axial offset 投影到地图 | 相同 offset 投影到 3D 实体格；四向镜头不改变坐标集合，缺失格不生成幽灵格 | 等价 | `target-range-yaw-*.png` + PlayMode |
| 时间轴合法性预览 | hover/拖放时显示可放置性，确认后才占格 | `CanPlace`/preview 无副作用；valid 绿、invalid 红，Commit 后才写入 | 等价 | `timeline-valid/invalid-1920x1080.png` + EditMode/PlayMode |
| 时间轴 | 12×3，按列再按行 | 相同，冲突/边界受测 | 等价 | EditMode XML |
| 雷击效果 | damage 100 | 10 HP 目标钳制为 0 | 等价 | snapshot 测试 + resolved PNG |
| 地震效果 | 目标中心与六邻格 `elevation +2`；Godot 层高间隔为 `0.32` | 七个有效柱各新增两个独立 mesh/renderer/collider block，顶面与 occupant anchor 上移 `0.64` | 等价 | `evidence/unity-decoupling-r3/earthquake-before-1920x1080.png`、`earthquake-after-1920x1080.png` + PlayMode |
| Recover | `+100`，Resolve 重查当前 occupant，钳制 MaxHP | 稳定 ID+HexCoord 选择与 Resolve 重判；HP=0 可恢复，满血/消失/替换 no-op，10→100 并输出 typed before/after | 等价 | `evidence/remaining-cards-gate-d/recover-*.png` + full XML |
| Built/Tower | 空地创建 Middle Tower，HP100；同结束回合 decay 属于下一波 | 空 tile 选择与 Resolve 重判，创建 Neutral Tower HP100；原图 billboard Prefab、无 collider；本阶段不 decay | 等价 | `evidence/remaining-cards-gate-d/tower-*.png` + full XML |
| Poison | 活体 +2，无上限累加；回合开始 tick 属于下一波 | 活体+status 重判，checked `0→2→4` typed snapshot；原 poison icon+整数层数；本阶段不 tick | 等价 | `evidence/remaining-cards-gate-d/poison-*.png` + full XML |
| Wind/Tornado clear | 无地图目标；2x2/12x1 mask；空清；任一格命中整 action 删除 | 独立 clear session；边界先验、identity 去重、玩家/敌人无过滤；红 `!`/蓝 `○`/绿 `HIT` 三态，取消恢复与完整 UI 清除 | 等价 | `evidence/remaining-cards-gate-d/{wind,tornado}-*.png` + full XML |
| 场景与组合边界 | Godot 场景保存稳定节点，脚本在运行时组织玩法 | Unity 稳定层级与十个 Prefab 可在 Inspector 编辑；`CombatCompositionRoot` 只在组合层装配 session、catalog、presenter 与 trace sink | 允许差异 | turn-lifecycle Gate D + Scene/Prefab EditMode |
| 表现层输入与刷新 | Godot 节点信号驱动卡牌、范围、时间轴和 HUD | `CombatPresentationBinding` 统一订阅输入，五个 Presenter 只消费 Application view/result；Controller 不再加载 JSON/Resources 或按 stable ID 分支 | 允许差异 | `evidence/remaining-cards-gate-d/editmode-results.xml` + PlayMode |
| 结构化诊断 | Godot 以运行日志与截图定位结算 | trace 包含 phase、card、target、timeline、effect kind 与 before/after；Unity sink 可关闭且 sink 异常不改变战斗结果 | 允许差异 | `evidence/unity-decoupling-r3/editmode-results.xml` |
| 敌人意图 | 可见占位；当前命令解析为空，建筑在时间轴后行动 | 固定意图可见，并在玩家 action 后记录“已处理”，不新增伤害 | 允许差异 | contract + snapshot 测试 |
| 回合变化 | 空结束回合 1/8→2/8，时间币变化 | 非首切片目标 | 未实现 | `battle-turn-2.png` |
| 胜负/奖励/返回 | 可进入奖励与局外返回路径 | 非首切片目标 | 未实现 | source map |
| 视觉 | 2D 像素/UI；1920 基线且 1280 菜单可读 | Blender 低多边形地块 + 原建筑 billboard + uGUI；1280/1920/2560 七卡、四向范围/Tower/Poison 与七卡结算前后均可读 | 允许差异 | `evidence/remaining-cards-gate-d/` 的 54 张 PNG + 人工总结 |

判定词只使用：`等价`、`允许差异`、`未实现`、`已知缺陷`、`待验证`。Slice 01、Wave 02A、Wave 02B1、Wave 02B2A、解耦 R1/R2/R3 与 Remaining Cards Gate D 已关闭。下一阶段为 02B3：固定回合生命周期、Tower decay、Poison tick 与敌方/建筑行动；牌库/回合资源/胜负留给 02B4，局外保持原状。

## Turn Lifecycle Gate 0 冻结

| 行为 | Godot 基线 | Unity Gate 0 状态 | 判定 | Gate A/后续验收 |
| --- | --- | --- | --- | --- |
| 回合阶段 | 结束回合后 timeline、建筑、清理、状态、02B4 no-op、意图刷新顺序固定 | 契约已冻结，Runner 待 Gate A 接入 | 待验证 | phase history + input lock + full regression |
| 首次启动 | 只运行 status/no-op/intent 尾段后解锁 | 契约已冻结 | 待验证 | 不得执行 timeline/build/turn advance |
| Action identity | 卡牌、敌人、Timeline、地图共享同一 action 身份 | 当前缺稳定 ActionId/source snapshot | 未实现 | preview 到 clear 全链同 ID，双向映射 |
| Timeline 排序 | 12×3 按列后按行，多格 action 去重 | 当前一次性 resolve/clear，需拆阶段 | 待验证 | x 后 y、玩家/敌人统一、失败原子性 |
| 敌人命令 | 当前 source command 为空，只显示意图 | 固定占位意图存在，尚无结构化 unsupported | 待验证 | `UnsupportedSourceCommand`，不得伪造伤害 |
| Tower 生命周期 | 同结束回合 100→50；下次 50→0 后 Remove | 尚未实现 decay | 未实现 | 两回合、占用原子清理、表现同步 |
| Poison tick | 旧状态三遍：传播聚合、旧 source 伤害、旧 source 衰减 | 尚未实现 tick | 未实现 | 新感染本轮不伤害/不衰减，顺序无关 |
| 死亡策略 | generic enemy RemainBroken；Tower/Radar underling Remove | generic enemy 已保持破损；typed remove 待实现 | 待验证 | Domain 结果和地图占用一致 |
| 交互框与映射 | 卡牌详情、玩家 action、敌人意图及地图/Timeline 联动 | 现有 card/timeline cell 基础表现，无统一映射 | 未实现 | 三视口实际截图 + PlayMode 双向高亮/清理 |

本节是 02B3 的 Gate 0 冻结状态，不覆盖上表已经关闭的前置功能。实现状态只允许在相应 Gate 的代码、自动化与实际渲染证据全部通过后更新。

Gate A 已关闭纯编排与共享 identity 基础：Runner、x 后 y plan、ActionId 去重、preview/commit/resolve/clear 传播和不可变 presentation snapshot 已通过 full EditMode `183/183` 与 full PlayMode `38/38`。因现有战斗入口尚未把 Tower/Poison/intent processor 接到 Runner，回合阶段、敌方命令和状态行为的矩阵判定仍保持“待验证/未实现”；卡牌/敌人/Timeline/地图的可见双向映射也必须等待 Gate B 实际渲染证据。

## Wave 02B3 最终补充

| 行为 | Godot 可观察语义 | Unity 当前实现 | 判定 | 证据 |
| --- | --- | --- | --- | --- |
| 回合阶段 | Timeline→building→clear→status→intent | 单一 runner 严格同序，02B4 hook 当前 no-op | 等价 | coordinator tests + Gate D JSON |
| Enemy intent | priority/shape/target；空 command | 显式 seed、最多 5、最终重判；`UnsupportedSourceCommand` no-effect | 等价并显式化 | intent `23/23` + Gate B PNG |
| Tower | 创建当回合与后续 building 自损 | HP100→50，同下一周期 50→0 Remove | 等价 | Gate D 四 yaw/removed PNG |
| Poison | 全图传播、快照伤害、衰减 | 三 pass；新感染本周期不受伤 | 等价 | processor tests + Gate D summary |
| 行动映射 | 卡牌/时间轴/地图意图共享数据 | 同一 action identity snapshot 双向 hover | 等价 | Gate B mapping PNG + PlayMode |
| 中文与字体 | 玩家可见中文 | 全要素简体中文，Silver Font/Material | 等价 | asset tests + Gate B/D PNG |
