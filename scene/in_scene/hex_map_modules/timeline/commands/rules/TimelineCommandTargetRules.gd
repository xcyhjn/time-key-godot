class_name TimelineCommandTargetRules
extends RefCounted


## 读取地块上的运行期实体。
## 所有命令统一通过 stack `occupant` 元数据寻址，避免每个命令重复写空地判断。
static func get_occupant(tile: Area2D) -> Node:
	if not is_instance_valid(tile) or not tile.has_meta("occupant"):
		return null
	var entity: Variant = tile.get_meta("occupant")
	if entity is Node and is_instance_valid(entity):
		return entity
	return null


## 读取地块在 HexMap 中的坐标。
## 建造命令需要坐标来创建新实体，其他命令可继续只依赖 occupant。
static func get_tile_coord(tile: Area2D, hex_map: Node) -> Variant:
	if not is_instance_valid(tile):
		return null
	if not is_instance_valid(hex_map) or not object_has_property(hex_map, &"stack_nodes"):
		return null
	return hex_map.stack_nodes.find_key(tile)


## 判断一个地块是否仍可建造。
## 这是时间轴结算时的最终防线，和放牌前 HexTargetRules 的 build 规则保持同样字段判断。
static func can_build_on_tile(tile: Area2D, hex_map: Node) -> bool:
	if not is_instance_valid(tile):
		return false
	if is_instance_valid(get_occupant(tile)):
		return false

	var coord: Variant = get_tile_coord(tile, hex_map)
	if coord == null:
		return false

	return is_map_data_empty_at(Vector2i(coord), hex_map)


## 判断 map_data 中某坐标是否没有运行期地貌。
## 同时检查 `landform` 和 `landform_in`，兼容旧数据字段。
static func is_map_data_empty_at(coord: Vector2i, hex_map: Node) -> bool:
	if not is_instance_valid(hex_map):
		return false
	if not object_has_property(hex_map, &"map_data") or not hex_map.map_data.has(coord):
		return false

	var tile_data: Variant = hex_map.map_data[coord]
	if typeof(tile_data) != TYPE_DICTIONARY:
		return false
	if tile_data.has("landform") and is_instance_valid(tile_data["landform"]):
		return false
	if tile_data.has("landform_in") and is_instance_valid(tile_data["landform_in"]):
		return false
	return true


## 伤害目标最终校验。
## 实体需要支持 `take_damage()`；如果存在 HP 字段，则 HP 必须大于 0。
static func can_damage_entity(entity: Node) -> bool:
	if not is_instance_valid(entity) or not entity.has_method("take_damage"):
		return false
	var hp: Variant = entity.get("HP")
	return hp == null or float(hp) > 0.0


## 回复目标最终校验。
## 实体需要支持 `heal()`；如果存在 HP/Max_Blood，则只允许未满血目标。
static func can_recover_entity(entity: Node) -> bool:
	if not is_instance_valid(entity) or not entity.has_method("heal"):
		return false

	var hp: Variant = entity.get("HP")
	var max_hp: Variant = entity.get("Max_Blood")
	if hp != null and max_hp != null:
		return float(hp) < float(max_hp)
	return true


## 中毒目标最终校验。
## 实体需要支持 `add_status()`；如果存在 HP 字段，则 HP 必须大于 0。
static func can_apply_poison(entity: Node) -> bool:
	if not is_instance_valid(entity) or not entity.has_method("add_status"):
		return false
	var hp: Variant = entity.get("HP")
	return hp == null or float(hp) > 0.0


## 动态属性检测工具。
## 命令层接收的是注入后的 Node 引用，先确认属性存在可以避免访问非 HexMap 节点。
static func object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
