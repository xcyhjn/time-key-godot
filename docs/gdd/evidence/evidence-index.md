# 证据索引

## 证据等级

| 标签 | 含义 |
| --- | --- |
| `OBSERVED` | 通过本轮真实窗口输入和截图观察到。 |
| `SOURCE_CONFIRMED` | 由 Godot 脚本、场景、JSON 或资源确认。 |
| `PROJECT_DOC` | 由仓库维护文档确认。 |
| `DESIGN_REFERENCE` | 来自旧策划设计案，不等于实装。 |
| `INFERRED` | 由多条证据推断，仍需运行或人工确认。 |
| `PROPOSED` | 为补齐可执行 GDD 提出的建议。 |
| `UNVERIFIED` | 尚无行为级证据。 |
| `BLOCKED` | 被当前实现、环境或不可达流程阻断。 |
| `CONFLICT` | 来源之间存在冲突。 |
| `TARGETED_ONLY` | 通过直接场景或定向入口验证，不能证明正常流程可达。 |

## 本轮截图

| ID | 文件 | 类型 | 说明 |
| --- | --- | --- | --- |
| OBS-001 | `media/OBS-001-冷启动主菜单.png` | `OBSERVED` | 冷启动主菜单，六个可见命令。 |
| OBS-002 | `media/OBS-002-首次局外地图.png` | `OBSERVED` | 新游戏后的首张局外地图，时代 1、阶段 1/8。 |
| OBS-003 | `media/OBS-003-角色选择士兵.png` | `OBSERVED` | 角色页显示“士兵”，确认按钮可用。 |
| OBS-004 | `media/OBS-004-选角后局外路线.png` | `OBSERVED` | 选角完成后路线收缩到所选扇区。 |
| OBS-005 | `media/OBS-005-普通战斗初始.png` | `OBSERVED` | 首场普通战斗，敌方总血量 930/930，时间轴和牌堆可见。 |
| OBS-006 | `media/OBS-006-抽牌堆查看.png` | `OBSERVED` | 抽牌堆查看器展示 7 张牌，手牌同时保留。 |
| OBS-008 | `media/OBS-008-中毒卡排程.png` | `OBSERVED` | 中毒牌被选中并拖入时间轴，出现多格排程。 |
| OBS-009 | `media/OBS-009-战斗暂停设置.png` | `OBSERVED` | 战斗暂停菜单、音量/全屏/动画速度/保存返回。 |
| OBS-010 | `media/OBS-010-保存返回标题.png` | `OBSERVED` | 保存并返回标题后重新出现主菜单。 |
| OBS-011 | `media/OBS-011-Continue恢复局外.png` | `OBSERVED` | Continue 进入局外恢复态。 |
| OBS-012 | `media/OBS-012-固定种子局外地图.png` | `OBSERVED` | 固定 seed `timekey-seed-20260825` 的局外地图。 |
| TARGETED-001 | `media/TARGETED-001-教程锁定角色.png` | `TARGETED_ONLY` | 直接教程场景入口显示“?? / [未解锁]”。 |

## 日志与来源

- `logs/cold-start-original.log`：冷启动、场景切换、Godot/Vulkan 信息和运行错误。
- `logs/targeted-tutorial.log`：直接加载教程局外场景的定向验证日志。
- `project.godot`：Godot 4.6、主场景、自动加载、分辨率。
- `scene/main_menu/`、`scene/out_scene/`、`scene/in_scene/`：菜单、地图、战斗。
- `card_data/*.json`：7 张实际卡牌。
- `scene/in_scene/enermy/*.gd`：敌方建筑、HP、意图和奖励。
- `scene/global/Saver.gd`、`scene/global/map_data.gd`：存档和状态。
- `docs/《时之钥》策划设计案.docx`：179 段、1 表、6 图，仅作为设计意图。

