# Combat Shell Agent C：GameStart / MainMenu / Transition visuals

你不是仓库唯一工作者。保护所有既有脏改，不得回退、stash、stage、commit、push、切分支或运行 Unity/Godot。

## 独占写入

- 新增 `unity/Assets/_Project/Runtime/Presentation/GameStart/**`
- 新增 `unity/Assets/_Project/Runtime/Presentation/MainMenu/**`
- 新增 `unity/Assets/_Project/Runtime/Presentation/TransitionVisuals/**`
- 新增 `unity/Assets/_Project/Prefabs/Shell/**`
- 新增 `unity/Assets/_Project/Animations/Shell/**`
- 新增对应局部 `unity/Assets/_Project/Tests/PlayMode/{GameStart,MainMenu,TransitionVisuals}/**`
- 自己的 `docs/migration/unity-3d/03-workstreams/agents/reports/combat-shell-agent-c-menu-transition.md`

禁止修改任何既有文件、正式 Scene、asmdef、Build Settings、SceneFlow runtime、Combat 规则/表现、Editor harness、共享文档和其他 Agent 路径。

## 目标

按源审计提供可接线的三字启动表现、中央大钟主菜单、六个非对称按钮、Continue disabled、设置/种子/退出弹层，以及只负责视觉 completion 的 cover/loading/reveal 组件。Presentation 不得直接加载 Scene、创建 payload/outcome 或持久化状态；所有命令通过 typed view callback 交给 SceneFlow。

动画禁用或 speed=0 时立即应用终态并报告 completion；不得用固定延时猜测。pointer、keyboard、ESC、弹层焦点与重复点击需可测试。全部中文使用现有 Silver Font/Material；共享 Scene 接线和最终视觉证据由主智能体完成。
