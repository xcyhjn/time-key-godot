# Unity 3D 迁移总待办

> 状态：Wave 02B2A 已完成；下一阶段为局内可维护性与解耦
> 负责人：主智能体
> 最后验证日期：2026-08-01
> 证据来源：迁移路线图、风险登记、首切片验收契约

## Wave 00：评估

- [x] 环境、分支、磁盘、版本控制和 Unity/Godot 工具预检。
- [x] Godot headless 与实机图形基线。
- [x] 源码、数据、依赖、存档、Shader/素材盘点。
- [x] 可行性、难度、3D 产品边界与迁移策略门禁。
- [x] 共享架构、数据、测试和所有权契约。

## Wave 01：首个可验证垂直切片

- [x] 创建 Unity 6000.4.10f1 工程并锁定 URP/uGUI/Test Framework。
- [x] 配置 scoped Unity ignore，保证缓存与 build 不入库。
- [x] 复制并校验 `lighting.json` fixture。
- [x] 实现纯 C# 卡牌、时间轴、伤害、意图和快照规则。
- [x] 完成 EditMode 测试。
- [x] 构建固定 seed 的 3D 六边形白盒场景。
- [x] 连通选卡→选目标→放置→结算→3D 状态更新。
- [x] 完成 PlayMode 测试、batchmode 验证与 Windows build。
- [x] 生成并审查 1920×1080、1280×720 截图。
- [x] 更新 parity/status/issues，精确提交并 push。

## 后续波次

- [x] Wave 02A：19 格局内棋盘、透视 360° 镜头、UI 输入互斥和四向选择验证。
- [x] Wave 02A：每层 `0.32` 的真实六边形 mesh/collider 堆叠。
- [x] Wave 02A：Blender 草地/裸土 FBX、可复现源文件、几何检查和 Unity 集成。
- [x] Wave 02A：原 `center_altar.png` 世界 billboard，四向可见且可选。
- [ ] Wave 02B：完整卡牌效果、敌方/建筑行动、胜负与奖励入口。
- [x] Wave 02B1：原 `lighting.png`/牌背接入，底部卡牌 idle/hover/selected/cancel 状态。
- [x] Wave 02B1：非变异时间轴 `CanPlace`、CardPlaySession 与 EditMode 失败路径。
- [x] Wave 02B1：3D `effect_range` 投影、单格时间轴 valid/invalid 预览与镜头输入互斥。
- [x] Wave 02B1：主场景接线、三视口/四向视觉证据、build/player smoke 与 Git 检查点。
- [x] Wave 02B2A：七卡异构 schema、七张原卡面与真实 fixture 解析。
- [x] Wave 02B2A：两卡手牌、`earthquake +2` 半径 1 范围、两格时间轴 shape 与真实 `0.32` 两层增量闭环。
- [x] Wave 02B2A：升高后 mesh/renderer/collider/top bounds/occupant anchor 同步，四向仍可选择。
- [ ] Wave 02B2A 后置门禁：把稳定 Camera/Canvas/HUD/Timeline/CardHand/Board 结构保存为可维护 Scene/Prefab，并冻结效果扩展接口与继承证据账本。
- [ ] Wave 02B2B：recover/poison/built 领域效果与最小局内表现；塔行动仍不接入。
- [ ] Wave 02B2C：wind/tornado 即时 clear 会话、完整 action 移除和专用时间轴预览。
- [ ] Wave 02B3：敌方/建筑行动、意图优先级/重判、塔自损与 poison 回合开始状态。
- [ ] Wave 02B4：抽弃牌、回合、时间币、胜负和局内奖励入口。
- [ ] Wave 03：暂停；局外地图和流程保持 Godot 现状。
- [ ] Wave 04：教程、中文字体、音频、VFX 和授权资产。
- [ ] Wave 05：平台冻结、性能预算、存档升级和发布构建。
