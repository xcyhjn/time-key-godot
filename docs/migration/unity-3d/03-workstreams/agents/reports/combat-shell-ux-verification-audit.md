# Combat Shell UX verification audit

> 结论：PASS WITH CONCERNS；无硬阻塞
> 所有者：主智能体只读审计
> 日期：2026-08-02

## 状态矩阵

| 区域 | 必测状态 | 必测输入/恢复 |
| --- | --- | --- |
| GameStart | 三字关键帧、终态、不可跳过 | pointer/key 不提前跳过；动画禁用时直接落终态 |
| MainMenu | idle、focus/hover、pressed、Continue disabled、设置/种子/退出弹层 | pointer/keyboard、ESC、弹层焦点圈、重复点击 Busy |
| Transition | cover、loading、first render、reveal | 遮罩拦截 pointer/navigation；失败恢复来源 focus |
| OutOfBattle | 初始地图、房间选择、士兵确认、HUD 入场 | 左键释放、确认/取消、相邻与不可通行、重复选择 |
| Combat | 首帧黑屏检查、地图/HUD/时间轴/意图/手牌入场 | payload 绑定前不可交互；入场完成后恢复焦点 |
| Victory | 输入锁、横幅、reward entry/claim、typed return | reward once；相同 outcome 幂等 |
| Defeat | 输入锁、GameOver 动画、返回 MainMenu | 无 reward；失败时恢复可操作来源或清晰错误态 |

## 视口与可读性

最终至少检查 `1920x1080`、`1280x720` 与一个超宽视口。每张图人工检查顶部条、中央时钟、敌方总生命、时间轴、时间币、手牌、牌库/弃牌、弹层及安全区的裁切、遮挡、字体和对比度。所有玩家可见中文继续使用 Silver Font/Material。

## 自动化与实机分工

- EditMode：typed contract、route/payload compatibility、phase/idempotency/failure、Build Settings 与 Scene 结构。
- PlayMode：真实 additive 往返、持久对象唯一性、camera/interactive root 唯一性、输入遮罩、焦点恢复与注入失败。
- Harness：关键状态截图和结构化 phase trace；截图不能替代行为断言。
- Player smoke：从 Bootstrap 冷启动，至少完成 MainMenu -> OutOfBattle -> Combat -> outcome -> return，并验证 marker、异常、build hash 与 typed identity。

## 当前限制

Gate 0 的 960x540 Godot 刷新只用于源体验冻结，不作为 Unity 最终视觉验收。hover 因桌面控制 API 不支持 pointer move 未补；GameOver 直接加载灰屏证明该场景依赖完整流程状态，后续必须从真实 defeat 路径捕获。
