# Combat Shell Gate 0 verification summary

> 结论：PASS WITH CONCERNS；Gate A 可开始
> 日期：2026-08-02
> Godot：4.6.2 stable，Vulkan Forward+，NVIDIA GeForce RTX 5060 Laptop GPU

## 前置与保护

- 分支：`unity_7.31`；开始时 `HEAD...origin/unity_7.31 = 0/0`。
- Wave 02B3/02B4 Gate D 已关闭；全部历史 Agent 已返回；图形刷新前无 Unity/Godot 写入进程。
- 继承 02B4 的 `300/300` EditMode、Direct3D12 `61/61` PlayMode、18 PNG、Windows build 与 Player smoke，仅限未触及的战斗规则、typed return、Silver、既有顶部资源区和 02B3 lifecycle。
- SceneFlow/Bootstrap/Build Settings/多场景视觉、build 与 Player 证据必须从本阶段重新建立。

## 实际 Godot 图形刷新

| 文件 | 输入/状态 | 人工检查 |
| --- | --- | --- |
| `godot/main-menu-idle-960x540.png` | 冷启动终态，pointer | 中央时钟、六按钮、中文均可读，无裁切 |
| `godot/out-scene-map-initial-960x540.png` | 新游戏，外景初始 | 顶部条、悬挂时钟、海面/六边形地图清晰 |
| `godot/room-confirm-soldier-960x540.png` | pointer 选士兵 | 红色斜切确认层与按钮可读 |
| `godot/out-scene-map-zoom-transition-960x540.png` | 确认后过渡 | 地图聚焦/收拢阶段有效 |
| `godot/in-scene-first-renderable-960x540.png` | 直接载入权威战斗 Scene | 顶部 HUD、总生命、时间轴、地图、手牌和牌堆同时可见 |
| `godot/game-over-direct-load-blank-960x540.png` | 直接载入 GameOver | 灰屏，非有效结算证据；场景依赖完整 defeat 状态 |

Computer Use API 不提供 pointer move，因此本次未刷新 hover；GameOver 直接加载不成立。两项均进入 Gate C/E 真实流程证据清单，不作为 Gate 0 失败或完成证据。

## 审计与冻结

- Source/visual audit：PASS WITH CONCERNS。
- Scene architecture audit：PASS WITH CONCERNS。
- UX verification audit：PASS WITH CONCERNS。
- ADR 0010 冻结 Bootstrap/additive flow、Build 顺序、唯一 EventSystem/Audio/Transition、typed payload/outcome、失败回滚和焦点契约。
- 三份实现 Prompt 路径交集为空，prompt review PASS；Gate A 立即开始。

## 未解决关注项

1. Gate A 必须显式取代历史 Combat EventSystem Scene/test 契约。
2. Combat payload 必须在启用现有 eager `Awake` 内容之前绑定。
3. 历史 direct-load tests/build harness 必须迁移到 Bootstrap/additive 入口。
4. hover、启动关键帧、弹层、Victory/Defeat/return、失败恢复和三视口 Unity 证据仍待后续 Gate 刷新。
