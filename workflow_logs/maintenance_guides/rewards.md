# 奖励页维护说明

日期：2026-06-12

## 当前职责

`scene/in_scene/rewards/*.gd` 下的主脚本仍是奖励页 composition root。它们继续负责编排页面打开、卡牌展示、选择状态、确认提交、关闭流程和与局内牌组同步。

## 已拆模块

奖励页模块集中在 `scene/in_scene/rewards/`：

- `bridges/`：CardManager、牌组同步、商店卡池和全局节点查找。
- `factory/`：临时牌堆、真实卡牌、DraftCard 工厂、真实卡清理。
- `presenters/`：Craft/Remove/Shop 的 UI 展示、tooltip、选择状态、商品槽和飞行动画。
- `rules/`：Craft 配方、结果写入、Remove 删除处理、Shop 定价/刷新/升级/时代权重。
- `resources/`：Craft 配方、Shop 定价、Shop 时代权重默认资源。
- `diagnostics/`：Shop 调试日志。

## 不要继续硬拆

- `CraftReward.gd` 不要继续硬拆确认/关闭链或 `_create_preview_card()` 的异步编排。
- `ShopManager.gd` 不要重复拆定价、时代权重、生成依赖检查、临时牌堆隐藏创建或单个商品生成编排。
- `RemoveReward.gd` 和 `AcquireReward.gd` 剩余主要是页面编排，低优先级。

## 后续可做

只有新增奖励页公共生命周期或统一确认动画时，再评估跨页面模块。新增配方优先改 `default_craft_recipe_book.tres`，商店调参优先改默认 Resource。

## 验证入口

改动后按涉及页面加载对应场景，并检查奖励卡展示、tooltip、购买/领取/删除/合成后的飞入牌库和 deck_manager 同步。
