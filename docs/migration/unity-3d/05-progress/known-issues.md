# 已知问题与待决项

> 状态：持续更新
> 负责人：主智能体
> 最后验证日期：2026-08-03
> 证据来源：Godot 运行日志、源码侦察、授权盘点

| ID | 问题 | 影响 | 当前处理 |
| --- | --- | --- | --- |
| MIG-001 | Godot editor import 有大量 TileSet 错误，主场景退出还有清理错误 | 说明源工程基线并非干净，不能要求 Unity 复刻错误 | 日志保留；按可观察玩法对照 |
| MIG-002 | Godot 敌人意图 command 解析当前返回空 | 不能声称已实现真实敌人攻击效果 | 02B3 已完成确定性生成、共享 snapshot、最终重判和 `UnsupportedSourceCommand` 显式 no-effect；真实 typed enemy effect 留给有权威数据的后续内容波次 |
| MIG-003 | 普通/精英房完成状态存在 TODO，事件场景路径缺失 | 局外闭环不可直接照搬 | Wave 03 单独修复/重设计，不阻塞首切片 |
| MIG-004 | Godot CFG 无 schema 且状态不完整 | 强兼容会明显增加工期 | Wave 03 前由用户选择兼容等级 |
| MIG-005 | 图片、音频、ARK Pixel 字体与 Dialogic vendored 根授权不完整 | 开发迁移不等于可直接公开发布 | 按用户要求在本地开发切片使用原 `center_altar.png`；发布前仍需逐项授权清单 |
| MIG-006 | 预检时可用内存低于 1 GiB | Unity 导入可能交换或超时 | 避免并行运行 Godot/Unity 图形实例；记录开发机性能基线 |
| MIG-007 | 图形基线操作可能更新 Godot 用户目录中的 slot 0 存档 | 外部用户数据发生可恢复的测试副作用 | 不把用户目录内容纳入迁移；后续基线使用专用 user data dir |
| MIG-008 | 首次 Unity batchmode 许可证检查返回 198 | 曾阻塞运行门禁 | 已解决；后续 Personal entitlement 检查成功，完整门禁通过 |
| MIG-009 | Codex 进程缺少 `ALLUSERSPROFILE` 时 Unity Package Manager 报 `path argument undefined` | 默认批处理命令无法解析包 | 每次命令使用任务局部 `$env:ALLUSERSPROFILE=$env:ProgramData`；命令目录已固化 |
| MIG-010 | 程序化材质只通过 `Shader.Find` 引用时，URP Lit 被 Player build 剥离 | 首次 Player 启动在 `Awake` 失败 | 将锁定包版本的 URP Lit 加入 Always Included Shaders；重建后 Player 冒烟通过 |
| MIG-011 | 首次 harness 经 ASCII junction 推导仓库根时，把 4 个证据文件写到 `D:\docs\migration\unity-3d\04-verification\evidence\unity-slice-01` | 仓库外留下可识别的一次性文件 | 未做不确定范围删除；harness 改为要求/验证 `TIMEKEY_REPOSITORY_ROOT`，正式证据已在仓库内重建 |
| MIG-012 | `git push` 警告当前 Git/GCM 的 TLS 证书校验被禁用 | HTTPS 远端连接缺少正常证书验证，历史上曾遇到 443 reset/timeout | 当前阶段各检查点结果记录在 `push-status.md`；未擅自修改用户级配置，应由用户审查 Git/GCM 配置后恢复 TLS 校验 |
| MIG-013 | 固定 19 格验证棋盘上，目标 `(1,0)` 的 `lighting` 第三个范围 offset 落到不存在的 `(3,0)` | 集成截图只显示两个真实范围格；若误生成第三格会制造幽灵地块 | `BoardRangePreview.MissingCoordinates` 明确报告 `(3,0)`，不创建对象；完整地图/多卡波次继续按真实棋盘边界验证 |
| MIG-014 | 原卡面图片本身包含烘焙的棋盘格角部，且原素材授权仍未闭合 | Unity 不能通过导入设置恢复不存在的 alpha；公开发布仍有素材合规风险 | Wave 02B1 保持原文件字节与可观察外观，不擅自修图；Wave 04 统一处理授权和美术修订决策 |
| MIG-015 | Unity `JsonUtility` 会静默强制转换异构 `value`，不能可靠区分 number/string/array | 七卡 schema 可能接受错误类型或丢失 clear mask | 已用 Unity 官方 `com.unity.nuget.newtonsoft-json 3.2.2` 做结构化 token 类型校验，决定记录在 ADR-0003；不使用 regex 或 fixture 改写 |
| MIG-016 | R2 Controller 曾解析两张 fixture、加载 Resources 卡图并按两卡刷新 hand | 已解决：共享 Controller 不再是新增普通卡的路由点，`Presentation -> Infrastructure` 临时依赖已移除 | R3 由 `CardContentCatalog`/effect registry 提供七卡内容边界，`CombatCompositionRoot` 统一装配；扩展测试已通过 |
| MIG-017 | R2 trace 曾缺少 effect kind 与 before/after | 已解决：结算日志可直接定位 effect 与值变化 | R3 已扩展 `CombatTraceEntry` 并加入可关闭 Unity sink；sink 中立性与 before/after 测试已通过 |
| MIG-018 | Unity 曾只有无效果的固定 enemy intent marker | 已解决固定 marker 与跨层映射问题；仍无新增敌人内容 | 02B3 已替换为确定性 source catalog/application service、frame/tooltip/map 映射和 lifecycle refresh；新增敌人按 `06-maintenance/add-enemy.md` 登记 |
| MIG-019 | 02B4 自动化截图中选中卡抬升会局部遮住相邻卡上缘 | 视觉密度略高，但关键标题/效果与点击边界仍可读，未发生容器或视口裁切 | 作为非阻塞视觉观察保留；后续修改 CardHandHost 布局时必须重拍三视口并逐图复核 |
| MIG-020 | D3D12 Player 未提供有效的 `Draw Calls Count`/`Batches Count` recorder，三次内存原始样本严格递增 | 无法直接给出 draw-call 计数；短样本也不能证明不存在长期泄漏 | smoke 显式记录 `SetPass Calls Count=18` 为 render counter；总增量 412,086 bytes，material/sustained-slope 均为 false，post-GC 门禁通过；长期结论仍需 Profiler |

Tower decay、Poison 传播/伤害/减层、action identity 映射、02B4 牌库/资源/终局和 Combat Shell Gate E 均已关闭。当前没有阻塞 Wave 03 的产品或环境问题；MIG-002 仍只限制“无权威 command 时不得发明敌人伤害”。MIG-019 与 MIG-020 均为非阻塞观察。
