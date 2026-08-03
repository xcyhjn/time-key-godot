# Wave 03P 局外移动与 UI 主题 Prompt 所有权审查

> 审查日期：2026-08-03
> 被审查 Prompt：`../../00-bootstrap/NEXT_STAGE_OVERWORLD_MOVEMENT_AND_UI_THEME_PROMPT.md`
> 结论：设计审查通过；执行时必须在 Gate 0 重新核对实际 dirty inventory 与进程

## 排队结论

- 本 Prompt 不打断当前 P0 插件化/资产工具任务。
- 只有当前任务完成精确 Git 检查点、全部 Agent 返回、全部 Unity/Godot/Blender 写入进程结束并交回所有权后，才可进入实现 Gate。
- 本 Prompt 完成后再进入 `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md`；后者必须继承本阶段的移动、主题、SceneFlow 和测试契约，不重复创建。

## 所有权矩阵

| 所有者 | 唯一可写范围 | 禁止范围 |
| --- | --- | --- |
| Movement Domain Agent | 新增 `Runtime/Domain/OverworldMovement/**`、对应新增 EditMode tests、独占报告 | 既有 SceneFlow state/payload、Presentation、Scene/Prefab、asmdef |
| Theme Core Agent | 新增 `Runtime/Presentation/Theming/**`、新增 `Editor/ThemeMigration/**`、对应新增 tests、独占报告 | 既有 View/Presenter、正式 Scene/Prefab、manifest/lock、共享 asmdef |
| Movement Presentation Agent | 新增 `Runtime/Presentation/OverworldMovement/**`、新增局部 prototype Prefab/PlayMode tests、独占报告 | 正式 `OutOfBattleShell` Scene/Prefab、SceneFlow、Theme core、Git |
| 主智能体 | 既有 `OutOfBattleShellState/Presenter/RoomView`、Composition/SceneFlow、正式 Scene/Prefab、主题 `.asset`、共享 asmdef、证据/共享文档/Git | 不把未审查 Agent 输出直接整批暂存 |

## 并发审查

- Gate 0 的三个 Agent 可并行只读审计，但不得同时启动 Unity/Godot/Blender 写入。
- Movement Domain 与 Theme Core 的纯代码实现可在白名单不重叠且不运行 Unity 时并行。
- Movement Presentation 依赖 Domain contract 冻结，必须后置。
- 所有 `.meta`、asmdef、正式 Prefab/Scene、主题资产创建、Unity import、测试、截图和 Build 由主智能体串行执行。
- 任一 Agent 发现必须修改共享路径时，只在独占报告提交最小 diff 建议并停止该路径写入。

## 契约审查

- 地图状态留在 Domain/Application；Theme 不知道地图规则。
- Presenter 只消费语义状态；Theme 只提供视觉数据。
- SceneFlow 继续使用既有 typed payload/outcome 与幂等边界，不创建 Dictionary/object 旁路。
- 稳定 Camera、Canvas、ThemeScope、MapHost、EdgeHost、PlayerMarker、详情层和 Prefab 保存于 Scene/Project；只有数据驱动重复节点从 Prefab 实例化。
- Godot 二进制 Theme 的提取属于 Editor/审计任务，不允许 Player 运行时解析。

## 需在执行 Gate 0 重新验证

1. P0 插件化任务是否已经修改 asmdef、Editor 工具目录或候选账本。
2. `NEXT_STAGE_ERA_CLOCK_ANIMATION_PROMPT.md` 是否仍在排队且是否占用局外正式 Scene/Prefab。
3. 正式 OutOfBattle Scene/Prefab 是否在本 Prompt 开始前已有新提交。
4. Godot `MainMenu.theme`/`OptionsMenu.theme` 的可导出字段、原素材授权和 Unity GUID。
5. 当前实际 EditMode/PlayMode/build/Player 基线，不能只继承旧口头数字。

## 审查判定

在当前设计层面，白名单互斥、依赖方向与串行 Unity 所有权成立。执行 Gate 0 若发现路径已经被 P0 工具任务占用，主智能体必须重写白名单或等待交回；不能依赖本审查越过实时冲突。
