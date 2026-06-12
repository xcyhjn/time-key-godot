# tile.gd 维护说明

日期：2026-06-12

## 当前职责

`scene/in_scene/tile.gd` 是地貌/建筑实体基类，承担地貌基础属性、状态组件、结算奖励、敌人意图协议、视觉挂接、血量状态和贴图切换。时间轴 shape 矩阵解析、默认敌人意图 `TimelineAction` 构造、血量纯规则计算、Broken 后死亡收尾执行和 protected 受击吸收规则已经拆出，但旧入口仍由主脚本负责写回成员、发信号或执行副作用。

## 已拆模块

已拆模块位于 `scene/in_scene/tile_modules/`：

- `controllers/TileDeathExecutionController.gd`：执行 Tile 已进入 Broken 后的状态组件清理、贴图回调、damage_rate 结果和拓扑通知，不判断血量、不释放节点、不选择贴图规则。
- `rules/TileTimelineShapeParser.gd`：把时间占位矩阵解析为坐标和尺寸，不读取场景树、不创建 TimelineAction。
- `rules/TileIntentActionFactory.gd`：为默认敌人意图创建 `TimelineAction`，不选择目标、不判断合法性、不修改血量或地图拓扑。
- `rules/TileHealthStateRules.gd`：计算 HP clamp、damage_rate 和下一主状态标签，不发信号、不切贴图、不通知 HexMap。
- `rules/TileDamageProtectionRules.gd`：判断受击时 protected 位是否吸收伤害，并返回新的副状态位，不扣血、不处理死亡或贴图。

## 当前耦合点

- 状态组件与中毒扩散会访问 `owner_battle`、邻居地块和 VFX。
- 结算奖励接口读取地貌规则、死亡状态和 used 状态。
- 敌人意图接口仍同时包含目标描述、目标范围、时间轴 shape 和优先级；默认行动创建已委托给 `TileIntentActionFactory.gd`，子类覆盖实现仍各自维护。
- 血量纯规则、死亡收尾执行和 protected 吸收规则已拆出，但 `take_damage()`、`set_health()`、`State_Update()` 和 `die()` 的旧入口仍负责调用顺序、写回成员、发信号和保持 Broken 占位语义。
- `timeline_shape_key`、`timeline_shape_coords` 和 `timeline_shape_size` 的旧入口还在主脚本内，解析细节已委托给 `TileTimelineShapeParser.gd`。

## 不要继续硬拆

- 不要重复拆死亡执行 controller 或 protected 受击规则，也不要同批拆贴图切换和子类敌人意图行动数据统一。
- 不要把 `owner_battle`、`map_data`、运行时地貌节点注册成 Resource。
- 不要在 tile 模块里直接查找场景树；需要地图信息时由主脚本或调用方传入。

## 后续可做

下一步可单独评估 `tex_toggle()` 的贴图选择边界，或多个敌方地貌子类的意图 action 数据是否能统一。它们都必须保持 `Blood_change`、Broken 贴图、`tile_topology_changed`、`effect_range`、`invalid_reason` 和 `TimelineAction` 数据契约不变。

## 验证入口

改动后加载 `res://scene/in_scene/in_scene.tscn`，检查敌方意图生成、时间轴占位、地貌血量变化、死亡贴图和地图拓扑重判。
