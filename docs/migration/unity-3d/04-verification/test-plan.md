# Unity Slice 01 / Wave 02A 测试计划

> 状态：已执行并通过
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
- 场景存在透视轨道相机、19 格、目标、Canvas、36 个时间轴槽。
- elevation 1 含 `Block-0`/`Block-1`，层间距严格为 `0.32`，每层有独立 collider 和 FBX 可视对象。
- 0/90/180/270 度均能选中同一目标；俯仰与距离输入被夹紧。
- UI 覆盖坐标阻止世界 raycast，空白区域仍能选格。
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
- 四个方位均有六边形实体、两层高地、目标、相机视角和 UI。
- 12×3 时间轴不越界，按钮/状态文字不重叠，1280×720 仍可读。
- 结算前/后至少一张截图能证明 HP 或目标状态变化。

## 执行结果

| 门禁 | 结果 | 结构化证据 |
| --- | --- | --- |
| 静态编译/领域反射测试 | 7 个程序集、7 个 asmdef、11 个领域用例通过 | `evidence/unity-slice-01/verification-summary.md` |
| Unity EditMode | 19/19，通过 0 失败 | `evidence/unity-slice-01/editmode-results.xml` |
| Unity PlayMode | 2/2，通过 0 失败 | `evidence/unity-slice-01/playmode-results.xml` |
| Harness/build | scene 校验、三张 PNG、Windows build 均通过 | `evidence/unity-slice-01/harness-summary.json` |
| Windows Player | 固定交互完成，`TIMEKEY_PLAYER_SMOKE_PASS`，退出码 0 | `evidence/unity-slice-01/verification-summary.md` |
| 人工视觉审查 | 1920×1080 与 1280×720 无裁切、重叠或缺失控件 | `evidence/unity-slice-01/verification-summary.md` |
| Wave 02A Unity PlayMode | 4/4，四向选择、UI 门禁、堆叠和结算均通过 | `evidence/unity-slice-02-board/playmode-results.xml` |
| Wave 02A Harness/build | 5 张 PNG、Windows build 与 Player 冒烟通过 | `evidence/unity-slice-02-board/verification-summary.md` |
| 地块几何 | 两变体各 120 三角形、0 非流形边，双层无缝 | `evidence/hex-tile-agent/model-validation.json` |

## Wave 02B1 计划门禁

- EditMode：`CanPlace` 无副作用；合法、越界、冲突、无目标、重复 commit、cancel 后 commit。
- PlayMode：idle/hover/select/cancel；未选牌不能选目标；未选目标不能预览/放置；UI 不泄漏世界 raycast。
- PlayMode：Selected/Targeting/Scheduling 暂停 orbit，右键优先取消；退出后恢复 orbit。
- PlayMode：有效/无效时间轴预览、确认占格、`lighting` 在敌人意图前结算、重复 Build 不复制监听。
- 视觉：`1920×1080` idle/hover/selected/target/timeline/resolved，`1280×720` placed，`2560×1080` selected。
- 四向：0/90/180/270 yaw 的相同 `HexCoord` 范围保持一致且不被卡牌 UI 遮住。
- 像素检查：卡牌区域、时间轴预览区域分别非空且状态间有差异；整图非单色不能替代局部证据。
- 回归：现有 EditMode 19/19、PlayMode 4/4、Windows build 与 Player smoke 不回退。
