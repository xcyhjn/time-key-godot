# Godot 可观察行为基线

> 状态：核心可验证路径已实跑；完整胜败/奖励/读档未本轮手动打通
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Computer Use 实际交互、Godot headless 日志、关键源码

## 实跑结果

| 步骤 | 输入 | 预期 | 实际 | 证据 |
| --- | --- | --- | --- | --- |
| 启动 | 运行主工程 | Logo 后进入主菜单 | 成功，显示新游戏/继续游戏/种子游戏/数据库/设置/退出 | `04-verification/evidence/godot-baseline/main-menu-window.png` |
| 小视口 | `--resolution 1280x720` | 主菜单可读且不裁切 | 成功；主要按钮、标题和时钟均完整 | `04-verification/evidence/godot-baseline/main-menu-1280x720.png` |
| 新游戏 | 点击新游戏并确认角色扇区 | 生成局外地图 | 成功，显示第 1 时代、1/8、六边形地图与相邻路径 | `out-scene-map.png`、`room-confirm-soldier.png` |
| 进入房间 | 点击相邻普通战斗格 | 切到局内 | 成功，显示 3D 感 2D 六边形堆叠战场、5 张手牌、3×12 时间轴和敌人意图 | `battle-initial.png` |
| 空回合 | 点击结束回合 | 清时间轴、建筑行动、抽新手牌 | 成功；回合 1/8→2/8，时间币 0→24 | `battle-turn-2.png` |
| 玩家行动放置 | 选“雷击”→点 `blockhouse`→点空时间槽 | 生成单格玩家行动 | 成功；时间轴出现青色玩家行动，手牌减少 | `timeline-player-action-placed.png` |
| 行动结算 | 点击结束回合 | 按时间轴结算玩家效果与敌方/建筑链 | 成功；目标 `blockhouse 10/10`→`0/10`，回合 2/8→3/8，时间币 24→49 | `timeline-action-resolved.png` |

## 交互契约

**已验证事实**：卡牌排程不是从手牌一步拖到时间轴。实际流程是：

```text
点击卡牌选中 → 点击六边形世界目标 → 卡牌进入拖拽/排程态并展开时间轴 → 点击合法时间槽确认
```

排程态可用 `Q/E` 旋转形状，右键取消；左键确认。首切片必须保留这三个阶段的语义，但可以将表现改成更清楚的 Unity 输入反馈。

## Headless 结果

- 编辑器导入：退出码 0；日志 `editor-import.log`。
- 主场景 15 秒：退出码 0；日志 `main-scene-headless.log`。
- 编辑器导入出现约 2,084 条 TileSet atlas 错误，主要是重复/越界 tile 创建。
- 主场景退出出现 25 条资源/节点退出清理错误和 ObjectDB 泄漏警告。
- 未观察到新增 `SCRIPT ERROR`、Parse Error 或 Compile Error。

**决策**：这些是源项目基线噪声，迁移阶段不修 Godot；Unity 门禁不允许复制同类退出泄漏。

## 代码对照确认

- 时间轴为 12×3：`scene/in_scene/timeline/TimelineManager.gd`、`timeline_ui.gd`。
- 结算顺序：X 从 0 到 11，每列 Y 从 0 到 2；多格行动只执行一次。
- 敌人时间轴解析 `EffectProcessor._parse_enemy_intent()` 当前返回空队列；实际建筑行为由时间轴结束后的 `Signal_Bus.step_next` 统一触发。
- 胜利阈值是敌方累计血量不高于累计上限 10%，不是必须清零。

## 本轮未完整复现

| 路径 | 原因 | 处理 |
| --- | --- | --- |
| 战斗胜利/失败 | 正常游玩需要多回合；本轮已验证单个效果与建筑行动链 | 后续固定战斗夹具在 Unity 中确定性覆盖 |
| 奖励/商店/返回局外 | 依赖胜利结算与 Broken 建筑 | 源码链已记录，后续独立切片实跑 |
| 退出并读档 | 本轮进房已触发 Godot 槽 0 保存，但未为验证而反复覆盖外部用户存档 | 只记录 schema；后续用隔离 user-data-dir 夹具测试 |
| 教程/Dialogic | 默认配置 timeline 字段为空，教程牌组引用不存在的数字 JSON | 记录为已知缺口，不以教程阻塞首切片 |
| 音频全路径 | 视觉走查中未测量每条 BGM/SFX | 总线和 SoundManager 注册表已静态核对 |

## 视觉检查

- 1920×1080 设计视口在当前 1680×1032 窗口中缩放显示；主菜单和战斗主要 UI 可读。
- 1280×720 窗口实际捕获区域为 1282×752（含标题栏）；主菜单无明显裁切、重叠或缺字。
- 战斗信息密度高；时间轴、卡牌、tooltip 适合继续作为屏幕空间 UI，而非强制 world-space UI。
