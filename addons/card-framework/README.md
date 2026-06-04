# Card Framework 插件说明

这是一个用于 Godot 4.x 的 2D 卡牌游戏插件。项目目前把它作为卡牌拖拽、牌堆和手牌容器等基础能力的依赖。

## 插件能力

这个插件主要提供：

- 拖拽系统：内置卡牌拖拽和基础交互。
- 容器节点：支持牌堆 `Pile` 和手牌扇形 `Hand`。
- JSON 卡牌数据：用配置数据创建卡牌。
- 可扩展结构：通过工厂模式和继承扩展卡牌行为。

## 基本使用流程

1. 在场景中添加 `CardManager` 节点。
2. 配置 `JsonCardFactory`，指定卡牌数据和资源目录。
3. 编写 JSON 卡牌定义。
4. 添加 `Pile` 或 `Hand` 容器节点。

## 原始文档和示例

完整文档在插件原仓库：

https://github.com/chun92/card-framework

示例项目可以从 release 页面下载：

https://github.com/chun92/card-framework/releases/latest

下载时查找 `card-framework-vX.X.X-full.zip`。这个包包含基础 demo、FreeCell 示例、API 说明和教程。

## 版本

当前插件版本是 1.3.1，兼容 Godot 4.5 及以上版本。

## 许可证

MIT License，Copyright (c) 2025 Hyunjoon Park。
