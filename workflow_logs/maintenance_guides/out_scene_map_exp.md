# out_scene_map_exp.gd 维护说明

日期：2026-06-13

## 当前职责

`scene/out_scene/out_scene_map_exp.gd` 是局外地图 composition root。它继续负责地图生成/恢复、玩家移动、路径坍塌、镜头限制入口、进房、保存、章节推进和切场景。

## 已拆模块

已拆模块位于 `scene/out_scene/out_scene_modules/`：

- `RoomResolutionController.gd`：房间结算 payload 消费、坐标解析、boss/tier 推进计划。
- `ChapterRevealAnimationRunner.gd`：boss 后新章节地块揭示动画。
- `OutScenePayloadBridge.gd`：切场前 payload 注入到 HexMap、MainBoard、根节点或旧兜底节点。
- `OutSceneCameraLimitController.gd`：局外地图层级边界和选角后扇区边界的 `Camera2D.limit_*` 写入。

## 不要继续硬拆

- 不要复用 `path_gone` 表示房间完成；它是路径坍塌记录。
- 不要重复拆镜头限制；旧入口已经转发给 `OutSceneCameraLimitController.gd`。
- 不要同批改地图移动和场景切换 executor。
- 不要把 `tile_data` 混成完成状态字典，避免影响渲染、移动和存档。

## 后续可做

镜头限制小模块已经完成，后续不要重复拆 `apply_tier_camera_limit()` 与 `_apply_sector_camera_limits()`。如果继续局外地图，先重新审查剩余函数；房间完成状态实现需要先新增独立 `MapState` 字段和 Saver 持久化，不能作为顺手拆分。

## 验证入口

改动后加载 `res://scene/out_scene/Out_Scene.tscn`，检查局外地图生成、移动、镜头边界、进房 payload 和 boss 章节推进。
