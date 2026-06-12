extends RefCounted


## TimecoinTweenStateController 只负责维护 TimecoinUI 的 active_tweens 列表。
## 它不创建 Tween，不拼接动画片段，不重置 UI 位置、缩放或颜色，也不连接 GlobalTimecoin 信号或控制沙漏 shader。


func register(active_tweens: Array[Tween], tween: Tween) -> Array[Tween]:
	if tween:
		active_tweens.append(tween)
	return active_tweens


func cleanup(active_tweens: Array[Tween], max_concurrent_tweens: int) -> Array[Tween]:
	if active_tweens.size() >= max_concurrent_tweens:
		print("[TimecoinUI] 达到最大 Tween 数量限制 (%d)，清理旧动画" % max_concurrent_tweens)

		var oldest_tween: Tween = active_tweens.pop_front()
		if oldest_tween and oldest_tween.is_valid():
			oldest_tween.kill()

	var valid_tweens: Array[Tween] = []
	for tween in active_tweens:
		if tween and tween.is_valid():
			valid_tweens.append(tween)
		else:
			print("[TimecoinUI] 清理无效的 Tween 引用")

	return valid_tweens


func kill_all(active_tweens: Array[Tween]) -> void:
	for tween in active_tweens:
		if tween and tween.is_valid():
			tween.kill()


func remove(active_tweens: Array[Tween], tween: Tween) -> Array[Tween]:
	var index := active_tweens.find(tween)
	if index != -1:
		active_tweens.remove_at(index)
	return active_tweens
