# 依赖与资产替换矩阵

> 状态：首切片依赖已决定；授权项待确认
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：addons 清单、项目引用、Unity 本机包目录、资产库存

| Godot 依赖/内容 | 使用面 | Unity 决策 | 首切片 | 风险 |
| --- | --- | --- | --- | --- |
| card-framework 1.3.1 | Card/Hand/Pile/Manager/Factory、奖励页 | 按可观察行为重写，不引入等价大插件 | 最小 CardDefinition/Deck/Timeline | 高；MIT 已确认 |
| Dialogic 2.0 Alpha | 教程 8 个直接使用文件 | 延后；先判断自研轻量对话或 Unity 包 | 不引入 | 功能低、授权高 |
| Godot Resource | 玩法和视觉配置 | 玩法→DTO/内容 SO；视觉→Prefab/Material/SO | 仅真实 JSON fixture | 中 |
| 24 Canvas shader | UI/hover/dim/game-over 等 | URP Shader Graph/HLSL/材质逐个重写或替代 | 只用 URP 默认材质 | 中 |
| AudioBus/SoundManager | 3 BGM、10 SFX、Music/SFX | Unity AudioMixer + 双音乐源/池化 SFX | 延后 | 素材授权待确认 |
| ARK Pixel 中文字体 | 至少 20 处引用 | 授权确认后导入；否则换已授权字体 | 不复制 | 高 |
| 2D PNG | 卡图、角色、地图、UI | 已授权项可作 UI/billboard/decal；最终 3D 分层替换 | 不批量复制 | 高 |
| ConfigFile CFG | 槽 0 与教程设置 | 版本化 JSON SaveEnvelope；可选一次性旧档导入器 | 只冻结接口 | 高 |
| Godot Signal/Autoload | 跨场景和全局状态 | C# event/接口/显式服务；UnityEvent 仅 Inspector 表现绑定 | 最小事件接口 | 中 |

## Unity 首批必要包

| 包 | 本机版本 | 用途 | 决策 |
| --- | --- | --- | --- |
| `com.unity.render-pipelines.universal` | 17.4.0 | 3D 白盒渲染、灯光和跨桌面一致性 | 引入并锁定 |
| `com.unity.ugui` | 2.0.0 | 运行时卡牌/时间轴/tooltip 的屏幕空间 UI | 引入并锁定 |
| `com.unity.test-framework` | 1.6.0 | EditMode/PlayMode | 引入并锁定 |

Input System、Cinemachine、Addressables、Shader Graph/VFX Graph 不在首切片引入；只有出现当前切片明确需求时再增加。

## 授权门禁

- 根仓库没有项目许可证或资产来源表。
- card-framework 根 LICENSE 为 MIT。
- Dialogic vendored 根没有顶层 LICENSE；示例字体/音效分别出现 Apache-2.0 与 CC BY-SA 4.0，不能据此推导整个插件或项目资产授权。
- 图片、13 个 OGG、ARK Pixel 字体在批量复制到 Unity 前必须由用户/项目方确认来源和发布授权。
