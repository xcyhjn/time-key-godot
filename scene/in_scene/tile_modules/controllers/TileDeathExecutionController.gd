extends RefCounted

## TileDeathExecutionController 只负责执行 Tile 已进入 Broken 后的通用死亡收尾。
## 它不判断血量、不扣血、不释放 tile 节点、不重建地图视觉，也不选择死亡贴图。

const BROKEN_DAMAGE_RATE: float = 1.0


func execute(status_component: Object, tex_toggle_callback: Callable) -> Dictionary:
	_clear_statuses(status_component)
	_apply_broken_texture(tex_toggle_callback)
	return {"damage_rate": BROKEN_DAMAGE_RATE}


func notify_topology_changed(owner_battle: Object) -> void:
	_emit_tile_topology_changed(owner_battle)


func _clear_statuses(status_component: Object) -> void:
	if is_instance_valid(status_component) and status_component.has_method("clear_statuses"):
		status_component.clear_statuses()


func _apply_broken_texture(tex_toggle_callback: Callable) -> void:
	if tex_toggle_callback.is_valid():
		tex_toggle_callback.call()


func _emit_tile_topology_changed(owner_battle: Object) -> void:
	if is_instance_valid(owner_battle) and owner_battle.has_signal("tile_topology_changed"):
		owner_battle.emit_signal("tile_topology_changed")
