class_name ExternalRenderNodeRegistrar
extends RefCounted


## 按 landform 实例重新收编它对应的血条视觉节点。
## 该入口只处理视觉节点注册，不负责创建血条，也不修改血量数据。
func recollect_for_landform(entity: landform, bar_manager: Node, context: Dictionary) -> void:
	if not is_instance_valid(entity):
		return
	if not is_instance_valid(bar_manager):
		return

	var health_bar := find_health_bar_for_landform(entity, bar_manager)
	if not is_instance_valid(health_bar):
		return

	register_node_at(entity.location, health_bar, context)


## 接收血条或其他外部 UI 节点，并把其中可渲染的贴图节点加入地块 sprites 队列。
## HexMap 后续的 hover、高亮和平铺视角同步都继续读取 stack meta 中的这份队列。
func register_node_at(coord: Vector2i, node: Node, context: Dictionary) -> void:
	if not is_instance_valid(node):
		return

	var stack_nodes: Dictionary = context.get("stack_nodes", {})
	if not stack_nodes.has(coord):
		return

	var stack := stack_nodes[coord] as Area2D
	if not is_instance_valid(stack):
		return

	var sprites_list := _get_or_init_stack_sprites(stack)
	var height := _get_stack_height(stack)
	_collect_render_nodes(node, sprites_list, height)
	stack.set_meta("sprites", sprites_list)


## 根据 landform 实例 id 查找 BarManager 生成的单体血条。
## 命名规则沿用旧路径：`HealthBar_<landform_instance_id>`。
func find_health_bar_for_landform(entity: landform, bar_manager: Node) -> Node:
	if not is_instance_valid(entity) or not is_instance_valid(bar_manager):
		return null
	return bar_manager.get_node_or_null("HealthBar_" + str(entity.get_instance_id()))


## 从 stack meta 读取 sprites，缺失或类型异常时创建空数组。
## 这里会把数组写回 stack，保证调用后 HexMap 读取到的是同一个队列。
func _get_or_init_stack_sprites(stack: Area2D) -> Array:
	if is_instance_valid(stack) and stack.has_meta("sprites"):
		var stored_sprites: Variant = stack.get_meta("sprites")
		if stored_sprites is Array:
			return stored_sprites

	var sprites: Array = []
	if is_instance_valid(stack):
		stack.set_meta("sprites", sprites)
	return sprites


## 读取地块高度元数据。
## 高度只用于给外部 UI shader 传 `block_idx/total_height`，不参与地形数据修改。
func _get_stack_height(stack: Area2D) -> int:
	if not is_instance_valid(stack):
		return 1
	if stack.has_meta("height"):
		return max(1, int(stack.get_meta("height")))
	return 1


## 递归收集外部节点中的可渲染节点。
## 支持 Sprite2D、TextureRect、TextureProgressBar，保持旧血条注册路径的渲染范围。
func _collect_render_nodes(node: Node, sprites_list: Array, height: int) -> void:
	if not is_instance_valid(node):
		return

	if _is_supported_render_node(node):
		_register_render_node(node, sprites_list, height)

	for child in node.get_children():
		_collect_render_nodes(child, sprites_list, height)


## 判断一个节点是否应该被加入地块渲染队列。
## 这些类型会参与地块 hover/highlight shader 参数和视角同步。
func _is_supported_render_node(node: Node) -> bool:
	return node is Sprite2D or node is TextureRect or node is TextureProgressBar


## 将一个外部渲染节点加入 sprites 队列，并补齐地块高度 shader 参数。
## 不替换节点 material；只写实例参数，避免覆盖血条自己的 UI shader。
func _register_render_node(node: Node, sprites_list: Array, height: int) -> void:
	if sprites_list.has(node):
		return

	if node is CanvasItem:
		var item := node as CanvasItem
		if item.material:
			item.set_instance_shader_parameter("block_idx", float(height + 1))
			item.set_instance_shader_parameter("total_height", float(height + 2))

	sprites_list.append(node)
