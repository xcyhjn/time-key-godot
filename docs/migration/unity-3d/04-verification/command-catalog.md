# 迁移命令目录

> 状态：本机路径已验证，Unity 命令待执行
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：环境预检、本机 Godot/Unity 安装

以下命令从仓库根目录运行；`$UnityEditor` 和 `$GodotConsole` 是任务专用变量，不覆盖系统变量。

```powershell
$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe'
$GodotConsole = 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe'
```

Godot 基线：

```powershell
& $GodotConsole --path . --editor --headless --quit --verbose
& $GodotConsole --path . --headless --quit-after 900
```

Unity 工程与编译：

```powershell
& $UnityEditor -batchmode -createProject .\unity -quit -logFile .\docs\migration\unity-3d\04-verification\evidence\unity-slice-01\create-project.log
& $UnityEditor -projectPath .\unity -batchmode -quit -logFile .\docs\migration\unity-3d\04-verification\evidence\unity-slice-01\compile.log
```

测试：

```powershell
& $UnityEditor -projectPath .\unity -batchmode -runTests -testPlatform EditMode -testResults .\docs\migration\unity-3d\04-verification\evidence\unity-slice-01\editmode-results.xml -logFile .\docs\migration\unity-3d\04-verification\evidence\unity-slice-01\editmode.log -quit
& $UnityEditor -projectPath .\unity -batchmode -runTests -testPlatform PlayMode -testResults .\docs\migration\unity-3d\04-verification\evidence\unity-slice-01\playmode-results.xml -logFile .\docs\migration\unity-3d\04-verification\evidence\unity-slice-01\playmode.log -quit
```

场景、截图和构建：

```powershell
& $UnityEditor -projectPath .\unity -batchmode -executeMethod TimeKey.Editor.VerticalSliceAutomation.BuildValidateAndCapture -quit -logFile .\docs\migration\unity-3d\04-verification\evidence\unity-slice-01\harness.log
```

Git 卫生：

```powershell
git diff --check -- docs/migration/unity-3d unity .gitignore
git status --short
git diff --cached --name-only
```
