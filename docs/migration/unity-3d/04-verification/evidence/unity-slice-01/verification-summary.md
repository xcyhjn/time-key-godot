# Unity Slice 01 验证摘要

> 状态：通过
> 验证日期：2026-07-31
> Unity：6000.4.10f1 / URP 17.4.0 / Windows 64-bit Development Player

## 自动化结果

| 检查 | 结果 |
| --- | --- |
| 静态程序集编译 | 7/7 通过 |
| asmdef JSON | 7/7 通过 |
| 领域反射测试 | 11/11 通过 |
| Unity EditMode | 19/19 通过，0 失败，0 跳过 |
| Unity PlayMode | 2/2 通过，0 失败，0 跳过 |
| 真实 fixture SHA-256 | `7DEC6590C409E3A5C9BB2E83B8E833FC1E6D9106EF5706E223F909D1E763FB0B`，源/目标一致 |
| Scene harness | 通过；36 个时间轴槽、9 个 hex mesh、正交相机、Screen Space Camera Canvas |
| Windows build | `Succeeded`，163,654,546 bytes |
| 构建产物启动 | D3D12 Player 退出码 0，日志标记 `TIMEKEY_PLAYER_SMOKE_PASS` |

结构化原始结果保存在同目录的 `editmode-results.xml`、`playmode-results.xml` 与 `harness-summary.json`。Unity Editor/Player 原始日志保留在本机但被 scoped ignore 排除，避免把许可证握手和机器标识提交到 Git。

## 视觉审查

- `initial-1920x1080.png`：目标为红色且显示 10/10 HP；12×3 时间轴完整，固定意图位于 03-B；卡牌和结算面板完整。
- `resolved-1920x1080.png`：LIGHTING 位于 01-A；目标变绿且显示 0/10；状态明确显示敌方意图已处理。
- `resolved-1280x720.png`：与 1080p 语义一致；标题、状态、时间轴 36 格、卡牌、目标与按钮均未裁切或重叠。
- 三张截图像素门禁 luminance range 分别为 0.889、0.908、0.885，均非空白或单色画面。

## 允许差异

Godot 是 2D 像素战场，Slice 01 是程序化 3D 白盒；玩法坐标仍使用冻结的 flat-top axial 映射。敌方意图保持可见并按顺序标记为已处理，但不擅自补齐 Godot 当前为空的 intent command 解析逻辑。
