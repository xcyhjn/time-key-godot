# Wave 02B1 验证摘要

> 状态：通过
> 验证日期：2026-07-31
> Unity：6000.4.10f1
> 分支：`unity_7.31`

## 功能闭环

- 原 `card_asset/lighting.png` 与 `image/behide.png` 逐字节复制；SHA-256 分别为 `DD27CCC0F0A9982286958D130E73E106DF309B1A116DA68139E30FA398ACE3DC`、`3AFF3AE738AACB3F08DDEA3CD00D00C0960C1C87B49AF9F5D3EB50E28FED0E87`。
- 卡牌支持 idle、hover、selected/targeting/scheduling、右键/Escape 取消和输入隔离；选牌期间禁用轨道镜头，取消或提交后恢复。
- `TimelineGrid.CanPlace` 和 `CardPlaySession.PreviewTimeline` 无副作用；invalid 预览不占格，合法确认后才写入 `LIGHTING`。
- `lighting` 的 axial 范围投影到真实 3D 格；四个 yaw 下坐标集合稳定。固定 19 格 fixture 中 `(3,0)` 不存在，作为 missing 可观测且没有创建幽灵格。
- 确认放置后卡牌离开手牌；seed 731 结算保持 `lighting` 在敌方意图前执行，目标 `10 HP -> 0`，意图标记为 processed。

## 自动化结果

| 门禁 | 结果 | 证据 |
| --- | --- | --- |
| Wave A EditMode | 31/31 passed | `wave-a-editmode-results.xml` |
| 最终 EditMode | 31/31 passed，0 failed/skipped | `editmode-results.xml` |
| 最终 PlayMode | 15/15 passed，0 failed/skipped | `playmode-results.xml` |
| Agent 03 Cards PlayMode | 4/4 passed；并行阶段全量 12/12 | `../wave-02b-card-ui-agent/*.xml` |
| Agent 04 Targeting PlayMode | 4/4 passed | `../wave-02b-targeting-agent/playmode-results.xml` |
| Harness | scene 校验通过，12 张 PNG 通过非空/尺寸门禁 | `harness-summary.json` |
| Windows build | `Succeeded`，171055406 bytes | `harness-summary.json` |
| Windows Player | 退出码 0，`TIMEKEY_PLAYER_SMOKE_PASS` | 本摘要；原始日志仅本机保留 |

## 实际渲染检查

- `card-idle/hover/selected-1920x1080.png`：完整原卡面保持纵横比，hover 与 selected 位移/缩放可辨，底边未裁切。
- `card-selected-2560x1080.png`：超宽屏仍底部居中，卡牌、时间轴、目标和结算区互不遮挡。
- `target-range-yaw-000/090/180/270-1920x1080.png`：真实高度表面保持高亮，范围随世界旋转而非随屏幕漂移。
- `timeline-invalid/valid-1920x1080.png`：冲突位置红色，合法位置绿色，预览前后时间轴占用不变。
- `placed-1280x720.png`：确认后手牌隐藏，`LIGHTING` 写入时间轴；棋盘、时间轴、目标信息和按钮均在安全区。
- `resolved-1920x1080.png`：目标显示 `HP 0/10` 与 disabled，状态栏记录 damage 和已处理意图。

未发现空白画面、卡面拉伸、文字/卡面裁切、关键控件重叠、UI 输入穿透或镜头旋转导致的预览坐标漂移。局外 Godot 文件与流程未进入本切片。
