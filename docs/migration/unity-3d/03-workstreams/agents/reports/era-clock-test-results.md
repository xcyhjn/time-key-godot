# Wave 03R Era Clock Test Results

## 结果摘要

| Gate | 结果 | Canonical output |
| --- | --- | --- |
| Gate 0 EditMode 红测 | 0/3 passed，预期失败 | `era-clock-animation-gate-0/editmode-red-20260803.xml` |
| Gate 0 PlayMode 红测 | 0/1 passed，预期失败 | `era-clock-animation-gate-0/playmode-red-20260803.xml` |
| EraClock 定向 EditMode | 14/14 passed | `era-clock-animation-gate-evidence/editmode-targeted-final.xml` |
| EraClock 定向 graphical PlayMode | 11/11 passed | `era-clock-animation-gate-evidence/playmode-targeted-final.xml` |
| 全量 EditMode | 410/410 passed | `era-clock-animation-gate-evidence/editmode-full-final.xml` |
| 全量 graphical PlayMode | 119/119 passed | `era-clock-animation-gate-evidence/playmode-full-final.xml` |
| Windows development build | Succeeded，227,078,396 bytes | `era-clock-animation-gate-player/build-summary.json` |
| 实际 Windows Player smoke | Passed，marker 正确，exit 0 | `era-clock-animation-gate-player/player-smoke-summary.json`、`player-run-final.log` |
| 正式接线定向 EditMode | 17/17 passed | `era-clock-animation-gate-evidence/editmode-formal-targeted.xml` |
| 正式接线定向 graphical PlayMode | 11/11 passed | `era-clock-animation-gate-evidence/playmode-formal-targeted.xml` |
| 正式接线全量 EditMode | 413/413 passed | `era-clock-animation-gate-evidence/editmode-formal-final.xml` |
| 正式接线全量 graphical PlayMode | 119/119 passed | `era-clock-animation-gate-evidence/playmode-formal-final.xml` |
| 正式六 Scene Windows build | Succeeded，227,421,258 bytes | `combat-shell-gate-e/build-summary.json` |
| 正式 Bootstrap Player smoke | Passed，Direct3D12，exit 0 | `combat-shell-gate-e/player-smoke-summary.json` |
| 03R-F 定向 EditMode | 17/17 passed | `era-clock-closeout/editmode-era-clock-targeted.xml` |
| 03R-F 定向 graphical PlayMode | 13/13 passed | `era-clock-closeout/playmode-era-clock-targeted.xml` |
| 03R-F 正式三次 Victory 往返 | 1/1 passed | `era-clock-closeout/playmode-three-victory-cycles.xml` |
| 03R-F 全量 EditMode | 413/413 passed | `era-clock-closeout/editmode-full.xml` |
| 03R-F 全量 graphical PlayMode | 121/121 passed | `era-clock-closeout/playmode-full.xml` |
| 03R-F 六 Scene Windows build | Succeeded，227,421,290 bytes | `combat-shell-gate-e/build-summary.json` |
| 03R-F 实际 Bootstrap Player smoke | Passed，Direct3D12，3 cycles，exit 0 | `combat-shell-gate-e/player-smoke-summary.json` |

所有最终 XML 均满足 `total > 0`、`failed = 0`。测试使用 Unity `6000.4.10f1`，PlayMode/Player renderer 为 Direct3D12，未使用 `-nographics` 代替视觉验收。

## 覆盖面

EditMode 14 项覆盖非法 Era/Phase/sequence、OutOfBattle adapter、input/reveal metadata、相邻 phase、8 -> 下一 Era 1、jump/snap、phase+anchor、repeat/stale/conflict、最新 transition completion、同视觉新 sequence 和 cancel generation。

Presenter PlayMode 9 项覆盖首次绑定、相邻 phase 指针与 progress、rollover reset/pulse/phase-one、快速连续 Apply、anchor transition、dynamic refresh、zero-duration、rebind、stale/repeat、disable/enable。视觉 PlayMode 2 项覆盖三视口、动态 resize、像素边界和 rollover initial/middle/complete。

实际 Player 运行结果：Era 8 / Phase 1 / `Settled` / `Hud`，`rootBlocksRaycasts=false`，异常 0，隔离 route transitions 3，presenter count 1，材质 `66 -> 66`，增长 0，日志 marker 为 `ERA_CLOCK_PLAYER_SMOKE_PASS`。

## 可重放命令

中文仓库路径在本机 Unity Package Manager 启动时不稳定，因此使用指向同一 `unity` 目录的现有 ASCII junction `D:\timekey-unity-731`。证据仍写回 canonical 仓库路径。

```powershell
$env:TIMEKEY_REPOSITORY_ROOT='D:\godot\时之钥\时之钥'
$unity='C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe'
$project='D:\timekey-unity-731'

& $unity -batchmode -projectPath $project -runTests -testPlatform EditMode `
  -testFilter 'TimeKey.Tests.EditMode.EraClock' `
  -testResults 'D:\godot\时之钥\时之钥\docs\migration\unity-3d\04-verification\evidence\era-clock-animation-gate-evidence\editmode-targeted-final.xml' `
  -logFile 'D:\godot\时之钥\时之钥\docs\migration\unity-3d\04-verification\evidence\era-clock-animation-gate-evidence\editmode-targeted-final.log'

& $unity -batchmode -projectPath $project -runTests -testPlatform PlayMode `
  -testFilter 'TimeKey.Tests.PlayMode.EraClock' `
  -testResults 'D:\godot\时之钥\时之钥\docs\migration\unity-3d\04-verification\evidence\era-clock-animation-gate-evidence\playmode-targeted-final.xml' `
  -logFile 'D:\godot\时之钥\时之钥\docs\migration\unity-3d\04-verification\evidence\era-clock-animation-gate-evidence\playmode-targeted-final.log'
```

去掉 `-testFilter` 可重放全量 suite。PowerShell 对 GUI executable 可能提前返回，自动化时使用 `Start-Process -Wait -PassThru -WindowStyle Hidden` 并检查 `ExitCode`。

Player build：

```powershell
& $unity -batchmode -quit -projectPath $project `
  -executeMethod 'TimeKey.Editor.EraClock.EraClockEvidenceAutomation.BuildPlayerEvidence' `
  -logFile 'D:\godot\时之钥\时之钥\docs\migration\unity-3d\04-verification\evidence\era-clock-animation-gate-player\player-build-final.log'
```

build automation 使用显式 Scene 数组，只包含 `Assets/_Project/Editor/EraClock/EraClockPlayerEvidence.unity`，`buildSettingsUnchanged=true`。执行 `build-summary.json` 的 `executablePath`，加 `-logFile player-run-final.log`，退出后检查 summary 和 marker。

## 失败诊断记录

| 非 canonical 迭代 | 根因 | 修复 |
| --- | --- | --- |
| 中文 `-projectPath` 启动失败 | Unity Package Manager 对该路径失败 | 使用指向同工程的 ASCII junction；未修改工程配置 |
| rollover 行为测试一度 frame-sensitive | 测试在双重 easing/临界帧取样 | 单一 easing owner，并等待可观察 pulse 条件 |
| 首轮视觉画面为空 | ScreenSpace Camera/RenderTexture 配置不完整 | 保存并显式绑定 Camera/RenderTexture |
| dynamic resize 触边 | HUD anchor 顶部安全内缩不足且 resize 未 settle | 增加 reference inset，并等待两帧 layout settle |
| 全量 PlayMode 一度 118/119 | 其他 suite 遗留 layer 0 渲染污染像素边界 | evidence Camera/Canvas 隔离到 layer 31 |
| timeline schema 首轮编译失败 | 测试引用了错误枚举名 | 改为现有 `EraClockPhaseTransition`，定向 11/11 重跑 |
| 全量 PlayMode 重写其他 wave 的历史 PNG | 既有视觉测试使用固定 canonical 输出路径；一次运行还遇到 Win32 error 1224 | 最终 suite 重跑通过；按任务开始时的逐文件 dirty inventory，仅将本次新增的 10 个历史 PNG diff 恢复到 HEAD，tracked dirty 集合精确回到基线 120 |

被最终结果取代的诊断日志/XML已删除；Gate 0 必然失败 XML/log 和全部 canonical 结果保留。

完整 PlayMode suite 会执行其他 wave 的视觉测试并写入它们各自的固定证据目录。后续重放应在 disposable worktree 中进行，或先保存逐文件 dirty inventory，并且只恢复运行前确认干净、运行后才变化的历史输出；不能批量清理或覆盖用户已有 dirty 证据。

## 正式接线补充

正式路由现在覆盖 MainMenu -> OutOfBattle -> Combat -> return，并连续执行三次 Victory。每轮均为唯一 Bootstrap、唯一内容 entry 和唯一 EraClockPresenter；最终 `eraClockValidated=true`、`finalInputLocked=false`、SetPass 18，三次内存样本不构成 sustained monotonic growth。

正式 Player 首轮视觉复核暴露了 Combat 首帧 Canvas/Layout 在 snapshot 之后重排的问题。Presenter 现于 settled 状态自动跟随 anchor，PlayMode 用例验证移动 anchor 后无需手工 `RefreshLayout()`；最终 Combat 截图中时钟位于 `ClockPlate`，不再覆盖时间轴。两次被取代的诊断运行在 PASS 后触发 `D3D12Core.dll` 退出崩溃；最终 canonical 重建运行 exit 0，Windows 事件日志无对应崩溃，因此不作为开放 Gate。

## Wave 03R-F 补充

局外 reveal Center 中间态先以正式 Bootstrap 路由红测复现 `EraClock` 与 `CombatRoom` 矩形相交，再以共享 Top HUD 的 `(0.20, 0.74)` Center anchor / `0.50` scale 关闭。绿测覆盖正式路由中帧、三视口四阶段和运行中 resize；结构化证据同时验证时钟不阻断 raycast。全量 EditMode 首轮因缺少 `TIMEKEY_REPOSITORY_ROOT` 且误用 `-nographics` 出现 2 个运行参数失败，补齐仓库根变量并使用 D3D12 图形设备后同一代码 `413/413` 通过，不构成产品缺陷。
