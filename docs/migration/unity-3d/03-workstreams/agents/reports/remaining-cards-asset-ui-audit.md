# Remaining Cards 资产与 UI 只读审计

> 智能体：`remaining-cards-asset-ui-audit`
> 日期：2026-08-01
> 分支：`unity_7.31`
> 范围：Tower、Poison、五张卡图、Tower/状态 Prefab、七卡布局、clear 专用预览
> 权限：只读审计；除本报告外未修改代码、Scene、Prefab、资源、测试或共享文档

## 1. 结论

- 五张剩余卡图已经在 Unity Resources 中存在，且与 Godot 权威源逐字节一致；路径、`FrontImage`、尺寸和导入方向均无歧义，不需要重新复制、重编码、裁切或修图。
- Unity 当前没有 Tower 世界素材副本、Tower Prefab、Poison 状态图标副本或 Poison 状态 Prefab。Godot 源均明确存在，缺口是尚未导入/装配，不是硬阻塞。
- Tower 的权威表现是单张透明像素精灵，不是 3D 模型。最小保真迁移应沿用项目现有 `CameraFacingBillboard` 方案，将精灵挂在真实地块 `OccupantAnchor` 下；不应临时制作新 3D 塔或用卡面插画替代世界精灵。
- Poison 本阶段最小表现应使用 `image/effect/poison_icon.png` 加整数层数，不使用较大的 `image/effect/poison.png` 作为常驻状态。后者更接近一次性 VFX，且最终 VFX 明确不属于本阶段。
- R3 的三张七卡闲置态截图证明七卡完整、比例正确且未挡右侧 HUD；但现有自动化只覆盖两卡布局测试，以及 1920 下 `lighting`/`earthquake` 选中。七卡激活后，中间卡在 1280x720 以 `1.50x + 80px lift` 选中时可能遮挡棋盘下缘，必须用真实截图检查后再决定是否改布局。
- Unity 当前 `TimelinePlacementPreview` 只有普通 valid/invalid 状态，不知道 clear 的“合法空格/命中 action/越界”三态，也没有 action identity 输入。clear 必须有独立 Presenter/preview 状态，不能只把 `ClearMask` 塞进现有 `Show(origin, shape, bool)`。
- **本地开发无硬阻塞。** MIG-005 的素材授权缺口仍只阻塞公开发行/分发，不阻塞本仓库内的迁移和验证。

## 2. 审计依据

已完整读取：

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_REMAINING_CARDS_PROMPT.md`
- `docs/migration/unity-3d/06-maintenance/scene-and-prefab-guide.md`
- `docs/migration/unity-3d/06-maintenance/testing-and-evidence.md`
- 当前 `CombatVerticalSlice.unity`、`CardView.prefab`、`TimelineCell.prefab`、`TargetView.prefab`
- 当前 CardHand、Timeline preview/Presenter、Scene asset tests、PlayMode card/vertical-slice tests 与 Editor harness
- 五份真实卡 JSON、Godot Tower/Poison/Recover/Clear 源脚本及相关图片 `.import`
- R3 三视口七卡截图、普通时间轴 valid/invalid 截图、卡图 manifest 和继承证据账本

`asset-audit` 的通用“必须 2 的幂和统一重命名”规则不覆盖本项目冻结的历史原图。这里以真实 JSON、冻结哈希、原文件名和主 Prompt 的保真要求为准，不改名、不补透明度、不改分辨率。

## 3. 五张卡图：Godot 真源与 Unity 现状

五张卡面均为 `1135x1590`、8-bit 24-bit RGB PNG（PNG color type 2），**没有 alpha 通道**。圆角外看到的灰白棋盘格是烘焙进 RGB 的源像素，不是透明背景；任何“抠透明”“去棋盘格”都会改变权威资产。

| stable ID | JSON `front_image` | Godot 权威源 | Unity 当前目标 | SHA-256 | 结论 |
| --- | --- | --- | --- | --- | --- |
| `recover` | `recover.png` | `card_asset/recover.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/recover.png` | `F9065BC4FD646D8EEFC463FBC35798EDDDDDE489C4214F6333ACCC6E54A23D99` | 逐字节一致 |
| `tower` | `tower_card.png` | `card_asset/tower_card.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/tower_card.png` | `73BFFC6162D4D0D09D196835E3AC1BE5D40FEE19BC6C5BC391008386471AF05B` | 逐字节一致；不能按 stable ID 猜成 `tower.png` |
| `poison` | `poison_card.png` | `card_asset/poison_card.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/poison_card.png` | `936B2BE26879462D0A18E1F0F5867E3681461A744E73E56823E3B1E8AEDCB879` | 逐字节一致；不能按 stable ID 猜成 `poison.png` |
| `wind` | `wind.png` | `card_asset/wind.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/wind.png` | `BFCDA4C0A872FE327EFDFB2FF6880E4A343CC176484CCE5BEE0BC13C39DDB498` | 逐字节一致 |
| `tornado` | `tornado.png` | `card_asset/tornado.png` | `unity/Assets/_Project/Resources/Art/Battle/Cards/tornado.png` | `B369DCC027670C4AA28E003FC61B97DCE5DB077B982AEE6BAFDC84766B00E564` | 逐字节一致 |

当前五份 `.meta` 一致采用：

- `textureType=8`（Sprite/UI）、`spriteMode=1`（Single）
- sRGB 开启；Point filter；Clamp；Max Size 2048
- mipmap 关闭；Default/Standalone/WebGL 均不压缩
- `Image.preserveAspect=true`

这些设置与 Godot 源图片的 lossless、无 mipmap 导入方向一致。卡图的 `alphaIsTransparency=0` 不构成问题，因为源文件根本没有 alpha。不要为了“透明圆角”改 importer；Importer 不能恢复不存在的透明度。

卡图源比例为 `1135/1590 = 0.713836...`；`CardHandView.DesignCardSize=125x175` 的比例为 `0.714286...`，且 `Image.preserveAspect=true`，因此是约 0.08 像素级留边而不是可见拉伸。卡面保持竖直、`FlipX=false`、`FlipY=false`，不应随棋盘 yaw 旋转或镜像。

## 4. Tower 权威资产与最小 Unity 方案

### 4.1 真源

| 内容 | 路径/事实 |
| --- | --- |
| 世界精灵 | `image/enermy/tower/tower.png` |
| 资产数据 | `256x256` RGBA；34506 bytes；SHA-256 `41DD24EF1670355906BA80247233BDC422E6022AB0A7AE42B8ACEB962E18F014` |
| 非透明内容边界 | `(74,2)-(180,245)`，实际内容约 `107x244`，左右透明留白很大 |
| 行为脚本 | `scene/in_scene/enermy/tower.gd` |
| 创建入口 | `scene/in_scene/timeline/commands/BuiltCommand.gd` 的 `creation=tower` |
| Godot 导入 | lossless、无 mipmap、fix alpha border、未预乘 alpha |
| 方向 | 单张 3/4 视角像素精灵；旗帜和门洞有明确左右方向，不应镜像 |

`tower_card.png` 是卡面，`tower.png` 才是世界 occupant 精灵；两者不可互换。

### 4.2 建议 Unity 路径

- 图片：`unity/Assets/_Project/Resources/Art/Battle/Occupants/tower.png`
- 图片 `.meta`：与图片同时提交并固定 GUID
- Prefab：`unity/Assets/_Project/Prefabs/Battle/Occupants/Tower.prefab`

建议最小 Prefab 层级：

```text
Tower
├── VisualBillboard          SpriteRenderer + CameraFacingBillboard
└── StatusAnchor             PoisonStatus 等状态表现的挂点
```

实现约束：

- `Tower` 根挂到目标 `HexTileColumn.OccupantAnchor`；根保持 `localPosition=0`、`localRotation=identity`，视觉偏移只放在 `VisualBillboard`。
- 复用现有 `CameraFacingBillboard`，让 SpriteRenderer 可见正面（本项目定义为 local `-Z`）始终朝向场景相机。Prefab 实例初始化时必须显式调用 `Initialize(sceneCamera)`；该组件不会自动找到相机。
- 建议初始试配 `VisualBillboard` 约 `scale=0.60~0.65`、局部 Y 约 `0.72~0.80`，与现有 `TargetView` 的 256px 原图、`scale=0.62`、Y=0.85 同量级；最终数值必须以实际四 yaw 截图为准，不能仅凭 YAML 冻结。
- `SpriteRenderer` 使用 Point、无 mipmap、无压缩、Clamp、sRGB、`alphaIsTransparency=true`、无阴影。透明图若使用纹理压缩或未启用 alpha transparency，灰塔边缘很容易出现黑/白 halo。
- 不翻转 X/Y；不要根据 yaw 切换镜像。单张源精灵的最低风险表现是相机朝向 billboard，而不是伪造四方向帧。
- Tower 视觉本身不需要额外 Collider。当前地图选择通过真实 HexBlock collider 向上找到 `BoardTileView`；视觉 collider 可能挡住或歧义化地块选择。若未来确需 occupant collider，应让 RaycastAll 仍能稳定回落到同一地块，并增加四 yaw 测试。
- Prefab 不存 HP/阵营等运行态真值；HP100、中立、HexCoord 由 Domain/Application 快照驱动。Prefab 只存人工可维护的视觉、锚点和序列化引用。
- 不新增 3D 模型、材质包或外部素材。主 Prompt 要求原 Tower 素材且最终 3D/VFX 不在本阶段。

## 5. Poison 权威资产与最小 Unity 方案

### 5.1 真源

| 内容 | 路径/事实 |
| --- | --- |
| 常驻状态图标 | `image/effect/poison_icon.png` |
| 图标数据 | `160x160` RGBA；22325 bytes；SHA-256 `4445561C52BBD9DC4206CA475D72E1D6178361281E558459739F28CB72013812` |
| 非透明内容边界 | `(0,6)-(159,153)`；图标边缘和小紫色粒子含半透明像素 |
| 状态注册 | `scene/global/StatusDB.gd`：颜色 `#a855f7`、`icon_path=poison_icon.png` |
| Godot 常驻布局 | `tile.gd` 默认图标目标 `56x56`，偏移 `(0,-82)`，高于建筑和地块高亮 |
| Godot 状态组件 | `scene/in_scene/status/status_component.gd`：一枚 icon，多次施加累加 stack，刷新不复制 icon |
| 大图 VFX | `image/effect/poison.png`，`1024x749` RGBA；本阶段不建议作为常驻状态 |

`poison_card.png` 只用于手牌；`poison_icon.png` 才用于 occupant 状态；`poison.png` 是更大的表现资产，当前阶段不应混用。

### 5.2 建议 Unity 路径

- 图片：`unity/Assets/_Project/Resources/Art/Battle/Status/poison_icon.png`
- Prefab：`unity/Assets/_Project/Prefabs/Battle/Status/PoisonStatus.prefab`

建议最小 Prefab：

```text
PoisonStatus
├── Icon                    SpriteRenderer（原 poison_icon.png）
└── StackLabel              整数层数；沿用项目现有字体方案，不新增字体包
```

实现约束：

- Root 或视觉子节点使用 `CameraFacingBillboard`，挂到 occupant 的 `StatusAnchor`；四 yaw 下图标和数字必须正向且不镜像。
- 图标 Import：Sprite/Single、Point、Clamp、无 mipmap、无压缩、sRGB、`alphaIsTransparency=true`，Max Size 256 即可。160 是历史 NPOT 尺寸，Unity 支持；不得为满足通用 2 的幂规则缩放成 128/256。
- 图标不带 Collider、不接收 UI raycast；不得遮断 tile/occupant 选择。
- 同一个 occupant 只有一个 PoisonStatus 实例，重复施加只把层数从 `2` 更新为 `4`、`6`……，不能复制多个骷髅图标。
- 层数数字是必要信息冗余；只有骷髅图标无法证明“重复累加”和 before/after stack。数字应至少在 1280x720 默认镜头下可读。
- 图标应在 occupant 上方、地块 highlight 之上，但不覆盖主体关键轮廓或 HP 表现；Tower 的旗帜很高，Tower 的 `StatusAnchor` 需要单独人工调高，不能假定所有 occupant 共用同一个 Y。
- 本阶段不播放传播/tick，不让图标自动衰减；只响应 Application 快照中的整数层数。

## 6. 当前 Unity Scene/Prefab/Resources 差距

当前保存的六个 Battle Prefab 只有：

- `UI/TimelineCell.prefab`
- `Cards/CardView.prefab`
- `Terrain/HexBlockGrass.prefab`
- `Terrain/HexBlockDirt.prefab`
- `Terrain/HexColumn.prefab`
- `Targets/TargetView.prefab`

当前没有：

- `Prefabs/Battle/Occupants/Tower.prefab`
- `Prefabs/Battle/Status/PoisonStatus.prefab`
- `Resources/Art/Battle/Occupants/tower.png`
- `Resources/Art/Battle/Status/poison_icon.png`

`CombatVerticalSlice.unity` 已保存 Camera、Canvas、Timeline、CardHandHost、BoardRoot、TargetAnchor 和普通 preview；CardHandHost 引用同一个 `CardView.prefab`，Timeline 保存 36 个 `TimelineCell` Prefab 实例。后续新增 Tower/Poison 时应给 Composition/Presenter 增加显式 Prefab 引用并扩展 `CombatSceneAssetTests`，不能让运行时代码 `Resources.Load` 后临时造稳定层级，也不能把 Prefab stable-ID 分支塞回 Controller。

`CombatSceneAssetTests.ProductionPrefabs_AreSavedAssetsWithExpectedComponents` 当前只断言上述六个 Prefab；Gate B 必须增加 Tower 与 PoisonStatus 的资产和组件断言。

## 7. 七卡布局现状与风险

### 已证明的部分

R3 的下列截图已人工查看：

- `docs/migration/unity-3d/04-verification/evidence/unity-decoupling-r3/seven-card-hand-1280x720.png`
- `.../seven-card-hand-1920x1080.png`
- `.../seven-card-hand-2560x1080.png`

闲置态下：七张卡均完整、原图比例正确、没有裁切，不挡右侧 HUD 和上方 Timeline。当前 `CardHandHost` 采用横向重叠布局：slot 宽 145、card 125x175、间距 102~116；Host 为底部左侧 67% 宽、280 高。这符合主 Prompt 允许的横向重叠方案，无需为了“七卡”改成卡片墙或 ScrollRect。

### 尚未证明的风险

- `CardHandView` 的 selected 状态固定 `scale=1.50`、`lift=80`；中间三张卡在 1280x720 被选中/拖拽时，可能覆盖棋盘下缘可选地块。现有三视口截图只有 idle；选中截图只有 1920 下的 `lighting`/`earthquake`，且两者位于手牌左端，不能代表 `recover`/`tornado`/`tower` 等中间卡。
- 1280x720 闲置卡实际显示较小，主要插画仍可识别，但源卡面内的中文描述很难直接阅读。选中放大可以承担阅读入口，因此不能在未截图前继续缩小卡牌。
- `BuildRig` 的 `RenderedTwoCardStates_FitThreeViewportsAndWriteEvidence` 只构造两张卡，不证明七卡 selected/dragged 的 bounds、sibling 顺序或互相遮挡。
- 新卡从“显式 unavailable”变为可交互后，selected/targeting/scheduling 状态持续时间更长；旧 idle 截图不能替代本阶段真实交互截图。

最小建议：先保持当前 idle 布局，新增“七卡 + 中间卡选中/拖拽”的三视口截图和 bounds 断言；只有真实失败时才做响应式调整。若 720p 确认遮挡，优先按 Host 实际高度/宽度限制 selected lift/scale，或把选中预览移向左侧安全区；不要改卡图比例，也不要引入不必要的滚动列表。

## 8. Clear Preview 视觉差距与最小方案

### 当前差距

`TimelinePlacementPreview` 当前 API 只有：

```text
Show(origin, shape, isValid)
```

它只支持：

- valid：统一绿色填充
- invalid：统一红色填充 + 黄色 Outline
- 越界坐标：只进入 `MissingCoordinates`/事件，本身没有可见 cell

它不知道哪些格子命中已有 action，也没有 action identity，因此无法满足：

- 合法空格
- 合法且命中 action
- 越界
- 同一 action 多格命中只作为一个移除对象

Godot `TimelineClearEffect.gd` 的可观察源色是空格天蓝、命中绿色、越界 IndianRed，并给每个 overlay 加深色 2px 边框；但本阶段主 Prompt 更严格，要求除颜色外还有边框、图标或形状冗余。

### 最小建议

- 保留现有普通 `TimelinePlacementPreview`，新增独立 clear preview 状态/Preset（例如 `ClearTimelinePreview`），从 Application 的 clear preview 结果读取 `inBounds`、每格 hit 状态和被命中的 action identities。Presentation 不自行扫描标签或复制去重规则。
- 可在 `TimelineCell.prefab` 增加一个默认关闭的 `ClearPreviewOverlay` 子层，由独立 Presenter 开关；这样 36 格的边框/角标可在 Prefab Mode 维护，不需要每次动态 `AddComponent<Outline>`。
- 推荐三态冗余：
  - 合法空格：天蓝半透明填充 + 细实线边框 + 空心方块/圆点角标。
  - 命中 action：绿色填充 + 更粗边框 + `HIT`/叉号角标；原 action 标签仍可读。
  - 越界：红色填充 + 黄色高对比边框 + `!` 角标；超出网格的不可见部分由 HUD 明确显示 `OUT OF BOUNDS`，不能只依赖 `MissingCoordinates` 日志。
- `wind` 的 2x2 合法性只看 12x3 边界；合法空清必须显示完整 2x2 天蓝并可确认。命中任意一格时只把命中格显示为绿色，其余合法空格仍为天蓝。
- `tornado` 的 12x1 在列 0 才能合法完整覆盖一行；列 1 起必须越界。合法态应把整行 12 格连贯标示，不能只亮起鼠标所在格。
- 点击或取消后，overlay、outline、角标和 HUD 状态必须同帧清除，并恢复 action 原标签/颜色；不得残留或把普通 placement preview 污染成 clear 状态。

## 9. 视口与四 yaw 验证矩阵

### 1280x720

- 七卡 idle、选中边缘卡、选中中间卡、拖拽中间卡都在屏幕内；重点检查 selected 卡是否挡住棋盘底行、Timeline 或右侧 HUD。
- `poison_icon` 和层数在默认镜头下至少可辨认，不与 occupant/地块选中 highlight 混成一团。
- Wind 2x2 与 Tornado 12x1 的边框/角标在缩放后仍至少保留清晰的 1~2 个实际屏幕像素；`!`/`HIT` 不被压缩掉。
- Tower 不被卡手或 HUD 裁切；顶部旗帜和底座都在画面内。

### 1920x1080

- 作为全交互标准证据视口，分别保存 Recover、Tower、Poison、Wind、Tornado 的选中/目标/预览/结算前后。
- 四个 yaw `0/90/180/270` 全部在此视口执行：Tower、Poison 图标、范围和地块选择绑定同一 HexCoord。
- Clear 同时保存合法空清、命中 action、越界；Tornado 保存整行合法和列 1 越界。

### 2560x1080

- 检查超宽屏 Canvas 锚点：七卡不能异常拉开、右 HUD 不漂移、Timeline 12 列仍连续居中。
- Tower/Poison 不应因相机宽视野或 Canvas scale 变得过小；世界 billboard 与 screen-space UI 不得产生相对漂移。
- Wind/Tornado overlay 与实际 action cell 一一对齐，不因宽高比改变出现半格偏移。

### yaw 0/90/180/270 每向必查

1. Tower 实例仍是同一对象，父节点仍为同一 `HexTileColumn.OccupantAnchor`，根局部姿态未漂移。
2. `VisualBillboard` 的可见 local `-Z` 正面朝相机；旗帜/门洞没有镜像，SpriteRenderer 可见且不被地形遮住。
3. Poison icon 和 StackLabel 朝相机、数字正向，且仍位于同一 occupant 上方。
4. 通过真实 top collider 点击时返回相同 `HexCoord`；Tower/状态表现不拦截或改变 Raycast 选择。
5. Range/highlight 仍覆盖 Tower/Poison 所在的真实地块；若列高改变，occupant/status 跟随 `OccupantAnchor` 顶面更新。
6. Clear 属于 Timeline UI，不应随 board yaw 改变任何 mask 坐标、action identity 或预览格。

建议自动断言 billboard：`dot(-visual.forward, (camera.position-visual.position).normalized)` 接近 1；视觉截图仍必须人工检查，因为该数学断言不能证明遮挡、比例或可读性。

## 10. 最小测试/证据建议

### EditMode / 资产

- 五张卡图 Source/Unity SHA、尺寸、`front_image -> Resources stem` 回归。
- Tower/Poison 图片 importer：Sprite/Single、Point、Clamp、无 mipmap、无压缩、alpha transparency。
- `CombatSceneAssetTests` 新增 Tower 和 PoisonStatus Prefab 路径、预期组件、无 missing script、Composition Inspector 引用非空。
- `CardView.prefab` 的 `Image.preserveAspect`、同一个 Prefab 动态生成七卡继续保持。

### PlayMode

- Tower 从 Prefab 实例化、挂到目标列 `OccupantAnchor`，重复刷新不复制实例；四 yaw 可见且 tile 选择仍返回原坐标。
- Poison `0 -> 2 -> 4`：一枚图标、层数字符串更新、无自动 tick、取消/失败不生成图标。
- Poison 图标跟随 occupant/列高，且不含 Collider/raycast blocker。
- 七卡在三视口逐卡或至少“左/中/右”代表卡选中；所有 `Artwork` 完整在屏幕内、保持源比例、selected sibling 在最上层，且不与 Timeline/HUD 相交。
- Clear 2x2/12x1 的空格、命中、越界三态；重复 Show/Clear 无残留、取消无副作用；完整 action 移除后所有占格恢复默认内容。

### 真实视觉证据

- 结构化断言之外，必须保存 Tower before/after、Poison before/after、Wind/Tornado 三态、七卡三视口和四 yaw 截图并逐张人工查看。
- R3 idle 七卡截图在 CardHand/Scene/Presenter 未修改前可作为风险参考；一旦本阶段修改 Scene、Prefab、Presenter、卡牌状态或 clear overlay，相关旧截图即失效并必须刷新。
- 非空像素、退出码 0 和 `Renderer.isVisible` 不能替代人工检查。

## 11. 风险与硬阻塞判断

| 项目 | 判断 | 处理 |
| --- | --- | --- |
| 五卡卡面来源/映射 | 无阻塞 | 已存在且哈希一致，继续使用现有 Resources |
| Tower 世界素材 | 无阻塞 | Godot `tower.png` 权威且唯一；按建议路径导入 |
| Poison 状态素材 | 无阻塞 | `poison_icon.png` 权威且唯一；本阶段无需大图 VFX |
| Tower 3D 模型缺失 | 非阻塞 | 源项目本来就是 2D 精灵；使用 billboard 保真 |
| Prefab/Scene 引用 | 当前未完成但非硬阻塞 | Gate B 新建两个 Prefab，并由主智能体串行接 Scene/Composition |
| Clear 三态 UI | 当前未完成但非硬阻塞 | Gate C 新增独立 clear preview，不复用普通布尔 preview 语义 |
| 七卡 720p 中间卡选中 | 视觉风险，非硬阻塞 | 先拍真实 selected/drag 截图；失败才做最小响应式调整 |
| 素材授权 MIG-005 | **公开发布硬阻塞；本阶段非阻塞** | 不把本报告当授权证明；本地迁移/测试可继续 |
| 视觉验收 | 功能完成前不可宣称通过 | 必须在实现后跑三视口、四 yaw、真实交互截图 |

**最终判断：当前无用户决策硬阻塞；Gate A/B/C 可以继续。** Tower 单张精灵采用 billboard、Poison 使用原图标加层数，是在现有来源和主 Prompt 下的最低风险实现，不需要新增素材、依赖或范围扩张。
