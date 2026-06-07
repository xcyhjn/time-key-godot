class_name CraftRecipeBook
extends Resource

## CraftRecipeBook 只负责保存合成配方表并提供只读查询。
## 它不修改牌组，不创建卡牌，也不访问场景树或奖励页 UI。


@export var recipes: Array[Resource] = []


func get_recipe_result(card_a_id: String, card_b_id: String) -> String:
	for recipe in recipes:
		if recipe == null:
			continue
		if _matches(recipe, card_a_id, card_b_id):
			return str(recipe.get("result_card_id"))

	return ""


func _matches(recipe: Resource, card_a_id: String, card_b_id: String) -> bool:
	return (
		str(recipe.get("card_a_id")) == card_a_id
		and str(recipe.get("card_b_id")) == card_b_id
	) or (
		str(recipe.get("card_a_id")) == card_b_id
		and str(recipe.get("card_b_id")) == card_a_id
	)
