# Deck & Battle Flow Agent 0：Godot 源语义审计

> 单一目标：只读核验 Wave 02B4 的 Godot 权威行为，并把准确顺序、数据和冲突写入独占报告；不得修改任何代码或共享文档。

## 必读

- `00-bootstrap/NEXT_STAGE_DECK_AND_BATTLE_FLOW_PROMPT.md` 的“主 Prompt”
- `02-architecture/adr/0008-turn-lifecycle-intents-and-action-presentation.md`
- `agents/reports/turn-lifecycle-gate-d-final.md`
- Godot `CardSystemBootstrap.gd`、`CardDrawFlowController.gd`、`HandDiscardFlowController.gd`
- Godot `in_scene.gd`、`TimelineManager.gd`、`global_clock.gd`、`global_timecoin.gd`
- Godot victory/defeat/settlement/reward/return-flow 模块

## 独占拥有路径

- `docs/migration/unity-3d/03-workstreams/agents/reports/deck-battle-flow-source-semantics.md`

## 禁止路径

除上述报告外的全部路径。尤其不得修改 Godot、Unity、Scene、Prefab、asmdef、证据、共享维护文档或 Git 状态。

## 必须裁决

- starter deck 的准确顺序和重复卡；牌堆 top 的方向、手牌上限、每轮抽牌数、空堆洗回和双空堆行为。
- 强制弃手牌、时间币空格计算、Timeline 结算、建筑、清空、状态、Era/phase、抽牌和 intent refresh 的可观察顺序。
- Era 为 1 起始、phase 为 1..8，推进溢出到下一 Era；时间币按 12x3 Timeline 空位和 `floor(empty * ratio)` 增加。
- 胜利、失败、settlement reward、牌组快照、返回局外 payload 的真实边界；指出哪些是战斗结算，哪些仍是局外 Godot 流程。
- Godot 使用节点/数组身份而 Wave 02B4 要求 stable card-instance identity、显式 seed 和 typed result 的迁移加强项。
- 若 Godot 顺序与 ADR 0008 reserved hook 存在表面冲突，给出不改 lifecycle 的最小裁决，不得自行修改 ADR。

## 验证与交回

报告必须引用文件和行号，列出冻结常量、顺序、异常/空状态和 Unity 可测试断言。运行 `git diff --check`；不得启动 Unity/Godot。你不是仓库唯一工作者，不得切分支、stash、暂存、commit、push 或回退他人修改。
