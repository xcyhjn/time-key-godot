# 功能: 高塔中立建筑，作为 tower 卡牌 built 效果生成的运行期单位。
# 核心逻辑: Behavior() 每回合自动损失固定生命；die() 播放像素消融退场后清理地块占用、血条和自身贴图，不切换破树废墟图。
extends landform

@export var decay_damage_per_turn: int = 50
@export var despawn_vfx_duration: float = 0.45

var _is_dying: bool = false


func _init(location_in: Vector2i, battle_in) -> void:
	damage_rate = 0.0
	Max_Blood = 100

	if location_in == Vector2i(-100, -100) or battle_in == null:
		return

	super._init(
		"tower",
		["res://image/enermy/tower/tower.png"],
		["res://image/enermy/tower/tower.png"],
		{"chance": 1.0},
		location_in,
		false,
		battle_in
	)

	Attitude = Attitude_Pool.Middle
	target = location_in
	willing_pool = {}
	set_timeline_shape("1")


## 高塔不生成敌方意图，也不会在时间轴上额外创建占位。
func is_intent_preview_enabled() -> bool:
	return false


## 每次 HexMap 收到 step_next 时会调用建筑 Behavior。
## 高塔的行为非常单一：每回合自损 50 点生命，生命归零后直接消失。
func Behavior(_step: int, _info_in: Dictionary, _other, _beha: int, _rng: RandomNumberGenerator) -> void:
	if _is_dying or State_Main == Main_State_Pool.Broken:
		return
	take_damage(decay_damage_per_turn)


func die() -> void:
	if _is_dying:
		return

	_is_dying = true
	State_Main = Main_State_Pool.Broken
	damage_rate = 1.0

	_clear_map_occupancy()
	HealthBar_free.emit()
	_notify_map_changed()

	var tw: Tween = VFXManager.play_pixel_despawn_vfx(self, get_tree(), despawn_vfx_duration)
	if is_instance_valid(tw):
		await tw.finished

	_free_visual_sprite()
	queue_free()


## 高塔死亡时直接释放，不调用基类 tex_toggle()，因此不会加载或切换到破树废墟图。
func damaged_tex_picker():
	return landform_tex[0] if not landform_tex.is_empty() else null


func _clear_map_occupancy() -> void:
	if not is_instance_valid(owner_battle):
		return

	if owner_battle.stack_nodes.has(location):
		var stack: Area2D = owner_battle.stack_nodes[location]
		if is_instance_valid(stack):
			var occupant: Variant = stack.get_meta("occupant") if stack.has_meta("occupant") else null
			if occupant == self:
				stack.remove_meta("occupant")

	if owner_battle.map_data.has(location):
		var tile_data: Dictionary = owner_battle.map_data[location]
		if tile_data.get("landform") == self:
			tile_data["landform"] = null
		if tile_data.get("landform_in") == self:
			tile_data["landform_in"] = null
		if tile_data.get("landform_type", "") == "tower":
			tile_data["landform_type"] = ""
		owner_battle.map_data[location] = tile_data


func _free_visual_sprite() -> void:
	if not is_instance_valid(tex):
		return

	if is_instance_valid(owner_battle) and owner_battle.stack_nodes.has(location):
		var stack: Area2D = owner_battle.stack_nodes[location]
		if is_instance_valid(stack) and stack.has_meta("sprites"):
			var sprites: Array = stack.get_meta("sprites")
			sprites.erase(tex)
			stack.set_meta("sprites", sprites)

	tex.queue_free()
	tex = null


func _notify_map_changed() -> void:
	if not is_instance_valid(owner_battle):
		return
	if owner_battle.has_signal("tile_topology_changed"):
		owner_battle.tile_topology_changed.emit()
	if owner_battle.has_signal("enemy_roster_changed"):
		owner_battle.enemy_roster_changed.emit()
	if owner_battle.has_method("_refresh_stack_interactivity"):
		owner_battle.call("_refresh_stack_interactivity")
