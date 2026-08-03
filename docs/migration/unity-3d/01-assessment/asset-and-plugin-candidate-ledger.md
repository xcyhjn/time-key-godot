# 插件与资产候选账本

> 审计日期：2026-08-03（Asia/Shanghai）
>
> 状态：P0 只读候选登记；本阶段未下载、安装、导入、执行或替换任何候选
>
> Unity：6000.4.10f1
>
> 本地资产清单摘要：402 个内容文件，路径与 SHA-256 排序后的清单摘要为 `D2291C287353366A5D1A29D94C7A3827CFD11DD20ED58F4A7856F5FEFBDA8C00`

## 状态定义

| 状态 | 含义 |
| --- | --- |
| `ACCEPT` | 已有证据允许在限定范围内使用；仍须遵守加入、验证和移除门禁 |
| `KEEP_LOCAL` | 既有本地迁移资产可继续用于当前开发/等价验证，但公开发布授权尚未放行 |
| `DEFER` | 有潜在收益，但当前切片收益不足或回归面过大；不进入正式工程 |
| `UNVERIFIED` | 来源、作者、许可、依赖或字节证据至少一项无法核验；不得新增导入或公开发布 |
| `REJECT` | 当前证据不满足准入；不得下载、安装、导入或执行 |

任何 `DEFER`/`UNVERIFIED` 候选都不是安装授权。联网检索只产生记录；只有主智能体能在独立 checkpoint 中修改 `manifest.json`、`packages-lock.json`、asmdef、正式 Scene/Prefab 或 ProjectSettings。

## 快速决策表

| ID | 名称/类别 | 状态 | 当前决定 |
| --- | --- | --- | --- |
| P-001 | UI Toolkit / Editor UI | `ACCEPT` | 只用于 Editor-only 工具；现有运行时 uGUI 不迁移 |
| P-002 | Input System / Unity package | `DEFER` | 工具阶段无收益，旧输入与交互契约冻结 |
| P-003 | Cinemachine / Unity package | `DEFER` | 现有 360 度棋盘镜头冻结 |
| P-004 | Addressables / Unity package | `DEFER` | 当前两处 `Resources.Load` 不足以证明迁移收益 |
| P-005 | Timeline / Unity package | `DEFER` | 当前无使用；以后也只能驱动表现 |
| P-006 | 未指名第三方工具/插件 | `REJECT` | 无确定来源、版本、许可、依赖和卸载证据 |
| P-007 | Godot card-framework 1.3.1 | `KEEP_LOCAL` | 保留为 Godot 源语义参考；不把整套插件移植到 Unity |
| P-008 | Godot Dialogic 2.0 Alpha 19 | `REJECT` | vendored 根许可缺失；示例许可不能覆盖整个插件 |
| A-001 | Silver 像素字体 | `ACCEPT` | Unity 中文 UI 的唯一正式字体；必须署名并复核 10 万美元门槛 |
| A-002 | 七张原卡面 | `KEEP_LOCAL` | 字节一致迁移；公开发布前必须补来源和许可 |
| A-003 | Godot/Unity 图片语料 | `UNVERIFIED` | 现有 41 张 Unity 副本保持；禁止继续批量复制 |
| A-004 | Godot 音频语料 | `UNVERIFIED` | 13 OGG 均未复制到 Unity；补许可前拒绝导入 |
| A-005 | ARK Pixel 字体 | `REJECT` | 字节可核验、许可不可核验；不得替换 Silver |
| A-006 | Godot Canvas shaders | `DEFER` | 仅作视觉语义参考；不得原样导入 Unity |
| A-007 | 六边形 Blender/FBX 产物 | `KEEP_LOCAL` | 可复现、低复杂度；发布权属仍需项目方确认 |
| A-008 | Unity 卡背 `behide.png` | `UNVERIFIED` | 当前静态扫描无引用；保留但不得当作已确认可删 |

## 插件候选

### P-001 UI Toolkit（Editor-only）

- 名称/类别/状态：UI Toolkit，Editor UI，`ACCEPT`。
- 来源 URL：<https://docs.unity3d.com/6000.0/Documentation/Manual/UIElements.html>、<https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-support-for-editor-ui.html>。
- 作者/检索日期/版本：Unity Technologies；2026-08-03；工程已直引内建 `com.unity.modules.uielements@1.0.0`。
- 许可证与全文 URL：Unity Engine 内建模块；受有效 Unity Engine License/Unity 条款约束，<https://unity.com/legal/terms-of-service/software>。
- 商用/再分发：可随有效 Unity 项目使用；不得把内建模块作为独立产品再分发。
- Unity 兼容性/依赖：Unity 6000.4.10f1 已安装；本机声明依赖 `ui`、`imgui`、`jsonserialize`、`hierarchycore`、`physics@1.0.0`。
- 二进制风险/SHA-256：不增加第三方二进制；`N/A - 本阶段未下载，使用既有引擎模块`。
- 导入路径/Importer/GUID：无需导入；既有 manifest direct dependency；Editor 代码只放 `Assets/_Project/Editor/**` 的 Editor-only asmdef；无单一资产 GUID。
- 性能/视觉影响：只增加 EditorWindow 布局、列表和重绘成本；正确隔离时对 Player 性能和游戏画面为零。EditorWindow 仍需三种实际尺寸检查。
- 加入路径：不改 manifest/lock；仅新增 Editor-only assembly、菜单/EditorWindow、结构化诊断和独占 EditMode 测试。
- 移除/回滚：删除工具自有 C#/UXML/USS、菜单入口和专用测试；保留阶段前已存在的 `uielements` direct dependency；重跑 EditMode、build 与 Player smoke。
- 拒绝原因：不适用；若实现要求迁移运行时 uGUI、Scene 或输入系统，则该实现方案直接拒绝。

### P-002 Input System

- 名称/类别/状态：`com.unity.inputsystem`，输入包，`DEFER`。
- 来源 URL：<https://packages.unity.com/com.unity.inputsystem>、<https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html>。
- 作者/检索日期/版本：Unity Technologies；2026-08-03；候选 `1.20.0`，package metadata 要求 Unity `6000.0+`。
- 许可证与全文 URL：Unity Companion License v1.4，<https://unity.com/legal/licenses/unity-companion-license>。
- 商用/再分发：有效 Unity Engine License 下可用于商业 Unity 内容；须保留许可/third-party notices，不得脱离 Unity-dependent content 单独再分发。
- Unity 兼容性/依赖：版本元数据兼容 Unity 6000.4；依赖 `com.unity.modules.uielements@1.0.0`。本工程 `activeInputHandler: 0`，仍使用旧 Input Manager、`StandaloneInputModule` 和 7 个直接 `UnityEngine.Input` 调用文件。
- 二进制风险/SHA-256：官方 registry，供应链风险较低；包体和平台后端未下载/未检查，`N/A - not downloaded`。Registry SHA-1 不能替代准入所需 SHA-256。
- 导入路径/Importer/GUID：未来只能通过 UPM cache + manifest/lock；`.inputactions` 建议放 `Assets/_Project/Input/**`；本阶段无 Importer/GUID。
- 性能/视觉影响：设备/action 事件、GC、输入延迟及热插拔需 profiling；切换 UI module 可能改变点击、焦点、导航、Escape、右键与全局输入锁。
- 加入路径：独立试验 checkpoint，固定版本，审查 lock/许可/二进制，显式决定 Active Input Handling，逐一迁移旧输入与 EventSystem，并完成键鼠/手柄/触控 PlayMode、三视口、build 和 Player smoke。
- 移除/回滚：恢复 `StandaloneInputModule`、旧输入代码和 ProjectSettings；删除 `.inputactions`/生成代码；从 manifest 移除 direct package，仅清理无人依赖的 transitive；重跑全交互验证。
- 拒绝/延期原因：当前 Editor 工具不消费运行时输入，引入只扩大稳定交互回归面。

### P-003 Cinemachine

- 名称/类别/状态：`com.unity.cinemachine`，相机包，`DEFER`。
- 来源 URL：<https://packages.unity.com/com.unity.cinemachine>、<https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html>。
- 作者/检索日期/版本：Unity Technologies；2026-08-03；候选 `3.1.7`，最低 Unity `2022.3`。
- 许可证与全文 URL：Unity Companion License v1.4，<https://unity.com/legal/licenses/unity-companion-license>。
- 商用/再分发：可用于商业 Unity 内容；保留许可/third-party notices，不独立再分发包。
- Unity 兼容性/依赖：元数据兼容当前 Unity；依赖 `com.unity.splines@2.0.0`、`com.unity.modules.imgui@1.0.0`。
- 二进制风险/SHA-256：官方 registry；未下载、未审查可选集成，`N/A - not downloaded`。
- 导入路径/Importer/GUID：UPM cache + manifest/lock；获批后会在 Camera Scene/Prefab 序列化 Brain/Camera/Extension；当前无 GUID。
- 性能/视觉影响：逐帧构图、阻尼和碰撞会增加 CPU，并直接改变右键旋转、中键平移、滚轮、俯仰/距离限制及宽屏 framing。
- 加入路径：只在隔离试验 Scene 建立现有 `BoardOrbitCameraController` 等价性、性能和四 yaw/三视口基线，获批后才可改正式 Camera。
- 移除/回滚：恢复原 Camera/Controller serialized 值，删除所有 Cinemachine components/assets，从 manifest/lock 移除 Cinemachine 和无人使用的 Splines，重跑全部相机交互、build 和 Player smoke。
- 拒绝/延期原因：当前 360 度棋盘镜头已稳定，本阶段明确冻结该契约。

### P-004 Addressables

- 名称/类别/状态：`com.unity.addressables`，资产加载/构建包，`DEFER`。
- 来源 URL：<https://packages.unity.com/com.unity.addressables>、<https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html>。
- 作者/检索日期/版本：Unity Technologies；2026-08-03；候选 `3.1.0`，要求 Unity `6000.0+`。
- 许可证与全文 URL：Unity Companion License v1.4，<https://unity.com/legal/licenses/unity-companion-license>。
- 商用/再分发：可用于商业 Unity 内容；保留包许可/third-party notices；远程内容仍须逐资产满足许可。
- Unity 兼容性/依赖：元数据兼容当前 Unity；依赖 `profiling.core@1.0.2`、`test-framework@1.4.5`、`scriptablebuildpipeline@3.1.1` 及多个 built-in 模块。
- 二进制风险/SHA-256：官方 registry，但引入内容构建管线和多项 transitive；全部未下载/未检查，`N/A - not downloaded`。
- 导入路径/Importer/GUID：UPM cache + manifest/lock，初始化通常生成 `Assets/AddressableAssetsData/**` 和内容构建输出；当前无 GUID。
- 性能/视觉影响：catalog/bundle 增加构建时间与缓存复杂度；错误分组可产生重复依赖、首载等待、峰值内存或缺失卡面。
- 加入路径：先定义资源规模/内存/远程交付阈值；隔离 fixture 固定版本，审查全部 transitive/license，验证 analyze/build layout、冷/热加载、失败恢复与 Player profiling；不得首试验迁移正式卡面或 SceneFlow。
- 移除/回滚：恢复 serialized/Resources 路径；删除仅由试验创建的 groups/profiles/catalog/output；移除 direct package 和无人依赖的 transitive；重建 Player 并验证全部卡面。
- 拒绝/延期原因：当前运行时只有两处卡面 `Resources.Load`，收益不足以承担内容管线迁移。

### P-005 Unity Timeline

- 名称/类别/状态：`com.unity.timeline`，表现编排包，`DEFER`。
- 来源 URL：<https://packages.unity.com/com.unity.timeline>、<https://docs.unity3d.com/Packages/com.unity.timeline@1.8/manual/index.html>。
- 作者/检索日期/版本：Unity Technologies；2026-08-03；候选 `1.8.12`，最低 Unity `2022.3`。
- 许可证与全文 URL：Unity Companion License v1.4，<https://unity.com/legal/licenses/unity-companion-license>。
- 商用/再分发：可用于商业 Unity 内容；保留许可/third-party notices，不独立再分发包。
- Unity 兼容性/依赖：元数据兼容当前 Unity；依赖 built-in `audio`、`director`、`animation`、`particlesystem@1.0.0`。当前只存在低层 `director` 模块，不等于安装 Timeline authoring package。
- 二进制风险/SHA-256：官方 registry；包体未下载/未审查，`N/A - not downloaded`。
- 导入路径/Importer/GUID：UPM cache + manifest/lock；未来 TimelineAsset 只能进入明确 Presentation 目录，绑定保存在 Scene/Prefab；当前无 GUID。
- 性能/视觉影响：活动 Director 评估 Playables graph；直接影响动画、音频、镜头时序，需验证宽屏、跳过、Scene rebind 与 timescale。
- 加入路径：隔离表现 fixture 中验证无玩法 side effect 的 clip；任何卡牌提交、敌人行动、回合推进和 outcome 仍由 Application/Domain typed contract 产生。
- 移除/回滚：恢复 Animator/代码表现入口与 Prefab bindings；删除 TimelineAsset/PlayableDirector/custom tracks；移除包并重跑动画、SceneFlow、build 和 Player smoke。
- 拒绝/延期原因：当前零 `PlayableDirector`/`TimelineAsset`/Timeline API 使用，尚无真实需求。

### P-006 未指名第三方 DI/Tween/Inspector/资产工具

- 名称/类别/状态：未指名第三方工具集合，`REJECT`。
- 来源 URL/作者/检索日期/版本：`UNVERIFIED`；没有具体候选，检索日期 2026-08-03。
- 许可证与全文 URL/商用/再分发：`UNVERIFIED`；不推断。
- Unity 兼容性/依赖：`UNVERIFIED`。
- 二进制风险/SHA-256：来源和依赖不透明，风险不可接受；`N/A - not downloaded`。
- 导入路径/Importer/GUID：无；禁止写入 `Assets/Plugins`、`Packages` 或执行安装脚本。
- 性能/视觉影响：未知。
- 加入路径：无；必须先拆为具体名称、官方来源、exact version、许可、依赖、二进制哈希和隔离试验，重新进入登记。
- 移除/回滚：无导入，因此当前回滚为零变更。
- 拒绝原因：不满足最小可审计、可复现、可卸载条件。

### P-007 Godot card-framework 1.3.1

- 名称/类别/状态：Godot 2D card framework，`KEEP_LOCAL`，仅作源语义参考。
- 来源 URL：<https://github.com/chun92/card-framework>；仓库内 `addons/card-framework/README.md` 与 `LICENSE`。
- 作者/检索日期/版本：Hyunjoon Park；2026-08-03；`1.3.1`，README 声明 Godot 4.5+。
- 许可证与全文 URL：MIT，<https://github.com/chun92/card-framework/blob/main/LICENSE>；本地许可证版权为 Copyright (c) 2025 Hyunjoon Park。
- 商用/再分发：MIT 允许商业使用、修改和再分发；分发副本/重要部分必须保留版权和许可文本。
- Unity 兼容性/依赖：Godot GDScript/Scene，不兼容 Unity 运行时；Unity 已按可观察行为实现 typed card/deck/hand，不应包装或翻译整插件。
- 二进制风险/SHA-256：vendored source，无新增二进制；候选本阶段未下载，完整目录未登记单一包哈希。
- 导入路径/Importer/GUID：保留 `addons/card-framework/**`；禁止导入 Unity；无 Unity Importer/GUID。
- 性能/视觉影响：仅作为 Godot 行为参考时对 Unity 为零；整包移植会重复造轮子并扩大输入/UI 回归面。
- 加入路径：不加入 Unity；需要新卡牌语义时只读取可观察行为并在现有 Domain/Application 扩展点实现最小差异。
- 移除/回滚：不得在本阶段删除 Godot 依赖；Unity 无包可移除。
- 拒绝/保留原因：许可完整，但技术栈不兼容，整体移植不产生可维护收益。

### P-008 Godot Dialogic 2.0 Alpha 19

- 名称/类别/状态：Dialogic，Godot 对话插件，`REJECT`（当前 Unity 候选）。
- 来源 URL：<https://github.com/dialogic-godot/dialogic>；本地 `addons/dialogic/plugin.cfg`。
- 作者/检索日期/版本：Jowan、Emi、Cake 等；2026-08-03；`2.0-Alpha-19 (Godot 4.4+)`。
- 许可证与全文 URL：vendored 根没有可确认的顶层 LICENSE，`UNVERIFIED`。示例字体的 Apache-2.0 和 Tim Krief 示例音效的 CC BY-SA 4.0 只覆盖各自示例，不能外推到插件根。
- 商用/再分发：`UNVERIFIED`；不允许据示例许可推导整个插件可商用/再分发。
- Unity 兼容性/依赖：Godot EditorPlugin/GDScript，不兼容 Unity；项目教程存在使用面，但当前 Unity 切片无对话需求。
- 二进制风险/SHA-256：vendored source/示例资产较多；根许可和完整依赖边界不透明；本阶段未下载，无候选包 SHA-256。
- 导入路径/Importer/GUID：保留 Godot `addons/dialogic/**`；禁止导入 Unity；无 Unity GUID。
- 性能/视觉影响：若整包替换会引入大型 authoring/runtime 系统并改变教程 UI/文本时序；当前不评估。
- 加入路径：无。未来必须先确认官方 exact tag 与根许可，再比较轻量 typed dialogue 数据或 Unity 原生实现。
- 移除/回滚：本阶段不删除 Godot vendored 插件；Unity 无导入可回滚。
- 拒绝原因：根许可缺失、Alpha 版本、技术栈不兼容、当前切片无需求。

## 资产候选

### A-001 Silver 像素字体

- 名称/类别/状态：Silver pixel font，字体，`ACCEPT`。
- 来源 URL：<https://poppyworks.itch.io/silver>。
- 作者/检索日期/版本：Poppy Works / Wolfgang Wozniak；2026-08-03；仓库未记录上游版本号，当前字节视为锁定版本。
- 许可证与全文 URL：CC BY 4.0，<https://creativecommons.org/licenses/by/4.0/>；项目署名位于 `Resources/Fonts/Silver-ATTRIBUTION.txt`。
- 商用/再分发：CC BY 4.0 条件下可商用/再分发并须署名；官方来源另要求总制作预算或总收入超过 USD 100,000 时联系 `hello@poppy.works` 取得直接许可，发布前须复核当前条款。
- Unity 兼容性/依赖：Unity `TrueTypeFontImporter`；无运行时第三方包依赖。
- 二进制风险/SHA-256：单一 TTF，无可执行代码；`7ADCF56D93142DED08FF18196F78FCAAF552411B9A94DC559DAC90EB5C91CEF1`，3,740,480 bytes。
- 导入路径/Importer/GUID：`Assets/_Project/Resources/Fonts/Silver.ttf`；fontSize 16、includeFontData 1、font name `Silver`；GUID `35b5b371d76876b4f8b26cd2376eb08c`。
- 性能/视觉影响：字体数据进入 Player；所有可见中文 Text/TextMesh 必须继续使用 Silver，TextMesh 同时绑定 font material，避免 tofu/不可见字形。
- 加入路径：已在正式工程；新增 UI 只引用同一 Font/GUID，不复制第二份字体；build 复制署名文件。
- 移除/回滚：只有获批替代字体、完成全量中文覆盖/视觉/build/Player 证据并更新署名后才可移除；删除前反查全部 serialized Font/material 引用。
- 拒绝原因：不适用；任何无署名副本或未复核预算门槛的发布产物直接拒绝。

### A-002 七张原卡面

- 名称/类别/状态：`earthquake`、`lighting`、`poison_card`、`recover`、`tornado`、`tower_card`、`wind` PNG，`KEEP_LOCAL`。
- 来源 URL/作者/检索日期/版本：来源 URL 与作者均 `UNVERIFIED`；2026-08-03；Godot `card_asset/**` 当前字节为迁移锁定版本。
- 许可证与全文 URL/商用/再分发：项目未提供许可或全文 URL，公开发布 `UNVERIFIED`；只能继承当前本地开发/等价验证用途，不能据“已在仓库”推导商用或再分发权。
- Unity 兼容性/依赖：PNG 可由 Unity 6000.4 导入；卡牌 JSON `front_image` 与 stable ID 是唯一内容映射，保留历史拼写 `lighting`。无新 package。
- 二进制风险/SHA-256：纯图片，无可执行代码；七组 Godot 源与 Unity 副本逐字节相同：

  | 文件 | SHA-256 | Unity GUID | Importer |
  | --- | --- | --- | --- |
  | `earthquake.png` | `E03EC4E9DAA4DA9BB26F4EF80803B0CB9F506497C777560B80567F773C6463F1` | `f527a20a8fcb4257a2c6797b595a4d68` | Sprite/Single, Point, uncompressed |
  | `lighting.png` | `DD27CCC0F0A9982286958D130E73E106DF309B1A116DA68139E30FA398ACE3DC` | `4de89fa029e12f94180ff9364ed55f14` | Default Texture, Point, uncompressed |
  | `poison_card.png` | `936B2BE26879462D0A18E1F0F5867E3681461A744E73E56823E3B1E8AEDCB879` | `4298bec2513245d39622fb9b048857cc` | Sprite/Single, Point, uncompressed |
  | `recover.png` | `F9065BC4FD646D8EEFC463FBC35798EDDDDDE489C4214F6333ACCC6E54A23D99` | `8c68f1759d704008a12bc05a5538def3` | Sprite/Single, Point, uncompressed |
  | `tornado.png` | `B369DCC027670C4AA28E003FC61B97DCE5DB077B982AEE6BAFDC84766B00E564` | `6a77ee2982e246d7bcd14b153374e8bb` | Sprite/Single, Point, uncompressed |
  | `tower_card.png` | `73BFFC6162D4D0D09D196835E3AC1BE5D40FEE19BC6C5BC391008386471AF05B` | `10a8065097cf4488b10adc4464b645a5` | Sprite/Single, Point, uncompressed |
  | `wind.png` | `BFCDA4C0A872FE327EFDFB2FF6880E4A343CC176484CCE5BEE0BC13C39DDB498` | `e430ea98fe4844d2979ac76e45990ced` | Sprite/Single, Point, uncompressed |

- 导入路径/Importer/GUID：源 `card_asset/<file>`；副本 `Assets/_Project/Resources/Art/Battle/Cards/<file>`。每张 1135×1590、非 2 的幂、maxTextureSize 2048、Point、平台 compression 0；GUID 如上。`lighting` 由 runtime Texture2D fallback 生成 Sprite，当前行为已测试，不能静默修改 Importer。
- 性能/视觉影响：七张源文件约 6.26 MiB；RGBA32 未压缩的理论 GPU 占用约 48.2 MiB（不含 mip，实际以 Player profiler 为准）。修改压缩、alpha、filter 或尺寸会直接改变中文烘焙卡面和像素边缘。
- 加入路径：现有七张保持不变；新卡先补来源/作者/许可、SHA-256、JSON/schema 和隔离 Importer 试验，再由主智能体接入。
- 移除/回滚：恢复 JSON `front_image` 与 catalog 映射后才能删除；验证手牌、详情框、Timeline identity、三视口、build/Player；优先 revert 单一资产 checkpoint。
- 拒绝/保留原因：本地等价迁移已有可信证据；公开发布许可缺失，禁止扩张使用范围。

### A-003 Godot/Unity 图片语料

- 名称/类别/状态：Godot 图片、Unity 已复制战斗/壳图片，`UNVERIFIED`。
- 来源 URL/作者/检索日期/版本：大多数条目没有来源 URL/作者；2026-08-03；仓库当前字节为基线。
- 许可证与全文 URL/商用/再分发：除 Silver 外，根仓库没有统一资产来源/许可表；图片商用和再分发均 `UNVERIFIED`。
- Unity 兼容性/依赖：项目范围扫描包含 228 PNG、2 JPG、1 ICO（含 addons/scene 输入）；Unity `_Project` 有 41 PNG，均能找到字节相同 Godot 源。依赖现有 TextureImporter、uGUI/URP material，无新 package。
- 二进制风险/SHA-256：纯图片，无执行风险；全部图片包含在清单摘要 `D229...8C00`。Unity 41 张 PNG 的逐文件 SHA-256 均与 Godot 对应路径一致；没有发现被重新编码的迁移副本。
- 导入路径/Importer/GUID：Godot 主要位于 `image/**`、`card_asset/**`；Unity 位于 `Assets/_Project/Resources/Art/**`，各 `.meta` 固定 GUID。不得批量重导或重写 `.meta`。
- 性能/视觉影响：`image/**` 的 142 张 PNG/JPG 中 56 张非 2 的幂、86 张为 2 的幂；UI 原图非 POT 不自动构成缺陷，但导入前必须记录 max size、压缩、alpha、filter 和实际 GPU/视觉结果。
- 加入路径：逐资产登记来源、作者、许可、用途、哈希和目标 Importer；隔离复制后跑 CardAssetAudit/SceneContract、三视口、build/Player，再进入单一 checkpoint。
- 移除/回滚：先反查 GUID、Resources 字符串和 Scene/Prefab/Material 引用；恢复源映射并删除目标+meta；重跑受影响 Scene/视觉/build/Player。不得用目录级删除推断孤立。
- 拒绝原因：授权缺口未关闭；当前只允许保持现状，禁止继续批量复制或发布放行。

### A-004 Godot 音频语料

- 名称/类别/状态：3 BGM + 10 SFX OGG/Vorbis，`UNVERIFIED`。
- 来源 URL/作者/检索日期/版本：项目未记录来源 URL、作者或版本；2026-08-03。
- 许可证与全文 URL/商用/再分发：`UNVERIFIED`；Dialogic 示例音效的 CC BY-SA 4.0 不覆盖项目 `audio/**`。
- Unity 兼容性/依赖：13 个文件均为 48 kHz、2 声道 Vorbis；时长 0.473-93.600 秒；Unity 可导入，但当前 `_Project` 没有 OGG/WAV/MP3，不能在缺许可时复制。
- 二进制风险/SHA-256：纯音频，无可执行代码；每个文件 SHA-256 已在 Agent 报告登记，组内无重复哈希。
- 导入路径/Importer/GUID：源 `audio/bgm/**`、`audio/sfx/**`；Unity 目标、Importer 与 GUID 均 `N/A - 未导入`。
- 性能/视觉影响：总源文件约 11.38 MiB；BGM streaming/decompress-on-load、SFX load type、双音乐源/池化策略都需在获批导入时单独 profiling；不产生直接视觉影响。
- 加入路径：先补逐文件来源/作者/许可和 attribution；再在隔离目录设定 AudioImporter/load type/compression，接 AudioMixer，跑听感、内存、build 和 Player smoke。
- 移除/回滚：解除 AudioClip/AudioMixer/serialized refs，删除目标+meta 和 attribution，确认没有 Resources/Addressables key，重建 Player。
- 拒绝原因：许可和来源缺失；当前禁止导入。

### A-005 ARK Pixel 中文字体

- 名称/类别/状态：`ark-pixel-12px-proportional-zh_cn.otf`，字体，`REJECT`（Unity 导入）。
- 来源 URL/作者/检索日期/版本：仓库没有可核验来源 URL、作者或上游版本；2026-08-03。
- 许可证与全文 URL/商用/再分发：`UNVERIFIED`；无许可全文，不能证明商用或再分发权。
- Unity 兼容性/依赖：OTF 技术上可导入 Unity，但当前 UI 已冻结使用 Silver，不存在替换需求。
- 二进制风险/SHA-256：单一 OTF、无可执行代码；`7CA829A2A5D702E72AE46B6CADA4CA121E8E2ADD46B879431D89822A05FA7E0D`，3,395,760 bytes。
- 导入路径/Importer/GUID：Godot `fonts/ark-pixel-12px-proportional-zh_cn.otf`；Unity `N/A - 未导入`，无 GUID。
- 性能/视觉影响：若导入会增加字体数据并改变中文度量、换行和像素风格，需重做所有三视口证据。
- 加入路径：无；只有取得可核验许可、来源和版本且用户明确要求替换时才能重新登记。
- 移除/回滚：Unity 无导入；Godot 源受保护，不在本阶段删除。
- 拒绝原因：授权不可核验且与“使用 Silver”明确约束冲突。

### A-006 Godot Canvas shaders

- 名称/类别/状态：Godot `.gdshader` 视觉语义语料，`DEFER`。
- 来源 URL/作者/检索日期/版本：项目内源码；外部来源/作者/版本大多未登记，2026-08-03。
- 许可证与全文 URL/商用/再分发：根项目授权未闭合，`UNVERIFIED`；不能把插件示例许可外推到项目 shaders。
- Unity 兼容性/依赖：扫描范围 28 个 `.gdshader`（项目 `shaders/**` 21 个，其余在 addons/scene）；Godot shader language 与 URP Shader Graph/HLSL 不兼容，Unity `_Project` 当前没有 `.shader`，只有 6 个 `.mat`。
- 二进制风险/SHA-256：文本源码，无二进制执行风险；包含在 `D229...8C00` 清单摘要。
- 导入路径/Importer/GUID：Godot `shaders/**`；Unity `N/A - 不得原样导入`。
- 性能/视觉影响：采样、透明、screen texture、overdraw 和分支成本必须逐 shader 重测；任何重写都会直接影响 hover、dim、intent、map、game-over 等画面。
- 加入路径：只在真实视觉需求出现时冻结单个 Godot shader 的可观察语义，在独立 URP material/shader 中最小重写并做 GPU/三视口证据；不得批量转换。
- 移除/回滚：移除单一 Unity material/shader 和 Prefab 引用，恢复原稳定材质 checkpoint；重跑视觉/build/Player。
- 拒绝/延期原因：当前稳定表现不需要新 shader；来源授权和 Unity 等价性尚未验证。

### A-007 六边形 Blender/FBX 产物

- 名称/类别/状态：Grass/Dirt 六边形 `.blend` 源、生成脚本和 `.fbx`，`KEEP_LOCAL`。
- 来源 URL/作者/检索日期/版本：本地可复现产物，无外部下载 URL；作者/权属 `UNVERIFIED`；2026-08-03；generator `1.0`、Blender `5.1.2`。
- 许可证与全文 URL/商用/再分发：仓库未附本组资产许可/作者声明，公开发布权属 `UNVERIFIED`。
- Unity 兼容性/依赖：FBX binary，Unity 6000.4 ModelImporter；flat-top、半径 0.94m、层高 0.32m、每个模型 62 vertices/120 triangles、无 non-manifold edge。生成需要 Blender，但正式 Player 只依赖 FBX/Prefab。
- 二进制风险/SHA-256：
  - `generate_hex_tiles.py` `A3449A181607CE5B496CBAED47FB6391D329891039510E810E5E70B907A44FEE`
  - Dirt blend `71CF17E3704F6734BA2FEB7FCB11B70FB22BE17CAB8CFC237BFC77E47D4ED452`
  - Grass blend `0F96017FCC0D631B2357A8B536245F9A30CC601ADB6B06AFF16E1B9A77FF0827`
  - Dirt FBX `FBD6A03B81E3093825226CA8BDE6757CDA1703AA484DF1C5732AEA9FA2682A75`
  - Grass FBX `B5B72C3537E48F1D9C6B6E48EEC759638F96461D7F3D99086020BC933B96305C`
- 导入路径/Importer/GUID：`ArtSource/HexTiles/**`；正式模型 `Resources/Art/Battle/Models/**`。Dirt FBX GUID `4db62cd47af6288458d9cf1e3014b0b2`，Grass `422a6fd99637d4243bc51fdf50416190`；globalScale 1、meshCompression 0、isReadable 0、无自动 collider。
- 性能/视觉影响：每层每变体 120 triangles，材质槽 Dirt 3/Grass 4；动态层数和 collider 数由现有 Prefab/`HexTileColumn` 控制。改轴、origin、层高或材质会破坏地震高度、occupant 锚点和 360 度视觉。
- 加入路径：已在正式工程；不得调用 Blender/MCP 重生成，除非出现可复现硬缺陷、先保存旧/新 hash 和模型验证 JSON，再由独占 Agent 修改 ArtSource。
- 移除/回滚：恢复旧 FBX+meta、Prefab mesh/material/collider refs 和 0.32m 契约，重跑模型 roundtrip、terrain PlayMode、三视口/四 yaw、build/Player。
- 拒绝/保留原因：技术与几何证据完整，可继续本地使用；作者/发布许可仍需项目方确认。

### A-008 Unity 卡背 `behide.png`

- 名称/类别/状态：卡背 PNG，`UNVERIFIED`（可能孤立）。
- 来源 URL/作者/检索日期/版本：Godot `image/behide.png`；外部 URL/作者/版本未记录；2026-08-03。
- 许可证与全文 URL/商用/再分发：`UNVERIFIED`。
- Unity 兼容性/依赖：PNG 兼容；当前静态扫描在 `_Project` 非 meta 文件中未找到 GUID 或 `behide` 字符串引用。
- 二进制风险/SHA-256：纯图片；源/副本均为 `3AFF3AE738AACB3F08DDEA3CD00D00C0960C1C87B49AF9F5D3EB50E28FED0E87`。
- 导入路径/Importer/GUID：`Assets/_Project/Resources/Art/Battle/Cards/behide.png`；Default Texture、Point、uncompressed、max 1024；GUID `a96a4deb2bdd3564a8b11330f1704f26`。
- 性能/视觉影响：若被 Resources 构建包含，会增加纹理体积；当前无可见影响证据。
- 加入路径：不新增引用；若未来需要卡背，先补许可并通过 typed card state 接入，禁止以文件存在替代需求。
- 移除/回滚：只能在 CardAssetAudit + build report 证明无 GUID、Resources 字符串、Prefab/Scene/Material、反射或外部脚本引用后，由独立 checkpoint 删除文件+meta；删除前后跑全卡牌/build/Player。
- 拒绝原因：当前只标记“可能孤立”，静态扫描不足以授权删除，故保留原文件。

## 统一加入门禁

1. 固定 exact version/字节，记录来源 URL、作者、检索日期、许可全文和 third-party notices。
2. 下载只进入隔离临时目录；下载后先计算 SHA-256，再审查 archive path traversal、可执行文件、DLL/native plugin、安装脚本和依赖图；未经批准不执行。
3. 保存试验前 `manifest.json`、lock、ProjectSettings、相关 asmdef、Scene/Prefab/Importer/GUID 哈希。
4. 只由主智能体在单一目的 checkpoint 加入；禁止 Package Manager 的隐式升级、Asset Store 批量导入和自动重写正式 Scene。
5. 运行独占 EditMode/PlayMode、build/Player smoke 和受影响三视口/Inspector/EditorWindow 实际视觉检查。

## 统一移除门禁

1. 在加入 checkpoint 时同时写明 exact owned paths、serialized GUID、Resources/Addressables key、package/transitive 和生成输出。
2. 先解除代码/Scene/Prefab/ProjectSettings/Importer 引用，再删除候选自有文件；只清理无人依赖的 transitive。
3. 比较移除后 manifest/lock/asmdef/正式 Scene/Prefab hash 与试验前基线；执行 `git diff --check` 和精确 staged-file 审查。
4. 重跑与加入路径同等级的测试、build、Player smoke 和视觉证据。无法恢复基线或无法解释残留即回滚失败，候选保持 `REJECT`。
