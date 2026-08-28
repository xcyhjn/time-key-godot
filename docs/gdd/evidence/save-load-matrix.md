# 存档与生命周期矩阵

## 当前字段

| 区段 | 字段 |
| --- | --- |
| meta | timestamp、era |
| player | player_hex、chosen_char_index、has_cut |
| map | map_seed、current_tier、is_initialized、tile_data、tile_features、path_gone |
| deck | player_deck |

## 观察与事实

- 暂停菜单可保存回标题；`save_0.cfg` 写入后 Continue 可恢复局外。
- 新游戏会重置 MapState、时代、时间币和 STARTER_DECK。
- GameWin 触发后删除 slot 0。
- Saver 对 JSON 数组/字典有类型兜底，但没有 schema_version。
- 战斗中的时间轴、手牌、当前敌意不在当前存档字段中；“战斗中保存”实际更接近局外快照。

## 边界

| 场景 | AS_IS | PROPOSED |
| --- | --- | --- |
| 无存档 | Continue 先 Has_save | 按钮明确禁用 |
| cfg 损坏 | load 失败返回 false | 备份、错误提示、可新开局 |
| JSON 字段损坏 | 回退空数组/字典 | 字段级报错，禁止静默清空牌组 |
| 旧/未来版本 | 无版本字段 | 增加 schema_version 和迁移器 |
| 重复奖励 | 依赖场景/建筑标记 | reward_event_id 幂等 |
| 战斗中退出 | 未保存完整战斗态 | 明确回滚到回合开始或禁存 |
| 写入中断 | ConfigFile 直接保存 | 临时文件+原子替换 |

