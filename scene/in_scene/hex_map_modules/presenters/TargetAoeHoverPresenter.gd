class_name TargetAoeHoverPresenter
extends RefCounted

const HEX_TARGET_RULES := preload("res://scene/in_scene/hex_map_modules/rules/HexTargetRules.gd")


## TargetAoeHoverPresenter 负责生成卡牌目标 AOE hover 的展示计划。
## 它只回答三个问题：
## - 当前中心地块对应哪些 AOE 地块。
## - 中心目标应该显示合法状态还是非法状态。
## - 是否需要让 MainBoard 刷新目标 tooltip。
## 真正的 shader 状态写入、MainBoard 查找和 tooltip 调用仍由 HexMap 负责。


## 根据卡牌和中心地块生成 AOE hover 展示计划。
## 返回 Dictionary 是为了让 HexMap 逐步迁移旧表现入口时保持兼容，避免一次性改动视觉 presenter 和 UI controller。
func build_display_plan(card: Variant, center_stack: Area2D, config: Dictionary) -> Dictionary:
	var stack_nodes: Dictionary = _get_stack_nodes(config)
	var center_coord: Variant = _get_center_coord(center_stack, stack_nodes)
	var new_aoe_stacks: Array[Area2D] = HEX_TARGET_RULES.get_effect_range_stacks(card, center_coord, stack_nodes)
	var target_state: int = _get_target_state(center_stack, config)

	return {
		"new_aoe_stacks": new_aoe_stacks,
		"target_state": target_state,
		"tooltip_requested": _should_request_tooltip(card, center_stack),
		"tooltip_stack": center_stack,
		"tooltip_card": card,
	}


## 从 HexMap 传入的配置中读取 stack_nodes。
## 本模块需要用它反查中心坐标，并把卡牌 effect_range 转换成真实存在的 Area2D。
func _get_stack_nodes(config: Dictionary) -> Dictionary:
	var value: Variant = config.get("stack_nodes", {})
	if typeof(value) == TYPE_DICTIONARY:
		return value
	return {}


## 按旧逻辑从 stack_nodes 反查中心地块坐标。
## 如果地块已经释放或不在地图里，返回 null，让 HexTargetRules 自然得到空范围。
func _get_center_coord(center_stack: Area2D, stack_nodes: Dictionary) -> Variant:
	if not is_instance_valid(center_stack):
		return null
	return stack_nodes.find_key(center_stack)


## 计算中心地块的视觉状态。
## 合法性仍由 HexMap 注入旧 `_is_stack_valid_target()`，保证本次拆分不改变 CardManager 与目标规则的实际判断来源。
func _get_target_state(center_stack: Area2D, config: Dictionary) -> int:
	var invalid_state := int(config.get("target_invalid_state", 2))
	if _is_stack_valid_target(center_stack, config):
		return int(config.get("target_valid_state", 1))
	return invalid_state


## 判断是否需要刷新目标 tooltip。
## 当前只在卡牌和中心地块都有效时请求刷新；真正是否有 MainBoard、是否有更新方法仍由 HexMap 判断。
func _should_request_tooltip(card: Variant, center_stack: Area2D) -> bool:
	return _is_live_object(card) and is_instance_valid(center_stack)


## 调用 HexMap 注入的目标合法性检查。
## 这里不直接读取 CardManager，是为了保持本模块只产出 hover 展示计划，不承担场景根依赖。
func _is_stack_valid_target(stack: Area2D, config: Dictionary) -> bool:
	var callback: Callable = config.get("is_stack_valid_target", Callable())
	if not callback.is_valid():
		return false
	return bool(callback.call(stack))


## 判断 Variant 是否是仍然存活的 Godot 对象。
## 卡牌对象来自 CardManager 或旧回调链，进入 presenter 前先做对象门禁能避免释放后实例继续刷新 tooltip。
func _is_live_object(value: Variant) -> bool:
	return value is Object and is_instance_valid(value)
