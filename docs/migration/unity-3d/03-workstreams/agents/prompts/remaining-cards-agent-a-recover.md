# Remaining Cards Agent A：Recover 与共享 occupant 结果契约

> 单一目标：在不修改 `VerticalSliceController.cs` 的前提下，让 `recover` 通过现有普通卡会话完成合法目标、时间轴、Resolve 重判、HP 钳制和 before/after 快照；同时建立 Built/Poison 后续复用的最小纯 Domain occupant/result 通道。

## 必读

- `00-bootstrap/NEXT_STAGE_REMAINING_CARDS_PROMPT.md`
- `agents/reports/remaining-cards-source-semantics.md`
- `agents/reports/remaining-cards-unity-extension-audit.md`
- `06-maintenance/add-effect.md`、`debugging-guide.md`
- 当前 `Runtime/Domain/**`、`Runtime/Application/**` 与对应 EditMode tests

## 独占拥有路径

- `unity/Assets/_Project/Runtime/Domain/CombatSliceState.cs`
- `unity/Assets/_Project/Runtime/Domain/ResolutionSnapshot.cs`
- `unity/Assets/_Project/Runtime/Domain/Effects/ICardEffectHandler.cs`
- 可在 `Runtime/Domain/`、`Runtime/Domain/Effects/` 新增 occupant/result/recover 纯 C# 文件及 `.meta`
- `unity/Assets/_Project/Runtime/Domain/TimelineGrid.cs`
- `unity/Assets/_Project/Runtime/Application/CombatApplicationModels.cs`
- `unity/Assets/_Project/Runtime/Application/CombatApplicationSession.cs`
- 可在 `Tests/EditMode/Effects/` 新增 Recover tests 及 `.meta`
- `Tests/EditMode/Application/CombatApplicationSessionTests.cs`
- 必要时最小修改 `Tests/EditMode/TimelineGridTests.cs`
- 独占报告 `agents/reports/remaining-cards-agent-a-recover.md`

## 禁止路径

- `VerticalSliceController.cs`、所有 Presentation、Composition、Infrastructure、Scene、Prefab、Editor harness、asmdef、Packages、ProjectSettings、共享迁移文档和既有证据
- 所有 Godot 源、用户 dirty 文件、其他 Agent 报告

## 冻结契约

- Recover `value=100`、range `(0,0),(1,0),(-1,1)`、shape `111`；不是负伤害。
- 选择阶段必须验证稳定 `entity ID + HexCoord` 指向存在、支持生命且 `HP < MaxHP` 的 occupant；仍存在的 `HP=0` occupant 可恢复。
- Resolve 重新按稳定坐标和 ID 查当前 occupant；消失、替换为不同 ID、满血或范围缺失为 no-op。
- 成功值为 `min(MaxHP, HP+100)`；失败无状态副作用。
- occupant/result 模型只含本阶段真实需要：runtime ID、coord、kind/creation、attitude、HP/MaxHP、poison stacks、能力标志；不得引用 Unity。
- handler 输出以兼容 `TileEffectResult` 的通用 result buffer 收集 occupant before/after；`ResolutionSnapshot.EffectResults` 继续兼容 earthquake，并新增只读 occupant 结果。
- handler 契约提供窄 payload 预检，供 Built 后续在占格前拒绝未知 creation/value；不要实现 Built/Poison 本身。
- `lighting`、`earthquake`、排序、enemy marker、seed、旧 public constructors 必须回归。

## 非目标

- Tower、Poison、Clear、回合 tick、Prefab/UI/VFX、卡牌 registry、Scene 接线、Controller 重构。

## 测试与验证

- 先写失败测试：合法/满血/HP0、缺失/ID 或 coord 不匹配、Resolve 前消失/变满、钳制、before/after、三格 shape 边界和失败纯度。
- 更新旧“Recover unsupported”测试为真实成功；保留真正未支持/非法 payload 的 fail-fast 测试。
- 可运行唯一 Unity 实例的过滤命令，结果写 `unity/Temp/remaining-cards-agent-a-*`；必须解析 XML `total=passed>0, failed=0`。
- `git diff --check`；确认 `git diff -- VerticalSliceController.cs` 为空。

## 停止条件

- Recover 必须修改 Controller、Presenter 或 stable-ID 分支才能完成。
- 需要反向 asmdef、UnityEngine 进入 Domain/Application、或无法保留 lighting/earthquake 兼容。

## Git 与协作规则

你不是仓库唯一工作者。只改拥有路径，不回退或格式化他人文件，适配并行变化。不得切分支、stash、暂存、commit、push 或清理目录。完成后写独占报告并把所有权交回主智能体。
