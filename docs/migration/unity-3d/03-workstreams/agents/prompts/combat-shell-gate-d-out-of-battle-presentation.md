# Combat Shell Gate D Agent: Out-of-battle presentation

> 单一目标：实现最小局外壳的保存式 Presentation 组件及其局部 PlayMode tests；不得接线共享 Scene。

## 启动门禁

- 分支必须为 `unity_7.31`。
- Gate C 已由主智能体提交并交回所有权。
- 你不是仓库中唯一工作者；不得回退、覆盖或格式化他人改动。
- 禁止切分支、stash、暂存、commit、push 或运行会重写共享 Scene 的 Unity authoring。

## 独占所有权

- 可新增/修改：
  - `unity/Assets/_Project/Runtime/Presentation/OutOfBattleShell/**`
  - `unity/Assets/_Project/Tests/PlayMode/OutOfBattleShell/**`
  - 与上述新文件一一对应的 `.meta`
- 只读：Application SceneFlow contracts、Composition、所有 Scene/Prefab、Editor、共享 docs/evidence。
- 禁止修改 `OutOfBattleShell.unity`、`GameOver.unity`、`CombatVerticalSlice.unity`、Bootstrap、asmdef 和共享测试。

## 冻结接口

组件只消费主智能体现有 `OutOfBattleShellState` snapshot，由主智能体在 Composition 接线；不得查找 Bootstrap、StateStore 或 SceneManager。组件必须：

- 用 Silver 字体显示 seed、时代、阶段、时间币和单一战斗房间。
- 房间有 idle、hover/focus、selected、confirming、settled/disabled 状态。
- 暴露 typed、单次的房间确认事件，事件只携带稳定 `roomId`，不构造 launch payload。
- 输入锁或转场中禁止重复确认；ESC 可从 selected 返回 idle。
- 不在运行时搭 UI 树，不持久化状态，不直接切场。

## 验证与返回

- 新增局部 PlayMode tests，覆盖 snapshot 文本、Silver 字体、hover/focus、selected、单次确认、settled 禁用、输入锁。
- 不要求运行 Unity；至少完成静态检查和 `git diff --check` 对独占文件检查。
- 返回改动清单、公开 API、测试矩阵和未运行项，明确交回所有权。
