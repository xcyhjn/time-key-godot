class_name SettlementRewardController
extends RefCounted


## SettlementRewardController 负责局外收获建筑的状态扫描和数据整理。
## 它不创建 tooltip、不写 shader，也不打开奖励场景；
## 这些表现和 UI 流程继续分别归 SettlementRewardPresenter 与 in_scene.gd 管理。

## 扫描当前地图，找出拥有局外收获接口的建筑。
## 返回值包含：
## - reward_stacks：本轮可被奖励系统跟踪的 stack 列表。
## - reward_stack_data：stack 到 reward_info 的映射。
## - stacks_to_clear：没有有效奖励建筑、需要清理旧表现的 stack。
func collect(stack_nodes: Dictionary, map_data: Dictionary, config: Dictionary) -> Dictionary:
	var reward_stacks: Array[Area2D] = []
	var reward_stack_data: Dictionary = {}
	var stacks_to_clear: Array[Area2D] = []

	for coord in stack_nodes.keys():
		var stack = stack_nodes[coord]
		if not is_instance_valid(stack):
			continue

		var reward_landform := _get_settlement_reward_landform(stack, coord, map_data, config)
		if not is_instance_valid(reward_landform):
			stacks_to_clear.append(stack)
			continue

		var reward_info := _build_reward_info(stack, coord, reward_landform)
		reward_stacks.append(stack)
		reward_stack_data[stack] = reward_info

	return {
		"reward_stacks": reward_stacks,
		"reward_stack_data": reward_stack_data,
		"stacks_to_clear": stacks_to_clear,
	}


## 判断一个被跟踪的奖励 stack 当前是否还可以点击。
## HexMap 会把当前奖励模式作为快照传进来；controller 只消费这份状态，不反向读取 HexMap。
func is_stack_available(stack: Area2D, reward_stack_data: Dictionary, config: Dictionary) -> bool:
	if not bool(config.get("is_mode_available", false)):
		return false
	if not reward_stack_data.has(stack):
		return false

	var reward_info = reward_stack_data[stack]
	var reward_landform = reward_info.get("landform")
	return (
		_is_valid_settlement_reward_landform(reward_landform, config)
		and not _is_settlement_reward_used(reward_landform)
	)


## 标记一个奖励建筑已经被局外奖励页消费。
## 优先调用建筑自己的 `mark_settlement_reward_used()`；
## 如果旧建筑只暴露字段，则沿用旧行为直接写 `settlement_reward_used = true`。
func mark_used(reward_info: Dictionary) -> bool:
	if reward_info.is_empty():
		return false

	var reward_landform = reward_info.get("landform")
	if is_instance_valid(reward_landform) and reward_landform.has_method("mark_settlement_reward_used"):
		reward_landform.mark_settlement_reward_used()
		return true

	if is_instance_valid(reward_landform) and _object_has_property(reward_landform, &"settlement_reward_used"):
		reward_landform.set("settlement_reward_used", true)
		return true

	return false


## 从 stack metadata 和 map_data 两条路径读取奖励建筑。
## 运行时地貌注册、局部重绘或旧数据残留都可能只保留其中一条引用，所以这里保持旧双通道读取。
func _get_settlement_reward_landform(stack: Area2D, coord: Variant, map_data: Dictionary, config: Dictionary) -> Node:
	var occupant = stack.get_meta("occupant") if stack.has_meta("occupant") else null
	if _is_valid_settlement_reward_landform(occupant, config):
		return occupant

	if map_data.has(coord):
		var tile_data = map_data[coord]
		if typeof(tile_data) == TYPE_DICTIONARY:
			var landform_inst = tile_data.get("landform")
			if _is_valid_settlement_reward_landform(landform_inst, config):
				return landform_inst

	return null


## 判断一个地貌是否具备局外收获资格。
## 奖励类型和文案仍由建筑自己提供；这里只校验阵营、接口和绑定生死状态。
func _is_valid_settlement_reward_landform(candidate: Variant, config: Dictionary) -> bool:
	if not is_instance_valid(candidate):
		return false
	if not (candidate is landform):
		return false
	if candidate.Attitude != candidate.Attitude_Pool.Enemy:
		return false
	if not candidate.has_method("has_settlement_reward"):
		return false
	if not candidate.has_settlement_reward():
		return false
	return _matches_settlement_reward_bind_state(candidate, config)


## 根据 HexMap 传入的绑定模式判断奖励入口对应死亡建筑还是存活建筑。
## 这里用 int 快照比较 enum 值，避免 controller 直接依赖 HexMap 的 enum 名称。
func _matches_settlement_reward_bind_state(candidate: landform, config: Dictionary) -> bool:
	var bind_state := int(config.get("bind_state", -1))
	var dead_state := int(config.get("bind_state_dead", -2))
	var alive_state := int(config.get("bind_state_alive", -3))

	if bind_state == dead_state:
		return candidate.State_Main == candidate.Main_State_Pool.Broken
	if bind_state == alive_state:
		return candidate.State_Main != candidate.Main_State_Pool.Broken
	return false


## 把建筑实例整理成 Main 场景可以消费的 reward_info。
## 这个字典是 HexMap 发出 `settlement_reward_requested` 信号时的稳定 payload。
func _build_reward_info(stack: Area2D, coord: Variant, reward_landform: Node) -> Dictionary:
	var reward_type = reward_landform.get_settlement_reward_type() if reward_landform.has_method("get_settlement_reward_type") else ""
	var reward_label = reward_landform.get_settlement_reward_label() if reward_landform.has_method("get_settlement_reward_label") else reward_type
	return {
		"stack": stack,
		"coord": coord,
		"landform": reward_landform,
		"reward_type": reward_type,
		"reward_label": reward_label,
	}


## 判断奖励建筑是否已经消费过局外收获。
## 不创建默认字段，只读取已有属性，避免把奖励状态写入不支持该机制的建筑。
func _is_settlement_reward_used(reward_landform: Node) -> bool:
	if not is_instance_valid(reward_landform):
		return true
	if _object_has_property(reward_landform, &"settlement_reward_used"):
		return bool(reward_landform.get("settlement_reward_used"))
	return false


## 判断对象是否真实暴露某个属性。
## 这个 helper 从 HexMap 迁移而来，用来安全兼容旧建筑字段。
func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", "") == String(property_name):
			return true
	return false
