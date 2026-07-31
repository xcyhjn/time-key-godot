# Wave 02B2A Agent 01 七卡 Schema 报告

> 状态：实现完成；等待主智能体刷新 Gate A 持久化测试证据
> 负责人：Wave 02B2A Agent 01；token 校验与包决策由主智能体集成
> 最后验证日期：2026-08-01

## 改动路径

- `unity/Assets/_Project/Runtime/Domain/CardDefinition.cs`
- `unity/Assets/_Project/Runtime/Infrastructure/CardJsonAdapter.cs`
- `unity/Assets/_Project/Content/Cards/{earthquake,wind,recover,tower,poison,tornado}.json` 及 `.meta`
- `unity/Assets/_Project/Tests/Infrastructure/CardJsonAdapterTests.cs`
- 本报告

未修改其他 Domain 根文件、Presentation、Scenes、Editor、asmdef、Godot 源或保护文件。Package/lock 与 ADR-0003 是主智能体在收到 JsonUtility 实测后独占落盘的集成改动。

## 实际 API

```text
CardEffectKind = Damage | Elevation | Recover | Built | Poison | Clear

CardEffect(CardEffectKind kind, int value)
CardEffect(CardEffectKind kind, int value, string creationId)
CardEffect(CardEffectKind kind, IReadOnlyList<TimelineCell> clearMask)
CardEffect.Kind
CardEffect.NumericAmount : int?
CardEffect.Value : int                       # 02B1 兼容读取面
CardEffect.CreationId : string
CardEffect.ClearMask : IReadOnlyList<TimelineCell>

CardDefinition(stableId, numericId, effects, range, shape)
CardDefinition(stableId, numericId, frontImage, effects, range, shape)
CardDefinition.FrontImage : string

CardJsonAdapter.Parse(string) -> CardDefinition
CardJsonAdapter.TryParse(string, out definition, out error) -> bool
```

旧 constructor 保留给既有测试/代码，因其调用面没有图片参数，`FrontImage` 为 `null`；从 JSON 解析的正式卡必须走新 constructor 并持有校验后的普通文件名。Clear 必须是唯一 effect，`CardDefinition.Shape` 为空，其 mask 只来自 effect payload。

Adapter 使用 common/numeric/string `JsonUtility` DTO 读取 typed payload；主智能体依据 ADR-0003 用 Newtonsoft `JToken.Type` 做原始 token 存在性和 number/string 类型门禁。未使用 regex、substring、文本替换或 fixture 改写。

## 七 fixture 字段表

| stable ID | NumericId | FrontImage | Effect payload | Range | 普通 Shape | ClearMask |
| --- | ---: | --- | --- | --- | --- | --- |
| `lighting` | 1 | `lighting.png` | `Damage,100` | `(0,0),(1,0),(2,0)` | `(0,0)` | - |
| `earthquake` | 2 | `earthquake.png` | `Elevation,2` | `(0,0),(1,0),(1,-1),(0,-1),(-1,0),(-1,1),(0,1)` | `(0,0),(1,0)` | - |
| `wind` | 4 | `wind.png` | `Clear` | `(0,0)` | empty | `(0,0),(1,0),(0,1),(1,1)` |
| `recover` | 5 | `recover.png` | `Recover,100` | `(0,0),(1,0),(-1,1)` | `(0,0),(1,0),(2,0)` | - |
| `tower` | 6 | `tower_card.png` | `Built,1,creation=tower` | `(0,0)` | `(1,0),(0,1),(1,1),(2,1)` | - |
| `poison` | 7 | `poison_card.png` | `Poison,2` | `(0,0)` | `(0,0),(1,0),(0,1),(1,1),(2,1)` | - |
| `tornado` | 8 | `tornado.png` | `Clear` | `(0,0)` | empty | `(0,0)..(11,0)` |

`earthquake` 按 JSON/运行语义固定为 `+2`，不是中文描述中的 `+1`。Wind/tornado 顶层 `shape=0` 只被校验，不会伪造普通时间轴占格。

## 七 fixture SHA-256

| stable ID | SHA-256 |
| --- | --- |
| `lighting` | `7dec6590c409e3a5c9bb2e83b8e833fc1e6d9106ef5706e223f909d1e763fb0b` |
| `earthquake` | `088206677487757631eec7f234635a5c46bb69336dd7f018f9308cf4c025999a` |
| `wind` | `a8124f671144edd6cfd756bda4f3c8defa5ba6083a5e285459441f93085a90d8` |
| `recover` | `55aaf6397840c8c6f09d85959eab3e2c6c91d81d7b754458387ce1f21a43c279` |
| `tower` | `1bf3079d050c61ef385d28cf84a900f9a154d0201451821e1de32827483772e3` |
| `poison` | `011138f0cdd2c4d54ccf204f4c49e633fb77b009f24d7d1baa05bcf088427202` |
| `tornado` | `78775dabe8a883db053812948d49e1f5150ee9f2e148160a0bfc1e7c30981023` |

七组 Unity/Godot 工作树文件哈希逐一相同。

## 验证

- 最终 Agent 01 全量 EditMode：`58/58 passed`，0 failed，0 skipped；其中 Infrastructure `35/35`。
- 主智能体先前持久化的 Gate A XML：`55/55 passed`，位于 `docs/migration/unity-3d/04-verification/evidence/wave-02b2a-gate-a/editmode-results.xml`。因随后增加 missing effects/小数等负例并清理重复 parser，主智能体应在 Gate A 收口时刷新该证据。
- 七真实 fixture 均逐字段通过；错误 effect type/value token/negative amount、Built creation、clear mask、front_image、missing stable ID/effects/value 和 malformed shape 均显式失败。
- `git diff --check` 通过；Unity 运行前后均确认无并行 Unity/Godot 实例。

## 已解决失败项

- 仓库 NUnit 不支持 `Assert.Multiple`：改为兼容的独立断言，未升级依赖。
- DTO-only EditMode `55 total / 51 passed / 4 failed`：实证 JsonUtility 会把 numeric string/null 与 clear number 静默强制转换。
- 主智能体据此接受 ADR-0003 和 Unity 官方 `com.unity.nuget.newtonsoft-json@3.2.2`；最终只保留 `JToken.Type` 门禁 + JsonUtility typed DTO，已删除临时探针和重复 DataContract 路径。

## 风险与下一步

- Agent 03 可从 `CardEffect.NumericAmount` 读取 earthquake `2`，并不可变复制 `CardDefinition.Effects/Range` 到普通 `TimelineAction`；`TargetCoord` 和 Resolve 不属于本代理所有权。
- Clear effect 的 `Shape` 为空，后续 02B2C 必须走独立即时 clear 会话，不能进入普通 `TimelineAction/CanPlace/Resolve`。
- Presentation 必须从 `CardDefinition.FrontImage` 加载 `tower_card.png`/`poison_card.png`，不能由 stable ID 推导。
- 主智能体需刷新持久化 Gate A EditMode XML，并在 Package/ADR 集成态上继续全量回归。
