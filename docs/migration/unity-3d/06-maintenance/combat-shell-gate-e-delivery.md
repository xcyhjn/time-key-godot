# Combat Shell Gate E 交付手册

> 状态：已关闭
> Unity：6000.4.10f1
> 基线：Combat Shell Gate D `105a66d`

## 固定交付边界

Build Settings 只启用并按顺序保存 `Bootstrap`、`GameStart`、`MainMenu`、`OutOfBattleShell`、`CombatVerticalSlice`、`GameOver`。Bootstrap 唯一拥有 SceneFlow、input gate、EventSystem、Audio 与 transition；内容 Scene 各自只有一个 typed entry。所有玩家可见文字继续使用 Silver，Windows build 根必须包含 `Silver-ATTRIBUTION.txt`。

OutOfBattle 使用 background -> context -> room；Combat 使用 status -> timeline -> detail/effect -> hand；GameOver 使用 background -> panel。SceneFlow 等待 `ISceneRevealPresentation.Completion` 后解锁输入。缺层、零时长、disable、destroy 和重复播放均进入终态，不使用 fixed delay 猜完成时间。

局外关卡选择背景复用 `image/outscene_block/out-bg_sea.png`。Unity import 与源文件 SHA-256 均为 `3226E113FA2EB03B85E89287BB25CA24F97E40AD351980A6B49BF0D5B939AE3F`；`OutOfBattleOceanBackground` 依据视口宽高比调整 tiled UV，保持源像素方形。

## 构建与 Player

Editor build 入口为 `TimeKey.Editor.CombatShellGateEAutomation.BuildGateE`。Player 入口为 `TimeKey.exe -timekeyCombatShellGateESmoke -logFile <path>`，必须连续完成三次 OutOfBattle -> Combat -> Victory -> OutOfBattle，保持一个 Bootstrap、一个内容 entry、每轮唯一 identity，并在 resize 请求后输入解锁。当前桌面把 2560x1080 请求限制为实际 1680x1050 Player PNG；精确 2560x1080 由 graphical PlayMode 证据覆盖。

最终 build 为 `Succeeded`、`227249074` bytes、Silver attribution 存在。actual Player exit 0，`PASS=1 / PERF=3 / FAIL=0`；三轮 transition 约 `1674 / 1513 / 1513 ms`，实际 recorder 为 `SetPass Calls Count=18`。内存总增量 `412086` bytes，material monotonic=false、sustained-slope=false；不据此宣称长期无泄漏。

## 权威证据

- full EditMode `343/343`：`editmode-full-final.xml`。
- full graphical Direct3D12 PlayMode `100/100`：`playmode-full-delivery.xml`。
- post-review Combat entrance `5/5` 与 stability `1/1`。
- layered reveal `7/7`、stability `1/1`、animation capture `1/1`。
- 9 张 reveal、3 张海洋三视口、5 张 Player，共 17 张逐图复核，缺陷数 0。
- Gate B 四 yaw 继续继承；Gate E 没有修改 combat camera/background/board。

权威入口是 `04-verification/evidence/combat-shell-gate-e/verification-summary.md`。整改前失败 XML 和早期日志只作诊断。最终本地 Git 检查点由主智能体精确暂存；按用户指示不尝试 push。
