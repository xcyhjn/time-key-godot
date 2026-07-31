# Unity 3D 迁移总待办

> 状态：Wave 01 执行中
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：迁移路线图、风险登记、首切片验收契约

## Wave 00：评估

- [x] 环境、分支、磁盘、版本控制和 Unity/Godot 工具预检。
- [x] Godot headless 与实机图形基线。
- [x] 源码、数据、依赖、存档、Shader/素材盘点。
- [x] 可行性、难度、3D 产品边界与迁移策略门禁。
- [x] 共享架构、数据、测试和所有权契约。

## Wave 01：首个可验证垂直切片

- [ ] 创建 Unity 6000.4.10f1 工程并锁定 URP/uGUI/Test Framework。
- [ ] 配置 scoped Unity ignore，保证缓存与 build 不入库。
- [ ] 复制并校验 `lighting.json` fixture。
- [ ] 实现纯 C# 卡牌、时间轴、伤害、意图和快照规则。
- [ ] 完成 EditMode 测试。
- [ ] 构建固定 seed 的 3D 六边形白盒场景。
- [ ] 连通选卡→选目标→放置→结算→3D 状态更新。
- [ ] 完成 PlayMode 测试、batchmode 验证与 Windows build。
- [ ] 生成并审查 1920×1080、1280×720 截图。
- [ ] 更新 parity/status/issues，精确提交并 push。

## 后续波次

- [ ] Wave 02：完整卡牌效果、敌方/建筑行动、胜负与奖励入口。
- [ ] Wave 03：3D 局外地图、房间、奖励返回、跨场景上下文和存档。
- [ ] Wave 04：教程、中文字体、音频、VFX 和授权资产。
- [ ] Wave 05：平台冻结、性能预算、存档升级和发布构建。
