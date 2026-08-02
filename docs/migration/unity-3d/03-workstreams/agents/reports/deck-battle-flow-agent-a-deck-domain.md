# Wave 02B4 Agent A：Deck Domain 交付报告

> 状态：完成
> 日期：2026-08-02
> 所有权：仅 `Runtime/Domain/Deck/**`、`Tests/EditMode/Deck/**` 与本报告

## 交付

- 新增无 Unity 依赖的 `DeckState` 聚合，牌堆顶固定为数组高索引。
- 固定 starter deck 为 `lighting, lighting, earthquake, earthquake, recover, wind, wind, recover, tower, tower, poison, poison`。
- `CardInstanceId` 与 stable ID 分离；创建 starter deck 时要求显式 battle identity scope，并在聚合入口拒绝重复 instance ID。
- 使用显式 `ulong seed` 和聚合私有 SplitMix64 状态执行 Fisher-Yates 洗牌，不读取 Unity 或进程全局随机状态。
- 手牌上限固定为 7，正式抽牌请求常量固定为 5；抽牌堆为空时至多把弃牌洗回一次，双空返回 typed `Exhausted`。
- 提供单卡弃置与强制弃手；强制弃手只消费调用时 hand 快照，空手为成功 no-op。
- 所有移动结果包含 typed reason、before/after zone counts、移动 instance IDs 和 shuffle 明细；非法、失败与重复命令无牌区副作用。
- `DeckSnapshot`、starter deck、移动 IDs 与 shuffle IDs 均为防御性只读复制。

## 验证

- Unity 随附 Mono/Roslyn 纯 C# 编译：5 个 Domain 源文件通过。
- 新增测试程序集纯 C# 编译：1 个测试源文件通过。
- 反射执行独占 NUnit 测试：`12 passed / 0 failed`。
- 静态扫描：`Runtime/Domain/Deck/**` 与测试中没有 `UnityEngine`、`UnityEditor`、`System.Random`、`Guid.NewGuid`、`DateTime` 或 `Environment.TickCount`。
- 覆盖 starter deck 与重复 stable ID、同 seed/异 seed、牌堆顶、手牌上限、空堆洗回、双空、跨多周期、空手弃置、单卡弃置、非法/重复命令、失败无副作用与快照不可变。

按 Agent Prompt 未运行 Unity、未修改 asmdef/共享文件、未执行 Git 操作。定向 Unity EditMode XML 由主智能体在集成门禁统一生成。
