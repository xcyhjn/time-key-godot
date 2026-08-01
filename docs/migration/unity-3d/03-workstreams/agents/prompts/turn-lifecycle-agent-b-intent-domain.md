# Turn Lifecycle Agent B1：Enemy Intent 纯数据与 Handler

> 单一目标：在 Gate A 已冻结的 ActionId/snapshot/lifecycle API 上新增真实 occupant intent 候选、确定性调度、重判与显式 unsupported result；只负责 Unity-free Domain/Application 与独占测试。

## 必读

- 本阶段主 Prompt、三份 `turn-lifecycle-*-audit.md`、Gate A 报告/API
- Godot `TimelineManager.gd`、intent candidate/priority/target resolver、`enemy_intent_resolver.gd`、`village.gd`、`EffectProcessor.gd`
- 当前 occupant/board/timeline/result API

## 独占拥有路径

- 新目录 `Runtime/Domain/Intents/**` 及 `.meta`
- 新目录 `Runtime/Application/Intents/**` 及 `.meta`
- 新目录 `Tests/EditMode/Intents/**` 及 `.meta`
- 独占报告 `agents/reports/turn-lifecycle-agent-b-intent-domain.md`

## 禁止路径

所有既有共享 Domain/Application 文件、Presentation、Composition、Infrastructure、Controller、Binding、Scene、Prefab、Editor、asmdef、共享文档/证据/Git 与 Godot 源。

## 冻结契约

- 候选来自仍存在、允许生成意图的 typed occupant；含 intent/action ID、source runtime ID/coord、target identity/coord、literal shape、priority、effect range/display payload、validity/reason。
- 高优先级先放；同级候选与 origin 使用显式 seed；最多 5 个；不得依赖集合枚举顺序或全局 RNG。
- 保留 source literal shape，包括 Village `011 -> (1,0),(2,0)`。
- Resolve 前重判 source identity/coord/alive/capability、target、shape 和 effect。失效返回 typed reason，不静默改目标。
- 源 enemy command 为空时输出 `UnsupportedSourceCommand` 的成功处理/no-effect 结果，不伪造 damage 0 攻击。
- Presentation 不进入本实现，不允许存 `GameObject`。

## 非目标、测试与停止

不实现 Tower/Poison、Prefab/UI、Scene 接线或正式敌人内容库。测试覆盖候选过滤、priority、seed 可复现、shape/边界、数量上限、source/target replacement/death、Resolve 最终重判、完整 removal snapshot 和 unsupported 结果。若必须修改共享文件，先交回主线程；不得建立第二套 action/snapshot。

## Git 与协作

你不是仓库唯一工作者。不得回退、切分支、stash、暂存、commit、push；只写拥有路径和独占报告。
