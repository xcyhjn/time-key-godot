# Wave 01 集成契约

> 状态：已冻结
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：玩法等价契约、目标架构、数据迁移边界

## Domain API

首切片只要求下列语义，不冻结不必要的具体实现：

```text
HexCoord(q, r)
TimelineGrid(width=12, height=3)
TryPlace(TimelineAction) -> success/failure
Resolve(CombatSliceState) -> ResolutionSnapshot
CardDefinition(stableId, numericId, effects, range, shape)
```

`TimelineAction` 至少包含 origin、actor kind、card ID、target ID；结算按列再按行且一个 action 只执行一次。`ResolutionSnapshot` 字段与 `gameplay-parity-contract.md` 完全一致。

## Adapter API

Infrastructure 从 TextAsset/字符串解析卡牌 JSON，输出 Domain 的 `CardDefinition`。解析器必须：

- 接受源文件未映射中文字段。
- 保留 `lighting` 稳定 ID、`damage=100`、range offsets 和单格 shape。
- 对缺失稳定 ID、未知 effect 类型或无效 shape 给出显式错误。
- 不修改 fixture 内容。

## Presentation API

Presentation 创建固定 seed 731 的场景，使用 axial XZ 映射显示至少 7 个六边形地块、一个可选目标和一个敌人意图。UI 暴露选卡、放置、结算、目标 HP、阶段与 12×3 时间轴。对 Domain 的调用通过明确方法完成，不能在按钮事件中重复实现伤害规则。

## 场景与截图

场景路径固定：`Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity`。Editor harness 输出证据到 `docs/migration/unity-3d/04-verification/evidence/unity-slice-01/`，文件名至少包含视口尺寸。PlayMode 测试通过场景控制器的公共交互方法驱动，不依赖屏幕坐标。

## 版本与提交

任何契约变更先由主智能体更新本文件和 ADR，再调整实现。代理不创建独立分支或提交；主智能体精确暂存并形成单一目的检查点。
