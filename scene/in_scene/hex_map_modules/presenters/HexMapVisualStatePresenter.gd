class_name HexMapVisualStatePresenter
extends RefCounted

const TILE_STATE_IDLE := 0
const TILE_STATE_HOVER_TARGET_VALID := 1
const TILE_STATE_HOVER_TARGET_INVALID := 2
const TILE_STATE_AOE_RANGE := 3


## 统一清理当前 AOE、选中地块和活动地块的视觉状态。
## 这个函数只负责把旧高亮写回 IDLE，并返回清理后的状态包；HexMap 继续持有 current_aoe_stacks、selected_stack 和 active_stack。
func clear_aoe_highlights(current_aoe_stacks: Array[Area2D], selected_stack: Area2D, active_stack: Area2D, config: Dictionary) -> Dictionary:
	for stack in current_aoe_stacks:
		change_tile_state(stack, TILE_STATE_IDLE, config)
	current_aoe_stacks.clear()

	if is_instance_valid(selected_stack):
		change_tile_state(selected_stack, TILE_STATE_IDLE, config)
		selected_stack = null

	if is_instance_valid(active_stack):
		change_tile_state(active_stack, TILE_STATE_IDLE, config)
		active_stack = null

	return {
		"current_aoe_stacks": current_aoe_stacks,
		"selected_stack": selected_stack,
		"active_stack": active_stack,
	}


## 应用新的 AOE 范围视觉状态。
## 旧范围中不再属于新范围的地块会恢复 IDLE，新范围内的地块统一写入目标状态。
func apply_aoe_state(
	current_aoe_stacks: Array[Area2D],
	new_aoe_stacks: Array[Area2D],
	target_state: int,
	config: Dictionary
) -> Array[Area2D]:
	for stack in current_aoe_stacks:
		if not new_aoe_stacks.has(stack):
			change_tile_state(stack, TILE_STATE_IDLE, config)

	for stack in new_aoe_stacks:
		change_tile_state(stack, target_state, config)

	return new_aoe_stacks


## 清理动态遮挡湮灭效果。
## 这里遍历当前地图全部地块，把 dissolve_blend 恢复到 0，并清空遮挡列表。
func clear_occlusion_effects(stack_nodes: Dictionary, currently_occluding_stacks: Array[Area2D], config: Dictionary) -> Array[Area2D]:
	for stack in stack_nodes.values():
		if is_instance_valid(stack) and stack is Area2D:
			tween_shader_param(stack, "dissolve_blend", 0.0, float(config.get("occlusion_clear_duration", 0.12)), config)
	currently_occluding_stacks.clear()
	return currently_occluding_stacks


## 更新目标地块前方的动态遮挡效果。
## 平铺视图下直接跳过；3D 视图下会先清理旧遮挡，再把挡住目标的前景柱子淡到半透明。
func update_occlusion(
	target_stack: Area2D,
	stack_nodes: Dictionary,
	currently_occluding_stacks: Array[Area2D],
	config: Dictionary
) -> Array[Area2D]:
	if bool(config.get("is_flat_view", false)):
		return currently_occluding_stacks

	currently_occluding_stacks = clear_occlusion_effects(stack_nodes, currently_occluding_stacks, config)
	if not is_instance_valid(target_stack):
		return currently_occluding_stacks

	var target_height := get_stack_height(target_stack)
	var target_position := target_stack.position
	var current_step_h := float(config.get("current_step_h", 48.0))
	var target_top_y := target_position.y - float(target_height - 1) * current_step_h
	var hitbox_width := float(config.get("hitbox_width", 168.0))
	var occlusion_x_factor := float(config.get("occlusion_x_factor", 0.8))

	for stack in stack_nodes.values():
		var area := stack as Area2D
		if not is_instance_valid(area) or area == target_stack:
			continue

		var stack_position := area.position
		if stack_position.y <= target_position.y:
			continue
		if abs(stack_position.x - target_position.x) >= hitbox_width * occlusion_x_factor:
			continue

		var stack_height := get_stack_height(area)
		var stack_top_y := stack_position.y - float(stack_height - 1) * current_step_h
		if stack_top_y < target_top_y:
			currently_occluding_stacks.append(area)
			tween_shader_param(
				area,
				"dissolve_blend",
				float(config.get("occlusion_dissolve_blend", 0.8)),
				float(config.get("occlusion_fade_duration", 0.25)),
				config
			)

	return currently_occluding_stacks


## 按状态机写入一个地块的 shader 高亮状态。
## 状态值沿用 HexMap 的 TileVisualState enum 数字，避免 presenter 直接依赖 HexMap enum 类型。
func change_tile_state(stack: Area2D, new_state: int, config: Dictionary) -> void:
	if not is_instance_valid(stack):
		return

	var current_state := int(stack.get_meta("visual_state")) if stack.has_meta("visual_state") else TILE_STATE_IDLE
	if current_state == new_state:
		return

	stack.set_meta("visual_state", new_state)
	var visual_config := _get_tile_state_visual_config(new_state)

	var meta_key := "tween_state_machine"
	if stack.has_meta(meta_key):
		var old_tween: Variant = stack.get_meta(meta_key)
		if is_instance_valid(old_tween) and old_tween.is_valid():
			old_tween.kill()

	var tween := _create_tween(config)
	if tween == null:
		_apply_tile_state_immediately(stack, visual_config)
		return

	tween.set_parallel(true)
	tween.set_trans(Tween.TRANS_SINE)
	tween.set_ease(Tween.EASE_IN_OUT)
	stack.set_meta(meta_key, tween)

	var sprites := get_stack_sprites(stack)
	for sprite in sprites:
		var item := sprite as CanvasItem
		if not is_instance_valid(item) or item.material == null:
			continue

		item.set_instance_shader_parameter("highlight_color", visual_config.get("highlight_color", Color.WHITE))
		var current_highlight_blend: Variant = item.get_instance_shader_parameter("highlight_blend")
		var current_selected_blend: Variant = item.get_instance_shader_parameter("is_selected_blend")
		if current_highlight_blend == null:
			current_highlight_blend = 0.0
		if current_selected_blend == null:
			current_selected_blend = 0.0

		tween.tween_method(
			func(value: float): if is_instance_valid(item): item.set_instance_shader_parameter("highlight_blend", value),
			float(current_highlight_blend),
			float(visual_config.get("highlight_blend", 0.0)),
			float(config.get("tile_state_tween_duration", 0.15))
		)
		tween.tween_method(
			func(value: float): if is_instance_valid(item): item.set_instance_shader_parameter("is_selected_blend", value),
			float(current_selected_blend),
			float(visual_config.get("selected_blend", 0.0)),
			float(config.get("tile_state_tween_duration", 0.15))
		)


## 通用 shader 参数 Tween。
## 遮挡、右键取消选中等旧路径都复用这个入口，避免 HexMap 自己遍历 sprites。
func tween_shader_param(stack: Area2D, param_name: String, target_value: float, duration: float, config: Dictionary) -> void:
	if not is_instance_valid(stack):
		return
	var sprites := get_stack_sprites(stack)
	if sprites.is_empty():
		return

	var meta_key := "tween_" + param_name
	if stack.has_meta(meta_key):
		var old_tween: Variant = stack.get_meta(meta_key)
		if is_instance_valid(old_tween) and old_tween.is_valid():
			old_tween.kill()

	var tween := _create_tween(config)
	if tween == null:
		_set_shader_param_immediately(sprites, param_name, target_value)
		return
	tween.set_trans(Tween.TRANS_SINE)
	tween.set_ease(Tween.EASE_IN_OUT)
	stack.set_meta(meta_key, tween)

	var first_item := sprites[0] as CanvasItem
	if not is_instance_valid(first_item) or first_item.material == null:
		return
	var current_value: Variant = first_item.get_instance_shader_parameter(param_name)
	if current_value == null:
		current_value = 0.0

	tween.tween_method(
		func(value: float):
			for sprite in sprites:
				var item := sprite as CanvasItem
				if is_instance_valid(item) and item.material:
					item.set_instance_shader_parameter(param_name, value),
		float(current_value),
		target_value,
		duration
	)


## 从地块 metadata 中读取 sprites 队列。
## 队列可能包含 Sprite2D、TextureRect 或血条子节点，因此返回 Array 而不是更窄的类型。
func get_stack_sprites(stack: Area2D) -> Array:
	if not is_instance_valid(stack) or not stack.has_meta("sprites"):
		return []
	var stored_sprites: Variant = stack.get_meta("sprites")
	if stored_sprites is Array:
		return stored_sprites
	return []


## 从地块 metadata 中读取高度。
## 缺失时返回 1，保证遮挡计算不会因为临时节点异常而报错。
func get_stack_height(stack: Area2D) -> int:
	if not is_instance_valid(stack):
		return 1
	if stack.has_meta("height"):
		return max(1, int(stack.get_meta("height")))
	return 1


## 根据 TileVisualState 数字返回 shader 目标值。
## 颜色和强度保持旧 HexMap 行为，不在本次拆分中改美术参数。
func _get_tile_state_visual_config(new_state: int) -> Dictionary:
	match new_state:
		TILE_STATE_HOVER_TARGET_VALID:
			return {
				"highlight_blend": 1.0,
				"selected_blend": 1.0,
				"highlight_color": Color(1.0, 0.8, 0.0, 1.0),
			}
		TILE_STATE_HOVER_TARGET_INVALID:
			return {
				"highlight_blend": 1.0,
				"selected_blend": 1.0,
				"highlight_color": Color(0.5, 0.5, 0.5, 1.0),
			}
		TILE_STATE_AOE_RANGE:
			return {
				"highlight_blend": 0.7,
				"selected_blend": 0.0,
				"highlight_color": Color(0.6, 0.8, 1.0, 1.0),
			}
		_:
			return {
				"highlight_blend": 0.0,
				"selected_blend": 0.0,
				"highlight_color": Color(1.0, 1.0, 1.0, 1.0),
			}


## 无 Tween 可用时立即应用 tile state。
## 这条路径主要用于场景卸载或测试环境，避免视觉状态卡在旧值。
func _apply_tile_state_immediately(stack: Area2D, visual_config: Dictionary) -> void:
	for sprite in get_stack_sprites(stack):
		var item := sprite as CanvasItem
		if is_instance_valid(item) and item.material:
			item.set_instance_shader_parameter("highlight_color", visual_config.get("highlight_color", Color.WHITE))
			item.set_instance_shader_parameter("highlight_blend", float(visual_config.get("highlight_blend", 0.0)))
			item.set_instance_shader_parameter("is_selected_blend", float(visual_config.get("selected_blend", 0.0)))


## 无 Tween 可用时立即写 shader 参数。
## 只在缺少 create_tween 回调时使用，正常战斗流程仍走 Tween。
func _set_shader_param_immediately(sprites: Array, param_name: String, target_value: float) -> void:
	for sprite in sprites:
		var item := sprite as CanvasItem
		if is_instance_valid(item) and item.material:
			item.set_instance_shader_parameter(param_name, target_value)


## 由 HexMap 创建 Tween。
## Presenter 不直接依赖场景树节点，Tween 生命周期仍跟随 HexMap。
func _create_tween(config: Dictionary) -> Tween:
	var create_tween_callable: Callable = config.get("create_tween", Callable())
	if not create_tween_callable.is_valid():
		return null
	var tween: Variant = create_tween_callable.call()
	if tween is Tween:
		return tween
	return null
