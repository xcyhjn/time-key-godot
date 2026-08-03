# Agent Prompt: pluginization-code-audit

## 单一目标

只读审计 `VerticalSliceSceneAuthoring.cs`、`VerticalSliceAutomation.cs`、`VerticalSliceController.cs`、资源加载、Scene 查找、运行时对象生成和重复测试/证据入口；给出 `SceneContractValidator` 的最小契约与风险排序，不实现功能。

## 独占写入路径

- `docs/migration/unity-3d/03-workstreams/agents/reports/pluginization-code-audit.md`

## 禁止路径

- `unity/Packages/**`、全部 asmdef、`unity/ProjectSettings/**`
- `unity/Assets/_Project/Editor/**`、`Runtime/**`、`Tests/**`
- 正式 Scene/Prefab、共享维护/进度文档、最终证据
- Gate 0 intake 和其他 Agent 报告

## 输入

- `docs/migration/unity-3d/03-workstreams/agents/reports/pluginization-intake.md`
- `docs/migration/unity-3d/06-maintenance/scene-and-prefab-guide.md`
- 上述三个 C# 文件、相关 Scene/Prefab YAML、现有 Scene/Prefab 测试

## 输出

报告必须包含：热点计数与位置、Editor authoring/运行时动态对象分类、稳定节点/组件/serialized reference/Prefab source 契约、旧 authoring 重跑风险、最小诊断 DTO、建议测试矩阵、必须由主智能体处理的最小 diff（如有）。

## 非目标

不重构、不过度抽象、不运行 authoring、不打开 Unity、不修改任何 Scene/Prefab/代码，不设计 CardAssetAudit，不改变战斗/SceneFlow/交互契约。

## 只读检查命令

```powershell
rg -n "new GameObject|AddComponent|GameObject.Find|Resources.Load|AssetDatabase.LoadAssetAtPath|MenuItem" unity/Assets/_Project
rg -n "m_Name:|m_EditorClassIdentifier:" unity/Assets/_Project/Scenes unity/Assets/_Project/Prefabs --glob '*.unity' --glob '*.prefab'
git status --porcelain=v1 -uall
```

## 停止条件

需要写独占报告以外路径、发现活动 Unity/Godot/Blender 写锁、无法区分前置脏改动，或结论要求重写正式 Scene/Prefab 时立即停止并报告。你不是唯一在工程中工作的 Agent；不得回滚或覆盖他人改动。
