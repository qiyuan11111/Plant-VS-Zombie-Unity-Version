# PvZ Reanimation XML 到 Unity 预设体与动画：通用 SOP

> 适用对象：植物、僵尸、阳光、子弹、掉落物、关卡物品及其他由分层 PNG 组成的 Reanimation 资源。
>
> 普通僵尸只作为文末示例；正文流程不得硬编码某一种资源的名称、部件、Layer、动画状态或 Root Motion 规则。

## 1. 目标

把一组 Reanimation XML 和原始 PNG 转换为 Unity 中可复用、可验证的资源：

- 正确导入的 Sprite 及 `.meta`；
- 一个或多个 `AnimationClip`；
- 一个 `AnimatorController`；
- 一个部件完整的 Prefab；
- 可重复执行的构建配置与验收结果。

必须保证：

- XML 可见层、PNG、Prefab 部件和动画绑定能够追溯；
- PPU 来自 PNG 原始像素尺寸与 XML `width/height` 的计算；
- 动画曲线写入正确组件和属性；
- 控制层不被误当作图片；
- 重新生成时尽量保留资产 GUID；
- 植物、僵尸和物品的差异由“资源配置”表达，不散落成硬编码判断。

## 2. 每个资源先建立资源配置

开始转换前，先填写以下配置。没有确认的字段不能靠复制其他预设体来猜。

| 配置项 | 含义 | 示例 |
| --- | --- | --- |
| `AssetId` | 资源唯一名称 | `ZombieNormal`、`PeaShooterSingle`、`Sun` |
| `Category` | `Plant`、`Zombie`、`Item`、`Projectile`、`Effect` | `Zombie` |
| `XmlClips` | 状态名到 XML 的映射 | `walk → Zombie_anim_walk1.xml` |
| `MasterPartPolicy` | 部件集合来源：主 XML 或全部动画并集 | `MasterXml: walk` |
| `SpriteDirectory` | 原始 PNG 目录 | `.../Sprite` |
| `PrefabPath` | 输出 Prefab | `.../ZombieNormal.prefab` |
| `AnimationDirectory` | `.anim` 和 Controller 输出目录 | `.../Animation` |
| `LayerAliases` | XML 层名到 PNG/节点名映射 | `anim_head1 → Zombie_head` |
| `ControlLayers` | 无图片控制层及其处理器 | `_ground → RootMotionX` |
| `Fps` | XML 帧率 | `12` |
| `LoopClips` | 需要循环的状态 | `idle`、`walk` |
| `DefaultState` | Controller 默认状态 | `walk` |
| `Material` | Sprite 共用材质 | `LightnessSkew.mat` |
| `UnityLayer` | 根对象和部件所在 Layer | `Plant`、`Zombie` 等 |
| `SortingLayer` | Sprite Sorting Layer | `plant`、`zombie-0` 等 |
| `RequiredComponents` | 业务脚本和表现组件 | 植物类、僵尸类、`SpriteGroup` 等 |
| `ColliderProfile` | Collider 类型、尺寸、Offset、Trigger | 按玩法配置 |
| `MotionPolicy` | `None`、`RootMotion` 或代码移动 | 僵尸 walk 使用 `RootMotion` |

资源配置是后续所有判断的唯一入口。不得因为当前示例是僵尸，就默认其他资源也使用 `walk`、Zombie Layer 或 Root Motion。

## 3. 标准流程

```text
收集 XML/PNG
→ 建立资源配置
→ 解析全部 XML
→ 确定部件集合
→ 映射 PNG
→ 计算并写入 PPU
→ 同步 Prefab 层级
→ 生成各 AnimationClip
→ 处理控制层
→ 创建/更新 Controller
→ 挂载业务组件
→ 自动校验
→ 实际播放验收
```

任何一步发现缺图、未知层名、尺寸冲突或绑定丢失，应停止生成并输出明确名称，不能静默跳过。

## 4. 收集和保存输入

1. 找到用户指定的最新版 XML；文件名相似时，以用户明确指定的版本为准。
2. 将源 XML 原样复制进项目，例如 `Assets/StreamingAssets/ReanimationSource/<AssetId>/`。
3. 比较源文件和项目副本的 SHA-256，确保内容一致。
4. 收集所有候选 PNG，但不要仅凭文件夹中存在图片就自动加入 Prefab。
5. PNG 是否属于 Prefab，必须由 XML 可见层集合决定。
6. 保留原始 XML 和 PNG；生成器只修改 Unity 资产与导入元数据。

## 5. 解析 XML 并分类 Layer

读取 `<animate>` 下的所有 `<layer>`，记录：

- `name`；
- `width`、`height`；
- `<frame>` 数量；
- 第一帧与最后一帧 `index`；
- 是否有图片/可见性切换信息；
- 是否属于已注册的控制层。

每个 `<frame>` 通常需要：

- `index`；
- `posx`、`posy`；
- `scalex`、`scaley`；
- `skewx`、`skewy`。

Layer 分为三类：

1. 可见部件层：映射到 PNG、Prefab 节点和 `SpriteTransform` 曲线。
2. 控制层：例如 `_ground`，没有 PNG，由专用处理器转换。
3. 未知层：既没有 PNG 映射，也没有控制层处理器；必须报错并等待确认。

## 6. 确定 Prefab 部件集合

按资源配置选择一种策略：

### 主 XML 策略

指定一个动画为部件权威来源，例如“以 walk1 为主”。Prefab 与主 XML 的可见层完全一致；其他动画允许只绑定其中一部分，但不能引用 Prefab 中不存在的节点。

适用于：用户明确指定主动画、其他动画尚未更新或旧动画包含历史备用部件。

### 全动画并集策略

Prefab 部件集合取所有动画 XML 可见层的并集。仅某些状态使用的部件保留在 Prefab 中，并由可见性曲线控制。

适用于：同一资源在不同状态确实会替换头、手、特效或其他部件。

无论采用哪种策略：

- Layer 先经过 `LayerAliases` 映射，再参与集合比较；
- 缺少的部件应创建；
- 不在期望集合中的旧节点应从 Prefab 移除；
- PNG 源文件可以保留，不等于必须挂入 Prefab；
- 自动校验必须比较名称集合，不能只比较数量。

## 7. Layer 名称与 PNG 映射

默认规则：XML Layer 名称直接对应同名 PNG 和 Prefab 节点。

名称不一致时，在资源配置中显式登记别名，例如：

```text
anim_head1      → Zombie_head
anim_head2      → Zombie_jaw
anim_innerarm1  → Zombie_innerarm_upper
```

要求：

- 一个可见 Layer 只能映射到一个目标节点；
- 多个 Layer 映射同一目标时，必须确认它们不会在同一 Clip 中产生冲突；
- 找不到 PNG 时报告 XML Layer 名、解析后的目标名和搜索目录；
- 不允许用“最像的文件名”自动覆盖已有明确映射。

## 8. 计算 Pixels Per Unit

对每个可见 Layer，读取对应 PNG 的原始像素宽高：

```text
PPU_width  = PNG原始像素宽 / layer.width
PPU_height = PNG原始像素高 / layer.height
```

判定规则：

1. 两个结果误差不超过 `0.0001` 时，使用平均值。
2. 不一致时，分别逼近简单分数，建议检查分母 1～12，例如 `1`、`1.25`、`1.3333333`、`1.5`。
3. 选择误差更小、更像简单分数的候选值，并产生警告供人工复核。
4. 同一个 PNG 被多个 Clip 使用时，所有 XML 给出的 PPU 必须一致；不一致时停止生成。
5. PPU 是图片导入属性，每张图片只有一个值，不能随帧变化。
6. 不通过裁剪 PNG、修改动画 Scale 或设置不同 X/Y 缩放来掩盖 PPU 错误。

将结果写入：

```csharp
TextureImporter.spritePixelsPerUnit = calculatedPpu;
```

推荐的其他导入设置：

```text
Texture Type          = Sprite (2D and UI)
Sprite Mode           = Single
Mip Maps              = Off
Alpha Is Transparency = On
Filter Mode           = Point
Wrap Mode             = Clamp
Compression           = Uncompressed
```

## 9. 创建或同步 Prefab

推荐部件结构：

```text
<AssetId>（根对象）
└─ <PartName>（SpriteTransform）
   └─ __AffineContent（SpriteRenderer）
```

`SpriteTransform` 通用初始值：

```text
position       = (0, 0)
scale          = (100, 100)
skew           = (0, 0)
brightness     = 1
alpha          = 1
alphaCoef      = 1
updatePosition = true
```

随后按资源配置设置：

- Unity Layer；
- Sorting Layer 和 Sorting Order；
- 共用材质；
- Animator；
- 业务组件；
- Collider；
- 资源注册或 GameConfig 引用。

注意：

- 植物、僵尸、物品的业务脚本和 Collider 不同，不能从示例硬复制；
- 更新现有 Prefab 时尽量原位修改，保留 Prefab GUID；
- 新增部件时创建完整的 `SpriteTransform/__AffineContent/SpriteRenderer` 层级；
- 删除部件前确认它不属于“全动画并集策略”中的其他状态。

## 10. 生成 AnimationClip

每个 XML 对应一个 Clip。时间换算：

```text
Unity时间（秒） = frame.index / Fps
```

常用曲线映射：

| XML 属性 | Unity 绑定 |
| --- | --- |
| `posx` | `SpriteTransform.position.x` |
| `posy` | `SpriteTransform.position.y` |
| `scalex` | `SpriteTransform.scale.x` |
| `scaley` | `SpriteTransform.scale.y` |
| `skewx` | `SpriteTransform.skew.x` |
| `skewy` | `SpriteTransform.skew.y` |

处理要求：

1. 使用 Invariant Culture 解析小数。
2. 重新生成前清除目标 Clip 的旧曲线。
3. 原位更新已有 `.anim`，保留 GUID 和 Controller 引用。
4. 关键帧切线按源格式选择；当前 Reanimation 转换默认使用 Linear。
5. 设置 `clip.frameRate = Fps`。
6. 按 `LoopClips` 配置 `loopTime`，不能默认所有动画都循环。
7. 如果 XML 提供显示/隐藏信息，应生成对应 `GameObject.m_IsActive` 或项目约定的可见性曲线。
8. Clip 的每条非控制绑定都必须能在 Prefab 中找到目标组件。
9. 本次只处理某一个动画时，加载其他现有 Clip，不得顺便重建或覆盖。

## 11. 控制层与运动策略

控制层必须注册处理器。没有处理器的控制层不能忽略。

### `_ground` / Root Motion 示例

只有资源配置声明 `MotionPolicy = RootMotion` 时才启用。

`_ground` 不创建 GameObject、不寻找 PNG。若其 X 增大表示角色相对地面向左前进：

```text
rootX(frame) = -(ground.posx(frame) - ground.posx(firstFrame))
```

写入：

```text
path         = 空字符串（Prefab 根对象）
type         = Transform
propertyName = m_LocalPosition.x
```

并设置：

```text
Animator.applyRootMotion = true
Animator.cullingMode     = AlwaysAnimate
```

根位移必须放在当前移动角色自己的 Prefab 根对象上，不能放到某个图片部件、场景地面或其他资源的 Prefab 上。

### 不使用 Root Motion 的资源

- 大多数植物：`MotionPolicy = None`，`Animator.applyRootMotion = false`。
- 由游戏逻辑移动的子弹或物品：保留代码移动，不能再叠加 Root Motion。
- 静态物品：控制层按配置忽略或转换为局部表现曲线，但必须有明确规则。

## 12. 创建或更新 AnimatorController

1. 每个 XML Clip 对应一个语义清晰的状态名。
2. 默认状态来自资源配置，不能统一设成 walk。
3. 循环、速度、Transition、参数和 StateMachineBehaviour 按资源玩法配置。
4. 原位更新 Controller，避免破坏 GUID。
5. 只更新本次指定状态，不改动无关状态、参数或 Transition。

常见配置：

| 类型 | 常见默认状态 | Root Motion | 额外要求 |
| --- | --- | --- | --- |
| 植物 | `idle` | 通常关闭 | 攻击、闪烁、生产等状态 |
| 僵尸 | `walk` | 按 XML/玩法决定 | 攻击、受伤、死亡状态 |
| 物品/阳光 | `idle` 或 `spawn` | 通常关闭 | 收集、消失、抛物线逻辑 |
| 子弹 | `idle`/`fly` | 通常关闭 | 代码速度、命中和销毁 |
| 特效 | `play` | 关闭 | 非循环，播放完回收 |

## 13. 自动校验

构建器至少执行以下检查：

- 源 XML 与项目副本一致；
- 主 XML/并集策略得到的可见部件名称集合与 Prefab 一致；
- `SpriteTransform` 和 `SpriteRenderer` 数量、名称一致；
- 每个可见 Layer 都有 PNG、PPU 和曲线绑定；
- 每个动画绑定路径都存在；
- 没有未知控制层；
- PPU 横纵计算一致，或已产生可见警告；
- Controller 状态引用正确 Clip；
- 必需组件、Layer、Sorting Layer、材质和 Collider 正确；
- Root Motion 开关符合 `MotionPolicy`；
- Unity Console 无编译错误、MissingReference 和导入异常。

## 14. 实际播放验收

自动检查通过后仍需实例化 Prefab 播放：

1. 采样第一帧，检查部件没有集中到原点、尺寸异常或层级颠倒。
2. 播放完整一轮，检查所有部件连续运动。
3. 循环 Clip 至少播放超过一轮，检查接缝。
4. 有 Root Motion 时记录根对象开始和结束位置，确认方向和累计位移。
5. 检查是否同时存在脚本移动和 Root Motion，避免双倍位移。
6. 切换 Controller 中相关状态，确认未更新的动画仍可正常使用。
7. 在实际 Sorting Layer、摄像机和材质环境中检查一次画面。

## 15. 通用自动化建议

后续不要为每个资源复制一份完全独立、充满硬编码的 Builder。推荐拆成：

```text
ReanimationAssetProfile
├─ 路径、类型、FPS、动画状态
├─ LayerAliases
├─ ControlLayerHandlers
├─ PartSetPolicy
├─ Import/Sorting/Material 设置
├─ MotionPolicy
└─ RequiredComponents/ColliderProfile

ReanimationPrefabBuilder
├─ ParseXml
├─ ResolveParts
├─ CalculatePpu
├─ ConfigureTextures
├─ BuildClips
├─ SynchronizePrefab
├─ ConfigureController
└─ ValidateAndPlayback
```

每次新增植物、僵尸或物品时，只新增或更新 `ReanimationAssetProfile`；通用解析、PPU、曲线和校验逻辑应复用。

## 16. 普通僵尸示例（非通用默认值）

当前普通僵尸配置仅作为验证案例：

- 主 XML：`Zombie_anim_walk1.xml`；
- `Fps = 12`；
- 主 XML 包含 16 个可见部件和 `_ground`；
- `anim_innerarm1 → Zombie_innerarm_upper`；
- `Zombie_innerarm_upper` 的 PPU 为 `1.0`；
- 其余 15 个可见部件的 PPU 为 `1.25`；
- `_ground` 转换为根对象向左 Root Motion；
- 默认状态为 `walk`；
- 当前实现：`Assets/Scripts/Editor/PrefabOptimizers/ZombieNormalPrefabBuilder.cs`。

这些值不能自动套用到植物、其他僵尸或物品。

## 17. 最终验收清单

- [ ] 已填写完整资源配置。
- [ ] 使用的是用户指定的最新版 XML。
- [ ] 已明确选择主 XML策略或全动画并集策略。
- [ ] 可见层、控制层和未知层已分类。
- [ ] 所有可见层都有唯一 PNG 映射。
- [ ] Prefab 部件名称集合与期望集合完全一致。
- [ ] 每张图片的 PPU 均由原始像素尺寸和 XML `width/height` 算出。
- [ ] 多动画引用同一 PNG 时 PPU 一致。
- [ ] 每个 Clip 的 FPS、Loop 和切线符合配置。
- [ ] 未修改本次范围外的 Clip、Controller 状态或业务设置。
- [ ] 控制层都有明确处理器。
- [ ] Root Motion 与代码移动没有叠加。
- [ ] Prefab 的组件、Layer、Sorting、材质和 Collider 符合资源类型。
- [ ] 自动校验通过。
- [ ] 实际播放超过一轮并通过画面检查。

## 18. 交付记录

每次使用本 SOP 转换资源，交付时记录：

- 使用的 XML 文件名和哈希；
- 部件数量、增加/删除的部件名；
- PPU 结果及所有不一致警告；
- 生成或修改的 Prefab、Clip、Controller 路径；
- 控制层处理方式；
- 自动校验和实际播放结果；
- 本次明确未处理的动画或玩法逻辑。