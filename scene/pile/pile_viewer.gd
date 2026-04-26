# 原文件名: pile_viewer(生成局外收获卡牌).gd
# 功能: 生成局外收获卡牌查看器
extends CanvasLayer

@onready var card_grid = $ScrollContainer/card
@onready var background = $background

# 引用 CardManager 来使用工厂生产展示用的卡牌
var card_manager: CardManager


func _ready():
	# 初始隐藏
	hide()
	$back.pressed.connect(_on_close_pressed)


func open_pile_view(source_pile: Pile, manager: CardManager):
	card_manager = manager
	show()

	# 1. 清理旧的显示内容
	for child in card_grid.get_children():
		child.queue_free()

	# 2. 遍历源牌堆的所有卡牌
	# 注意：我们不能直接把 source_pile 里的卡牌移动过来，那样会破坏游戏逻辑。
	# 我们需要根据数据生成“视觉副本”。
	for real_card in source_pile._held_cards:
		_create_visual_copy(real_card)


func _create_visual_copy(real_card: Card):
	# 1. 获取卡牌的基础场景（我们需要手动实例化，不能用工厂的 create_card）
	# 注意：这里需要引用 factory 里的 default_card_scene
	var card_scene = card_manager.card_factory.default_card_scene
	if not card_scene:
		push_error("CardFactory 没有设置 default_card_scene！")
		return
	# 2. 手动实例化卡牌
	var visual_card = card_scene.instantiate()
	# 3. 手动填充数据 (模仿工厂的 _create_card_node 逻辑)
	# 获取原卡牌的数据和图片
	var card_info = real_card.card_info
	var card_name = card_info.get("name", "unknown")

	# 设置卡牌基本属性
	visual_card.card_info = card_info
	visual_card.card_name = card_name
		# 如果没有便捷方法，尝试从 card_info 重新加载（慢一点但稳妥）
		# 注意：这需要 factory 暴露加载图片的方法，或者我们自己加载
		# 简单起见，我们直接尝试复制原卡牌的 TextureRect/Sprite 的贴图
		# 请根据你的 Card 场景结构调整下面的路径，比如 $FrontSprite 或 $TextureRect
	var real_front_node = real_card.get_node_or_null("FrontFace/TextureRect")
	var front_texture = null

	if real_front_node and real_front_node.texture:
		front_texture = real_front_node.texture
	else:
		# 如果真卡没加载出来（极少情况），尝试从 factory 缓存拿，或者打印个警告
		pass

	# 2. 将图片“贴”到【副本卡】上
	# 我们直接操作副本卡的 TextureRect 节点，这样最直接，不依赖 set_faces
	var visual_front_node = visual_card.get_node_or_null("FrontFace/TextureRect")
	if visual_front_node:
		visual_front_node.texture = front_texture
		# 确保 TextureRect 撑满卡牌大小 (根据你的设置可能需要)
		visual_front_node.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		visual_front_node.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		visual_front_node.size = Vector2(125, 175)  # 强制对齐大小

	# 3. 处理背面 (给副本卡也贴上背面图)
	var back_texture = card_manager.card_factory.back_image
	var visual_back_node = visual_card.get_node_or_null("BackFace/TextureRect")
	if visual_back_node:
		visual_back_node.texture = back_texture
		visual_back_node.size = Vector2(125, 175)

	# --- 【关键】确保副本卡显示的是“正面” ---
	# 如果你的 Card 脚本里有 show_front 变量，设为 true
	if "show_front" in visual_card:
		visual_card.show_front = true

	# 手动强制显示正面节点，隐藏背面节点 (双重保险)
	if visual_front_node: visual_front_node.get_parent().visible = true
	if visual_back_node: visual_back_node.get_parent().visible = false
# --- 【关键修复 1】强制设置卡牌在 UI 中的大小 ---
	# GridContainer 需要知道子节点有多大，否则会把它压扁成 0x0
	visual_card.custom_minimum_size = Vector2(125, 175)

	# --- 【关键修复 2】修正偏移 ---
	# 很多卡牌设计的中心点在图片中心(0,0)，但在 UI 容器里，中心点需要在左上角
	# 我们用一个 Control 节点把卡牌位置“推”回来（如果你的卡牌显示只有 1/4，就调整这里）
	# visual_card.position = Vector2(62.5, 87.5) # 可选：如果卡牌只有右下角可见，取消这行注释

	# --- 【关键修复 3】确保卡牌是“正面朝上”的 ---
	if "card_face_up" in visual_card:
		visual_card.card_face_up = true
	# 调用可能存在的更新视觉的方法
	if visual_card.has_method("_update_visuals"):
		visual_card._update_visuals()
	# 4. 添加到 GridContainer 中
	card_grid.add_child(visual_card)

	# 5. 彻底禁用交互 (防止在查看界面被拖拽)
	visual_card.set_process_input(false)
	visual_card.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_disable_input_recursive(visual_card)


func _disable_input_recursive(node: Node):
	if node is Control:
		node.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if node is CollisionObject2D:  # 如果卡牌用了物理检测
		node.input_pickable = false


	for child in node.get_children():
		_disable_input_recursive(child)
func _on_close_pressed():
	hide()
	# 关闭时清理，节省内存
	for child in card_grid.get_children():
		child.queue_free()
