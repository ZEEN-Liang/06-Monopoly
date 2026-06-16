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
            Start,
            Shop,
            Upgrade,
            Event
        }

        private enum DecorationType
        {
            Tree,
            Flower,
            Lamp
        }

        private struct DecorationPlacement
        {
            public Vector3 Position;
            public float Radius;
        }

        [Header("Map Layout")]
        [SerializeField] private int gridWidth = 5;
        [SerializeField] private int gridHeight = 7;
        [SerializeField] private float tileSpacing = 3f;

        [Header("Tile Prefabs")]
        [SerializeField] private GameObject startTilePrefab;
        [SerializeField] private GameObject shopTilePrefab;
        [SerializeField] private GameObject upgradeTilePrefab;
        [SerializeField] private GameObject eventTilePrefab;
        [SerializeField] private GameObject shopBuildingPrefab;
        [SerializeField] private bool verboseGenerationLog = true;

        [Header("Shop Building Placeholder")]
        [SerializeField] private float shopBuildingOffset = 2.1f;
        [SerializeField] private Vector3 shopBuildingScale = new Vector3(1.2f, 1.4f, 1.2f);
        [SerializeField] private Vector3 shopBuildingLocalLift = new Vector3(0f, 0.7f, 0f);

        [Header("Road Connector Placeholder")]
        [SerializeField] private Vector3 roadConnectorScale = new Vector3(0.9f, 0.12f, 1.3f);
        [SerializeField] private float roadConnectorLift = 0.06f;
        [SerializeField] private Color roadConnectorColor = new Color(0.75f, 0.72f, 0.65f, 1f);

        [Header("Decoration Placeholder")]
        [SerializeField] private int outerDecorationCount = 30;
        [SerializeField] private int innerDecorationCount = 12;
        [SerializeField] private Vector2 outerDecorationScaleRange = new Vector2(0.45f, 0.9f);
        [SerializeField] private Vector2 innerDecorationScaleRange = new Vector2(0.35f, 0.75f);
        [SerializeField] private float outerDecorationOffsetMin = 3.2f;
        [SerializeField] private float outerDecorationOffsetMax = 5.1f;
        [SerializeField] private float innerDecorationMargin = 1.6f;
        [SerializeField] private float diceArenaAvoidRadius = 4.1f;
        [SerializeField] private Color treeDecorationColor = new Color(0.34f, 0.68f, 0.36f, 1f);
        [SerializeField] private Color flowerDecorationColor = new Color(0.96f, 0.56f, 0.76f, 1f);
        [SerializeField] private Color lampDecorationColor = new Color(0.94f, 0.82f, 0.38f, 1f);

        [Header("Random Generation")]
        [SerializeField] private int startShopCount = 9;
        [SerializeField] private int startUpgradeCount = 2;
        [SerializeField] private int startEventCount = 3;
        [SerializeField] private bool keepFirstTileAsStart = true;

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

            decisionController.Configure(playerData, uiManager, shopSystem, shopUpgradeSystem, shopRebuildSystem, playerUpgradeSystem);

            List<ShopData> demoShops = CreateDemoShopData();
            List<EventData> demoEvents = CreateDemoEvents();
            List<CustomerData> demoCustomers = CreateDemoCustomers();

            eventManager.Configure(demoEvents, uiManager);

            Transform existingBoardRoot = GameObject.Find("BoardRoot")?.transform;
            Transform boardRoot = existingBoardRoot != null
                ? existingBoardRoot
                : new GameObject("BoardRoot").transform;
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
                    CreateShopBuildingVisual(shopTile, borderCells);
                }
                else if (tile is StartTile startTile)
                {
                    startTile.Configure(node);
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

            CreateRoadConnectors(nodes, boardRoot);
            CreateDecorations(nodes, borderCells, boardRoot);

            boardManager.Configure(nodes, new List<RouteBranch>(), tiles);

            PlayerPawn playerPawn = FindExistingComponent<PlayerPawn>("PlayerPawn");
            if (playerPawn == null)
            {
                GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObject.name = "PlayerPawn";
                playerObject.transform.position = nodes[0].StandPoint.position + Vector3.up * 0.8f;
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

                if (generatedTileType == GeneratedTileType.Start)
                {
                    StartTile startTile = nodeObject.AddComponent<StartTile>();
                    startTile.Configure();
                }
                else if (generatedTileType == GeneratedTileType.Event)
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
            if (generatedTileType == GeneratedTileType.Start)
            {
                return startTilePrefab;
            }

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
            int reservedStartCount = keepFirstTileAsStart ? 1 : 0;
            if (configuredTotal + reservedStartCount != totalTileCount)
            {
                shopCount += totalTileCount - configuredTotal - reservedStartCount;
                shopCount = Mathf.Max(1, shopCount);
            }

            List<GeneratedTileType> result = new List<GeneratedTileType>();
            if (keepFirstTileAsStart)
            {
                result.Add(GeneratedTileType.Start);
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

        private void CreateRoadConnectors(List<PathNode> nodes, Transform boardRoot)
        {
            if (nodes == null || nodes.Count <= 1 || boardRoot == null)
            {
                return;
            }

            Transform existingRoot = boardRoot.Find("RoadConnectors");
            if (existingRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(existingRoot.gameObject);
                }
            }

            GameObject connectorRootObject = new GameObject("RoadConnectors");
            connectorRootObject.transform.SetParent(boardRoot, false);
            Transform connectorRoot = connectorRootObject.transform;

            for (int i = 0; i < nodes.Count; i++)
            {
                PathNode currentNode = nodes[i];
                PathNode nextNode = nodes[(i + 1) % nodes.Count];
                if (currentNode == null || nextNode == null)
                {
                    continue;
                }

                CreateSingleRoadConnector(currentNode, nextNode, connectorRoot, i);
            }
        }

        private void CreateSingleRoadConnector(PathNode fromNode, PathNode toNode, Transform parent, int index)
        {
            Vector3 fromPosition = fromNode.StandPoint.position;
            Vector3 toPosition = toNode.StandPoint.position;
            Vector3 connectorDirection = toPosition - fromPosition;
            connectorDirection.y = 0f;

            float connectorLength = connectorDirection.magnitude;
            if (connectorLength <= 0.01f)
            {
                return;
            }

            GameObject connectorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            connectorObject.name = $"RoadConnector_{index:D2}";
            connectorObject.transform.SetParent(parent, false);
            connectorObject.transform.position = (fromPosition + toPosition) * 0.5f + Vector3.up * roadConnectorLift;
            connectorObject.transform.rotation = Quaternion.LookRotation(connectorDirection.normalized, Vector3.up);
            connectorObject.transform.localScale = new Vector3(
                roadConnectorScale.x,
                roadConnectorScale.y,
                Mathf.Max(0.2f, connectorLength - roadConnectorScale.z));

            Renderer connectorRenderer = connectorObject.GetComponent<Renderer>();
            if (connectorRenderer != null)
            {
                connectorRenderer.material.color = roadConnectorColor;
            }
        }

        private void CreateDecorations(List<PathNode> nodes, List<Vector2Int> borderCells, Transform boardRoot)
        {
            if (nodes == null || nodes.Count == 0 || borderCells == null || borderCells.Count == 0 || boardRoot == null)
            {
                return;
            }

            Transform existingRoot = boardRoot.Find("Decorations");
            if (existingRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(existingRoot.gameObject);
                }
            }

            GameObject decorationsRootObject = new GameObject("Decorations");
            decorationsRootObject.transform.SetParent(boardRoot, false);
            Transform decorationsRoot = decorationsRootObject.transform;
            List<DecorationPlacement> placements = new List<DecorationPlacement>();

            CreateOuterDecorations(nodes, borderCells, decorationsRoot, placements);
            CreateInnerDecorations(borderCells, decorationsRoot, placements);
        }

        private void CreateOuterDecorations(List<PathNode> nodes, List<Vector2Int> borderCells, Transform parent, List<DecorationPlacement> placements)
        {
            float minX;
            float maxX;
            float minZ;
            float maxZ;
            GetBorderWorldBounds(borderCells, out minX, out maxX, out minZ, out maxZ);

            int created = 0;
            int attempts = 0;
            while (created < outerDecorationCount && attempts < outerDecorationCount * 12)
            {
                attempts++;
                PathNode anchorNode = nodes[Random.Range(0, nodes.Count)];
                if (anchorNode == null)
                {
                    continue;
                }

                Vector3 anchorPosition = anchorNode.StandPoint.position;
                Vector3 outward = GetOutwardDirection(anchorPosition, minX, maxX, minZ, maxZ);
                float offset = Random.Range(outerDecorationOffsetMin, outerDecorationOffsetMax);
                Vector3 decorationPosition = anchorPosition + outward * offset;
                decorationPosition.y = 0f;

                if (!TryCreateDecorationCube(parent, $"OuterDecoration_{created:D2}", decorationPosition, true, placements))
                {
                    continue;
                }

                created++;
            }
        }

        private void CreateInnerDecorations(List<Vector2Int> borderCells, Transform parent, List<DecorationPlacement> placements)
        {
            float minX;
            float maxX;
            float minZ;
            float maxZ;
            GetBorderWorldBounds(borderCells, out minX, out maxX, out minZ, out maxZ);

            float innerMinX = minX + innerDecorationMargin;
            float innerMaxX = maxX - innerDecorationMargin;
            float innerMinZ = minZ + innerDecorationMargin;
            float innerMaxZ = maxZ - innerDecorationMargin;

            int created = 0;
            int attempts = 0;
            while (created < innerDecorationCount && attempts < innerDecorationCount * 12)
            {
                attempts++;
                Vector3 candidate = new Vector3(
                    Random.Range(innerMinX, innerMaxX),
                    0f,
                    Random.Range(innerMinZ, innerMaxZ));

                Vector2 planar = new Vector2(candidate.x, candidate.z);
                if (planar.magnitude < diceArenaAvoidRadius)
                {
                    continue;
                }

                if (!TryCreateDecorationCube(parent, $"InnerDecoration_{created:D2}", candidate, false, placements))
                {
                    continue;
                }

                created++;
            }
        }

        private bool TryCreateDecorationCube(
            Transform parent,
            string objectName,
            Vector3 worldPosition,
            bool outerArea,
            List<DecorationPlacement> placements)
        {
            float scale = outerArea
                ? Random.Range(outerDecorationScaleRange.x, outerDecorationScaleRange.y)
                : Random.Range(innerDecorationScaleRange.x, innerDecorationScaleRange.y);

            DecorationType decorationType = (DecorationType)Random.Range(0, 3);
            Vector3 decorationScale = GetDecorationScale(decorationType, scale);
            float placementRadius = Mathf.Max(decorationScale.x, decorationScale.z) * 0.7f;

            if (!CanPlaceDecoration(worldPosition, placementRadius, placements))
            {
                return false;
            }

            GameObject decorationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            decorationObject.name = objectName;
            decorationObject.transform.SetParent(parent, false);
            decorationObject.transform.position = worldPosition;
            decorationObject.transform.localScale = decorationScale;
            decorationObject.transform.position += Vector3.up * decorationScale.y * 0.5f;

            decorationObject.transform.rotation = Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                Random.Range(-4f, 4f));

            Renderer decorationRenderer = decorationObject.GetComponent<Renderer>();
            if (decorationRenderer != null)
            {
                decorationRenderer.material.color = GetDecorationColor(decorationType);
            }

            placements.Add(new DecorationPlacement
            {
                Position = worldPosition,
                Radius = placementRadius
            });

            return true;
        }

        private Vector3 GetDecorationScale(DecorationType decorationType, float scale)
        {
            switch (decorationType)
            {
                case DecorationType.Tree:
                    return new Vector3(scale, scale * 2.4f, scale);
                case DecorationType.Flower:
                    return new Vector3(scale * 0.9f, scale * 0.6f, scale * 0.9f);
                case DecorationType.Lamp:
                    return new Vector3(scale * 0.45f, scale * 3f, scale * 0.45f);
                default:
                    return Vector3.one * scale;
            }
        }

        private bool CanPlaceDecoration(Vector3 candidatePosition, float candidateRadius, List<DecorationPlacement> placements)
        {
            if (placements == null)
            {
                return true;
            }

            for (int i = 0; i < placements.Count; i++)
            {
                DecorationPlacement existing = placements[i];
                Vector2 a = new Vector2(candidatePosition.x, candidatePosition.z);
                Vector2 b = new Vector2(existing.Position.x, existing.Position.z);
                float minDistance = candidateRadius + existing.Radius;
                if ((a - b).sqrMagnitude < minDistance * minDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private void GetBorderWorldBounds(List<Vector2Int> borderCells, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            minZ = float.MaxValue;
            maxZ = float.MinValue;

            for (int i = 0; i < borderCells.Count; i++)
            {
                Vector3 world = GridToWorld(borderCells[i]);
                minX = Mathf.Min(minX, world.x);
                maxX = Mathf.Max(maxX, world.x);
                minZ = Mathf.Min(minZ, world.z);
                maxZ = Mathf.Max(maxZ, world.z);
            }
        }

        private Vector3 GetOutwardDirection(Vector3 position, float minX, float maxX, float minZ, float maxZ)
        {
            const float edgeTolerance = 0.2f;
            Vector3 outward = Vector3.zero;

            if (Mathf.Abs(position.x - minX) <= edgeTolerance)
            {
                outward += Vector3.left;
            }
            if (Mathf.Abs(position.x - maxX) <= edgeTolerance)
            {
                outward += Vector3.right;
            }
            if (Mathf.Abs(position.z - minZ) <= edgeTolerance)
            {
                outward += Vector3.back;
            }
            if (Mathf.Abs(position.z - maxZ) <= edgeTolerance)
            {
                outward += Vector3.forward;
            }

            if (outward.sqrMagnitude <= 0.0001f)
            {
                outward = (position - Vector3.zero).normalized;
            }

            return outward.normalized;
        }

        private Color GetDecorationColor(DecorationType decorationType)
        {
            switch (decorationType)
            {
                case DecorationType.Tree:
                    return treeDecorationColor;
                case DecorationType.Flower:
                    return flowerDecorationColor;
                case DecorationType.Lamp:
                    return lampDecorationColor;
                default:
                    return Color.white;
            }
        }

        private void CreateShopBuildingVisual(ShopTile shopTile, List<Vector2Int> borderCells)
        {
            if (shopTile == null || borderCells == null || borderCells.Count == 0)
            {
                return;
            }

            for (int i = shopTile.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = shopTile.transform.GetChild(i);
                if (child != null && child.name.StartsWith("ShopBuilding"))
                {
                    Destroy(child.gameObject);
                }
            }

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            for (int i = 0; i < borderCells.Count; i++)
            {
                Vector3 world = GridToWorld(borderCells[i]);
                minX = Mathf.Min(minX, world.x);
                maxX = Mathf.Max(maxX, world.x);
                minZ = Mathf.Min(minZ, world.z);
                maxZ = Mathf.Max(maxZ, world.z);
            }

            Vector3 position = shopTile.transform.position;
            const float edgeTolerance = 0.15f;

            bool onLeftEdge = Mathf.Abs(position.x - minX) <= edgeTolerance;
            bool onRightEdge = Mathf.Abs(position.x - maxX) <= edgeTolerance;
            bool onBottomEdge = Mathf.Abs(position.z - minZ) <= edgeTolerance;
            bool onTopEdge = Mathf.Abs(position.z - maxZ) <= edgeTolerance;

            bool isCornerShop = (onLeftEdge || onRightEdge) && (onTopEdge || onBottomEdge);

            List<Vector3> edgeOutwards = new List<Vector3>();
            if (onTopEdge)
            {
                edgeOutwards.Add(Vector3.forward);
            }
            if (onBottomEdge)
            {
                edgeOutwards.Add(Vector3.back);
            }
            if (onLeftEdge)
            {
                edgeOutwards.Add(Vector3.left);
            }
            if (onRightEdge)
            {
                edgeOutwards.Add(Vector3.right);
            }

            if (edgeOutwards.Count == 0)
            {
                edgeOutwards.Add(Vector3.forward);
            }

            if (isCornerShop)
            {
                Vector3 outwardA = edgeOutwards[0];
                Vector3 outwardB = edgeOutwards[1];
                Vector3 cornerOutward = (outwardA + outwardB).normalized;

                // Center shop opens toward the corner street junction.
                CreateSingleShopBuilding(
                    shopTile,
                    cornerOutward * shopBuildingOffset,
                    -cornerOutward,
                    "ShopBuilding_Center");

                // Side shops each sit on the same street-center axis as normal edge shops.
                CreateSingleShopBuilding(
                    shopTile,
                    outwardA * shopBuildingOffset,
                    -outwardA,
                    "ShopBuilding_SideA");

                CreateSingleShopBuilding(
                    shopTile,
                    outwardB * shopBuildingOffset,
                    -outwardB,
                    "ShopBuilding_SideB");
                return;
            }

            CreateSingleShopBuilding(
                shopTile,
                edgeOutwards[0] * shopBuildingOffset,
                -edgeOutwards[0],
                "ShopBuilding");
        }

        private void CreateSingleShopBuilding(
            ShopTile shopTile,
            Vector3 localOffset,
            Vector3 doorForward,
            string objectName)
        {
            Vector3 buildingPosition = shopTile.transform.position
                + localOffset
                + shopBuildingLocalLift;

            GameObject buildingObject;
            if (shopBuildingPrefab != null)
            {
                buildingObject = Instantiate(shopBuildingPrefab, buildingPosition, Quaternion.identity, shopTile.transform);
            }
            else
            {
                buildingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                buildingObject.transform.SetParent(shopTile.transform);
                buildingObject.transform.position = buildingPosition;
            }

            buildingObject.name = objectName;
            buildingObject.transform.localScale = shopBuildingScale;

            // Forward is the shop door direction, and doors should face the street.
            Vector3 forward = doorForward;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            buildingObject.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

            Renderer renderer = buildingObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetShopBuildingColor(shopTile.OriginalShopData);
            }
        }

        private Color GetShopBuildingColor(ShopData shopData)
        {
            if (shopData == null)
            {
                return new Color(0.9f, 0.6f, 0.4f);
            }

            switch (shopData.category)
            {
                case ShopCategory.Drink:
                    return new Color(0.45f, 0.8f, 1f);
                case ShopCategory.Snack:
                    return new Color(1f, 0.68f, 0.32f);
                case ShopCategory.Dessert:
                    return new Color(1f, 0.72f, 0.88f);
                case ShopCategory.Seafood:
                    return new Color(0.35f, 0.82f, 0.88f);
                case ShopCategory.MainDish:
                    return new Color(0.82f, 0.46f, 0.28f);
                case ShopCategory.Support:
                    return new Color(0.72f, 0.72f, 1f);
                default:
                    return new Color(0.9f, 0.6f, 0.4f);
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
