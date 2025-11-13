# Player 系统使用说明

## 📦 文件结构

```
Player/
├── PlayerController.cs         # 核心玩家控制器
├── Editor/
│   └── PlayerControllerEditor.cs  # 编辑器界面
└── Player使用说明.md           # 本文档
```

---

## 🚀 快速开始

### 1️⃣ 前置准备

**A. 配置 Layer**
- `Project Settings` → `Tags and Layers`
- 添加新 Layer：`HexNode`
- 将所有 HexNode 物体的 Layer 设为 `HexNode`

**B. 检查地图配置**
- 确保场景中有 HexNode 网格
- 确保有一个 HexNode 的 `isOrigin = true`
- 确保 HexNode 的 `autoSetupMeshCollider = true`（会自动为子物体添加 MeshCollider）

### 2️⃣ 创建 Player

**方法一：创建新物体**
```
1. 在场景中创建空物体，命名为 "Player"
2. 添加 PlayerController 组件
3. 创建子物体作为视觉表现（如 Cube、Sphere 或模型）
4. 配置参数（见下方）
```

**方法二：为现有物体添加**
```
1. 选择你的 Player 模型
2. 添加 PlayerController 组件
3. 配置参数
```

### 3️⃣ 配置参数

**必须配置：**
- `Hex Node Layer`：选择 `HexNode` Layer（LayerMask）

**可选配置：**
- `Player Transform`：不设置则使用当前物体
- `Position Offset`：默认 (0, 0.1, 0)，可根据模型调整
- `Jump Height`：默认 0.5
- `Jump Duration`：默认 0.3 秒

### 4️⃣ 运行测试

1. 点击 Unity 的 Play 按钮
2. Player 会自动出现在 Origin 位置
3. **左键点击** HexNode 寻路移动
4. **右键点击** 取消移动

---

## 🎮 操作说明

### 基本操作

| 操作 | 功能 |
|------|------|
| **左键点击 HexNode** | 寻路并移动到目标 |
| **右键点击** | 取消当前移动，停在当前格子 |
| **移动中左键点击新目标** | 完成当前跳跃后转向新目标 |

### 行为细节

**正常移动：**
```
左键点击目标 → 寻路 → 逐格跳跃 → 到达目标
```

**中途改变目标：**
```
移动中 → 左键点击新目标 → 完成当前这一跳 → 重新寻路 → 前往新目标
```

**取消移动：**
```
移动中 → 右键点击 → 完成当前这一跳 → 停止
```

**无法到达：**
```
点击无法到达的节点 → 提示"无法到达" → 保持原位
```

---

## 🔧 编辑器工具

PlayerController 提供了友好的编辑器界面：

### 当前状态显示（运行时）
- 当前位置节点
- 坐标 (q, r)
- 是否在移动

### 工具按钮
- **🔄 重新初始化**：重新将 Player 放回 Origin
- **⏹️ 强制停止**：立即停止移动
- **传送到 Origin**：瞬移回起点

### 场景检查（编辑器模式）
- **检查场景配置**：检查 HexNode 数量、Origin、MeshCollider 等

---

## 🔌 扩展接口

PlayerController 提供了多个虚方法，可以通过继承添加特效：

```csharp
public class MyPlayer : PlayerController
{
    protected override void OnMovementStart(List<HexNode> path)
    {
        // 移动开始时触发
        // 可以：显示路径高亮、播放音效等
    }

    protected override void OnStepStart(HexNode targetNode, int stepIndex)
    {
        // 每一跳开始时触发
        // 可以：播放跳跃音效、粒子特效等
    }

    protected override void OnStepFinished(HexNode arrivedNode)
    {
        // 每一跳完成时触发
        // 可以：播放落地音效、震动效果等
    }

    protected override void OnMovementEnd(HexNode finalNode)
    {
        // 移动完成时触发
        // 可以：播放完成音效、显示提示等
    }

    protected override void OnMovementCancelled(HexNode stoppedNode)
    {
        // 移动取消时触发
        // 可以：播放取消音效、特效等
    }

    protected override void OnMovementFailed(HexNode targetNode)
    {
        // 无法到达目标时触发
        // 可以：显示错误提示、播放错误音效等
    }
}
```

---

## 📡 公共 API

```csharp
// 获取当前所在节点
HexNode currentNode = playerController.GetCurrentNode();

// 检查是否在移动
bool isMoving = playerController.IsMoving();

// 强制停止移动
playerController.ForceStop();

// 传送到指定节点（不播放动画）
playerController.TeleportTo(targetNode);
```

---

## ⚙️ 参数说明

### Position Offset
- **作用**：Player 相对于 HexNode 中心的偏移
- **默认值**：(0, 0.1, 0)
- **建议**：根据 Player 模型高度调整 Y 值

### Jump Height
- **作用**：跳跃时的最高点高度
- **默认值**：0.5
- **建议**：0.3-1.0 之间较自然

### Jump Duration
- **作用**：每一跳的耗时（秒）
- **默认值**：0.3
- **建议**：0.2-0.5 之间较流畅

### Hex Node Layer
- **作用**：用于射线检测点击的 HexNode
- **必须配置**：选择 HexNode Layer

### Raycast Distance
- **作用**：射线检测的最大距离
- **默认值**：100
- **一般不需要修改**

---

## ❓ 常见问题

### Q: 点击 HexNode 没有反应？
**A: 检查以下几点：**
1. `Hex Node Layer` 是否正确设置
2. HexNode 是否有 MeshCollider（检查子物体）
3. Camera.main 是否存在
4. Console 是否有错误信息

### Q: Player 没有出现在 Origin？
**A: 检查：**
1. 场景中是否有 HexNode 的 `isOrigin = true`
2. Console 是否有 "未找到Origin节点" 的错误
3. 使用编辑器的 "检查场景配置" 按钮

### Q: Player 位置不对？
**A: 调整：**
1. `Position Offset` 参数
2. 确保 HexNode 的坐标正确

### Q: 移动速度太快/太慢？
**A: 调整：**
1. `Jump Duration` 参数（降低=加快，提高=减慢）

### Q: 如何添加移动特效？
**A: 两种方式：**
1. 继承 PlayerController，重写 OnXXX 方法
2. 直接在 PlayerController.cs 的 OnXXX 方法中添加代码

---

## 🎨 与其他系统配合

### 与 HexPathfinder 配合
```csharp
// PlayerController 内部使用 HexPathfinder.FindPath()
// 无需手动调用
```

### 与 HexGridBuilder 配合
```csharp
// 确保先构建网格，再初始化 Player
// PlayerController 会自动查找 Origin
```

### 添加移动范围限制
```csharp
// 可以在 TryStartNewPath 中添加范围检查
// 使用 HexPathfinder.GetReachableNodes() 判断是否在范围内
```

---

## 📝 开发建议

1. **调试模式**：保持 `Show Debug Log = true`，方便查看状态
2. **Layer 隔离**：将 HexNode 单独放在一个 Layer，避免误点击其他物体
3. **性能优化**：大地图时可以考虑使用 Physics Layers 限制射线检测范围
4. **扩展性**：通过继承而不是修改源码来添加功能

---

## 🎯 下一步

- [ ] 添加移动范围限制（如每回合移动步数）
- [ ] 添加路径高亮显示
- [ ] 添加移动音效和粒子特效
- [ ] 添加多个 Player 支持
- [ ] 添加障碍物动态更新支持

---

## 📞 技术支持

如有问题，请检查：
1. Console 错误信息
2. 使用编辑器的 "检查场景配置" 功能
3. 查看 HexNode 的 MeshCollider 是否正确配置

---

**版本：** 1.0  
**最后更新：** 2025-11-12



