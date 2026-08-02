# Combat Shell Agent B：TopHUD / Background

你不是仓库唯一工作者。保护所有既有脏改，不得回退、stash、stage、commit、push、切分支或运行 Unity/Godot。

## 独占写入

- 新增 `unity/Assets/_Project/Runtime/Presentation/CombatShell/**`
- 新增 `unity/Assets/_Project/Prefabs/Battle/CombatShell/**`
- 新增 `unity/Assets/_Project/Materials/Battle/Background/**`
- 新增 `unity/Assets/_Project/Tests/PlayMode/CombatShell/**`
- 自己的 `docs/migration/unity-3d/03-workstreams/agents/reports/combat-shell-agent-b-combat-shell.md`

禁止修改任何既有文件、Scene、asmdef、Build Settings、Controller、Composition、Binding、Editor harness、共享文档和其他 Agent 路径。不得写 SceneFlow/Menu。

## 目标

为 Gate B 提供局部可接线的 TopHUD 与 3D 背景组件/Prefab/材质。延续 Godot 的金棕顶部条、中央时钟、敌方总生命与地图层次，但复用 02B4 `BattleFlowPanel` 的时间币/牌区真相，不复制资源状态。全部玩家可见中文使用现有 Silver Font/Material。

组件必须适配 1920x1080、1280x720 和超宽，稳定尺寸且不遮挡时间轴、手牌、牌库/弃牌。背景只补现有 3D 棋盘远景/海面/雾层，不改规则、相机轨道或目标判定。测试只验证本地 Prefab/Presenter；共享 Scene 接线和最终截图由主智能体完成。
