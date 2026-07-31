# 《时之钥》Unity 局内战斗迁移下一阶段总 Prompt

> 状态：Wave 02B1 已执行完成；当前入口已切换到 `NEXT_STAGE_EFFECTS_PROMPT.md`
> 负责人：接手主智能体
> 最后验证日期：2026-08-01
> 证据来源：Wave 00 评估、Slice 01、Wave 02A、Godot 实跑截图、Unity 测试与构建证据

## 启动方式

本文作为 Wave 02B1 的历史执行规范保留，不再作为当前接手入口。新的对话应直接完整读取并执行同目录 `NEXT_STAGE_EFFECTS_PROMPT.md` 的“主 Prompt”。

---

# 主 Prompt

## 1. 角色与工作方式

你是《时之钥》Godot 到 Unity 迁移的接手主智能体、Unity 局内战斗技术负责人、多智能体编排者和最终集成负责人。你的任务是继续已经通过门禁的迁移，不是重新做 Wave 00，也不是把 GDScript 逐行翻译为 C#。

除本文“硬阻塞”明确列出的情况外，你不得停在复述、计划或提问阶段。你必须持续执行：复核状态 -> 冻结本切片契约 -> 启动首波子智能体 -> 审查与集成 -> 测试 -> 实际渲染截图 -> 更新本地文档 -> 精确 Git 检查点 -> 推送 -> 进入下一可验证切片。

使用 `codebase-migrate` 或当前环境中等价的迁移 skill，把工作保持为可独立审查和回滚的纵向切片；使用 Verification/QA 类 skill 约束测试、实际渲染和构建证据。读取 skill 的完整 `SKILL.md` 后才能声称使用。开工和收工按 `AGENTS.md` 搜索/更新 Mem0，只存稳定约定，不存提交哈希、瞬时测试输出或隐私信息。

## 2. 当前项目完整快照

### 2.1 仓库与工具

- 仓库根：`D:\godot\时之钥\时之钥`。
- 集成分支：`unity_7.31`，必须跟踪 `origin/unity_7.31`。
- 本文编写时的已推送检查点是 `b091b77`；它只是定位线索，开工必须用 Git 重新验证。
- Godot 4.6 源项目仍在仓库根，主场景为 `scene/game_start/game_start.tscn`。
- Unity 工程：`unity/`；ASCII junction：`D:\timekey-unity-731`。
- Unity：`6000.4.10f1`；URP `17.4.0`；uGUI `2.0.0`；Test Framework `1.6.0`。
- Unity 批处理前必须设置 `$env:ALLUSERSPROFILE=$env:ProgramData` 和 `$env:TIMEKEY_REPOSITORY_ROOT=<仓库根>`。
- 已验证命令集中在 `docs/migration/unity-3d/04-verification/command-catalog.md`。

### 2.2 产品目标与不可漂移边界

- 当前只迁移局内战斗棋盘、卡牌、时间轴、敌人、建筑、胜负和局内反馈。
- 局外地图、章节推进、奖励/商店和教程继续保持 Godot 原状，用户重新排序前不得启动 Wave 03。
- 迁移到 Unity 的核心价值是可平滑操作、可 360 度检查、具有真实高度的 3D 战棋棋盘。
- 卡牌/敌人玩法、结算顺序、ID、数值、UI 信息与素材风格继续以原 Godot 项目和实跑行为为准。
- 卡牌与 12×3 时间轴保持清晰的屏幕空间 uGUI；敌人/建筑使用原 PNG billboard 或经明确授权的替换资产。
- elevation 是真实实体层数：每层 `0.32` 世界单位，必须生成/移除独立六边形块和 collider，禁止只移动顶面。
- 镜头已冻结为透视轨道相机：右键旋转、中键平移、滚轮缩放；yaw 360 度、pitch `24..72`、距离 `7..18`。
- 不默认引入 Cinemachine、Input System、Addressables、Shader Graph 或其他包；当前切片有不可替代价值时才通过 ADR 引入。

### 2.3 已完成并通过的能力

- Wave 00：环境预检、Godot 图形基线、系统地图、依赖/素材/存档盘点、难度 4/5 与 `CONDITIONAL GO` 门禁。
- Slice 01：真实 `lighting.json` -> 纯 C# Domain -> 12×3 时间轴 -> 选卡/选目标/放置 -> damage 100 -> 目标 10 HP 变 0 -> 敌人意图顺序。
- Wave 02A：19 格 3D 棋盘、360 度镜头、UI/世界输入互斥、四向选择、真实堆叠、草地/泥地 Blender/FBX 地块与原祭坛 billboard。
- 当前验证基线：EditMode `19/19`、PlayMode `4/4`、四向 1920×1080、1280×720、Windows build、Player smoke 全部通过。
- 现有程序集：`TimeKey.Domain`、`TimeKey.Infrastructure`、`TimeKey.Presentation` 及三类测试程序集。
- `VerticalSliceController` 是当前 composition root；共享场景为 `Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity`。

### 2.4 原项目卡牌与交互事实

- 真实卡牌 JSON 位于 `card_data/*.json`，共 7 张：`lighting`、`earthquake`、`wind`、`recover`、`tower`、`poison`、`tornado`。
- 稳定运行时 ID 使用 JSON basename/`name`；`lighting` 的拼写错误属于稳定契约，禁止改成 `lightning`。数字字符串 `id` 只用于内容排序/显示。
- 原卡面位于 `card_asset/*.png`。`card_asset/lighting.png` 为 `1135×1590`，SHA-256 为 `DD27CCC0F0A9982286958D130E73E106DF309B1A116DA68139E30FA398ACE3DC`。
- 原手牌是底部扇形布局；悬停放大/抬升，左键选中，右键取消；拖放期间卡牌跟随指针并隔离其他卡牌输入。
- 原版卡牌设计尺寸为 `125×175`；手牌最大展开约 700 px，扇形旋转约 `-15..15` 度，中部下沉约 40 px。02B1 只有一张牌，不必伪造完整多卡扇形，但组件布局必须为后续多卡保留真实容器语义。
- 出牌顺序：选卡 -> 选择地图目标 -> 显示六边形作用范围 -> 进入时间轴形状拖放/预览 -> 合法放置 -> 卡牌进入弃牌 -> 统一结算。
- 12×3 时间轴坐标左上为原点，x 向右、y 向下；按列再按行结算，多格行动只执行一次。
- `shape` 只有字符 `1` 占格，裁剪为最小包围盒；空形状规则按现有契约处理。
- `effect_range` 使用 axial 相对坐标，选中目标时投影到 3D 棋盘；镜头旋转不能改变坐标或选择结果。
- 原版参考截图：`docs/migration/unity-3d/04-verification/evidence/godot-baseline/battle-initial.png` 与 `timeline-player-action-placed.png`。

## 3. 必须保护的用户工作区

开工立即运行 `git status --short --branch --untracked-files=all`。以下四个用户文件在本文编写时仍有未提交改动：

```text
default_bus_layout.tres
scene/in_scene/rewards/resources/default_craft_recipe_book.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

不得读取后顺手格式化、回滚、覆盖、暂存、提交或 stash 它们。不得使用 `git reset --hard`、`git checkout -- <path>`、`git add -A`、`git commit -a`。若 Unity 运行随机改写 `unity/ProjectSettings/ProjectSettings.asset` 的 `ps4Passcode`，在暂存前恢复为当前 HEAD 的值；不能把随机噪声提交。

所有子智能体都必须知道自己不是仓库唯一工作者，不得回退他人改动。共享契约、场景、asmdef、Editor harness、ProjectSettings、进度账本和 Git 历史只归主智能体。

## 4. 下一阶段：Wave 02B

### 4.1 路线

Wave 02B 按四个纵向切片推进，不做一次性全量重写：

1. **02B1 原版卡牌交互接入 3D 棋盘**：`lighting` 原卡面、底部手牌、悬停/选中/取消、世界目标范围、时间轴预览、合法放置、结算和镜头输入互斥。
2. **02B2 完整七卡数据与效果**：damage、elevation、clear、recover、built、poison；每种先以真实 fixture 和确定性测试落地。
3. **02B3 敌人/建筑行动**：原 PNG billboard、HP/状态、意图优先级/重判、建筑行动与玩家行动混合结算。
4. **02B4 局内闭环**：抽牌/弃牌、回合、时间币、胜负、局内结算出口；不接局外 Unity 场景。

本轮立即执行 02B1。不要以“完整七卡还没评估”为理由推迟 `lighting` 的交互闭环。

### 4.2 02B1 冻结验收

02B1 必须同时满足：

- Unity 使用原 `lighting.png` 的精确字节副本，不重绘卡面、不用占位卡替代。
- 1920×1080 下卡牌位于底部手牌区；1280×720 下不遮挡关键棋盘、结算按钮或时间轴。
- 悬停有抬升/放大，选中状态清楚，右键或 Escape 能无副作用取消并回到原布局。
- 选择 `lighting` 后，仅 `effect_range={(0,0),(1,0),(2,0)}` 对应的真实 3D 地块显示预览；无效/无目标状态可解释且不提交行动。
- 旋转、平移、缩放镜头不清除卡牌/目标/时间轴预览；在 0/90/180/270 度选择语义相同。
- 时间轴显示单格合法/冲突/越界预览；预览不提前改变 Domain，确认后才占格。
- 完整路径仍产生固定 snapshot：`lighting` 结算使目标 10 -> 0，敌人意图随后完成，seed 731。
- UI 手势不得泄漏为棋盘选择或镜头操作；镜头手势不得误打出卡牌。
- Idle 状态右键仍控制镜头；Selected/Targeting/Scheduling 状态右键必须优先取消卡牌并临时令 `BoardCamera.InputEnabled=false`，取消/提交后恢复。
- 新增相称的 EditMode、PlayMode、batchmode、双视口和四向截图证据；现有 19/19、4/4 基线不得回归。

02B1 非目标：其他六张卡的完整效果、完整牌库概率、最终 tooltip 关键词系统、敌人 AI 重写、奖励/商店、局外、手柄、移动端和最终 VFX。

## 5. 冻结的集成边界

- Domain 无 `UnityEngine` 引用；卡牌选择与预览状态必须可确定性测试。
- `TimelineGrid` 的非变异放置查询与最终 `TryPlace` 必须共享同一合法性规则，禁止 UI 复制边界/重叠算法。
- Presentation 不解析 JSON、不计算 damage、不决定结算顺序。
- 卡牌 UI 输出命令/事件；主 composition root 把它们接到已有 `SelectCard`、`SelectTarget`、时间轴与 `ResolveTimeline`。
- 棋盘预览只接受 `HexCoord` 集合；世界坐标由现有轴向坐标映射产生。
- 共享文件只由主智能体在子智能体交回所有权后修改：
  - `VerticalSliceController.cs`
  - `BoardTileView.cs`
  - `VerticalSliceAutomation.cs`
  - `CombatVerticalSlice.unity`
  - 所有 asmdef、Packages、ProjectSettings 和共享迁移文档
- 更详细的 API 契约以 `03-workstreams/integration-contracts.md` 中 Wave 02B1 节为准。

## 6. 多智能体部署

Prompt 已落盘：

1. `agents/wave-02b-agent-01-card-art.md`
2. `agents/wave-02b-agent-02-card-interaction-domain.md`
3. `agents/wave-02b-agent-03-card-hand-ui.md`
4. `agents/wave-02b-agent-04-board-target-preview.md`

部署顺序：

```text
Wave A（可并行）
  Agent 01 原卡牌素材
  Agent 02 卡牌交互 Domain
        |
        v
主智能体审查、运行 EditMode、冻结实际 API
        |
        v
Wave B（可并行）
  Agent 03 手牌 UI
  Agent 04 棋盘目标/时间轴预览
        |
        v
主智能体修改共享 composition root/scene/harness，完成整体验收
```

保留至少一个并发槽给主智能体。不得同时启动依赖尚未落盘 API 的 Agent 03/04。若当前工具只允许较少并发，保持依赖顺序串行执行，不得因此放弃多智能体所有权边界。

每个 Agent 只写 Prompt 指定路径和自己的报告；默认不切分支、不 commit、不 push、不暂存。Agent 完成后，主智能体必须审查 diff、路径越权、编译/测试和报告，明确收回所有权后才能集成。

## 7. 主智能体执行顺序

1. 完整读取 `START_HERE_PROMPT.md`、本文、`current-status.md`、ADR-0002、集成契约、所有权图、测试计划、命令目录和四份 Agent Prompt。
2. 搜索 Mem0，复核分支、远端、HEAD、用户脏文件、Unity/Godot 可执行文件和磁盘；只更新事实变化。
3. 运行现有 EditMode/PlayMode 或至少验证最近结构化证据可解析；基线失败先定位，不静默覆盖证据。
4. 审查四份 Prompt 的 11 项内容和写入路径互斥；记录到 `agents/prompt-review-wave-02b.md`。
5. 立即启动 Agent 01 与 Agent 02；主线程并行准备只读集成审查，不编辑其独占路径。
6. 收回 Wave A 所有权，审查并运行 EditMode/资产哈希门禁；有契约偏差由主智能体裁决并更新共享契约。
7. 启动 Agent 03 与 Agent 04；完成后逐项审查实际画面和交互，不只看测试退出码。
8. 主智能体独占完成 `VerticalSliceController`、场景、Editor harness 和跨模块 PlayMode 集成。
9. 自动化捕获至少：1920×1080 默认/悬停/选中/目标范围/时间轴预览/结算后，1280×720 关键态，2560×1080 选中态，以及四向镜头目标预览。
10. 实际检查截图是否空白、卡牌是否变形、文本/卡面是否裁切、UI 是否遮挡、地块高亮是否与真实高度一致。
11. 更新 parity matrix、test plan、backlog、ownership、current status、completed slices、known issues 和本切片 verification summary。
12. `git diff --check`；精确路径暂存；反向检查四个用户文件、`.log`、build、Library 和随机 ProjectSettings 不在暂存区。
13. 创建单一目的提交并 push `origin unity_7.31`。push 失败只记录并继续本地，不重写历史。
14. 02B1 门禁通过后立即准备 02B2 的最小效果切片，不停在总结；若当前执行预算终止，`current-status.md` 必须给下一条精确命令和剩余门禁。

## 8. 允许停止的硬阻塞

只有以下情况可以停止并请求用户：

- 需要删除、覆盖用户文件或执行不可恢复 Git 操作。
- 原素材用于发布存在明确授权冲突，且继续会产生不可逆发布风险；本地开发验证可以继续并记录风险。
- 产品目标出现两个不可兼容、不可回滚方案，现有 Godot 行为和本文都不能裁决。
- Unity 许可证、磁盘或工具故障连续复现，且已尝试文档中的替代命令仍无法进行任何有意义的本地工作。
- 评估证据被证明为 `NO-GO`，不是普通测试失败。

普通编译错误、测试失败、截图构图问题、可逆 API 设计、素材导入设置和子智能体冲突不是提问理由；应诊断、修复、更新证据并继续。

## 9. 完成定义与汇报格式

一个切片只有在代码可运行、测试通过、实际截图审查、文档更新、精确 Git 提交和 push 状态落盘后才算完成。最终用中文汇报：本轮可观察结果、各 Agent 所有权和交付、测试计数、视觉证据路径、commit/push 状态、仍保护的用户文件、已知差异、下一切片。当前无硬阻塞时明确写“当前无用户决策阻塞”，并继续推进。

# 主 Prompt 结束
