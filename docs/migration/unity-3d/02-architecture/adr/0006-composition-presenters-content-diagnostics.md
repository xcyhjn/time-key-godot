# ADR 0006：以 Composition、Presenter、内容目录与结构化诊断关闭 R3 解耦

> 状态：Accepted
> 日期：2026-08-01

## 背景

ADR 0005 引入 `CombatApplicationSession` 后，R2 仍把七卡 fixture 解析、`Resources` 卡图加载、ViewModel 创建、输入订阅、时间轴格管理和 UI 刷新集中在 `VerticalSliceController`。`TimeKey.Presentation` 因此引用 `TimeKey.Infrastructure`，内容增长可能重新产生 stable ID 分支；session 和运行时图片也没有一个明确的外层生命周期所有者。原有 trace 能说明命令与 phase，却不能直接说明具体效果及状态前后变化。

R3 需要在不改变已验证 lighting/earthquake 玩法、不隐藏 Scene/Prefab Inspector 引用的前提下关闭这些耦合点。

## 决策

### 1. 单向程序集与显式组合根

- 新增 `TimeKey.Composition`，它依赖 Domain、Application、Infrastructure、Diagnostics 和 Presentation；其余模块不引用 Composition。
- 从 `TimeKey.Presentation.asmdef` 移除 Infrastructure，保持 `Domain <- Application <- 外层适配器` 的无环方向。
- 在战斗 Scene 根对象上序列化 `CombatCompositionRoot`。它验证 `VerticalSliceController`、`CombatPresentationBinding`、`UnityCombatTraceSink` 与卡牌 `TextAsset` 列表，然后创建 catalog、图片、state、timeline、enemy intent 和 `CombatApplicationSession`。
- `CombatCompositionRoot` 拥有并在 `OnDestroy()` 释放 session 与运行时 Sprite。拒绝服务定位器、全局可变单例和运行时 Scene 搜索作为依赖来源。

### 2. 用四个窄 Presenter 和一个 Binding 隔离表现协调

- `CardHandPresenter` 管理卡牌 ViewModel、选择态和卡手输入。
- `BoardRangePresenter` 只把 `CombatSessionView` 的 target/range 映射到 `BoardRangePreview`。
- `TimelinePresenter` 拥有序列化的时间轴格引用、hover/click 订阅、合法性预览和 action 渲染。
- `CombatHudPresenter` 映射 phase、目标文本和 Resolve 按钮状态。
- `CombatPresentationBinding` 负责四者的 `Bind()`/`Unbind()`、输入事件汇聚和 `Refresh(CombatSessionView)` 分发。Controller 通过 Binding 交互，不直接拥有这些 View 的重复订阅。

Controller 暂时保留 Unity 射线检测、世界棋盘/目标同步、相机和兼容公共 facade。这是受控的过渡边界，不代表允许它重新承担内容解析或用例生命周期。

### 3. 内容目录与 `front_image` 是资源接入契约

- `CardContentCatalog` 实现 `ICardCatalog`，解析任意非空 JSON 集合、保序并拒绝重复 stable ID；不把“恰好七张”编码为运行时限制。
- `CardContentEntry` 以 `Path.GetFileNameWithoutExtension(FrontImage)` 派生 `Art/Battle/Cards/<stem>`。组合根只按这个路径加载图片，缺图立即失败。
- Controller 和 Presenter 不允许按 stable ID 选择图片、目标规则或效果行为。新增使用既有效果的普通卡牌只需内容、图片、Inspector 引用与测试。

### 4. 区分内容可交互登记与 Domain 效果处理器

- `CardEffectRegistrationCatalog.CreateVerticalSlice()` 当前登记 `Damage` 和 `Elevation`，组合根用它设置 `CardViewModel.IsInteractable`。
- 该 catalog 是数据化的内容支持清单，不执行效果。`TimelineGrid` 注册的 `ICardEffectHandler` 才是结算权威；默认处理器为 `DamageCardEffectHandler` 和 `ElevationCardEffectHandler`。
- JSON 中已知但尚未登记的效果可以出现在手牌中，但不可交互。缺少 handler 的效果在 action 占格前失败，避免产生部分提交状态。

### 5. 诊断端口增加效果前后值，Unity 输出留在外层

- `CombatTraceEntry` 增加可选 `EffectKind`、`BeforeValue`、`AfterValue`，保留 command、phase、卡牌、typed target、origin 和 failure 字段。
- Application 在 resolve 期间记录效果级条目，使 damage 的 HP 变化和 elevation 的层数变化可直接审计。
- `UnityCombatTraceSink` 放在 Composition，因为它依赖 `MonoBehaviour`/`Debug.Log`；Inspector 的 `loggingEnabled` 可以关闭输出。
- Application 只调用 `ICombatTraceSink`。sink 异常仍不得改变状态、seed、命令结果或结算快照。

## 取舍

- 显式组合根增加了 Scene Inspector 引用和启动校验，但对象图、资源契约与销毁责任可见且可测试。
- 四个 Presenter 与 Binding 增加少量类型和转发代码，换来输入订阅对称、职责边界清晰以及 `Presentation -> Infrastructure` 的移除。
- `Resources` 仍用于本阶段卡图加载；统一的 `front_image` 路径消除了 Controller 分支，但尚未引入 Addressables 或异步资源生命周期。
- 内容支持登记与 Domain handler 是两套必须同步的清单。这个显式重复用于区分“允许交互”和“能够结算”；新增效果测试必须同时覆盖两侧。
- Controller 仍有 Unity 世界表现职责。本 ADR 不做高风险重写，而是禁止其重新拥有 JSON、卡图、session 创建和 stable ID 玩法分支。

## 后果

运行时依赖现在是无环的，Domain/Application/Diagnostics 可脱离 Unity 测试，Presentation 不再依赖 Infrastructure。七卡 schema 与原图由 catalog 和组合根统一接入；第八张使用既有效果的普通卡牌无需新增 Controller 路由。lighting 与 earthquake 继续通过同一个 typed session/handler 链路结算，表现由 Binding/Presenter 消费状态。

后续新增卡牌、效果或诊断输出必须遵守以下不变量：内层模块不引用 Unity 外层；资源缺失显式失败；效果执行不进入 Presenter；stable ID 不成为玩法分支；创建 session/运行时资源的边界负责释放；任何表现改动继续提供 Scene/Prefab、PlayMode 与实际渲染证据。
