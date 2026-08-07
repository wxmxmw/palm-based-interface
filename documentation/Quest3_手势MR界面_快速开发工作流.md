# Quest 3 手势 MR 界面：快速开发工作流

## 当前项目结论

项目目前是 **Unity 6.3.21f1 + URP 的空白模板**：仅有 `Assets/Scenes/SampleScene.unity`，未安装 XR/Meta 相关包，也没有业务脚本。故不要从零实现手部骨骼、射线或捏合识别；使用 Meta XR SDK 的现成 Passthrough、Hand Tracking、Ray/Pinch 交互，再只实现需求特有的“右手掌心朝上开掌切换”。

> 以 0.7 秒作为默认值，并将其序列化为 Inspector 参数；验收前可修改调整。

## 最短实施路径（按顺序完成）

### 1. 一次性完成项目与设备配置

1. 在 Package Manager 安装 **Meta XR All-in-One SDK**（含 Core、Interaction、Platform/Building Blocks）；启用 OpenXR / Meta Quest 支持。
2. 执行 Meta XR Project Setup 的 *Fix All*，目标平台改为 Android，纹理/图形使用 Quest 推荐项。
3. 在 `SampleScene` 加入 Meta 的 MR/Passthrough + Hand Tracking 预制体（优先使用 SDK 的 Building Block）。设置：仅手部输入、左右手启用、控制器不作为交互输入。
4. 真机启动后先验证两件事：能看见透视环境；双手模型和手部射线可用。未通过前不做业务 UI。

完成定义：Quest 3 上没有天空盒或不透明背景，且不连接控制器时手部追踪正常。

### 2. 搭建一个可独立验证的 UI Prefab

1. 创建 `Assets/Prefabs/PalmMenu.prefab`，用 World Space Canvas 或 SDK 推荐的空间 UI 容器。
2. 层级固定为 `PalmMenuRoot / Panel / MessageText / Button1 / Button2`；文案默认分别为“系统已激活”“按钮 1”“按钮 2”。
3. 为两个按钮接入 SDK 的手部射线悬停和 Pinch Select；使用其 Hover/Select 状态做颜色、缩放或描边反馈，不自行模拟点击。
4. `PalmMenuRoot` 初始 `SetActive(true)`；启动即显示菜单并允许交互。被隐藏时，同时关闭 Canvas 的交互组件/射线目标，确保没有不可见点击。

完成定义：菜单开启时能看到两按钮的悬停、按下与“一次捏合一次点击”；关闭时无法被射线点击。

### 3. 实现唯一的自定义脚本：菜单状态机

创建 `Assets/Scripts/PalmMenuController.cs`，职责严格限制如下：

```text
Visible（启动初始状态） --(有效姿势连续 holdSeconds)--> Hidden
Hidden --(有效姿势连续 holdSeconds)--> Visible
任一状态 --(切换后 cooldownSeconds)--> 保持该状态，忽略触发姿势
```

脚本公开以下可调参数：

| 参数 | 默认值 | 判定 |
| --- | ---: | --- |
| `holdSeconds` | 0.7 s | 姿势连续成立时长 |
| `cooldownSeconds` | 1.0 s | 每次切换后的锁定时长 |
| `minHeightAboveHead` | -0.25 m | 右手至少在胸前附近（可按真机调） |
| `palmUpMaxAngle` | 45° | 右手掌心法线与世界上方向的夹角上限 |
| `openHandThreshold` | SDK 手势置信度/五指伸展 | 五指展开 |
| `menuDistance` | 0.8 m | 首次显示时距视线前方的距离 |

每帧逻辑：

1. 读取**右手**追踪状态；未追踪或置信度不足，重置本次姿势计时，保持界面状态。
2. 同时验证：手高度、五指展开、`Angle(palmNormal, Vector3.up) <= 45`。
3. 条件持续到 `holdSeconds` 且不在冷却期时切换一次；切换后重置计时并进入冷却。
4. 姿势失效后才允许下次重新累计，避免一次持续开掌重复切换。

> 手掌法线、五指展开和追踪置信度均应从已安装 Meta SDK 的手部数据 API 获取；该脚本不负责捏合点击。

### 4. 真机调参、验收与构建

依序在 Quest 3 上做以下短测，每一项通过才进入下一项：

1. 冷启动：透视正常，菜单默认显示且可交互。
2. 右手开掌、掌心向上、保持阈值时间：菜单只隐藏一次；持续不松手不会反复切换。
3. 松手、等待冷却后重复该手势：菜单只显示一次。
4. 不连接控制器：对两个按钮分别完成“悬停 → 捏合 → 松开 → 再捏合”，各回调一次，反馈正确。
5. 故意移出右手或短暂遮挡：菜单状态不变化，不能误触发。
6. 清查范围：没有空间锚点、场景网格/遮挡、真实物体识别、多窗口、语音、账号、网络或持久化功能。

最后用 Android Development Build 安装测试；通过后关闭 Development Build，生成交付 APK。

## 建议的最小文件结构

```text
Assets/
  Scenes/SampleScene.unity                 # 唯一场景
  Prefabs/PalmMenu.prefab                  # 面板和两个按钮
  Scripts/PalmMenuController.cs            # 状态机、姿势判定、菜单跟随
  Scripts/MenuButtonCallbacks.cs           # 两个本地 UnityEvent 占位回调（可选）
```

## 里程碑与风险控制

| 里程碑 | 产出 | 不通过时优先检查 |
| --- | --- | --- |
| M1：平台 | Passthrough + 双手正常 | Meta XR Project Setup、Quest 权限、Android/OpenXR 配置 |
| M2：交互 | Pinch 可点击测试按钮 | 手部射线 Interactor、Canvas/交互层、按钮的 Hover/Select 绑定 |
| M3：状态 | 开掌稳定显示/隐藏 | 右手限定、法线方向、持续计时、姿势释放与冷却 |

这条路径的关键是：**SDK 负责 MR、手部射线和捏合；项目代码只负责需求中的状态机。** 这样能以最少代码完成需求，也便于在 Quest 3 真机上调阈值。
