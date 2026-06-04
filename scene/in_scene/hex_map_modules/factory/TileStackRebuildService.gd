class_name TileStackRebuildService
extends RefCounted


## TileStackRebuildService 负责单个地块 stack 的重建边界。
## 当前版本严格保留旧流程：删除旧 stack、从 stack_nodes 移除、调用 HexMap 的 `_create_stack_at()` 重建，
## 然后刷新地块交互状态。它先把边界集中起来，不在本批直接做对象池或 metadata 复用。

## 重建指定坐标的地块视觉。
## 返回 true 表示找到 map_data 并完成重建；返回 false 表示该坐标当前没有地图数据。
func rebuild(coord: Vector2i, map_data: Dictionary, stack_nodes: Dictionary, config: Dictionary) -> bool:
	if not map_data.has(coord):
		return false

	var data = map_data[coord]
	_remove_old_stack(coord, stack_nodes)

	var create_stack_at := config["create_stack_at"] as Callable
	create_stack_at.call(coord, data)

	var refresh_stack_interactivity := config["refresh_stack_interactivity"] as Callable
	refresh_stack_interactivity.call()

	return true


## 移除旧 stack 节点并同步 stack_nodes。
## 旧节点可能已经被地块销毁流程释放，所以仍保留 `is_instance_valid()` 判断。
func _remove_old_stack(coord: Vector2i, stack_nodes: Dictionary) -> void:
	if not stack_nodes.has(coord):
		return

	var old_node = stack_nodes[coord]
	if is_instance_valid(old_node):
		old_node.queue_free()
	stack_nodes.erase(coord)
