extends RefCounted

## CraftRecipeResolver 只负责合成配方查询。
## 它不修改牌库，不创建卡牌，也不处理合成界面的选择状态。


func get_recipe_result(card_a_id: String, card_b_id: String, recipe_book: Resource, fallback_recipes: Dictionary) -> String:
	if recipe_book != null and recipe_book.has_method("get_recipe_result"):
		var resource_result := str(recipe_book.get_recipe_result(card_a_id, card_b_id))
		if not resource_result.is_empty():
			return resource_result

	return get_recipe_result_from_dictionary(card_a_id, card_b_id, fallback_recipes)


func get_recipe_result_from_dictionary(card_a_id: String, card_b_id: String, recipes: Dictionary) -> String:
	var key_ab := "%s_%s" % [card_a_id, card_b_id]
	if recipes.has(key_ab):
		return str(recipes[key_ab])

	var key_ba := "%s_%s" % [card_b_id, card_a_id]
	if recipes.has(key_ba):
		return str(recipes[key_ba])

	return ""
