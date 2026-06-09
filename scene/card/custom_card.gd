# 原文件名: custom_card(卡牌模板).gd
# 功能: 卡牌模板
class_name CustomCard
extends Card  # 直接继承插件自带的 Card 类，白嫖它所有底层功能！

const TimelineClearEffectUtil = preload("res://scene/in_scene/timeline/TimelineClearEffect.gd")
const CustomCardTimelineShapeParserScript = preload("res://scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd")

# ================= 我们的视觉变量 =================
var tween: Tween
var _timeline_shape_parser = null


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false


func _get_timeline_shape_parser():
	if _timeline_shape_parser == null:
		_timeline_shape_parser = CustomCardTimelineShapeParserScript.new()
	return _timeline_shape_parser

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
@export var float_height: float = -80.0  # 选中时向上浮动的高度
@export var tilt_intensity: float = 0.15  # 倾斜跟随鼠标的强度
@export var selected_scale_multiplier: float = 1.5  # 选中时的整体放大倍率
@export var selected_enter_duration: float = 0.2  # 进入选中状态的补间时长
@export var selected_exit_duration: float = 0.3  # 取消选中返回手牌的补间时长
@export var selected_tilt_follow_speed: float = 12.0  # 选中状态下旋转跟随鼠标的速度
@export var selected_shadow_follow_speed: float = 10.0  # 选中状态下阴影跟随鼠标的速度
@export var selected_shadow_base_offset: Vector2 = Vector2(10.0, 20.0)  # 选中状态下阴影基础偏移
@export var selected_shadow_parallax_ratio: float = 0.05  # 选中状态下阴影视差强度
@export var state_hover_scale_multiplier: float = 1.0  # 覆盖底层 hover_scale 的额外倍率
@export var state_holding_scale_multiplier: float = 1.05  # 拖拽状态相对 hover 的额外倍率
@export var state_hover_tween_duration: float = 0.1  # custom_card 内部状态切换的缩放时长

var raw_description: String = ""
var active_keywords: Array = []  # 记录当前牌有哪些关键词，供悬停Tooltip读取
# 动态属性存储
var base_stats: Dictionary = { }
var current_stats: Dictionary = { }
var is_selected: bool = false
@onready var shadow: TextureRect = $ShadowLayer

# ================= 时间轴放置形状数据 =================
var timeline_shape_key: String = ""  # 形状键名，如 "1x1", "2x2"
var timeline_shape_size: Vector2i = Vector2i(1, 1)  # 形状尺寸，如 1x1, 1x2, 2x2
var timeline_shape_coords: Array[Vector2i] = []  # 形状坐标数组

# ★ 新增：卡牌影响范围 (AOE) 相对坐标
var effect_range_offsets: Array[Vector2i] = [Vector2i(0, 0)] # 默认仅影响自身

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


## 进入拖拽状态
func enter_dragging_state() -> void:
	if card_current_state == CustomCardState.DRAGGING:
		return

	card_current_state = CustomCardState.DRAGGING

	# 拖拽通常从“已选中放大”的状态进入；这里不再覆盖手牌原始状态，
	# 否则右键取消时会把放大/竖直状态误当作回手牌基准。

	# 应用拖拽视觉效果（由DragShapeController设置材质）
	# 这里只更新状态，材质由外部控制器设置


## 返回手牌状态
func return_to_hand() -> void:
	if card_current_state == CustomCardState.RETURNING:
		return

	card_current_state = CustomCardState.RETURNING

	_restore_normal_visuals()
	if _restore_hand_fan_layout():
		card_current_state = CustomCardState.IDLE
		return

	# 兜底：找不到 Hand 时才使用旧坐标返回，避免卡牌丢失。
	var tw = create_tween().set_parallel(true)
	tw.tween_property(self, "global_position", card_original_position, selected_exit_duration)
	tw.tween_property(self, "scale", card_original_scale, selected_exit_duration)
	tw.tween_property(self, "rotation", 0.0, selected_exit_duration)
	tw.chain().tween_callback(func(): card_current_state = CustomCardState.IDLE)


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


## 获取玩家手牌容器。回手牌必须交给 Hand 重新布局，不能只依赖卡牌保存的旧坐标。
func _get_player_hand_container() -> Hand:
	if card_container is Hand:
		return card_container as Hand
	var hand = _find_player_hand()
	return hand as Hand


## 当前是否已经有另一张卡处于选中状态；用于隔绝手牌区其它卡牌点击。
func _is_another_card_selected() -> bool:
	var cm = get_card_manager()
	if cm == null:
		return false
	var selected_card = cm.get("current_selected_card")
	return is_instance_valid(selected_card) and selected_card != self


## 清掉选中/拖拽的临时视觉，让 Hand 的扇形布局接管最终位置与旋转。
func _restore_normal_visuals() -> void:
	if tween and tween.is_valid():
		tween.kill()
	if move_tween and move_tween.is_valid():
		move_tween.kill()
		move_tween = null
	if hover_tween and hover_tween.is_valid():
		hover_tween.kill()
		hover_tween = null

	current_state = DraggableState.IDLE
	is_moving_to_destination = false
	is_returning_to_original = false
	is_selected = false
	is_pressed = false
	mouse_filter = Control.MOUSE_FILTER_STOP
	var current_global_pos = global_position
	set_as_top_level(false)
	global_position = current_global_pos
	set_card_transparency(1.0)
	scale = card_original_scale
	material = original_material
	if front_face_texture:
		front_face_texture.material = original_material
	if shadow:
		shadow.position = selected_shadow_base_offset
	_set_shader(false)
	_request_tooltip(false)


## 将卡牌放回 Hand/Cards 节点并刷新 Hand 的扇形排列，保证取消后不是竖直堆放。
func _restore_hand_fan_layout() -> bool:
	var hand = _get_player_hand_container()
	if not is_instance_valid(hand):
		return false

	var current_global_pos = global_position
	var current_parent = get_parent()
	if is_instance_valid(hand.cards_node) and current_parent != hand.cards_node:
		if current_parent:
			current_parent.remove_child(self)
		hand.cards_node.add_child(self)
		global_position = current_global_pos

	card_container = hand
	if not hand.has_card(self):
		hand.add_card(self)
	else:
		hand.update_card_ui()
	return true

# ==========================================
# ★ 修改：使用动态获取的引用来操作选中状态
# ==========================================
func toggle_selection() -> void:
	if _is_another_card_selected():
		return

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
			if tween and tween.is_valid():
				tween.kill()
			tween = create_tween().set_parallel(true).set_trans(Tween.TRANS_QUAD)
			# 强制构造一个完整的 Vector2，绝对不会报类型错！
			tween.tween_property(self, "position", Vector2(card_original_position.x, card_original_position.y + float_height), selected_enter_duration)
			tween.tween_property(self, "scale", card_original_scale * selected_scale_multiplier, selected_enter_duration)

			set_card_transparency(1.0)

			# 更新地块的条件效果（选中卡牌时）
			_update_map_conditional_effects()

			# clear 类卡牌不需要地图目标。
			# 选中后延迟一帧直接进入时间轴放置状态，相当于“目标为空但判定合法”的专用打出路径；
			# 这样不会伪造一次地图点击，也不会触发正常目标地块 AOE 高亮。
			if TimelineClearEffectUtil.is_clear_card(self):
				call_deferred("_play_no_target_timeline_card")

func force_deselect() -> void:
	var cm = get_card_manager()  
	if cm:
		cm.deselect_card()  

	card_current_state = CustomCardState.RETURNING
	_restore_normal_visuals()
	_update_map_conditional_effects()

	if _restore_hand_fan_layout():
		card_current_state = CustomCardState.IDLE
		return

	z_index = original_z_index
	var tw = create_tween().set_parallel(true).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tw.tween_property(self, "position", card_original_position, selected_exit_duration)
	tw.tween_property(self, "scale", card_original_scale, selected_exit_duration)
	tw.tween_property(self, "rotation", 0.0, selected_exit_duration)
	tw.chain().tween_callback(func(): card_current_state = CustomCardState.IDLE)
				
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
	# ★ 时间轴放置形状解析：全自动裁剪与去重
	# ==========================================
	var raw_shape = card_info.get("shape", ["1"]) # 默认给个单格
	_normalize_and_parse_shape(raw_shape)
	# ★ 新增：解析卡牌六边形作用范围
	var raw_range = card_info.get("effect_range", 0) 
	_parse_hex_effect_range(raw_range)

# ==========================================
# ★ 核心矩阵解析与归一化方法
# ==========================================
func _normalize_and_parse_shape(shape_data: Variant) -> void:
	var parsed_shape: Dictionary = _get_timeline_shape_parser().parse(shape_data)
	timeline_shape_coords.clear()
	for coord: Vector2i in parsed_shape.get("coords", [Vector2i(0, 0)]):
		timeline_shape_coords.append(coord)
	timeline_shape_size = parsed_shape.get("size", Vector2i(1, 1))
	timeline_shape_key = parsed_shape.get("key", "1")
	
	# 【修复2】：绝对不要覆写原数据字典，保持原数据的纯洁性！
	# card_info["shape"] = timeline_shape_coords # <--- 删掉这行，大功告成！
	
# 2. ★ 核心：动态文本渲染器
# ==========================================
# ★ 修改：不再向 UI 渲染，而是返回解析好的 BBCode 字符串

## 调整卡牌透明度（方案A）
func set_card_transparency(alpha: float) -> void:
	if has_method("set_modulate"):
		var current_color = modulate
		modulate = Color(current_color.r, current_color.g, current_color.b, alpha)


# ==========================================
func get_parsed_description() -> String:
	var final_text = raw_description
	active_keywords.clear()

	# 1. 替换动态变量并变色 (如果有的话)
	if _object_has_property(self, &"current_stats"):
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
	var safe_hover_scale: Vector2 = original_scale
	var hover_scale_value: Variant = hover_scale
	if typeof(hover_scale_value) == TYPE_FLOAT or typeof(hover_scale_value) == TYPE_INT:
		safe_hover_scale = Vector2(float(hover_scale_value), float(hover_scale_value))
	elif typeof(hover_scale_value) == TYPE_VECTOR2:
		var hover_scale_vector: Vector2 = hover_scale_value
		safe_hover_scale = hover_scale_vector

	safe_hover_scale *= state_hover_scale_multiplier
	match state:
		DraggableState.HOVERING:
			z_index = 100
			# 绝对安全的类型匹配动画
			tween.tween_property(self, "scale", safe_hover_scale, state_hover_tween_duration)
			_set_shader(true)
			_request_tooltip(true)
			is_pressed = false

		DraggableState.HOLDING:
			z_index = 101
			# 虽然由于拦截了点击，这里基本不会触发了，但依然保持规范
			tween.tween_property(self, "scale", safe_hover_scale * state_holding_scale_multiplier, state_hover_tween_duration)
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

	tween.tween_property(self, "scale", card_original_scale, state_hover_tween_duration)
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
		get_viewport().set_input_as_handled()
		return
		
	if event is InputEventMouseButton and event.pressed:
		if _is_another_card_selected():
			get_viewport().set_input_as_handled()
			return

		if event.button_index == MOUSE_BUTTON_LEFT:
			toggle_selection()
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			if is_selected:
				force_deselect()

	get_viewport().set_input_as_handled()

# ==========================================
# ★ 实时检测与追踪
# ==========================================
func _process(delta: float):
	# ★ 恢复：只保留拖拽倾斜跟踪，把之前的轮询查找代码全删了
	if not is_selected: 
		return

	# 获取鼠标相对于卡牌中心的局部坐标
	var center = size / 2.0
	var mouse_local = get_local_mouse_position() - center

	# 将距离限制在一定范围内，防止鼠标离得太远导致卡牌翻转过度
	var clamped_offset = mouse_local.clamp(Vector2(-200, -200), Vector2(200, 200))

	# 根据鼠标位置计算目标旋转角度 (鼠标在右，牌向右倾斜)
	var target_rot = clamped_offset.x * (tilt_intensity * 0.01)

	# 丝滑插值过度
	rotation = lerp(rotation, target_rot, delta * selected_tilt_follow_speed)

	# 立体视差效果：阴影向鼠标反方向移动
	if shadow:
		var target_shadow_pos = selected_shadow_base_offset - (clamped_offset * selected_shadow_parallax_ratio)
		shadow.position = lerp(shadow.position, target_shadow_pos, delta * selected_shadow_follow_speed)
## 重构：打出卡牌不再直接生效，而是移交时间轴排程
func play_card(target_hex: Area2D):
	is_selected = false
	var cm = get_card_manager()
	if cm:
		cm.deselect_card()

	# 直接通过群组寻找 DragShapeController
	var drag_controller = get_tree().get_first_node_in_group("DragShapeController")

	if drag_controller and drag_controller.has_method("start_dragging"):
		drag_controller.start_dragging(self, target_hex)
	else:
		apply_effect_immediate(target_hex)


## clear 类即时卡牌专用入口。
## 它只负责把卡牌移交给 DragShapeController，真正的时间轴蓝/绿/红预览和清除逻辑
## 都集中在 TimelineClearEffect.gd 与 DragShapeController 的 clear 分支中。
func _play_no_target_timeline_card() -> void:
	if not TimelineClearEffectUtil.is_clear_card(self):
		return
	if card_current_state != CustomCardState.SELECTED:
		return

	play_card(null)

# (仅作兜底或无时间轴卡牌使用)
func apply_effect_immediate(target_hex: Area2D):
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
