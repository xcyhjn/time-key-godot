# tile.gd 维护说明

日期：2026-06-12

## 当前职责

`scene/in_scene/tile.gd` 是地貌/建筑实体基类，承担地貌基础属性、状态组件、结算奖励、敌人意图协议、视觉挂接、血量状态、贴图切换和时间轴 shape 解析。

## 当前耦合点

- 状态组件与中毒扩散会访问 `owner_battle`、邻居地块和 VFX。
- 结算奖励接口读取地貌规则、死亡状态和 used 状态。
- 敌人意图接口同时包含目标描述、目标范围、时间轴 shape、行动创建和优先级。
- 血量状态会触发贴图切换、状态清理和 `tile_topology_changed`。
- `timeline_shape_key`、`timeline_shape_coords` 和 `timeline_shape_size` 的解析还在主脚本内。

## 不要继续硬拆

- 不要同批拆血量死亡状态、贴图切换和敌人意图行动创建。
- 不要把 `owner_battle`、`map_data`、运行时地貌节点注册成 Resource。
- 不要在 tile 模块里直接查找场景树；需要地图信息时由主脚本或调用方传入。

## 后续可做

下一步低风险拆分是时间轴矩阵 shape 解析。推荐先拆 `TileTimelineShapeParser.gd`，只返回 `coords` 和 `size`，由 `tile.gd` 旧入口继续写回成员变量。

更高风险的候选是血量状态服务和结算奖励 adapter，等 shape 解析稳定后再评估。

## 验证入口

改动后加载 `res://scene/in_scene/in_scene.tscn`，检查敌方意图生成、时间轴占位、地貌血量变化、死亡贴图和地图拓扑重判。
