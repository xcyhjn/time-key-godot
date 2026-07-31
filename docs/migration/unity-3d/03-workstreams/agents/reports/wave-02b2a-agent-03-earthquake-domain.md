# Wave 02B2A Agent 03 Earthquake Domain 报告

> 状态：实现完成；等待主智能体刷新 Gate B 持久化测试证据
> 负责人：Wave 02B2A Agent 03
> 最后验证日期：2026-08-01

## 改动路径

- `unity/Assets/_Project/Runtime/Domain/TimelineAction.cs`
- `unity/Assets/_Project/Runtime/Domain/TimelineGrid.cs`
- `unity/Assets/_Project/Runtime/Domain/CombatSliceState.cs`
- `unity/Assets/_Project/Runtime/Domain/ResolutionSnapshot.cs`
- `unity/Assets/_Project/Runtime/Domain/CardPlaySession.cs`
- `unity/Assets/_Project/Runtime/Domain/Terrain/CombatBoardState.cs` 及 Unity `.meta`
- `unity/Assets/_Project/Tests/EditMode/Terrain/EarthquakeResolutionTests.cs` 及 Unity `.meta`
- 本报告

未修改 Agent 01/02 所有权、Infrastructure、Presentation、Scenes、Editor、asmdef、Packages、Godot 源或保护文件；未暂存、提交或推送。

## 实际 API

```text
BoardTileState.LogicalLayerCount

CombatBoardState.TileCount
CombatBoardState.AddTile(HexCoord, logicalLayerCount)
CombatBoardState.TryGetTile(HexCoord, out BoardTileState)
CombatBoardState.TryApplyElevation(HexCoord, amount, out TileEffectResult)

CombatSliceState(..., CombatBoardState board, ...)
CombatSliceState.Board

TimelineAction.TargetCoord : HexCoord?
TimelineAction.Effects : IReadOnlyList<CardEffect>
TimelineAction.EffectRange : IReadOnlyList<HexCoord>
TimelineAction.FromCard(card, targetId, targetCoord, origin)

ResolutionSnapshot.EffectResults : IReadOnlyList<TileEffectResult>
TileEffectResult = Coordinate + BeforeLayers + AfterLayers + Removed
```

旧 `TimelineAction(..., damage)`、`TimelineAction.Damage`、无坐标 `FromCard` 和 `CombatSliceState` constructor 保持兼容。普通 action 在构造时复制 shape/effects/range；`CardPlaySession` Commit 使用已保存的 `TargetCoord`。

## 冻结规则

| 输入 | 领域结果 | 快照结果 |
| --- | --- | --- |
| earthquake，目标中心及六个 axial 邻格均存在，1 层 | 每格执行 `1 + 2 = 3` | 按 fixture range 顺序返回 7 个 `1 -> 3, removed=false` |
| range 坐标缺失 | 跳过；`TileCount` 不变，不创建 tile | 不生成该坐标的 result |
| 5 层执行 `+2` | 结果超过 6，删除 tile | `5 -> 0, removed=true` |
| 1 层通过 board domain API 执行 `-1` | 结果不高于 0，删除 tile | `1 -> 0, removed=true` |
| 两格时间轴 shape `11` | Preview 不占格，合法 Commit 后占 2 格 | Resolve 时 action 只执行一次 |
| shape 从第 11 列开始 | Preview/Commit 失败 | TimelineGrid 与 board 均不变；Cancel 幂等关闭会话 |

lower-bound 通过公开的纯 Domain `TryApplyElevation` 冻结，不允许负数卡牌 payload；Agent 01 的 `CardEffect` 仍拒绝负 numeric amount。Elevation 不检查 occupant，Resolve 每次按 `TargetCoord + EffectRange` 重新查询当前 board。

## 验证

- Unity 6000.4.10f1 全量 EditMode：`67/67 passed`，0 failed，0 skipped。
- Earthquake Domain：`9/9 passed`，覆盖中心七格、missing/no ghost、`1 -> 3`、`5 -> removed`、`<=0 -> removed`、两格 shape、右边界失败纯度、target coord 传递、immutable copies 和 seed 731 确定性。
- 既有 EditMode 回归：其余 `58/58 passed`；lighting damage、旧 constructor、CardPlaySession 与七卡 adapter 均未回退。
- 本地最终 XML：`D:\timekey-unity-731\Temp\Agent03\editmode-results-final.xml`。正式 Gate B XML 由主智能体在收回所有权后刷新。
- `git diff --check -- unity/Assets/_Project/Runtime/Domain unity/Assets/_Project/Tests/EditMode`：通过。
- `Runtime/Domain` 的 `UnityEngine`/`using Unity` 搜索：0 命中。

## 失败项与处理

首次运行因 Unity Test Runner 将 XML 写到产品默认 `TestResults.xml`，而不是命令给定的工程 Temp 路径；测试本身退出码 0。已解析默认 XML确认 `67/67` 后复制到上述本地 Agent03 路径。没有编译失败、断言失败或许可证硬阻塞。

## 复杂度与 Presentation 消费面

- 时间轴结算维持既有列优先扫描；单个 elevation effect 对 `R` 个 range offset 做 `R` 次字典查询，平均 `O(R)`，earthquake 固定 `R=7`；board 存储为 `O(tile count)`。
- Presentation 只需读取 `ResolutionSnapshot.EffectResults`：`Removed=false` 时按 `AfterLayers` 更新真实柱；`Removed=true` 时可在后续表现切片处理销毁。Wave 02B2A 演示 fixture 不触发销毁。
- `AfterLayers` 是一基逻辑层数；表现换算 `blockCount=AfterLayers`、`unityElevation=AfterLayers-1`。`1 -> 3` 的顶面位移为 `2 * 0.32 = 0.64`。
