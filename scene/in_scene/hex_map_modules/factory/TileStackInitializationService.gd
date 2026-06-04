class_name TileStackInitializationService
extends RefCounted


## TileStackInitializationService 负责地块栈创建完成后的初始化收尾。
## 它只接收 HexMap 已经准备好的 stack、metadata、状态开关和回调，
## 不直接读取 HexMap 成员变量，也不主动查找场景树，避免把地图主控重新耦合进来。

## 初始化一个已经创建好的地块栈。
## 当前迁移的是旧 `_create_stack_at()` 的末尾逻辑：
## 写入 metadata、设置入场 dissolve 初始值、连接鼠标输入信号，并在平铺视图下创建高度标签。
func initialize(stack: Area2D, config: Dictionary) -> void:
	_write_stack_metadata(stack, config)
	_apply_intro_dissolve(stack, config)
	_connect_input_signals(stack, config)
	_create_flat_height_indicator(stack, config)


## 写入后续战斗、视图切换、奖励扫描和地块销毁都会读取的 stack metadata。
## `occupant` 即使为空也保持写入，延续旧逻辑中 `has_meta("occupant")` 可以成立的行为。
func _write_stack_metadata(stack: Area2D, config: Dictionary) -> void:
	stack.set_meta("sprites", config["sprites"])
	stack.set_meta("height", int(config["height"]))
	stack.set_meta("occupant", config.get("occupant", null))


## 如果地图开场揭示动画正在运行，就把当前地块的 dissolve 初始值设为完全隐藏。
## 真正的 shader 参数写入仍由 HexMap 回调完成，服务不关心具体材质参数名。
func _apply_intro_dissolve(stack: Area2D, config: Dictionary) -> void:
	if not bool(config["intro_reveal_active"]):
		return
	var set_stack_dissolve := config["set_stack_dissolve"] as Callable
	set_stack_dissolve.call(stack, 1.0)


## 连接地块的 hover、离开 hover 和点击输入信号。
## 回调由 HexMap 预先 bind 好 stack 参数，这里只负责把旧信号连接搬离 `_create_stack_at()`。
func _connect_input_signals(stack: Area2D, config: Dictionary) -> void:
	stack.mouse_entered.connect(config["mouse_entered_callback"] as Callable)
	stack.mouse_exited.connect(config["mouse_exited_callback"] as Callable)
	stack.input_event.connect(config["input_event_callback"] as Callable)


## 平铺高度视图下，为新创建的 stack 补齐顶部高度数字。
## 指示器的实际节点结构归 HeightViewIndicatorPresenter 管，这里只通过 HexMap 回调触发旧入口。
func _create_flat_height_indicator(stack: Area2D, config: Dictionary) -> void:
	if not bool(config["is_flat_view"]):
		return
	var create_height_indicator := config["create_height_indicator"] as Callable
	create_height_indicator.call(stack, int(config["height"]))
