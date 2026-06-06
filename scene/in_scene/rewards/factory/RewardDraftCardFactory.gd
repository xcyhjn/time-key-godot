extends RefCounted


## RewardDraftCardFactory 只负责创建奖励页使用的轻量 DraftCard 并写入基础展示尺寸。
## 它不读取真实卡牌数据，不加入页面容器，也不连接点击信号。


func create_draft_card(draft_card_scene: PackedScene, card_id: String, display_size: Vector2) -> Control:
	var draft_card := draft_card_scene.instantiate() as Control
	draft_card.card_id = card_id
	draft_card.custom_set_size = display_size
	return draft_card
