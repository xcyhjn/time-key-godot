# 测试与证据指南

> 状态：Combat Shell Gate E 完整门禁已通过
> Unity：6000.4.10f1

## 分层门禁

| 层 | 真实入口 | 证明内容 |
| --- | --- | --- |
| Domain/EditMode | `unity/Assets/_Project/Tests/EditMode/**` | 坐标、时间轴、牌区、回合资源、终局、效果、快照、确定性与失败无副作用 |
| Application/EditMode | `Tests/EditMode/Application/**`、`Diagnostics/**` | 命令顺序、typed target、battle-flow hook、typed return、幂等和 trace 中立性 |
| Infrastructure/EditMode | `Tests/Infrastructure/**` | 七卡 JSON、typed token、catalog、`front_image` 定位和效果注册 |
| Scene/EditMode | `Tests/EditMode/Composition/CombatSceneAssetTests.cs` | Play 前层级、Inspector 引用、十一个 Prefab、Silver Font/Material、action 与 BattleFlow 接线 |
| Presentation/PlayMode | `Tests/PlayMode/**` | Binding 生命周期、动态实体手牌、BattleFlow/settlement、范围、Timeline、地形和四向选择 |
| Editor harness | `TimeKey.Editor.VerticalSliceAutomation.BuildValidateAndCapture` | 实际渲染、公共交互路径、像素检查与 Windows build |
| Player smoke | `TimeKeySlice.exe -timekeySmokeQuit` | 构建产物端到端路径和退出码 |

## 可复制命令

从仓库根目录执行；同一时刻只运行一个 Unity 实例：

```powershell
$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe'
$UnityProjectAlias = 'D:\timekey-unity-731'
$RepositoryRoot = (Get-Location).Path
$EvidenceDirectory = Join-Path $RepositoryRoot 'docs\migration\unity-3d\04-verification\evidence\deck-battle-flow-gate-d'
$env:ALLUSERSPROFILE = $env:ProgramData
$env:TIMEKEY_REPOSITORY_ROOT = $RepositoryRoot

$Arguments = @('-projectPath',$UnityProjectAlias,'-batchmode','-runTests','-testPlatform','EditMode','-testResults',(Join-Path $EvidenceDirectory 'editmode-results.xml'),'-logFile',(Join-Path $EvidenceDirectory 'editmode.log'))
$Process = Start-Process $UnityEditor -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
if ($Process.ExitCode -ne 0) { throw "EditMode failed: $($Process.ExitCode)" }

$Arguments = @('-projectPath',$UnityProjectAlias,'-batchmode','-runTests','-testPlatform','PlayMode','-testResults',(Join-Path $EvidenceDirectory 'playmode-results.xml'),'-logFile',(Join-Path $EvidenceDirectory 'playmode.log'))
$Process = Start-Process $UnityEditor -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
if ($Process.ExitCode -ne 0) { throw "PlayMode failed: $($Process.ExitCode)" }

$Arguments = @('-projectPath',$UnityProjectAlias,'-batchmode','-executeMethod','TimeKey.Editor.VerticalSliceAutomation.CaptureDeckBattleFlowGateD','-quit','-logFile',(Join-Path $EvidenceDirectory 'capture.log'))
$Process = Start-Process $UnityEditor -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
if ($Process.ExitCode -ne 0) { throw "Harness failed: $($Process.ExitCode)" }

$Player = Join-Path $UnityProjectAlias 'Builds\Windows\TimeKeySlice.exe'
$PlayerLog = Join-Path $EvidenceDirectory 'player-smoke.log'
$Process = Start-Process $Player -ArgumentList @('-batchmode','-timekeySmokeQuit','-logFile',$PlayerLog) -Wait -PassThru -WindowStyle Hidden
if ($Process.ExitCode -ne 0 -or -not (Select-String $PlayerLog 'TIMEKEY_PLAYER_SMOKE_PASS' -Quiet)) { throw 'Player smoke failed' }
```

Unity 退出码不足以证明测试执行；还要解析 XML 根 `test-run`，确认 `total=passed`、`failed=0` 且 `total>0`。02B4 Gate D 最终结果为 full EditMode `300/300`、Direct3D12 PlayMode `61/61`；Windows build `Succeeded`、`211133001` bytes，Player 退出码 0 且 smoke marker 恰好一次。

## 视觉与继承

任何 Scene、Prefab、Presenter、Controller、材质、布局、镜头或资源变化都会使相关视觉证据失效。最终至少人工打开 1280x720、1920x1080、2560x1080 和 yaw 0/90/180/270；检查七卡完整显示、原图比例、右侧 HUD、范围、Timeline valid/invalid、残留高亮和四向选择。

`earthquake` 必须同时由 Domain、PlayMode 和截图证明：七个有效柱各从 1 层变 3 层，每层独立 mesh/renderer/collider，间距 `0.32`，顶面与目标锚点增量 `0.64`。`harness-summary.json` 的像素非空检查不能代替人工查看。

Godot 基线、原卡面哈希和未受本阶段修改影响的契约可按 `04-verification/inherited-verification-ledger.md` 继承。修改某模块后只继承未被覆盖的证据，并在最终集成态刷新所有受影响门禁。

原始 `.log` 由证据目录 `.gitignore` 排除；只提交 XML、JSON、PNG 和人工总结。失败先分类为编译、纯逻辑、生命周期、Scene 接线、资源、渲染或 build/Player，再修复受影响层并跑全量终验。回滚使用单一目的提交，不使用破坏性 Git 命令。

Gate D 共提交 54 张 PNG，覆盖三视口七卡、lighting/earthquake 四 yaw、Tower/Poison 四 yaw 和 Wind/Tornado 三态/清除残留；这些图已逐张人工打开。最终结构化结果与视觉结论见 `04-verification/evidence/remaining-cards-gate-d/verification-summary.md`。

## Wave 02B3 命令与结果

Test Runner 命令不要与 `-quit` 同用；Unity 6 会在生成 XML 前退出。EditMode 可加 `-nographics`，PlayMode 视觉回归必须保留图形设备。最终解析结果为 EditMode `236/236`、PlayMode `53/53`。

阶段 harness 入口：

```text
TimeKey.Editor.VerticalSliceAutomation.CaptureTurnLifecycleGateB
TimeKey.Editor.VerticalSliceAutomation.CaptureTurnLifecycleGateD
TimeKey.Editor.VerticalSliceAutomation.BuildTurnLifecycleGateD
```

Player 使用 `TimeKeySlice.exe -batchmode -nographics -timekeySmokeQuit -logFile <path>`，同时要求 exit 0 与 `TIMEKEY_PLAYER_SMOKE_PASS`。证据目录保存 XML/JSON/PNG/人工总结；Unity 日志仍不提交。Silver TextMesh 资产测试必须同时断言 Font 和 sharedMaterial。

## Wave 02B4 命令与结果

阶段 harness/build 入口：

```text
TimeKey.Editor.VerticalSliceAutomation.CaptureDeckBattleFlowGateD
TimeKey.Editor.VerticalSliceAutomation.BuildDeckBattleFlowGateD
```

最终只提交 `editmode-final.xml`、`playmode-final.xml`、18 张 PNG、三个 JSON、`visual-review.md` 与 `verification-summary.md`。Player marker 前必须断言 `7/5/0 -> 2/5/5 -> 7/5/0`、phase/timecoins、确定性回洗、card-instance/action identity 分离、Victory 输入锁、相反 outcome conflict 与 typed return；项目程序集 SHA-256 记录在 `player-smoke-summary.json`。

## Combat Shell Gate A

Scene author/build 入口为 `TimeKey.Editor.CombatShellGateAAutomation.AuthorGateA` 与 `.BuildGateA`。Unity 必须通过 `D:/timekey-unity-731` ASCII junction 启动，避免 Unicode project path 的 Package Manager `path undefined`；该 junction 指向真实 `unity/`，不是副本。

最终整改证据为 `combat-shell-gate-a-remediation/editmode-review-closure-final.xml`、`playmode-review-closure-rendered-final.xml`、build/player 两个 JSON 和 verification summary；raw logs 与失败/超时诊断不提交。PlayMode 视觉回归不得使用 `-nographics`，否则 RenderTexture 用例会产生假失败。Player 以可见窗口和 `-timekeyCombatShellSmoke` 启动，因为隐藏窗口在 `runInBackground=false` 时会暂停 player-loop。要求 exit 0、marker 一次、异常 0；最终刷新结果为 `330/330` 与 `64/64`。

## Combat Shell Gate E

Build 入口是 `TimeKey.Editor.CombatShellGateEAutomation.BuildGateE`，菜单为 `Time Key/Build Combat Shell Gate E`。它验证六 Scene 精确顺序、构建 StandaloneWindows64 Development player、复制 Silver attribution，并写入可执行文件 SHA-256。当前权威 build JSON 是 `combat-shell-gate-e/build-summary.json`。

Player 使用可见窗口启动：

```text
TimeKey.exe -timekeyCombatShellGateESmoke -logFile <path>
```

要求进程 exit 0、`TIMEKEY_COMBAT_SHELL_GATE_E_PLAYER_SMOKE_PASS` 恰好一次、三条 PERF 记录且无 FAIL marker。本地最终日志为 `player-smoke-delivery-final-2.log`，不进入检查点；权威结构化结果为 `player-smoke-summary.json`。D3D12 Player 当前选择的有效 recorder 是 `SetPass Calls Count`；JSON 必须同时保存实际 recorder 名称和值，不能把该值称为 draw-call 计数。

定向测试的当前权威 XML：

- `editmode-assets-targeted.xml`：`3/3`。
- `editmode-build-smoke-compile.xml`：`3/3`。
- `playmode-layered-targeted.xml`：`7/7`。
- `playmode-combat-entrance-post-review.xml`：`5/5`，含运行中强制完成竞态。
- `playmode-stability-post-review.xml`：`1/1`，含内存持续斜率判定。
- `playmode-visual-final.xml`：`1/1`。
- `editmode-full-final.xml`：`343/343`。
- `playmode-full-delivery.xml`：`100/100`，Direct3D12。

`playmode-stability-targeted.xml`、旧 `playmode-stability-final.xml`、`playmode-visual-targeted-2.xml`、`-3.xml`、首次失败的 `playmode-full.xml` 和 99 用例的 `playmode-full-final.xml` 是整改前/中间诊断，不能作为最终结论。动画采集必须保留图形设备；`animation-timeline.json`、9 张 reveal、3 张海洋三视口和 5 张 Player PNG 已逐图复核。Gate B 四 yaw 证据继续继承，因为 Gate E 未改 combat camera/background/board。
