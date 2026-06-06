extends RefCounted

## ShopEraWeightSelector 只负责根据商店权重随机选择目标时代。
## 它不读取 CardDataPool，不创建卡牌，也不处理商店刷新或购买。


func select_era(
	base_era: int,
	weight_previous_era: float,
	weight_current_era: float,
	weight_next_era: float,
	weight_next_next_era: float
) -> int:
	var era_weights := {
		base_era - 1: weight_previous_era,
		base_era: weight_current_era,
		base_era + 1: weight_next_era,
		base_era + 2: weight_next_next_era,
	}

	for era in era_weights.keys():
		if int(era) < 1:
			era_weights.erase(era)

	var total_weight := 0.0
	for weight in era_weights.values():
		total_weight += float(weight)

	if total_weight <= 0.0:
		return -1

	var rand_val := randf() * total_weight
	var cumulative := 0.0
	var selected_era := base_era

	for era in era_weights:
		cumulative += float(era_weights[era])
		if rand_val <= cumulative:
			selected_era = int(era)
			break

	return selected_era
