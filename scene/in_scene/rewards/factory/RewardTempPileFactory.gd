extends RefCounted


## RewardTempPileFactory 只负责为奖励页面创建临时幽灵牌堆，并做创建后的基础可见性设置。
## 它不生成卡牌，不读取卡牌数据，不清理临时牌堆，也不决定奖励页面的选择或确认流程。


const PILE_SCENE: PackedScene = preload("res://addons/card-framework/pile.tscn")


func create_temp_pile(deck_manager: Node, owner_label: String) -> Pile:
	if deck_manager == null:
		push_error("%s: 无法创建临时牌堆，deck_manager 为空" % owner_label)
		return null

	var temp_pile := PILE_SCENE.instantiate() as Pile
	deck_manager.add_child(temp_pile)
	return temp_pile


func create_hidden_temp_pile(deck_manager: Node, owner_label: String) -> Pile:
	var temp_pile: Pile = create_temp_pile(deck_manager, owner_label)
	if temp_pile != null:
		temp_pile.visible = false
	return temp_pile
