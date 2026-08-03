# Wave 03R Era Clock Formal Integration Report

## 结论

Wave 03R 正式共享接线已关闭。Contract、Presenter、MainMenu、OutOfBattleShell、CombatTopHUD、SceneFlow、Composition、正式 Scene/Prefab、测试、build 和 Bootstrap Player 形成同一条可验证路径；没有第二个时钟 owner，也没有新增运行时稳定 UI 树生成。

## 正式接线

- MainMenu 复用原 `CentralClock`、`clock_noring/ring/point` 与 Silver；新游戏使用同一 `RunStartPayload` 生成 Center snapshot。
- OutOfBattle 先投影 authoritative state 到 Center，等待真实 layered reveal completion，再把同一 presenter 移到 HUD。
- Combat 从 `BattleFlowSnapshot` 投影 HUD snapshot；不推导或写回 Era/Phase，不持有 input lease。
- 共享 Top HUD 的 `HudAnchor` 保存为 `TopBar/ClockPlate` 子节点；稳定 UI 均在 Scene/Prefab 中可由 Inspector 调整。
- Presenter settled 后在 `LateUpdate` 跟随 anchor，覆盖首帧 Canvas/Layout 重排和动态 resize；动画、cancel、disable、rebind 与 zero-duration 契约不变。
- 正式 Player smoke 检查每个 Scene 只有一个 presenter，并等待 Era/Phase/HUD settled 后才取证。

## 验证

| 门禁 | 结果 |
| --- | --- |
| EraClock 定向 EditMode | `17/17` |
| EraClock 定向 graphical PlayMode | `13/13` |
| 正式三次 Victory 往返 | `1/1` |
| 全量 EditMode | `413/413` |
| 全量 graphical PlayMode | `121/121` |
| Windows Development build | `Succeeded`，六 Scene，`227421290` bytes，Silver attribution 存在 |
| 实际 Player | Direct3D12，exit `0`，PASS `1`，FAIL/异常 `0` |

Player 连续三轮的 `eraClockPresenterCount` 均为 1，SetPass 均为 18，最终输入未锁定，`eraClockValidated=true`。正式截图覆盖 MainMenu、OutOfBattle、Combat、Victory 和第三次返回的 2560x1080 状态；全部逐张检查，无裁切、重叠或输入阻断。

## Wave 03R-F reveal overlap closeout

正式局外 reveal 的 Center 中间态红测复现了 Era Clock 与中央 `CombatRoom` 的实际矩形重叠。根因是共享 Top HUD 的 `CenterAnchor` 仍位于画面中心，且 Center scale 为 `1.0`；这与 SceneFlow completion、地图状态或 Presenter owner 无关。修复仅把正式共享 Prefab 的 Center anchor 移到归一化 `(0.20, 0.74)` 并把 Center scale 调为 `0.50`，HUD anchor、reveal completion 和唯一 Presenter 状态机保持不变。

新的正式路由用例保存红/绿 1920x1080 中间态，新的响应式用例覆盖 `1280x720`、`1920x1080`、`2560x1080` 的 initial/middle/complete/HUD，以及运行中 `1600x900` Center/HUD resize。14 个结构化 frame 全部为 `presenterState=Settled`、`inputLockedByClock=false`、`clockBlocksRaycasts=false`；逐图检查未发现中央房间、上下文栏、暂停或设置按钮重叠。

## 诊断与关闭

首次正式截图发现 Combat 时钟覆盖时间轴。根因是 snapshot 在 Canvas/Layout 完成首帧重排前保存了旧世界坐标，而不是 timeline 或 SceneFlow 身份错误。将 HUD anchor 归属到 `ClockPlate` 并让 settled presenter 自动跟随后，定向测试和实际 Player 均关闭该问题。Wave 03R-F 随后单独关闭了局外 Center reveal 与中央房间的几何重叠；两项修复边界互不替代。

两次被取代的 Player 诊断运行在逻辑 PASS 后于退出阶段命中 `D3D12Core.dll` `0xc0000005`；最终从最新正式 build 重跑为 exit 0，且没有新 Windows 崩溃事件。原始诊断日志受 ignore 规则排除，不作为 canonical 证据。

## 下一恢复点

P0 工具链、Wave 03P 移动/Theme 和地图 Domain Gate A 已在当前提交历史中存在。下一阶段必须从 `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md` Gate B 恢复，不得重做 Gate A 或把 EraClock 视觉完成误报为完整局外地图完成。当前无用户决策阻塞。
