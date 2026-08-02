# Combat Shell Gate C authoring

Run the idempotent authoring entry from Unity or batch mode:

```text
Time Key/Author Combat Shell Gate C
TimeKey.Editor.CombatShellGateCAutomation.AuthorGateC
```

The authoring step creates or refreshes:

- `Prefabs/Shell/GameStartLogo.prefab`
- `Prefabs/Shell/MainMenu.prefab`
- `Prefabs/Shell/TransitionVisual.prefab`
- `Resources/Art/Shell/MainMenu/{clock_noring,ring,point}.png`
- `Resources/Art/Shell/GameStart/key.png`
- `Resources/Art/Shell/MainMenu/Buttons/*.png`
- formal `GameStart`, `MainMenu` and persistent `Bootstrap` connections

`GameStartLogo` owns the serialized key/character references, duration, travel distance and skip policy. `MainMenu` owns all stable RectTransforms, layered entrance CanvasGroups, source button textures, settings controls and Silver text references. Runtime-only navigation, settings persistence/system application, quit and payload creation remain in Composition; do not add SceneManager, PlayerPrefs, AudioListener, Screen or payload construction to a Presenter.

The content canvases use a 1920x1080 reference resolution with balanced width/height matching. Verify 1280x720, 1920x1080 and 2560x1080 after layout edits. Settings, modal or seed overlays must keep `blocksRaycasts=true` while visible and release only their own `SceneInputLockState` lease when closed. Master volume/fullscreen are the only connected settings; keep Music/SFX visibly disabled until real channels exist.

The Bootstrap overlay uses `TransitionVisualPresenter` through `TransitionCanvasPresenter`. Cover completion is awaited before the old camera is disabled; reveal completion and the target content reveal are both awaited before input unlock. Never replace this with a fixed delay.

Source key, clock and button files are read-only. The author copies them into Unity and the Gate C asset test checks byte equality. Public redistribution remains blocked by MIG-005 until image licensing is resolved.
