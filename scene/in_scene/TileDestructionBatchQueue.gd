class_name TileDestructionBatchQueue
extends RefCounted

const QUEUED_META_KEY := "queued_for_height_limit_destruction"

var _pending_entries: Array[Dictionary] = []
var _is_running := false


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
