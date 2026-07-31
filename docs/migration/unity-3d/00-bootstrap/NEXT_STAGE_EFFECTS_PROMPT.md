# 《时之钥》Unity 局内战斗 Wave 02B2 最小效果切片 Prompt

> 状态：Wave 02B1 通过后可执行
> 负责人：接手主智能体
> 最后验证日期：2026-07-31
> 前置：`NEXT_STAGE_COMBAT_PROMPT.md`、`START_HERE_PROMPT.md`、Wave 02B1 检查点

## 1. 目标

在现有 `unity_7.31` 分支与 360° 19 格战斗棋盘上，完成七张真实卡牌 fixture 的统一 schema、解析和最小确定性效果 Domain 闭环：`lighting`/`damage`、`earthquake`/`elevation`、`wind`/`clear`、`recover`、`tower`/`built`、`poison`、`tornado`。本波只扩展局内规则和必要的表现适配，不接敌方/建筑行动、胜负奖励或局外 Unity 场景。

必须保留稳定拼写 `lighting`，不得改成 `lightning`；原始 JSON 不得格式化、覆盖或改名。

## 2. 现状与已冻结接口

- Wave 02B1 已通过 EditMode 31/31、PlayMode 15/15、Windows build 与 Player smoke；不得重跑或重写 Wave 00/01/02A/02B1 证据。
- `CardJsonAdapter.Parse` 当前读取源 fixture；`CardDefinition` 保留 stable ID、numeric ID、effects、effect range、shape。
- `TimelineGrid.CanPlace`、`CardPlaySession`、`VerticalSliceController` 公共集成面已冻结。预览必须继续只读，Commit 才能变异。
- Godot `EffectProcessor` 已确认按 `effects[].type` 工厂化处理 `damage/elevation/recover/built/poison`；`clear` 使用独立 `TimelineClearEffect`，不创建普通 `TimelineAction`；未知 effect 必须显式失败。
- 七张原始 fixture：

| stable ID | effect | 原始 shape/range 要点 |
| --- | --- | --- |
| `lighting` | damage 100 | shape `1`；range `0,0 / 1,0 / 2,0` |
| `earthquake` | elevation value 2 | shape `11`；hex radius-1 七格 |
| `wind` | clear `11,11` | shape `0`；中心点地图范围 |
| `recover` | recover 100 | shape `111`；三格 map range |
| `tower` | built `tower`, 1 | shape `010,111`；中心点 map range |
| `poison` | poison 2 | shape `110,111`；中心点 map range |
| `tornado` | clear `111111111111` | shape `0`；中心点 map range |

## 3. 最小可交付范围

1. 为 effect union 建立明确类型/验证规则；保留 `creation`、数值 value、字符串 shape/value，不把 `clear` 强行解释成普通放置行动。
2. 为七张 fixture 添加解析测试：稳定 ID/numeric ID、effect type/value、shape 最小包围盒、axial range、空/未知/非法 effect 的显式失败。
3. 为每类 effect 添加纯 Domain 测试和固定 seed 731 snapshot：
   - damage：目标 HP 钳制到 0；
   - elevation：只改变目标范围的 elevation，按原 JSON value 与现有 Godot 语义建立单一明确映射；
   - clear：清除命中范围内完整时间轴 action，普通 `CanPlace` 不被伪造为可占格；
   - recover：不超过 Max HP；
   - built：生成确定类型/等级的建筑状态，不依赖 Unity GameObject；
   - poison：写入确定层数/状态，重复应用规则由测试冻结；
   - tornado：按原始 12 格 clear value 处理边界、冲突和整行动清除。
4. 只在 Domain contract 通过后接入现有 Controller 的最小动态卡牌列表；沿用 `CardHandView` 的单选、取消、拖放和输入隔离，不复制规则到 UI。
5. 为 lighting 回归现有 02B1 证据，至少补一条“动态解析七卡后 lighting 仍能完成选卡→目标→预览→Commit→Resolve”的 PlayMode smoke；不重新生成旧波次截图。

## 4. 所有权与并发

- Wave A：Agent 01 只写 `Runtime/Domain/**` 与 `Tests/EditMode/**` 的 effect schema/规则；Agent 02 只写 `Runtime/Infrastructure/**`、卡牌 fixture adapter 测试与自己的证据/报告。两者并行，不写共享 Controller/场景/账本。
- 主智能体等待 Wave A、审查并冻结实际 API 后，才启动 Wave B：卡牌列表/适配表现与效果状态预览；各 Agent 使用独立 Prompt 和互斥路径。
- 主智能体独占 `VerticalSliceController.cs`、共享场景、Editor harness、PlayMode 集成测试、迁移文档、最终截图、Git staging/commit/push。
- 所有 Agent 都必须知道自己不是仓库唯一工作者；不得改四个用户原有脏文件、切分支、stash、commit、push、回退或 `git add -A`。

## 5. 门禁

- EditMode：七 fixture 解析与七类 effect 纯规则全部通过，失败数 0；`CanPlace`/preview 无副作用回归通过。
- PlayMode：动态手牌可切换七张卡；clear/built/poison 等状态至少有一条实际 3D 棋盘或时间轴可观察证据；lighting 02B1 闭环不回退。
- Harness/build：至少 1920×1080 默认、动态卡牌、效果后状态和 1280×720 关键态；Windows build 成功。
- Player：退出码 0 且日志含 `TIMEKEY_PLAYER_SMOKE_PASS`。
- Git：精确路径 staging；四个用户文件、`.log`、Library、build、随机 ProjectSettings 不在暂存区；push 到 `origin/unity_7.31`。

## 6. 禁止扩张

本波不实现敌方/建筑行动优先级、不实现抽弃牌/回合/时间币/胜负/奖励、不改 Godot 局外流程、不做授权素材修图、不把 `clear` 或 `shape=0` 伪装成普通时间轴行动。
