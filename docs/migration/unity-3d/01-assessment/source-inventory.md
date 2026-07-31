# Godot 源项目库存

> 状态：已验证
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：`rg --files` 统计、资源内容、Git LFS/大文件检查

## 库存摘要

| 分类 | 数量/规模 | 迁移含义 |
| --- | --- | --- |
| 非 addons 文件 | 575 | 只作为候选，不做全量迁移清单 |
| GDScript | 295 / 28,165 行 | 迁移行为和数据所有权，不逐行翻译 |
| 场景 | 38 `.tscn` | composition root 和导出字段需要分类 |
| Resource | 16 `.tres` | 玩法配置、视觉配置、Theme、Dialogic、AudioBus 混合 |
| Shader | 24 | 全部 `canvas_item`；17 个有明确引用 |
| 卡牌 JSON | 7 | 字段集一致，适合先冻结 DTO/golden fixture |
| 非插件媒体 | 165 / 约 40 MB | 149 PNG、13 OGG、2 JPG、1 OTF |

## 卡牌事实

- 稳定内容 ID 是 JSON 文件 basename/`name`；数字 `id` 主要用于校验和排序。
- `lighting` 是现有稳定拼写，不能擅自改成 `lightning`。
- 数字 ID 为 `1,2,4,5,6,7,8`，唯一但稀疏。
- 字段：`name`、`front_image`、`效果`、`effects`、`effect_range`、`shape`、`时代`、`id`。
- `shape` 用 `1` 表示占格并裁到最小包围盒；`wind/tornado` 的 clear 形状来自 effect value，是即时清除特例。
- 首个真实 fixture：`card_data/lighting.json`，SHA-256 `7DEC6590C409E3A5C9BB2E83B8E833FC1E6D9106EF5706E223F909D1E763FB0B`。

## 配置分类

- 玩法：合成配方、商店时代权重、价格配置。
- 表现：时间轴视觉、Hurt/Tile VFX、Tooltip、Theme/StyleBox。
- 引擎适配：AudioBus、DialogicStyle、场景导出字段。

**决策**：玩法数据转换为版本化 JSON/纯 DTO 或内容 ScriptableObject；运行时状态不放入 ScriptableObject。纯视觉配置留在 Unity Prefab/Material/ScriptableObject。

## 不迁移或延后

- `.godot/`、编辑器生成物、导入缓存、历史日志。
- Godot 插件源码本体；只迁移已确认的行为契约。
- 未引用 shader/资产在动态引用审查前不删除、不复制。
- 来源不明的字体、图片、音频在授权确认前不进入 Unity 发布资产。

## 已知库存异常

- `event_scene` 指向不存在的 `res://event.tscn`。
- 教程固定牌组引用不存在的 `1.json/2.json/3.json`。
- `GlobalDB.ICONS` 两个路径指向不存在的 `res://图片/`。
- 根仓库无项目 LICENSE；ARK Pixel 字体和项目自有媒体无授权清单。
