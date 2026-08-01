# 添加卡牌

> 适用范围：复用已注册效果的普通时间轴卡或独立即时 Clear 卡

## 最小修改面

1. 在 `unity/Assets/_Project/Content/Cards/` 添加与 Godot 权威数据一致的 JSON。保留 stable/numeric ID、typed effects、range、shape 和 `front_image`，不要改写 fixture 迎合 Unity。
2. 把原卡面放入 `unity/Assets/_Project/Resources/Art/Battle/Cards/`。`front_image` 使用真实文件名；`CardContentEntry` 会定位到 `Art/Battle/Cards/<file stem>`，禁止按 stable ID 猜测。
3. 通过 Scene authoring 重建 `CombatCompositionRoot` 的 `TextAsset` 列表，或在 Inspector 明确添加新 JSON。`CardContentCatalog` 接受任意非空、stable ID 唯一的列表并保留顺序。
4. 确认卡牌的每个 `CardEffectKind` 已被 `CardEffectRegistrationCatalog` 支持。未注册效果会让卡牌保留在目录中但命令显式失败，不会静默 no-op。

普通新卡不应修改 `VerticalSliceController`。需要新效果时按 `add-effect.md` 扩展；Clear 不得伪装成普通 `TimelineAction`。

Clear 卡必须只有一个 typed `CardEffectKind.Clear`，mask 只来自 `effects[].value`，普通 `shape` 必须为空且不能作为 fallback。Application 通过 `CombatInteractionMode.TimelineClear` 进入独立 session；新增同语义 Clear 卡只需内容、注册和测试，不应在 Controller 添加 stable-ID 分支。

## 测试与 Inspector

在 `Tests/Infrastructure/Cards/CardContentCatalogTests.cs` 增加真实 fixture、顺序、重复 ID、`front_image` 和资源路径断言；JSON token/shape 规则放在 `CardJsonAdapterTests.cs`。增加第八张使用既有效果的普通卡时，应有测试证明无需 Controller 路由变化。

打开 `CombatVerticalSlice.unity`，确认 Composition 的 fixture 列表和 `CardHandPresenter` 的 `CardView` Prefab 引用。运行全量 EditMode/PlayMode、harness/build/Player。视觉上至少检查 1280x720、1920x1080、2560x1080 的整手牌、选中态、目标范围和 Timeline 预览；最长卡名与原卡面不得裁切或遮住右 HUD。

## 调试与回滚

解析失败先看 `CardJsonAdapter` 的明确异常；目录失败看 stable ID 唯一性；无图看 `CardContentEntry.ArtworkResourcePath` 与 Unity import 类型；不可用看 `UnsupportedEffect` trace。不要在 Presenter 写例外分支。

回滚时移除这张 JSON、卡面及 Composition 引用，并删除只属于它的测试；保留共享 catalog、Controller 和其他资源，不改 Godot 原数据。

## 已验证样例：Recover

`recover.json` 证明新增普通卡不需要修改 Controller：目录从原 fixture 读取 stable ID、`FrontImage`、`Recover +100`、三格 shape 与 range；Infrastructure 只注册已落地的 `CardEffectKind.Recover`。Application 按稳定 occupant ID + `HexCoord` 选择和 Resolve 重判，Domain handler 产出 occupant before/after 快照。完整路径与 1280x720 截图见 `04-verification/evidence/remaining-cards-gate-a/`。

`tower.json` 与 `poison.json` 进一步证明同一内容目录可驱动 tile/entity 两种目标策略和多格 shape；卡面仍来自原 `front_image`，Controller 不按 stable ID 决定效果。需要世界表现时通过序列化 occupant creation→Prefab 表和 snapshot 状态接入，不把资源路径写进 JSON handler。

`wind.json` 与 `tornado.json` 是同语义 Clear 卡样例：内容只提供 2×2/12×1 typed mask，均复用独立 session 和通用三态 Presenter；Wind 的空清/命中与 Tornado 的空清/命中都已验证，无 stable-ID Controller 分支。七卡最终全量证据见 `04-verification/evidence/remaining-cards-gate-d/`。
