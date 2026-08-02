# Deck & Battle Flow Agent A：Deck Domain

> 单一目标：只新增无 Unity 依赖的牌库、手牌、弃牌、稳定卡实例身份、抽牌与固定 seed 洗牌 Domain，以及独占 EditMode tests。

## 必读

- 本阶段主 Prompt
- `agents/reports/deck-battle-flow-source-semantics.md`
- ADR 0008 与 02B3 Gate D 最终报告
- 当前 `Runtime/Domain/CardDefinition.cs`、`CardPlaySession.cs`、`TimelineActionIdentity.cs`

## 独占拥有路径

- 新目录 `unity/Assets/_Project/Runtime/Domain/Deck/**` 及 `.meta`
- 新目录 `unity/Assets/_Project/Tests/EditMode/Deck/**` 及 `.meta`
- 独占报告 `agents/reports/deck-battle-flow-agent-a-deck-domain.md`

## 禁止路径

所有既有文件、Application、Presentation、Composition、Scene、Prefab、Editor、asmdef、共享文档/证据/Git 和 Godot 源。

## 冻结契约

- starter deck 精确为 `lighting, lighting, earthquake, earthquake, recover, wind, wind, recover, tower, tower, poison, poison`。
- `CardStableId` 只表示内容；每张实体卡另有非空且全局唯一的 `CardInstanceId`，在 deck/hand/discard 移动中保持不变。
- 所有随机来自显式 seed/自有确定性 PRNG 状态；不得读取 Unity time、`UnityEngine.Random` 或进程全局随机。
- 抽牌堆顶语义固定且受测；手牌上限 7、正式每轮请求抽 5，但只补到上限。
- 抽牌堆空且弃牌非空时，只把弃牌洗回一次再继续；两者都空时返回 typed Exhausted，不循环、不造牌。
- 强制弃置只移动当前 hand 快照；空手牌为成功 no-op。所有成功/失败结果包含 before/after counts、移动的 instance IDs、shuffle 信息和原因。
- 非法输入、重复命令或失败不改变状态；所有输出集合防御性复制。

## 测试与验证

覆盖重复 stable ID 但不同 instance ID、初始牌组、固定 seed 同序/异 seed、抽到上限、空抽牌堆洗回、双空堆、跨多周期、空手弃置、失败无副作用和快照不可变。运行纯 C# 编译和定向 Unity EditMode，解析 XML `total=passed>0, failed=0`；静态确认无 `UnityEngine`。不得运行 graphical Unity、不得执行 Git 操作。

## 停止条件

只有必须修改禁止路径或无法表达 stable identity/typed result 时停止并报告；普通编译、命名或测试修正不是停止理由。你不是仓库唯一工作者，只改拥有路径并适配并行变化。
