# Wave 02B3R 效果框稳定化 Gate E

> 日期：2026-08-03
> 结果：PASS

## 关闭范围

- `TimelineActionFrame` 按真实占用格生成填充和外围边缘，不再用 bounding box 填满 Tower/Poison 缺格。
- 首次 layout、父尺寸变化和同实例 runtime resize 都会重算吸附；Scene rebind 会原子销毁旧 frame/index。
- `TimelineActionIdentity`、`CardInstanceId`、card stable ID、地图 source/target runtime ID 保持分离，并通过 `ActionIdentityIndex` 双向查找。
- Overlay 优先级固定为 `Disabled > Resolving > CardTargeting > Clear > Drag > Scheduling > IdleActionHover > None`；Binding Refresh 从真实 Application phase 恢复 owner。
- 无卡地块检查支持替换、同格复点清除、空地/Escape/短右键清除；右键拖拽只旋转；选卡、取消、提交、结算、rebind 与全局输入锁均清理或拒绝陈旧状态。
- Timeline pointer exit 使用现有 `CancelCard` 退出真实 `TimelinePreview`，不再只清 Presentation。

## 验证

- Gate 0 红灯：EditMode contract `0/3`，PlayMode geometry `0/2`，均为预期失败。
- 定向 EditMode：`8/8`。
- 几何与既有 frame PlayMode：`6/6`。
- 地块检查/取消/rebind/全局锁 PlayMode：`2/2`。
- 三视口与动态 resize：普通 action `1/1`，Poison/Tower `1/1`；9 张 PNG 已逐图复核。
- 最终 full EditMode：`351/351`。
- 最终 graphical D3D12 PlayMode：`107/107`。
- Windows x64 Development build：190 files，`162094283` bytes；launcher SHA-256 `FE5E81292DF0F6591DCEEC172141B6F0F22D7CBB853DE83786B725E1BC68BEEE`；Silver attribution 存在。
- 实际可见 Player smoke：`PASS=1 / Exception=0 / Error=0`，Player 已自动退出。

原始 `.log` 留在本地但不进入检查点；检查点只保留最终 XML、PNG 和本总结。前置 Combat Shell Gate E 的三轮 SceneFlow、内存、render counter 与回程 Player 证据继续继承，未覆盖。

## 交接

`NEXT_STAGE_COMBAT_SHELL_AND_SCENE_FLOW_PROMPT.md` 已恢复核对，其 Gate A-E 均已关闭，因此不存在可继续执行的 Combat Shell 未完成 Gate。当前状态账本中的后继队列保持不变；后续 Combat UI 必须继承本阶段三视口和动态 resize 证据。
