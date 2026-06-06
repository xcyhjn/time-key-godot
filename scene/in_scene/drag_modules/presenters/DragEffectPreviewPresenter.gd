extends RefCounted

## DragEffectPreviewPresenter 只负责拖拽目标效果预览的显示和清理。
## 它不生成预览文案，不执行卡牌效果，也不改变拖拽或放置状态。

var _owner: Node


func _init(owner: Node) -> void:
	_owner = owner


func trigger_preview(target_tile: Node, damage_amount: int) -> void:
	var target_enemy = get_enemy_on_tile(target_tile)
	if target_enemy and target_enemy.has_method("show_card_effect_preview"):
		target_enemy.show_card_effect_preview(damage_amount)

	var health_manager = get_health_bar_manager()
	if health_manager and health_manager.has_method("preview_damage_effect"):
		health_manager.preview_damage_effect(damage_amount)


func clear_preview(target_tile: Node) -> void:
	var target_enemy = get_enemy_on_tile(target_tile)
	if target_enemy and target_enemy.has_method("clear_card_effect_preview"):
		target_enemy.clear_card_effect_preview()

	var health_manager = get_health_bar_manager()
	if health_manager and health_manager.has_method("clear_preview_effect"):
		health_manager.clear_preview_effect()


func get_enemy_on_tile(tile: Node) -> Node:
	if not is_instance_valid(tile):
		return null

	# 这里暂时保留原有占位行为，后续可按项目结构补充地块到敌人的查询。
	return null


func get_health_bar_manager() -> Node:
	if not is_instance_valid(_owner):
		return null

	var tree = _owner.get_tree()
	if tree == null:
		return null

	var scene_root = tree.current_scene
	if not scene_root:
		return null

	var health_manager = scene_root.get_node_or_null("HealthBarManager")
	if health_manager:
		return health_manager

	var managers = tree.get_nodes_in_group("health_bar_manager")
	if not managers.is_empty():
		return managers[0]

	return null
