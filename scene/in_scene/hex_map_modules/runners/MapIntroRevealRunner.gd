class_name MapIntroRevealRunner
extends RefCounted


## 判断本次地图构建是否需要播放初始入场动画。
## 这个函数只根据 HexMap 传入的配置做状态切换，不读取场景树，也不创建任何视觉节点。
func begin_if_needed(config: Dictionary) -> Dictionary:
	var state: Dictionary = config.get("state", {})
	_clear_pending_health_bar_requests(state)

	var should_start := (
		bool(config.get("enabled", false))
		and not bool(state.get("has_played", false))
		and int(config.get("current_view_state", 0)) == int(config.get("view_state_3d", 0))
	)

	state["is_active"] = should_start
	if should_start:
		state["has_played"] = true

	return state


## 返回当前入场动画是否仍在播放。
## BarManager、教程引导和开局回合启动都通过 HexMap 的旧入口读取这个状态。
func is_active(state: Dictionary) -> bool:
	return bool(state.get("is_active", false))


## 返回单体血条是否应该延后生成。
## 这里和 is_active() 使用同一份状态，保持旧行为：只有初始入场期间会拦截血条请求。
func should_defer_health_bars(state: Dictionary) -> bool:
	return is_active(state)


## 暂存入场期间收到的单体血条生成请求。
## 用实体实例 id 去重，避免同一个实体在视觉刷新或信号重入时重复排队。
## 参数保持为 Object，避免 runner 直接依赖 landform 类名和敌人脚本加载顺序。
func queue_health_bar_request(state: Dictionary, landform_in: Object, situation: int, x: float, y: float) -> void:
	if not is_instance_valid(landform_in):
		return

	var request_ids: Dictionary = state.get("pending_request_ids", {})
	var request_id := landform_in.get_instance_id()
	if request_ids.has(request_id):
		return

	var requests: Array = state.get("pending_requests", [])
	request_ids[request_id] = true
	requests.append({
		"landform": landform_in,
		"situation": situation,
		"x": x,
		"y": y
	})
	state["pending_request_ids"] = request_ids
	state["pending_requests"] = requests


## 播放整张地图的“倒放湮灭”涟漪入场。
## Tween 必须由 HexMap 通过 config 注入创建，这样动画仍挂在真实场景节点下，并遵守原来的暂停策略。
func play(config: Dictionary) -> void:
	var stacks := _get_reveal_stacks(config)
	if stacks.is_empty():
		finish(config)
		return

	var origin := _get_reveal_origin(stacks, config)
	var wave_step := maxf(float(config.get("wave_pixel_step", 130.0)), 1.0)
	var wave_delay := maxf(float(config.get("wave_delay", 0.07)), 0.0)
	var tile_duration := maxf(float(config.get("tile_duration", 0.42)), 0.001)
	var longest_time := 0.0

	for stack in stacks:
		if not is_instance_valid(stack):
			continue

		var wave_index := int(floor(stack.position.distance_to(origin) / wave_step))
		var delay := float(wave_index) * wave_delay
		longest_time = maxf(longest_time, delay + tile_duration)

		var tween := _create_tween(config)
		if tween == null:
			_set_stack_dissolve(stack, 0.0, config)
			continue

		tween.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
		tween.set_trans(config.get("trans_type", Tween.TRANS_SINE))
		tween.set_ease(config.get("ease_type", Tween.EASE_OUT))
		if delay > 0.0:
			tween.tween_interval(delay)
		tween.tween_method(
			Callable(self, "_set_stack_dissolve_from_tween").bind(stack, config),
			1.0,
			0.0,
			tile_duration
		)

	var total_wait := longest_time + maxf(float(config.get("finish_delay", 0.08)), 0.0)
	var create_timer: Callable = config.get("create_timer", Callable())
	if create_timer.is_valid():
		var timer: Variant = create_timer.call(total_wait)
		if timer != null:
			await timer.timeout
	finish(config)


## 完成入场收尾。
## 这里负责清理 dissolve、补发延迟血条、恢复辅助 UI 和调用 HexMap 注入的刷新/通知回调。
func finish(config: Dictionary) -> void:
	var state: Dictionary = config.get("state", {})
	state["is_active"] = false

	var stack_nodes: Dictionary = config.get("stack_nodes", {})
	for stack in stack_nodes.values():
		if is_instance_valid(stack):
			_set_stack_dissolve(stack, 0.0, config)

	flush_health_bar_requests(config)
	set_auxiliary_visuals_visible(true, config)
	_call_config_callback(config, "refresh_stack_interactivity")
	_call_config_callback(config, "emit_enemy_roster_changed")
	_call_config_callback(config, "emit_reveal_finished")


## 统一补发入场期间缓存的血条创建请求。
## BarManager 查找和真实创建函数由 HexMap 注入，这个模块只消费缓存队列。
func flush_health_bar_requests(config: Dictionary) -> void:
	var state: Dictionary = config.get("state", {})
	var requests: Array = state.get("pending_requests", [])
	var create_health_bar: Callable = config.get("create_health_bar", Callable())

	if create_health_bar.is_valid():
		for request in requests:
			var landform_in: Variant = request.get("landform", null)
			if not is_instance_valid(landform_in):
				continue
			create_health_bar.call(
				landform_in,
				int(request.get("situation", 0)),
				float(request.get("x", 0.0)),
				float(request.get("y", 0.0))
			)

	_clear_pending_health_bar_requests(state)


## 设置入场期间需要隐藏或恢复的辅助 UI。
## 目前只管 BarManager 和总血量条的可见性，具体节点仍由 HexMap 回调提供。
func set_auxiliary_visuals_visible(is_visible: bool, config: Dictionary) -> void:
	if not bool(config.get("hide_health_ui", false)):
		return

	var get_bar_manager: Callable = config.get("get_bar_manager", Callable())
	if get_bar_manager.is_valid():
		var bar_manager: Variant = get_bar_manager.call()
		if is_instance_valid(bar_manager):
			bar_manager.visible = is_visible
			if bar_manager.has_method("set_all_health_bars_visible"):
				bar_manager.set_all_health_bars_visible(is_visible)

	var get_total_enemy_health_bar: Callable = config.get("get_total_enemy_health_bar", Callable())
	if get_total_enemy_health_bar.is_valid():
		var total_health_bar: Variant = get_total_enemy_health_bar.call()
		if is_instance_valid(total_health_bar):
			total_health_bar.visible = is_visible


## 清空延迟血条请求队列和去重表。
## begin_if_needed() 与 flush_health_bar_requests() 都复用这一步，避免跨房间残留请求。
func _clear_pending_health_bar_requests(state: Dictionary) -> void:
	var requests: Array = state.get("pending_requests", [])
	requests.clear()
	state["pending_requests"] = requests

	var request_ids: Dictionary = state.get("pending_request_ids", {})
	request_ids.clear()
	state["pending_request_ids"] = request_ids


## 收集需要参与入场动画的地块栈。
## 非 Area2D 或已经失效的节点会被跳过，避免 Tween 挂到无效对象上。
func _get_reveal_stacks(config: Dictionary) -> Array[Area2D]:
	var result: Array[Area2D] = []
	var stack_nodes: Dictionary = config.get("stack_nodes", {})
	for stack in stack_nodes.values():
		if is_instance_valid(stack) and stack is Area2D:
			result.append(stack)
	return result


## 计算涟漪源点。
## 旧行为是从地图左下角外推一个 padding，让入场从屏幕左下方向推入。
func _get_reveal_origin(stacks: Array[Area2D], config: Dictionary) -> Vector2:
	var first_stack := stacks[0]
	var min_x := first_stack.position.x
	var max_y := first_stack.position.y

	for stack in stacks:
		if not is_instance_valid(stack):
			continue
		min_x = minf(min_x, stack.position.x)
		max_y = maxf(max_y, stack.position.y)

	var origin_padding: Vector2 = config.get("origin_padding", Vector2(80.0, 80.0))
	return Vector2(min_x - origin_padding.x, max_y + origin_padding.y)


## Tween 回调入口。
## 参数顺序匹配 tween_method 绑定：Tween 写入 value，后两个参数是固定的 stack 和 config。
func _set_stack_dissolve_from_tween(value: float, stack: Area2D, config: Dictionary) -> void:
	_set_stack_dissolve(stack, value, config)


## 设置一个地块栈所有已注册渲染节点的 dissolve_blend。
## 这里读取 stack meta 中的 sprites 队列，和 hover/highlight/平铺视图共享同一份渲染节点清单。
func _set_stack_dissolve(stack: Area2D, value: float, config: Dictionary) -> void:
	var set_stack_dissolve: Callable = config.get("set_stack_dissolve", Callable())
	if set_stack_dissolve.is_valid():
		set_stack_dissolve.call(stack, value)


## 由 HexMap 创建 Tween。
## Runner 不直接依赖场景树节点，便于后续把入场动画单独测试或替换。
func _create_tween(config: Dictionary) -> Tween:
	var create_tween_callable: Callable = config.get("create_tween", Callable())
	if not create_tween_callable.is_valid():
		return null
	var tween: Variant = create_tween_callable.call()
	if tween is Tween:
		return tween
	return null


## 调用 HexMap 注入的无参回调。
## 收尾阶段的交互刷新、敌人 roster 通知和动画完成信号都从这里发回 composition root。
func _call_config_callback(config: Dictionary, key: String) -> void:
	var callback: Callable = config.get(key, Callable())
	if callback.is_valid():
		callback.call()
