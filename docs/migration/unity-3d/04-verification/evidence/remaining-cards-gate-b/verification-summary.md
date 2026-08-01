# Remaining Cards Gate B 验证摘要

> 日期：2026-08-01
> Unity：6000.4.10f1
> 卡牌：`tower`、`poison`

## 结论

Gate B 通过。Built 在真实空地创建 Neutral Tower occupant（HP/MaxHP 100），Poison 对仍存活且支持状态的稳定 occupant 累加 2 stacks。两者共享 Gate A occupant/result 契约；本门禁不执行 Tower decay、Poison tick、传播或伤害。

## 自动化

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| 全量 EditMode | `130/130`，0 失败、0 跳过 | `editmode-results.xml` |
| Gate B Scene PlayMode | `3/3`，0 失败；Tower、Poison、序列化场景回归 | `playmode-results.xml` |
| Scene authoring | 定向 authoring PASS；Scene 保存 Tower/Poison Prefab GUID、Presenter 与 Binding 引用 | `authoring.log`（按规则不入 Git） |
| 渲染 Harness | PASS marker；Tower HP100、Poison stacks2；8 张 1280x720 PNG | `gate-b-summary.json` |

Domain/Application 自动化覆盖空地与占用、Resolve 前占用、unknown creation/value、Tower 字段、Poison 活体/死亡/无状态、`0→2→4`、snapshot 副本、溢出与失败纯度。Scene tests 还验证两个 Prefab 是保存资产、Tower 无 collider、原图 Point/Clamp、无 mipmap/压缩，并且所有 Inspector 引用非空。

## 素材与 Prefab

- `image/enermy/tower/tower.png` 与 Unity 副本 SHA-256 均为 `41DD24EF1670355906BA80247233BDC422E6022AB0A7AE42B8ACEB962E18F014`。
- `image/effect/poison_icon.png` 与 Unity 副本 SHA-256 均为 `4445561C52BBD9DC4206CA475D72E1D6178361281E558459739F28CB72013812`。
- `Tower.prefab` 为 billboard、无 collider；`PoisonStatus.prefab` 使用原图标与整数 `TextMesh`。
- TargetView 与 Tower 都通过 `CombatOccupantView.StatusAnchor` 接入；Prefab 不保存 Domain identity。

## 人工视觉检查

逐张打开 `tower-*` 与 `poison-*` 8 张 PNG：

- 两张原卡面比例正确；选中、目标黄高亮和多格绿色 Timeline 预览清晰。
- Tower 在目标 tile anchor 上显示完整，无遮挡/漂浮；HUD 为 `TOWER | HP 100`。
- Poison 原紫色图标与数字 `2` 可读，不与目标主体或时间轴标签混淆；HUD 为 `TARGET-01 | POISON 2`。
- 1280x720 下未见文字重叠、卡面裁切、右 HUD/Timeline 关键遮挡或缺失控件。

本阶段仍继承未触及的 R3 三视口 idle、四 yaw、Terrain/Camera 与 0.32 层高证据；最终 Gate D 必须从完整七卡集成态刷新全量 PlayMode、build、Player smoke 和三视口。
