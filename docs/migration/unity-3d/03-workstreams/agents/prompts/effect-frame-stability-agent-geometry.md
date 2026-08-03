# Wave 02B3R Geometry Agent Prompt

> 状态：Gate 0 后启动；只拥有 Action frame 布局与真实轮廓
> 负责人：effect-frame-layout-agent
> 最后验证日期：2026-08-03

## 单一目标

让 `TimelineActionFrame` 在首次 Canvas rebuild、父级尺寸变化、动态 resize、隐藏/复用、Scene rebind 后按 layout signature 重新吸附，并以真实 occupied-cell 联合绘制底板和外边缘，不填充 Tower/Poison 缺格。

## 必读资料

- `00-bootstrap/START_HERE_PROMPT.md`
- `00-bootstrap/NEXT_STAGE_EFFECT_FRAME_STABILITY_PROMPT.md`
- `03-workstreams/integration-contracts.md`
- `03-workstreams/agents/reports/turn-lifecycle-interaction-visual-audit.md`
- `unity/Assets/_Project/Runtime/Presentation/Actions/TimelineActionFrame.cs`
- `unity/Assets/_Project/Tests/PlayMode/Actions/TimelineActionFramePresentationTests.cs`
- `unity/Assets/_Project/Prefabs/Battle/UI/TimelineActionFrame.prefab`

## 独占写入范围

- `unity/Assets/_Project/Runtime/Presentation/Actions/**`
- `unity/Assets/_Project/Tests/PlayMode/Actions/EffectFrameGeometryStabilityTests.cs`（可新建）
- `unity/Assets/_Project/Prefabs/Battle/UI/TimelineActionFrame.prefab` 及其必要局部 `.meta`
- `docs/migration/unity-3d/03-workstreams/agents/reports/effect-frame-geometry-agent.md`

## 禁止写入

不得修改 `TimelinePresenter.cs`、`CombatPresentationBinding.cs`、`VerticalSliceController.cs`、正式 Scene、Composition、asmdef、Build Settings、共享文档、历史证据或其他 Agent 路径。不得运行 Unity/Godot、stage/commit/push、stash、切分支或回退他人改动。你不是仓库唯一工作者。

## 冻结契约

- ActionLayer 继续 `ignoreLayout=true`，不是 Timeline GridLayout 的第 37 项；frame/detail Graphic 不阻断 raycast。
- 1x1、直线、L/T、缺角矩形、Tower `010,111`、Poison `110,111` 和相邻同色 Action 均只绘制真实格集合。
- 每个真实 occupied cell 归属同一 ActionId；相邻 Action 不能合并 identity。
- 只在 snapshot、occupied cells 或父级 layout signature 变化时重算 geometry，禁止每帧无条件分配/重建。
- 视觉对象从保存 Prefab/局部序列化原型复用，不能运行时用空 GameObject+AddComponent 搭稳定 UI 树。

## 实现与验证

1. 添加最小布局 signature/invalidator 与 per-cell/edge geometry，不引入通用多边形框架。
2. 先新增 Gate 0 几何回归：对当前外接矩形实现应必然失败，断言缺格不着色、真实格同 ActionId、父级 1280→2560→1920 重新吸附和复用不漂移。
3. 运行独立 PlayMode filter；每个测试断言 RectTransform/cell 几何关系和 raycast，而非仅对象数量/文本。

## 完成回报

报告列出布局签名、轮廓算法、Prefab/测试改动、命令与结果、视觉证据需求和主集成接线点。发现共享契约冲突停止写入并报告。
