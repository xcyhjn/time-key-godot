# Wave 02A 局内 3D 棋盘验证摘要

> 状态：Passed
> 日期：2026-07-31
> Unity：6000.4.10f1 / URP 17.4.0 / Windows 64-bit Development Player

## 结果

- EditMode：19/19 通过，0 失败。
- PlayMode：4/4 通过，0 失败。
- 场景：19 格透视战斗棋盘，36 个时间轴槽。
- 镜头：yaw 0/90/180/270 度均已渲染并通过同目标 raycast 选择。
- 高度：高地由两个 `0.32` 高的独立 FBX mesh/collider 实体堆叠，无悬空。
- 美术：Blender 草地/裸土地块已在 Unity 实际渲染；原 `center_altar.png` 作为相机面向 billboard。
- 构建：Windows Player `Succeeded`，大小 `164643534` bytes。
- 运行时：Player 固定交互路径输出 `TIMEKEY_PLAYER_SMOKE_PASS`，退出码 0。

## 视觉证据

- `board-yaw-000-1920x1080.png`
- `board-yaw-090-1920x1080.png`
- `board-yaw-180-1920x1080.png`
- `board-yaw-270-1920x1080.png`
- `resolved-1280x720.png`

五张图像均通过像素非空门禁和人工检查：棋盘、高地、目标和 UI 无裁切或不可读重叠。

## 结构化文件

- `editmode-results.xml`
- `playmode-results.xml`
- `harness-summary.json`
- `../hex-tile-agent/model-validation.json`

原始 Unity `.log` 仅用于本机调试，不作提交证据。
