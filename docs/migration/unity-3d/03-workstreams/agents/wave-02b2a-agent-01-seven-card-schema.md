# Wave 02B2A Agent 01：七卡 Schema 与 Adapter

> 状态：已审查；Wave A 可启动
> 负责人：Wave 02B2A Agent 01
> 最后验证日期：2026-08-01

## 角色与单一目标

把七张 Godot 卡牌原始 JSON 转为可验证的 Unity typed effect 定义，同时保持 02B1 `lighting` API 兼容。只负责数据定义、解析和 Infrastructure 测试；不实现效果结算、UI 或场景。你不是仓库唯一工作者，不得回退他人改动。

## 必读和能力

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_EFFECTS_PROMPT.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- `card_data/*.json`
- `unity/Assets/_Project/Runtime/Domain/CardDefinition.cs`
- `unity/Assets/_Project/Runtime/Infrastructure/CardJsonAdapter.cs`
- `unity/Assets/_Project/Tests/Infrastructure/CardJsonAdapterTests.cs`

使用 `codebase-migrate` 逐 fixture 固化数据等价；使用 `Verification & Quality Assurance` 设计错误输入和回归门禁。不要使用 Godot 实现 skill 写 Unity C#。

## 独占写入范围

- `unity/Assets/_Project/Runtime/Domain/CardDefinition.cs`
- 可新增 `unity/Assets/_Project/Runtime/Domain/Cards/**`
- `unity/Assets/_Project/Runtime/Infrastructure/CardJsonAdapter.cs`
- `unity/Assets/_Project/Content/Cards/**`
- `unity/Assets/_Project/Tests/Infrastructure/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b2a-agent-01-seven-card-schema.md`

禁止修改其他 Domain 根文件、Presentation、Scenes、Editor、asmdef、Packages、ProjectSettings、共享文档、Godot 源、其他 Agent 路径和 4 个保护文件。

## 冻结输入输出契约

- 支持 `Damage/Elevation/Recover/Built/Poison/Clear`。
- numeric effect 的 amount 为非负整数；Built 还必须有非空 `CreationId`，其 value 是 count；Clear 必须有规范化、非空的只读 `TimelineCell` mask。
- Clear 必须是卡牌唯一 effect；不得与普通 Resolve effect 混合。
- 保留 `new CardEffect(CardEffectKind.Damage, 100)` 和既有 `CardDefinition` 读取面兼容。
- `CardDefinition.Shape` 表示普通时间轴占格；Clear 的 mask 来自 `effects[].value`，不能从顶层 `shape=0` 伪造。
- `CardDefinition` 保留源 `front_image` 文件名/等价资源引用；只允许普通文件名，拒绝 rooted path、`..` 和目录分隔符。不得用 stable ID 推导素材名。
- stable ID 使用 JSON `name`/basename，保留 `lighting`；数字字符串 `id` 继续映射为现有 `NumericId`。
- 未知 type、错误 value、Built 缺 creation、Clear 非 0/1 矩阵或空 mask 显式 `FormatException`。

## 实现步骤

1. 先扩展 Infrastructure 测试期望表，逐张锁定 stable/numeric ID、front_image、effect payload、range、普通 shape/clear mask。
2. 逐字节复制缺失的六张 JSON 到 Unity `Content/Cards`，不得格式化或改名；记录 SHA-256。
3. 最小扩展 effect union 和不可变值对象，不提前建立完整 ECS/命令总线。
4. 对异构 `value` 使用结构化解析。首选同一 JSON 的 common/numeric/string typed DTO 视图后按 type 组合；禁止 regex、substring、文本替换或修改 fixture。
5. 保持中文未映射字段可忽略、02B1 lighting 测试和 shape 归一化行为。
6. 写独占报告，列明实际 API、fixture 哈希、测试计数和主智能体接线要求。

## 测试与验收

- 七真实 fixture 全部解析，字段与总 Prompt 表一致；`tower -> tower_card.png`、`poison -> poison_card.png` 不丢失。
- `earthquake` 精确为 `Elevation,+2`、7 个 range offset、两格 shape。
- `wind/tornado` 顶层 shape 不成为普通占格，clear mask 分别为 2x2 和 12x1。
- 错误 type/value/creation/mask、缺 stable ID 都显式失败。
- 既有 Infrastructure 与全部 EditMode 0 失败。
- 执行 `git diff --check -- unity/Assets/_Project/Runtime/Domain unity/Assets/_Project/Runtime/Infrastructure unity/Assets/_Project/Content/Cards unity/Assets/_Project/Tests/Infrastructure`。
- Unity 命令使用 `04-verification/command-catalog.md` 的单实例 EditMode 流程；不得与其他 Agent 同时启动 Unity。

## 非目标和停止条件

不实现 TimelineAction、Resolve、棋盘状态、clear commit、UI、卡面、场景或包依赖。若 typed DTO 实测无法解析异构值，先在报告中给出最小复现，停止修改 Packages/asmdef 并通知主智能体；不得自行引入第三方 JSON 包。

## Git 与完成回报

不得暂存、commit、push、切分支、merge、stash。回报必须包含：改动路径、API、七 fixture 哈希/字段表、测试结果、失败项、风险和下一步建议。
