# Unity Slice 01 测试计划

> 状态：已冻结，等待执行
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：harness 设计、首切片契约、Unity Test Framework 1.6.0

## EditMode

- `HexCoord` 相等性与 axial→XZ 已知点。
- 时间轴接受合法单格，拒绝越界与冲突。
- 多格 action 只执行一次；顺序为 x 后 y。
- damage 100 把 10 HP 钳制为 0。
- 敌人意图记录位于玩家 action 后。
- 同 seed 731 两次得到完全相同 snapshot。
- 真实 `lighting.json` 得到稳定 ID、damage、range 与 shape。

## PlayMode

- 加载 `CombatVerticalSlice.unity` 不产生 Error/Exception。
- 场景存在正交相机、7+ 六边形 mesh、目标、Canvas、36 个时间轴槽。
- 通过控制器公共方法完成选卡、选目标、放置与结算。
- HP 文本和 3D 目标状态从 10 更新到 0；状态显示意图已处理。
- 1920×1080 与 1280×720 布局锚点不导致主要控件超出安全区。

## Batchmode 与构建

- 首次导入/编译退出码 0。
- EditMode 和 PlayMode XML 中失败数为 0。
- scene validation 退出码 0。
- Windows Player build 结果为 Succeeded；build 产物忽略，不入 Git。

## 视觉审查

- PNG 尺寸与文件名匹配，像素不是全透明/单色。
- 六边形地面、目标、相机视角和 UI 均存在。
- 12×3 时间轴不越界，按钮/状态文字不重叠，1280×720 仍可读。
- 结算前/后至少一张截图能证明 HP 或目标状态变化。
