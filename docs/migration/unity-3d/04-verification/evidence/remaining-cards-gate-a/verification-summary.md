# Remaining Cards Gate A 验证摘要

> 日期：2026-08-01
> Unity：6000.4.10f1
> 卡牌：`recover`

## 结论

Gate A 通过。Recover 沿现有普通卡牌公共路径完成卡牌选择、稳定 occupant ID + `HexCoord` 目标选择、三格 Timeline 预览/提交和 Resolve；目标由 10 HP 恢复并钳制到 100 HP。`VerticalSliceController.cs` 无差异，Domain/Application 无 `UnityEngine` 或 `UnityEditor` 引用。

## 自动化

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| 全量 EditMode | `107/107`，0 失败、0 跳过 | `editmode-results.xml` |
| Recover 公共 Scene PlayMode | `1/1`，0 失败、0 跳过 | `playmode-results.xml` |
| 渲染 Harness | PASS marker；10 HP -> 100 HP；5 张 1280x720 PNG | `gate-a-summary.json` |
| 静态边界 | Controller 零 diff；Domain/Application Unity 引用扫描为空；`git diff --check` 通过 | 本阶段 Git 门禁 |

EditMode 覆盖合法恢复、HP=0、满血、缺 range、错误 ID/coord、不同 ID 替换、Resolve 时消失/变满、MaxHP 钳制、三格 shape 边界、before/after 与失败纯度。PlayMode 使用 `CombatVerticalSlice.unity` 和控制器既有公共方法，不增加 Recover Controller 分支。

## 人工视觉检查

逐张打开 `recover-selected.png`、`recover-targeted.png`、`recover-timeline-valid.png`、`recover-before-resolve.png` 和 `recover-after-resolve.png`：

- 原 Recover 卡面比例正确，选中卡完整可见；七卡没有裁切右 HUD 或时间轴。
- 目标范围黄高亮、`TARGET-01 | LOCKED` 与三格绿色 Timeline 预览清晰可辨。
- 结算后 Timeline 保留 `RECOV` action，HUD 显示 `TARGET | HP 100`。
- 1280x720 下无文字重叠、关键控件裁切、缺失卡面或棋盘/Timeline 关键区域遮挡。

此前尝试在 `-nographics` PlayMode 中调用 URP `Camera.Render()`，被 Unity 明确拒绝；该失败运行未作为证据。最终截图由启用图形设备的 Editor Harness 生成并覆盖，且由 HP 结果断言和人工开图共同验收。
