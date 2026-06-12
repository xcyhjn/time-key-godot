extends RefCounted


## OutSceneCameraLimitController 只负责计算并写入局外地图 Camera2D 的边界限制。
## 它不移动镜头，不锁定或解锁镜头，不修改地图数据，也不处理玩家移动、进房、保存或场景切换。

const TIER_MARGIN: float = 300.0
const TIER_HORIZONTAL_PADDING: float = 1000.0
const TIER_TOP_PADDING: float = 500.0
const TIER_BOTTOM_PADDING: float = -500.0
const SECTOR_MARGIN: float = 1400.0


func apply_tier_limit(
	camera: Camera2D,
	layer_boundaries: Array,
	tier_index: int,
	step_x: float,
	step_y: float,
	stagger_y: float
) -> void:
	var max_idx: int = layer_boundaries.size() - 1
	var safe_tier: int = clamp(tier_index, 0, max_idx)
	var radius: float = float(layer_boundaries[safe_tier])
	var w_limit: float = radius * step_x + TIER_MARGIN
	var h_limit: float = radius * (step_y + stagger_y) + TIER_MARGIN

	camera.limit_left = int(-w_limit - TIER_HORIZONTAL_PADDING)
	camera.limit_right = int(w_limit + TIER_HORIZONTAL_PADDING)
	camera.limit_top = int(-h_limit + TIER_TOP_PADDING)
	camera.limit_bottom = int(h_limit + TIER_BOTTOM_PADDING)
	camera.limit_smoothed = true


func apply_sector_limit(camera: Camera2D, chosen_hex: Vector2i) -> void:
	if chosen_hex == Vector2i(0, -1):
		camera.limit_bottom = int(SECTOR_MARGIN)
	elif chosen_hex == Vector2i(0, 1):
		camera.limit_top = int(-SECTOR_MARGIN)
	elif chosen_hex == Vector2i(-1, 0):
		camera.limit_right = int(SECTOR_MARGIN)
		camera.limit_bottom = int(SECTOR_MARGIN)
	elif chosen_hex == Vector2i(-1, 1):
		camera.limit_right = int(SECTOR_MARGIN)
		camera.limit_top = int(-SECTOR_MARGIN)
	elif chosen_hex == Vector2i(1, -1):
		camera.limit_left = int(-SECTOR_MARGIN)
		camera.limit_bottom = int(SECTOR_MARGIN)
	elif chosen_hex == Vector2i(1, 0):
		camera.limit_left = int(-SECTOR_MARGIN)
		camera.limit_top = int(-SECTOR_MARGIN)

	camera.limit_smoothed = true
