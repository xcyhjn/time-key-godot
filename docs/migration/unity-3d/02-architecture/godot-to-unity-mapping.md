# Godot 到 Unity 映射

> 状态：首切片映射已冻结
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Godot 场景/脚本侦察、实际战斗流程、3D 边界决策

## 系统映射

| Godot 现状 | Unity 目标 | 首切片 |
| --- | --- | --- |
| `game_start.tscn` 与手动切场 | 显式 bootstrap 与场景上下文 DTO | 单场景 composition root |
| `Global*` / `Signal_Bus` autoload | 实例服务、C# event、显式依赖 | 不迁移全局单例 |
| `card-framework` | 自有纯 C# 卡牌/时间轴规则 | 只实现 `lighting` 与单格放置 |
| `hex_map.gd` axial 坐标 | `HexCoord` + XZ 世界映射 | 固定白盒布局 |
| 2D TileMap/Node2D 战场 | 3D mesh、碰撞体、正交斜视相机 | 程序化六边形 mesh |
| Control 卡牌/时间轴 | screen-space camera uGUI | 12×3 网格、卡牌和状态 |
| CFG 无版本存档 | `SaveEnvelope` schema version | 只冻结接口，不导入旧档 |
| `canvas_item` shader | URP 材质/Shader Graph/VFX 按需重做 | 仅 URP Lit/Unlit |

## 坐标、单位与朝向

- 逻辑坐标：flat-top axial `(q, r)`。
- Unity 地面：XZ 平面；Y 轴表示高度。
- 世界映射：`x = 1.5 * radius * q`，`z = sqrt(3) * radius * (r + q / 2)`，`y = elevation * elevationStep`。
- 首切片 `radius = 1`、`elevationStep = 0.35`，一个 axial 邻接距离就是一个逻辑格。
- 相机为正交斜视，向世界原点观察；世界模型不承担卡牌文字与时间轴信息。

## 数据语义

- 卡牌稳定 ID 是文件 basename/`name`，`lighting` 的历史拼写保持不变。
- JSON 中未映射的中文描述字段允许由解析器忽略；首切片必须读取 `name`、`effects`、`effect_range`、`shape` 和 `id`。
- 时间轴原点在左上，12 列×3 行，`x` 先于 `y` 结算。
- 目标 HP 使用整数并钳制到零；`lighting` 的 `damage=100` 对 10 HP 目标结果为 0。
- 当前 Godot 敌人意图仅形成可见占位，建筑行为在整条时间轴后执行；首切片保留“玩家 action 后记录敌人意图已处理”的可观察顺序，不声称已复刻完整建筑 AI。

## 不做逐行映射

Godot 节点路径、信号名和 autoload 生命周期不是兼容接口。Unity 只复建可观察规则、数据 ID、结算顺序和交互结果；节点组织可按 Unity 生命周期重新组合。
