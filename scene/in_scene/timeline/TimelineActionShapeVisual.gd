class_name TimelineActionShapeVisual
extends Control

## ==========================================
## 时间轴行动整体形状绘制组件
## ==========================================
## 这个脚本只负责“视觉归属感”，不参与时间轴数据、不接收鼠标事件，也不改变 TimelineAction 的放置逻辑。
##
## 原本每个时间占位方格都是独立 Panel，多个格子贴在一起时，玩家很难判断它们属于同一个意图，
## 还是属于相邻的两个不同意图。这里用一个轻量 Control 的 _draw() 来补足两层视觉：
## - BACKPLATE：画在方格下面，把同一个意图的相邻格子用同色桥接起来；
## - OUTLINE：画在方格上面，只画整个形状的外轮廓，并可选画非常淡的内部连接线。
##
## 这样像 "11,1" 的 L 形会被看成一个整体，而两个相邻的 "11" 会被各自的外轮廓分开。
## 使用 _draw() 而不是为每条边额外创建节点，是为了减少节点数量，保持时间轴刷新和清除动画都足够轻。

enum VisualMode {
	BACKPLATE,
	OUTLINE
}

const DIR_LEFT := Vector2i(-1, 0)
const DIR_RIGHT := Vector2i(1, 0)
const DIR_UP := Vector2i(0, -1)
const DIR_DOWN := Vector2i(0, 1)

var visual_mode: int = VisualMode.BACKPLATE
var shape_coords: Array[Vector2i] = []
var slot_size: float = 40.0
var spacing: float = 2.0
var fill_color: Color = Color.WHITE
var outline_color: Color = Color.BLACK
var outline_width: float = 3.0
var internal_seam_color: Color = Color(1.0, 1.0, 1.0, 0.12)
var internal_seam_width: float = 1.0


## 由 timeline_ui.gd 创建后统一配置。
## 注意：shape_coords 必须是已经归一化到容器局部坐标的坐标，例如 [Vector2i(0,0), Vector2i(1,0)]。
func setup(
	p_visual_mode: int,
	p_shape_coords: Array[Vector2i],
	p_slot_size: float,
	p_spacing: float,
	p_fill_color: Color,
	p_outline_color: Color,
	p_outline_width: float,
	p_internal_seam_color: Color,
	p_internal_seam_width: float
) -> void:
	visual_mode = p_visual_mode
	shape_coords = p_shape_coords.duplicate()
	slot_size = p_slot_size
	spacing = max(p_spacing, 0.0)
	fill_color = p_fill_color
	outline_color = p_outline_color
	outline_width = max(p_outline_width, 0.0)
	internal_seam_color = p_internal_seam_color
	internal_seam_width = max(p_internal_seam_width, 0.0)

	mouse_filter = Control.MOUSE_FILTER_IGNORE
	queue_redraw()


## 清除动画生成 ghost 时使用：复制纯绘制参数，不复制任何运行时 shader / hover 状态。
func clone_visual() -> TimelineActionShapeVisual:
	var visual := TimelineActionShapeVisual.new()
	visual.name = name
	visual.mouse_filter = Control.MOUSE_FILTER_IGNORE
	visual.position = position
	visual.size = size
	visual.custom_minimum_size = custom_minimum_size
	visual.pivot_offset = pivot_offset
	visual.scale = scale
	visual.rotation = rotation
	visual.z_index = z_index
	visual.setup(
		visual_mode,
		shape_coords,
		slot_size,
		spacing,
		fill_color,
		outline_color,
		outline_width,
		internal_seam_color,
		internal_seam_width
	)
	return visual


func _draw() -> void:
	if shape_coords.is_empty():
		return

	var coord_lookup := _build_coord_lookup()
	if visual_mode == VisualMode.BACKPLATE:
		_draw_backplate(coord_lookup)
	else:
		_draw_internal_seams(coord_lookup)
		_draw_outer_outline(coord_lookup)


func _build_coord_lookup() -> Dictionary:
	var result: Dictionary = {}
	for coord in shape_coords:
		result[coord] = true
	return result


func _has_cell(coord_lookup: Dictionary, coord: Vector2i) -> bool:
	return coord_lookup.has(coord)


func _cell_position(coord: Vector2i) -> Vector2:
	var cell_step := slot_size + spacing
	return Vector2(coord.x * cell_step, coord.y * cell_step)


## 底板层：只负责让同一个意图的格子“粘起来”。
## 每个格子本体仍由 Panel 绘制；这里主要填补相邻格子之间的 spacing 缝隙。
func _draw_backplate(coord_lookup: Dictionary) -> void:
	for coord in shape_coords:
		var pos := _cell_position(coord)
		draw_rect(Rect2(pos, Vector2(slot_size, slot_size)), fill_color)

		var has_right := _has_cell(coord_lookup, coord + DIR_RIGHT)
		var has_down := _has_cell(coord_lookup, coord + DIR_DOWN)

		if has_right and spacing > 0.0:
			draw_rect(Rect2(pos + Vector2(slot_size, 0.0), Vector2(spacing, slot_size)), fill_color)
		if has_down and spacing > 0.0:
			draw_rect(Rect2(pos + Vector2(0.0, slot_size), Vector2(slot_size, spacing)), fill_color)
		if has_right and has_down and spacing > 0.0:
			# L 形或 2x2 连接处会出现一个 spacing 大小的角落空洞，这里顺手补齐。
			draw_rect(Rect2(pos + Vector2(slot_size, slot_size), Vector2(spacing, spacing)), fill_color)


## 内部弱分割线：让玩家仍能看出这个意图占了几个时间格，但不会像外边界一样强。
func _draw_internal_seams(coord_lookup: Dictionary) -> void:
	if internal_seam_width <= 0.0 or internal_seam_color.a <= 0.0:
		return

	var seam_inset: float = min(outline_width, slot_size * 0.45)
	for coord in shape_coords:
		var pos := _cell_position(coord)
		if _has_cell(coord_lookup, coord + DIR_RIGHT):
			var seam_x := pos.x + slot_size + spacing * 0.5
			draw_line(
				Vector2(seam_x, pos.y + seam_inset),
				Vector2(seam_x, pos.y + slot_size - seam_inset),
				internal_seam_color,
				internal_seam_width
			)
		if _has_cell(coord_lookup, coord + DIR_DOWN):
			var seam_y := pos.y + slot_size + spacing * 0.5
			draw_line(
				Vector2(pos.x + seam_inset, seam_y),
				Vector2(pos.x + slot_size - seam_inset, seam_y),
				internal_seam_color,
				internal_seam_width
			)


## 外轮廓层：只给没有同意图邻居的一侧画线。
## 同意图内部的边不画黑线，贴在一起的不同意图则会各自保留外边界。
func _draw_outer_outline(coord_lookup: Dictionary) -> void:
	if outline_width <= 0.0 or outline_color.a <= 0.0:
		return

	for coord in shape_coords:
		var pos := _cell_position(coord)
		var has_left := _has_cell(coord_lookup, coord + DIR_LEFT)
		var has_right := _has_cell(coord_lookup, coord + DIR_RIGHT)
		var has_up := _has_cell(coord_lookup, coord + DIR_UP)
		var has_down := _has_cell(coord_lookup, coord + DIR_DOWN)

		if not has_up:
			_draw_horizontal_outline(pos.y, pos.x, has_left, has_right)
		if not has_down:
			_draw_horizontal_outline(pos.y + slot_size, pos.x, has_left, has_right)
		if not has_left:
			_draw_vertical_outline(pos.x, pos.y, has_up, has_down)
		if not has_right:
			_draw_vertical_outline(pos.x + slot_size, pos.y, has_up, has_down)


func _draw_horizontal_outline(y: float, cell_x: float, has_left: bool, has_right: bool) -> void:
	var start_x := cell_x
	var end_x := cell_x + slot_size
	if has_left:
		start_x -= spacing
	if has_right:
		end_x += spacing
	draw_line(Vector2(start_x, y), Vector2(end_x, y), outline_color, outline_width)


func _draw_vertical_outline(x: float, cell_y: float, has_up: bool, has_down: bool) -> void:
	var start_y := cell_y
	var end_y := cell_y + slot_size
	if has_up:
		start_y -= spacing
	if has_down:
		end_y += spacing
	draw_line(Vector2(x, start_y), Vector2(x, end_y), outline_color, outline_width)
