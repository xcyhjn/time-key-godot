# 《时之钥》Godot 原作 GDD 预检报告

## 预检时间

2026-08-25（Asia/Shanghai）

## 研究对象与保护边界

- 研究对象：仓库根目录 Godot 原作 `D:\godot\时之钥\时之钥`。
- 不研究 `unity/` 迁移版本的运行事实；迁移文档只作为历史线索。
- 本任务不修改 Godot 游戏代码、场景、资源、卡牌数据、旧策划案或用户存档。
- 现有工作树包含用户/迁移侧未提交改动：`git status --short --untracked-files=all` 统计为 145 个已修改条目、402 个未跟踪条目、无删除条目。以上改动不属于本任务，研究期间保留原样。
- 本任务新增文件只放在 `docs/gdd/`，不使用 `git add -A`、回滚、清理或覆盖既有文件。

## Godot 运行环境

| 项目 | 结果 | 证据 |
| --- | --- | --- |
| Godot 可执行文件 | 可用 | `D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64.exe` |
| Godot 版本 | `4.6.2.stable.official.71f334935` | `Godot_v4.6.2-stable_win64_console.exe --version` |
| 工程版本声明 | Godot `4.6`、Forward Plus | `project.godot` 的 `config/features` |
| 主场景 | `res://scene/main_menu/main_menu.tscn` | `project.godot` 的 `run/main_scene` UID 解析 |
| 设计分辨率 | `1920 x 1080` | `project.godot` 的 `display/window/size/viewport_*` |
| 拉伸模式 | `canvas_items` | `project.godot` |
| 渲染器/显示驱动 | 默认 Forward Plus；实际运行时以窗口日志记录为准 | `project.godot` 与启动日志 |
| GUI 控制 | 可用 | 已读取 Computer Use Windows API 说明，后续用真实窗口、截图和输入操作 |
| 项目锁/进程 | 预检时未发现《时之钥》Godot 运行进程；编辑器缓存存在 | `Get-Process`、`.godot/editor/` |

## 自动加载与输入

自动加载单例：`Global`、`MapState`、`GlobalClock`、`Signal_Bus`、`GlobalDB`、`GlobalTimecoin`、`Dialogic`、`StatusDb`、`Saver`、`SceneLog`、`SoundManager`。输入映射至少包含 Dialogic 默认确认（Enter、鼠标左键、空格、X 和手柄按钮）；常规 UI 输入还依赖 Godot 默认 `ui_accept`、`ui_cancel`、方向键动作。

## 存档与隔离方案

- 原作存档语义：`user://save_0.cfg`，由 `scene/global/Saver.gd` 写入 `meta`、`player`、`map`、`deck` 四组字段；场景日志写入 `user://logs/scene_flow.log`；教程偏好写入 `user://tutorial_settings.cfg`。
- 预检时 `%APPDATA%\Godot\app_userdata\时之钥` 不存在，未发现可复用的《时之钥》用户存档；其他 Godot 项目目录（如 `cgj-gamejam`）不在本研究范围。
- 第一轮自然游玩使用原项目但先核对实际用户数据目录，禁止读取或覆盖其他项目存档。
- 损坏存档、重复提交、写入中断等破坏性场景使用临时工程副本/独立用户数据环境，证据标记为 `TARGETED_ONLY`；不在原用户目录做删除性验证。

## 旧策划案读取状态

已完整读取 `docs/《时之钥》策划设计案.docx`：

- 179 个正文段落；1 个三列表格（7 种敌方意图）；1 个内嵌形状；6 张内嵌 PNG 图片。
- 图片已逐张检查：时间轴 A-H 示例、卡牌毒素示例、两种地形视角、金色/银白时之钥图示。
- 已提取的核心 DESIGN_REFERENCE 主题：肉鸽卡牌/战棋地形/空间化时间轴；3×12 时间轴；六时代；地形高度 0/7/8 设想；表里双层剧情、接纳/反思、普通结局与弑神真结局；普通/精英/事件/Boss 路线；卡牌六分类；敌方意图与祭坛示例；60–80 分钟和 14–18 场战斗节奏目标。
- 旧策划案只作为设计意图来源，不能作为当前实装证据。

## 资源与已知风险

- 工程包含约 295 个 Godot 脚本、约 1108 个项目资源文件；卡牌、敌人/建筑、教程、奖励、商店、暂停、音频、VFX 和 shader 目录均存在。
- 工程日志目录存在历史 `game.log`，本任务将使用本轮运行的独立日志文件名/时间戳区分历史记录。
- 已知风险：工程已有大量共享工作树改动；原作可能存在自然流程不可达内容；Godot 运行过程中可能生成 `.godot` 导入缓存或用户数据；截图/日志只能证明观察到的状态，不能替代完整可达性证明。

## 首轮自然游玩覆盖

冷启动、主菜单按钮与焦点、首次新游戏教程提示、拒绝/接受教程、角色选择、局外路线、首场普通战斗、暂停/设置、存档与重启 Continue、失败返回主菜单。之后再以第二个 seed 或独立新局补覆盖，并记录精英/事件/商店/Boss 的可达性或阻塞证据。

