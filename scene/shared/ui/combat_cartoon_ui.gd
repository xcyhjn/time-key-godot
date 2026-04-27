extends CanvasLayer

## 局内战斗专用 CartoonUI。
## 设计目标：
## - 只做“固定 HUD”，不播放局外第一次出现时的收纳动画。
## - Rect / Pole / Ring / Clock 使用与局外 CartoonUI 同一套视觉资源。
## - 闹钟固定停靠在左上角，面板和立杆固定在屏幕正上方。
## - 对外提供刷新时代、阶段、角色头像和顶部占位高度的接口，方便 in_scene.gd 调用。

const OUT_SCENE_TEMPLATE := preload("res://scene/out_scene/Out_Scene.tscn")

## 局内 HUD 预计占用的顶部高度。
## TimelineUI 会读取这个值，把时间轴向下挪，避免与上方面板互相盖住。
@export var reserved_height: float = 90.0

@export_group("Canvas Layers")
## Rect / Pole 所在的画布层。
## 局内主 UI 的 CanvasLayer 是 1000，因此面板层放到 900；
## 这样面板作为背景存在，不会盖住总血条、按钮、时间轴等信息。
@export var panel_canvas_layer: int = 900

## 文字信息层略高于面板，但仍低于局内主 UI。
## 这样局内其它信息需要显示在最上方时，不会被 CartoonUI 抢层级。
@export var menu_canvas_layer: int = 901

## 闹钟独立放在更高层。
## 面板可以不挡信息，同时闹钟仍然固定可见。
@export var clock_canvas_layer: int = 1100

## 闹钟最终缩放值，与局外 CartoonUI 缩到顶部后的大小保持一致。
@export var clock_scale: Vector2 = Vector2(1.5, 1.5)

## 闹钟中心距离屏幕顶部的像素位置。
## 这个值沿用局外收纳完成后的 y 坐标，让两个场景视觉一致。
@export var clock_top_margin: float = 160.0

## 闹钟视觉左边缘距离屏幕左侧的像素距离。
## 通过“左边距 + 半个闹钟宽度”计算中心点，和原右上角算法保持同一套边缘锚定逻辑。
@export var clock_left_margin: float = 50.0

## Rect / Pole 面板在屏幕顶部的水平锚点。
## 0.5 表示固定在屏幕正上方居中；窗口宽度变化时会重新计算。
@export_range(0.0, 1.0, 0.01) var panel_anchor_x_ratio: float = 0.0

## Rect / Pole 可见内容距离屏幕顶部的像素距离。
## TileMapLayer 的有效格子不是从 (0, 0) 开始，所以实际摆放时会扣掉 used_rect 的顶部偏移。
@export var panel_visual_top_margin: float = 0.0

## 顶部面板的额外像素偏移。
## 正常保持 0；如果未来需要让面板整体微调，只改这里即可。
@export var panel_offset: Vector2 = Vector2.ZERO

## Ring 是闹钟顶部铃铛装饰，应跟随右上角 Clock，而不是跟随居中的 Rect / Pole。
@export var ring_offset_from_clock: Vector2 = Vector2(0.0, -16.0)

@onready var rect: TileMapLayer = $Rect
@onready var pole: TileMapLayer = $Pole
@onready var clock_layer: CanvasLayer = get_node_or_null("ClockLayer") as CanvasLayer
@onready var menu_canvas: CanvasLayer = $MenuUI
@onready var ring: Sprite2D = get_node_or_null("ClockLayer/Ring") as Sprite2D
@onready var clock: Sprite2D = get_node_or_null("ClockLayer/Clock") as Sprite2D
@onready var point_controller: Node2D = get_node_or_null("ClockLayer/Clock/Point") as Node2D
@onready var menu_control: Control = $MenuUI/Control
@onready var color_bg: ColorRect = $ColorBG
@onready var era_label: Label = $MenuUI/Control/era
@onready var process_label: Label = $MenuUI/Control/process
@onready var character_icon: TextureRect = $MenuUI/Control/character

## 保存场景里配置好的原始缩放。
## 局内没有展开动画，所以运行时会直接恢复到这个值，而不是先压扁再 tween。
@onready var rect_target_scale: Vector2 = rect.scale if rect else Vector2.ONE
@onready var pole_target_scale: Vector2 = pole.scale if pole else Vector2.ONE

## 角色顺序与 Out_Scene.tscn 里 MapRenderer.tex_player_icons 的顺序保持一致。
const CHARACTER_TEXTURES: Array[Texture2D] = [
	preload("res://image/character/4.png"),
	preload("res://image/character/6.png"),
	preload("res://image/character/3.png"),
	preload("res://image/character/1.png"),
	preload("res://image/character/2.png"),
	preload("res://image/character/5.png")
]


func _ready() -> void:
	_apply_canvas_layers()
	_connect_viewport_resize_signal()
	apply_combat_layout()


## 统一设置局内 CartoonUI 的 CanvasLayer 层级。
## 这里是解决“调 z_index 仍然遮挡”的关键：
## z_index 只在同一个 CanvasLayer 内比较，跨 CanvasLayer 时优先看 layer 值。
func _apply_canvas_layers() -> void:
	layer = panel_canvas_layer

	if is_instance_valid(menu_canvas):
		menu_canvas.layer = menu_canvas_layer

	if is_instance_valid(clock_layer):
		clock_layer.layer = clock_canvas_layer


## 监听窗口尺寸变化。
## CanvasLayer 里的 Node2D 不会像 Control 一样自动重算 anchor，
## 所以这里需要在分辨率变化时重新计算右上角坐标。
func _connect_viewport_resize_signal() -> void:
	var viewport := get_viewport()
	if viewport and not viewport.size_changed.is_connected(_on_viewport_size_changed):
		viewport.size_changed.connect(_on_viewport_size_changed)


func _on_viewport_size_changed() -> void:
	apply_combat_layout()


## 局内入口：一次性生成并固定所有 UI 视觉。
## 这个方法可以安全重复调用；它只会刷新坐标、透明度和 tile 数据，不创建重复节点。
func apply_combat_layout() -> void:
	_ensure_panel_tile_data()
	_show_all_visuals_without_animation()
	_stop_clock_pointer()
	_place_panel()
	_place_clock_and_ring()


## 从局外 CartoonUI 模板复制 Rect / Pole 的 tile_set 与 tile_map_data。
## 关键点：
## - TileMapLayer 只有 tileset 不会自动出图，还必须有“哪些格子放了哪些 tile”的 tile_map_data。
## - 局外场景已经有正确数据，因此这里直接把源数据复制过来，保证局内面板和局外完全一致。
## - 如果将来你在 combat_cartoon_ui.tscn 里手动画好了格子，这个函数会检测到已有数据并跳过复制。
func _ensure_panel_tile_data() -> void:
	if not is_instance_valid(rect) or not is_instance_valid(pole):
		push_warning("CombatCartoonUI: Rect 或 Pole 节点缺失，无法初始化顶部面板。")
		return

	var rect_has_tiles := rect.tile_map_data.size() > 0
	var pole_has_tiles := pole.tile_map_data.size() > 0
	if rect_has_tiles and pole_has_tiles:
		return

	var template_scene := OUT_SCENE_TEMPLATE.instantiate()
	if not is_instance_valid(template_scene):
		push_warning("CombatCartoonUI: 无法实例化 Out_Scene 模板，顶部面板 tile 数据复制失败。")
		return

	var source_rect := template_scene.get_node_or_null("UI/CartoonUI/Rect") as TileMapLayer
	var source_pole := template_scene.get_node_or_null("UI/CartoonUI/Pole") as TileMapLayer

	if is_instance_valid(source_rect):
		rect.tile_set = source_rect.tile_set
		rect.tile_map_data = source_rect.tile_map_data
	else:
		push_warning("CombatCartoonUI: Out_Scene 模板中找不到有效的 Rect TileMapLayer。")

	if is_instance_valid(source_pole):
		pole.tile_set = source_pole.tile_set
		pole.tile_map_data = source_pole.tile_map_data
	else:
		push_warning("CombatCartoonUI: Out_Scene 模板中找不到有效的 Pole TileMapLayer。")

	template_scene.free()


## 局内不需要任何淡入、展开或黑幕转场。
## 这里直接把所有可见元素设为最终态，避免复用局外动画时出现“只剩闹钟”的中间状态。
func _show_all_visuals_without_animation() -> void:
	if is_instance_valid(color_bg):
		color_bg.hide()

	if is_instance_valid(ring):
		ring.show()
		ring.modulate.a = 1.0

	if is_instance_valid(rect):
		rect.modulate.a = 1.0
		rect.scale = rect_target_scale

	if is_instance_valid(pole):
		pole.modulate.a = 1.0
		pole.scale = pole_target_scale

	if is_instance_valid(menu_control):
		menu_control.modulate.a = 1.0
		menu_control.show()


## 停止闹钟指针状态机。
## point.gd 的 0 号状态是 STOP；局内 HUD 不跟随鼠标也不旋转。
func _stop_clock_pointer() -> void:
	if is_instance_valid(point_controller) and point_controller.has_method("change"):
		point_controller.change(0)


## 计算并应用顶部居中面板布局。
## Rect / Pole 是 TileMapLayer，不支持 Control 的 anchor；
## 因此这里用 viewport 宽度实时计算“屏幕顶部居中锚点”，达到固定锚点的效果。
func _place_panel() -> void:
	var screen_size := get_viewport().get_visible_rect().size
	var panel_x := screen_size.x * panel_anchor_x_ratio + panel_offset.x

	if is_instance_valid(rect):
		rect.position = Vector2(panel_x, _get_panel_layer_origin_y(rect) + panel_offset.y)

	if is_instance_valid(pole):
		pole.position = Vector2(panel_x, _get_panel_layer_origin_y(pole) + panel_offset.y)


## 把“可见内容顶部”转换为 TileMapLayer 原点的 y 坐标。
## 你的 Rect / Pole 数据来自局外场景，格子从较大的 y 坐标开始绘制；
## 如果直接把 TileMapLayer.position.y 设为顶部，就会像截图那样整体下沉。
func _get_panel_layer_origin_y(layer: TileMapLayer) -> float:
	if not is_instance_valid(layer) or layer.tile_set == null:
		return panel_visual_top_margin

	var used_rect := layer.get_used_rect()
	var tile_size := layer.tile_set.tile_size
	var used_top_offset := float(used_rect.position.y * tile_size.y) * layer.scale.y
	return panel_visual_top_margin - used_top_offset


## 计算并应用左上角闹钟布局。
## Clock / Ring 独立固定到左上角，不再把 Rect / Pole 一起拖到侧边。
func _place_clock_and_ring() -> void:
	var target_pos := _get_clock_target_position()
	var ring_pos := target_pos + ring_offset_from_clock

	if is_instance_valid(clock):
		clock.position = target_pos
		clock.scale = clock_scale
		clock.rotation = 0.0
		clock.modulate.a = 1.0

	if is_instance_valid(ring):
		ring.position = ring_pos


## 左上角停靠坐标。
## 计算中心点时加上半个贴图宽度，使 clock_left_margin 表示“闹钟左边缘到屏幕左侧”的距离。
func _get_clock_target_position() -> Vector2:
	var half_clock_width := 0.0

	if is_instance_valid(clock) and clock.texture:
		half_clock_width = clock.texture.get_size().x * clock_scale.x * 0.5

	return Vector2(clock_left_margin + half_clock_width, clock_top_margin)


## 给 TimelineUI 或其它布局系统读取顶部占位高度。
func get_reserved_height() -> float:
	return reserved_height


## 更新顶部时代和阶段文字。
## 局内战斗回合推进时，in_scene.gd 会重新调用这个函数刷新显示。
func set_progress_labels(era_value: int, phase_value: int) -> void:
	if is_instance_valid(era_label):
		era_label.text = "第%d时代" % max(era_value, 1)

	if is_instance_valid(process_label):
		process_label.text = "%d / 8" % max(phase_value, 1)


## 设置顶部左侧角色头像。
## 如果 index 无效，保持场景里默认的未知头像，避免进入未选角状态时报错。
func set_character_index(index: int) -> void:
	if not is_instance_valid(character_icon):
		return

	if index >= 0 and index < CHARACTER_TEXTURES.size():
		character_icon.texture = CHARACTER_TEXTURES[index]

func _on_setting_button_down() -> void:
	pass # Replace with function body.
