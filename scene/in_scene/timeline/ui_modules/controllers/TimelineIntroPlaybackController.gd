extends RefCounted

## TimelineIntroPlaybackController 负责在 TimelineUI 与 TimelineIntroAnimator 之间维护入场动画播放状态，
## 并转发初始化、背景格子入场、行动入场和停止请求。
## 它不创建时间轴行动容器，不播放具体 Tween，不修改 TimelineManager 数据，也不决定敌人意图规则。

var _intro_has_played: bool = false
var _intro_in_progress: bool = false


func setup(
	animator: TimelineIntroAnimator,
	timeline_ui: Control,
	grid_background: Control,
	shape_layer: Control,
	grid_cells: Dictionary
) -> void:
	if not is_instance_valid(animator):
		return

	animator.setup(timeline_ui, grid_background, shape_layer)
	animator.prepare_grid_for_intro(grid_cells)


func play_intro(
	animator: TimelineIntroAnimator,
	grid_cells: Dictionary,
	grid_width: int,
	grid_height: int
) -> void:
	if not is_instance_valid(animator):
		return
	if _intro_in_progress:
		await animator.grid_intro_finished
		return
	if _intro_has_played and animator.play_grid_intro_once:
		return

	_intro_in_progress = true
	await animator.play_grid_intro(grid_cells, grid_width, grid_height)
	_intro_in_progress = false
	_intro_has_played = true


func is_intro_in_progress() -> bool:
	return _intro_in_progress


func should_play_action_intro(animator: TimelineIntroAnimator, action: TimelineAction) -> bool:
	if not is_instance_valid(animator):
		return false
	return animator.should_animate_action(action)


func play_action_intro(
	animator: TimelineIntroAnimator,
	action_container: Control,
	action: TimelineAction,
	grid_width: int,
	grid_height: int,
	force_restart: bool
) -> void:
	if not is_instance_valid(animator):
		return
	animator.play_action_intro(action_container, action, grid_width, grid_height, force_restart)


func stop_action_intros(animator: TimelineIntroAnimator) -> void:
	if not is_instance_valid(animator):
		return
	animator.stop_action_intros()
