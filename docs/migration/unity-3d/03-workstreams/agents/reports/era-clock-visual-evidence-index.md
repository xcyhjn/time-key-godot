# Wave 03R Era Clock Visual Evidence Index

## Canonical PlayMode evidence

目录：`docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/`

| 文件 | 视口/状态 | 像素 bounds | 人工结论 | SHA-256 |
| --- | --- | --- | --- | --- |
| `center-1280x720.png` | 1280x720 center | 572,204 - 708,453 | 完整可见，比例稳定，标签清晰，无触边/重叠 | `3DEE8093...25DA99` |
| `center-1920x1080.png` | 1920x1080 center | 858,306 - 1063,680 | 完整可见，居中稳定，Silver 文本可读 | `DFC374FB...390C8A` |
| `center-2560x1080.png` | 2560x1080 center | 1162,269 - 1398,702 | 超宽视口无拉伸/裁切，anchor 正确 | `BD357F24...4CAC0F` |
| `dynamic-resize-1600x900.png` | 运行中 resize 后 HUD | 936,696 - 1014,840 | 单实例到达 HUD，顶部有安全边距，无残影 | `C8815FFF...F925B` |
| `rollover-initial-1920x1080.png` | Era 7 / Phase 8 | - | 端点文本、指针和满进度一致 | `97F53000...2801F` |
| `rollover-middle-1920x1080.png` | reset/pulse | - | 指针和进度归零，ring pulse 可见，无旧标签 | `093C8165...C1F1A` |
| `rollover-complete-1920x1080.png` | Era 8 / Phase 1 | - | Phase 1 终态、1/8 progress、pointer 归零一致 | `C551BBC4...4D5BE` |

`visual-evidence-index.json` 记录 renderer、实际尺寸、distinct colors、checksum、raycast、geometry 和 raster margin。四个 geometry frame 均为 `rootBlocksRaycasts=false`、`withinViewport=true`、`rasterHasMargin=true`。

`animation-timeline.json` 是 canonical 结构化时间线：from `7/8#20` 到 `8/1#21`，kind `Rollover`，owner `EraClockPresenter`，Center -> Center，配置时长 2.0 秒，completion `Completed`，cancellation `None`，input lock false。实测 pulse 取样 alpha 大于 0.85，终态 progress 0.125、pointer 0、pulse 0。

## Actual Player evidence

目录：`docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-player/`

| 文件 | 状态 | 人工结论 | SHA-256 |
| --- | --- | --- | --- |
| `player-rollover-initial-1280x720.png` | Era 7 / Phase 8 | 时钟、标签、满进度完整，无遮挡 | `0017AF98...5C294` |
| `player-rollover-pulse-1280x720.png` | reset/pulse | pulse 与归零态可区分，无残影 | `C6DEA546...C19BE` |
| `player-rollover-complete-1280x720.png` | Era 8 / Phase 1 | 新 Era 文本和 Phase 1 终态一致 | `E2C72504...C37A24` |
| `player-hud-terminal-1280x720.png` | HUD terminal | 同一 presenter 缩放并移动到 HUD，边缘/标签未裁切 | `956934F2...D8B88` |

四张 Player 图已逐张实际打开检查，不以退出码或非空像素替代人工结论。Player summary 同时确认单 presenter、三轮隔离 route、异常 0、材质增长 0 和 non-raycast。

## 已知视觉边界

独立 evidence Scene 继续用于隔离 pointer/progress/pulse 和 resize 行为。正式叠放关系已由下列 Bootstrap Player 证据补齐。

## Formal Bootstrap route evidence

目录：`docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/`

| 文件 | 状态 | 人工结论 | SHA-256 |
| --- | --- | --- | --- |
| `player-main-menu-1280x720.png` | MainMenu | 原 Godot 大时钟和按钮共存，进度完整，无输入遮挡 | `72A5992A...28955B` |
| `player-out-of-battle-1280x720.png` | OutOfBattle HUD settled | 时钟位于顶部 `ClockPlate`，不遮挡地图上下文框 | `7CA8A716...E03489` |
| `player-combat-1280x720.png` | Combat HUD settled | 时钟位于顶部 `ClockPlate`，不再覆盖 36 格时间轴 | `9174B427...FFCA1C` |
| `player-victory-1280x720.png` | Victory | 顶部时钟与结算/棋盘层级保持分离 | `C3F34B7A...6D843` |
| `player-returned-shell-2560x1080.png` | 第三次返回局外 | 超宽返回态不下沉、不裁切、不阻断按钮 | `BED04649...14C0F9` |

五张图均逐张打开检查。正式 Player summary 为 Direct3D12、三次 Victory、每轮 `eraClockPresenterCount=1`、异常 0、exit 0；三视口和动态 1600x900 的 EraClock 自身边界继续由上方独立 graphical evidence 覆盖。

## Wave 03R-F reveal overlap evidence

目录：`docs/migration/unity-3d/04-verification/evidence/era-clock-closeout/`

| 文件 | 状态 | 人工结论 | SHA-256 |
| --- | --- | --- | --- |
| `red-formal-middle-overlap-1920x1080.png` | 修复前正式 reveal 中间态 | Era Clock 覆盖中央 `CombatRoom`，作为红证据保留 | `7A461B79...1F7FBF` |
| `formal-middle-1920x1080.png` | 修复后正式 Bootstrap 路由中间态 | 时钟位于左上安全区，与房间、上下文和顶部按钮均不相交 | `C93C5B8C...C52E29` |
| `out-of-battle-reveal-{initial,middle,complete,hud}-{1280x720,1920x1080,2560x1080}.png` | 三视口四阶段 | 12 张逐图检查通过；无裁切、重叠或不可读文本 | 见同目录结构化索引 |
| `out-of-battle-reveal-resize-{center,hud}-1600x900.png` | 运行中 resize | Center 与 HUD 均稳定落位，无残影或输入遮挡 | 见同目录结构化索引 |

`formal-middle-layout-1920x1080.json` 的 `overlaps=false`，`visual-evidence-index.json` 记录 14 个 frame，全部为 `presenterState=Settled`、`inputLockedByClock=false`、`clockBlocksRaycasts=false`。初始黑帧是预期转场遮罩，不作为空渲染缺陷。
