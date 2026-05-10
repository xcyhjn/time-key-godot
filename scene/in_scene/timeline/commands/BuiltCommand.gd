# 功能: 时间轴建造命令，负责把 built 类卡牌效果结算为地图上的运行期建筑。
# 核心逻辑: execute() 校验目标地块为空，根据 creation 注册表创建实体，写回 HexMap 的 map_data/occupant，再触发通用像素入场 VFX。
class_name BuiltCommand
extends EffectCommand

const CREATION_SCRIPT_BY_ID := {
	"tower": preload("res://scene/in_scene/enermy/tower.gd"),
}

var creation_id: StringName = &""
var build_count: int = 1


func _init(new_creation_id: StringName, new_build_count: int = 1) -> void:
	creation_id = new_creation_id
	build_count = maxi(1, new_build_count)


func execute(tree: SceneTree) -> void:
	if not is_instance_valid(hex_map) or target_tiles.is_empty() or creation_id == &"":
		return

	var created_count: int = 0
	for tile in target_tiles:
		if created_count >= build_count:
			break
		if not _can_build_on_tile(tile):
			continue

		var coord: Variant = _get_tile_coord(tile)
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


func _can_build_on_tile(tile: Area2D) -> bool:
	if not is_instance_valid(tile):
		return false

	if tile.has_meta("occupant"):
		var occupant: Variant = tile.get_meta("occupant")
		if is_instance_valid(occupant):
			return false

	var coord: Variant = _get_tile_coord(tile)
	if coord == null:
		return false

	if not _is_map_data_empty_at(Vector2i(coord)):
		return false

	return true


func _get_tile_coord(tile: Area2D) -> Variant:
	if not is_instance_valid(hex_map) or not _object_has_property(hex_map, &"stack_nodes"):
		return null
	return hex_map.stack_nodes.find_key(tile)


func _is_map_data_empty_at(coord: Vector2i) -> bool:
	if not _object_has_property(hex_map, &"map_data") or not hex_map.map_data.has(coord):
		return false

	var tile_data: Variant = hex_map.map_data[coord]
	if typeof(tile_data) != TYPE_DICTIONARY:
		return false

	if tile_data.has("landform") and is_instance_valid(tile_data["landform"]):
		return false
	if tile_data.has("landform_in") and is_instance_valid(tile_data["landform_in"]):
		return false

	return true


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


func _register_creation(_tile: Area2D, coord: Vector2i, entity: landform) -> bool:
	# 建造效果只负责提交实体；地图数据、节点挂载、贴图生成和视角同步统一交给 HexMap。
	if hex_map.has_method("register_runtime_landform"):
		return bool(hex_map.call("register_runtime_landform", coord, entity, String(creation_id)))
	return false


func _notify_map_changed() -> void:
	if hex_map.has_signal("tile_topology_changed"):
		hex_map.tile_topology_changed.emit()
	if hex_map.has_signal("enemy_roster_changed"):
		hex_map.enemy_roster_changed.emit()
	if hex_map.has_method("_refresh_stack_interactivity"):
		hex_map.call("_refresh_stack_interactivity")


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
