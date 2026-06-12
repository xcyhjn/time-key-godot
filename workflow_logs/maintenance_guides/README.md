# 大文件维护入口索引

日期：2026-06-12

## 用途

这个目录存放大文件的单独维护说明。它不替代 `docs/` 下的总结性文档，也不记录逐批过程；逐批过程仍写入 `workflow_logs/current-modularization-process.md`。

这些文件只回答三件事：

- 主脚本现在还负责什么。
- 已拆模块在哪里，维护时不要越界做什么。
- 下一批还能安全拆哪里，以及哪些地方应该停止硬拆。

## 当前维护入口

| 主文件 | 维护说明 |
| --- | --- |
| `scene/in_scene/hex_map.gd` | 以 `docs/hex-map-ultimate-operation-guide.md` 为准；本目录不重复维护。 |
| `scene/in_scene/in_scene.gd` | `workflow_logs/maintenance_guides/in_scene.md` |
| `scene/in_scene/DragShapeController.gd` | `workflow_logs/maintenance_guides/drag_shape_controller.md` |
| `scene/in_scene/timeline/timeline_ui.gd` | `workflow_logs/maintenance_guides/timeline_ui.md` |
| `scene/card/custom_card.gd` | `workflow_logs/maintenance_guides/custom_card.md` |
| `scene/in_scene/timecoin_ui.gd` | `workflow_logs/maintenance_guides/timecoin_ui.md` |
| `scene/in_scene/tile.gd` | `workflow_logs/maintenance_guides/tile.md` |
| `scene/out_scene/out_scene_map_exp.gd` | `workflow_logs/maintenance_guides/out_scene_map_exp.md` |
| `scene/in_scene/rewards/*.gd` | `workflow_logs/maintenance_guides/rewards.md` |

## 使用规则

- 改某个大文件前，先读它对应的维护说明，再看 `docs/modularized-files-ultimate-operation-guide.md` 中的模块条目。
- 新增拆分模块后，更新对应维护说明里的“已拆模块”和“下一步”。
- 如果只是本批过程、失败尝试或验证输出，写入 `workflow_logs/current-modularization-process.md`，不要写进这些维护说明。
- `docs/` 仍只保留总结性说明；本目录用于补齐单文件维护入口。
