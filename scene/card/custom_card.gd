# 原文件名: custom_card(卡牌模板).gd
# 功能: 卡牌模板
class_name CustomCard
extends Card  # 直接继承插件自带的 Card 类，白嫖它所有底层功能！

# ================= 我们的视觉变量 =================
var tween: Tween

# ================= 卡牌状态机 =================
enum CustomCardState {
	IDLE,  # 在手牌中（默认状态）
	SELECTED,  # 选中状态（悬浮动画）
	DRAGGING,  # 拖拽中（离开手牌容器，跟随鼠标）
	PLACED,  # 已放置在时间轴
	RETURNING  # 正在返回手牌
}

var card_current_state: CustomCardState = CustomCardState.IDLE
var original_parent: Node = null  # 记录原始父节点（手牌容器）
var card_original_position: Vector2  # 原始位置（用于返回动画）
var card_original_scale: Vector2  # 原始缩放（用于返回动画）
var original_z_index: int  # 原始层级（用于返回动画）
var original_material: Material = null  # 原始材质（用于清除拖拽效果）

# ★ 导出调整项：在属性面板实时调参
# ================================
@export_group("卡牌悬浮")
@export var float_height: float = -40.0  # 选中时向上浮动的高度
@export var tilt_intensity: float = 0.15  # 倾斜跟随鼠标的强度

var raw_description: String = ""
var active_keywords: Array = []  # 记录当前牌有哪些关键词，供悬停Tooltip读取
# 动态属性存储
var base_stats: Dictionary = { }
var current_stats: Dictionary = { }
var is_selected: bool = false
@onready var shadow: TextureRect = $ShadowLayer

# ================= 时间占位系统变量 =================
@onready var time_block_sprite: Node = $TimeBlock  # 时间占位图片节点（可能是Control类型如TextureRect/ColorRect，也可能是Sprite2D）
var timeline_shape_key: String = ""  # 形状键名，如 "1x1", "2x2"
var timeline_shape_size: Vector2i = Vector2i(1, 1)  # 形状尺寸，如 1x1, 1x2, 2x2
var timeline_shape_coords: Array[Vector2i] = []  # 形状坐标数组

# 时间占位图片资源字典
const TIMELINE_SHAPE_TEXTURES: Dictionary = {
	"1x1": preload("res://image/time_block/1x1.png"),
	"1x2": preload("res://image/time_block/1x2.png"),
	"2x2": preload("res://image/time_block/2x2.png")
	# 可扩展更多形状
}

# 时间占位图片设置
@export_group("时间占位图片设置")
@export var time_block_offset: Vector2 = Vector2.ZERO  # 位置偏移量
@export var time_block_scale_factor: float = 1.0  # 缩放因子
@export var time_block_base_size: float = 80.0  # 基础格子大小，应与DragShapeController的slot_size一致
@export var show_time_block_in_hand: bool = true  # 在手牌中是否显示时间占位图片

func _ready() -> void:
	super._ready()  # 必须先执行父类原有的初始化逻辑

	# 延迟一帧执行，确保卡牌工厂已经把 JSON 数据赋值给了 card_info
	call_deferred("setup_card_data")

	# 确保材质独立
	if material:
		material = material.duplicate()
	elif front_face_texture and front_face_texture.material:
		front_face_texture.material = front_face_texture.material.duplicate()

	# 保存卡牌原始状态（用于状态恢复）
	_save_original_state()

	# 如果有框架自带的 hovered 信号，可以在这里断开或覆盖
	#gui_input.connect(_on_gui_input)
	# ==========================================
# ★ 新增：状态管理函数
# ==========================================


## 保存卡牌原始状态
func _save_original_state() -> void:
	original_parent = get_parent()
	card_original_position = global_position
	card_original_scale = scale
	original_z_index = z_index
	if material:
		original_material = material.duplicate()
	else:
		original_material = null

	# 同步到父类变量，确保兼容性
	original_position = card_original_position
	original_scale = card_original_scale

	GameLogger.debug("卡牌原始状态已保存: parent=%s, pos=%s, scale=%s" % [
		original_parent.name if original_parent else "null",
		card_original_position,
		card_original_scale
	], "CustomCard")


## 进入拖拽状态
func enter_dragging_state() -> void:
	if card_current_state == CustomCardState.DRAGGING:
		return

	GameLogger.info("卡牌进入拖拽状态", "CustomCard")
	card_current_state = CustomCardState.DRAGGING

	# 保存当前状态（确保最新）
	_save_original_state()

	# 应用拖拽视觉效果（由DragShapeController设置材质）
	# 这里只更新状态，材质由外部控制器设置


## 返回手牌状态
func return_to_hand() -> void:
	if card_current_state == CustomCardState.RETURNING or card_current_state == CustomCardState.IDLE:
		return

	GameLogger.info("卡牌开始返回手牌", "CustomCard")
	card_current_state = CustomCardState.RETURNING

	# 清除拖拽视觉效果
	material = original_material
	if front_face_texture:
		front_face_texture.material = original_material
	set_card_transparency(1.0)

	# 调试日志：缩放值
	GameLogger.debug("返回手牌 - 原始缩放: %s, 当前缩放: %s, 原始位置: %s" % [
		card_original_scale, scale, card_original_position
	], "CustomCard")

	# 重新父级化：将卡牌返回原始父节点（手牌容器）
	var current_parent = get_parent()
	if current_parent and is_instance_valid(original_parent) and current_parent != original_parent:
		GameLogger.info("将卡牌重新父级化到原始父节点: %s" % original_parent.name, "CustomCard")

		# 保存当前全局位置，以便动画平滑过渡
		var current_global_pos = global_position
		var current_global_scale = scale
		var current_z_index = z_index

		# 重新父级化
		current_parent.remove_child(self)
		original_parent.add_child(self)
		set_as_top_level(false)

		# 设置位置和缩放，保持视觉连续性
		global_position = current_global_pos
		scale = current_global_scale
		z_index = current_z_index

		# 调试信息：记录父节点类型
		GameLogger.debug("原始父节点类型: %s, 是CanvasItem: %s, 有to_local方法: %s" % [
			original_parent.get_class(),
			original_parent is CanvasItem,
			original_parent.has_method("to_local")
		], "CustomCard")

		# 启动返回动画 - 直接使用全局位置补间，避免坐标转换问题
		var tw = create_tween().set_parallel(true)
		tw.tween_property(self, "global_position", card_original_position, 0.3)
		tw.tween_property(self, "scale", card_original_scale, 0.3)
		tw.tween_property(self, "z_index", original_z_index, 0.1)

		# 动画完成后恢复状态
		await tw.finished

		# 通知手牌容器重新布局（如果支持）
		if original_parent.has_method("add_card"):
			original_parent.add_card(self)
			GameLogger.info("已通知手牌容器重新布局", "CustomCard")
	else:
		# 如果已经在原始父节点中，直接执行动画
		GameLogger.info("卡牌已在原始父节点中，直接执行返回动画", "CustomCard")
		var tw = create_tween().set_parallel(true)
		tw.tween_property(self, "global_position", card_original_position, 0.3)
		tw.tween_property(self, "scale", card_original_scale, 0.3)
		tw.tween_property(self, "z_index", original_z_index, 0.1)
		await tw.finished

	card_current_state = CustomCardState.IDLE
	GameLogger.info("卡牌已返回手牌，状态重置为IDLE", "CustomCard")


## 更新拖拽位置（由DragShapeController调用）
func update_drag_position(new_position: Vector2) -> void:
	if card_current_state != CustomCardState.DRAGGING:
		return

	global_position = new_position


# ==========================================
# ★ 新增：动态获取 CardManager 的函数
# ==========================================
func get_card_manager() -> Node:
	# 尝试通过父节点链查找CardManager
	var current = get_parent()
	while current != null:
		if current.name == "CardManager" or current is CardManager:
			return current
		current = current.get_parent()
	
	# 备用方案：通过元数据查找
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		return tree_root.get_meta("card_manager")
	
	# 回退到当前场景元数据
	var scene_root = get_tree().current_scene
	if scene_root and scene_root.has_meta("card_manager"):
		return scene_root.get_meta("card_manager")
	
	return null


# ==========================================
# ★ 修改：使用动态获取的引用来操作选中状态
# ==========================================
func toggle_selection() -> void:
	if is_selected:
		force_deselect()
	else:
		var cm = get_card_manager()  # 获取管理器
		if cm and cm.select_card(self):  # 调用管理器的函数
			is_selected = true
			card_current_state = CustomCardState.SELECTED
			card_original_position = position
			z_index = 100

			# 卡牌升起动画 (替换 toggle_selection 里的 tw 动画部分)
			var tw = create_tween().set_parallel(true).set_trans(Tween.TRANS_QUAD)
			# 强制构造一个完整的 Vector2，绝对不会报类型错！
			tw.tween_property(self, "position", Vector2(card_original_position.x, card_original_position.y + float_height), 0.2)
			tw.tween_property(self, "scale", card_original_scale * 1.1, 0.2)

			# 显示时间占位图片并调整卡牌透明度（方案A）
			show_timeline_shape()
			set_card_transparency(0.5)  # 半透明

			# 更新地块的条件效果（选中卡牌时）
			_update_map_conditional_effects()


## 查找玩家手牌容器
func _find_player_hand() -> Node:
	var scene_root = get_tree().current_scene
	if not scene_root:
		return null

	# 方法1：直接通过名称查找（project.gd中手牌被命名为"PlayerHand"）
	var hand = scene_root.get_node_or_null("PlayerHand")
	if hand:
		GameLogger.info("找到手牌容器: PlayerHand（通过名称）", "CustomCard")
		return hand

	# 方法2：通过相对路径查找（手牌可能在当前节点的父节点下）
	hand = get_node_or_null("../PlayerHand")
	if hand:
		GameLogger.info("找到手牌容器: PlayerHand（通过相对路径）", "CustomCard")
		return hand

	# 方法3：搜索整个场景树中的Hand类型节点
	var hand_nodes = []
	_find_hand_nodes_recursive(scene_root, hand_nodes)
	if not hand_nodes.is_empty():
		GameLogger.info("找到手牌容器: Hand类型节点（共%d个）" % hand_nodes.size(), "CustomCard")
		return hand_nodes[0]

	# 方法4：通过组名查找
	var hands = get_tree().get_nodes_in_group("player_hand")
	if not hands.is_empty():
		GameLogger.info("找到手牌容器: player_hand组（共%d个）" % hands.size(), "CustomCard")
		return hands[0]

	# 方法5：查找包含"hand"的节点（大小写不敏感）
	for child in scene_root.get_children():
		if "hand" in child.name.to_lower():
			GameLogger.info("找到手牌容器: 名称包含'hand'", "CustomCard")
			return child

	GameLogger.warning("未找到任何手牌容器", "CustomCard")
	return null


## 递归查找Hand类型节点
func _find_hand_nodes_recursive(node: Node, result: Array) -> void:
	# 检查是否是Hand类型
	if node.is_class("Hand"):
		result.append(node)
		return

	# 递归搜索子节点
	for child in node.get_children():
		_find_hand_nodes_recursive(child, result)


func force_deselect() -> void:
	is_selected = false
	card_current_state = CustomCardState.IDLE
	var cm = get_card_manager()  # 获取管理器
	if cm:
		cm.deselect_card()  # 通知管理器取消选中

	# 隐藏时间占位图片并恢复卡牌透明度
	hide_timeline_shape()
	set_card_transparency(1.0)  # 恢复完全不透明
	material = original_material
	if front_face_texture:
		front_face_texture.material = original_material
	
	z_index = original_z_index
	var tw = create_tween().set_parallel(true).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tw.tween_property(self, "position", card_original_position, 0.3)
	tw.tween_property(self, "scale", card_original_scale, 0.3)
	tw.tween_property(self, "rotation", 0.0, 0.3)

	# 更新地块的条件效果（取消选中卡牌时）
	_update_map_conditional_effects()

	# 确保卡牌返回手牌容器
	var hand = _find_player_hand()
	if hand:
		GameLogger.info("找到手牌容器: %s (类型: %s)" % [hand.name, hand.get_class()], "CustomCard")

		# 检查手牌容器是否有add_card方法
		if hand.has_method("add_card"):
			# 记录卡牌当前状态
			GameLogger.info("卡牌当前父节点: %s" % (get_parent().name if get_parent() else "null"), "CustomCard")
			GameLogger.info("尝试调用hand.add_card()...", "CustomCard")

			# 调用card-framework的add_card方法
			hand.add_card(self)
			GameLogger.info("卡牌已通过add_card()返回手牌容器", "CustomCard")
		else:
			GameLogger.warning("手牌容器没有add_card方法，尝试直接重新父级化", "CustomCard")
			# 备用方案：直接将卡牌添加到手牌容器
			var current_parent = get_parent()
			if current_parent != hand:
				if current_parent:
					current_parent.remove_child(self)
				hand.add_child(self)
				GameLogger.info("卡牌已通过重新父级化返回手牌容器", "CustomCard")
			else:
				GameLogger.info("卡牌已经在手牌容器中", "CustomCard")
	else:
		GameLogger.warning("无法找到手牌容器", "CustomCard")


## 更新地图地块的条件效果
func _update_map_conditional_effects() -> void:
	# 直接定位HexMap节点
	var hex_map = get_tree().root.get_node_or_null("/root/project/map/HexMap")
	if hex_map and hex_map.has_method("update_all_stack_conditional_effects"):
		hex_map.update_all_stack_conditional_effects()


func setup_card_data() -> void:
	if card_info.is_empty(): return
	raw_description = card_info.get("效果", "")

	if card_info.has("能量"): base_stats["能量"] = card_info["能量"]
	if card_info.has("ATK"): base_stats["ATK"] = card_info["ATK"]

	# ==========================================
	# ★ 重构：处理 JSON 中的 shape 数据，支持多种格式
	# ==========================================
	# 支持的格式:
	# 1. 二进制矩阵格式: "010,111,010" 或 "010\n111\n010" (敌人意图格式)
	# 2. 字符串格式: "1x1", "2x2" (传统格式)
	# 3. 数组格式: [[0,0], [1,0]] (坐标数组)
	# ==========================================
	if card_info.has("shape"):
		var shape_data = card_info["shape"]
		var parsed_shape: Array[Vector2i] = []
		
		# 判断 shape 数据类型
		if shape_data is String:
			var shape_str = shape_data as String
			GameLogger.debug("卡牌shape为字符串格式: " + shape_str, "CustomCard")
			
			# ★ 首先尝试解析为二进制矩阵格式 (如果字符串包含'0'或'1'，并且看起来像矩阵)
			if shape_str.contains("0") or shape_str.contains("1"):
				# 尝试解析矩阵
				if _parse_matrix_shape(shape_str):
					GameLogger.debug("成功解析为矩阵格式: " + shape_str + " -> " + timeline_shape_key, "CustomCard")
					parsed_shape = timeline_shape_coords
				else:
					# 矩阵解析失败，尝试传统"1x1"格式
					GameLogger.debug("矩阵解析失败，尝试传统格式: " + shape_str, "CustomCard")
					_parse_traditional_shape_format(shape_str)
					parsed_shape = timeline_shape_coords
			else:
				# 不包含0或1，直接尝试传统格式
				_parse_traditional_shape_format(shape_str)
				parsed_shape = timeline_shape_coords
		else:
			# 数组格式，保持原有逻辑
			GameLogger.debug("卡牌shape为数组格式: " + str(shape_data), "CustomCard")
			for point in shape_data:
				# 强转为 int，组装成 Godot 坐标
				parsed_shape.append(Vector2i(int(point[0]), int(point[1])))
			
			# 计算数组格式对应的形状键名和尺寸
			_calculate_shape_info_from_coords(parsed_shape)
		
		card_info["shape"] = parsed_shape
	else:
		# 如果没填 shape，默认给它一个 1x1 的单格
		card_info["shape"] = [Vector2i(0, 0)]
		timeline_shape_key = "1x1"
		timeline_shape_size = Vector2i(1, 1)
		timeline_shape_coords = [Vector2i(0, 0)]
	
	# 显示时间占位图片（如果启用手牌显示）
	if show_time_block_in_hand:
		show_timeline_shape()


# 2. ★ 核心：动态文本渲染器
# ==========================================
# ★ 修改：不再向 UI 渲染，而是返回解析好的 BBCode 字符串

## 解析矩阵形状（二进制字符串格式）
## 格式: 逗号分隔 "010,111,010"、换行分隔 "010\n111\n010" 或 空格分隔 "010 111 010"
## 占用字符: 1
## 空字符: 0 或其他
func _parse_matrix_shape(matrix_str: String) -> bool:
	var rows: Array[String] = []
	var trimmed = matrix_str.strip_edges()
	
	if trimmed.contains(","):
		# 逗号分隔格式
		for row in trimmed.split(","):
			var clean = row.strip_edges()
			if not clean.is_empty():
				rows.append(clean)
	elif trimmed.contains("\n"):
		# 换行分隔格式
		for line in trimmed.split("\n"):
			var clean = line.strip_edges()
			if not clean.is_empty():
				rows.append(clean)
	else:
		# 空格分隔格式 (支持用户输入的 "010 111 010" 格式)
		# 注意：需要检查是否包含空格，并且字符串看起来像矩阵（包含0或1）
		if trimmed.contains(" ") and (trimmed.contains("0") or trimmed.contains("1")):
			for row in trimmed.split(" "):
				var clean = row.strip_edges()
				if not clean.is_empty():
					rows.append(clean)
		else:
			# 可能是单行格式，如 "11" 或 "1"
			rows.append(trimmed)
	
	if rows.is_empty():
		return false
	
	# 检查行长度一致
	var width = rows[0].length()
	for i in range(1, rows.size()):
		if rows[i].length() != width:
			return false
	
	# 解析坐标
	var parsed_shape: Array[Vector2i] = []
	for y in range(rows.size()):
		var row = rows[y]
		for x in range(row.length()):
			if row[x] == "1":
				parsed_shape.append(Vector2i(x, y))
	
	if parsed_shape.is_empty():
		return false
	
	# 计算包围盒（Bounding Box）
	var min_x = 1000
	var min_y = 1000
	var max_x = -1000
	var max_y = -1000
	
	for coord in parsed_shape:
		min_x = min(min_x, coord.x)
		max_x = max(max_x, coord.x)
		min_y = min(min_y, coord.y)
		max_y = max(max_y, coord.y)
	
	# 包围盒尺寸计算：宽度 = 最大X - 最小X + 1，高度 = 最大Y - 最小Y + 1
	timeline_shape_size = Vector2i(max_x - min_x + 1, max_y - min_y + 1)
	timeline_shape_coords = parsed_shape
	
	# 生成形状键名
	timeline_shape_key = str(timeline_shape_size.x) + "x" + str(timeline_shape_size.y)
	
	# 尺寸验证：确保形状尺寸适合时间轴网格
	# 时间轴网格高度为3（0-2），形状高度不应超过3
	if timeline_shape_size.y > 3:
		GameLogger.warning("矩阵形状高度 %d 超出时间轴网格最大高度3，已限制为3" % timeline_shape_size.y, "CustomCard")
		timeline_shape_size.y = 3
	
	# 形状宽度不应超过时间轴网格宽度12
	if timeline_shape_size.x > 12:
		GameLogger.warning("矩阵形状宽度 %d 超出时间轴网格最大宽度12，已限制为12" % timeline_shape_size.x, "CustomCard")
		timeline_shape_size.x = 12
	
	return true

## 解析传统形状格式 (如 "1x1", "2x2")
func _parse_traditional_shape_format(shape_str: String) -> void:
	timeline_shape_key = shape_str
	GameLogger.debug("尝试解析传统形状格式: " + shape_str, "CustomCard")
	
	# 解析尺寸，如 "1x2" -> width=1, height=2
	var parts = shape_str.split("x")
	if parts.size() >= 2:
		var width = int(parts[0])
		var height = int(parts[1])
		
		# 形状验证：确保形状尺寸适合时间轴网格
		# 时间轴网格高度为3（0-2），形状高度不应超过3
		if height > 3:
			GameLogger.warning("卡牌形状高度 %d 超出时间轴网格最大高度3，已限制为3。形状键: %s" % [height, shape_str], "CustomCard")
			height = 3
		
		# 形状宽度不应超过时间轴网格宽度12
		if width > 12:
			GameLogger.warning("卡牌形状宽度 %d 超出时间轴网格最大宽度12，已限制为12。形状键: %s" % [width, shape_str], "CustomCard")
			width = 12
		
		timeline_shape_size = Vector2i(width, height)
		
		# 生成坐标数组，例如 1x2 生成 [Vector2i(0,0), Vector2i(1,0)]
		var parsed_shape: Array[Vector2i] = []
		for y in range(height):
			for x in range(width):
				parsed_shape.append(Vector2i(x, y))
		
		timeline_shape_coords = parsed_shape
		GameLogger.debug("生成传统shape坐标: " + str(parsed_shape) + ", 尺寸: " + str(timeline_shape_size), "CustomCard")
	else:
		GameLogger.warning("无法解析传统形状格式: " + shape_str, "CustomCard")
		timeline_shape_key = "1x1"
		timeline_shape_size = Vector2i(1, 1)
		timeline_shape_coords = [Vector2i(0, 0)]

## 从坐标数组计算形状键名和尺寸
func _calculate_shape_info_from_coords(coords: Array[Vector2i]) -> void:
	if coords.is_empty():
		timeline_shape_key = "1x1"
		timeline_shape_size = Vector2i(1, 1)
		timeline_shape_coords = [Vector2i(0, 0)]
		return
	
	# 计算坐标范围
	var min_x = 999
	var min_y = 999
	var max_x = -999
	var max_y = -999
	
	for coord in coords:
		min_x = min(min_x, coord.x)
		min_y = min(min_y, coord.y)
		max_x = max(max_x, coord.x)
		max_y = max(max_y, coord.y)
	
	# 计算尺寸
	var width = max_x - min_x + 1
	var height = max_y - min_y + 1
	
	# 形状验证：确保形状尺寸适合时间轴网格
	# 时间轴网格高度为3（0-2），形状高度不应超过3
	if height > 3:
		GameLogger.warning("从坐标计算的形状高度 %d 超出时间轴网格最大高度3，已限制为3。坐标: %s" % [height, coords], "CustomCard")
		height = 3
	
	# 形状宽度不应超过时间轴网格宽度12
	if width > 12:
		GameLogger.warning("从坐标计算的形状宽度 %d 超出时间轴网格最大宽度12，已限制为12。坐标: %s" % [width, coords], "CustomCard")
		width = 12
	
	timeline_shape_size = Vector2i(width, height)
	timeline_shape_key = str(width) + "x" + str(height)
	timeline_shape_coords = coords
	
	GameLogger.debug("从坐标计算形状: 尺寸=" + str(timeline_shape_size) + ", 键名=" + timeline_shape_key, "CustomCard")


## 加载时间占位图片
func _load_timeline_shape_texture() -> void:
	if timeline_shape_key.is_empty():
		GameLogger.warning("timeline_shape_key为空，无法加载时间占位图片", "CustomCard")
		return
	
	if timeline_shape_key in TIMELINE_SHAPE_TEXTURES:
		var texture = TIMELINE_SHAPE_TEXTURES[timeline_shape_key]
		if time_block_sprite:
			# 调试：记录节点类型
			var node_class = time_block_sprite.get_class()
			GameLogger.debug("TimeBlock节点类型: " + node_class + ", 路径: " + str(time_block_sprite.get_path()), "CustomCard")
			
			# 根据节点类型设置相应属性
			if time_block_sprite is TextureRect:
				time_block_sprite.texture = texture
				GameLogger.debug("已加载时间占位图片到TextureRect: " + timeline_shape_key, "CustomCard")
			elif time_block_sprite is ColorRect:
				# ColorRect无法显示纹理，只能显示纯色
				# 这里设置为半透明蓝色作为占位
				time_block_sprite.color = Color(0.2, 0.4, 0.8, 0.7)
				GameLogger.debug("TimeBlock是ColorRect，设置为半透明蓝色: " + timeline_shape_key, "CustomCard")
			elif time_block_sprite is Sprite2D:
				# Sprite2D支持纹理
				time_block_sprite.texture = texture
				GameLogger.debug("已加载时间占位图片到Sprite2D: " + timeline_shape_key, "CustomCard")
			else:
				# 如果是普通Control节点，尝试动态添加TextureRect子节点
				GameLogger.debug("TimeBlock是" + node_class + "类型，尝试动态处理", "CustomCard")
				
				# 检查节点是否支持texture属性
				if time_block_sprite.has_method("set_texture") or "texture" in time_block_sprite:
					GameLogger.debug("节点支持texture属性，直接设置", "CustomCard")
					# 尝试安全设置texture属性
					if time_block_sprite.set_texture is Callable:
						time_block_sprite.set_texture(texture)
					elif "texture" in time_block_sprite:
						time_block_sprite.texture = texture
				else:
					# 检查是否已经有TextureRect子节点
					var texture_child = null
					for child in time_block_sprite.get_children():
						if child is TextureRect:
							texture_child = child
							break
					
					if not texture_child:
						# 创建TextureRect子节点
						texture_child = TextureRect.new()
						texture_child.name = "TimelineShapeTexture"
						texture_child.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
						texture_child.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
						texture_child.size = Vector2(timeline_shape_size.x * time_block_base_size, timeline_shape_size.y * time_block_base_size)  # 根据形状尺寸和基础大小设置
						time_block_sprite.add_child(texture_child)
					
					texture_child.texture = texture
					GameLogger.debug("已创建TextureRect子节点显示时间占位图片: " + timeline_shape_key, "CustomCard")
		else:
			GameLogger.warning("time_block_sprite节点不存在", "CustomCard")
	else:
		GameLogger.warning("未找到时间占位图片: " + timeline_shape_key, "CustomCard")


## 显示时间占位图片
func show_timeline_shape() -> void:
	if not time_block_sprite:
		GameLogger.warning("time_block_sprite节点不存在，无法显示时间占位", "CustomCard")
		return
	
	# 确保图片已加载
	_load_timeline_shape_texture()
	
	# 设置位置：卡牌上半部分，居中，应用偏移量
	var card_size = size if has_method("get_size") else Vector2(100, 140)
	var base_position = Vector2(card_size.x / 2, card_size.y / 6)  # 卡牌上半部分中央
	time_block_sprite.position = base_position + time_block_offset
	
	# 设置大小：根据形状尺寸和基础格子大小，应用缩放因子
	# 使用固定框大小，确保所有时间占位图片有统一的视觉大小
	var max_shape_dim = max(timeline_shape_size.x, timeline_shape_size.y)
	var base_block_size = time_block_base_size * time_block_scale_factor
	var block_width = max_shape_dim * base_block_size
	var block_height = max_shape_dim * base_block_size
	
	# 根据节点类型设置大小
	if time_block_sprite is TextureRect:
		time_block_sprite.custom_minimum_size = Vector2(block_width, block_height)
		time_block_sprite.size = Vector2(block_width, block_height)
	elif time_block_sprite is ColorRect:
		time_block_sprite.size = Vector2(block_width, block_height)
	elif time_block_sprite is Sprite2D:
		# Sprite2D大小由纹理和缩放决定，设置缩放
		# 假设时间占位图片的基础纹理大小为time_block_base_size x time_block_base_size
		var texture_size = Vector2(time_block_base_size, time_block_base_size)
		var scale_x = block_width / texture_size.x
		var scale_y = block_height / texture_size.y
		time_block_sprite.scale = Vector2(scale_x, scale_y)
	else:
		# 其他Control节点，尝试设置大小和位置属性
		GameLogger.debug("处理通用Control节点: " + str(time_block_sprite.get_class()), "CustomCard")
		
		# 尝试设置大小
		if "size" in time_block_sprite:
			time_block_sprite.size = Vector2(block_width, block_height)
		if "custom_minimum_size" in time_block_sprite:
			time_block_sprite.custom_minimum_size = Vector2(block_width, block_height)
		
		# 对于Control节点，设置锚点为左上角，确保位置正确
		if "anchor_left" in time_block_sprite:
			time_block_sprite.anchor_left = 0.0
			time_block_sprite.anchor_top = 0.0
			time_block_sprite.anchor_right = 0.0
			time_block_sprite.anchor_bottom = 0.0
		
		# 设置位置偏移模式为绝对位置
		if "offset_left" in time_block_sprite:
			time_block_sprite.offset_left = time_block_sprite.position.x
			time_block_sprite.offset_top = time_block_sprite.position.y
			time_block_sprite.offset_right = time_block_sprite.position.x + block_width
			time_block_sprite.offset_bottom = time_block_sprite.position.y + block_height
	
	# 设置层级比卡牌高一级
	time_block_sprite.z_index = z_index + 1
	
	# 显示
	time_block_sprite.visible = true
	
	# 应用卡牌当前shader效果（如果有）- 仅对支持material的节点类型
	if material:
		# 检查节点是否支持material属性
		if time_block_sprite is TextureRect or time_block_sprite is ColorRect or time_block_sprite is Sprite2D:
			if time_block_sprite.material:
				time_block_sprite.material = material.duplicate()
		elif "material" in time_block_sprite:
			# 对于其他可能有material属性的节点类型
			time_block_sprite.material = material.duplicate()
	
	GameLogger.debug("显示时间占位图片，位置: " + str(time_block_sprite.position) + ", 大小: " + str(Vector2(block_width, block_height)), "CustomCard")


## 隐藏时间占位图片
func hide_timeline_shape() -> void:
	if time_block_sprite:
		time_block_sprite.visible = false
		GameLogger.debug("隐藏时间占位图片", "CustomCard")


## 获取时间占位图片位置（供DragShapeController使用）
func get_timeline_shape_position() -> Vector2:
	if time_block_sprite and time_block_sprite.visible:
		return time_block_sprite.position
	else:
		# 如果时间占位图片不可见，返回卡牌中心偏上的位置
		var card_size = size if has_method("get_size") else Vector2(100, 140)
		return Vector2(card_size.x / 2, card_size.y / 4)


## 获取时间占位图片全局位置（考虑旋转）
func get_timeline_shape_global_position() -> Vector2:
	if time_block_sprite and time_block_sprite.visible:
		return time_block_sprite.global_position
	else:
		# 如果时间占位图片不可见，返回卡牌中心偏上的全局位置
		var card_size = size if has_method("get_size") else Vector2(100, 140)
		var local_pos = Vector2(card_size.x / 2, card_size.y / 4)
		return global_position + local_pos


## 设置时间占位图片偏移量（供DragShapeController使用）
func set_timeline_shape_offset(offset: Vector2) -> void:
	# 这个方法可以用于存储额外的偏移量，目前不需要实际存储
	# 但为了接口兼容性保留
	pass


## 调整卡牌透明度（方案A）
func set_card_transparency(alpha: float) -> void:
	if has_method("set_modulate"):
		var current_color = modulate
		modulate = Color(current_color.r, current_color.g, current_color.b, alpha)
		GameLogger.debug("设置卡牌透明度: " + str(alpha), "CustomCard")


# ==========================================
func get_parsed_description() -> String:
	var final_text = raw_description
	active_keywords.clear()

	# 1. 替换动态变量并变色 (如果有的话)
	if "current_stats" in self:
		for stat_key in current_stats.keys():
			var placeholder = "{" + stat_key + "}"
			if placeholder in final_text:
				var base_val = base_stats[stat_key]
				var cur_val = current_stats[stat_key]
				var val_str = str(cur_val)
				#数值强化变绿，削弱变红
				if cur_val > base_val:
					val_str = "[color=#55ff55]" + val_str + "[/color]"
				elif cur_val < base_val:
					val_str = "[color=#ff5555]" + val_str + "[/color]"
				final_text = final_text.replace(placeholder, val_str)

	# 2. 自动雷达：识别关键词并高亮
	for kw in GlobalDB.KEYWORDS.keys():
		if kw in final_text:
			if not active_keywords.has(kw):
				active_keywords.append(kw)  # 记录下来供右侧注释框生成
			# 兼容插件的高级包裹特效
			if GlobalDB.KEYWORDS[kw].has("bbcode_wrap"):
				var format_string = GlobalDB.KEYWORDS[kw]["bbcode_wrap"]
				final_text = final_text.replace(kw, format_string % kw)
			else:
				var kw_color = GlobalDB.KEYWORDS[kw]["color"]
				var colored_kw = "[color=" + kw_color + "]" + kw + "[/color]"
				final_text = final_text.replace(kw, colored_kw)

	# 3. 替换文本图标
	for icon_tag in GlobalDB.ICONS.keys():
		if icon_tag in final_text:
			var img_path = GlobalDB.ICONS[icon_tag]
			var img_bbcode = "[img=20]" + img_path + "[/img]"
			final_text = final_text.replace(icon_tag, img_bbcode)

	return final_text


# 3. 外部调用的属性修改接口 (供主界面的法术/Buff调用)
func apply_stat_modifier(stat_name: String, amount: float, is_multiplier: bool = false) -> void:
	if not current_stats.has(stat_name): return

	if is_multiplier:
		current_stats[stat_name] = int(current_stats[stat_name] * amount)  # 比如攻击翻倍
	else:
		current_stats[stat_name] = int(current_stats[stat_name] + amount)  # 比如攻击 +2

	# 可选：加个小缩放动画让卡牌弹一下，反馈感极佳
	var tw = create_tween()
	tw.tween_property(self, "scale", Vector2(1.1, 1.1), 0.1)
	tw.tween_property(self, "scale", Vector2.ONE, 0.1)
	# ★ 核心改动：由于数值发生了变化，重新请求主界面刷新 Tooltip 显示！
	if is_selected or is_pressed:
		_request_tooltip(true)


# 拦截并重写状态进入逻辑
func _enter_state(state: DraggableState, from_state: DraggableState) -> void:
	super._enter_state(state, from_state)  # 先让底层插件处理它的全局计数器

	# ==========================================
	# ★ 核心冲突修复：如果卡牌处于“选中悬浮”状态，
	# 坚决拦截底层框架的视觉重置！让 toggle_selection 接管全场。
	# ==========================================
	if is_selected:
		return

	if tween and tween.is_valid():
		tween.kill()
	tween = create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

	# ==========================================
	# ★ 致命报错修复：安全转换缩放值
	# Godot 4 严禁使用 float 去改变 Vector2 的 scale
	# ==========================================
	var safe_hover_scale: Vector2
	if typeof(hover_scale) == TYPE_FLOAT or typeof(hover_scale) == TYPE_INT:
		safe_hover_scale = Vector2(hover_scale, hover_scale)
	else:
		#safe_hover_scale = hover_scale # 如果你原本声明的就是 Vector2，则直接使用
		pass
	match state:
		DraggableState.HOVERING:
			z_index = 100
			# 绝对安全的类型匹配动画
			tween.tween_property(self, "scale", safe_hover_scale, 0.1)
			_set_shader(true)
			_request_tooltip(true)
			is_pressed = false

		DraggableState.HOLDING:
			z_index = 101
			# 虽然由于拦截了点击，这里基本不会触发了，但依然保持规范
			tween.tween_property(self, "scale", safe_hover_scale * 1.05, 0.1)
			_set_shader(true)
			_request_tooltip(false)
			is_pressed = true

		_:  # 回归普通闲置状态
			force_reset_visuals()


# 独立表现处理函数
func _set_shader(active: bool) -> void:
	if material is ShaderMaterial:
		material.set_shader_parameter("is_hovered", active)
	elif front_face_texture and front_face_texture.material is ShaderMaterial:
		front_face_texture.material.set_shader_parameter("is_hovered", active)


func _request_tooltip(should_show: bool) -> void:
	var main = get_tree().get_first_node_in_group("MainBoard")
	if main and main.has_method("show_tooltip"):
		if should_show:
			main.show_tooltip(self)
		else:
			main.hide_tooltip(self)  # ★ 加上 self


# 兜底恢复接口
func force_reset_visuals() -> void:
	if tween and tween.is_valid(): tween.kill()
	tween = create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

	tween.tween_property(self, "scale", card_original_scale, 0.1)
	_set_shader(false)
	_request_tooltip(false)
	is_pressed = false
	material = original_material
	if front_face_texture:
		front_face_texture.material = original_material


func _on_gui_input(event: InputEvent):
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			toggle_selection()
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			# 右键永远用于取消选中
			if is_selected:
				force_deselect()

	# ★ 核心：拦截事件，防止框架的原生拖拽代码执行
	get_viewport().set_input_as_handled()


# ==========================================
# ★ 小丑牌核心：实时鼠标向量倾斜追踪
# ==========================================
func _process(delta: float):
	if not is_selected: return

	# 获取鼠标相对于卡牌中心的局部坐标
	var center = size / 2.0
	var mouse_local = get_local_mouse_position() - center

	# 将距离限制在一定范围内，防止鼠标离得太远导致卡牌翻转过度
	var clamped_offset = mouse_local.clamp(Vector2(-200, -200), Vector2(200, 200))

	# 根据鼠标位置计算目标旋转角度 (鼠标在右，牌向右倾斜)
	var target_rot = clamped_offset.x * (tilt_intensity * 0.01)

	# 丝滑插值过度
	rotation = lerp(rotation, target_rot, delta * 12.0)

	# 立体视差效果：阴影向鼠标反方向移动
	if shadow:
		var target_shadow_pos = Vector2(10, 20) - (clamped_offset * 0.05)
		shadow.position = lerp(shadow.position, target_shadow_pos, delta * 10.0)


# ==========================================
# ★ 重构：打出卡牌不再直接生效，而是移交时间轴排程
# ==========================================
func play_card(target_hex: Area2D):
	# 1. 拦截底层框架：通知框架当前卡牌被放下了，但先不要销毁它
	is_selected = false
	var cm = get_card_manager()
	if cm:
		cm.deselect_card()

	# 2. 寻找全局的拖拽形状控制器（现在在 TimelineSystem 节点下）
	# 尝试多种查找方式，适应不同节点层级
	var drag_controller = null

	# 方案1：从场景根节点查找（最可靠）
	drag_controller = get_tree().root.get_node_or_null("project/ui/TimelineSystem/DragShapeController")
	if is_instance_valid(drag_controller):
		GameLogger.info("✅ 通过方案1找到 DragShapeController", "CustomCard")

	# 方案2：从当前场景查找
	if not is_instance_valid(drag_controller):
		drag_controller = get_tree().current_scene.get_node_or_null("ui/TimelineSystem/DragShapeController")
		if is_instance_valid(drag_controller):
			GameLogger.info("✅ 通过方案2找到 DragShapeController", "CustomCard")

	# 方案3：尝试查找 TimelineSystem 节点下的控制器
	if not is_instance_valid(drag_controller):
		var timeline_system = get_tree().root.find_child("TimelineSystem", true, false)
		if timeline_system:
			drag_controller = timeline_system.find_child("DragShapeController", true, false)
			if is_instance_valid(drag_controller):
				GameLogger.info("✅ 通过方案3找到 DragShapeController", "CustomCard")

	# 方案4：最后尝试相对路径
	if not is_instance_valid(drag_controller):
		drag_controller = get_tree().current_scene.get_node_or_null("TimelineSystem/DragShapeController")
		if is_instance_valid(drag_controller):
			GameLogger.info("✅ 通过方案4找到 DragShapeController", "CustomCard")

	if drag_controller and drag_controller.has_method("start_dragging"):
		# 移交控制权：将卡牌自身和目标地块传给控制器，开启时间轴排版模式！
		GameLogger.info("🃏 卡牌打出！移交 DragShapeController 变形处理...", "CustomCard")
		drag_controller.start_dragging(self, target_hex)
	else:
		# 兜底：如果没找到控制器，直接执行效果（用于不带时间轴的普通测试）
		GameLogger.warning("❌ 未找到 DragShapeController，尝试的路径均失败！", "CustomCard")
		GameLogger.warning("场景根节点路径: project/ui/TimelineSystem/DragShapeController", "CustomCard")
		GameLogger.warning("当前场景路径: ui/TimelineSystem/DragShapeController", "CustomCard")
		push_warning("未找到 DragShapeController，直接生效！")
		apply_effect_immediate(target_hex)


# (仅作兜底或无时间轴卡牌使用)
func apply_effect_immediate(target_hex: Area2D):
	GameLogger.info("对地块 %s 直接释放了效果！" % target_hex.position, "CustomCard")
	# ... 直接结算的逻辑 ...
	queue_free()
