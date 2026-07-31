# Wave 02B2A Agent 05：真实高度表现

> 状态：已审查；仅在 Gate B 后启动
> 负责人：Wave 02B2A Agent 05
> 最后验证日期：2026-08-01

## 角色与单一目标

建立可复用的 Unity 六边形实体柱表现组件：根据纯领域 logical layer count 增删真实 block，并让 renderer、collider、顶面 bounds、occupant anchor 和选中命中同步。只负责组件和组件级 PlayMode，不改共享 Controller/场景。你不是仓库唯一工作者，不得回退他人改动。

## 启动前置、必读与能力

主智能体必须确认 Agent 03 的 logical layer/elevation result API 已冻结，现有 FBX prefab 可读取。

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_EFFECTS_PROMPT.md`
- `docs/migration/unity-3d/02-architecture/adr/0002-combat-board-orbit-and-stacking.md`
- `unity/Assets/_Project/Runtime/Presentation/VerticalSliceController.cs` 的 `CreateTile`（只读）
- `unity/Assets/_Project/Runtime/Presentation/BoardTileView.cs`（只读）
- `unity/Assets/_Project/Resources/Art/Battle/Models/**`
- Agent 03 报告

使用 `codebase-migrate` 对照真实高度语义；使用 `Verification & Quality Assurance` 做物理结构和四向渲染检查。本阶段禁止 Blender；模型本身已有独占证据。

## 独占写入范围

- `unity/Assets/_Project/Runtime/Presentation/Terrain/**`
- `unity/Assets/_Project/Prefabs/Battle/Terrain/**`
- `unity/Assets/_Project/Tests/PlayMode/Terrain/**`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b2a-elevation-agent/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b2a-agent-05-elevation-presentation.md`

禁止修改 Controller、BoardTileView、Cards、Targeting、Scenes、Editor、Domain、Infrastructure、模型/Blender 源、asmdef、ProjectSettings、共享文档和其他 Agent 路径。

## 冻结组件契约

- 输入：logical layer count、grass/dirt block prefab、严格 layer spacing `0.32`。
- 输出只读：`LayerCount`、有序 block transforms/renderers/colliders、`TopBounds`、`OccupantAnchor`、结构变更事件。
- 1 层时 block local Y=0；3 层时为 `0,0.32,0.64`。每层独立 mesh/renderer/collider，不能纵向缩放单 mesh 冒充。
- 增减后 top bounds 从实际最上层 collider/renderer 计算；occupant 和交互 anchor 跟随真实 top，不使用预设相对高度。
- 重复 Apply 相同层数幂等，不重复对象/监听；销毁态可清空整柱但本组件不决定领域销毁规则。
- range/selection 材质必须能覆盖所有层并可完整恢复，不能遗留高亮。

## 实现与验收

1. 先写 PlayMode：1->3、3->1、重复 Apply、每层组件、相邻 y 差、TopBounds/anchor、销毁清空。
2. 使用现有 grass/dirt prefab；运行时创建的对象有稳定命名和父子层级。
3. 在测试 fixture 中放置 occupant marker，升高后验证 marker/collider/top selection 同步。
4. 捕获 before/after 和 0/90/180/270 四向证据；做 canvas/世界区域像素检查并人工检查穿插、悬空、缝隙和选择偏移。
5. 运行 Terrain PlayMode 子集、`git diff --check`，写独占报告。

不实现卡牌、时间轴、规则、完整地图生成、模型修整、VFX 或 Controller 接线。若现有 FBX 的 bounds 无法建立稳定 top，先提供数值证据并报告，不得修改模型或共享 BoardTileView。

## Git 与完成回报

不得暂存、commit、push、切分支、merge、stash。回报：组件/API、层坐标/Bounds 数据、测试计数、截图、性能/对象数量、失败项和主集成接线步骤。
