extends Control

var hand_scene = load("res://addons/card-framework/hand.tscn")
var player_hand: Hand
var pile_scene = load("res://addons/card-framework/pile.tscn")
var deck_pile: Pile
var discard_pile: Pile # 提为变量方便全局访问
var manager_instance: CardManager
var drop_area = 4.0 / 7.0
var card_size : Vector2 = Vector2(125.0,175.0)

func _on_discard_pile_card_added(card: Card):
	# 这等同于之前的 card_moved 逻辑，但专门针对弃牌堆
	print("卡牌进入弃牌堆：", card.card_info.get("name"))
	_handle_discard_effects(card)

func _ready() -> void:
	setup_card_system()

func setup_card_system():
	var screen_size = get_viewport_rect().size
	var manager_scene = load("res://addons/card-framework/card_manager.tscn")
	manager_instance = manager_scene.instantiate() as CardManager # 赋值给全局变量
	
	manager_instance.card_factory_scene = load("res://addons/card-framework/card_factory.tscn")
	manager_instance.debug_mode = false
	add_child(manager_instance)
	
	# --- 1. 实例化手牌区 ---
	player_hand = hand_scene.instantiate() as Hand
	add_child(player_hand) 
	player_hand.position = Vector2(screen_size.x / 2.0 - card_size.x / 2, screen_size.y * 0.7)

	# --- 2. 实例化抽牌区 ---
	deck_pile = pile_scene.instantiate() as Pile
	add_child(deck_pile)
	deck_pile.position = Vector2(0.2 * card_size.x, screen_size.y - 1.2 * card_size.y) 
	deck_pile.card_face_up = false
	deck_pile.restrict_to_top_card = true  
	deck_pile.allow_card_movement = false  # 抽牌库设为不可手动拖拽（只能点按抽牌）
	deck_pile.enable_drop_zone = false     # 抽牌库不可放回
	deck_pile.stack_display_gap = 2        

	# --- 3. 实例化弃牌区 ---
	discard_pile = pile_scene.instantiate() as Pile
	add_child(discard_pile)
	discard_pile.position = Vector2(screen_size.x - 1.2 * card_size.x, screen_size.y - 1.2 * card_size.y) 
	discard_pile.card_face_up = true   
	discard_pile.enable_drop_zone = false  
	discard_pile.stack_display_gap = 2    

	# --- 4. 初始化生成卡牌 ---
	await get_tree().process_frame # 等待一帧确保工厂准备就绪
	for i in range(5): # 牌堆放n张
		manager_instance.card_factory.create_card("bailong", deck_pile)
		manager_instance.card_factory.create_card("zfy", deck_pile)
		manager_instance.card_factory.create_card("bailong2", deck_pile)
	deck_pile._held_cards.shuffle() #洗牌
	

	# 连接信号：监听全场卡牌移动（这是实现弃牌效果的核心）
	# 注意：需要检查card-framework是否真的有card_moved信号
	# 如果没有，你可能需要检查框架文档或使用其他方式监听卡牌移动
	# 这里添加一个安全检查

# --- 逻辑 A：条件抽牌规则 ---
func _input(event):
	# --- 逻辑 A: 点击抽牌 (你原有的代码) ---
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		var mouse_pos = get_global_mouse_position()
		if mouse_pos.distance_squared_to(deck_pile.global_position + Vector2(62.5, 87.5)) < 13000:
			attempt_draw_cards(3)
			return
		

	# --- 逻辑 B: 截获“空地释放”实现弃牌 (新增) ---
	if event is InputEventMouseButton and not event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		# 检查当前是否有卡牌正在被玩家拖拽
		# 注意：Card 脚本通常会有 is_dragging 或类似状态，如果框架没提供，
		# 我们可以遍历手牌看看哪张牌的 is_pressed 为 true (根据 Pile 代码推断)
		for card in player_hand._held_cards:
			if card.is_pressed: 
				var mouse_pos = get_global_mouse_position()
				var screen_size = get_viewport_rect().size
				var discard_threshold = screen_size.y * drop_area
				#这里可以加一个向量计算距离，然后切换动画的操作***
				#if mark in field这种感觉
				#然后计算到每个mark的距离，动画选取为最近距离然后施加一个箭头
				
				# 如果在上方区域松手 #这里我再加一个锁定逻辑就行，这样就有索敌功能了
				if mouse_pos.y < discard_threshold:
					# 强行打断框架的回弹逻辑，将其移入弃牌堆
					# 使用 call_deferred 确保在当前物理帧处理完后再移动
					discard_pile.call_deferred("move_cards", [card])
					# 这里由于是强制移动，可能不会触发 card_moved，我们手动调一下效果
					_handle_discard_effects(card)
					break

func attempt_draw_cards(count: int):
	# 特定条件判定示例：比如手牌不能超过 7 张
	if player_hand._held_cards.size() >= 7:
		print("手牌已满，无法摸牌！")
		return
		
	print("符合摸牌条件，开始摸牌...")
	for i in range(count):
		if deck_pile._held_cards.size() > 0 and player_hand._held_cards.size() < 7:
			var top_card = deck_pile._held_cards.back()
			player_hand.move_cards([top_card])
			await get_tree().create_timer(0.25).timeout # 摸牌间隔动画
		elif deck_pile._held_cards.size() == 0:
			print("卡组已空")
			shuffle_card()
			await shuffle_card()
			await get_tree().create_timer(0.8).timeout
			var top_card = deck_pile._held_cards.back()
			player_hand.move_cards([top_card])
			await get_tree().create_timer(0.25).timeout # 摸牌间隔动画
			continue
		else:
			print("手牌已满")
			break

# 处理具体效果的函数
func _handle_discard_effects(card: Card):
	var card_name = card.card_info.get("name", "未知卡牌")
	print("正在处理卡牌效果：", card_name)
	
	match card_name:
		"bailong":
			trigger_bailong_effect()
		# 你可以在这里添加更多卡牌效果

func trigger_bailong_effect():
	# 这里写你的业务逻辑，比如加血、扣血、播放音效等
	print("白龙入墓！触发了神圣的白光效果！")

func _disintegrate_card(card: Card):
	# 销毁逻辑：等待动画飞完后删除
	await get_tree().create_timer(0.5).timeout
	discard_pile.remove_card(card)
	card.queue_free()
	print("卡牌已从弃牌区彻底销毁（移出游戏）")

func shuffle_card():
	if discard_pile._held_cards.is_empty():
		print("弃牌堆也是空的，无牌可洗！")
		return
	print("正在将弃牌堆洗入抽牌堆...")
	
	# 1. 批量移动卡牌：从 discard_pile 移到 deck_pile
	# 注意：move_cards 会自动处理节点父子关系的切换
	var cards_to_move = discard_pile._held_cards.duplicate()
	deck_pile.move_cards(cards_to_move)

	# 2. 逻辑洗牌：打乱数组顺序
	# _held_cards 是 CardContainer 定义的存储卡牌引用的数组
	deck_pile._held_cards.shuffle()

	# 3. 视觉更新：确保所有牌盖着，并根据 Pile 逻辑重新堆叠
	# 强制抽牌堆的所有牌背面向上
	deck_pile.card_face_up = false 
	deck_pile.update_card_ui()
	print("洗牌完成！当前抽牌堆数量：", deck_pile._held_cards.size())

func _process(_delta):
	queue_redraw() # 实时刷新绘图
	for card in player_hand._held_cards:
			if card.current_state == DraggableObject.DraggableState.HOVERING:
				var mat = card.front_face_texture.material as ShaderMaterial
				if mat:
					mat.set_shader_parameter("use_flash", true)
			if card.current_state == DraggableObject.DraggableState.IDLE:
				var mat = card.front_face_texture.material as ShaderMaterial
				if mat:
					mat.set_shader_parameter("use_flash", false)

func _draw():
	if manager_instance and manager_instance.debug_mode:
		var screen_size = get_viewport_rect().size
		var y_line = screen_size.y * drop_area
		
		# 画一条红色的虚线，代表弃牌区的下边界
		draw_line(Vector2(0, y_line), Vector2(screen_size.x, y_line), Color.RED, 2.0)
		
		# 给上方区域涂上一层淡淡的蓝色，表示有效感应区
		var rect = Rect2(0, 0, screen_size.x, y_line)
		draw_rect(rect, Color(0, 0, 1, 0.05)) # 5% 透明度的蓝色
	pass
