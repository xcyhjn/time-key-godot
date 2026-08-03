# ADR 0012: Overworld Map Determinism and Identity

## Status

Gate 0/A continuation record, pending full map SceneFlow integration.

## Decision

固定 seed 的局外拓扑必须在同一 config 下生成相同的节点 identity、层级、房间类型和边集合。若 Godot CFG/RNG 无法逐位复现，则兼容等级定义为“相同 seed 产生可审计的相同拓扑/类型序列”，并保留 Unity 生成器算法与版本证据，不静默替换 Godot 算法。

地图 Domain 复用 Wave 03P 已冻结的 `TimeKey.Domain.OverworldMovement.MapNodeId`，不新增第二个节点 identity 类型。章节状态快照只含 immutable 值、visited/settled 集合、当前节点、活动房间、revision 和一次性 operation journal；所有集合对外防御性复制。战斗、事件、商店和 Boss 结果必须在既有 typed SceneFlow boundary 上分别建模，不能把 `Dictionary`, `object` 或 Unity object 放入 payload。

## Compatibility and migration

当前 Gate A 仅关闭纯 Domain 的拓扑与房间操作切片。Save schema、Continue、事件/商店 UI、Boss 章节切换和正式 SceneFlow 尚未宣称完成；它们从地图 Prompt 的下一个未完成 Gate 继续。任何 future schema must use versioned atomic write and preserve the previous file until the new snapshot validates.
