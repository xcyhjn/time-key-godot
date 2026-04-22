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
	"1": preload("res://image/time_block/1x1.png"),
	"11": preload("res://image/time_block/1x2.png"),
	"11,11": preload("res://image/time_block/2x2.png")
	# 可扩展更多形状
}

# ★ 新增：卡牌影响范围 (AOE) 相对坐标
var effect_range_offsets: Array[Vector2i] = [Vector2i(0, 0)] # 默认仅影响自身

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

## 统一获取主面板 (MainBoard) 的快捷方法
func _get_main_board() -> Node:
	return get_tree().get_first_node_in_group("MainBoard")

## 动态获取 CardManager 的函数
func get_card_manager() -> Node:
	var main = _get_main_board()
	if main and main.get("manager_instance"):
		return main.manager_instance
	return null

## 查找玩家手牌容器
func _find_player_hand() -> Node:
	var main = _get_main_board()
	if main and main.get("player_hand"):
		return main.player_hand
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
	var main = _get_main_board()
	if main:
		var hex_map = main.get_node_or_null("../../map/HexMap")
		if hex_map and hex_map.has_method("update_all_stack_conditional_effects"):
			hex_map.update_all_stack_conditional_effects()


func setup_card_data() -> void:
	if card_info.is_empty(): return
	raw_description = card_info.get("效果", "")
	# ==========================================
	# ★ 终极时间占位解析引擎：全自动裁剪与去重
	# ==========================================
	var raw_shape = card_info.get("shape", ["1"]) # 默认给个单格
	_normalize_and_parse_shape(raw_shape)
	# ★ 新增：解析卡牌六边形作用范围
	var raw_range = card_info.get("effect_range", 0) 
	_parse_hex_effect_range(raw_range)
	# 显示时间占位图片（如果启用手牌显示）
	if show_time_block_in_hand:
		show_timeline_shape()

# ==========================================
# ★ 核心矩阵解析与归一化方法
# ==========================================
func _normalize_and_parse_shape(shape_data: Variant) -> void:
	var temp_coords: Array[Vector2i] = []
	
	# 【修复1】：判断是否已经是解析好的坐标数组（防止引用污染引发二次解析爆炸）
	var is_already_parsed = true
	if typeof(shape_data) == TYPE_ARRAY and not shape_data.is_empty():
		for item in shape_data:
			if typeof(item) != TYPE_VECTOR2I:
				is_already_parsed = false
				break
	else:
		is_already_parsed = false

	if is_already_parsed:
		# 如果已经被前一张牌解析过了，直接拷贝拿来用！
		temp_coords = shape_data.duplicate()
	else:
		# 正常读取字符串进行解析
		var raw_rows: Array[String] = []
		if typeof(shape_data) == TYPE_ARRAY:
			for row in shape_data:
				raw_rows.append(str(row))
		elif typeof(shape_data) == TYPE_STRING:
			var s = shape_data as String
			s = s.replace("\n", ",").replace(" ", ",")
			for row in s.split(",", false):
				raw_rows.append(row.strip_edges())
		
		# 提取所有 "1" 的绝对坐标
		for y in range(raw_rows.size()):
			var row_str = raw_rows[y]
			for x in range(row_str.length()):
				if row_str[x] == "1":
					temp_coords.append(Vector2i(x, y))
	
	# 兜底防错：如果全填了0或空，强行给个 (0,0)
	if temp_coords.is_empty():
		temp_coords.append(Vector2i(0, 0))
		
	# 寻找边界框 (Bounding Box)，用于裁切四周多余的 "0"
	var min_x = 9999
	var min_y = 9999
	var max_x = -9999
	var max_y = -9999
	
	for c in temp_coords:
		if c.x < min_x: min_x = c.x
		if c.x > max_x: max_x = c.x
		if c.y < min_y: min_y = c.y
		if c.y > max_y: max_y = c.y
		
	# 平移坐标 (归一化)，使形状紧贴左上角 (0,0)
	timeline_shape_coords.clear()
	for c in temp_coords:
		timeline_shape_coords.append(Vector2i(c.x - min_x, c.y - min_y))
		
	# 计算真实占用的尺寸
	timeline_shape_size = Vector2i(max_x - min_x + 1, max_y - min_y + 1)
	
	# 生成极简标准化 Key (用于统一读取图片)
	var canonical_rows: PackedStringArray = []
	for y in range(timeline_shape_size.y):
		var row_str = ""
		for x in range(timeline_shape_size.x):
			if timeline_shape_coords.has(Vector2i(x, y)):
				row_str += "1"
			else:
				row_str += "0"
		canonical_rows.append(row_str)
	
	# 生成绝对唯一的特征码，比如 "11"
	timeline_shape_key = ",".join(canonical_rows)
	
	# 【修复2】：绝对不要覆写原数据字典，保持原数据的纯洁性！
	# card_info["shape"] = timeline_shape_coords # <--- 删掉这行，大功告成！
	
	GameLogger.debug("解析并归一化形状成功！唯一特征码: [%s], 尺寸: %s" % [timeline_shape_key, str(timeline_shape_size)], "CustomCard")
# 2. ★ 核心：动态文本渲染器
# ==========================================
# ★ 修改：不再向 UI 渲染，而是返回解析好的 BBCode 字符串

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
	# ★ 核心修复 1：拦截拖拽状态！
	# 如果卡牌正在被拖拽，彻底无视底层框架的悬浮、高亮事件
	# ==========================================
	if card_current_state == CustomCardState.DRAGGING:
		return
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
	set_card_transparency(1.0)

func _on_gui_input(event: InputEvent):
	# ★ 核心修复 2：拖拽期间禁止卡牌响应任何鼠标点击！
	if card_current_state == CustomCardState.DRAGGING:
		return
		
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			toggle_selection()
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			if is_selected:
				force_deselect()

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


## 重构：打出卡牌不再直接生效，而是移交时间轴排程
func play_card(target_hex: Area2D):
	is_selected = false
	var cm = get_card_manager()
	if cm:
		cm.deselect_card()

	# 直接通过群组寻找 DragShapeController
	var drag_controller = get_tree().get_first_node_in_group("DragShapeController")

	if drag_controller and drag_controller.has_method("start_dragging"):
		GameLogger.info("🃏 卡牌打出！移交 DragShapeController 变形处理...", "CustomCard")
		drag_controller.start_dragging(self, target_hex)
	else:
		GameLogger.warning("❌ 未找到 DragShapeController，直接生效！", "CustomCard")
		apply_effect_immediate(target_hex)

# (仅作兜底或无时间轴卡牌使用)
func apply_effect_immediate(target_hex: Area2D):
	GameLogger.info("对地块 %s 直接释放了效果！" % target_hex.position, "CustomCard")
	# ... 直接结算的逻辑 ...
	queue_free()
	
## ★ 解析六边形范围
func _parse_hex_effect_range(range_data: Variant) -> void:
	effect_range_offsets.clear()
	
	# 情况1：填的是一个整数（代表半径）。例如 1 代表自身+周围6格
	if typeof(range_data) == TYPE_INT or typeof(range_data) == TYPE_FLOAT:
		var radius = int(range_data)
		for q in range(-radius, radius + 1):
			for r in range(max(-radius, -q - radius), min(radius, -q + radius) + 1):
				effect_range_offsets.append(Vector2i(q, r))
				
	# 情况2：填的是自定义坐标偏移数组，比如 ["0,0", "1,0", "0,1"]
	elif typeof(range_data) == TYPE_ARRAY:
		for item in range_data:
			if typeof(item) == TYPE_STRING:
				var parts = item.split(",")
				if parts.size() == 2:
					effect_range_offsets.append(Vector2i(int(parts[0]), int(parts[1])))
					
	# 兜底：如果解析失败，仅作用于自身
	if effect_range_offsets.is_empty():
		effect_range_offsets.append(Vector2i(0, 0))

## 获取以指定坐标为中心的绝对影响范围
func get_absolute_effect_range(center_coord: Vector2i) -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	for offset in effect_range_offsets:
		result.append(center_coord + offset)
	return result
