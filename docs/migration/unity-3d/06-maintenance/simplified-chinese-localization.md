# Unity 战斗切片简体中文本地化

## 范围

当前 Unity 战斗切片以简体中文作为唯一玩家界面语言。覆盖范围包括：

- HUD 标题、状态提示、目标/生命/中毒信息和结算按钮；
- 选牌、选目标、时间轴预览、提交、取消、结算和清除反馈；
- 时间轴上的敌方意图、卡牌行动与清除命中标记；
- Scene 默认文本、TimelineCell Prefab 动态文本字体；
- 七张当前卡牌的显示名和既有中文卡面美术。

当前切片没有对白、配音、字幕、设置页、教程页或商店界面，因此没有对应的本地化资产。Godot 局外流程不在本阶段范围内。

## 文案来源

玩家可见的运行时文案集中在
`unity/Assets/_Project/Runtime/Presentation/Localization/CombatChineseText.cs`。
Scene/Prefab 的默认文本由
`Time Key/Author Simplified Chinese Localization` 编辑器命令从同一目录写入。

内部 stable ID、JSON 字段、场景节点名、异常信息和诊断日志不翻译。它们属于数据或开发者接口，不应进入玩家界面。

## 术语表

| Stable ID / 英文概念 | 简体中文 | 说明 |
| --- | --- | --- |
| `lighting` | 雷击 | 保留数据中的历史拼写 |
| `earthquake` | 地震 | 卡牌显示名 |
| `wind` | 台风 | 与现有卡面一致 |
| `recover` | 恢复 | 卡牌显示名 |
| `tower` | 高塔 | 卡牌及生成物名称 |
| `poison` | 中毒 | 卡牌及状态名称 |
| `tornado` | 龙卷风 | 卡牌显示名 |
| `enemy-intent` | 敌方意图 / 意图 | 详情区 / 时间轴短标签；位置写作“第 03 格·第二行” |
| HP | 生命 | 玩家界面不显示英文缩写 |
| target | 目标 | 实体目标 |
| hex | 地块 | 六边形棋盘坐标 |
| timeline | 时间轴 | 行动排布区域 |
| clear | 清除 | 移除完整时间轴行动 |
| hit | 命中 | 清除预览覆盖已有行动 |

## 字体与许可

Unity UI 使用 Poppy Works / Wolfgang Wozniak 制作的 `Silver.ttf`。官方发布页将其列为 CC BY 4.0，并要求署名；当项目总预算或总收入超过 10 万美元时，必须联系 Poppy Works 取得直接许可。项目内署名和条件记录保存在
`unity/Assets/_Project/Resources/Fonts/Silver-ATTRIBUTION.txt`。
Windows 构建 Harness 会把同一署名文件复制到 `TimeKeySlice.exe` 同级。

Godot 现有 ARK Pixel 字体的授权仍未确认，因此本阶段没有复制或复用该字体。

## 维护规则

1. 新增玩家可见文案时，先在 `CombatChineseText` 中定义，再由 Presenter/Controller 引用。
2. 新卡牌必须补齐 `GetCardName` 和 `GetTimelineLabel` 的测试用例，不允许在界面回退显示 stable ID。
3. 新 Scene 或 UI Prefab 必须使用 Silver 字体；发布前复核项目预算门槛和署名展示位置。
4. 验收至少包含 EditMode、PlayMode、Windows Build、Player smoke，以及 1280x720、1920x1080、2560x1080 的实际截图检查。
5. 截图证据写入 `docs/migration/unity-3d/04-verification/evidence/simplified-chinese-localization/`，不覆盖历史 Gate 证据。
