# Combat Shell Godot source / visual audit

> 结论：PASS WITH CONCERNS；无 Gate 0 硬阻塞
> 模式：只读子智能体审计
> 日期：2026-08-02

## 冻结事实

- 启动为不可跳过的约 3 秒三字 Logo 动画；脚本存在 skip 分支，但当前 `allow_skip=false`。
- 主菜单实际背景为 `image/BG.png`，视觉为中央大钟、左右各三枚非对称按钮、暗色网格/雾与漂浮六边形。新游戏顺序为状态重置、钟声、时钟旋转、约 `1.5s + 0.25s` Iris 收拢、异步加载外景。
- Continue 无存档时只静默返回；Unity 需提供明确 disabled 状态。Godot 主菜单转场遮罩为 `mouse_filter=IGNORE`；Unity 的全局输入锁必须由 Bootstrap 另行承担。
- 外景为顶部金棕横条、纪元/进度/角色/设置和中央悬挂时钟。首次落定包括 Iris、时钟减速、顶部 HUD 分段入场和相机聚焦。
- 地图移动只接受左键释放，要求相邻且可通行；移动约 `0.3s`、房间切换前 `0.5s`、Dim 约 `0.4s`。士兵确认框暂停 SceneTree，ESC 取消。
- 战斗首次可操作前顺序为地图六边形波纹、顶部 HUD/总生命/时间轴首屏动画、卡牌解锁。地图单格 `0.42s`、波间隔 `0.07s`；时间轴格 `0.26s`、间隔 `0.055s`；意图入场 `0.34s`。
- Victory 先停 BGM、锁输入、隐藏战斗 UI/清时间轴/锁地图，再播放约 `1.25s` 横幅并进入奖励。Defeat 进入 GameOver；GameOver 暂停 SceneTree并顺序播放燃烧、时钟死亡、标题、统计和返回按钮。
- 音频含菜单、普通战斗、Boss 三套 BGM，双播放器约 1 秒交叉淡化，以及时钟/Dim/抽牌/洗牌/时间轴/伤害/胜负 SFX。

## 跨场景风险

- Godot 入战仍使用字符串 battle tag/seed，返回仍有 Dictionary/Variant；Unity 必须收敛为同一 run/room/action identity 的 typed payload/outcome。
- 正向 payload 在目标进入 SceneTree 前注入；返回 outcome 在视觉转场前写入并由外景一次性消费。Unity 必须保持该先后关系。
- Godot GameOver/GameWin/Pause 的加载失败恢复不完整；Unity 必须显式恢复遮罩、暂停、输入与焦点。
- Godot 输入锁只覆盖局部战斗控件；Unity 由 SceneFlow 与既有终局锁共同组成全局 gate。
- Godot 使用 ark-pixel；Unity 延续已冻结的 `Silver.ttf`，不复制字体。

## 证据缺口

本次实际图形刷新覆盖主菜单 idle、外景地图、士兵确认、地图收拢过渡和战斗首个可渲染帧。Computer Use API 无 pointer-move，未取得 hover；GameOver 直接加载因缺完整运行状态得到灰屏。启动关键帧、设置/种子弹层、Victory/Defeat/返回和失败恢复必须在相应 Gate 通过真实完整流程补拍。
