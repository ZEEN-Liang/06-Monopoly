using System.Collections.Generic;
using Monopoly.Board;
using Monopoly.Events;
using Monopoly.Shop;
using UnityEngine;

namespace Monopoly.Core
{
    public class DemoMapGenerator : MonoBehaviour
    {
        private enum GeneratedTileType
        {
            Start,
            Shop,
            Upgrade,
            Event
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
        [SerializeField] private bool verboseGenerationLog = true;

        [Header("Random Generation")]
        [SerializeField] private int startShopCount = 9;
        [SerializeField] private int startUpgradeCount = 2;
        [SerializeField] private int startEventCount = 3;
        [SerializeField] private bool keepFirstTileAsStart = true;
        [SerializeField] private ShopPoolData shopPoolData;
        [SerializeField] private ShopCategoryPoolData shopCategoryPoolData;

        public bool HasAssignedShopPoolData => shopPoolData != null || shopCategoryPoolData != null;

        public void GenerateBoard(
            EventManager eventManager,
            List<ShopData> demoShops,
            out Transform boardRoot,
            out List<PathNode> nodes,
            out List<BoardTile> tiles,
            out List<Vector2Int> borderCells)
        {
            boardRoot = GetOrCreateBoardRoot();
            ClearBoardRoot(boardRoot);

            nodes = new List<PathNode>();
            tiles = new List<BoardTile>();
            borderCells = GetBorderCells();
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
                    if (shopCategoryPoolData != null)
                    {
                        shopTile.ConfigureRandom(shopCategoryPoolData, node);
                    }
                    else if (shopPoolData != null)
                    {
                        shopTile.ConfigureRandom(shopPoolData, node);
                    }
                    else
                    {
                        shopTile.ConfigureRandom(demoShops, node);
                    }
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
        }

        public void SetShopPoolData(ShopPoolData poolData)
        {
            if (shopPoolData == null)
            {
                shopPoolData = poolData;
            }
        }

        public void SetShopCategoryPoolData(ShopCategoryPoolData poolData)
        {
            if (shopCategoryPoolData == null)
            {
                shopCategoryPoolData = poolData;
            }
        }

        public Vector3 GridToWorld(Vector2Int cell)
        {
            float offsetX = (gridWidth - 1) * tileSpacing * 0.5f;
            float offsetZ = (gridHeight - 1) * tileSpacing * 0.5f;
            return new Vector3(cell.x * tileSpacing - offsetX, 0f, -(cell.y * tileSpacing - offsetZ));
        }

        private Transform GetOrCreateBoardRoot()
        {
            Transform existingBoardRoot = GameObject.Find("BoardRoot")?.transform;
            return existingBoardRoot != null
                ? existingBoardRoot
                : new GameObject("BoardRoot").transform;
        }

        private void ClearBoardRoot(Transform boardRoot)
        {
            for (int i = boardRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = boardRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
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
    }
}
