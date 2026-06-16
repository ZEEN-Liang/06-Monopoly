# Scripts Index

本目录按“谁负责什么”组织，先服务课程项目落地，再考虑后续精细化拆分。

## Core
- `GameManager.cs`: 整局开始、结束、暂停、胜负判定
- `TurnManager.cs`: 玩家回合流程
- `BoardManager.cs`: 棋盘节点、格子与路径查询
- `MapStateManager.cs`: 分支路线、地图状态与局内地图改造

## Board
- `BoardTile.cs`: 所有棋盘格子的基类
- `ShopTile.cs`: 原生店铺格，处理收购、升级、拆改、顾客消费
- `UpgradeTile.cs`: 升级格
- `EventTile.cs`: 机会格
- `PathNode.cs`: 路径点
- `RouteBranch.cs`: 可解锁/可切换的路线分支

## Player
- `PlayerPawn.cs`: 玩家棋子移动表现
- `PlayerData.cs`: 玩家经营数据
- `PlayerDecisionController.cs`: 玩家经营决策入口
- `DiceController.cs`: 通用骰子

## Customer
- `CustomerFlowManager.cs`: 顾客刷新与离场
- `CustomerAgent.cs`: 单个顾客的移动与消费
- `CustomerData.cs`: 顾客静态配置
- `CustomerDecisionHelper.cs`: 顾客步数、偏好、消费计算

## Shop
- `ShopData.cs`: 店铺静态配置
- `ShopInstance.cs`: 店铺运行时状态
- `ShopSystem.cs`: 收购、收益、消费等基础规则
- `ShopUpgradeSystem.cs`: 升级、分支、联动刷新
- `ShopRebuildSystem.cs`: 拆除重建

## Event
- `EventManager.cs`: 事件池与事件应用
- `EventData.cs`: 单个事件数据
- `EventChoiceData.cs`: 单个事件选项数据

## UI
- `UIManager.cs`: 统一 UI 入口

## Utils
- `GameEnums.cs`: 全局枚举

建议的第一轮开发顺序：
1. `GameEnums`
2. `PathNode` / `BoardTile` / `BoardManager`
3. `DiceController` / `PlayerPawn` / `TurnManager`
4. `ShopData` / `ShopInstance` / `ShopTile`
5. `CustomerData` / `CustomerAgent` / `CustomerFlowManager`
6. `EventTile` / `EventManager`
7. `UIManager`
