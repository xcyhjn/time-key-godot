# ADR 0001：Unity 工程、渲染与 UI 栈

> 状态：已接受
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：本机 Unity 包、Godot UI 交互、3D 产品边界

## 背景

迁移需要同时表达 3D 六边形世界和高密度卡牌/时间轴 UI。首切片还必须在离线可用的本机 Unity 安装中可靠导入、测试和截图。

## 决策

- 工程目录：`unity/`。
- Editor：Unity `6000.4.10f1`。
- 渲染：URP `17.4.0`。
- UI：uGUI `2.0.0`，Canvas 使用 screen-space camera 与 CanvasScaler。
- 测试：Unity Test Framework `1.6.0`。
- 首切片不引入 Input System、Cinemachine、Addressables、Shader Graph 自定义图或 VFX Graph。

## 理由

uGUI 能直接支持卡牌按钮、网格时间轴、tooltip 和运行时拖放/点击，且与当前 Control 树交互模型接近。URP 提供稳定的 3D Lit/Unlit 基线。附加包当前没有不可替代价值，引入会扩大导入和生命周期风险。

## 后果

- 输入首切片使用单一旧式 `Input`/uGUI event 路径；若后续引入 Input System，需单独 ADR 并一次性迁移，不能长期双栈。
- 3D 世界和信息 UI 边界清楚，但世界目标选择需要 Presentation adapter 将射线命中转为领域 target ID。
- 后续可以在不改变 Domain 的前提下替换材质、动画或 UI 表现。

## 被否决方案

- UI Toolkit：首切片运行时卡牌/时间轴交互收益不足以抵消新适配成本。
- Built-in Render Pipeline：与目标 3D 方向和后续材质/VFX 路线不一致。
- 同时维护 uGUI 与 UI Toolkit：增加焦点、输入和样式系统的重复成本。
