# 已知问题与待决项

> 状态：持续更新
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Godot 运行日志、源码侦察、授权盘点

| ID | 问题 | 影响 | 当前处理 |
| --- | --- | --- | --- |
| MIG-001 | Godot editor import 有大量 TileSet 错误，主场景退出还有清理错误 | 说明源工程基线并非干净，不能要求 Unity 复刻错误 | 日志保留；按可观察玩法对照 |
| MIG-002 | 敌人意图 command 解析当前返回空，建筑在整条时间轴后行动 | 设计意图与当前行为可能不同 | 首切片保留可观察顺序，Wave 02 前确认设计 |
| MIG-003 | 普通/精英房完成状态存在 TODO，事件场景路径缺失 | 局外闭环不可直接照搬 | Wave 03 单独修复/重设计，不阻塞首切片 |
| MIG-004 | Godot CFG 无 schema 且状态不完整 | 强兼容会明显增加工期 | Wave 03 前由用户选择兼容等级 |
| MIG-005 | 图片、音频、ARK Pixel 字体与 Dialogic vendored 根授权不完整 | 开发迁移不等于可直接公开发布 | 按用户要求在本地开发切片使用原 `center_altar.png`；发布前仍需逐项授权清单 |
| MIG-006 | 预检时可用内存低于 1 GiB | Unity 导入可能交换或超时 | 避免并行运行 Godot/Unity 图形实例；记录开发机性能基线 |
| MIG-007 | 图形基线操作可能更新 Godot 用户目录中的 slot 0 存档 | 外部用户数据发生可恢复的测试副作用 | 不把用户目录内容纳入迁移；后续基线使用专用 user data dir |
| MIG-008 | 首次 Unity batchmode 许可证检查返回 198 | 曾阻塞运行门禁 | 已解决；后续 Personal entitlement 检查成功，完整门禁通过 |
| MIG-009 | Codex 进程缺少 `ALLUSERSPROFILE` 时 Unity Package Manager 报 `path argument undefined` | 默认批处理命令无法解析包 | 每次命令使用任务局部 `$env:ALLUSERSPROFILE=$env:ProgramData`；命令目录已固化 |
| MIG-010 | 程序化材质只通过 `Shader.Find` 引用时，URP Lit 被 Player build 剥离 | 首次 Player 启动在 `Awake` 失败 | 将锁定包版本的 URP Lit 加入 Always Included Shaders；重建后 Player 冒烟通过 |
| MIG-011 | 首次 harness 经 ASCII junction 推导仓库根时，把 4 个证据文件写到 `D:\docs\migration\unity-3d\04-verification\evidence\unity-slice-01` | 仓库外留下可识别的一次性文件 | 未做不确定范围删除；harness 改为要求/验证 `TIMEKEY_REPOSITORY_ROOT`，正式证据已在仓库内重建 |
| MIG-012 | `git push` 警告当前 Git/GCM 的 TLS 证书校验被禁用 | HTTPS 远端连接缺少正常证书验证，存在供应链风险 | 本轮 push 成功但未擅自修改用户级配置；应由用户审查 Git/GCM 配置后恢复 TLS 校验 |
| MIG-013 | 固定 19 格验证棋盘上，目标 `(1,0)` 的 `lighting` 第三个范围 offset 落到不存在的 `(3,0)` | 集成截图只显示两个真实范围格；若误生成第三格会制造幽灵地块 | `BoardRangePreview.MissingCoordinates` 明确报告 `(3,0)`，不创建对象；完整地图/多卡波次继续按真实棋盘边界验证 |
| MIG-014 | 原卡面图片本身包含烘焙的棋盘格角部，且原素材授权仍未闭合 | Unity 不能通过导入设置恢复不存在的 alpha；公开发布仍有素材合规风险 | Wave 02B1 保持原文件字节与可观察外观，不擅自修图；Wave 04 统一处理授权和美术修订决策 |
