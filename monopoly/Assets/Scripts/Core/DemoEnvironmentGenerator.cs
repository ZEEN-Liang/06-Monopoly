using System.Collections.Generic;
using Monopoly.Board;
using Monopoly.Shop;
using Monopoly.Utils;
using UnityEngine;

namespace Monopoly.Core
{
    public class DemoEnvironmentGenerator : MonoBehaviour
    {
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

        [Header("Grid Reference")]
        [SerializeField] private int gridWidth = 5;
        [SerializeField] private int gridHeight = 7;
        [SerializeField] private float tileSpacing = 3f;

        [Header("Shop Building Placeholder")]
        [SerializeField] private GameObject shopBuildingPrefab;
        [SerializeField] private float shopBuildingOffset = 2.1f;
        [SerializeField] private Vector3 shopBuildingScale = new Vector3(1.2f, 1.4f, 1.2f);
        [SerializeField] private Vector3 shopBuildingLocalLift = new Vector3(0f, 0.7f, 0f);

        [Header("Road Connector Placeholder")]
        [SerializeField] private GameObject roadConnectorPrefab;
        [SerializeField] private Vector3 roadConnectorScale = new Vector3(0.58f, 0.08f, 1.3f);
        [SerializeField] private float roadConnectorLift = 0.015f;
        [SerializeField] private Color roadConnectorColor = new Color(0.75f, 0.72f, 0.65f, 1f);
        [SerializeField] private Vector3 roadConnectorLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 roadConnectorLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 roadConnectorLocalScale = new Vector3(0.8f, 0.8f, 1f);

        [Header("Decoration Placeholder")]
        [SerializeField] private int outerDecorationCount = 56;
        [SerializeField] private int innerDecorationCount = 32;
        [SerializeField] private GameObject[] outerDecorationPrefabs;
        [SerializeField] private GameObject[] innerDecorationPrefabs;
        [SerializeField] private Vector2 outerDecorationScaleRange = new Vector2(0.7f, 1f);
        [SerializeField] private Vector2 innerDecorationScaleRange = new Vector2(0.7f, 1f);
        [SerializeField] private Vector2 outerDecorationTargetSizeRange = new Vector2(1.2f, 1.8f);
        [SerializeField] private Vector2 innerDecorationTargetSizeRange = new Vector2(0.8f, 1.3f);
        [SerializeField] private float outerDecorationOffsetMin = 3.2f;
        [SerializeField] private float outerDecorationOffsetMax = 5.1f;
        [SerializeField] private float outerDecorationLateralJitter = 1.1f;
        [SerializeField] private float innerDecorationMargin = 1.6f;
        [SerializeField] private float diceArenaAvoidRadius = 4.1f;
        [SerializeField] private float outerDecorationRadiusMultiplier = 0.7f;
        [SerializeField] private float innerDecorationRadiusMultiplier = 0.3f;
        [SerializeField] private int innerDecorationCandidateChecks = 10;
        [SerializeField] private float decorationPositionJitter = 0.45f;
        [SerializeField] private Color treeDecorationColor = new Color(0.34f, 0.68f, 0.36f, 1f);
        [SerializeField] private Color flowerDecorationColor = new Color(0.96f, 0.56f, 0.76f, 1f);
        [SerializeField] private Color lampDecorationColor = new Color(0.94f, 0.82f, 0.38f, 1f);

        private GameObject[] outerDecorationPrefabPool;
        private GameObject[] innerDecorationPrefabPool;

        public void BuildEnvironment(Transform boardRoot, List<PathNode> nodes, List<Vector2Int> borderCells)
        {
            if (boardRoot == null || nodes == null || nodes.Count == 0 || borderCells == null || borderCells.Count == 0)
            {
                return;
            }

            CreateRoadConnectors(nodes, boardRoot);
            CreateDecorations(nodes, borderCells, boardRoot);

            for (int i = 0; i < nodes.Count; i++)
            {
                ShopTile shopTile = nodes[i] != null ? nodes[i].Tile as ShopTile : null;
                if (shopTile != null)
                {
                    CreateShopBuildingVisual(shopTile, borderCells);
                }
            }
        }

        private Vector3 GridToWorld(Vector2Int cell)
        {
            float offsetX = (gridWidth - 1) * tileSpacing * 0.5f;
            float offsetZ = (gridHeight - 1) * tileSpacing * 0.5f;
            return new Vector3(cell.x * tileSpacing - offsetX, 0f, -(cell.y * tileSpacing - offsetZ));
        }

        private void CreateRoadConnectors(List<PathNode> nodes, Transform boardRoot)
        {
            Transform existingRoot = boardRoot.Find("RoadConnectors");
            if (existingRoot != null)
            {
                DestroyObject(existingRoot.gameObject);
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

            Vector3 connectorPosition = (fromPosition + toPosition) * 0.5f + Vector3.up * roadConnectorLift;
            Quaternion connectorRotation = Quaternion.LookRotation(connectorDirection.normalized, Vector3.up);
            GameObject connectorObject;

            if (roadConnectorPrefab != null)
            {
                connectorObject = Instantiate(roadConnectorPrefab, parent);
                connectorObject.name = $"RoadConnector_{index:D2}";
                ApplyRoadConnectorPrefabTransform(connectorObject.transform, connectorPosition, connectorRotation, connectorLength);
            }
            else
            {
                connectorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                connectorObject.name = $"RoadConnector_{index:D2}";
                connectorObject.transform.SetParent(parent, false);
                connectorObject.transform.position = connectorPosition;
                connectorObject.transform.rotation = connectorRotation;
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
        }

        private void ApplyRoadConnectorPrefabTransform(Transform connectorTransform, Vector3 worldPosition, Quaternion worldRotation, float connectorLength)
        {
            if (connectorTransform == null)
            {
                return;
            }

            Transform prefabTransform = roadConnectorPrefab != null ? roadConnectorPrefab.transform : null;
            Vector3 baseLocalPosition = prefabTransform != null ? prefabTransform.localPosition : Vector3.zero;
            Quaternion baseLocalRotation = prefabTransform != null ? prefabTransform.localRotation : Quaternion.identity;
            Vector3 baseLocalScale = prefabTransform != null ? prefabTransform.localScale : Vector3.one;

            connectorTransform.position = worldPosition;
            connectorTransform.rotation = worldRotation;
            connectorTransform.localPosition += connectorTransform.rotation * (baseLocalPosition + roadConnectorLocalPosition);
            connectorTransform.localRotation = connectorTransform.localRotation * baseLocalRotation * Quaternion.Euler(roadConnectorLocalEulerAngles);

            float lengthScale = Mathf.Max(0.2f, connectorLength - roadConnectorScale.z);
            connectorTransform.localScale = new Vector3(
                baseLocalScale.x * roadConnectorLocalScale.x,
                baseLocalScale.y * roadConnectorLocalScale.y,
                baseLocalScale.z * roadConnectorLocalScale.z * lengthScale);
        }

        private void CreateDecorations(List<PathNode> nodes, List<Vector2Int> borderCells, Transform boardRoot)
        {
            EnsureDecorationPrefabPools();

            Transform existingRoot = boardRoot.Find("Decorations");
            if (existingRoot != null)
            {
                DestroyObject(existingRoot.gameObject);
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
            while (created < outerDecorationCount && attempts < outerDecorationCount * 18)
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
                Vector3 tangent = new Vector3(-outward.z, 0f, outward.x);
                float lateralOffset = Random.Range(-outerDecorationLateralJitter, outerDecorationLateralJitter);
                Vector3 jitter = new Vector3(
                    Random.Range(-decorationPositionJitter, decorationPositionJitter),
                    0f,
                    Random.Range(-decorationPositionJitter, decorationPositionJitter));
                Vector3 decorationPosition = anchorPosition + outward * offset + tangent * lateralOffset + jitter;
                decorationPosition.y = 0f;

                if (!TryCreateDecoration(parent, $"OuterDecoration_{created:D2}", decorationPosition, true, placements))
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
            while (created < innerDecorationCount && attempts < innerDecorationCount * 24)
            {
                attempts++;
                Vector3 candidate;
                if (!TryFindSparseInnerCandidate(innerMinX, innerMaxX, innerMinZ, innerMaxZ, placements, out candidate))
                {
                    continue;
                }

                if (!TryCreateDecoration(parent, $"InnerDecoration_{created:D2}", candidate, false, placements))
                {
                    continue;
                }

                created++;
            }
        }

        private bool TryCreateDecoration(Transform parent, string objectName, Vector3 worldPosition, bool outerArea, List<DecorationPlacement> placements)
        {
            float scale = outerArea
                ? Random.Range(outerDecorationScaleRange.x, outerDecorationScaleRange.y)
                : Random.Range(innerDecorationScaleRange.x, innerDecorationScaleRange.y);

            DecorationType decorationType = (DecorationType)Random.Range(0, 3);
            Vector3 decorationScale = GetDecorationScale(decorationType, scale);
            float radiusMultiplier = outerArea ? outerDecorationRadiusMultiplier : innerDecorationRadiusMultiplier;
            float placementRadius = Mathf.Max(decorationScale.x, decorationScale.z) * radiusMultiplier;

            if (!CanPlaceDecoration(worldPosition, placementRadius, placements))
            {
                return false;
            }

            bool usedPrefab;
            GameObject decorationObject = CreateDecorationObject(objectName, worldPosition, outerArea, parent, out usedPrefab);
            if (usedPrefab)
            {
                Transform visualRoot = decorationObject.transform.childCount > 0
                    ? decorationObject.transform.GetChild(0)
                    : decorationObject.transform;

                visualRoot.localScale = Vector3.one;
                Bounds prefabBounds = CalculateRenderableBounds(decorationObject);
                float targetSize = outerArea
                    ? Random.Range(outerDecorationTargetSizeRange.x, outerDecorationTargetSizeRange.y)
                    : Random.Range(innerDecorationTargetSizeRange.x, innerDecorationTargetSizeRange.y);
                float currentSize = Mathf.Max(prefabBounds.size.x, prefabBounds.size.z, 0.01f);
                float normalizedScale = Mathf.Clamp(targetSize / currentSize, 0.7f, 1f);
                float uniformScale = normalizedScale * Mathf.Max(0.85f, scale);

                visualRoot.localScale = Vector3.one * uniformScale;
                prefabBounds = CalculateRenderableBounds(decorationObject);
                float groundOffset = worldPosition.y - prefabBounds.min.y;
                decorationObject.transform.position += Vector3.up * groundOffset;
            }
            else
            {
                decorationObject.transform.localScale = decorationScale;
                decorationObject.transform.position += Vector3.up * decorationScale.y * 0.5f;
            }

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

        private bool TryFindSparseInnerCandidate(float minX, float maxX, float minZ, float maxZ, List<DecorationPlacement> placements, out Vector3 candidate)
        {
            candidate = Vector3.zero;
            float bestScore = float.MinValue;
            bool found = false;

            for (int i = 0; i < innerDecorationCandidateChecks; i++)
            {
                Vector3 probe = new Vector3(Random.Range(minX, maxX), 0f, Random.Range(minZ, maxZ));
                probe += new Vector3(
                    Random.Range(-decorationPositionJitter, decorationPositionJitter),
                    0f,
                    Random.Range(-decorationPositionJitter, decorationPositionJitter));

                Vector2 planar = new Vector2(probe.x, probe.z);
                if (planar.magnitude < diceArenaAvoidRadius)
                {
                    continue;
                }

                float nearestDistance = GetNearestDecorationDistance(probe, placements);
                if (nearestDistance > bestScore)
                {
                    bestScore = nearestDistance;
                    candidate = probe;
                    found = true;
                }
            }

            return found;
        }

        private GameObject CreateDecorationObject(string objectName, Vector3 worldPosition, bool outerArea, Transform parent, out bool usedPrefab)
        {
            GameObject[] prefabPool = outerArea ? outerDecorationPrefabPool : innerDecorationPrefabPool;
            GameObject selectedPrefab = GetRandomDecorationPrefab(prefabPool);

            GameObject decorationObject;
            if (selectedPrefab != null)
            {
                decorationObject = new GameObject(objectName);
                decorationObject.transform.SetParent(parent, false);
                decorationObject.transform.position = worldPosition;

                GameObject visualInstance = Instantiate(selectedPrefab, worldPosition, Quaternion.identity, decorationObject.transform);
                visualInstance.name = "Visual";
                usedPrefab = true;
            }
            else
            {
                decorationObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                decorationObject.transform.SetParent(parent, false);
                decorationObject.transform.position = worldPosition;
                usedPrefab = false;
            }
            decorationObject.name = objectName;
            return decorationObject;
        }

        private Bounds CalculateRenderableBounds(GameObject rootObject)
        {
            Renderer[] renderers = rootObject != null ? rootObject.GetComponentsInChildren<Renderer>(true) : null;
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(rootObject != null ? rootObject.transform.position : Vector3.zero, Vector3.one);
            }

            Bounds mergedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                mergedBounds.Encapsulate(renderers[i].bounds);
            }

            return mergedBounds;
        }

        private void EnsureDecorationPrefabPools()
        {
            if (outerDecorationPrefabs != null && outerDecorationPrefabs.Length > 0)
            {
                outerDecorationPrefabPool = FilterDecorationPrefabs(outerDecorationPrefabs);
            }
            else if (outerDecorationPrefabPool == null || outerDecorationPrefabPool.Length == 0)
            {
                outerDecorationPrefabPool = LoadDecorationPrefabs(
                    "Map/nature/Prefabs/Tree_01",
                    "Map/nature/Prefabs/Tree_02",
                    "Map/nature/Prefabs/Tree_03",
                    "Map/nature/Prefabs/Tree_04",
                    "Map/nature/Prefabs/Tree_05",
                    "Map/nature/Prefabs/Bush_01",
                    "Map/nature/Prefabs/Bush_02",
                    "Map/nature/Prefabs/Bush_03",
                    "Map/nature/Prefabs/Rock_01",
                    "Map/nature/Prefabs/Rock_02",
                    "Map/nature/Prefabs/Rock_03");
            }

            if (innerDecorationPrefabs != null && innerDecorationPrefabs.Length > 0)
            {
                innerDecorationPrefabPool = FilterDecorationPrefabs(innerDecorationPrefabs);
            }
            else if (innerDecorationPrefabPool == null || innerDecorationPrefabPool.Length == 0)
            {
                innerDecorationPrefabPool = LoadDecorationPrefabs(
                    "Map/nature/Prefabs/Flowers_01",
                    "Map/nature/Prefabs/Flowers_02",
                    "Map/nature/Prefabs/Grass_01",
                    "Map/nature/Prefabs/Grass_02",
                    "Map/nature/Prefabs/Bush_01",
                    "Map/nature/Prefabs/Bush_02",
                    "Map/nature/Prefabs/Bush_03",
                    "Map/nature/Prefabs/Mushroom_01",
                    "Map/nature/Prefabs/Mushroom_02",
                    "Map/nature/Prefabs/Rock_01",
                    "Map/nature/Prefabs/Rock_04",
                    "Map/nature/Prefabs/Rock_05");
            }
        }

        private GameObject[] LoadDecorationPrefabs(params string[] resourcePaths)
        {
            List<GameObject> loadedPrefabs = new List<GameObject>();
            for (int i = 0; i < resourcePaths.Length; i++)
            {
                GameObject prefab = Resources.Load<GameObject>(resourcePaths[i]);
                if (prefab != null)
                {
                    loadedPrefabs.Add(prefab);
                }
            }

            return loadedPrefabs.ToArray();
        }

        private GameObject[] FilterDecorationPrefabs(GameObject[] sourcePrefabs)
        {
            List<GameObject> filteredPrefabs = new List<GameObject>();
            for (int i = 0; i < sourcePrefabs.Length; i++)
            {
                if (sourcePrefabs[i] != null)
                {
                    filteredPrefabs.Add(sourcePrefabs[i]);
                }
            }

            return filteredPrefabs.ToArray();
        }

        private GameObject GetRandomDecorationPrefab(GameObject[] prefabPool)
        {
            if (prefabPool == null || prefabPool.Length == 0)
            {
                return null;
            }

            return prefabPool[Random.Range(0, prefabPool.Length)];
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

        private float GetNearestDecorationDistance(Vector3 candidatePosition, List<DecorationPlacement> placements)
        {
            if (placements == null || placements.Count == 0)
            {
                return float.MaxValue;
            }

            float nearest = float.MaxValue;
            Vector2 candidatePlanar = new Vector2(candidatePosition.x, candidatePosition.z);
            for (int i = 0; i < placements.Count; i++)
            {
                Vector2 existingPlanar = new Vector2(placements[i].Position.x, placements[i].Position.z);
                float distance = Vector2.Distance(candidatePlanar, existingPlanar);
                nearest = Mathf.Min(nearest, distance);
            }

            return nearest;
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
            for (int i = shopTile.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = shopTile.transform.GetChild(i);
                if (child != null && child.name.StartsWith("ShopBuilding"))
                {
                    DestroyObject(child.gameObject);
                }
            }

            float minX;
            float maxX;
            float minZ;
            float maxZ;
            GetBorderWorldBounds(borderCells, out minX, out maxX, out minZ, out maxZ);

            Vector3 position = shopTile.transform.position;
            const float edgeTolerance = 0.15f;
            bool onLeftEdge = Mathf.Abs(position.x - minX) <= edgeTolerance;
            bool onRightEdge = Mathf.Abs(position.x - maxX) <= edgeTolerance;
            bool onBottomEdge = Mathf.Abs(position.z - minZ) <= edgeTolerance;
            bool onTopEdge = Mathf.Abs(position.z - maxZ) <= edgeTolerance;
            bool isCornerShop = (onLeftEdge || onRightEdge) && (onTopEdge || onBottomEdge);

            List<Vector3> edgeOutwards = new List<Vector3>();
            if (onTopEdge) edgeOutwards.Add(Vector3.forward);
            if (onBottomEdge) edgeOutwards.Add(Vector3.back);
            if (onLeftEdge) edgeOutwards.Add(Vector3.left);
            if (onRightEdge) edgeOutwards.Add(Vector3.right);
            if (edgeOutwards.Count == 0) edgeOutwards.Add(Vector3.forward);

            if (isCornerShop)
            {
                Vector3 outwardA = edgeOutwards[0];
                Vector3 outwardB = edgeOutwards[1];
                Vector3 cornerOutward = (outwardA + outwardB).normalized;

                CreateSingleShopBuilding(shopTile, cornerOutward * shopBuildingOffset, -cornerOutward, "ShopBuilding_Center");
                CreateSingleShopBuilding(shopTile, outwardA * shopBuildingOffset, -outwardA, "ShopBuilding_SideA");
                CreateSingleShopBuilding(shopTile, outwardB * shopBuildingOffset, -outwardB, "ShopBuilding_SideB");
                return;
            }

            CreateSingleShopBuilding(shopTile, edgeOutwards[0] * shopBuildingOffset, -edgeOutwards[0], "ShopBuilding");
        }

        private void CreateSingleShopBuilding(ShopTile shopTile, Vector3 localOffset, Vector3 doorForward, string objectName)
        {
            ShopData shopData = shopTile != null && shopTile.CurrentShop != null
                ? shopTile.CurrentShop.Data
                : shopTile != null ? shopTile.OriginalShopData : null;
            GameObject targetPrefab = shopData != null && shopData.shopModelPrefab != null
                ? shopData.shopModelPrefab
                : shopBuildingPrefab;
            Vector3 forward = doorForward.sqrMagnitude <= 0.0001f ? Vector3.forward : doorForward;
            Quaternion facingRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            Vector3 targetScale = shopData != null ? shopData.shopModelScale : shopBuildingScale;
            Vector3 targetLift = shopData != null ? shopData.shopModelLocalOffset : Vector3.zero;

            GameObject buildingObject;
            bool usesPrefab = targetPrefab != null;
            Vector3 worldHorizontalOffset = facingRotation * new Vector3(targetLift.x, 0f, targetLift.z);
            Vector3 baseLift = usesPrefab ? Vector3.zero : shopBuildingLocalLift;
            Vector3 verticalOffset = Vector3.up * targetLift.y;
            Vector3 buildingPosition = shopTile.transform.position + localOffset + worldHorizontalOffset + baseLift + verticalOffset;

            if (targetPrefab != null)
            {
                Quaternion prefabRotation = targetPrefab.transform.rotation;
                buildingObject = Instantiate(targetPrefab, buildingPosition, facingRotation * prefabRotation, shopTile.transform);
                Debug.Log($"Spawned shop model for {shopTile.name}: data={(shopData != null ? shopData.shopName : "null")}, prefab={targetPrefab.name}");
            }
            else
            {
                buildingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                buildingObject.transform.SetParent(shopTile.transform);
                buildingObject.transform.position = buildingPosition;
                Debug.LogWarning($"No shop model prefab found for {shopTile.name}. Falling back to cube.");
            }

            buildingObject.name = objectName;

            ApplyShopBuildingScale(buildingObject, targetScale, usesPrefab);
            if (!usesPrefab)
            {
                buildingObject.transform.rotation = facingRotation;
            }

            if (usesPrefab)
            {
                Bounds bounds = CalculateRenderableBounds(buildingObject);
                float groundOffset = buildingPosition.y - bounds.min.y;
                buildingObject.transform.position += Vector3.up * groundOffset;
            }

            EnsureClickableBuildingCollider(buildingObject);

            Renderer renderer = buildingObject.GetComponentInChildren<Renderer>();
            if (!usesPrefab && renderer != null)
            {
                renderer.material.color = GetShopBuildingColor(shopTile.OriginalShopData);
            }
        }

        private void ApplyShopBuildingScale(GameObject buildingObject, Vector3 scaleMultiplier, bool usesPrefab)
        {
            if (buildingObject == null)
            {
                return;
            }

            Vector3 sanitizedScale = new Vector3(
                Mathf.Max(0.01f, scaleMultiplier.x),
                Mathf.Max(0.01f, scaleMultiplier.y),
                Mathf.Max(0.01f, scaleMultiplier.z));

            if (usesPrefab)
            {
                Vector3 prefabScale = buildingObject.transform.localScale;
                buildingObject.transform.localScale = new Vector3(
                    prefabScale.x * sanitizedScale.x,
                    prefabScale.y * sanitizedScale.y,
                    prefabScale.z * sanitizedScale.z);
                return;
            }

            buildingObject.transform.localScale = sanitizedScale;
        }

        private void EnsureClickableBuildingCollider(GameObject buildingObject)
        {
            if (buildingObject == null)
            {
                return;
            }

            if (buildingObject.GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            Bounds bounds = CalculateRenderableBounds(buildingObject);
            BoxCollider collider = buildingObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = buildingObject.AddComponent<BoxCollider>();
            }

            collider.center = buildingObject.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = buildingObject.transform.InverseTransformVector(bounds.size);
            collider.size = new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z));
        }

        private Color GetShopBuildingColor(ShopData shopData)
        {
            if (shopData == null)
            {
                return new Color(0.9f, 0.6f, 0.4f);
            }

            switch (shopData.category)
            {
                case ShopCategory.Exotic:
                    return new Color(0.5f, 0.82f, 1f);
                case ShopCategory.Snack:
                    return new Color(1f, 0.68f, 0.32f);
                case ShopCategory.Chinese:
                    return new Color(0.9f, 0.34f, 0.28f);
                case ShopCategory.FastFood:
                    return new Color(1f, 0.9f, 0.35f);
                default:
                    return new Color(0.9f, 0.6f, 0.4f);
            }
        }

        private void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
