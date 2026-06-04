class_name TileTurnBehaviorRunner
extends RefCounted


## TileTurnBehaviorRunner 负责触发地貌/建筑的旧回合行为。
## 它只迁移 HexMap 里 `_on_step_next()` 的遍历顺序和调用方式，
## 不修改任何建筑脚本的 `Behavior()` 协议，避免影响旧地貌行为。

## 按旧顺序执行一轮建筑行为。
## 必须先处理 iron_mine.Library 中记录的矿井，再处理其他地貌；
## 某些建筑会依赖这个顺序影响周围建筑的 willing 行为。
func run(step: int, behavior: int, map_data: Dictionary, rng: RandomNumberGenerator) -> void:
	_run_iron_mine_behaviors(step, behavior, map_data, rng)
	_run_other_landform_behaviors(step, behavior, map_data, rng)


## 优先处理 iron_mine.Library 中登记的矿井坐标。
## 这里保留旧判断：只有 map_data 中仍是 iron_mine 的有效 landform 才会执行。
func _run_iron_mine_behaviors(step: int, behavior: int, map_data: Dictionary, rng: RandomNumberGenerator) -> void:
	for coord_v2 in iron_mine.Library:
		if not map_data.has(coord_v2):
			continue
		var data = map_data[coord_v2]
		if not _is_named_landform(data, "iron_mine"):
			continue
		_call_behavior(data["landform"], step, behavior, map_data, rng)


## 再处理地图中除 iron_mine 之外的其他地貌。
## 遍历 `map_data.keys()` 的旧方式保持不变，避免改变同一回合内扩张/清理后的读取顺序。
func _run_other_landform_behaviors(step: int, behavior: int, map_data: Dictionary, rng: RandomNumberGenerator) -> void:
	for coord_v2 in map_data.keys():
		var data = map_data[coord_v2]
		if not _has_valid_landform(data):
			continue
		var landform_inst = data["landform"]
		if landform_inst.landform_name == "iron_mine":
			continue
		_call_behavior(landform_inst, step, behavior, map_data, rng)


## 判断地块数据里是否有一个有效地貌实例。
## 旧代码依赖 `data["landform"]` 和 `landform_name`，本批只集中判断，不改变字段契约。
func _has_valid_landform(data: Variant) -> bool:
	if not (data is Dictionary):
		return false
	if not data.has("landform"):
		return false
	return is_instance_valid(data["landform"])


## 判断地块数据里是否有指定名称的有效地貌。
## 用于保留 iron_mine 优先行为，不扩大到新的类型系统。
func _is_named_landform(data: Variant, expected_name: String) -> bool:
	if not _has_valid_landform(data):
		return false
	return data["landform"].landform_name == expected_name


## 调用旧建筑行为入口。
## `Other` 参数继续传 null，参数顺序保持旧 `_on_step_next()` 完全一致。
func _call_behavior(landform_inst: Node, step: int, behavior: int, map_data: Dictionary, rng: RandomNumberGenerator) -> void:
	if not is_instance_valid(landform_inst):
		return
	if not landform_inst.has_method("Behavior"):
		return
	landform_inst.Behavior(step, map_data, null, behavior, rng)
