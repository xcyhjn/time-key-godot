# 原文件名: reward_manager(局外收获界面).gd
# 功能: 局外收获界面管理器
extends CanvasLayer

@onready var bg_mask = $BackgroundMask
@onready var options_hbox = $OptionsHBox
@onready var btn_acquire = $OptionsHBox/BtnAcquire
@onready var btn_remove = $OptionsHBox/BtnRemove
@onready var btn_craft = $OptionsHBox/BtnCraft
@onready var btn_back = $BtnBack
@onready var btn_confirm = $BtnConfirm
@onready var draft_container = $DraftContainer
@onready var deck_selection_panel = $DeckSelectionPanel
@onready var deck_grid = $DeckSelectionPanel/DeckGrid
@onready var crafting_board = $CraftingBoard
@onready var btn_leave = $BtnLeave
var draft_card_scene = preload("res://scene/card/DraftCard.tscn") 

var deck_manager = null
var current_draft_cards: Array = []
var selected_draft_card: Control = null
# ★ 导出红色拖影的特效参数供你微调
@export_group("飞入牌库特效")
@export var trail_color: Color = Color(1.0, 0.2, 0.2, 0.8)  # 红色半透明
@export var trail_width: float = 15.0
@export var fly_duration: float = 0.6
# ★ 导出合成界面排版参数
@export_group("合成工作台 (Crafting)")
@export var slot_small_size: Vector2 = Vector2(100, 140)
@export var slot_large_size: Vector2 = Vector2(150, 210)
@export var slot_spacing_x: float = 300.0
@export var slot_spacing_y: float = 200.0
@export var line_color: Color = Color(0.8, 0.8, 0.8, 0.8)
@export var line_width: float = 4.0
# ==========================================
# ★ 局外操作核心变量与合成字典
# ==========================================
enum Mode { NONE, ACQUIRE, REMOVE, CRAFT_SLOT_1, CRAFT_SLOT_2 }
var current_mode: Mode = Mode.NONE
# 记录当前在操作的卡牌实体
var craft_source_card_1: Control = null
var craft_source_card_2: Control = null
var craft_result_id: String = ""

# ★ 合成配方字典 (配方键名按字母表排序组合，保证 A+B 和 B+A 都能识别)
# 格式: "素材1_素材2" : "合成结果"
const CRAFTING_RECIPES = {
	"1_2": "3",  # 壶 + 强金 = 白龙
}


func _ready():
	hide_all()

	# 绑定通用按钮
	btn_back.pressed.connect(_on_back_pressed)
	btn_confirm.pressed.connect(_on_confirm_pressed)

	# 绑定选项按钮
	btn_acquire.pressed.connect(_on_acquire_pressed)
	btn_remove.pressed.connect(_on_remove_pressed)
	btn_craft.pressed.connect(_on_craft_pressed)
	if btn_leave:
		btn_leave.pressed.connect(_on_leave_pressed)


# ==========================================
# 每次打开时的【完全净化重置】
# ==========================================
func open_reward_screen():
	self.show()
	bg_mask.show()

	# 1. 重置所有容器可见性
	options_hbox.show()
	draft_container.hide()
	deck_selection_panel.hide()
	crafting_board.hide()
	btn_back.hide()
	btn_confirm.hide()

	# 2. 暴力清空所有动态生成的节点（防止残留）
	for child in draft_container.get_children(): child.queue_free()
	for child in deck_grid.get_children(): child.queue_free()
	for child in crafting_board.get_children(): child.queue_free()  # 清除画线残留

	# 3. 清空所有内存引用和状态
	current_mode = Mode.NONE
	current_draft_cards.clear()
	selected_draft_card = null
	craft_source_card_1 = null
	craft_source_card_2 = null
	craft_result_id = ""

	# 4. 重置三大选项按钮状态
	btn_acquire.disabled = false
	btn_remove.disabled = false
	btn_craft.disabled = false


func hide_all():
	self.hide()
	#
	## 清除时间轴UI
	#var main = get_tree().get_first_node_in_group("MainBoard")
	#if main:
		## 调用project.gd中的时间轴清理函数
		#if main.has_method("clear_timeline_ui"):
			#main.clear_timeline_ui()
		#elif main.has_method("_clear_timeline_for_reward"):
			#main._clear_timeline_for_reward()


# ==========================================
# 进入三选一界面
# ==========================================
func _on_acquire_pressed():
	GameLogger.info("进入获取卡牌界面...", "RewardManager")
	options_hbox.hide()
	btn_back.show()
	draft_container.show()
	btn_confirm.show()
	btn_confirm.disabled = true  # 还没选牌时不能点确认

	# 清空旧数据
	for child in draft_container.get_children():
		child.queue_free()
	current_draft_cards.clear()
	selected_draft_card = null
	# ★ 核心修复 1：先让容器显示，并强制等待一帧，让 Godot 算好真实的碰撞体积！
	draft_container.show()
	await get_tree().process_frame

	# 模拟从时代卡池抽取 3 张牌 (这里用假数据代替，你需要对接你的字典)
	var pool = ["qiangjin", "hailong", "pot"]
	pool.shuffle()

	# ==========================================
	# ★ 核心修复：构建幽灵牌堆，借用 Factory 偷取数据
	# ==========================================
	var temp_pile = null
	if deck_manager and deck_manager.card_factory:
		# 实例化一个临时的 Pile 容器
		temp_pile = preload("res://addons/card-framework/pile.tscn").instantiate()
		add_child(temp_pile)
		temp_pile.visible = false  # 隐形

	for i in range(3):
		var card_id = pool[i]
		var fake_card = draft_card_scene.instantiate()

		# 注入你的字典数据（这里伪造一些数据供 Tooltip 读取）
		fake_card.card_id = card_id

		# === 偷取真实数据环节 ===
		if temp_pile:
			# 让工厂真刀真枪地造一张牌出来
			deck_manager.card_factory.create_card(card_id, temp_pile)
			# 取出这张刚造出来的真牌
			await get_tree().process_frame
			var real_card = temp_pile._held_cards[-1]

			# 1. 究极暴力提取文本 (涵盖框架存文本的所有可能路径)
			if "raw_description" in real_card and real_card.raw_description != "":
				fake_card.raw_description = real_card.raw_description
			elif "card_info" in real_card and typeof(real_card.card_info) == TYPE_DICTIONARY:
				fake_card.raw_description = real_card.card_info.get("description", "")
			if real_card.has_method("get_parsed_description"):
				fake_card.raw_description = real_card.get_parsed_description()

			# 2. 提取词条
			if "active_keywords" in real_card:
				fake_card.active_keywords = real_card.active_keywords.duplicate()

			# 2. 偷取真牌贴图 (完美对接你的 card_asset)
			if real_card.has_node("FrontFace/TextureRect"):
				fake_card.texture = real_card.get_node("FrontFace/TextureRect").texture
			elif "front_face_texture" in real_card and real_card.front_face_texture != null:
				if real_card.front_face_texture is TextureRect:
					fake_card.texture = real_card.front_face_texture.texture
				elif real_card.front_face_texture is Texture2D:
					fake_card.texture = real_card.front_face_texture

		# ==========================

		draft_container.add_child(fake_card)
		fake_card.card_clicked.connect(_on_draft_card_clicked)
		current_draft_cards.append(fake_card)

		# 等待一帧后记录它的原始排版缩放，防止动画错乱
		fake_card.call_deferred("set", "original_scale", fake_card.scale)

		# 销毁幽灵牌堆（用完了就毁尸灭迹，不占一丝内存）
	if temp_pile:
		temp_pile.queue_free()


# ==========================================
# 选中卡牌逻辑 (单选互斥)
# ==========================================
func _on_draft_card_clicked(clicked_card):
	selected_draft_card = clicked_card
	for card in current_draft_cards:
		card.set_selected(card == clicked_card)

	if current_mode == Mode.REMOVE:
		btn_confirm.disabled = false  # 删牌模式直接允许确认

	elif current_mode == Mode.CRAFT_SLOT_1 or current_mode == Mode.CRAFT_SLOT_2:
		btn_confirm.disabled = false  # 选择素材，点击确认将素材装入槽位

	btn_confirm.disabled = false  # 选了牌，可以确认了

	# 让所有牌取消选中，只高亮被点中的这张
	for card in current_draft_cards:
		if card == clicked_card:
			card.set_selected(true)
		else:
			card.set_selected(false)


func _on_remove_pressed():
	current_mode = Mode.REMOVE
	options_hbox.hide()
	btn_back.show()
	btn_confirm.show()
	btn_confirm.disabled = true
	_open_deck_selection()  # 弹出牌组供选择


func _on_craft_pressed():
	current_mode = Mode.NONE  # 等待玩家点击槽位
	options_hbox.hide()
	btn_back.show()
	btn_confirm.show()
	btn_confirm.disabled = true

	craft_source_card_1 = null
	craft_source_card_2 = null
	craft_result_id = ""
	crafting_board.show()
	crafting_board.queue_redraw()  # 触发画线与画槽逻辑


# ==========================================
# 删牌逻辑
# ==========================================
func _open_deck_selection():
	crafting_board.hide()
	deck_selection_panel.show()

	# 清空网格
	for child in deck_grid.get_children():
		child.queue_free()
	current_draft_cards.clear()
	selected_draft_card = null

	var main = get_tree().get_first_node_in_group("MainBoard")
	if not main or not is_instance_valid(main.deck_pile): return

	# 遍历真实抽牌堆，生成伪卡片映射
	var real_cards = main.deck_pile._held_cards
	for real_card in real_cards:
		var fake_card = draft_card_scene.instantiate()
		fake_card.card_id = real_card.card_info.get("name", "未知")
		fake_card.raw_description = real_card.raw_description if "raw_description" in real_card else ""
		if "active_keywords" in real_card:
			fake_card.active_keywords = real_card.active_keywords.duplicate()

		# 把真实的卡牌节点存在 meta 里，删牌时要用到！
		fake_card.set_meta("real_card_ref", real_card)
		# ==========================================
		# ★ 核心修复：抓取真实卡牌的贴图！
		# (根据你卡牌的实际结构，这里提供了几种常见的抓取方式)
		# ==========================================
		if real_card.has_node("FrontFace/TextureRect"):
			fake_card.texture = real_card.get_node("FrontFace/TextureRect").texture
		elif "front_face_texture" in real_card and real_card.front_face_texture != null:
			# 如果你用的是框架自带的 front_face_texture 变量
			if real_card.front_face_texture is TextureRect:
				fake_card.texture = real_card.front_face_texture.texture
			elif real_card.front_face_texture is Texture2D:
				fake_card.texture = real_card.front_face_texture

		deck_grid.add_child(fake_card)
		fake_card.card_clicked.connect(_on_draft_card_clicked)
		current_draft_cards.append(fake_card)
		fake_card.call_deferred("set", "original_scale", fake_card.scale)


# ==========================================
# 重写返回逻辑，确保清理
# ==========================================
func _on_back_pressed():
	# 如果处于选牌界面，清空假卡
	if draft_container.visible:
		for child in draft_container.get_children():
			child.queue_free()
		draft_container.hide()
		current_draft_cards.clear()
		selected_draft_card = null

	# 隐藏 Tooltip 防残留
	var main = get_tree().get_first_node_in_group("MainBoard")
	if main and main.has_method("hide_tooltip"):
		main.hide_tooltip()

	options_hbox.show()
	btn_back.hide()
	btn_confirm.hide()
	deck_selection_panel.hide()
	crafting_board.hide()

	# 清除隐形按钮缓存，防止重复添加
	for child in crafting_board.get_children():
		child.queue_free()

	options_hbox.show()
	btn_back.hide()
	btn_confirm.hide()
	
	# 清理合成槽位引用（如果用户返回而未确认合成）
	if is_instance_valid(craft_source_card_1):
		craft_source_card_1.queue_free()
	if is_instance_valid(craft_source_card_2):
		craft_source_card_2.queue_free()
	craft_source_card_1 = null
	craft_source_card_2 = null
	craft_result_id = ""
	
	current_mode = Mode.NONE


# ==========================================
# 确认逻辑：红色拖影飞入抽牌堆！
# ==========================================
func _on_confirm_pressed():
	if draft_container.visible and selected_draft_card != null:
		GameLogger.info("确认获取卡牌：%s" % selected_draft_card.card_id, "RewardManager")
		btn_confirm.disabled = true
		btn_back.disabled = true

		# 1. 未被选中的牌直接淡出消失
		for card in current_draft_cards:
			if card != selected_draft_card:
				var tw = create_tween()
				tw.tween_property(card, "modulate:a", 0.0, 0.3)

		# 2. 剥离选中的卡牌，使其不受 HBox 排版限制，准备自由飞翔
		var global_pos = selected_draft_card.global_position
		draft_container.remove_child(selected_draft_card)
		self.add_child(selected_draft_card)
		selected_draft_card.global_position = global_pos

		# 3. 创建极具动感的【红尾焰 Line2D 拖影】
		var trail = Line2D.new()
		trail.width = trail_width
		trail.default_color = trail_color
		trail.z_index = selected_draft_card.z_index - 1
		# 让拖影头部尖锐，尾部变细
		var curve = Curve.new()
		curve.add_point(Vector2(0, 0))
		curve.add_point(Vector2(1, 1))
		trail.width_curve = curve
		self.add_child(trail)

		# 4. 飞行 Tween 动画
		# 假设抽牌堆在屏幕左下角偏下的位置 (DeckPile 的近似位置)
		var deck_target_pos = Vector2(100, get_viewport().get_visible_rect().size.y + 100)
		var tw = create_tween().set_parallel(true).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)

		tw.tween_property(selected_draft_card, "global_position", deck_target_pos, fly_duration)
		tw.tween_property(selected_draft_card, "scale", Vector2.ZERO, fly_duration)
		tw.tween_property(selected_draft_card, "rotation", PI * 2, fly_duration)  # 炫酷旋转

		# 5. 在帧循环中实时更新红尾焰的轨迹
		var trail_timer = Timer.new()
		trail_timer.wait_time = 0.01
		trail_timer.autostart = true
		self.add_child(trail_timer)
		trail_timer.timeout.connect(func():
			trail.add_point(selected_draft_card.global_position + selected_draft_card.size / 2)
			# 保持拖影长度不超过 20 个点
			if trail.get_point_count() > 20:
				trail.remove_point(0)
		)

		# 6. 动画结束后的清理与实质数据添加
		tw.chain().tween_callback(func():
			trail_timer.queue_free()
			trail.queue_free()
			selected_draft_card.queue_free()

			# ==========================================
			# ★ 核心数据打通：让工厂真正生产这张牌进抽牌堆！
			# ==========================================
			if deck_manager != null and deck_manager.card_factory != null:
				var main = get_tree().get_first_node_in_group("MainBoard")
				if main and main.deck_pile:
					deck_manager.card_factory.create_card(selected_draft_card.card_id, main.deck_pile)
					GameLogger.info("✅ 已成功将 %s 加入抽牌堆！" % selected_draft_card.card_id, "RewardManager")

			# 恢复 UI 状态，把这个获取按钮置灰，返回主选项
			btn_acquire.disabled = true
			btn_back.disabled = false
			_on_back_pressed()
		)
	var main = get_tree().get_first_node_in_group("MainBoard")

	# === 【情况A：删除卡牌】 ===
	if current_mode == Mode.REMOVE and selected_draft_card != null:
		var real_card = selected_draft_card.get_meta("real_card_ref")
		if main and main.deck_pile and is_instance_valid(real_card):
			# 调用底层的完全删除
			main.deck_pile.remove_card(real_card)
			real_card.queue_free()
			GameLogger.info("🔥 已永久删除卡牌：%s" % selected_draft_card.card_id, "RewardManager")

		btn_remove.disabled = true  # 删过一次就变灰
		_on_back_pressed()

	# === 【情况B：将卡牌装入合成槽位】 ===
	elif (current_mode == Mode.CRAFT_SLOT_1 or current_mode == Mode.CRAFT_SLOT_2) and selected_draft_card != null:
		# 从当前选择列表中移除这张卡牌，防止在重新打开选择面板时被释放
		if selected_draft_card in current_draft_cards:
			current_draft_cards.erase(selected_draft_card)
		# 从网格中移除卡牌，但保持对象存活以供合成界面显示
		if selected_draft_card.get_parent() == deck_grid:
			deck_grid.remove_child(selected_draft_card)
		
		if current_mode == Mode.CRAFT_SLOT_1:
			craft_source_card_1 = selected_draft_card
		else:
			craft_source_card_2 = selected_draft_card

		deck_selection_panel.hide()
		crafting_board.show()

		# 检测配方运算！
		_check_crafting_recipe()

		current_mode = Mode.NONE  # 恢复为不可选状态，等待下一次点击槽位

	# === 【情况C：执行真正的合成动作】 ===
	elif current_mode == Mode.NONE and craft_result_id != "":
		GameLogger.info("✨ 锻造成功！获得：%s" % craft_result_id, "RewardManager")

		# 1. 安全检查：确保合成素材仍然有效
		if not (craft_source_card_1 != null and is_instance_valid(craft_source_card_1) and \
				craft_source_card_2 != null and is_instance_valid(craft_source_card_2)):
			GameLogger.error("合成素材无效或已被释放，无法完成合成", "RewardManager")
			_on_back_pressed()
			return
		
		# 2. 销毁作为素材的两张真实卡牌
		var r1 = craft_source_card_1.get_meta("real_card_ref")
		var r2 = craft_source_card_2.get_meta("real_card_ref")
		
		# 检查真实卡牌引用是否有效
		if not (is_instance_valid(r1) and is_instance_valid(r2)):
			GameLogger.error("真实卡牌引用无效，无法完成合成", "RewardManager")
			_on_back_pressed()
			return
			
		main.deck_pile.remove_card(r1)
		main.deck_pile.remove_card(r2)
		r1.queue_free()
		r2.queue_free()
		
		# 2. 生成新卡牌入库
		if deck_manager and deck_manager.card_factory:
			deck_manager.card_factory.create_card(craft_result_id, main.deck_pile)
		
		# 3. 清理合成槽位引用并释放DraftCard对象
		if is_instance_valid(craft_source_card_1):
			craft_source_card_1.queue_free()
		if is_instance_valid(craft_source_card_2):
			craft_source_card_2.queue_free()
		craft_source_card_1 = null
		craft_source_card_2 = null
		craft_result_id = ""


		btn_craft.disabled = true
		_on_back_pressed()
# 检测合成公式
func _check_crafting_recipe():
	# 必须两个槽位都有卡，才进行检测
	if craft_source_card_1 != null and is_instance_valid(craft_source_card_1) and \
	   craft_source_card_2 != null and is_instance_valid(craft_source_card_2):
		var id1 = craft_source_card_1.card_id
		var id2 = craft_source_card_2.card_id

		# 排序保证 A+B 和 B+A 都可以识别
		var arr = [id1, id2]
		arr.sort()
		var recipe_key = arr[0] + "_" + arr[1]

		if CRAFTING_RECIPES.has(recipe_key):
			craft_result_id = CRAFTING_RECIPES[recipe_key]
			btn_confirm.disabled = false  # 只有配方正确，才能点击确认执行合成！
			GameLogger.info("配方匹配成功！预测结果：%s" % craft_result_id, "RewardManager")
		else:
			craft_result_id = ""
			btn_confirm.disabled = true
			GameLogger.warning("未知配方：%s" % recipe_key, "RewardManager")

	# 无论怎样，强制刷新绘制槽位！
	crafting_board.queue_redraw()


#画合成路线的函数，占位用
func _on_crafting_board_draw() -> void:
	if not crafting_board.visible: return
	
	# 清除之前创建的交互区域，防止重复添加
	for child in crafting_board.get_children():
		if child.name == "SlotInteractionArea":
			child.queue_free()

	var center = get_viewport().get_visible_rect().size / 2

	# 计算三个槽位的中心坐标
	var pos_slot1 = center + Vector2(-slot_spacing_x, -slot_spacing_y / 2)
	var pos_slot2 = center + Vector2(-slot_spacing_x, slot_spacing_y / 2)
	var pos_result = center + Vector2(slot_spacing_x * 0.5, 0)

	# 1. 绘制折线连接 (小槽 -> 中点 -> 大槽)
	var mid_x = center.x - slot_spacing_x * 0.3

	# 槽1出线
	crafting_board.draw_line(pos_slot1 + Vector2(slot_small_size.x / 2, 0), Vector2(mid_x, pos_slot1.y), line_color, line_width)
	crafting_board.draw_line(Vector2(mid_x, pos_slot1.y), Vector2(mid_x, 0), line_color, line_width)
	# 槽2出线
	crafting_board.draw_line(pos_slot2 + Vector2(slot_small_size.x / 2, 0), Vector2(mid_x, pos_slot2.y), line_color, line_width)
	crafting_board.draw_line(Vector2(mid_x, pos_slot2.y), Vector2(mid_x, 0), line_color, line_width)
	# 汇合指向大槽
	var arrow_start = Vector2(mid_x, 0)
	var arrow_end = pos_result - Vector2(slot_large_size.x / 2 + 20, 0)
	crafting_board.draw_line(arrow_start, arrow_end, line_color, line_width)

	# 绘制箭头三角
	var arrow_points = PackedVector2Array([
		arrow_end,
		arrow_end + Vector2(-20, -10),
		arrow_end + Vector2(-20, 10)
	])
	crafting_board.draw_polygon(arrow_points, PackedColorArray([line_color, line_color, line_color]))

	# 2. 绘制槽位封装函数 (灰底、黑边、缩放图案)
	# 检查卡牌引用是否有效，避免传递已释放的对象
	if craft_source_card_1 != null and not is_instance_valid(craft_source_card_1):
		craft_source_card_1 = null
	if craft_source_card_2 != null and not is_instance_valid(craft_source_card_2):
		craft_source_card_2 = null
	
	_draw_slot(pos_slot1, slot_small_size, craft_source_card_1, Mode.CRAFT_SLOT_1)
	_draw_slot(pos_slot2, slot_small_size, craft_source_card_2, Mode.CRAFT_SLOT_2)
	_draw_slot(pos_result, slot_large_size, null, Mode.NONE, craft_result_id)  # 结果槽不可点击


# 根据卡牌ID获取贴图
func _get_card_texture_by_id(card_id: String) -> Texture2D:
	# 尝试从卡牌工厂获取贴图
	if deck_manager != null and deck_manager.card_factory != null:
		# 检查卡牌工厂是否有方法获取贴图
		if deck_manager.card_factory.has_method("get_card_texture"):
			var tex = deck_manager.card_factory.get_card_texture(card_id)
			if tex != null:
				return tex
	
	# 尝试从常见路径加载贴图
	var possible_paths = [
		"res://card_asset/%s.png" % card_id,
		"res://card_data/%s.png" % card_id,
		"res://%s.png" % card_id,
		"res://textures/cards/%s.png" % card_id,
		"res://assets/cards/%s.png" % card_id,
	]
	
	for path in possible_paths:
		if ResourceLoader.exists(path):
			var tex = load(path)
			if tex != null:
				return tex
	
	# 如果找不到，返回null
	GameLogger.warning("无法找到卡牌贴图: %s" % card_id, "RewardManager")
	return null

# 槽位鼠标进入事件
func _on_slot_mouse_entered(area: Control):
	var card_id = area.get_meta("card_id")
	if card_id == "":
		return
	
	# 获取主界面显示Tooltip
	var main = get_tree().get_first_node_in_group("MainBoard")
	if main and main.has_method("show_tooltip"):
		# 为area动态添加卡牌属性，以便show_tooltip能读取
		var description = area.get_meta("description")
		var keywords = area.get_meta("keywords")
		
		# 确保description和keywords不是null
		if description == null:
			description = ""
		if keywords == null:
			keywords = []
		
		# 设置动态属性（使用set方法）
		area.set("raw_description", description)
		area.set("active_keywords", keywords)
		
		# 显示Tooltip（show_tooltip会检查get_parsed_description方法，如果没有则使用raw_description）
		main.show_tooltip(area)

# 槽位鼠标离开事件
func _on_slot_mouse_exited():
	var main = get_tree().get_first_node_in_group("MainBoard")
	if main and main.has_method("hide_tooltip"):
		main.hide_tooltip()

# 槽位输入事件
func _on_slot_gui_input(event: InputEvent, area: Control):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		var target_mode = area.get_meta("target_mode")
		
		if target_mode == Mode.CRAFT_SLOT_1 or target_mode == Mode.CRAFT_SLOT_2:
			# 点击素材槽位：重新进入选牌界面
			current_mode = target_mode
			_open_deck_selection()
		elif target_mode == Mode.NONE and craft_result_id != "":
			# 点击成品槽位且配方有效：触发确认合成
			_on_confirm_pressed()

# 创建槽位交互区域（透明Control节点）
func _create_slot_interaction_area(rect: Rect2, target_mode: Mode, card_id: String, description: String, keywords: Array):
	var area = Control.new()
	area.mouse_filter = Control.MOUSE_FILTER_STOP  # 接收鼠标事件
	area.modulate = Color(1, 1, 1, 0)  # 完全透明
	area.position = rect.position
	area.size = rect.size
	area.name = "SlotInteractionArea"
	
	# 存储卡牌信息供Tooltip使用
	area.set_meta("card_id", card_id)
	area.set_meta("description", description)
	area.set_meta("keywords", keywords)
	area.set_meta("target_mode", target_mode)
	
	# 连接鼠标事件
	area.mouse_entered.connect(_on_slot_mouse_entered.bind(area))
	area.mouse_exited.connect(_on_slot_mouse_exited)
	area.gui_input.connect(_on_slot_gui_input.bind(area))
	
	crafting_board.add_child(area)


# 计算贴图在容器矩形内保持宽高比的最大适应矩形
func _get_fitted_texture_rect(texture: Texture2D, container_rect: Rect2) -> Rect2:
	var container_size = container_rect.size
	var texture_size = texture.get_size()
	
	if texture_size.x <= 0 or texture_size.y <= 0:
		return container_rect
	
	var container_ratio = container_size.x / container_size.y
	var texture_ratio = texture_size.x / texture_size.y
	
	var fit_size: Vector2
	if texture_ratio > container_ratio:
		# 贴图较宽，以宽度为基准
		fit_size = Vector2(container_size.x, container_size.x / texture_ratio)
	else:
		# 贴图较高，以高度为基准
		fit_size = Vector2(container_size.y * texture_ratio, container_size.y)
	
	# 居中
	var fit_pos = container_rect.position + (container_size - fit_size) / 2
	return Rect2(fit_pos, fit_size)


func _draw_slot(pos: Vector2, size: Vector2, card_ref: Control, target_mode: Mode, fixed_id: String = ""):
	var rect = Rect2(pos - size / 2, size)

	# 灰底
	crafting_board.draw_rect(rect, Color(0.3, 0.3, 0.3, 0.8), true)
	# 深黑粗边框
	crafting_board.draw_rect(rect, Color(0.1, 0.1, 0.1, 1.0), false, 4.0)

	# 如果槽位里有卡，绘制贴图（等比缩放贴合边框）
	var display_id = fixed_id
	var display_texture: Texture2D = null
	var display_description: String = ""
	var display_keywords: Array = []
	
	if card_ref != null and is_instance_valid(card_ref): 
		display_id = card_ref.card_id
		# 获取卡牌贴图
		if card_ref.has_method("get_texture"):
			display_texture = card_ref.get_texture()
		elif "texture" in card_ref:
			display_texture = card_ref.texture
		# 获取卡牌描述和关键词
		if "raw_description" in card_ref:
			display_description = card_ref.raw_description
			if display_description == null:
				display_description = ""
		if "active_keywords" in card_ref:
			display_keywords = card_ref.active_keywords
			if display_keywords == null:
				display_keywords = []
	elif card_ref != null:
		# 对象已被释放，重置引用避免后续错误
		if card_ref == craft_source_card_1:
			craft_source_card_1 = null
		elif card_ref == craft_source_card_2:
			craft_source_card_2 = null
		display_id = ""
	
	# 如果是成品槽位，尝试根据ID获取贴图
	if display_id != "" and display_texture == null and fixed_id != "":
		# 尝试从卡牌工厂或资源路径加载贴图
		display_texture = _get_card_texture_by_id(display_id)
	
	if display_texture != null:
		# 计算贴图适应区域 (留出 5 像素内边距)
		var container_rect = Rect2(pos - size / 2 + Vector2(5, 5), size - Vector2(10, 10))
		var fitted_rect = _get_fitted_texture_rect(display_texture, container_rect)
		crafting_board.draw_texture_rect(display_texture, fitted_rect, false)

	# 为所有槽位生成透明交互区域（用于Tooltip和点击）
	_create_slot_interaction_area(rect, target_mode, display_id, display_description, display_keywords)





# ==========================================
# "跳过/离开"界面的专属钩子接口
# ==========================================
func _on_leave_pressed():
	GameLogger.info("⏭️ 玩家选择了跳过，离开营地！", "RewardManager")
	hide_all()

	## ★ 在这里对接你的下一关逻辑
	#var main = get_tree().get_first_node_in_group("MainBoard")
	#if main:
		## 假设你在 Main.gd 里写了一个进入下一层的方法
		#if main.has_method("proceed_to_next_stage"):
			#main.proceed_to_next_stage()
		#else:
			#print("提示: 请在 Main.gd 中实现 proceed_to_next_stage() 来生成新地图")
