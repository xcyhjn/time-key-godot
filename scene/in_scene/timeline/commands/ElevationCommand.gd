# ==========================================
# 脚本名称: ElevationCommand.gd
# 功能概述: 地形升降执行命令，负责调度地块的高度更改和特效等待。
# ==========================================
class_name ElevationCommand extends EffectCommand

const TARGET_RULES := preload("res://scene/in_scene/timeline/commands/TimelineCommandTargetRules.gd")

var elevation_value: int

## 初始化地形升降命令。
## elevation_value 是对每个目标地块应用的高度变化量。
func _init(amt: int):
	self.elevation_value = amt

## 执行地形升降效果。
## 地形命令不需要 occupant 校验，只把目标地块交给 HexMap 的高度动画入口。
func execute(tree: SceneTree) -> void:
	if not is_instance_valid(hex_map) or target_tiles.is_empty():
		return
		
	
	if hex_map.has_method("animate_elevation_change"):
		# 1. 并行触发：循环调用所有地块的升降动画。
		# 这些协程会自行等待高度变化、超限销毁队列和最终清理。
		for tile in target_tiles:
			if is_instance_valid(tile):
				hex_map.animate_elevation_change(tile, elevation_value)
		
		# 2. 统一阻塞：等待本次所有地块退出动画/销毁队列。
		await _wait_for_tiles_to_finish(tree)
	else:
		pass


## 等待所有目标地块完成升降动画或超限销毁队列。
## 这个等待保证时间轴结算不会在地形还未稳定时进入下一条命令。
func _wait_for_tiles_to_finish(tree: SceneTree) -> void:
	var has_pending := true
	while has_pending:
		has_pending = false
		for tile in target_tiles:
			if not is_instance_valid(tile):
				continue
			var is_animating := tile.has_meta("is_animating") and bool(tile.get_meta("is_animating"))
			var queued_for_destruction := tile.has_meta("queued_for_height_limit_destruction")
			if is_animating or queued_for_destruction:
				has_pending = true
				break
		if has_pending:
			await tree.process_frame

	if is_instance_valid(hex_map):
		var padding := 0.0
		if TARGET_RULES.object_has_property(hex_map, &"elevation_resolution_padding"):
			padding = maxf(0.0, float(hex_map.get("elevation_resolution_padding")))
		if padding > 0.0:
			await tree.create_timer(padding).timeout
