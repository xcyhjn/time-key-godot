# Wave 04 Audio Agent Prompt

## 角色与单一目标

你负责 Gate A 音频基础：把现有 3 BGM/10 SFX 的可观察 cue 语义变成可测试、可维护、可回滚的 Unity
AudioMixer/cue/pool 候选。你不是仓库唯一工作者，必须保护其他人的修改。

## 必读资料

- `docs/migration/unity-3d/00-bootstrap/START_HERE_PROMPT.md`
- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_CONTENT_AND_EXPERIENCE_PROMPT.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts-wave-04.md`
- `docs/migration/unity-3d/04-verification/evidence/wave-04-gate-0/asset-license-ledger.md`
- `docs/migration/unity-3d/06-maintenance/plugin-and-asset-tooling-guide.md`
- `scene/global/sound_manager.gd`、`default_bus_layout.tres`
- `unity/Assets/_Project/Runtime/Presentation/MainMenu/MainMenuPresenter.cs`
- `unity/Assets/_Project/Runtime/Composition/SceneFlow/MainMenuSceneNavigation.cs`

使用 `Verification & Quality Assurance` 思路记录可解析 XML/结构化诊断；不引入第三方包。

## 独占所有权

允许写：

- `unity/Assets/_Project/Runtime/Presentation/Audio/**`
- `unity/Assets/_Project/Tests/EditMode/Audio/**`
- `unity/Assets/_Project/Tests/PlayMode/Audio/**`
- `unity/Assets/_Project/Audio/Wave04/**` 与 `unity/Assets/_Project/Prefabs/Audio/**` 的独立候选
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-04-audio.md`

禁止写：Bootstrap/MainMenu/OutOfBattle/Combat/GameOver Scene 或 Prefab、MainMenu settings/navigation、
SceneFlow、Composition、Domain/Application、任何共享 asmdef、Package/ProjectSettings、共享文档、
历史 evidence、其他 Agent 路径。不要把候选接入正式 Scene；主智能体在 Gate C 集成。

## 实现契约与非目标

实现 Inspector 可编辑 cue catalog、唯一 AudioRoot runtime binding、Master/Music/SFX Mixer group、
双 BGM source、looping SFX source、有限 one-shot pool、幂等切换/交叉淡入淡出、暂停和缺失 clip/池
耗尽诊断。运行时状态不得进 ScriptableObject；BGM/SFX 设置通过公开接口供主智能体后接入，但你不改设置
Presenter。保留 13 个稳定 id 和原 OGG 字节；Dialogic 示例 WAV 保持 HOLD，不复制。

## 验收与报告

编写 EditMode/PlayMode 测试覆盖 catalog 映射、Mixer 路由、音量/持久化接口、重复 bind/rebind、crossfade、
缺资源、池耗尽、pause/disable/destroy。可运行时在 `1280x720`、`1920x1080`、`2560x1080` 做独立
Audio candidate evidence，并记录实际 clip 时长、声道、采样率、导入压缩设置和 hash；不能用截图替代听觉
验收，Player 听觉不可用时明确 `未完成听觉验收`。

不切分支、不 stash、不回退、不暂存、不 commit、不 push，不并发运行 Unity；遇到共享契约冲突立即停止
写入并报告主智能体。回报格式：改动路径、契约、测试/XML、候选资产与许可证、视觉/听觉证据、失败/风险、
移除步骤、建议下一步。
