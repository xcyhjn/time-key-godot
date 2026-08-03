# 数据与存档迁移

> 状态：边界已冻结，旧档转换待产品决策
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：7 份卡牌 JSON、Godot CFG 存档代码、Resource/场景导出字段

## 静态内容

首切片将 `card_data/lighting.json` 原样复制为 Unity TextAsset fixture，并在导入前后校验 SHA-256：

```text
7DEC6590C409E3A5C9BB2E83B8E833FC1E6D9106EF5706E223F909D1E763FB0B
```

稳定键使用 `lighting`。数字 `id` 不作为跨版本主键。解析 DTO 只接收本切片需要的 ASCII 字段，未映射的中文字段保留在源 JSON 中但不进入领域对象。

最小 schema：

```text
CardDefinition { stableId, numericId, effects[], rangeOffsets[], shapeRows[] }
EffectDefinition { type, value }
TimelineAction { origin, actorKind, cardId, targetId }
EnemyIntent { origin, intentId, targetId }
```

## 运行态与场景上下文

运行态不写回配置对象。跨场景数据使用版本化 DTO：

```text
SceneContextV1 { schemaVersion=1, runId, seed, sourceScene, payload }
```

调用方创建 DTO，加载目标场景后由 composition root 消费；缺失或不支持的版本必须显式失败并记录，不用字符串字段在节点入树前注入。

## Unity 存档信封

```text
SaveEnvelope { schemaVersion, gameVersion, savedAtUtc, payload }
```

- 首个 Unity schema 从 `1` 开始。
- 运行数据保存使用稳定字符串 ID，不序列化 GameObject/MonoBehaviour 引用。
- 每次 schema 变更必须提供迁移函数或明确废弃策略。

## Godot 旧档边界

**事实**：现有 CFG 没有 schema version，Vector2i 用字符串键，且时间币、完整进度和活动场景上下文并未完整落盘。
**决策**：首切片不实现旧档导入；预留独立 `GodotCfgImporter` adapter 边界，不能污染 Domain。
**待用户决策**：进入局外闭环切片前，确认是“不兼容旧档”“尽力导入”还是“强兼容”。这不阻塞当前切片。

## 授权约束

来源不明的图片、音频和字体不得复制进 Unity 工程。首切片只复制自有规则 JSON，并使用程序化几何、颜色和 Unity 内置运行时字体。
## Wave 03 Overworld save schema 2

The production overworld save is a persistence-owned immutable DTO. It stores primitive
run/resources, generator config/version, exact map fingerprint/current/visited/settled
identity, Domain revision and operation journal, Application outcome identities and
operation/persistence cursors. It never serializes a Domain/Application object,
Dictionary, `object`, Unity object or scene reference.

Writes use UTF-8 temp creation, durable flush, reload/schema/content validation and
atomic replacement with a recoverable backup. Schema 0 and 1 migrate to schema 2;
future, corrupt and I/O failures remain typed and cannot enable Continue. SceneFlow saves
the complete candidate before source unload; a failed prepare leaves the prior primary
unchanged, while a later pre-commit rollback restores the prior document.

### Gate C local-room commits

Event safe-skip and Shop purchase reuse schema 2; no schema bump is required. Both paths
construct a copied candidate, apply the room outcome, persist and validate the complete
candidate, then promote it in memory. Shop promotion includes the exact timecoin balance,
the purchased stable card ID and the settled room identity in the same write. Persistence
failure preserves the prior in-memory state and prior primary file, while exact operation
replay remains idempotent and a changed fingerprint remains a typed conflict.
