# Wave 03R Era Clock Source Parity

## 权威语义

Godot `global_clock.gd` 与 Unity `BattleRoundLedger` 共同确认：Era 从 1 开始，Phase 合法范围为 1 到 8；`8 -> 1` 时 Era 加 1。Unity typed snapshot 必须拒绝 Era < 1、Phase 不在 1..8 的输入，不复制 Godot `set_current_phase` 对大步长只归一化一次的历史行为。

`point.gd` 定义 STOP、TRACKING、SPINNING、DECELERATING、SF、SD 六态。迁移只保留用户可观察语义：指针可旋转、相位变化有方向明确的过渡、rollover 在端点完成后回到下一 Era 的 Phase 1。源脚本把状态和速度声明为 `static`，会令多个实例共享状态，这是已识别的源缺陷，Unity Presenter 不复制。

`cartoon_ui.gd` 的核心约束是同一时钟实例从中央锚点移动到 UI 锚点，期间并行移动、缩放和归零旋转，完成后显示环、进度和标签；恢复路径使用 snap，不复制第二个实例。Unity 过渡保留同实例、anchor-relative 终态、可取消与 zero-duration；不复刻 Godot 固定 `screenWidth / 2, 160` 像素坐标。

`shared/ui/era_progress.gd` 与 `Out_Scene.tscn` 确认进度上限为 8、从左到右填充，显示文案为 `第%d时代` 与 `%d / 8`。`out_scene_map_exp.gd` 确认正常路径先执行指针 settle，再移动时钟到 UI；恢复路径直接 snap 到已 settled UI。

## Unity 消费边界

- `BattleFlowPresentationSnapshot` 已携带 authoritative Era/Phase，但现有正式 HUD 只投影静态文本。
- `OutOfBattleShellState` 已携带 Era/Phase；正式 shell 只投影 Era/Phase 文本，没有动画时钟。
- `MainMenuPresenter` 的 `ClockEntrance` 只是整体入口 alpha layer，不是 Era/Phase 状态机。
- 本 wave 新增独立 typed adapter 与 presenter；不改变上述正式 consumer，正式接线留给主智能体串行完成。

## Prefab 只读审计

| Prefab | 行数 | YAML docs / GameObjects / MonoBehaviours | SHA-256 | 结论 |
| --- | ---: | --- | --- | --- |
| `MainMenu.prefab` | 5,990 | 289 / 68 / 87 | `EC4175AA0AD1343452E90CD5F3001488304768603FB934AD66BE1A1A77CF42E6` | 已有 `ClockEntrance/CentralClock/Ring/Face/Hand`，三张素材各引用一次，无 EraClock presenter |
| `OutOfBattleShell.prefab` | 3,966 | 178 / 38 / 53 | `6A3A5C59468B38E953480EBE92D627EFF6298A9B20CD5D7B13B65165F337988A` | 有 EraLabel/PhaseLabel 和地图 presenter；无完整时钟节点 |
| `CombatTopHUD.prefab` | 1,930 | 96 / 23 / 28 | `5FD1E2F4E651E58A9A5B3A5C8F4889345E5E976E6831530175C89ECAB77861FD` | 只有 ClockPlate/ClockLabel 与静态 HUD presenter |

三个 Prefab 均完整读入内存，并核对 YAML 文档边界、节点名、MonoBehaviour 类型、脚本 GUID、Silver 引用与时钟素材 GUID。正式 Prefab 不在本智能体写入范围。

## 素材证据

Unity `Resources/Art/Shell/MainMenu` 下的三张素材与 Godot `image/clock` 原件逐字节相同：

| 资产 | 尺寸 | SHA-256 |
| --- | --- | --- |
| `ring.png` | 192x192 | `A5F2D81981DAF2F50213A8F244E6176C2B1D6DCD3F374AF617D4EDAC924A37FD` |
| `point.png` | 512x512 | `9C9000C24F0A4630C9498A4C00716652A4EB1A9A9FB30DAC5DB7D6B3601A0FB9` |
| `clock_noring.png` | 128x128 | `871A9CA3B1AB44965B89D81DFEECF4DF5EA08D68295B8C283EC019C48335225E` |

三者在 MainMenu 中以 `RawImage` 使用，`raycastTarget=0`。字体使用现有 `Resources/Fonts/Silver.ttf`，SHA-256 `7ADCF56D93142DED08FF18196F78FCAAF552411B9A94DC559DAC90EB5C91CEF1`。Godot 的八张 512x512 `frame_000..007` 是独立帧动画；当前 Unity 已有视觉由 face/ring/point 组成，因此本 wave 不复制帧图或新增素材。

## 明确差异

- 指针相位映射采用 8 等分、顺时针 45 度的确定性映射；Godot 源只定义旋转状态，没有 phase-to-angle 映射。
- 非相邻 snapshot 不伪造多个中间 Domain 事件，采用可记录的 jump/snap 策略。
- rollover 时间线显式经历 Phase 8 端点、reset pulse、Phase 1 终态，以消除 `8 -> 1` 的反向视觉歧义。
- anchor transition 使用 `RectTransform` 锚点和当前 canvas 计算，支持三视口与 dynamic resize，不保留 1920x1080 固定坐标。
