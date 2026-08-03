# ADR 0012: Overworld Map Determinism and Identity

## Status

Accepted through Gate D. The end-to-end overworld route is closed.

## Decision

固定 seed 的局外拓扑必须在同一 config 下生成相同的节点 identity、层级、房间类型和边集合。若 Godot CFG/RNG 无法逐位复现，则兼容等级定义为“相同 seed 产生可审计的相同拓扑/类型序列”，并保留 Unity 生成器算法与版本证据，不静默替换 Godot 算法。

地图 Domain 复用 Wave 03P 已冻结的 `TimeKey.Domain.OverworldMovement.MapNodeId`，不新增第二个节点 identity 类型。章节状态快照只含 immutable 值、visited/settled 集合、当前节点、活动房间、revision 和一次性 operation journal；所有集合对外防御性复制。战斗、事件、商店和 Boss 结果必须在既有 typed SceneFlow boundary 上分别建模，不能把 `Dictionary`, `object` 或 Unity object 放入 payload。

## Compatibility and migration

Gate B 已将唯一 `OverworldChapterState` 接入 typed SceneFlow 和 schema 2 原子存档；Gate C 的正式地图 UI 只投影该状态，不创建平行节点表或第二套状态机。事件房在源资源缺失时只允许显式安全跳过；商店使用确定性 offer，并把时间币扣除、卡牌加入 deck、房间结算和存档作为单一原子提交。Continue 仅对有效、已提交的存档启用，损坏、future schema 与 I/O failure 使用 typed 结果和可恢复中文提示。

Gate D 已通过两个独立 Player 进程证明跨多房间、Boss、单次章节推进、失败返回和重启恢复。重启 Continue 会先从已验证的 schema 2 snapshot 提升 Bootstrap transition sequence floor，再预留新 request sequence，避免新进程序列与持久 operation cursor 重用。该修正不增加 schema、payload 或状态机。任何 future schema 继续使用版本化原子写入，并在新快照通过验证前保留旧文件。
