class_name HexMapCollisionPresenter
extends RefCounted


## 根据当前导出的碰撞参数，构建标准六边形碰撞多边形。
## 这个函数不读取场景节点，只把 HexMap 传入的宽、高和顶部收窄比例转换成 CollisionPolygon2D 需要的点集。
func build_hitbox_polygon(config: Dictionary) -> PackedVector2Array:
	var half_w := float(config.get("hitbox_width", 168.0)) * 0.5
	var half_h := float(config.get("hitbox_base_height", 60.0)) * 0.5
	var top_half_w := half_w * float(config.get("hitbox_top_width_ratio", 0.5))

	return PackedVector2Array([
		Vector2(-top_half_w, -half_h),
		Vector2(top_half_w, -half_h),
		Vector2(half_w, 0.0),
		Vector2(top_half_w, half_h),
		Vector2(-top_half_w, half_h),
		Vector2(-half_w, 0.0)
	])


## 根据当前地图状态刷新所有地块的 input_pickable。
## 这里只决定“哪些地块能接收鼠标输入”，不处理 hover shader、卡牌目标规则或奖励领取行为。
func refresh_stack_interactivity(config: Dictionary) -> void:
	var stack_nodes: Dictionary = config.get("stack_nodes", {})
	if bool(config.get("intro_reveal_active", false)) and bool(config.get("intro_lock_interaction", false)):
		_set_all_stacks_pickable(stack_nodes, false)
		return

	if bool(config.get("settlement_reward_available", false)):
		_refresh_settlement_reward_interactivity(stack_nodes, config)
		return

	var allow_all := false
	if not bool(config.get("tiles_interactive_master_enabled", true)):
		allow_all = false
	elif not bool(config.get("smart_collision_enabled", true)):
		allow_all = true
	else:
		allow_all = bool(config.get("target_selection_active", false))

	for stack in stack_nodes.values():
		var area := stack as Area2D
		if not is_instance_valid(area):
			continue

		if not bool(config.get("tiles_interactive_master_enabled", true)):
			area.input_pickable = false
		elif allow_all:
			area.input_pickable = true
		else:
			area.input_pickable = stack_has_enemy_building(area)


## 重新应用单个地块的碰撞箱形状、位置和层级。
## 地块高度、当前视图、缩放与 z-index 都通过 config 注入，presenter 不直接读取 HexMap 成员变量。
func refresh_collision_for_stack(stack: Area2D, config: Dictionary) -> void:
	if not is_instance_valid(stack):
		return

	var collision := get_collision_node(stack)
	if not is_instance_valid(collision):
		return

	var height := get_stack_height(stack)
	var current_step_h := float(config.get("step_height", 48.0)) * _get_scale_ratio(config)
	var top_block_y := 0.0
	if int(config.get("current_view_state", 0)) != int(config.get("view_state_flat", 1)):
		top_block_y = -(height - 1) * current_step_h

	collision.polygon = build_hitbox_polygon(config)
	collision.position = Vector2(
		float(config.get("hitbox_offset_x", 0.0)),
		top_block_y + float(config.get("hitbox_offset_y", -110.0))
	)
	collision.z_as_relative = false
	collision.z_index = get_safe_hitbox_z_index(config)


## 对当前地图中的所有碰撞箱执行一次重建。
## 这个入口服务检查器调参后的手动刷新，也可被后续工具脚本复用。
func rebuild_all_collision_shapes(config: Dictionary) -> void:
	var stack_nodes: Dictionary = config.get("stack_nodes", {})
	for stack in stack_nodes.values():
		if is_instance_valid(stack) and stack is Area2D:
			refresh_collision_for_stack(stack, config)


## 读取并夹紧碰撞箱的安全 z-index。
## Godot CanvasItem 常用安全范围是 [-4096, 4096]，这里保留旧 HexMap 的保护逻辑。
func get_safe_hitbox_z_index(config: Dictionary) -> int:
	return clampi(int(config.get("hitbox_z_index", 4096)), -4096, 4096)


## 判断某个格子上当前是否存在敌人建筑。
## 这里只依赖 occupant metadata 和 Enemies 分组，不关心具体敌人脚本类型。
func stack_has_enemy_building(stack: Area2D) -> bool:
	if not is_instance_valid(stack):
		return false
	if not stack.has_meta("occupant"):
		return false

	var occupant: Variant = stack.get_meta("occupant")
	if not is_instance_valid(occupant):
		return false

	return occupant.is_in_group("Enemies")


## 从地块 metadata 中读取碰撞节点。
## 只有 CollisionPolygon2D 会被刷新，避免误改其它辅助节点。
func get_collision_node(stack: Area2D) -> CollisionPolygon2D:
	if not is_instance_valid(stack):
		return null
	var collision: Variant = stack.get_meta("collision_node") if stack.has_meta("collision_node") else null
	if collision is CollisionPolygon2D and is_instance_valid(collision):
		return collision
	return null


## 从地块 metadata 中读取高度。
## 缺失或异常时回到 1，保持旧 HexMap 的保守碰撞位置。
func get_stack_height(stack: Area2D) -> int:
	if not is_instance_valid(stack):
		return 1
	if stack.has_meta("height"):
		return max(1, int(stack.get_meta("height")))
	return 1


## 根据奖励资格回调刷新结算奖励阶段的地块输入。
## 奖励资格仍由 HexMap 判断，presenter 只负责把结果写到 input_pickable。
func _refresh_settlement_reward_interactivity(stack_nodes: Dictionary, config: Dictionary) -> void:
	var is_reward_stack_available: Callable = config.get("is_settlement_reward_stack_available", Callable())
	for stack in stack_nodes.values():
		var area := stack as Area2D
		if not is_instance_valid(area):
			continue
		area.input_pickable = bool(is_reward_stack_available.call(area)) if is_reward_stack_available.is_valid() else false


## 一次性设置所有有效地块是否接收鼠标输入。
## 入场锁和全局禁用路径都会走这里，避免重复遍历逻辑散在 HexMap 中。
func _set_all_stacks_pickable(stack_nodes: Dictionary, is_pickable: bool) -> void:
	for stack in stack_nodes.values():
		var area := stack as Area2D
		if is_instance_valid(area):
			area.input_pickable = is_pickable


## 计算贴图缩放比。
## 碰撞高度偏移沿用旧公式：`step_height * tile_scale / REF_SCALE`。
func _get_scale_ratio(config: Dictionary) -> float:
	var ref_scale := float(config.get("ref_scale", 0.6))
	if is_zero_approx(ref_scale):
		return 1.0
	return float(config.get("tile_scale", 0.6)) / ref_scale
