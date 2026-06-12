# timecoin_ui.gd 维护说明

日期：2026-06-12

## 当前职责

`scene/in_scene/timecoin_ui.gd` 是时间币 UI 的 composition root。它继续负责节点引用校验、连接 `GlobalTimecoin` 信号、刷新数值文本、播放获得/消耗/不足动画、维护活跃 tween 队列、恢复原始视觉状态和保留旧 shader 控制入口。

## 已拆模块

已拆模块位于 `scene/in_scene/timecoin_ui_modules/`：

- `bridges/TimecoinGlobalBridge.gd`：查找 `GlobalTimecoin`，不连接信号、不读写时间币、不刷新 UI。
- `presenters/TimecoinHourglassShaderController.gd`：准备沙漏 `ShaderMaterial` 并写入震动参数，不播放普通 tween 动画。
- `animation/TimecoinShakeTweenBuilder.gd`：把普通动画里的位置抖动片段追加到传入 Tween，不创建 Tween、不管理 active_tweens。

## 不要继续硬拆

- 不要重复拆 `GlobalTimecoin` 查找或沙漏 shader 控制。
- 不要同批改数值来源、信号协议和动画收尾状态。
- 不要把 `active_tweens` 管理直接塞进 shader controller。

## 后续可做

下一步优先拆纯动画小边界：

1. 获得/消耗/不足动画 runner：必须另开批次，因为它们共享 `active_tweens`、原始位置、缩放、颜色和完成回调。
2. tween 状态清理 controller：只有在能保持旧完成回调语义时再评估。

## 验证入口

改动后加载 `res://scene/in_scene/in_scene.tscn`，检查时间币初始刷新、获得动画、消耗动画、余额不足动画和沙漏 shader 震动。
