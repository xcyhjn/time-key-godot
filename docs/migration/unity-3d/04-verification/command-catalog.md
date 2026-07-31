# 迁移命令目录

> 状态：命令已实际验证
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：环境预检、本机 Godot/Unity 安装

以下命令从仓库根目录运行。Unity 使用现有 ASCII junction `D:\timekey-unity-731` 指向仓库 `unity/`；`ALLUSERSPROFILE` 只在当前 PowerShell 进程设置，修复 Codex 启动环境中 Unity Package Manager 缺失系统路径的问题。

```powershell
$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe'
$GodotConsole = 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe'
$RepositoryRoot = (Get-Location).Path
$UnityProjectAlias = 'D:\timekey-unity-731'
$EvidenceDirectory = Join-Path $RepositoryRoot 'docs\migration\unity-3d\04-verification\evidence\unity-slice-01'
$env:ALLUSERSPROFILE = $env:ProgramData
$env:TIMEKEY_REPOSITORY_ROOT = $RepositoryRoot
```

Godot 基线：

```powershell
& $GodotConsole --path . --editor --headless --quit --verbose
& $GodotConsole --path . --headless --quit-after 900
```

Unity 工程导入：

```powershell
$UnityArguments = @('-projectPath', $UnityProjectAlias, '-batchmode', '-quit', '-logFile', (Join-Path $EvidenceDirectory 'unity-import.log'))
$UnityProcess = Start-Process -FilePath $UnityEditor -ArgumentList $UnityArguments -Wait -PassThru -WindowStyle Hidden
if ($UnityProcess.ExitCode -ne 0) { throw "Unity import failed: $($UnityProcess.ExitCode)" }
```

测试：

```powershell
$UnityArguments = @('-projectPath', $UnityProjectAlias, '-batchmode', '-runTests', '-testPlatform', 'EditMode', '-testResults', (Join-Path $EvidenceDirectory 'editmode-results.xml'), '-logFile', (Join-Path $EvidenceDirectory 'editmode.log'))
$UnityProcess = Start-Process -FilePath $UnityEditor -ArgumentList $UnityArguments -Wait -PassThru -WindowStyle Hidden
if ($UnityProcess.ExitCode -ne 0) { throw "EditMode tests failed: $($UnityProcess.ExitCode)" }

$UnityArguments = @('-projectPath', $UnityProjectAlias, '-batchmode', '-runTests', '-testPlatform', 'PlayMode', '-testResults', (Join-Path $EvidenceDirectory 'playmode-results.xml'), '-logFile', (Join-Path $EvidenceDirectory 'playmode.log'))
$UnityProcess = Start-Process -FilePath $UnityEditor -ArgumentList $UnityArguments -Wait -PassThru -WindowStyle Hidden
if ($UnityProcess.ExitCode -ne 0) { throw "PlayMode tests failed: $($UnityProcess.ExitCode)" }
```

场景、截图和构建：

```powershell
$UnityArguments = @('-projectPath', $UnityProjectAlias, '-batchmode', '-executeMethod', 'TimeKey.Editor.VerticalSliceAutomation.BuildValidateAndCapture', '-quit', '-logFile', (Join-Path $EvidenceDirectory 'harness.log'))
$UnityProcess = Start-Process -FilePath $UnityEditor -ArgumentList $UnityArguments -Wait -PassThru -WindowStyle Hidden
if ($UnityProcess.ExitCode -ne 0) { throw "Harness/build failed: $($UnityProcess.ExitCode)" }
```

构建产物运行时冒烟：

```powershell
$Player = Join-Path $RepositoryRoot 'unity\Builds\Windows\TimeKeySlice.exe'
$PlayerArguments = @('-batchmode', '-timekeySmokeQuit', '-logFile', (Join-Path $EvidenceDirectory 'player-startup.log'))
$PlayerProcess = Start-Process -FilePath $Player -ArgumentList $PlayerArguments -Wait -PassThru -WindowStyle Hidden
if ($PlayerProcess.ExitCode -ne 0) { throw "Player smoke failed: $($PlayerProcess.ExitCode)" }
```

Git 卫生：

```powershell
git diff --check -- .gitignore docs/migration/unity-3d unity
git status --short
git diff --cached --name-only
```

原始 Unity `.log` 只保留在本机并由 scoped ignore 排除；提交使用 NUnit XML、harness JSON、PNG 与 `verification-summary.md`，避免包含许可证握手和机器标识。
