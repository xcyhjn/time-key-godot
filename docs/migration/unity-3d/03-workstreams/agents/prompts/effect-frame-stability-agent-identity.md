# Wave 02B3R Identity Agent Prompt

> 状态：Gate 0 后启动；只拥有新增身份链接与索引
> 负责人：effect-frame-identity-agent
> 最后验证日期：2026-08-03

## 单一目标

建立 `TimelineActionIdentity`、可选 `CardInstanceId`、stable content ID 与地图 runtime identity 的不可变 typed link/index，使重复 stable ID、提交后离手卡牌、地图 source/target/occupant 和 Timeline 任一格都映射到同一 action snapshot。

## 必读资料

- `00-bootstrap/START_HERE_PROMPT.md`
- `00-bootstrap/NEXT_STAGE_EFFECT_FRAME_STABILITY_PROMPT.md`
- `03-workstreams/integration-contracts.md`
- `03-workstreams/agents/reports/turn-lifecycle-interaction-visual-audit.md`
- `unity/Assets/_Project/Runtime/Application/TimelineActionPresentationSnapshot.cs`
- `unity/Assets/_Project/Runtime/Application/CombatApplicationModels.cs`
- `unity/Assets/_Project/Runtime/Presentation/Cards/CardHandPresenter.cs`

## 独占写入范围

- `unity/Assets/_Project/Runtime/Presentation/Identity/**`（仅新增文件）
- `unity/Assets/_Project/Tests/EditMode/Identity/**`（仅新增文件）
- `docs/migration/unity-3d/03-workstreams/agents/reports/effect-frame-identity-agent.md`

## 禁止写入

不得修改 `CombatPresentationBinding.cs`、`TimelinePresenter.cs`、`CardHandPresenter.cs`、`CardHandHost.cs`、`VerticalSliceController.cs`、地图 View、正式 Scene/Prefab、Application/Domain、asmdef、Build Settings、共享文档或历史证据。不得运行 Unity/Godot、stage/commit/push、stash、切分支或回退他人改动。你不是仓库唯一工作者。

## 冻结契约

- `TimelineActionIdentity` 是唯一 action 键，不能以卡名、颜色、坐标或对象引用替代。
- `CardStableId` 是内容身份；`CardInstanceId` 是手牌实例身份；两者均不得充当 action identity。
- 一个 action 最多关联一个来源 `CardInstanceId`；同 stable ID 的不同实例必须得到不同 link。
- Link/index 必须可由 Timeline cell、地图 source/target/occupant 或 CardInstanceId 反查同一 immutable snapshot，未知或陈旧 link 返回显式 miss，不按 Dictionary 遍历顺序兜底。
- 清除、死亡、refresh、rebind 后索引按 ActionId 原子移除且幂等。

## 实现与验证

1. 添加 Unity-free immutable link/value 与双向索引，不解析 Label/颜色，不持有 Unity Object。
2. 为重复 stable ID、来源卡离手、同 Action 多格、相邻同色 Action、地图反查、陈旧 link 清理写 NUnit EditMode 测试。
3. 运行独立 filter，报告证据和主集成需要的查询 API；不直接接入共享 Binding。

## 完成回报

报告列出公开 API、identity 分离证明、测试命令/结果、失败项和主集成接线建议。遇到共享契约冲突停止写入并报告。
