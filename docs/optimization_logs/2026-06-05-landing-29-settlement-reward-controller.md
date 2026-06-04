# 第 29 批落地：结算奖励状态 Controller 拆分

日期：2026-06-05

## 本次目标

本次继续处理 `HexMap` 中的跨系统耦合，把结算奖励的“状态扫描与 payload 构造”从地图主控里拆出去。

上一阶段已经把结算奖励的地图表现拆到 `SettlementRewardPresenter.gd`，但 `HexMap` 里仍保留：

- 扫描哪些 stack 拥有奖励建筑。
- 从 stack metadata 和 `map_data` 双通道读取建筑。
- 判断建筑是否具备奖励资格。
- 构造 `reward_info`。
- 判断奖励是否已经使用。
- 奖励页关闭后写入 used 状态。

这些属于奖励状态逻辑，不是地图表现逻辑。本批新增 `SettlementRewardController.gd` 集中管理。

## 修改文件

### `scene/in_scene/hex_map_modules/rewards/SettlementRewardController.gd`

新增结算奖励状态 controller，职责是：

- `collect(stack_nodes, map_data, config)`：扫描当前地图，返回 `reward_stacks`、`reward_stack_data`、`stacks_to_clear`。
- `is_stack_available(stack, reward_stack_data, config)`：判断一个奖励 stack 当前是否还能点击。
- `mark_used(reward_info)`：奖励页确认后，标记对应建筑已使用。
- 保留旧双通道建筑读取：优先读 stack metadata 的 `occupant`，再读 `map_data[coord].landform`。
- 保留旧绑定模式：根据 HexMap 传入的 `DEAD/ALIVE` enum 数值判断死亡建筑或存活建筑。

这个模块不处理：

- shader 高亮。
- tooltip 创建、刷新或动画。
- 打开奖励场景。
- `settlement_reward_requested` 信号发出。
- 相机、输入锁定和地图视觉锁定。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `SETTLEMENT_REWARD_CONTROLLER` preload。
- 新增 `_settlement_reward_controller` 实例。
- `_collect_settlement_reward_stacks()` 改为调用 controller，并把结果交给 presenter 刷新。
- `_is_settlement_reward_stack_available()` 改为调用 controller。
- `mark_settlement_reward_used()` 改为调用 controller 写 used 状态。
- 新增 `_build_settlement_reward_controller_config()`，把奖励模式和绑定 enum 快照传给 controller。
- 删除 HexMap 内部的奖励资格扫描、reward_info 构造和 used 状态细节函数。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

仍由 HexMap 导出并传给 controller 的配置：

- `settlement_reward_bind_state`

仍由 HexMap 导出并传给 presenter 的表现配置：

- `settlement_reward_tooltip_offset`
- `settlement_reward_tooltip_size`
- `settlement_reward_tooltip_font_size`
- `settlement_reward_tooltip_z_index`
- `settlement_reward_highlight_color`
- `settlement_reward_hover_color`
- `settlement_reward_highlight_blend`
- `settlement_reward_hover_blend`
- `settlement_reward_tooltip_hover_scale`

## 行为边界

本次保持以下行为不变：

- 只有敌方 `landform` 才参与结算奖励。
- 建筑仍需要实现 `has_settlement_reward()` 才能被扫描到。
- `reward_type` 和 `reward_label` 仍优先由建筑脚本提供。
- used 状态仍优先调用建筑的 `mark_settlement_reward_used()`。
- 如果旧建筑只有 `settlement_reward_used` 字段，仍直接写字段。
- `HexMap` 仍发出 `settlement_reward_requested(reward_info)`。
- 具体打开奖励页仍由 `in_scene.gd` 负责。

## 当前完成度回顾

已完成：

- 结算奖励状态扫描离开 `HexMap`。
- `SettlementRewardPresenter.gd` 继续只管视觉表现。
- `HexMap` 只保留模式切换、信号发出、presenter 刷新和输入状态编排。

仍待处理：

- 建筑奖励接口仍是鸭子类型，`has_method()` 暂时保留。
- 奖励页内部的 CardManager 查找链暂未迁移到 `CardManagerLocator.gd`。

## 后续建议

下一批建议继续抽 MainBoard tooltip 适配器，把目标 hover tooltip 的旧 UI 接口从 HexMap 中移出。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归时重点看：

- 胜利后进入结算奖励模式。
- 可奖励建筑是否显示高亮和 tooltip。
- hover 可奖励建筑时是否仍有白色悬浮和 tooltip 缩放。
- 点击奖励建筑是否仍打开对应奖励页。
- 奖励页关闭后，已使用建筑是否取消高亮并禁用点击。

验证结果：

- `git diff --check` 通过。
- 项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，局内建图、CardManager 注册和场景初始化日志正常。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB/RID/resource 退出提示，本批没有改动这些旧问题。
