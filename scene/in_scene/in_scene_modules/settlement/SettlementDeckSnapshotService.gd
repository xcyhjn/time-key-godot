class_name SettlementDeckSnapshotService
extends RefCounted


## SettlementDeckSnapshotService 只负责在进入结算整理前保存当前牌组快照。
## 它不移动卡牌、不刷新 UI，也不决定是否进入结算阶段。


func snapshot_current_deck(global_db: Variant, map_state: Variant) -> bool:
	if not global_db:
		return false

	if map_state:
		map_state.set_saved_deck(global_db.player_deck)
	return true
