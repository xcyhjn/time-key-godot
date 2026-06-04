class_name InSceneNodeBridge
extends RefCounted


## InSceneNodeBridge 集中维护局内主场景的固定节点路径。
## 它只负责查找节点，不保存战斗状态，也不修改场景树；
## 后续调整 in_scene.tscn 层级时，优先改这里，避免路径散落回主脚本。

const NODE_PATHS: Dictionary = {
	"deck_button": "../DeckButton",
	"discard_button": "../DiscardButton",
	"deck_count_label": "../DeckButton/Label",
	"discard_count_label": "../DiscardButton/Label",
	"shop_button": "../ShopButton",
	"acquire_reward_button": "../AcquireRewardButton",
	"remove_reward_button": "../RemoveRewardButton",
	"craft_reward_button": "../CraftRewardButton",
	"lose_button": "../LoseButton",
	"win_debug_button": "../WinButton",
	"combat_victory_debug_button": "../CombatVictoryDebugButton",
	"game_over_ui": "../GameOver",
	"hex_map": "../../map/HexMap",
	"timecoin_container": "/root/in_scene/TimecoinView/TimecoinCanvasLayer/TimecoinContainer",
	"end_turn_button": "../EndTurnButton",
	"end_combat_button": "../EndCombatButton",
	"height_view_toggle_button": "../HeightViewToggleButton",
	"cursor_tooltip": "../CursorTooltip",
	"timeline_ui": "../TimelineUI",
	"timeline_manager": "../TimelineSystem/TimelineManager",
	"dim": "../DimMenu",
	"win": "../GameWinScreen",
	"total_enemy_health_bar": "../TotalEnemyHealthBar",
	"combat_victory_banner": "../../combat_victory_banner",
	"combat_cartoon_ui": "../../CartoonUI",
}


## 按旧路径一次性解析局内主控需要的节点。
## 返回字典让主脚本继续持有原变量，避免本批影响已有公共入口。
func resolve(main_node: Node) -> Dictionary:
	var nodes: Dictionary = {}
	for key in NODE_PATHS:
		nodes[key] = find_node(main_node, key)
	return nodes


## 查找单个节点。
## 绝对路径和相对路径都保留旧行为：找不到时返回 null，不抛出错误。
func find_node(main_node: Node, key: String) -> Node:
	if not is_instance_valid(main_node):
		return null
	if not NODE_PATHS.has(key):
		return null

	var path: String = String(NODE_PATHS[key])
	return main_node.get_node_or_null(path)
