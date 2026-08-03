# Pluginization and Asset Tooling Intake

> 阶段：P0 Pluginization and Asset Tooling
> Gate 0 记录时间：2026-08-03T13:06:02+08:00
> 状态：Gate 0 通过；Gate A 只读审计完成，待 Agent Prompt Review 后进入 Gate B
> 工作区：`D:\\godot\\时之钥\\时之钥`

## 1. 执行规范与前置证据

本阶段共同遵守以下已完整读取文件：

- `docs/migration/unity-3d/00-bootstrap/START_HERE_PROMPT.md`
- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_PLUGINIZATION_AND_ASSET_TOOLING_PROMPT.md`
- `docs/migration/unity-3d/01-assessment/pluginization-and-tooling-assessment.md`
- `docs/migration/unity-3d/03-workstreams/agents/prompt-review-pluginization-and-asset-tooling.md`
- `docs/migration/unity-3d/06-maintenance/plugin-and-asset-tooling-guide.md`
- `docs/migration/unity-3d/05-progress/current-status.md`
- `docs/migration/unity-3d/02-architecture/**`、ADR 0001-0010、`docs/migration/unity-3d/06-maintenance/**`
- Effect Frame、Era Clock、Overworld Map、Combat Shell Prompt 与最近 final report/XML/visual/push/known-issues 证据。

前置闭环证据可继承：Wave 02B3R 与 Combat Shell Gate E 已关闭；最近 canonical XML 为 EditMode `351/351`、graphical Direct3D12 PlayMode `107/107`；Windows Development build 与 actual Player smoke 已通过。插件化只刷新实际受影响的 Editor、测试、build/player 和视觉面，不重新执行已关闭功能 Gate。

## 2. 原队列恢复点

P0 插入前的正式未完成队列位置为：

- Prompt：`docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_OVERWORLD_MAP_PROMPT.md`
- 准确 Gate：`Gate 0`
- 前置条件：Combat Shell Gate E、Wave 02B3R Effect Frame Stability 已关闭；SceneFlow/typed payload/outcome、顶部 UI、背景、转场、Silver 与 action identity 契约保持冻结。
- 恢复后的第一条动作：重新完整读取 Overworld Map Prompt，重新记录当时 HEAD/ahead-behind/dirty/process/lock，然后核对 Godot `scene/out_scene/**` 的局外地图选择、房间确认和海洋背景语义。
- 禁止重做：Decoupling、Remaining Cards、Deck/Battle Flow、Turn Lifecycle、Combat Shell A-E、Effect Frame 02B3R 已关闭 Gate。
- Era Clock：保留其原前置关系；只在 Overworld Prompt 指定的位置恢复，不因本 P0 改变相对顺序。

`START_HERE_PROMPT.md` 的旧排队文字仍提到 Decoupling/Remaining Cards，但当前状态、提交历史与完成证据均确认它们已关闭。按“不得重新执行已关闭 Gate”规则，本阶段结束后从 Overworld Map Gate 0 恢复。

## 3. Git 与工作区基线

- 分支：`unity_7.31`
- HEAD：`c55053cf781bea17294d36a523c568ab7e08a15b`
- origin 对比：ahead `1` / behind `0`
- 最近检查点：`c55053c` Effect Frame Stability；远端尖端 `d7dbed0` Combat Shell Gate E。
- Gate 0 dirty/untracked：`378` 项，staged `0`。
- 保护清单标准化文本 SHA-256：`d9e85aff2db9cdbaf1b9897f3fad5a9e9d11b06d0d7480be7d85793462019b75`。
- 规则：下列全部路径均早于本阶段存在，视为用户/前序任务所有；本阶段不回滚、不覆盖、不格式化、不移动、不暂存、不混入提交。

## 4. Agent、进程与锁

- Agent：`/root/deck_flow_application` shutdown；`/root/effect_frame_interaction_audit` interrupted 且不再运行；`/root/effect_frame_layout_audit` completed；无活动子 Agent。
- Unity Editor/batchmode、Godot、TimeKey Player、ShaderCompilerWorker、MSBuild/testhost：无活动进程。
- Unity Hub/授权服务：后台常驻，不指向项目写入任务。
- Blender：PID `18072` 打开 `HexTile_Dirt.blend`，窗口无未保存标记；该文件 ReadWrite + FileShare.None 独占打开成功，未持有写锁，文件时间未变化。判定为“打开但非写入”，不调用 Blender/MCP，不触碰 ArtSource。
- Blender MCP：存在多个长期空闲 server 进程；本阶段不调用、不视为工程写入所有者，结束时再次核对无 active write。
- `unity/Library/EditorInstance.json`：不存在。
- `unity/Temp/UnityLockfile`：不存在。
- `ArtifactDB-lock` / `SourceAssetDB-lock`：遗留文件存在，但独占打开成功，判定为未持有。
- 结论：上一任务 Git 检查点存在、Agent 已返回、工程写锁已释放；Gate 0 可以进入 Gate A。若上述 Blender 文件出现写锁、未保存标记或时间变化，立即退回只读模式。

## 5. Gate A 只读审计结论

### Editor 与运行时边界

- `VerticalSliceSceneAuthoring.cs`：1,352 行 / 63,825 bytes；同时创建/配置 Camera、EventSystem、Canvas、HUD、Timeline、CardHandHost、BoardRoot、多个 Prefab、字体和 importer。旧菜单重跑可能把稳定层级恢复成过期模板。
- `VerticalSliceAutomation.cs`：1,987 行 / 89,616 bytes；集中多个 Gate 的截图、build、验证、路径和摘要写入，存在重复证据入口。
- `VerticalSliceController.cs`：1,335 行 / 48,044 bytes；稳定对象均为 serialized reference，运行时只实例化动态 HexColumn 与 target prefab。当前无需重写 Controller。
- 正式 Scene 已保存 `VerticalSliceRoot`、`SliceCamera`、`CombatBoardRoot`、`HUD`、`Timeline`、`CardHandHost`、`CombatSceneEntry`；Bootstrap 保存唯一 EventSystem/transition。
- 最高风险不是运行时规则重复，而是缺少只读工具阻止旧 authoring 破坏稳定 Scene/Prefab 契约。

### Package 与 asmdef

- Unity：`6000.4.10f1`。
- 已安装：URP `17.4.0`、Newtonsoft JSON `3.2.2`、Test Framework `1.6.0`、uGUI `2.0.0` 与内置模块。
- 未安装：Input System、Cinemachine、Addressables；没有第三方 DI/Tween/Inspector 包。
- UI Toolkit/uielements 已为 Unity 6 内置模块，不需安装新包；只用于 Editor 诊断窗口。
- Domain/Application/Diagnostics 保持 `noEngineReferences=true`；新工具只进入 Editor assembly，不新增反向依赖。
- 本切片不改 `manifest.json`、`packages-lock.json`、ProjectSettings、正式 Scene/Prefab 或运行时代码。

### 资产

- Godot 非 Unity 资产粗库存：565 PNG、13 OGG、5 WAV、3 TTF、1 OTF、28 gdshader、45 tres；其中包含插件示例与证据，不能批量导入。
- Unity `Assets/_Project`：41 PNG、1 TTF、2 FBX、2 blend、6 Material；未发现项目音频副本或 Unity shader 资产。
- 七张真实卡图 `earthquake/lighting/poison_card/recover/tornado/tower_card/wind` 的 Godot 源与 Unity 副本 SHA-256 分别完全一致；`lighting` 稳定拼写不修正。
- Silver 有作者、来源、CC BY 4.0 署名与生产预算/收入超过 USD 100,000 时联系作者的附加发布复核项。
- Godot 一般图片、13 OGG、ARK Pixel 字体、Blender/MCP 产物仍缺完整公开发布授权链，状态保持 `UNVERIFIED`，不得新增导入或发布声明。

## 6. 首个实现切片决定

选择：`SceneContractValidator`。

依据：

1. 正式 Scene/Prefab 稳定节点已经形成可冻结契约。
2. 旧 authoring 文件覆盖面大且模板已出现落后风险，误运行的影响横跨 Camera、EventSystem、HUD、Timeline、CardHandHost 和 Prefab。
3. 核心卡图当前字节一致，CardAssetAudit 可留作第二候选，不应在首切片同时实现。
4. Validator 可以完全 Editor-only、只读执行、结构化输出并由独占 EditMode 测试覆盖，不触碰 Domain/Application 结果。

最小冻结契约：

- 输入：显式 Scene path + 不可变 contract definition；默认正式 Combat Scene。
- 输出：`SceneContractReport` + `SceneContractDiagnostic`，包含 severity、contract ID、asset path、object path、message、remediation。
- 检查：稳定节点存在/唯一、组件存在、Prefab source、serialized reference、Bootstrap 唯一 EventSystem、稳定节点非运行时生成。
- 执行：菜单和 EditorWindow；只读打开 Scene，不保存、不静默修复、不执行 authoring。
- UI：使用 Unity 6 内置 UI Toolkit，不新增包。
- 测试：独占 EditMode fixture + 正式 Scene smoke；重复执行幂等；失败路径精确定位。
- 停止条件：任何实现需要修改正式 Scene/Prefab、Domain/Application、ProjectSettings 或 package manifest 时停止并交回主智能体。

## 7. 完整前置保护清单

```text
     M default_bus_layout.tres
     M docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_TURN_LIFECYCLE_PROMPT.md
     M docs/migration/unity-3d/00-bootstrap/START_HERE_PROMPT.md
     M docs/migration/unity-3d/02-architecture/migration-roadmap.md
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/combat-shell-entrance-1280x720-complete.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/combat-shell-entrance-1280x720-initial.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/combat-shell-entrance-1280x720-middle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-1280x720-initial.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-1280x720-middle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-1920x1080-initial.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-1920x1080-middle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-2560x1080-initial.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-2560x1080-middle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-1280x720-entrance-initial.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-1280x720-entrance-middle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-1920x1080-entrance-initial.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-1920x1080-entrance-middle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-2560x1080-entrance-initial.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-2560x1080-entrance-middle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/game-over-1280x720-defeat.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/game-over-1920x1080-defeat.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/game-over-2560x1080-defeat.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-confirming.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-hover.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-idle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-selected.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-settled.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-confirming.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-hover.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-idle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-selected.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-settled.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-confirming.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-hover.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-idle.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-selected.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-settled.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/animation-timeline.json
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/combat-reveal-complete.png
     M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/combat-reveal-middle.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1280x720-hover.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1280x720-idle.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1280x720-selected.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1920x1080-hover.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1920x1080-idle.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1920x1080-selected.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/2560x1080-hover.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/2560x1080-idle.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/2560x1080-selected.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-0.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-180.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-270.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-90.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/timeline-invalid.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1280x720-earthquake-selected.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1280x720-idle.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1280x720-lighting-selected.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1920x1080-earthquake-selected.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1920x1080-idle.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1920x1080-lighting-selected.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-2560x1080-earthquake-selected.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-2560x1080-idle.png
     M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-2560x1080-lighting-selected.png
     M docs/migration/unity-3d/05-progress/current-status.md
     M scene/in_scene/rewards/resources/default_craft_recipe_book.tres
     M shaders/color_BG.gdshader
     M shaders/game_over.gdshader
     M unity/ProjectSettings/ProjectSettings.asset
     M unity/ProjectSettings/URPProjectSettings.asset
    ?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_COMBAT_SHELL_AND_SCENE_FLOW_PROMPT.md
    ?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md
    ?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_EFFECT_FRAME_STABILITY_PROMPT.md
    ?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_ERA_CLOCK_ANIMATION_PROMPT.md
    ?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_PLUGINIZATION_AND_ASSET_TOOLING_PROMPT.md
    ?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_REMAINING_CARDS_PROMPT.md
    ?? docs/migration/unity-3d/01-assessment/pluginization-and-tooling-assessment.md
    ?? docs/migration/unity-3d/03-workstreams/agents/prompt-review-pluginization-and-asset-tooling.md
    ?? docs/migration/unity-3d/04-validation/combat-shell-gate-b/editmode-targeted-final.log
    ?? docs/migration/unity-3d/04-validation/combat-shell-gate-b/editmode-targeted-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-0/godot/game-over-960x540.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-0/godot/game-start-960x540.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-0/godot/game-start.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-0/godot/in-scene-960x540.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/build-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/build-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/build-review-closure-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/build-transaction-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full-3.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full-3.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-remediation-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-remediation-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-review-closure-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-review-closure-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-review-closure-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-scene-flow-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-scene-flow-transaction.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-scene-flow-transaction.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-scene-flow.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-transaction-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-transaction-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/player-smoke-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/player-smoke-review-closure-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/player-smoke-transaction-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-card-lock-review-closure.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-card-lock-review-closure.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-final-3.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-final-3.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-rendered-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-rendered-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-review-closure-rendered-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-scene-flow-review-closure.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-scene-flow-review-closure.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-transaction-rendered-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-transaction-rendered-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/build-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/build-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/build.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-final-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-full.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow-assets.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow-assets.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/player-smoke-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/player-smoke-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/player-smoke-visible.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/player-smoke.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-final-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-full.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow-frame.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow-frame.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/scene-authoring-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/scene-authoring.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-3.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-material-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-review-closure.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-post-build-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/build-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture-material-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture-review-closure-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture-review-closure.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-gate-b-scene-flow-review-closure-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-gate-b-scene-flow-review-closure-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-gate-b-scene-flow-review-closure.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-gate-b-scene-flow-review-closure.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-post-build-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-targeted-material-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-targeted-material-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/player-smoke-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-review-closure-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-review-closure-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-review-closure.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-review-closure.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-entrance-reuse-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-entrance-reuse-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-full-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-full.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-scene-flow-review-closure.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-scene-flow-review-closure.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-targeted-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-targeted-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/author-gate-c.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/editmode-asset-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/editmode-asset-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/editmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-component-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-component-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-scene-flow-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-scene-flow-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-visual-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-visual-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/author-gate-d-canvas-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/author-gate-d-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/author-gate-d.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-assets-post-format.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-assets-post-format.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-full-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-full-final-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-full-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-full-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-sceneflow-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-sceneflow-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final-3.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final-3.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-gate-c-visual-regression.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-gate-c-visual-regression.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-out-of-battle-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-out-of-battle-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-roundtrip-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-roundtrip-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/author-final-reveal.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/author-layered-reveal.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/author-ocean-and-enemy-reveal.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build-delivery-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build-delivery-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build-post-review.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-assets-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-build-smoke-compile.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-full-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-full.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-delivery-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-delivery-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-exit-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-post-review.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-visible.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-combat-entrance-post-review.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full-delivery.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-layered-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-post-review.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-targeted-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-targeted-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-ocean-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-ocean-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-3.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-3.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-4.log
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-4.xml
    ?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-a/editmode-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-a/editmode.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-b/editmode-application.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-b/editmode-integration.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-b/editmode.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-b/playmode.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/editmode-scene.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-battle-flow-integration.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-full.xml
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-regression.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-targeted-graphical.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-targeted.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-targeted.xml
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/scene-authoring.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/build-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/build.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/capture-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/capture.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode-victory-rule.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode-victory-rule.xml
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode.xml
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/player-smoke-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/player-smoke.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-battleflow-rule.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-battleflow-rule.xml
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-battleflow-smoke.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-battleflow-smoke.xml
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode.log
    ?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-0/editmode-contracts-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-0/editmode-contracts.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-0/editmode-gate0.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-0/playmode-geometry-gate0.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/editmode-controller.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/editmode-controller.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/editmode-post-controller.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea-3.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea-3.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-resize-debug.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-resize-debug.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-b/playmode-geometry-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-b/playmode-geometry-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-b/playmode-geometry-poison-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice-3.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice-3.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-cancel-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-final-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-final-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-2.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-3.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-3.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-4.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-4.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-5.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-5.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-6.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-6.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-settled-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-stable-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-stable-final.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport-2.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport-3.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport-3.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/build-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/build.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/editmode-full-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/editmode-full.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/editmode-full.xml
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/player-smoke-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/player-smoke.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/playmode-full-graphical-final.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/playmode-full-graphical.log
    ?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/playmode-full-graphical.xml
    ?? docs/migration/unity-3d/06-maintenance/plugin-and-asset-tooling-guide.md
    ?? unity/Assets/InitTestScene4fa99104-2a41-4d45-95ce-47775840620a.unity
    ?? unity/Assets/InitTestScene4fa99104-2a41-4d45-95ce-47775840620a.unity.meta
    ?? unity/TestResults/effect-frame-geometry-targeted-final.xml
    ?? unity/TestResults/effect-frame-geometry-targeted.xml
    ?? unity/TestResults/timeline-action-frame-existing.xml
    ?? unity/docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/author-gate-c-review-fix.log
```

## Gate E Closure

- Gate 0 through Gate E: closed on 2026-08-03.
- Protected baseline inventory: all 378 entries still match exactly. Four
  unrelated Overworld/UI Theme documents appeared after Gate 0; they are treated
  as external protected work and remain untouched and unstaged.
- First slice: Editor-only SceneContractValidator; no package or external asset
  installed.
- Canonical verification: validator 6/6, full EditMode 357/357, full PlayMode
  107/107, six-scene Windows build PASS, strict Player smoke 3/3.
- Original queue restore point remains
  `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md`, Gate 0.
- Newest explicit queue instruction: after checkpoint and complete ownership
  release, send the supplied Wave 03R Era Clock handoff verbatim to a completely
  fresh agent without inherited chat context.
