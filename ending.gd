extends CanvasLayer # 或 Control，取决于你的根节点类型

@onready var map_proxy = $MapViewportContainer/SubViewport/WorldMapProxy
@onready var pulse_mask = $VFX_Layer/PulseMask
@onready var shatter_particles = $VFX_Layer/ShatterParticles
@onready var ui_layer = $UI_Layer
@onready var post_process = $PostProcess

# UI 引用
@onready var title = $UI_Layer/FalseVictoryTitle
@onready var stats_panel = $UI_Layer/StatsPanel
@onready var sector_icons = $UI_Layer/SectorIcons

# 内部状态
var player_token_proxy: Sprite2D # 假设你在 WorldMapProxy 里动态生成了主角的 Token

func play_ending_sequence(run_data: Dictionary, logical_map: Dictionary):
	# 0. 初始化与重绘镜像地图
	_rebuild_map_mirror(logical_map)
	show()
	get_tree().paused = true
	
	# 1. 第一阶段：点亮全图
	await stage_1_grand_reveal(run_data.current_sector)
	
	# 2. 第二阶段：命运裁决 (灰暗与破碎)
	await stage_2_false_judgment(run_data.cleared_sectors)
	
	# 3. 第三阶段：重归原点 (时空牵引)
	await stage_3_great_regression(run_data.start_pos, run_data.end_pos)
	
	# 4. 第四阶段：最终结算
	await stage_4_observers_sigh(run_data)
