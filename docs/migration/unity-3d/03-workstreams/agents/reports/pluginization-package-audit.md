# Pluginization Package Audit

- 审计日期：2026-08-03（Asia/Shanghai）
- Unity：`6000.4.10f1`
- 审计范围：`unity/Packages/manifest.json`、`unity/Packages/packages-lock.json`、`unity/Assets/_Project/**/*.asmdef`，以及候选 UI Toolkit、Input System、Cinemachine、Addressables、Timeline
- 执行约束：仅做本地只读检查与官方 Unity 来源检索；未下载、安装、执行或导入任何候选；未修改 Packages、asmdef、ProjectSettings 或 Assets

## 结论

| 候选 | 状态 | 本阶段结论 |
|---|---|---|
| UI Toolkit（仅 Editor 工具） | `ACCEPT` | 已由 Unity 以 `com.unity.modules.uielements@1.0.0` 内建并在 manifest 直引。首个 EditorWindow 可以使用，不需要改包清单，也不得迁移现有运行时 uGUI。 |
| Input System | `DEFER` | 官方当前候选 `1.20.0` 支持 Unity `6000.0+`，但项目仍使用旧 Input Manager、`StandaloneInputModule` 和 7 个直接读取 `UnityEngine.Input` 的运行时文件；引入会扩大交互回归面，本工具切片无收益。 |
| Cinemachine | `DEFER` | 官方当前候选 `3.1.7` 的最低 Unity 为 `2022.3`，但稳定 360 度棋盘镜头已由 `BoardOrbitCameraController` 驱动。引入会触碰冻结 Camera/Scene 契约。 |
| Addressables | `DEFER` | 官方当前候选 `3.1.0` 支持 Unity `6000.0+`，但当前运行时只有 2 处卡面 `Resources.Load`；不足以抵消目录、加载、构建与内容目录迁移成本。 |
| Timeline | `DEFER` | 官方当前候选 `1.8.12`，Unity 6000.0 手册发布基线为 `1.8.10`。当前无 `PlayableDirector`、`TimelineAsset` 或 Timeline API 使用；以后只能作为表现层驱动，不能成为玩法提交或结算源。 |
| 未指名第三方 DI/Tween/Inspector/Asset 工具 | `REJECT` | 没有具体来源、版本、许可证、依赖和可移除证据，不进入试用或正式工程。 |

首个工具切片不需要安装任何包。若采用 UI Toolkit，实现必须位于 Editor-only assembly；运行时、正式 Scene/Prefab 与现有 uGUI 保持不变。

## 当前包清单

`manifest.json` 有 38 个 direct dependency，`packages-lock.json` 共解析 50 个 dependency；审计时 `git diff -- unity/Packages ':(glob)unity/Assets/_Project/**/*.asmdef'` 为空。

Direct registry/builtin packages：

- `com.unity.render-pipelines.universal@17.4.0`
- `com.unity.nuget.newtonsoft-json@3.2.2`
- `com.unity.test-framework@1.6.0`
- `com.unity.ugui@2.0.0`
- Built-in `@1.0.0` modules：`accessibility`、`adaptiveperformance`、`ai`、`androidjni`、`animation`、`assetbundle`、`audio`、`cloth`、`director`、`imageconversion`、`imgui`、`jsonserialize`、`particlesystem`、`physics`、`physics2d`、`screencapture`、`terrain`、`terrainphysics`、`tilemap`、`ui`、`uielements`、`umbra`、`unityanalytics`、`unitywebrequest`、`unitywebrequestassetbundle`、`unitywebrequestaudio`、`unitywebrequesttexture`、`unitywebrequestwww`、`vectorgraphics`、`vehicles`、`video`、`vr`、`wind`、`xr`

Transitive packages：

| Depth | Package |
|---:|---|
| 1 | `com.unity.ext.nunit@2.0.5` |
| 1 | `com.unity.modules.hierarchycore@1.0.0` |
| 1 | `com.unity.modules.subsystems@1.0.0` |
| 1 | `com.unity.render-pipelines.core@17.4.0` |
| 1 | `com.unity.render-pipelines.universal-config@17.4.0` |
| 1 | `com.unity.shadergraph@17.4.0` |
| 2 | `com.unity.burst@1.8.29` |
| 2 | `com.unity.collections@6.4.0` |
| 2 | `com.unity.mathematics@1.3.3` |
| 2 | `com.unity.searcher@4.9.4` |
| 3 | `com.unity.nuget.mono-cecil@1.11.6` |
| 3 | `com.unity.test-framework.performance@3.4.0` |

## asmdef 依赖方向

共 10 个 asmdef，运行时依赖图无环：

```text
TimeKey.Domain
  <- TimeKey.Application
  <- TimeKey.Diagnostics
  <- TimeKey.Infrastructure
  <- TimeKey.Presentation

TimeKey.Composition
  -> Domain + Application + Infrastructure + Diagnostics + Presentation

TimeKey.Editor [Editor only]
  -> Domain + Application + Infrastructure + Presentation + Composition

TimeKey.Tests.EditMode [Editor only]
  -> Domain + Application + Diagnostics + Presentation + Composition
TimeKey.Tests.Infrastructure [Editor only]
  -> Domain + Application + Infrastructure
TimeKey.Tests.PlayMode
  -> Domain + Application + Diagnostics + Infrastructure + Presentation + Composition
```

`Domain`、`Application`、`Diagnostics` 均为 `noEngineReferences: true`，只读扫描未发现其中引用 `UnityEngine` 或 `UnityEditor`。`TimeKey.Tests.EditMode` 当前没有引用 `TimeKey.Editor`；若首切片的独占 EditMode 测试要直接调用 Editor 工具，必须由主智能体精确增加该引用，或创建独立 Editor 测试 asmdef，不得让 Domain/Application 反向依赖 Editor。

## 候选登记

以下所有候选的作者/发布者均为 Unity Technologies。官方 registry URL 为 `https://packages.unity.com/<package-id>`；检索日期均为 2026-08-03。

### UI Toolkit（Editor-only）

- 状态：`ACCEPT`
- 来源：<https://docs.unity3d.com/6000.0/Documentation/Manual/UIElements.html>、<https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-support-for-editor-ui.html>、本机 Unity 6000.4 内建 `com.unity.modules.uielements/package.json`
- 版本/兼容性：工程已直引 `com.unity.modules.uielements@1.0.0`；本机 Unity 6000.4 内建包声明依赖 `ui`、`imgui`、`jsonserialize`、`hierarchycore`、`physics`（均 `1.0.0`）
- 许可证：Unity Engine 内建模块，无独立 UPM Companion License 文件；使用与商用受有效 Unity Engine License/Unity 条款约束。不得把内建模块作为独立产品再分发。
- 二进制风险：代码位于已安装 Unity Editor/Player 模块；不新增第三方二进制或供应链入口。EditorWindow 仍须限制在 Editor assembly，避免进入 Player。
- SHA-256：`N/A - not downloaded`（本阶段没有新下载；已安装引擎模块也不作为候选包体复制）
- 导入路径：无需导入；声明已在 `Packages/manifest.json`，引擎模块位于 Unity 安装目录。项目窗口代码应放入 `Assets/_Project/Editor/**` 或本地 Editor-only UPM 模块。
- 真实收益：结构化 EditorWindow、列表/树视图、筛选与数据绑定适合 SceneContractValidator/CardAssetAudit 的诊断展示；可在不改运行时 UI 的前提下使用。
- 性能/视觉影响：只影响 EditorWindow 的编辑器布局、重绘与内存；若完全留在 Editor assembly，对 Player 性能和游戏画面为零。需要按 1280×720、1920×1080、2560×1080 对窗口缩放、截断和滚动做实际检查。
- 加入路径：直接在 Editor-only assembly 中引用 `UnityEngine.UIElements`/`UnityEditor.UIElements`；不改 manifest/lock/ProjectSettings；为窗口增加独占 EditMode 测试。
- 移除/回滚：删除该工具拥有的 C#/UXML/USS 和专用测试；移除其菜单入口；保留原有 `com.unity.modules.uielements` direct dependency，因为它是阶段前已存在的工程配置。运行 EditMode、编译和 build 验证 Player 不变。

### Input System

- 状态：`DEFER`
- 来源：<https://packages.unity.com/com.unity.inputsystem>、<https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.inputsystem.html>、<https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html>、<https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/license/LICENSE.html>
- 版本/兼容性：官方 registry `latest=1.20.0`，package metadata `unity=6000.0`；Unity 6000.0 手册较早发布记录为 `1.17.0`
- 许可证：Unity Companion License v1.4。可在有效 Unity Engine License 下免费、免版税用于开发和分发商业 Unity 内容；Work 的大量部分需保留许可证及随附 third-party notices；不得用于竞品分析/竞品开发，也不应脱离 Unity-dependent content 单独再分发。许可证：<https://unity.com/legal/licenses/unity-companion-license>
- 依赖：`com.unity.modules.uielements@1.0.0`
- 二进制风险：官方 Unity registry，供应链风险低于未知第三方；未下载包体，未独立检查 package 内容、预编译程序集或平台后端，故仍为 `UNVERIFIED until trial import review`。
- SHA-256：`N/A - not downloaded`；registry 仅公布候选 tarball 的 SHA-1 `7a4e1a2a81941121ffc0721a14009b46299dada1`，不能替代要求的 SHA-256
- 导入路径：UPM cache（不应提交）+ `Packages/manifest.json`/lock；输入动作建议仅在获批试验时放 `Assets/_Project/Input/**`
- 真实收益：动作映射、设备抽象、手柄/触控和可测试输入注入；对未来多设备支持有价值。
- 当前成本：`ProjectSettings.asset` 为 `activeInputHandler: 0`；7 个运行时文件直接使用旧 `Input`，Bootstrap 保存 `StandaloneInputModule`，多组 PlayMode 测试也构造该模块。当前工具链不消费运行时输入，因此收益为零。
- 性能/视觉影响：新增 action/device 处理与事件路径，需对 GC、输入延迟和设备热插拔做 profiling；UI module 切换可能改变点击、焦点、导航、Escape/右键以及全局输入锁语义，具有直接交互回归风险。
- 加入路径：建立独立 checkpoint；在 manifest 固定 `1.20.0` 并审查 lock；显式决定 Active Input Handling；创建 input actions；逐一迁移 7 个运行时文件和 EventSystem prefab/scene；扩充键鼠/手柄/触控 PlayMode；完成三视口、build、Player smoke 后才可接受。
- 移除/回滚：先恢复 `StandaloneInputModule`、旧输入代码、输入锁与测试；删除 `.inputactions`/生成 C#；把 Active Input Handling 恢复为 Input Manager (Old)；从 manifest 移除 direct package 并确认 lock 只移除不再被使用的 transitive；再跑全套交互、build 和 Player smoke。推荐以单一 checkpoint revert 完成，禁止留下双输入半迁移状态。

### Cinemachine

- 状态：`DEFER`
- 来源：<https://packages.unity.com/com.unity.cinemachine>、<https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.cinemachine.html>、<https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html>、<https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/license/LICENSE.html>
- 版本/兼容性：official registry `latest=3.1.7`、package metadata `unity=2022.3`；Unity 6000.0 手册发布基线 `3.1.5`，因此 `3.1.7` 只可作为待试验候选，尚未在本工程验证
- 许可证/商用/再分发：Unity Companion License v1.4，条件同上；保留 license/third-party notices，不独立再分发 package
- 依赖：`com.unity.splines@2.0.0`、`com.unity.modules.imgui@1.0.0`
- 二进制风险：官方 registry；未下载，package 内容和所有可选集成未审查，标记 `UNVERIFIED until trial import review`
- SHA-256：`N/A - not downloaded`；registry SHA-1 `f3f96bcb59af572d4d28704d53f6388471d7dd21`
- 导入路径：UPM cache + manifest/lock；会在正式 Camera Scene/Prefab 上新增 Brain/Camera/Extension 等 serialized component
- 真实收益：多机位、跟随/构图、阻挡、镜头混合和 Timeline 集成；只有出现可量化的新镜头需求时才有收益。
- 当前成本：稳定 360 度棋盘镜头已经由 `BoardOrbitCameraController` 处理右键旋转、中键平移、滚轮和 UI 抑制；本阶段禁止重写该契约。
- 性能/视觉影响：活动相机会逐帧求解构图、阻尼和可选碰撞；视觉影响直接且可能改变旋转手感、俯仰/距离限制、遮挡和宽屏 framing，必须重新做全交互及三视口证据。
- 加入路径：只能在隔离分支/试验 Scene 建立等价性基线；固定包版本和 Splines；不改正式 Scene，先量化镜头需求、CPU 与视觉差异；获批后才由主智能体改正式 Camera Prefab/Scene。
- 移除/回滚：先恢复原 Camera/`BoardOrbitCameraController` 的 serialized 值与引用；删除所有 Cinemachine components/assets；从 manifest 移除 Cinemachine，并仅在无人使用时移除 Splines；确认 lock 收敛，重跑相机交互、resize、build、Player smoke。

### Addressables

- 状态：`DEFER`
- 来源：<https://packages.unity.com/com.unity.addressables>、<https://docs.unity.cn/6000.0/Documentation/Manual/com.unity.addressables.html>、<https://docs.unity3d.com/Packages/com.unity.addressables@3.1/manual/index.html>、<https://docs.unity3d.com/Packages/com.unity.addressables@3.1/license/LICENSE.html>
- 版本/兼容性：official registry `latest=3.1.0`、package metadata `unity=6000.0`；Unity 6000.0 早期手册发布基线 `2.0.8`
- 许可证/商用/再分发：Unity Companion License v1.4，条件同上；除 package license 外必须保留其 third-party notices，远程内容的资产许可证仍需逐资产满足
- 依赖：`com.unity.profiling.core@1.0.2`、`com.unity.test-framework@1.4.5`、`com.unity.scriptablebuildpipeline@3.1.1`，以及 built-in `assetbundle`、`jsonserialize`、`imageconversion`、`unitywebrequest`、`unitywebrequestassetbundle`
- 二进制风险：官方 registry；会引入内容构建管线和更多 transitive package。未下载/检查 package 或其 transitive 内容，标记 `UNVERIFIED until trial import review`。
- SHA-256：`N/A - not downloaded`；registry SHA-1 `9bffe1a216ceaa96bece9c54a1091e23335c6952`
- 导入路径：UPM cache + manifest/lock；首次初始化通常生成 `Assets/AddressableAssetsData/**`；构建还会生成 Library、StreamingAssets/ServerData 或配置的本地/远程输出
- 真实收益：地址/标签加载、异步依赖解析、AssetBundle 构建、远程内容和增量更新；只有资源规模、平台内存或远程交付需求达到阈值时值得采用。
- 当前成本：运行时只有 `CombatCompositionRoot` 两次 `Resources.Load`（卡面 texture/sprite fallback），其余稳定 Scene/Prefab 使用 serialized references；直接迁移会把小问题变成新的内容构建系统。
- 性能/视觉影响：增加 catalog、bundle、build 时间和缓存管理；运行时异步加载可改善峰值内存，但错误分组会造成重复依赖、首载等待、内存峰值或缺失/延迟卡面，需用 build layout 和 Player profiling 验证。
- 加入路径：先在隔离分支定义可量化阈值与一个非正式 fixture；固定 `3.1.0` 并审查全部 transitive/license；配置本地 group/profile；验证 analyze/build layout、冷/热加载、失败恢复、Windows build 与 Player；禁止首试验迁移正式卡面或 SceneFlow。
- 移除/回滚：先把所有 `AssetReference`/Addressables load 恢复为原 serialized/Resources 路径；验证不再有 address key；删除仅由试验创建的 `AddressableAssetsData`、groups、profiles、StreamingAssets/ServerData 输出；从 manifest 移除 package，确认 SBP/profiling 等 transitive 无其他使用后由 lock 清除；重建 Player 并验证卡面完整。优先 revert 独立试验 checkpoint。

### Timeline

- 状态：`DEFER`
- 来源：<https://packages.unity.com/com.unity.timeline>、<https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.timeline.html>、<https://docs.unity3d.com/Packages/com.unity.timeline@1.8/manual/index.html>、<https://docs.unity3d.com/Packages/com.unity.timeline@1.8/license/LICENSE.html>
- 版本/兼容性：official registry `latest=1.8.12`、package metadata `unity=2022.3`；Unity 6000.0 手册发布基线 `1.8.10`。工程仅直引低层 `com.unity.modules.director@1.0.0`，不等于已安装完整 Timeline authoring package。
- 许可证/商用/再分发：Unity Companion License v1.4，条件同上；保留 license/third-party notices，不独立再分发 package
- 依赖：built-in `audio@1.0.0`、`director@1.0.0`、`animation@1.0.0`、`particlesystem@1.0.0`
- 二进制风险：官方 registry；未下载检查 package 内容，标记 `UNVERIFIED until trial import review`
- SHA-256：`N/A - not downloaded`；registry SHA-1 `8cd8509afae5d2655af4657a214db1b563c28425`
- 导入路径：UPM cache + manifest/lock；获批后 TimelineAsset/clip 只能放入明确的表现层目录，例如 `Assets/_Project/Timelines/**`，绑定保存在 Scene/Prefab
- 真实收益：可视化编排镜头、动画、音频和 VFX；适合可预测的表现序列，不适合卡牌提交、敌人行动、回合推进或胜负结算。
- 当前成本：只读扫描没有 `PlayableDirector`、`TimelineAsset`、`PlayableAsset` 或 `UnityEngine.Timeline` 使用。现有动画已稳定，不需要新增 authoring 系统。
- 性能/视觉影响：仅活动 Director 评估 Playables graph；成本取决于 track/clip/绑定数量。它会直接改变动画时序和画面，宽屏、跳过、Scene rebind 与 timescale 都需重新验证。
- 加入路径：只在隔离表现 fixture 中固定 `1.8.12`，建立无玩法 side effect 的 clip；验证停止/跳过/Scene unload 后状态恢复；任何 gameplay outcome 仍由 Application/Domain typed contract 产生。
- 移除/回滚：先恢复 Animator/代码表现入口和 Prefab bindings；删除 TimelineAsset、PlayableDirector 和自定义 tracks；从 manifest 移除 package；确认 lock 收敛后重跑动画、SceneFlow、build、Player smoke。

## 加入与移除门禁

任何未来候选 trial 都必须同时满足：

1. 单独 checkpoint，固定 exact version，保存 trial 前 manifest/lock/ProjectSettings/Scene/Prefab hash。
2. Package Manager 只从 `packages.unity.com` 取包；记录最终 lock、依赖、license、third-party notices 和下载包 SHA-256。
3. 先在隔离 fixture 验证收益，不触碰正式战斗规则、SceneFlow、相机、卡牌交互或稳定 Scene/Prefab。
4. 通过专属 EditMode/PlayMode、受影响 build、Player smoke 和实际视觉检查后才可从 `TRIAL` 升级。
5. 移除路径必须反向恢复 serialized components/assets/settings，再移除 direct package；禁止只删 manifest 留下丢脚本或孤立引用。
6. 移除后重新解析 lock，确认仅移除无人使用的 transitive；运行 `git diff --check`、精确 staged-file 审查和与加入前 hash 对比。

本阶段没有执行加入或移除操作，所以没有 package cache、下载二进制、SHA-256、generated settings 或正式资产需要清理。

## 首切片约束建议

- UI Toolkit 可用于 SceneContractValidator 或 CardAssetAudit 的 EditorWindow，因为它已存在且不改变 Player。
- 不应为了首切片安装 Input System、Cinemachine、Addressables 或 Timeline。
- 新工具必须位于 Editor-only assembly，结构化诊断逻辑与窗口展示分离，测试直接调用只读扫描 API。
- 现有 EditMode asmdef 尚未引用 `TimeKey.Editor`；由主智能体决定最小 asmdef 变更并单独审查。
- 保持 `Domain/Application -> UnityEditor/UnityEngine` 为零依赖，保持 runtime assembly 不引用新 Editor module。

## 官方许可证摘要

Unity Companion License v1.4（2024-10-29）授予全球、非独占、免费且免版税的权利，但只允许在有效 Unity Engine License 下开发或分发 Unity-dependent content；要求在 Work 的大量部分保留许可证和 third-party notices；禁止竞品分析或开发竞争产品。以上为工程审计摘要，不替代法律意见。
