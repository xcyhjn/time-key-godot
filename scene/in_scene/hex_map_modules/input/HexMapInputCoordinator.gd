class_name HexMapInputCoordinator
extends RefCounted


## HexMapInputCoordinator 负责地块鼠标输入的流程分发。
## 它不判断卡牌范围、不写 shader、不打开具体奖励界面，只把点击、悬浮、平铺光柱和敌人意图 hover 转发到 HexMap 注入的旧入口。
## 这样 HexMap 可以继续持有场景状态，而输入流程本身不再散落在主脚本里。


## 处理 Area2D.input_event 传入的鼠标事件。
## 左键会根据当前模式分发到结算奖励点击或普通地块点击；右键继续走取消卡牌选择。
func handle_stack_input(_viewport: Node, event: InputEvent, _shape_idx: int, stack: Area2D, config: Dictionary) -> Dictionary:
	if not (event is InputEventMouseButton):
		return {}

	var mouse_event := event as InputEventMouseButton
	if not mouse_event.pressed:
		return {}

	match mouse_event.button_index:
		MOUSE_BUTTON_LEFT:
			if bool(config.get("settlement_reward_available", false)):
				_call_stack_callback(config, "handle_settlement_reward_click", stack)
				return {}
			return handle_tile_click(stack, config)
		MOUSE_BUTTON_RIGHT:
			_call_config_callback(config, "cancel_card_selection")

	return {}


## 处理普通地块左键点击。
## 有选中卡牌时尝试打出卡牌；没有选中卡牌时，只把当前地块写成选中高亮。
func handle_tile_click(stack: Area2D, config: Dictionary) -> Dictionary:
	if not is_instance_valid(stack):
		return {}

	var card_manager := _get_card_manager(config)
	if not is_instance_valid(card_manager):
		return {}

	var active_card: Variant = card_manager.get("current_selected_card")
	if is_instance_valid(active_card):
		return _try_play_selected_card(stack, active_card, config)

	return _select_idle_stack(stack, config)


## 处理鼠标进入或离开地块。
## 结算奖励模式优先接管 hover；普通模式下再处理平铺光柱、hovered_stacks、AOE 刷新和敌人意图 hover。
func handle_stack_hover(stack: Area2D, is_entered: bool, config: Dictionary) -> Dictionary:
	if bool(config.get("settlement_reward_available", false)):
		_call_stack_bool_callback(config, "handle_settlement_reward_hover", stack, is_entered)
		return {}

	if bool(config.get("visuals_locked", false)):
		return {}

	var result := {}
	if bool(config.get("is_flat_view", false)):
		result["height_view_hovered_stack"] = _handle_flat_view_hover(stack, is_entered, config)

	result["hovered_stacks"] = _update_hovered_stack_list(stack, is_entered, config)
	_call_config_callback(config, "update_highlight")
	_call_stack_bool_callback(config, "handle_enemy_intent_stack_hover", stack, is_entered)
	return result


## 尝试把当前选中卡牌打到目标地块上。
## 目标合法性和实际出牌仍由 HexMap 与卡牌自己负责；协调器只维护点击流程和选中状态回写。
func _try_play_selected_card(stack: Area2D, active_card: Variant, config: Dictionary) -> Dictionary:
	if not _is_stack_valid_target(stack, config):
		return {}

	if active_card.has_method("play_card"):
		active_card.play_card(stack)
		_call_config_callback(config, "clear_all_aoe_highlights")
		return {"selected_stack": null}

	return {}


## 未选中卡牌时，点击地块只切换地块选中高亮。
## 旧选中地块会先恢复 IDLE，新地块写入有效 hover 状态。
func _select_idle_stack(stack: Area2D, config: Dictionary) -> Dictionary:
	var selected_stack := _get_area_from_config(config, "selected_stack")
	if is_instance_valid(selected_stack) and selected_stack != stack:
		_change_tile_state(selected_stack, int(config.get("tile_state_idle", 0)), config)

	selected_stack = stack
	_change_tile_state(selected_stack, int(config.get("tile_state_hover_target_valid", 1)), config)
	return {"selected_stack": selected_stack}


## 平铺高度视图下处理光柱 hover 状态。
## 这里只维护当前 hover 的 stack，并调用 HexMap 注入的光柱动画入口。
func _handle_flat_view_hover(stack: Area2D, is_entered: bool, config: Dictionary) -> Variant:
	var height_view_hovered_stack := _get_area_from_config(config, "height_view_hovered_stack")
	if is_entered:
		height_view_hovered_stack = stack
		_call_stack_callback(config, "start_pillar_floating", stack)
		return height_view_hovered_stack

	if height_view_hovered_stack == stack:
		height_view_hovered_stack = null
	_call_stack_callback(config, "stop_pillar_floating", stack)
	return height_view_hovered_stack


## 维护当前鼠标覆盖的地块列表。
## 这个列表仍由 HexMap 持有；协调器只按进入/离开事件追加或移除。
func _update_hovered_stack_list(stack: Area2D, is_entered: bool, config: Dictionary) -> Array:
	var hovered_stacks: Array = config.get("hovered_stacks", [])
	if is_entered:
		if not hovered_stacks.has(stack):
			hovered_stacks.append(stack)
	else:
		hovered_stacks.erase(stack)
	return hovered_stacks


## 从配置中读取 CardManager。
## 查找路径仍在 HexMap 内部，这里只调用注入回调，避免输入模块直接依赖场景树路径。
func _get_card_manager(config: Dictionary) -> Node:
	var get_card_manager: Callable = config.get("get_card_manager", Callable())
	if not get_card_manager.is_valid():
		return null
	var result: Variant = get_card_manager.call()
	if result is Node and is_instance_valid(result):
		return result
	return null


## 调用 HexMap 的目标合法性检查。
## 目标规则仍由 HexTargetRules 和 HexMap 的上下文构造负责。
func _is_stack_valid_target(stack: Area2D, config: Dictionary) -> bool:
	var is_stack_valid_target: Callable = config.get("is_stack_valid_target", Callable())
	if not is_stack_valid_target.is_valid():
		return false
	return bool(is_stack_valid_target.call(stack))


## 按状态值调用 HexMap 的地块状态切换入口。
## 状态数字来自 HexMap 的 TileVisualState enum，模块本身不直接依赖 enum 类型。
func _change_tile_state(stack: Area2D, state: int, config: Dictionary) -> void:
	var change_tile_state: Callable = config.get("change_tile_state", Callable())
	if change_tile_state.is_valid():
		change_tile_state.call(stack, state)


## 从配置中安全读取 Area2D。
## 用一个小工具集中处理 Variant，避免每个分支都写重复类型判断。
func _get_area_from_config(config: Dictionary, key: String) -> Area2D:
	var value: Variant = config.get(key, null)
	if value is Area2D and is_instance_valid(value):
		return value
	return null


## 调用无参数回调。
## 用于取消卡牌选择、刷新高亮、清理 AOE 等 HexMap 旧入口。
func _call_config_callback(config: Dictionary, key: String) -> void:
	var callback: Callable = config.get(key, Callable())
	if callback.is_valid():
		callback.call()


## 调用单个 stack 参数的回调。
## 用于结算奖励点击和平铺高度光柱动画。
func _call_stack_callback(config: Dictionary, key: String, stack: Area2D) -> void:
	var callback: Callable = config.get(key, Callable())
	if callback.is_valid():
		callback.call(stack)


## 调用 stack + bool 参数的回调。
## 用于结算奖励 hover 和敌人意图 hover。
func _call_stack_bool_callback(config: Dictionary, key: String, stack: Area2D, value: bool) -> void:
	var callback: Callable = config.get(key, Callable())
	if callback.is_valid():
		callback.call(stack, value)
