class_name TargetHoverController
extends RefCounted


## TargetHoverController 负责“卡牌选中后的地块 hover 中心”编排。
## 它不计算卡牌范围、不写 shader、不查找场景节点路径，只消费 HexMap 传入的回调：
## - 读取当前选中卡牌。
## - 读取 MainBoard，用于旧 tooltip 更新链路。
## - 清理 AOE 与遮挡。
## - 当 hover 中心变化时触发 AOE 表现和动态遮挡表现。
## 这样输入模块只维护 hovered_stacks，HexMap 只保留状态和旧表现入口。


## 根据当前 hovered_stacks 与 active_stack 计算下一帧 hover 结果。
## 返回值只包含需要 HexMap 回写的状态，避免模块直接持有场景根状态。
func update(hovered_stacks: Array, active_stack: Area2D, config: Dictionary) -> Dictionary:
	var valid_hovered_stacks: Array[Area2D] = _filter_valid_hovered_stacks(hovered_stacks)
	var active_card: Variant = _get_active_card(config)
	var main_board: Node = _get_main_board(config)

	if not _is_live_object(active_card):
		_call_config_callback(config, "clear_all_aoe_highlights")
		_call_config_callback(config, "clear_occlusion_effects")
		_hide_cursor_tooltip(main_board)
		return {
			"hovered_stacks": valid_hovered_stacks,
			"active_stack": null,
		}

	var front_stack: Area2D = _find_front_stack(valid_hovered_stacks)
	if active_stack == front_stack:
		return {
			"hovered_stacks": valid_hovered_stacks,
			"active_stack": active_stack,
		}

	active_stack = front_stack
	if is_instance_valid(active_stack):
		_call_card_stack_board_callback(config, "update_aoe_display", active_card, active_stack, main_board)
		_call_stack_callback(config, "update_occlusion", active_stack)
	else:
		_call_config_callback(config, "clear_all_aoe_highlights")

	return {
		"hovered_stacks": valid_hovered_stacks,
		"active_stack": active_stack,
	}


## 清理 hovered_stacks 中已经释放的 Area2D。
## 鼠标 enter/exit 与 queue_free 可能错帧发生；这里集中清理，避免 HexMap 每次 hover 刷新都重复写过滤逻辑。
func _filter_valid_hovered_stacks(hovered_stacks: Array) -> Array[Area2D]:
	var valid_stacks: Array[Area2D] = []
	for stack in hovered_stacks:
		if stack is Area2D and is_instance_valid(stack):
			valid_stacks.append(stack)
	return valid_stacks


## 从所有当前 hover 的堆叠地块中选择最靠前的一块。
## 旧逻辑按 global_position.y 最大值作为视觉前景，这里保留同一规则。
func _find_front_stack(hovered_stacks: Array[Area2D]) -> Area2D:
	var front_stack: Area2D = null
	var max_y: float = -INF
	for stack in hovered_stacks:
		if stack.global_position.y > max_y:
			max_y = stack.global_position.y
			front_stack = stack
	return front_stack


## 读取当前选中卡牌。
## CardManager 路径仍由 HexMap 自己处理，控制器只调用注入的读取回调。
func _get_active_card(config: Dictionary) -> Variant:
	var callback: Callable = config.get("get_active_card", Callable())
	if not callback.is_valid():
		return null
	return callback.call()


## 读取 MainBoard 节点。
## Tooltip 的具体更新仍在 HexMap::_update_aoe_display() 中，这里只在无卡牌时隐藏旧 tooltip。
func _get_main_board(config: Dictionary) -> Node:
	var callback: Callable = config.get("get_main_board", Callable())
	if not callback.is_valid():
		return null
	var result: Variant = callback.call()
	if result is Node and is_instance_valid(result):
		return result
	return null


## 无选中卡牌时隐藏目标 tooltip。
## cursor_tooltip 是 MainBoard 上的旧 UI 引用，当前按 CanvasItem 处理即可覆盖 Control/Panel 等常见 tooltip 节点。
func _hide_cursor_tooltip(main_board: Node) -> void:
	if not is_instance_valid(main_board):
		return
	var cursor_tooltip: Variant = main_board.get("cursor_tooltip")
	if cursor_tooltip is CanvasItem and is_instance_valid(cursor_tooltip):
		cursor_tooltip.hide()


## 判断 Variant 是否是仍然存活的 Godot 对象。
## 这里用于保留旧语义：只有当前选中卡牌实例有效时，才触发 AOE 与遮挡表现。
func _is_live_object(value: Variant) -> bool:
	return value is Object and is_instance_valid(value)


## 调用无参数回调。
## 用于清理 AOE、清理遮挡等 HexMap 旧入口。
func _call_config_callback(config: Dictionary, key: String) -> void:
	var callback: Callable = config.get(key, Callable())
	if callback.is_valid():
		callback.call()


## 调用单个 stack 参数回调。
## 用于 active_stack 变化后触发动态遮挡刷新。
func _call_stack_callback(config: Dictionary, key: String, stack: Area2D) -> void:
	var callback: Callable = config.get(key, Callable())
	if callback.is_valid():
		callback.call(stack)


## 调用 card + stack + main_board 参数回调。
## 用于 active_stack 变化后复用 HexMap 原有的 AOE 与 tooltip 更新入口。
func _call_card_stack_board_callback(config: Dictionary, key: String, card: Variant, stack: Area2D, main_board: Node) -> void:
	var callback: Callable = config.get(key, Callable())
	if callback.is_valid():
		callback.call(card, stack, main_board)
