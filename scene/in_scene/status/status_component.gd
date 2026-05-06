# 功能: 挂载在 landform 上的通用状态组件，统一管理状态层数、常驻 icon 和 tooltip 数据。
# 核心逻辑:
# - add_status(): 增加某个状态层数，并刷新 icon。
# - tick_poison_after_damage(): 中毒每回合扣血后衰减层数。
# - get_tooltip_status_lines()/get_keyword_names(): 为建筑 tooltip 提供主状态文本和关键词副 tooltip 数据。
# - refresh_status_icons(): 根据当前状态生成/移除常驻 icon。
class_name StatusComponent
extends Node2D

const STATUS_ICON_META: StringName = &"_status_icon_nodes"

@export_group("状态图标布局")
## icon 统一挂在建筑节点内，使用本地坐标偏移。y 越小越靠近建筑上方。
@export var icon_base_offset: Vector2 = Vector2(34.0, -82.0)
## 多个状态 icon 横向排列时的间距。
@export var icon_spacing: Vector2 = Vector2(26.0, 0.0)
## 常驻状态 icon 的像素目标尺寸。
@export var icon_size: Vector2 = Vector2(28.0, 28.0)
## 状态 icon 的层级，需高于建筑贴图和地块高亮。
@export var icon_z_index: int = 3600
## 状态新增或层数变化时是否播放轻微弹出反馈。
@export var animate_icon_refresh: bool = true

var owner_landform: Node = null
var stacks_by_status: Dictionary = {}
var icon_unlock_time_by_status: Dictionary = {}


func setup(new_owner: Node) -> void:
	owner_landform = new_owner
	refresh_status_icons()


func configure_display(base_offset: Vector2, spacing: Vector2, target_size: Vector2, target_z_index: int, should_animate_refresh: bool) -> void:
	icon_base_offset = base_offset
	icon_spacing = spacing
	icon_size = target_size
	icon_z_index = target_z_index
	animate_icon_refresh = should_animate_refresh


func has_status(status_id: StringName) -> bool:
	return get_status_stacks(status_id) > 0


func get_status_stacks(status_id: StringName) -> int:
	return maxi(int(stacks_by_status.get(status_id, 0)), 0)


func add_status(status_id: StringName, stacks: int) -> void:
	if stacks <= 0:
		return
	if not StatusDB.has_status(status_id):
		push_warning("未知状态: %s" % String(status_id))
		return

	stacks_by_status[status_id] = get_status_stacks(status_id) + stacks
	_schedule_icon_unlock(status_id)
	refresh_status_icons()


func set_status_stacks(status_id: StringName, stacks: int) -> void:
	if stacks <= 0:
		stacks_by_status.erase(status_id)
		icon_unlock_time_by_status.erase(status_id)
	else:
		stacks_by_status[status_id] = stacks
		_schedule_icon_unlock(status_id)
	refresh_status_icons()


func remove_status(status_id: StringName) -> void:
	if not stacks_by_status.has(status_id):
		return
	stacks_by_status.erase(status_id)
	icon_unlock_time_by_status.erase(status_id)
	refresh_status_icons()


func clear_statuses() -> void:
	if stacks_by_status.is_empty():
		return
	stacks_by_status.clear()
	icon_unlock_time_by_status.clear()
	refresh_status_icons()


func tick_poison_after_damage() -> void:
	var status_id: StringName = StatusDB.POISON_ID
	var stacks: int = get_status_stacks(status_id)
	if stacks <= 0:
		return

	var next_stacks: int = stacks - StatusDB.get_poison_decay_per_turn()
	set_status_stacks(status_id, next_stacks)


func get_tooltip_status_lines() -> Array[String]:
	var lines: Array[String] = []
	for status_id in _get_sorted_status_ids():
		var stacks: int = get_status_stacks(status_id)
		if stacks <= 0:
			continue

		var display_name: String = StatusDB.get_status_display_name(status_id)
		var color: String = StatusDB.get_status_color(status_id)
		lines.append("[color=%s]%s%d[/color]" % [color, display_name, stacks])
	return lines


func get_keyword_names() -> Array[String]:
	var keywords: Array[String] = []
	for status_id in _get_sorted_status_ids():
		var display_name: String = StatusDB.get_status_display_name(status_id)
		if display_name != "" and not keywords.has(display_name):
			keywords.append(display_name)
	return keywords


func get_status_snapshot() -> Dictionary:
	return stacks_by_status.duplicate()


func refresh_status_icons() -> void:
	_remove_all_icon_nodes()

	var icon_index: int = 0
	for status_id in _get_sorted_status_ids():
		if get_status_stacks(status_id) <= 0:
			continue
		if not _is_icon_unlocked(status_id):
			continue

		var icon_path: String = StatusDB.get_status_icon_path(status_id)
		if icon_path == "":
			continue

		var texture: Texture2D = load(icon_path) as Texture2D
		if texture == null:
			continue

		var icon := Sprite2D.new()
		icon.name = "StatusIcon_%s" % String(status_id)
		icon.texture = texture
		icon.centered = true
		icon.z_index = icon_z_index
		icon.position = icon_base_offset + icon_spacing * icon_index
		_apply_icon_scale(icon, texture)
		add_child(icon)
		icon_index += 1

		if animate_icon_refresh:
			_play_icon_refresh_animation(icon)

	set_meta(STATUS_ICON_META, _collect_icon_nodes())


func _schedule_icon_unlock(status_id: StringName) -> void:
	var delay_seconds: float = StatusDB.get_status_icon_delay_seconds(status_id)
	if delay_seconds <= 0.0:
		icon_unlock_time_by_status[status_id] = 0
		return

	var now_msec: int = Time.get_ticks_msec()
	var existing_unlock: int = int(icon_unlock_time_by_status.get(status_id, 0))
	if existing_unlock > now_msec:
		return

	var unlock_msec: int = now_msec + int(delay_seconds * 1000.0)
	icon_unlock_time_by_status[status_id] = unlock_msec
	_refresh_icons_after_delay(status_id, delay_seconds)


func _refresh_icons_after_delay(status_id: StringName, delay_seconds: float) -> void:
	await get_tree().create_timer(delay_seconds).timeout
	if not is_inside_tree() or get_status_stacks(status_id) <= 0:
		return
	refresh_status_icons()


func _is_icon_unlocked(status_id: StringName) -> bool:
	var unlock_msec: int = int(icon_unlock_time_by_status.get(status_id, 0))
	return unlock_msec <= Time.get_ticks_msec()


func _get_sorted_status_ids() -> Array[StringName]:
	var ids: Array[StringName] = []
	for key in stacks_by_status.keys():
		var status_id: StringName = StringName(str(key))
		if get_status_stacks(status_id) > 0:
			ids.append(status_id)
	ids.sort_custom(func(a: StringName, b: StringName) -> bool: return String(a) < String(b))
	return ids


func _apply_icon_scale(icon: Sprite2D, texture: Texture2D) -> void:
	var texture_size: Vector2 = texture.get_size()
	if texture_size.x <= 0.0 or texture_size.y <= 0.0:
		return
	var scale_factor: float = minf(icon_size.x / texture_size.x, icon_size.y / texture_size.y)
	icon.scale = Vector2.ONE * scale_factor


func _play_icon_refresh_animation(icon: Sprite2D) -> void:
	if not is_instance_valid(icon):
		return
	var final_scale: Vector2 = icon.scale
	icon.scale = final_scale * 0.72
	icon.modulate.a = 0.0
	var tw := create_tween().set_parallel(true).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tw.tween_property(icon, "scale", final_scale, 0.16)
	tw.tween_property(icon, "modulate:a", 1.0, 0.12)


func _remove_all_icon_nodes() -> void:
	for child in get_children():
		if child is Sprite2D and child.name.begins_with("StatusIcon_"):
			child.queue_free()
	if has_meta(STATUS_ICON_META):
		remove_meta(STATUS_ICON_META)


func _collect_icon_nodes() -> Array[Node]:
	var result: Array[Node] = []
	for child in get_children():
		if child is Sprite2D and child.name.begins_with("StatusIcon_"):
			result.append(child)
	return result
