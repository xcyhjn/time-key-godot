# Pluginization Code Audit

> Agent：`pluginization-code-audit`
> 范围：只读审计，未打开 Unity/Godot/Blender，未运行任何 authoring、build 或测试写入入口
> 结论：`SceneContractValidator` 是正确的首个实现切片；正式 Scene/Prefab 当前契约可被只读验证，旧完整 authoring 入口不可安全重跑

## 1. 审计基线

- 仓库历史：2026-03-31 至 2026-08-03，共 329 个提交、10 个本地/远端分支引用；三个目标文件均由同一主要贡献者维护。
- `VerticalSliceSceneAuthoring.cs`：1,352 行 / 63,825 bytes，历史变更 9 次。
- `VerticalSliceAutomation.cs`：1,987 行 / 89,616 bytes，历史变更 14 次。
- `VerticalSliceController.cs`：1,335 行 / 48,044 bytes，历史变更 16 次。
- Gate 0 intake 记录的 378 个前置 dirty/untracked 路径全部视为受保护输入。本 Agent 只新增本报告。

## 2. 热点计数与位置

下表是目标文件中的精确字符串命中计数；它反映维护表面积，不等同于缺陷数量。

| 文件 | 热点 | 计数 | 主要位置 |
| --- | --- | ---: | --- |
| `VerticalSliceSceneAuthoring.cs` | `MenuItem` | 4 | 55、390、449、504 |
| 同上 | `new GameObject` | 24 | 147-332、667-982、1251-1327 |
| 同上 | `AddComponent` token | 24 | 185-332、676、899、945、966、1233-1235 |
| 同上 | `AssetDatabase.LoadAssetAtPath` | 32 | 77-118、396-410、566-616、684-1085 |
| 同上 | `Resources.Load` | 2 | 622、661 |
| 同上 | `OpenScene` / `SaveScene` | 4 / 4 | 125/384、419/444、473/499、507/537 |
| 同上 | `SaveAsPrefabAsset` | 11 | 466、682、698、734、753、802、840、863、874、921、982 |
| 同上 | `DestroyImmediate` | 10 | 143、683、699、735、803、841、864、875、922、983 |
| `VerticalSliceAutomation.cs` | `MenuItem` | 10 | 32、168、209、287、434、607、649、862、928、1026 |
| 同上 | `GameObject.Find` | 13 | 376、396、522、659、872、972、1010、1063、1070、1150、1249、1274、1351 |
| 同上 | 其他全局对象查找 | 11 | 74、131、309、346、348、415、449、514、1060、1107、1390 |
| 同上 | `OpenScene` | 3 | 653、866、1144 |
| 同上 | `BuildPipeline.BuildPlayer` | 4 | 178、270、617、839 |
| 同上 | `Directory.CreateDirectory` / `File.WriteAll*` | 14 / 12 | 37-176、612-645、837-858、1397-1947 |
| 同上 | evidence-directory helper | 8 | 1502、1525、1566、1589、1607、1757、1775、1793 |
| `VerticalSliceController.cs` | 稳定 serialized references | 18 | 32-50 |
| 同上 | 动态 Prefab references | 4 | 52-56 |
| 同上 | `Instantiate` | 2 | 890、928 |
| 同上 | `new GameObject` / `AddComponent` / `GameObject.Find` / `Resources.Load` | 0 | 全文件 |

Editor 目录整体另有 20 个 `MenuItem`、6 个 `BuildPlayer` 调用（3 个文件）、14 个 `OpenScene` 调用（7 个文件）、22 个 `Directory.CreateDirectory` 调用（7 个文件）、15 个 `File.WriteAll*` 调用（3 个文件）和 85 个 `Capture(` 命中（2 个文件）。这些入口已经形成明显的历史 Gate 重复，但不应在首切片顺手重构。

## 3. Editor Authoring 与运行时动态对象分类

### 3.1 必须保持为保存资产的稳定对象

正式 Combat Scene 当前保存并唯一命名以下对象：

- 根：`VerticalSliceRoot`，默认 inactive；根上保存 `VerticalSliceController`、`CombatPresentationBinding`、`CombatCompositionRoot`、`UnityCombatTraceSink`、`CombatShellEntrancePresenter` 和 Scene navigation。
- Scene entry：独立 active 根 `CombatSceneEntry`，`SceneContentEntry.contentRoot` 指向 `VerticalSliceRoot`，`contentCamera` 指向 `SliceCamera`。
- 世界：`World/SliceCamera`、`World/Environment/KeyLight`、`World/Environment/FillLight`、`World/BattlefieldGround`、`World/CombatBoardRoot`、`TargetAnchor`、`BoardRangePreview`。
- UI：`SliceCanvas/HUD`、`Header`、`Timeline`、`ActionLayer`、`CardHandHost`、`EffectFrameHost`、`DetailPanel`、`BattleFlowPanel`、Combat Top HUD。
- Scene 中保存的 Prefab instances：36 个 `TimelineCell`、1 个 `BattleFlowPanel`、1 个 `CombatBattleBackground`、1 个 `CombatTopHUD`。
- Bootstrap 保存唯一 `EventSystem`、AudioRoot、transition 与 SceneFlow 服务；Combat 和其他内容 Scene 不得保存 EventSystem。

`VerticalSliceController` 的 22 个 serialized references 当前全部落盘：18 个稳定 Scene 引用和 4 个动态 Prefab 引用。`CombatPresentationBinding`、`TimelinePresenter`、`CombatOccupantPresenter`、`CombatInteractionOverlayPresenter` 与 `CardHandHost` 还保存其 Presenter/host/prefab 接线。

### 3.2 允许的运行时动态实例

- `VerticalSliceController.BuildWorld()`：19 个 `HexColumn` Prefab 实例和一个 `TargetView` Prefab 实例；位置和逻辑高度由运行态决定。
- `HexTileColumn`：按逻辑层数创建 block wrapper，并实例化 grass/dirt Prefab；这是动态地形内容，不是稳定 Scene 根。
- `CardHandHost`：按 `CardInstanceId` 从已保存 `CardView` Prefab 实例化当前手牌。
- `TimelinePresenter`：从 `TimelineActionFrame` Prefab 实例化当前行动/意图框。
- `CombatOccupantPresenter`：从 Tower/状态 Prefab 实例化 occupant 与 poison 状态。
- `CombatInteractionOverlayPresenter`：从 `CardEffectFrame` Prefab 实例化当前详情框。
- `TimelineActionFrame` 内部按保存的 template 生成行动覆盖块；这是该动态 View 的内部表现。

### 3.3 仅作为兼容 fallback、正式 Scene 不应触发的生成路径

- `CardHandHost.EnsureContainer()` 会在 `cardContainer` 为空时创建 `Cards`；`CreateCardView()` 会在 `cardViewPrefab` 为空时创建裸 `CardHandView`（232-268）。
- `CardHandView.EnsureHierarchy()` 会在 Prefab 稳定子引用缺失时创建完整卡面层级并 `AddComponent`（462-504）。
- `HexTileColumn.EnsureOccupantAnchor()` 会在 Prefab 锚点缺失时创建锚点（195-204），`CreateBlock()` 会在模型缺 collider 时动态补 `MeshCollider`（108-127）。
- Timeline clear/placement preview 会在 cell 缺 `Outline` 时动态补组件。

这些 fallback 对隔离组件测试有价值，但会掩盖正式 Prefab 接线回退。Validator 应验证生产 Scene/Prefab 已满足引用与组件契约，使正式 Player 不进入 fallback；本切片不删除 fallback。

## 4. 资源加载与 Scene 查找

- 运行时没有 `GameObject.Find`。
- `CombatCompositionRoot` 从 Scene 中保存的 7 个 `TextAsset` 读取卡牌 JSON，并通过两次 `Resources.Load` 读取卡面，再创建运行时 Sprite（234-266）。这是现有 Composition 资产桥，不属于本 Validator 的重写范围。
- Runtime 内共有 12 个 `FindFirstObjectByType` 命中，均在 Composition/SceneFlow rebind 与 Player smoke；另有 10 个 `FindObjectsByType`，全部在 `SceneFlowPlayerSmoke` 验证唯一性。它们不是 Domain/Application 依赖，也不是本切片要替换的 Service Locator。
- `VerticalSliceAutomation` 使用 13 个字符串 `GameObject.Find` 和 11 个全局对象查找来驱动历史证据。这些查找只应继续留在旧 harness，不能进入 Validator。
- Validator 必须以传入的 `Scene`、显式 asset path、从该 Scene roots 开始的局部遍历和 `SerializedObject` 检查工作，不能使用 `GameObject.Find` 或全局 `Find*ObjectByType`。

## 5. 现有 Scene/Prefab 契约与测试缺口

已有 `CombatSceneAssetTests` 覆盖：主要稳定路径、Controller 22 个非空引用、Composition 三个引用与 7 个唯一 fixture、Timeline 36 个唯一坐标、11 个正式 Battle Prefab 的关键组件、Silver/中文默认值，以及 Controller 不构造稳定对象。

已有 `SceneFlowSceneAssetTests` 覆盖：固定六 Scene Build Settings 顺序、Bootstrap 唯一 EventSystem/服务集、所有内容 Scene 恰好一个 entry 且无持久服务副本。

缺口：

1. 测试断言分散且失败消息不是可复用的结构化诊断，人工执行无法一次得到完整问题清单。
2. 多数 path 检查只验证 `Transform.Find` 非空，未验证同名对象唯一性。
3. 未集中验证 `VerticalSliceRoot inactive`、`CombatSceneEntry active` 以及 entry 的 contentRoot/camera/interaction refs。
4. 未集中验证 Scene 中 36 个 TimelineCell、BattleFlowPanel、Combat background、Combat Top HUD 的 Prefab source。
5. Binding 测试只显式抽查 occupant/overlay/battleFlow，未把所有 Presenter 引用作为一张完整契约表输出。
6. 没有验证只读检查前后 Scene 文件 hash、`scene.isDirty`、当前打开 Scene 集合和 active Scene 均不变。
7. 没有缺节点、重复节点、错误组件、错误 Prefab source、空 serialized ref 等负向 fixture，也没有重复执行的确定性顺序断言。

## 6. 旧 authoring 重跑风险

### P0：完整 authoring 已与正式 Scene 契约不兼容

- `AuthorEditableCombatScene()` 在 141-144 删除 `VerticalSliceRoot` 的全部子节点，随后重建 Camera、Board、Canvas、HUD 和 Timeline。
- 它在 194-196 把 EventSystem 重新放回 Combat 内容 Scene，直接违反 Bootstrap 独占 EventSystem 契约。
- 它在 1041 试图设置 `VerticalSliceController.sceneEventSystem`，但当前 Controller 已不存在该字段；仓库内该标识符只剩这一处。`SetReference` 会抛出异常。
- 异常发生前，它已经 SaveAsPrefabAsset/SaveAssets/ForceSynchronousImport 多个 Prefab、材质和 importer，因此失败不是原子回滚。
- 重建逻辑没有恢复 Combat background、Combat Top HUD 与 Gate E staged reveal 子层级；根上的 Entrance/Navigation 组件可能保留，但其 serialized refs 会指向已销毁对象。

结论：不得把该菜单作为“修复 Scene”的回退手段。Validator 只能诊断，不能调用 authoring 或静默修复。

### P1：局部 authoring 仍是有副作用的维护入口

- Gate B、汉化和 Clear UI 入口都以 `OpenSceneMode.Single` 打开并保存正式 Scene/Prefab。
- Clear UI 会重写 36 个 cell 的 default visual；汉化入口会保存 Timeline Prefab 和 Scene；Gate B 会重写 Tower/Poison/Target Prefab 与 Scene binding。
- 这些入口应继续视为显式覆盖操作，执行前必须单独审查工作树；Validator 不应复用它们。

### P1：Automation 的历史证据入口已经过期并重复

- 文件内 4 条 BuildPlayer 路径都只构建 `CombatVerticalSlice.unity`，不符合当前 Bootstrap 起始的六 Scene 正式 build。
- `BuildValidateAndCapture()` 会串行调用多个旧 Gate capture，再复制证据，职责过宽。
- Turn Lifecycle Gate B 仍断言 `CardHandHost.CardCount == 7`（297），当前正式 battle flow 是 5 张手牌。
- Remaining Cards summary 仍写 `savedPrefabs: 8`（1886），当前维护契约为 11 个 Battle Prefab，另有 Combat Shell 保存 Prefab。
- 8 个 evidence-directory helper 重复 repository root 解析，校验规则还不一致。

首切片不要重构这些历史入口；只把它们登记为后续工具债务，避免 Validator 依赖其行为或输出。

## 7. SceneContractValidator 最小冻结契约

### 输入

- 显式 Unity asset path；首版只支持正式 Combat Scene 与 Bootstrap Scene 的内置 contract catalog。
- 调用方可以从菜单或 EditorWindow 选择 contract，但不能传入“自动修复”选项。
- 验证时若目标 Scene 未打开，以 Additive 只读方式打开；结束时只关闭 Validator 自己打开的 Scene，并恢复 active Scene。

### 规则

- `NODE_REQUIRED` / `NODE_UNIQUE`：稳定 root/path 存在且唯一。
- `COMPONENT_REQUIRED` / `COMPONENT_UNIQUE`：路径上的关键组件存在且数量正确。
- `SERIALIZED_REFERENCE_REQUIRED`：Controller、Composition、Binding、Presenter、entry 的冻结引用非空且指向目标 Scene/预期 Prefab。
- `PREFAB_SOURCE_REQUIRED`：36 TimelineCell 及 BattleFlow/background/top HUD 的 Scene instance 来源正确；动态 View 字段指向正式 Card/terrain/target/frame/occupant/status Prefab。
- `BOOTSTRAP_SERVICE_UNIQUE`：Bootstrap 恰好一个 EventSystem/service set，内容 Scene 为零。
- `SCENE_DEFAULT_STATE`：Combat root inactive、entry active，entry 指向正确 content root/camera/interaction group。
- `NO_MUTATION`：不保存、不 `SetDirty`、不执行 authoring、不改 importer，不改变调用前 dirty/open/active Scene 状态。

### 最小诊断 DTO

```csharp
public enum SceneContractSeverity
{
    Warning,
    Error
}

public sealed class SceneContractDiagnostic
{
    public string ContractId { get; }
    public SceneContractSeverity Severity { get; }
    public string AssetPath { get; }
    public string ObjectPath { get; }
    public string PropertyPath { get; }
    public string Message { get; }
    public string Remediation { get; }
}

public sealed class SceneContractReport
{
    public string AssetPath { get; }
    public IReadOnlyList<SceneContractDiagnostic> Diagnostics { get; }
    public bool IsValid { get; }
}
```

首版不需要通用 DSL、反射式插件注册、Service Locator、EventBus 或自动修复器。诊断按 `Severity -> ContractId -> ObjectPath -> PropertyPath` 稳定排序，便于 JSON/测试比较。

## 8. 建议 EditMode 测试矩阵

| 测试 | 核心断言 | fixture/副作用 |
| --- | --- | --- |
| ProductionCombatScene | 正式 Scene `Error == 0` | 只读正式资产 |
| ProductionBootstrap | 唯一 EventSystem/service set，内容 entry 为 0 | 只读正式资产 |
| MissingNode | 返回唯一 `NODE_REQUIRED`，含精确 object path | 临时未保存 Scene |
| DuplicateNode | 返回 `NODE_UNIQUE` 和实际数量 | 临时未保存 Scene |
| MissingComponent | 返回组件全名与路径 | 临时未保存 Scene |
| NullSerializedReference | 返回 component/property path | 临时未保存 Scene |
| WrongPrefabSource | 返回 expected/actual asset path | 临时测试 Prefab/Scene，测试结束删除自身 fixture |
| ProductionPrefabBindings | 11 个 Battle Prefab、Shell Prefab 与全部 serialized prefab refs 匹配 | 只读正式资产 |
| TimelineCells | 36 个唯一坐标且全部来自 TimelineCell Prefab | 只读正式资产 |
| SceneEntryDefaults | root/entry active 状态及三条 entry refs 正确 | 只读正式资产 |
| NoMutation | Scene 文件 SHA-256、dirty flag、open Scene 集合、active Scene 前后相同 | 调用两次 Validator |
| DeterministicReport | 两次结果顺序和内容完全一致 | 不含动态时间戳 |
| MenuAndWindowSmoke | 菜单可打开窗口，执行后显示 pass/error summary | Editor-only，不打开 Player |

测试不得调用 `VerticalSliceSceneAuthoring`、`VerticalSliceAutomation` 或保存正式 Scene。

## 9. 必须由主智能体处理的最小 diff

1. 新增 `unity/Assets/_Project/Editor/AssetTooling/SceneContracts/**`：只读 validator、DTO、内置 Combat/Bootstrap contract、菜单和 EditorWindow。
2. 新增 `unity/Assets/_Project/Tests/EditMode/AssetTooling/**`：上述独占测试与临时 fixture helper。
3. 由主智能体决定一个最小 asmdef 变更：优先新增独立 Editor-only test asmdef 引用 `TimeKey.Editor` 与 TestAssemblies；不要让 Domain/Application 引用 Editor/UnityEngine。
4. 不修改现有 Runtime/Application/Presentation/Composition/Infrastructure、正式 Scene/Prefab、ProjectSettings、`manifest.json` 或 `packages-lock.json`。
5. 不在首切片重构/删除旧 authoring 和 automation。维护文档只需明确它们是 legacy destructive/write harness，并记录后续清理候选。

## 10. 停止与交回

- 本审计未发现必须重写正式 Scene/Prefab 才能实现 Validator 的条件。
- 当前正式资产已有足够冻结信息支持只读诊断；建议 Gate B 继续实现 `SceneContractValidator`。
- 若实现过程中只能通过保存 Scene、补写正式引用或运行旧 authoring 才能让生产 smoke 通过，应立即停止并把该差异作为独立 Scene 修复任务交回主智能体。

## 11. 实现后复审

> 复审日期：2026-08-03
> 范围：`unity/Assets/_Project/Editor/AssetTooling/SceneContracts/**`、`unity/Assets/_Project/Tests/EditMode/AssetTooling/**`
> 方式：只读静态复审；未启动 Unity/Godot/Blender，未修改代码、Scene、Prefab、Packages、ProjectSettings 或测试资产

### Findings

#### P1：冻结的 Scene 对象引用只检查非空，错误接线仍会返回 PASS

`SceneContractValidator.cs:413-438` 与 `491-508` 把 Controller/Binding 的稳定 Scene 引用交给 `CheckReferences()`；`CheckNonNullReference()` 在 `836-865` 只有当调用方传入 expected Prefab path 时才比较目标，否则任意非空 `UnityEngine.Object` 都通过。因此 required path 仍存在时，把 `controller.timelineRoot`、`controller.sceneCamera` 或 `CombatPresentationBinding.timelinePresenter` 指向同 Scene 内另一个对象，报告仍可为 PASS，但运行时会绑定错误层级。该行为不满足本报告第 7 节冻结的“非空且指向目标 Scene/预期 Prefab”契约。

建议让 Validator 从已解析的稳定对象建立 expected-reference 表，比较具体对象/组件；增加一个只修改临时 fixture 的 wrong-scene-reference 负例，并断言精确 component/property path。现有四项测试只覆盖 production、read-only、missing root 和窗口结构，无法捕获该漏报。

#### P1：Timeline 与 occupant 集合缺少运行时必需的语义校验，损坏配置可被误判为有效

`SceneContractValidator.cs:538-576` 只要求 36 个不同的 `TimelineCellView` 对象来自正确 Prefab，没有验证 36 个 `Coordinate` 唯一；36 个各自独立但坐标重复的实例会通过 Validator，随后由 `TimelinePresenter.ValidateConfiguration()` 因重复坐标抛异常。`SceneContractValidator.cs:645-674` 对 `creationViews` 只要求数组非空且引用某个 `.prefab`，没有检查非空/唯一 `creationId`、正式 Tower Prefab 来源或 `CombatOccupantView` 组件；任意无关 Prefab 也可通过，随后由 `CombatOccupantPresenter.ValidateConfiguration()` 抛异常。

建议按冻结契约增加 timeline coordinate set 和 creation ID -> reviewed Prefab/component 检查，并分别加入重复坐标、空 creation ID、错误 occupant Prefab 负例。首版无需引入通用 schema/DSL。

#### P1：视觉证据 update 回调异常时不会退订、关闭窗口或退出 Unity

`SceneContractValidatorEvidenceAutomation.cs:63-90` 只在全成功路径执行 `EditorApplication.update -= Tick`、关闭窗口和 `EditorApplication.Exit(0)`；`_window.Repaint()`、`ReadScreenPixel()`、PNG/JSON 写入中的任一异常，或人工提前关闭窗口导致 `_window` 失效，都会让 Tick 异常退出而 update 订阅继续存在。该入口为了跨帧截图通常不能依赖同步 `-quit`，所以失败时可能留下 Unity 写入进程和工程锁，直接违反本阶段“异常退出后进程结束并交回所有权”的门禁。

建议用单一 `Finish(exitCode, exception)` 在 `try/catch/finally` 语义下退订、关闭仍有效的窗口、销毁临时 Texture、写失败摘要并非零退出；重复 Run 也应先清理既有静态订阅。至少增加可注入 capture/write failure 的 Editor 测试，验证只退出一次且不残留回调。

### 未发现问题的边界

- `ValidateCombatScene()` 未调用 authoring、SaveScene、SetDirty 或 importer 写入；它只关闭自己 additive 打开的目标 Scene，并比较文件 SHA-256、dirty/open/active setup。静态复审未发现正式 Scene/Prefab 写入路径。
- `TimeKey.Editor.AssetTooling.asmdef` 与独占测试 asmdef 均限定 Editor；Runtime asmdef 未反向引用工具程序集。静态复审未发现 Player assembly 泄漏。
- EditorWindow 使用 UI Toolkit，Silver 字体引用位于 Editor 层；没有改写现有战斗 uGUI、Domain/Application 结果或 SceneFlow。
- 实现没有新增 Service Locator、EventBus、全局可变玩法状态或运行时稳定树生成逻辑；当前复杂度仍可保持在首切片范围。

### 复审验证命令

```powershell
rg -n "CheckReferences|CheckNonNullReference|timelineCells|creationViews|EditorApplication.update|ReadScreenPixel|EditorApplication.Exit" unity/Assets/_Project/Editor/AssetTooling/SceneContracts unity/Assets/_Project/Tests/EditMode/AssetTooling
rg -n "ValidateConfiguration|duplicate timeline coordinate|creation Prefab registration" unity/Assets/_Project/Runtime/Presentation
git status --short -- unity/Assets/_Project/Editor/AssetTooling/SceneContracts unity/Assets/_Project/Tests/EditMode/AssetTooling docs/migration/unity-3d/03-workstreams/agents/reports/pluginization-code-audit.md
git diff --check -- docs/migration/unity-3d/03-workstreams/agents/reports/pluginization-code-audit.md
```

上述均为只读/文本检查；按所有权约束未运行 Unity 测试。主智能体仍需在修复后运行独占 EditMode、窗口实际截图、build 与 Player smoke。
