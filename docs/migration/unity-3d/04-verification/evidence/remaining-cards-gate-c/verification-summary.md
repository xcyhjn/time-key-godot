# Remaining Cards Gate C 验证摘要

> 日期：2026-08-02
> Unity：6000.4.10f1
> 卡牌：`wind`、`tornado`

## 结论

Gate C 通过。Wind/Tornado 使用独立即时 `TimelineClearSession`，不选地图目标、不创建普通 `TimelineAction`、不占新格；mask 内任一格命中会按 action identity 去重并移除该 action 的完整 shape，空清合法，玩家/敌人均不筛除。

## 自动化

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| 全量 EditMode | `152/152`，0 失败、0 跳过 | `editmode-results.xml` |
| Gate C Scene/Presentation PlayMode | `4/4`，0 失败；Wind、Tornado、2×2/12×1 三态与恢复 | `playmode-results.xml` |
| Scene authoring | 定向 authoring PASS；保存 `ClearTimelinePreview` 组件、Presenter 引用与 36 格默认外观 | `authoring.log`（按规则不入 Git） |
| 渲染 Harness | PASS marker；Wind removed=1、Tornado empty removed=0；9 张 1280×720 PNG | `gate-c-summary.json` |

Domain/Application 覆盖边界、空清、多格 action 去重、多个 action、玩家/敌人、取消、重复 preview/commit、ordinary API 回归和失败纯度。Presentation 覆盖重复 Bind/Show/Clear、outline 唯一、颜色/文字恢复与完整 action UI 清除。

## 人工视觉检查

- `wind-selected.png`：原 Wind 卡图、七卡手牌、无地图目标提示和原敌方 `INTENT` 可读。
- `wind-clear-invalid.png`：右边界可见 mask 全为红色 `!`，状态明确显示 out of bounds，不依赖颜色单一表达。
- `wind-clear-hit.png`：2×2 中三个空格为天蓝 `○`，命中敌方 action 的格为绿色 `HIT`；HUD 显示 `HITS 1`。
- `wind-after-cancel.png`：预览、marker 与 outline 全部消失，原 `INTENT` 和七卡布局恢复。
- `wind-after-clear.png`：敌方 action 的完整表现消失，所有格恢复默认序号，HUD 显示 `REMOVED 1`。
- `tornado-selected.png`：原 Tornado 卡图与无地图目标提示可读。
- `tornado-clear-invalid.png`：偏移后的可见 11 格均为红色 `!`，第 12 格越界；整片状态一致且无裁切。
- `tornado-clear-empty.png`：合法 12×1 空清整行显示天蓝 `○`，敌方 action 位于另一行且不受影响。
- `tornado-after-empty-clear.png`：预览完全恢复，原敌方 `INTENT` 保留，HUD 显示 `REMOVED 0`。

所有截图均为实际 1280×720 渲染；未发现文字裁切、关键 UI/棋盘重叠、缺图、颜色不可辨或取消/提交后的预览残留。实际图像人工检查通过，不以非空像素或退出码替代视觉结论。

当前无用户决策阻塞。
