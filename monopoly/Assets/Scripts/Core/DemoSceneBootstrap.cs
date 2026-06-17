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
        [Header("Customer")]
        [SerializeField] private float customerSpawnInterval = 4f;
        [SerializeField] private int maxCustomers = 6;

        private void Start()
        {
            BuildDemoScene();
        }

        private void BuildDemoScene()
        {
            CleanupDuplicateComponents<DiceController>("PlayerDice");

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

            DemoMapGenerator mapGenerator = CreateManager<DemoMapGenerator>("DemoMapGenerator");
            DemoEnvironmentGenerator environmentGenerator = CreateManager<DemoEnvironmentGenerator>("DemoEnvironmentGenerator");

            decisionController.Configure(playerData, uiManager, shopSystem, shopUpgradeSystem, shopRebuildSystem, playerUpgradeSystem);

            List<ShopData> demoShops = CreateDemoShopData();
            List<EventData> demoEvents = CreateDemoEvents();
            List<CustomerData> demoCustomers = CreateDemoCustomers();

            eventManager.Configure(demoEvents, uiManager);
            if (!mapGenerator.HasAssignedShopPoolData)
            {
                ShopPoolData demoShopPool = CreateDemoShopPool(demoShops);
                mapGenerator.SetShopPoolData(demoShopPool);
            }

            Transform boardRoot;
            List<PathNode> nodes;
            List<BoardTile> tiles;
            List<Vector2Int> borderCells;
            mapGenerator.GenerateBoard(eventManager, demoShops, out boardRoot, out nodes, out tiles, out borderCells);
            environmentGenerator.BuildEnvironment(boardRoot, nodes, borderCells);

            boardManager.Configure(nodes, new List<RouteBranch>(), tiles);

            PlayerPawn playerPawn = FindExistingComponent<PlayerPawn>("PlayerPawn");
            if (playerPawn == null)
            {
                GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObject.name = "PlayerPawn";
                Renderer playerRenderer = playerObject.GetComponent<Renderer>();
                if (playerRenderer != null)
                {
                    playerRenderer.material.color = new Color(0.2f, 0.7f, 1f);
                }

                playerPawn = playerObject.AddComponent<PlayerPawn>();
            }

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
            T existing = FindExistingComponent<T>(objectName);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject(objectName);
            return root.AddComponent<T>();
        }

        private T FindExistingComponent<T>(string preferredObjectName) where T : Component
        {
            T[] existingObjects = FindObjectsOfType<T>();
            if (existingObjects == null || existingObjects.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < existingObjects.Length; i++)
            {
                T component = existingObjects[i];
                if (component != null && component.gameObject.name == preferredObjectName)
                {
                    return component;
                }
            }

            return existingObjects[0];
        }

        private void CleanupDuplicateComponents<T>(string preferredObjectName) where T : Component
        {
            T[] existingObjects = FindObjectsOfType<T>();
            if (existingObjects == null || existingObjects.Length <= 1)
            {
                return;
            }

            T keeper = FindExistingComponent<T>(preferredObjectName);
            for (int i = 0; i < existingObjects.Length; i++)
            {
                T component = existingObjects[i];
                if (component == null || component == keeper)
                {
                    continue;
                }

                Destroy(component.gameObject);
            }
        }

        private void SetupCamera()
        {
            if (Camera.main != null)
            {
                if (Camera.main.GetComponent<OrbitCameraController>() != null)
                {
                    return;
                }

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
                CreateShopData("exotic", "Exotic", ShopCategory.Exotic, ShopRole.Income, 110, 160, 34, 55),
                CreateShopData("snack", "Snack", ShopCategory.Snack, ShopRole.Income, 90, 140, 25, 45),
                CreateShopData("chinese", "Chinese", ShopCategory.Chinese, ShopRole.Core, 120, 170, 40, 60),
                CreateShopData("fast_food", "Fast Food", ShopCategory.FastFood, ShopRole.Support, 100, 150, 30, 50)
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
            data.baseAcquireCost = acquireCost;
            data.rebuildCost = rebuildCost;
            data.baseCustomerProfit = baseIncome;
            data.baseUpgradeCost = upgradeCost;
            data.growthProfile = CreateGrowthProfileForCategory(category);
            data.baseRent = Mathf.Max(8, acquireCost / 8);
            data.baseAttractionRate = GetBaseAttractionForCategory(category);
            data.baseCustomerStayDuration = 0.8f;
            data.customerStayDurationMultiplier = 1f;
            return data;
        }

        private ShopPoolData CreateDemoShopPool(List<ShopData> shopDataList)
        {
            ShopPoolData poolData = ScriptableObject.CreateInstance<ShopPoolData>();
            for (int i = 0; i < shopDataList.Count; i++)
            {
                ShopData shopData = shopDataList[i];
                if (shopData == null)
                {
                    continue;
                }

                ShopPoolEntry entry = new ShopPoolEntry
                {
                    shopData = shopData,
                    weight = 1,
                    enabled = true
                };
                poolData.entries.Add(entry);
            }

            return poolData;
        }

        private ShopGrowthProfile CreateGrowthProfileForCategory(ShopCategory category)
        {
            ShopGrowthProfile profile = ScriptableObject.CreateInstance<ShopGrowthProfile>();
            profile.ownedStartingLevel = 1;
            profile.ownedRentGrowthPerLevel = 10;
            profile.ownedCustomerProfitGrowthPerLevel = 10;
            profile.ownedAttractionGrowthPerLevel = 12f;
            profile.ownedStayDurationReductionPerLevel = 0.08f;
            profile.ownedUpgradeCostStepPerLevel = 25;
            profile.wildStartingLevel = 1;
            profile.wildMaxLevel = 5;
            profile.baseCustomersNeededForAutoUpgrade = 3;
            profile.extraCustomersNeededPerLevel = 1;
            profile.wildIncomeBonusPerLevel = 8;
            profile.wildAcquireCostBonusPerLevel = 40;
            profile.wildAttractionBonusPerLevel = 2.5f;
            profile.wildStopDurationBonusPerLevel = 0.15f;

            switch (category)
            {
                case ShopCategory.Exotic:
                    profile.ownedCustomerProfitGrowthPerLevel = 8;
                    profile.ownedAttractionGrowthPerLevel = 18f;
                    profile.baseCustomersNeededForAutoUpgrade = 2;
                    profile.wildAttractionBonusPerLevel = 3.5f;
                    break;
                case ShopCategory.Snack:
                    profile.ownedRentGrowthPerLevel = 8;
                    profile.ownedCustomerProfitGrowthPerLevel = 9;
                    profile.wildIncomeBonusPerLevel = 7;
                    break;
                case ShopCategory.Chinese:
                    profile.ownedRentGrowthPerLevel = 14;
                    profile.ownedCustomerProfitGrowthPerLevel = 12;
                    profile.ownedUpgradeCostStepPerLevel = 30;
                    profile.baseCustomersNeededForAutoUpgrade = 4;
                    profile.wildIncomeBonusPerLevel = 10;
                    profile.wildAcquireCostBonusPerLevel = 55;
                    break;
                case ShopCategory.FastFood:
                    profile.ownedRentGrowthPerLevel = 9;
                    profile.ownedCustomerProfitGrowthPerLevel = 10;
                    profile.ownedAttractionGrowthPerLevel = 16f;
                    profile.ownedStayDurationReductionPerLevel = 0.12f;
                    profile.wildAttractionBonusPerLevel = 2.8f;
                    break;
            }

            return profile;
        }

        private float GetBaseAttractionForCategory(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Exotic:
                    return 6f;
                case ShopCategory.Snack:
                    return 4f;
                case ShopCategory.Chinese:
                    return 4.5f;
                case ShopCategory.FastFood:
                    return 5.5f;
                default:
                    return 0f;
            }
        }

        private List<CustomerData> CreateDemoCustomers()
        {
            return new List<CustomerData>
            {
                CreateCustomerData(
                    "Student",
                    CustomerType.Student,
                    1,
                    2,
                    16,
                    12f,
                    5.5f,
                    2.0f,
                    0.35f,
                    0.8f,
                    ShopCategory.FastFood,
                    ShopCategory.Exotic,
                    ShopCategory.Snack),
                CreateCustomerData(
                    "Merchant",
                    CustomerType.Merchant,
                    1,
                    3,
                    28,
                    10.5f,
                    4.5f,
                    2.4f,
                    0.3f,
                    1f,
                    ShopCategory.Chinese,
                    ShopCategory.Exotic,
                    ShopCategory.FastFood),
                CreateCustomerData(
                    "Worker",
                    CustomerType.Worker,
                    2,
                    4,
                    24,
                    9.5f,
                    4f,
                    2.6f,
                    0.25f,
                    1.1f,
                    ShopCategory.Chinese,
                    ShopCategory.Snack,
                    ShopCategory.FastFood)
            };
        }

        private CustomerData CreateCustomerData(
            string nameText,
            CustomerType customerType,
            int minStep,
            int maxStep,
            int baseSpend,
            float attractionSensitivity,
            float preferredAttractionBonus,
            float baseStopDuration,
            float stopDurationVariance,
            float minStopDuration,
            params ShopCategory[] preferredCategories)
        {
            CustomerData data = ScriptableObject.CreateInstance<CustomerData>();
            data.customerName = nameText;
            data.customerType = customerType;
            data.minMoveStep = minStep;
            data.maxMoveStep = maxStep;
            data.baseSpend = baseSpend;
            data.attractionSensitivity = attractionSensitivity;
            data.preferredAttractionBonus = preferredAttractionBonus;
            data.baseStopDuration = baseStopDuration;
            data.stopDurationVariance = stopDurationVariance;
            data.minStopDuration = minStopDuration;
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
            foodFestival.choices.Add(new EventChoiceData { choiceText = "Rent a premium booth", moneyDelta = 90, satisfactionDelta = -12 });
            foodFestival.choices.Add(new EventChoiceData { choiceText = "Run a tasting promo", moneyDelta = -30, satisfactionDelta = 16 });
            foodFestival.choices.Add(new EventChoiceData { choiceText = "Stay conservative", moneyDelta = 10, satisfactionDelta = -3 });
            events.Add(foodFestival);

            EventData rushHour = ScriptableObject.CreateInstance<EventData>();
            rushHour.eventId = "rush_hour";
            rushHour.title = "Rush Hour";
            rushHour.description = "An unexpected rush hits the street. You can chase revenue, but service quality may suffer.";
            rushHour.choices.Add(new EventChoiceData { choiceText = "Push for maximum turnover", moneyDelta = 100, satisfactionDelta = -15 });
            rushHour.choices.Add(new EventChoiceData { choiceText = "Add temp staff", moneyDelta = -25, satisfactionDelta = 12 });
            rushHour.choices.Add(new EventChoiceData { choiceText = "Control the queue", moneyDelta = 35, satisfactionDelta = 4 });
            events.Add(rushHour);

            EventData influencerVisit = ScriptableObject.CreateInstance<EventData>();
            influencerVisit.eventId = "influencer_visit";
            influencerVisit.title = "Influencer Visit";
            influencerVisit.description = "A local food influencer arrives. One good move can explode your popularity, but a bad one becomes public fast.";
            influencerVisit.choices.Add(new EventChoiceData { choiceText = "Sponsor a full tasting set", moneyDelta = -40, satisfactionDelta = 18 });
            influencerVisit.choices.Add(new EventChoiceData { choiceText = "Charge for the special menu", moneyDelta = 70, satisfactionDelta = -8 });
            influencerVisit.choices.Add(new EventChoiceData { choiceText = "Offer a balanced collaboration", moneyDelta = 20, satisfactionDelta = 8 });
            events.Add(influencerVisit);

            return events;
        }
    }
}
