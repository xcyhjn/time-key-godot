# 原文件名: global_db(卡牌关键词).gd
# 功能: 卡牌关键词库
#class_name GlobalDB
extends Node

# ★ 新增：玩家当前的逻辑牌组（存储卡牌的 ID 字符串）
const STARTER_DECK: Array[String] = ["lighting", "lighting", "2", "2", "recover", "wind", "wind", "recover","tower","tower","poison","poison"]
var player_deck: Array[String] = STARTER_DECK.duplicate()
## 关键词库：包含颜色和详细解释
const KEYWORDS: Dictionary = {
	"消耗": {
		"color": "#aaaaaa",
		"desc": "打出这张牌后，将其置入消耗区而不是弃牌堆"
	},
	"抬升": {
		"color": "#cda4ff",
		"desc": "地块高度上升1"
	},
	"下降": {
		"color": "#ff4d4d",
		"desc": "地块高度下降1"
	},
	"中毒": {
		"color": "#a855f7",
		"desc": "每回合向周围扩散，每层中毒在回合开始时候扣除所属建筑10%的血量，在这之后减少一层"
	},
	"迷信": {
		"color": "#750a99",
		"desc": "目标以及目标周围一圈的单位进化速度变缓"
	},
	"伤害": {
		"color": "#750a99",
		"desc": "使目标失去X点血量"
	},
	"启蒙": {
		"color": "#1b00cd",
		"desc": "目标每回合结束增加一次特殊行动，推动时间轴一格"
	},
}

## 文本替换图标
## 注意：请把这里的路径换成你项目里真实的图片路径
const ICONS: Dictionary = {
	"[ATK]": "res://图片/fc155.png",
	"[能量]": "res://图片/fc172.png"
}


## 把当前玩家牌组重置为开局默认牌组。
## 新开局时应调用这个接口，而不是在外部硬编码一份初始数组，
## 这样后续如果你调整 starter deck，只改这里一处就够了。
func reset_player_deck() -> void:
	player_deck = STARTER_DECK.duplicate()
