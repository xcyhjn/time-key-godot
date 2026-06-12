# in_scene.gd 维护说明

日期：2026-06-12

## 当前职责

`scene/in_scene/in_scene.gd` 是局内战斗场景的 composition root。它继续负责把地图、卡牌系统、时间轴、奖励结算、胜负流程、局外返回和 UI 状态接起来。

## 已拆模块

已拆模块位于 `scene/in_scene/in_scene_modules/`：

- `bridges/`：场景节点和全局时钟桥接。
- `cards/`：卡牌系统初始化、抽牌、牌堆 UI、强制弃牌。
- `ui/`：输入锁、UI 显隐、tooltip、hover、顶部战斗 UI。
- `turn/`：首回合入场等待和敌人意图刷新。
- `scene_flow/`：外部 payload、局外返回和场景切换。
- `settlement/`：战斗胜负、奖励页入口、奖励消费、牌堆快照回收。

## 不要继续硬拆

- 不要为了减少行数拆 `_ready()` 的配置组装。
- 不要同批拆回合推进、奖励返回和场景切换。
- 不要让新模块反向持有整个局内生命周期。

## 后续可做

只有在补足完整手动回归路径后，才考虑继续拆回合推进或场景切换 wrapper。优先新增小模块，不要重写局内主流程。

## 验证入口

改动后至少运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```
