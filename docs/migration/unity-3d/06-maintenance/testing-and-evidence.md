# 测试与证据指南

> 状态：Remaining Cards Gate C 已通过，Gate D 最终门禁待执行
> Unity：6000.4.10f1

## 分层门禁

| 层 | 真实入口 | 证明内容 |
| --- | --- | --- |
| Domain/EditMode | `unity/Assets/_Project/Tests/EditMode/**` | 坐标、时间轴、效果、快照、确定性与失败无副作用 |
| Application/EditMode | `Tests/EditMode/Application/**`、`Diagnostics/**` | 命令顺序、typed target、取消、幂等和 trace 中立性 |
| Infrastructure/EditMode | `Tests/Infrastructure/**` | 七卡 JSON、typed token、catalog、`front_image` 定位和效果注册 |
| Scene/EditMode | `Tests/EditMode/Composition/CombatSceneAssetTests.cs` | Play 前层级、Inspector 引用、八个 Prefab 与 Clear preview 接线 |
| Presentation/PlayMode | `Tests/PlayMode/**` | Binding 生命周期、卡手、范围、普通/Clear Timeline、地形和四向选择 |
| Editor harness | `TimeKey.Editor.VerticalSliceAutomation.BuildValidateAndCapture` | 实际渲染、公共交互路径、像素检查与 Windows build |
| Player smoke | `TimeKeySlice.exe -timekeySmokeQuit` | 构建产物端到端路径和退出码 |

## 可复制命令

从仓库根目录执行；同一时刻只运行一个 Unity 实例：

```powershell
$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe'
$UnityProjectAlias = 'D:\timekey-unity-731'
$RepositoryRoot = (Get-Location).Path
$EvidenceDirectory = Join-Path $RepositoryRoot 'docs\migration\unity-3d\04-verification\evidence\unity-decoupling-r3'
$env:ALLUSERSPROFILE = $env:ProgramData
$env:TIMEKEY_REPOSITORY_ROOT = $RepositoryRoot

$Arguments = @('-projectPath',$UnityProjectAlias,'-batchmode','-runTests','-testPlatform','EditMode','-testResults',(Join-Path $EvidenceDirectory 'editmode-results.xml'),'-logFile',(Join-Path $EvidenceDirectory 'editmode.log'))
$Process = Start-Process $UnityEditor -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
if ($Process.ExitCode -ne 0) { throw "EditMode failed: $($Process.ExitCode)" }

$Arguments = @('-projectPath',$UnityProjectAlias,'-batchmode','-runTests','-testPlatform','PlayMode','-testResults',(Join-Path $EvidenceDirectory 'playmode-results.xml'),'-logFile',(Join-Path $EvidenceDirectory 'playmode.log'))
$Process = Start-Process $UnityEditor -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
if ($Process.ExitCode -ne 0) { throw "PlayMode failed: $($Process.ExitCode)" }

$Arguments = @('-projectPath',$UnityProjectAlias,'-batchmode','-executeMethod','TimeKey.Editor.VerticalSliceAutomation.BuildValidateAndCapture','-quit','-logFile',(Join-Path $EvidenceDirectory 'harness.log'))
$Process = Start-Process $UnityEditor -ArgumentList $Arguments -Wait -PassThru -WindowStyle Hidden
if ($Process.ExitCode -ne 0) { throw "Harness failed: $($Process.ExitCode)" }

$Player = Join-Path $UnityProjectAlias 'Builds\Windows\TimeKeySlice.exe'
$PlayerLog = Join-Path $UnityProjectAlias 'player-startup.log'
$Process = Start-Process $Player -ArgumentList @('-batchmode','-timekeySmokeQuit','-logFile',$PlayerLog) -Wait -PassThru -WindowStyle Hidden
if ($Process.ExitCode -ne 0 -or -not (Select-String $PlayerLog 'TIMEKEY_PLAYER_SMOKE_PASS' -Quiet)) { throw 'Player smoke failed' }
```

Unity 退出码不足以证明测试执行；还要解析 XML 根 `test-run`，确认 `total=passed`、`failed=0` 且 `total>0`。Gate C 当前结果为 EditMode `152/152`、Clear Scene/Presentation PlayMode `4/4`；完整 PlayMode、build 与 Player 必须在 Gate D 最终刷新。

## 视觉与继承

任何 Scene、Prefab、Presenter、Controller、材质、布局、镜头或资源变化都会使相关视觉证据失效。最终至少人工打开 1280x720、1920x1080、2560x1080 和 yaw 0/90/180/270；检查七卡完整显示、原图比例、右侧 HUD、范围、Timeline valid/invalid、残留高亮和四向选择。

`earthquake` 必须同时由 Domain、PlayMode 和截图证明：七个有效柱各从 1 层变 3 层，每层独立 mesh/renderer/collider，间距 `0.32`，顶面与目标锚点增量 `0.64`。`harness-summary.json` 的像素非空检查不能代替人工查看。

Godot 基线、原卡面哈希和未受本阶段修改影响的契约可按 `04-verification/inherited-verification-ledger.md` 继承。修改某模块后只继承未被覆盖的证据，并在最终集成态刷新所有受影响门禁。

原始 `.log` 由证据目录 `.gitignore` 排除；只提交 XML、JSON、PNG 和人工总结。失败先分类为编译、纯逻辑、生命周期、Scene 接线、资源、渲染或 build/Player，再修复受影响层并跑全量终验。回滚使用单一目的提交，不使用破坏性 Git 命令。
