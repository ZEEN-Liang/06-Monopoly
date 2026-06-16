# Scene Setup Guide

如果你想最快跑通当前项目，建议先用一个空场景配合 `DemoSceneBootstrap`。

## 方案 A：最快验证玩法
1. 新建一个空场景，例如 `DemoScene`
2. 在层级里创建一个空物体，命名为 `DemoBootstrap`
3. 给它挂上 [DemoSceneBootstrap.cs](</d:/09-Coding/06-Monopoly/monopoly/Assets/Scripts/Core/DemoSceneBootstrap.cs:1>)
4. 直接点击 Play

这个脚本会自动创建：
- 管理器对象
- 一圈 12 个棋盘节点
- 店铺格 / 升级格 / 机会格
- 玩家棋子
- 顾客刷新系统
- 基础相机

没有美术素材时，它会自动使用：
- `Cube` 作为格子
- `Capsule` 作为玩家
- `Sphere` 作为顾客

## 方案 B：手动接线，适合你边学边做
如果你想更熟悉 Unity，建议后面按这个顺序手动替换：

### 1. 场景中的核心对象
- `GameManager`
- `TurnManager`
- `BoardManager`
- `MapStateManager`
- `EventManager`
- `CustomerFlowManager`
- `UIManager`
- `PlayerData`
- `PlayerDecisionController`

### 2. 路径节点
- 每个路径点建一个空物体
- 挂 `PathNode`
- 把它们串成一个环

### 3. 棋盘格子
- 店铺格挂 `ShopTile`
- 升级格挂 `UpgradeTile`
- 机会格挂 `EventTile`

### 4. 玩家
- 创建一个 `Capsule`
- 挂 `PlayerPawn`

### 5. 顾客
- 创建一个 `Sphere` 作为顾客 prefab
- 挂 `CustomerAgent`

## 无美术素材时的替代方案

### 棋盘
- `Cube`：普通格子
- 改颜色区分功能：
  - 红色：店铺格
  - 绿色：升级格
  - 黄色：机会格

### 玩家和顾客
- `Capsule`：玩家
- `Sphere`：顾客

### 店铺状态
- 先不用复杂模型
- 用名字和颜色表达即可
- 例如：
  - 粉色：甜品
  - 蓝色：饮品
  - 橙色：小吃
  - 青色：海鲜

### UI
- 先用 `Debug.Log`
- 再逐步替换成 `Canvas + Text + Button`

## 推荐你接下来的开发顺序
1. 先让 `DemoSceneBootstrap` 跑起来
2. 确认玩家能沿棋盘移动
3. 确认顾客会持续刷新并沿路径移动
4. 确认停到店铺格能触发收购或消费
5. 再开始补真正的 UI 和更精细的规则
