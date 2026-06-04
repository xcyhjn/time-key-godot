class_name InSceneGlobalClockBridge
extends RefCounted


## InSceneGlobalClockBridge 只处理局内主控与 GlobalClock 的同步边界。
## 它不推进回合、不决定 UI 文案，只保证时代/阶段值从同一个全局来源读写。


## 连接 GlobalClock.progress_changed。
## 调用方传入已有的“一次性连接”函数，避免本模块复制信号防重逻辑。
func connect_progress_changed(connect_once: Callable, callback: Callable) -> void:
	if not GlobalClock:
		return
	connect_once.call(GlobalClock.progress_changed, callback)


## 从 GlobalClock 拉取当前时代，最小值固定为 1。
func pull_era() -> int:
	if not GlobalClock:
		return 1
	return max(int(GlobalClock.get_current_era()), 1)


## 将局内时代值写回 GlobalClock 和 MapState。
## 返回规范化后的时代值，主脚本继续持有 current_era_value。
func push_era(era_value: int) -> int:
	var normalized_era: int = max(era_value, 1)
	if GlobalClock:
		GlobalClock.set_current_era(normalized_era)
	if MapState and GlobalClock:
		MapState.set_saved_era_progress(normalized_era, int(GlobalClock.get_current_phase()))
	return normalized_era


## 推进全局阶段，并返回推进后的时代/阶段快照。
## 这里只封装 GlobalClock 桥接，不包含回合开始的卡牌或敌人意图流程。
func advance_phase() -> Dictionary:
	if GlobalClock:
		GlobalClock.advance_phase()
	var era_value: int = pull_era()
	var phase_value: int = int(GlobalClock.get_current_phase()) if GlobalClock else 0
	if MapState:
		MapState.set_saved_era_progress(era_value, phase_value)
	return {
		"era": era_value,
		"phase": phase_value,
	}


## 读取当前阶段，用于刷新顶部战斗 UI。
func get_phase() -> int:
	if not GlobalClock:
		return 0
	return int(GlobalClock.get_current_phase())
