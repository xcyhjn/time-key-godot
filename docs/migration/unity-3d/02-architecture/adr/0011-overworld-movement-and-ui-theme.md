# ADR 0011: Overworld Movement and Resource UI Theme

## Status

Accepted for Wave 03P on `unity_7.31`.

## Decision

局外移动的唯一运行时状态由 Unity-free `TimeKey.Domain.OverworldMovement.OverworldMovementModel` 持有。节点身份使用既有 `MapNodeId`；轴向坐标使用 `AxialHexCoord`，合法移动只允许距离为 1 的六轴相邻节点。请求、提交、取消和结果均通过 typed command/result，失败不产生部分状态变化；Presenter 只负责在已保存的 Scene/Prefab 节点上驱动动画和输入锁。

uGUI 使用资源化 `TimeKeyUiTheme`、保存于正式 Prefab 的 `UiThemeScope` 与 `UiThemeBinder`。Silver 是唯一玩家可见字体；Godot ARK Pixel 仅作为本地审计参考。局部样式只能通过 `LocalOverride` 的显式字段覆盖，不能替换整棵 Theme。稳定 Canvas、MapViewport、MapHost、EdgeHost、PlayerMarker、HUD 和确认层保留在 Prefab/Scene；运行时只实例化动态节点内容。

## Consequences

- 节点点击、拖拽、滚轮缩放、键盘平移和 SceneFlow transition lock 共享同一输入门禁。
- `OutOfBattleShellState` 只记录当前 room identity；地图结算通过 `ArrivalCommitted` 进入既有 SceneFlow/typed boundary。
- Theme 资源校验器可以在 Editor-only 环境发现缺字体、缺 Sprite、无效 9-slice border 和重复 style id。
- Godot 原主题中的压缩字段保持 `UNKNOWN`，未经 ResourceLoader/Inspector 证据不得复制或臆造；未验证授权的 ARK/图片不进入 Unity。

## Verification

`editmode-full.xml` 376/376、`playmode-full.xml` 108/108、`playmode-overworld-visual.xml` 1/1、`playmode-sceneflow-roundtrip.xml` 2/2；三视口与动态 resize PNG 位于 `04-verification/evidence/overworld-movement-theme-gate-d/`。
