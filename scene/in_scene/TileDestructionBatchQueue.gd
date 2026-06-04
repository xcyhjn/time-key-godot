class_name TileDestructionBatchQueue
extends RefCounted

## TileDestructionBatchQueue 只负责高度超限地块的销毁调度。
## 真正的 VFX、map_data/stack_nodes 清理、信号派发仍由调用方传入的 `perform_destruction` 完成。

const QUEUED_META_KEY := "queued_for_height_limit_destruction"

var _pending_entries: Array[Dictionary] = []
var _is_running := false


## 将一个地块加入销毁队列，并等待它的 QUEUED_META_KEY 被移除。
## `batch_size/collect_delay/batch_interval` 来自 `hex_map.gd` 的导出变量，
## `perform_destruction` 必须是可 await 的 Callable，参数为 `(stack, coord)`。
func queue_and_wait(
	stack: Area2D,
	coord: Vector2i,
	tree: SceneTree,
	batch_size: int,
	collect_delay: float,
	batch_interval: float,
	perform_destruction: Callable
) -> void:
	if not is_instance_valid(stack):
		return
	if tree == null:
		return

	stack.set_meta("is_animating", false)
	if not stack.has_meta(QUEUED_META_KEY):
		stack.set_meta(QUEUED_META_KEY, true)
		_pending_entries.append({"stack": stack, "coord": coord})

	if not _is_running:
		_run_batches(tree, batch_size, collect_delay, batch_interval, perform_destruction)

	while is_instance_valid(stack) and stack.has_meta(QUEUED_META_KEY):
		await tree.process_frame


## 按批处理当前 pending 队列。
## 第一次启动时会先等待 collect_delay，用于把同一波高度结算产生的多个地块收集到同一批。
func _run_batches(
	tree: SceneTree,
	batch_size: int,
	collect_delay: float,
	batch_interval: float,
	perform_destruction: Callable
) -> void:
	if _is_running:
		return

	_is_running = true
	if collect_delay > 0.0:
		await tree.create_timer(collect_delay).timeout
	else:
		await tree.process_frame

	while not _pending_entries.is_empty():
		var batch: Array[Dictionary] = []
		var normalized_batch_size := maxi(1, batch_size)
		while batch.size() < normalized_batch_size and not _pending_entries.is_empty():
			var entry: Dictionary = _pending_entries.pop_front()
			var entry_stack: Variant = entry.get("stack", null)
			if is_instance_valid(entry_stack):
				batch.append(entry)

		if batch.is_empty():
			continue

		for entry in batch:
			_perform_entry(entry, perform_destruction)

		await _wait_for_batch(tree, batch)
		if batch_interval > 0.0 and not _pending_entries.is_empty():
			await tree.create_timer(batch_interval).timeout

	_is_running = false


## 执行单个队列条目。
## 回调执行结束后移除 queued meta，让 `queue_and_wait()` 和 `_wait_for_batch()` 都能结束等待。
func _perform_entry(entry: Dictionary, perform_destruction: Callable) -> void:
	var stack_value: Variant = entry.get("stack", null)
	var coord: Vector2i = entry.get("coord", Vector2i.ZERO)
	if not is_instance_valid(stack_value):
		return

	if not (stack_value is Area2D):
		return

	var stack: Area2D = stack_value
	await perform_destruction.call(stack, coord)
	if is_instance_valid(stack):
		stack.remove_meta(QUEUED_META_KEY)


## 等待一批里的所有有效地块都完成销毁。
## 判断依据是每个 stack 是否还保留 QUEUED_META_KEY，而不是依赖固定动画时长。
func _wait_for_batch(tree: SceneTree, batch: Array[Dictionary]) -> void:
	var has_pending := true
	while has_pending:
		has_pending = false
		for entry in batch:
			var stack_value: Variant = entry.get("stack", null)
			if not is_instance_valid(stack_value):
				continue

			if not (stack_value is Area2D):
				continue

			var stack = stack_value
			if is_instance_valid(stack) and stack.has_meta(QUEUED_META_KEY):
				has_pending = true
				break
		if has_pending:
			await tree.process_frame
