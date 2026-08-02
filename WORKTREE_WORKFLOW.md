# Unity 多 Worktree 开发约定

## 分支职责

| 分支 | 主要负责内容 |
| --- | --- |
| `integration/fla-presentation` | 集成、冲突处理和最终验证 |
| `feature/fla-global-transform` | `SpriteTransform`、`SpriteGroup`、共享 Shader 和核心测试 |
| `feature/sunflower-presentation` | SunFlower Prefab、动画、运行时代码和专项测试 |
| `feature/sunshroom-presentation` | SunShroom Prefab、动画、生成器和专项测试 |

## 规则

1. 每个 worktree 同时最多打开一个 Unity Editor，禁止共享 `Library`、`Temp` 和 `UserSettings`。
2. 同一个 `.prefab`、`.anim`、`.controller` 或 `.unity` 文件同一时间只能由一个功能分支修改。
3. 新增、移动或删除 Unity 资源时，必须连同对应 `.meta` 一起提交。
4. `Assets/Scenes/SampleScene.unity` 默认只在 integration 分支修改。
5. 功能分支合并前，先合并或变基最新 integration，并运行相关 EditMode 测试。

## 推荐合并顺序

1. `feature/fla-global-transform` 合并进 integration。
2. SunFlower 和 SunShroom 分支同步最新 integration。
3. 两个植物分支分别完成测试后合并进 integration。
4. integration 完成全量验证后再合并进 `main`。

## 常用命令

```powershell
git -C <worktree> status --short --branch
git -C <feature-worktree> merge integration/fla-presentation
git -C <integration-worktree> merge --no-ff <feature-branch>
git worktree list
```
