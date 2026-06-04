# 功能: 时间轴建造命令，负责把 built 类卡牌效果结算为地图上的运行期建筑。
# 核心逻辑: execute() 校验目标地块为空，根据 creation 注册表创建实体，写回 HexMap 的 map_data/occupant，再触发通用像素入场 VFX。
class_name BuiltCommand
extends EffectCommand

const TARGET_RULES := preload("res://scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd")
const CREATION_SCRIPT_BY_ID := {
	"tower": preload("res://scene/in_scene/enermy/tower.gd"),
}

var creation_id: StringName = &""
var build_count: int = 1


## 初始化建造命令。
## `creation_id` 对应 CREATION_SCRIPT_BY_ID，`build_count` 限制一次结算最多创建几个实体。
func _init(new_creation_id: StringName, new_build_count: int = 1) -> void:
	creation_id = new_creation_id
	build_count = maxi(1, new_build_count)


## 执行建造效果。
## 目标地块在这里再次通过 TimelineCommandTargetRules 校验，防止放牌后到结算前地块被占用。
func execute(tree: SceneTree) -> void:
	if not is_instance_valid(hex_map) or target_tiles.is_empty() or creation_id == &"":
		return

	var created_count: int = 0
	for tile in target_tiles:
		if created_count >= build_count:
			break
		if not TARGET_RULES.can_build_on_tile(tile, hex_map):
			continue

		var coord: Variant = TARGET_RULES.get_tile_coord(tile, hex_map)
		if coord == null:
			continue

		var entity: landform = _create_creation(Vector2i(coord))
		if not is_instance_valid(entity):
			continue

		if _register_creation(tile, Vector2i(coord), entity):
			created_count += 1
		else:
			entity.queue_free()

	if created_count > 0:
		_notify_map_changed()


## 根据 creation_id 创建对应的 landform 实体。
## 创建失败时返回 null，不在命令层做地图写入。
func _create_creation(coord: Vector2i) -> landform:
	var script: Script = CREATION_SCRIPT_BY_ID.get(String(creation_id), null)
	if script == null:
		push_warning("未知建造 creation: %s" % String(creation_id))
		return null

	var entity: Variant = script.new(coord, hex_map)
	if not is_instance_valid(entity) or not (entity is landform):
		if is_instance_valid(entity):
			entity.queue_free()
		return null

	return entity as landform


## 把新建实体交给 HexMap 的运行期注册入口。
## 地图数据、节点挂接、贴图生成和当前视角同步都由 HexMap 统一处理。
func _register_creation(_tile: Area2D, coord: Vector2i, entity: landform) -> bool:
	# 建造效果只负责提交实体；地图数据、节点挂载、贴图生成和视角同步统一交给 HexMap。
	if hex_map.has_method("register_runtime_landform"):
		return bool(hex_map.call("register_runtime_landform", coord, entity, String(creation_id)))
	return false


## 通知地图相关系统刷新。
## 保留旧行为：建造成功后刷新拓扑、敌人 roster 和交互状态。
func _notify_map_changed() -> void:
	if hex_map.has_signal("tile_topology_changed"):
		hex_map.tile_topology_changed.emit()
	if hex_map.has_signal("enemy_roster_changed"):
		hex_map.enemy_roster_changed.emit()
	if hex_map.has_method("_refresh_stack_interactivity"):
		hex_map.call("_refresh_stack_interactivity")
