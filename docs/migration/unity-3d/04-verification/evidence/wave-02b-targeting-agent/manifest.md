# Wave 02B Targeting Agent Evidence

> 验证日期：2026-07-31
> Unity：6000.4.10f1
> 范围：`TimeKey.Tests.PlayMode.Targeting`

## 自动化结果

- PlayMode：`4/4 passed`、`0 failed`、`0 skipped`。
- NUnit：`playmode-results.xml`，SHA-256 `063E4D0A5DFFC15178839785901B5D9BE2B9CE9D8B34F66144A2C36E7879AC7A`。
- 行为覆盖：六态颜色、范围投影/去重、缺失坐标观测、重复 Show/Clear、属性块恢复与共享材质隔离、Domain 合法/冲突结果转发、时间轴颜色恢复。
- 渲染覆盖：四个 cardinal yaw 的相同三坐标范围，以及一个 conflict invalid 时间轴 cell。

## 截图审查

所有截图均为 `1280x720`，已实际查看，不是仅检查退出码。四向图中 `(0,0),(1,0),(2,0)` 始终为金黄色；`(1,0)` 是两个独立 `0.32` 高度块，顶面和侧面均可辨识。invalid 图中红色时间轴 cell 清楚且未遮挡棋盘范围。

| 文件 | SHA-256 |
| --- | --- |
| `board-range-yaw-0.png` | `FE2DBB4B71B9C7837C5E2AB691D75C2AFB7E4EAFB85BBAF495E816371FD806CC` |
| `board-range-yaw-90.png` | `C192730E6619D6E9AE87569F21A9A513630881FD3AB6EBF5D6E049C0061B46F0` |
| `board-range-yaw-180.png` | `3991C2019FF0921092F91572F41E6786FC56D1EBB9233337814B8FEDE4969D2F` |
| `board-range-yaw-270.png` | `98A72C6C8846125746A462BE7A45347CB7B60EDEA41F1CF0781A163165D78403` |
| `timeline-invalid.png` | `DF58CA5C358DDE3C964042BD8048083DBA5F1F950FB3A9189F4A2481624FE054` |

原始 Unity 日志仅用于本地诊断，不纳入证据；结构化 NUnit XML 和 PNG 为可审查证据。
