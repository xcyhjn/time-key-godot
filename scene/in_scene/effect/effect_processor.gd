class_name EffectProcessor
extends Node

var hex_map: Node

func _ready():
	# 延迟获取地图引用，防止场景未加载完
	call_deferred("_setup_references")

func _setup_references():
	var main_board = get_tree().get_first_node_in_group("MainBoard")
	if main_board:
		hex_map = main_board.get_node_or_null("../../map/HexMap")

# ==========================================
# ★ 核心枢纽：遍历执行标准化的效果数组
# ==========================================
func execute_action(action: TimelineAction):
	var target_stack = action.target_tile as Area2D
	if not is_instance_valid(target_stack): return
	
	# ★ 核心改动：不再解析字符串，直接读取结构化的 effects 数组
	var effects_array = action.action_data.get("effects", [])
	
	for effect in effects_array:
		var effect_type = effect.get("type", "")
		var effect_value = effect.get("value", 0)
		
		match effect_type:
			"damage":
				_apply_damage(target_stack, effect_value)
			"elevation":
				_apply_elevation(target_stack, effect_value)
			# 未来可以无限扩展："heal", "add_shield", "stun" 等等

# ==========================================
# 具体效果的原子逻辑
# ==========================================
func _apply_damage(target_stack: Area2D, amount: int):
	var occupant = target_stack.get_meta("occupant")
	if is_instance_valid(occupant) and occupant.has_method("take_damage"):
		occupant.take_damage(amount)
		Signal_Bus.emit_damage_dealt(occupant, amount)
		GameLogger.info("💥 造成伤害: %d" % amount, "EffectProcessor")

func _apply_elevation(target_stack: Area2D, delta_height: int):
	if is_instance_valid(hex_map) and hex_map.has_method("animate_elevation_change"):
		hex_map.animate_elevation_change(target_stack, delta_height)
		GameLogger.info("⛰️ 改变地块高度: %d" % delta_height, "EffectProcessor")
