extends RefCounted


## RewardCardTextureExtractor 只负责从真实卡牌节点或卡牌工厂缓存中读取奖励页卡面贴图。
## 它不创建卡牌，不修改 DraftCard，也不决定奖励页面后续流程。


func extract_front_texture(real_card: Node, card_id: String, deck_manager, owner: Node) -> Texture2D:
	var direct_texture := _read_front_texture(real_card)
	if direct_texture != null:
		return direct_texture

	if owner != null and owner.is_inside_tree():
		await owner.get_tree().process_frame

	var delayed_texture := _read_front_texture(real_card)
	if delayed_texture != null:
		return delayed_texture

	if deck_manager and deck_manager.card_factory:
		var cached_texture := _read_cached_texture(deck_manager.card_factory, card_id)
		if cached_texture != null:
			return cached_texture

		var fallback_texture := _load_front_image(real_card, deck_manager.card_factory)
		if fallback_texture != null:
			return fallback_texture

	return null


func _read_front_texture(real_card: Node) -> Texture2D:
	if real_card == null:
		return null

	if real_card.has_node("FrontFace/TextureRect"):
		var front_rect = real_card.get_node("FrontFace/TextureRect")
		if front_rect is TextureRect and front_rect.texture != null:
			return front_rect.texture

	if _object_has_property(real_card, &"front_face_texture"):
		var front_face_texture = real_card.get("front_face_texture")
		if front_face_texture is TextureRect and front_face_texture.texture != null:
			return front_face_texture.texture
		if front_face_texture is Texture2D:
			return front_face_texture

	return null


func _read_cached_texture(card_factory, card_id: String) -> Texture2D:
	var preloaded_cards = card_factory.get("preloaded_cards")
	if typeof(preloaded_cards) != TYPE_DICTIONARY or not preloaded_cards.has(card_id):
		return null

	var cached = preloaded_cards[card_id]
	if typeof(cached) == TYPE_DICTIONARY and cached.has("texture") and cached["texture"] != null:
		return cached["texture"]

	return null


func _load_front_image(real_card: Node, card_factory) -> Texture2D:
	if real_card == null:
		return null

	var card_info = real_card.get("card_info")
	if typeof(card_info) != TYPE_DICTIONARY or not card_info.has("front_image"):
		return null

	var asset_dir = card_factory.get("card_asset_dir")
	if typeof(asset_dir) != TYPE_STRING or asset_dir == "":
		return null

	var texture_path = asset_dir + "/" + str(card_info["front_image"])
	return load(texture_path) as Texture2D


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false

	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true

	return false
