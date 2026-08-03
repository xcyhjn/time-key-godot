# Godot / Unity 等价矩阵

> 状态：Combat Shell Gate E 已关闭；Wave 03 局外地图待执行
> 负责人：主智能体
> 最后验证日期：2026-08-03
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
| 手牌状态 | 正式牌库抽 5；底部卡牌 idle/hover/selected；右键取消 | 12 张 starter deck 按固定 seed 抽出动态 5 手牌；重复 stable ID 以唯一 CardInstanceId 区分，取消恢复预览、action 与镜头输入 | 等价 | Deck EditMode + 02B4 Gate D PNG/PlayMode |
| 交互顺序 | 选卡→世界目标→时间轴格 | 相同语义，可用点击/公共控制器路径完成 | 等价 | PlayMode XML + 截图 |
| 世界范围预览 | `effect_range` axial offset 投影到地图 | 相同 offset 投影到 3D 实体格；四向镜头不改变坐标集合，缺失格不生成幽灵格 | 等价 | `target-range-yaw-*.png` + PlayMode |
| 时间轴合法性预览 | hover/拖放时显示可放置性，确认后才占格 | `CanPlace`/preview 无副作用；valid 绿、invalid 红，Commit 后才写入 | 等价 | `timeline-valid/invalid-1920x1080.png` + EditMode/PlayMode |
| 时间轴 | 12×3，按列再按行 | 相同，冲突/边界受测 | 等价 | EditMode XML |
| 雷击效果 | damage 100 | 10 HP 目标钳制为 0 | 等价 | snapshot 测试 + resolved PNG |
| 地震效果 | 目标中心与六邻格 `elevation +2`；Godot 层高间隔为 `0.32` | 七个有效柱各新增两个独立 mesh/renderer/collider block，顶面与 occupant anchor 上移 `0.64` | 等价 | `evidence/unity-decoupling-r3/earthquake-before-1920x1080.png`、`earthquake-after-1920x1080.png` + PlayMode |
| Recover | `+100`，Resolve 重查当前 occupant，钳制 MaxHP | 稳定 ID+HexCoord 选择与 Resolve 重判；HP=0 可恢复，满血/消失/替换 no-op，10→100 并输出 typed before/after | 等价 | `evidence/remaining-cards-gate-d/recover-*.png` + full XML |
| Built/Tower | 空地创建 Middle Tower，HP100；同结束回合及后续 building 阶段 decay | 空 tile 创建 Neutral Tower HP100；创建周期 100→50，下一周期 50→0 Remove | 等价 | turn-lifecycle Gate D XML/PNG |
| Poison | 活体 +2；回合开始按旧状态传播、伤害、减层 | typed status 累加；三 pass 快照隔离，本周期新感染不伤害或衰减 | 等价 | turn-lifecycle Gate D XML/PNG |
| Wind/Tornado clear | 无地图目标；2x2/12x1 mask；空清；任一格命中整 action 删除 | 独立 clear session；边界先验、identity 去重、玩家/敌人无过滤；红 `!`/蓝 `○`/绿 `HIT` 三态，取消恢复与完整 UI 清除 | 等价 | `evidence/remaining-cards-gate-d/{wind,tornado}-*.png` + full XML |
| 场景与组合边界 | Godot 场景保存稳定节点，脚本在运行时组织玩法 | Unity 稳定层级与十一个 Prefab 可在 Inspector 编辑；`CombatCompositionRoot` 只在组合层装配 session、catalog、presenter 与 trace sink | 允许差异 | 02B4 Gate D + Scene/Prefab EditMode |
| 表现层输入与刷新 | Godot 节点信号驱动卡牌、范围、时间轴和 HUD | `CombatPresentationBinding` 统一订阅输入，五个 Presenter 只消费 Application view/result；Controller 不再加载 JSON/Resources 或按 stable ID 分支 | 允许差异 | `evidence/remaining-cards-gate-d/editmode-results.xml` + PlayMode |
| 结构化诊断 | Godot 以运行日志与截图定位结算 | trace 包含 phase、card、target、timeline、effect kind 与 before/after；Unity sink 可关闭且 sink 异常不改变战斗结果 | 允许差异 | `evidence/unity-decoupling-r3/editmode-results.xml` |
| 敌人意图 | 可见意图；当前 command 解析为空，建筑在时间轴后行动 | 显式 seed/priority/shape/target，执行前重判；空 command 返回 `UnsupportedSourceCommand` no-effect | 等价并显式化 | turn-lifecycle Gate D |
| 回合变化 | 空结束回合推进 phase，按 36 格空位增加时间币并抽新手牌 | 同一 lifecycle hook 使用 pre-clear 占格；实际 phase `1→2→3`、时间币 `0→31→64`、抽弃/回洗 `7/5/0→2/5/5→7/5/0` | 等价 | 02B4 Gate D XML/JSON/PNG |
| 胜负/奖励/返回 | 生命比例触发胜负，可进入奖励与局外返回路径 | 最大生命 10% 胜利规则；胜负互斥、一次 reward entry/claim 与 typed outcome；Victory 返回局外壳、Defeat 进入 GameOver 的生产 SceneFlow 已接入 | 允许差异 | Gate A remediation XML + Player summary |
| 视觉 | 2D 像素/UI；1920 基线且 1280 菜单可读 | 3D 棋盘 + uGUI；三视口 5 手牌、多回合、回洗、Victory/Defeat 与 Silver 均可读 | 允许差异 | `evidence/deck-battle-flow-gate-d/` 的 18 张 PNG + 人工总结 |

判定词只使用：`等价`、`允许差异`、`未实现`、`已知缺陷`、`待验证`。Slice 01、Wave 02A、Wave 02B1-02B4、解耦 R1/R2/R3、Remaining Cards Gate D 与 Combat Shell Gate A 已关闭。完整局外地图仍保持 Godot 权威，Gate B-E 继续补视觉与正式交互。

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
| 回合阶段 | Timeline→building→clear→status→turn resources/draw→intent | 单一 runner 严格同序，命名的 BattleFlow hook 已接入 | 等价 | coordinator/BattleFlow tests + Gate D JSON |
| Enemy intent | priority/shape/target；空 command | 显式 seed、最多 5、最终重判；`UnsupportedSourceCommand` no-effect | 等价并显式化 | intent `23/23` + Gate B PNG |
| Tower | 创建当回合与后续 building 自损 | HP100→50，同下一周期 50→0 Remove | 等价 | Gate D 四 yaw/removed PNG |
| Poison | 全图传播、快照伤害、衰减 | 三 pass；新感染本周期不受伤 | 等价 | processor tests + Gate D summary |
| 行动映射 | 卡牌/时间轴/地图意图共享数据 | 同一 action identity snapshot 双向 hover | 等价 | Gate B mapping PNG + PlayMode |
| 中文与字体 | 玩家可见中文 | 全要素简体中文，Silver Font/Material | 等价 | asset tests + Gate B/D PNG |

## Wave 02B4 最终补充

| 行为 | Godot 可观察语义 | Unity 当前实现 | 判定 | 证据 |
| --- | --- | --- | --- | --- |
| 牌区与回洗 | 12 张 starter、抽 5、弃手、空 deck 洗回 discard | 固定 seed、唯一 CardInstanceId、三堆守恒与确定性回洗 | 等价并显式化 | full EditMode + Player summary |
| 时间币与阶段 | 12x3 空位发币，phase 1..8 后推进 Era | pre-clear occupancy 快照，幂等 ledger，phase/Era rollover 受测 | 等价 | BattleFlow tests + PNG |
| 终局 | 最大生命比例判定，胜负进入不同流程 | 10% 纯 Domain 规则、互斥 typed outcome、终局输入锁 | 等价并显式化 | `BattleVictoryRuleTests` + PlayMode |
| 奖励与返回 | 胜利进入奖励并把战斗状态返回局外 | 一次 reward entry/claim 和 typed return payload 已实现；外部 Scene Flow 留给下一阶段 | 允许差异 | Player/build/return tests |

## Combat Shell Gate 0

| 行为 | Godot 可观察语义 | Unity Gate 0 状态 | 判定 | 后续验收 |
| --- | --- | --- | --- | --- |
| Bootstrap/场景流 | 节点树切换并使用全局 pending payload/outcome | Unity-free typed coordinator + Bootstrap additive runtime；route/payload、提交点原子性、多 run、幂等和真实 bind fault 恢复已实现 | 允许差异 | Gate A remediation `330/330 + 64/64`、build/Player |
| 启动/主菜单 | 约 3 秒钥匙/三字启动；中央时钟、六枚源纹理按钮、设置/种子/退出与 Iris | 保存 Prefab 已实现钥匙/三字、源比例中央钟/按钮、分层菜单入场、typed run-start/settings、设置与输入/焦点；Iris 以持久 cover/reveal 替代 | 允许差异 | Gate C `340/340 + 85/85`、51 张 PNG |
| 局外壳 | 顶部 HUD、悬挂时钟、六边形地图、士兵确认 | 保存的响应式局外 Prefab 复用原六边形地图，显示共享顶部 HUD、单一战斗房间及确认/已结算状态 | 允许差异 | Gate D 15 张局外 PNG + Presenter/asset tests |
| 战斗入场 | 地图波纹 -> HUD/生命/时间轴 -> 手牌解锁 | 保存的 Combat 背景/Top HUD 以显式 0.45 秒 reveal completion 驱动解锁；更完整分层动画留 Gate E | 允许差异 | Gate B entrance frames + SceneFlow tests |
| 胜利/失败 | Victory 横幅后奖励返回；Defeat 进入 GameOver | Victory 必须领取一次奖励后返回同一已结算房间；Defeat 绑定 Silver GameOver 并返回 MainMenu 清理 run | 允许差异 | Gate D production roundtrip `2/2` + 3 张 GameOver PNG |
| 持久所有权 | Godot autoload 管理全局服务 | Bootstrap 唯一 SceneFlow/EventSystem/Audio/Transition；内容 Scene 无副本 | 等价并显式化 | Gate A Scene 结构和多轮往返 |
| 中文与字体 | Godot ark-pixel | Unity 继续全要素简体中文与 Silver Font/Material | 允许差异 | 每 Gate asset tests + 实际截图 |

## Combat Shell Gate B final supplement

| Behavior | Godot observable semantics | Unity implementation | Verdict | Evidence |
| --- | --- | --- | --- | --- |
| Combat Top HUD | Gold/brown top strip, central clock, player/target state and turn resources | Saved responsive Top HUD consumes the existing Application/BattleFlow snapshot; all visible text uses Silver | 允许差异 | three viewport PNGs + PlayMode |
| Combat background | Layered water/map atmosphere remains present while navigating the battle | Saved sea, shallow and four-panel horizon remain continuous for four yaw and camera bounds; old black renderer disabled, collider preserved | 允许差异 | yaw/pitch/zoom PNGs + asset tests |
| Combat entrance | HUD/map/timeline/hand reveal before input becomes available | Saved 0.45s HUD/background reveal implements an explicit SceneFlow completion boundary and input remains locked | 允许差异 | three PlayMode frames + SceneFlow tests |
| Pause/settings | Modal blocks underlying combat and restores prior focus | Scoped input-lock lease, sorting order 500, raycast dim layer, Escape close and focus restoration | 等价并显式化 | modal PNG + PlayMode |
| Existing battle interaction | Card/detail/intent/timeline/map share action identity | Gate B adds no alternate identity or resource state; coexistence remains visible at minimum/reference viewport | 等价 | coexistence PNGs + full regression |

Gate B is closed at `334/334 + 69/69`, including direct Application snapshot-to-TopHUD coverage, Windows build and actual Bootstrap Player smoke. Main menu, formal overworld/reward presentation and full transition orchestration remain Gate C-E work.

## Combat Shell Gate C supplement

| Behavior | Godot observable semantics | Unity implementation | Verdict | Evidence |
| --- | --- | --- | --- | --- |
| GameStart | Approximately 3-second key/three-character motion; current source disables skip | Source key, independent Silver characters, black/gold/black sequence, `allowSkip=false` and explicit completion | 等价 | three real frames + asset/PlayMode tests |
| Main menu composition | Original hex map, central clock and asymmetric left/right buttons | Reuses original BG plus byte-identical clock and six normal/active button texture pairs in a saved responsive Prefab | 等价 | entrance + three viewport PNGs |
| Six commands | New, seed, continue, settings, database and quit | Same six commands; Continue disabled without save, Database shows unavailable notice | 等价并显式化 | component tests + idle/modal PNGs |
| Input states | Pointer/keyboard, arbitrary seed entry, Escape and modal focus | Source active textures, initial focus, fixed Escape priority, scoped overlay lock and focus restore | 等价并显式化 | interaction PNGs + PlayMode |
| Settings | Master/music/SFX sliders and fullscreen | Saved panel applies/persists master/fullscreen; unavailable music/SFX channels are present but explicitly disabled | 允许差异 | settings PNG + asset/PlayMode |
| Transition | Iris/cover blocks input until the target is ready | Persistent cover/reveal completion blocks input and is awaited by SceneFlow; visual shape differs from Iris | 允许差异 | SceneFlow `2/2` + transition tests |

Gate C is closed at `340/340 + 85/85` with 51 manually inspected PNGs. Formal overworld/reward/GameOver visuals and complete success/failure round trips remain Gate D/E work.

## Combat Shell Gate D supplement

| Behavior | Godot observable semantics | Unity implementation | Verdict | Evidence |
| --- | --- | --- | --- | --- |
| Battle room launch | Stable room enters combat with current run/deck/round state | Typed launch factory preserves run, character, room, correlation, battle, deck, Era/phase and timecoins before activation | 等价并显式化 | SceneFlow EditMode + production roundtrip |
| Victory return | Reward is consumed before overworld room settles | Reward button claims authoritative entry once; outcome closes launch and settles the same room once | 等价并显式化 | Gate D Victory roundtrip |
| Defeat return | Defeat enters GameOver, then returns to menu | Typed defeat binds saved GameOver; return clears the old run and reaches MainMenu | 等价 | Gate D Defeat roundtrip + PNG |
| Out-of-battle states | Room supports hover, select, confirm and settled feedback | Saved responsive room covers idle/hover/focus/selected/confirming/settled/disabled and transition lock | 允许差异 | 15 PNG + Presenter `4/4` |
| Identity replay | Scene callbacks must not duplicate outcomes or rooms | Exact outcome replay is idempotent; different/opposite replay conflicts after active launch closes | 等价并显式化 | state-store tests |

Gate D is closed at `343/343 + 92/92` with 18 manually inspected PNGs. Build, actual Player smoke, repeated three-cycle stability, performance and expanded layered animation remain Gate E work.

## Combat Shell Gate E final supplement

| Behavior | Godot observable semantics | Unity implementation | Verdict | Evidence |
| --- | --- | --- | --- | --- |
| Layered scene entrance | Scene elements enter in a readable ordered sequence before interaction | Saved OutOfBattle, Combat and GameOver layers animate with explicit completion and deterministic terminal states | 等价并显式化 | automated timeline `1/1`; 9 reveal PNGs manually passed |
| Repeated battle return | Re-entering combat must not duplicate persistent services or lose run identity | Three typed Victory cycles retain one Bootstrap/one content entry and distinct room/launch/outcome identities | 等价并显式化 | stability `1/1`; actual Player three-cycle summary |
| Final build route | Startup and return paths remain reachable in the shipped scene order | Six enabled Scenes build as Windows Development player with Silver attribution | 等价并显式化 | `build-summary.json` |
| Runtime stability | Repeated route remains interactive and responsive | Player exits unlocked after resize; raw memory grows 412,086 bytes while material and sustained-slope predicates are false | 等价并显式化 | `player-smoke-summary.json`; long profiler review not claimed |
| Out-of-battle ocean | Godot room-selection scene uses the ocean tile without aspect distortion | Unity shell uses the byte-identical tile with responsive square-pixel UV scaling | 等价 | 3 viewport PNGs; source/import SHA-256 match |

Gate E closes with full EditMode `343/343`, full graphical Direct3D12 PlayMode `100/100`, passing targeted tests, a successful six-Scene build, an actual three-cycle Player smoke and 17 manually reviewed PNGs. Gate B four-yaw evidence remains inherited because that combat boundary was unaffected.
