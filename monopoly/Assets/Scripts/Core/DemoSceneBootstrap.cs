using System.Collections.Generic;
using Monopoly.Board;
using Monopoly.Customer;
using Monopoly.Events;
using Monopoly.Player;
using Monopoly.Shop;
using Monopoly.UI;
using Monopoly.Utils;
using UnityEngine;

namespace Monopoly.Core
{
    public class DemoSceneBootstrap : MonoBehaviour
    {
        private enum GeneratedTileType
        {
            Shop,
            Upgrade,
            Event
        }

        [Header("Map Layout")]
        [SerializeField] private int gridWidth = 5;
        [SerializeField] private int gridHeight = 4;
        [SerializeField] private float tileSpacing = 3f;

        [Header("Tile Prefabs")]
        [SerializeField] private GameObject shopTilePrefab;
        [SerializeField] private GameObject upgradeTilePrefab;
        [SerializeField] private GameObject eventTilePrefab;
        [SerializeField] private bool verboseGenerationLog = true;

        [Header("Random Generation")]
        [SerializeField] private int startShopCount = 9;
        [SerializeField] private int startUpgradeCount = 2;
        [SerializeField] private int startEventCount = 3;
        [SerializeField] private bool keepFirstTileAsShop = true;

        [Header("Customer")]
        [SerializeField] private float customerSpawnInterval = 4f;
        [SerializeField] private int maxCustomers = 6;

        private void Start()
        {
            BuildDemoScene();
        }

        private void BuildDemoScene()
        {
            UIManager uiManager = CreateManager<UIManager>("UIManager");
            BoardManager boardManager = CreateManager<BoardManager>("BoardManager");
            CreateManager<MapStateManager>("MapStateManager");
            EventManager eventManager = CreateManager<EventManager>("EventManager");
            ShopSystem shopSystem = CreateManager<ShopSystem>("ShopSystem");
            ShopUpgradeSystem shopUpgradeSystem = CreateManager<ShopUpgradeSystem>("ShopUpgradeSystem");
            ShopRebuildSystem shopRebuildSystem = CreateManager<ShopRebuildSystem>("ShopRebuildSystem");
            PlayerUpgradeSystem playerUpgradeSystem = CreateManager<PlayerUpgradeSystem>("PlayerUpgradeSystem");
            CustomerDecisionHelper customerDecisionHelper = CreateManager<CustomerDecisionHelper>("CustomerDecisionHelper");
            CustomerFlowManager customerFlowManager = CreateManager<CustomerFlowManager>("CustomerFlowManager");
            TurnManager turnManager = CreateManager<TurnManager>("TurnManager");
            GameManager gameManager = CreateManager<GameManager>("GameManager");

            PlayerData playerData = CreateManager<PlayerData>("PlayerData");
            PlayerDecisionController decisionController = CreateManager<PlayerDecisionController>("PlayerDecisionController");
            DiceController diceController = CreateManager<DiceController>("PlayerDice");

            decisionController.Configure(playerData, uiManager, shopSystem, shopUpgradeSystem, shopRebuildSystem, playerUpgradeSystem);

            List<ShopData> demoShops = CreateDemoShopData();
            List<EventData> demoEvents = CreateDemoEvents();
            List<CustomerData> demoCustomers = CreateDemoCustomers();

            eventManager.Configure(demoEvents, uiManager);

            Transform boardRoot = new GameObject("BoardRoot").transform;
            List<PathNode> nodes = new List<PathNode>();
            List<BoardTile> tiles = new List<BoardTile>();

            List<Vector2Int> borderCells = GetBorderCells();
            List<GeneratedTileType> generatedTileTypes = CreateRandomTilePlan(borderCells.Count);
            for (int i = 0; i < borderCells.Count; i++)
            {
                Vector3 position = GridToWorld(borderCells[i]);
                GameObject nodeObject = CreateTileObject(i, position, boardRoot, eventManager, demoShops, generatedTileTypes[i]);

                PathNode node = nodeObject.GetComponent<PathNode>();
                BoardTile tile = nodeObject.GetComponent<BoardTile>();

                if (node == null || tile == null)
                {
                    Debug.LogError($"Tile prefab on {nodeObject.name} must contain both PathNode and a BoardTile-derived component.");
                    continue;
                }

                node.Configure($"node_{i:D2}", nodeObject.transform, tile);
                tile.Configure($"tile_{i:D2}", tile.TileType, node);

                if (tile is ShopTile shopTile)
                {
                    shopTile.ConfigureRandom(demoShops, node);
                }
                else if (tile is UpgradeTile upgradeTile)
                {
                    upgradeTile.Configure(node);
                }
                else if (tile is EventTile mapEventTile)
                {
                    mapEventTile.Configure(eventManager, node);
                }

                nodes.Add(node);
                tiles.Add(tile);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                PathNode nextNode = nodes[(i + 1) % nodes.Count];
                nodes[i].SetNextNodes(new List<PathNode> { nextNode });
            }

            boardManager.Configure(nodes, new List<RouteBranch>(), tiles);

            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "PlayerPawn";
            playerObject.transform.position = nodes[0].StandPoint.position + Vector3.up * 0.8f;
            Renderer playerRenderer = playerObject.GetComponent<Renderer>();
            if (playerRenderer != null)
            {
                playerRenderer.material.color = new Color(0.2f, 0.7f, 1f);
            }

            PlayerPawn playerPawn = playerObject.AddComponent<PlayerPawn>();
            playerPawn.Initialize(boardManager, decisionController, playerData, nodes[0]);

            turnManager.Configure(playerPawn, diceController, uiManager, playerData);
            customerFlowManager.Configure(customerDecisionHelper, boardManager, nodes[0], demoCustomers, customerSpawnInterval, maxCustomers);
            gameManager.Configure(turnManager, customerFlowManager, uiManager, playerData);
            uiManager.Bind(playerData, turnManager);

            SetupCamera();
            gameManager.StartGame();
        }

        private T CreateManager<T>(string objectName) where T : Component
        {
            GameObject root = new GameObject(objectName);
            return root.AddComponent<T>();
        }

        private GameObject CreateTileObject(
            int index,
            Vector3 position,
            Transform boardRoot,
            EventManager eventManager,
            List<ShopData> shopDataList,
            GeneratedTileType generatedTileType)
        {
            GameObject prefab = SelectPrefab(generatedTileType);
            GameObject nodeObject;

            if (prefab != null)
            {
                nodeObject = Instantiate(prefab, position, Quaternion.identity, boardRoot);
                if (verboseGenerationLog)
                {
                    Debug.Log($"Generated node {index:D2} with prefab: {prefab.name} ({generatedTileType})");
                }
            }
            else
            {
                nodeObject = new GameObject($"Node_{index:D2}");
                nodeObject.transform.SetParent(boardRoot);
                nodeObject.transform.position = position;

                if (verboseGenerationLog)
                {
                    Debug.LogWarning($"Generated node {index:D2} without prefab, using fallback placeholder ({generatedTileType})");
                }

                CreatePlaceholderTileVisual(nodeObject.transform, index);
                nodeObject.AddComponent<PathNode>();

                if (generatedTileType == GeneratedTileType.Event)
                {
                    EventTile eventTile = nodeObject.AddComponent<EventTile>();
                    eventTile.Configure(eventManager);
                }
                else if (generatedTileType == GeneratedTileType.Upgrade)
                {
                    UpgradeTile upgradeTile = nodeObject.AddComponent<UpgradeTile>();
                    upgradeTile.Configure();
                }
                else
                {
                    ShopTile shopTile = nodeObject.AddComponent<ShopTile>();
                    shopTile.Configure(shopDataList[index % shopDataList.Count]);
                }
            }

            nodeObject.name = $"Node_{index:D2}";
            nodeObject.transform.position = position;
            return nodeObject;
        }

        private GameObject SelectPrefab(GeneratedTileType generatedTileType)
        {
            if (generatedTileType == GeneratedTileType.Event)
            {
                return eventTilePrefab;
            }

            if (generatedTileType == GeneratedTileType.Upgrade)
            {
                return upgradeTilePrefab;
            }

            return shopTilePrefab;
        }

        private List<GeneratedTileType> CreateRandomTilePlan(int totalTileCount)
        {
            int shopCount = Mathf.Max(1, startShopCount);
            int upgradeCount = Mathf.Max(0, startUpgradeCount);
            int eventCount = Mathf.Max(0, startEventCount);

            int configuredTotal = shopCount + upgradeCount + eventCount;
            if (configuredTotal != totalTileCount)
            {
                shopCount += totalTileCount - configuredTotal;
                shopCount = Mathf.Max(1, shopCount);
            }

            List<GeneratedTileType> result = new List<GeneratedTileType>();
            if (keepFirstTileAsShop)
            {
                result.Add(GeneratedTileType.Shop);
                shopCount--;
            }

            List<GeneratedTileType> randomPool = new List<GeneratedTileType>();

            AddTilesToPool(randomPool, GeneratedTileType.Shop, shopCount);
            AddTilesToPool(randomPool, GeneratedTileType.Upgrade, upgradeCount);
            AddTilesToPool(randomPool, GeneratedTileType.Event, eventCount);

            Shuffle(randomPool);
            result.AddRange(randomPool);

            while (result.Count < totalTileCount)
            {
                result.Add(GeneratedTileType.Shop);
            }

            if (result.Count > totalTileCount)
            {
                result.RemoveRange(totalTileCount, result.Count - totalTileCount);
            }

            return result;
        }

        private void AddTilesToPool(List<GeneratedTileType> pool, GeneratedTileType tileType, int count)
        {
            for (int i = 0; i < count; i++)
            {
                pool.Add(tileType);
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }

        private List<Vector2Int> GetBorderCells()
        {
            List<Vector2Int> borderCells = new List<Vector2Int>();

            for (int x = 0; x < gridWidth; x++)
            {
                borderCells.Add(new Vector2Int(x, 0));
            }

            for (int y = 1; y < gridHeight; y++)
            {
                borderCells.Add(new Vector2Int(gridWidth - 1, y));
            }

            for (int x = gridWidth - 2; x >= 0; x--)
            {
                borderCells.Add(new Vector2Int(x, gridHeight - 1));
            }

            for (int y = gridHeight - 2; y >= 1; y--)
            {
                borderCells.Add(new Vector2Int(0, y));
            }

            return borderCells;
        }

        private Vector3 GridToWorld(Vector2Int cell)
        {
            float offsetX = (gridWidth - 1) * tileSpacing * 0.5f;
            float offsetZ = (gridHeight - 1) * tileSpacing * 0.5f;
            return new Vector3(cell.x * tileSpacing - offsetX, 0f, -(cell.y * tileSpacing - offsetZ));
        }

        private void CreatePlaceholderTileVisual(Transform parent, int index)
        {
            GameObject tileVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tileVisual.name = $"TileVisual_{index:D2}";
            tileVisual.transform.SetParent(parent);
            tileVisual.transform.localPosition = Vector3.zero;
            tileVisual.transform.localScale = new Vector3(1.8f, 0.3f, 1.8f);

            Renderer renderer = tileVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (index % 4 == 0)
                {
                    renderer.material.color = new Color(1f, 0.8f, 0.3f);
                }
                else if (index % 3 == 0)
                {
                    renderer.material.color = new Color(0.4f, 1f, 0.6f);
                }
                else
                {
                    renderer.material.color = new Color(1f, 0.5f, 0.5f);
                }
            }
        }

        private void SetupCamera()
        {
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(0f, 18f, -6f);
                Camera.main.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
                return;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.transform.position = new Vector3(0f, 18f, -6f);
            cameraComponent.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
        }

        private List<ShopData> CreateDemoShopData()
        {
            return new List<ShopData>
            {
                CreateShopData("milk_tea", "Milk Tea", ShopCategory.Drink, ShopRole.Income, 100, 150, 30, 50),
                CreateShopData("snack", "Snack", ShopCategory.Snack, ShopRole.Income, 90, 140, 25, 45),
                CreateShopData("dessert", "Dessert", ShopCategory.Dessert, ShopRole.Support, 110, 160, 35, 55),
                CreateShopData("seafood", "Seafood", ShopCategory.Seafood, ShopRole.Core, 130, 180, 45, 65)
            };
        }

        private ShopData CreateShopData(
            string id,
            string nameText,
            ShopCategory category,
            ShopRole role,
            int acquireCost,
            int rebuildCost,
            int baseIncome,
            int upgradeCost)
        {
            ShopData data = ScriptableObject.CreateInstance<ShopData>();
            data.shopId = id;
            data.shopName = nameText;
            data.category = category;
            data.role = role;
            data.acquireCost = acquireCost;
            data.rebuildCost = rebuildCost;
            data.baseIncome = baseIncome;
            data.baseUpgradeCost = upgradeCost;
            data.customerStopFlatModifier = 0.8f;
            data.customerStopMultiplier = 1f;
            return data;
        }

        private List<CustomerData> CreateDemoCustomers()
        {
            return new List<CustomerData>
            {
                CreateCustomerData("Normal", CustomerType.Normal, 1, 3, 20, ShopCategory.Snack, ShopCategory.Drink),
                CreateCustomerData("Student", CustomerType.Student, 1, 2, 15, ShopCategory.Drink, ShopCategory.Dessert),
                CreateCustomerData("Tourist", CustomerType.Tourist, 1, 4, 30, ShopCategory.Seafood, ShopCategory.MainDish)
            };
        }

        private CustomerData CreateCustomerData(
            string nameText,
            CustomerType customerType,
            int minStep,
            int maxStep,
            int baseSpend,
            params ShopCategory[] preferredCategories)
        {
            CustomerData data = ScriptableObject.CreateInstance<CustomerData>();
            data.customerName = nameText;
            data.customerType = customerType;
            data.minMoveStep = minStep;
            data.maxMoveStep = maxStep;
            data.baseSpend = baseSpend;
            data.baseStopDuration = 2.2f;
            data.stopDurationVariance = 0.35f;
            data.minStopDuration = 0.8f;
            data.preferredCategories = new List<ShopCategory>(preferredCategories);
            return data;
        }

        private List<EventData> CreateDemoEvents()
        {
            List<EventData> events = new List<EventData>();

            EventData foodFestival = ScriptableObject.CreateInstance<EventData>();
            foodFestival.eventId = "festival";
            foodFestival.title = "Food Festival";
            foodFestival.description = "A weekend food festival breaks out nearby. Big traffic is possible, but the street may get chaotic.";
            foodFestival.choices.Add(new EventChoiceData
            {
                choiceText = "Rent a premium booth",
                moneyDelta = 90,
                satisfactionDelta = -12
            });
            foodFestival.choices.Add(new EventChoiceData
            {
                choiceText = "Run a tasting promo",
                moneyDelta = -30,
                satisfactionDelta = 16
            });
            foodFestival.choices.Add(new EventChoiceData
            {
                choiceText = "Stay conservative",
                moneyDelta = 10,
                satisfactionDelta = -3
            });
            events.Add(foodFestival);

            EventData rushHour = ScriptableObject.CreateInstance<EventData>();
            rushHour.eventId = "rush_hour";
            rushHour.title = "Rush Hour";
            rushHour.description = "An unexpected rush hits the street. You can chase revenue, but service quality may suffer.";
            rushHour.choices.Add(new EventChoiceData
            {
                choiceText = "Push for maximum turnover",
                moneyDelta = 100,
                satisfactionDelta = -15
            });
            rushHour.choices.Add(new EventChoiceData
            {
                choiceText = "Add temp staff",
                moneyDelta = -25,
                satisfactionDelta = 12
            });
            rushHour.choices.Add(new EventChoiceData
            {
                choiceText = "Control the queue",
                moneyDelta = 35,
                satisfactionDelta = 4
            });
            events.Add(rushHour);

            EventData influencerVisit = ScriptableObject.CreateInstance<EventData>();
            influencerVisit.eventId = "influencer_visit";
            influencerVisit.title = "Influencer Visit";
            influencerVisit.description = "A local food influencer arrives. One good move can explode your popularity, but a bad one becomes public fast.";
            influencerVisit.choices.Add(new EventChoiceData
            {
                choiceText = "Sponsor a full tasting set",
                moneyDelta = -40,
                satisfactionDelta = 18
            });
            influencerVisit.choices.Add(new EventChoiceData
            {
                choiceText = "Charge for the special menu",
                moneyDelta = 70,
                satisfactionDelta = -8
            });
            influencerVisit.choices.Add(new EventChoiceData
            {
                choiceText = "Offer a balanced collaboration",
                moneyDelta = 20,
                satisfactionDelta = 8
            });
            events.Add(influencerVisit);

            return events;
        }
    }
}
