using System.Collections;
using Monopoly.Board;
using Monopoly.Core;
using UnityEngine;

namespace Monopoly.Customer
{
    public class CustomerAgent : MonoBehaviour
    {
        [System.Serializable]
        private class CustomerModelEntry
        {
            public Monopoly.Utils.CustomerType customerType;
            public GameObject modelPrefab;
        }

        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private CustomerDecisionHelper decisionHelper;
        [SerializeField] private CustomerData data;
        [SerializeField] private PathNode currentNode;
        [SerializeField] private PathNode startNode;
        [SerializeField] private float moveInterval = 1.5f;
        [SerializeField] private float minimumIdleInterval = 0.2f;
        [Header("Visual Override")]
        [SerializeField] private Renderer placeholderRenderer;
        [SerializeField] private Transform modelAnchor;
        [SerializeField] private GameObject customerModelPrefab;
        [SerializeField] private CustomerModelEntry[] customerModelEntries;
        [SerializeField] private bool hidePlaceholderWhenModelLoaded = true;
        [SerializeField] private Vector3 customerModelLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 customerModelLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 customerModelLocalScale = Vector3.one;
        [Header("Floating Income Text")]
        [SerializeField] private Vector3 incomeTextOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private Color incomeTextColor = new Color(0.15f, 0.9f, 0.35f);
        [SerializeField] private float incomeFloatDuration = 0.9f;
        [SerializeField] private float incomeFloatDistance = 0.8f;
        [Header("Floating Status Text")]
        [SerializeField] private Vector3 statusTextOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private Color attractedTextColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private float statusFloatDuration = 0.9f;
        [SerializeField] private float statusFloatDistance = 0.6f;
        [Header("Stay Presentation")]
        [SerializeField] private float shopEnterVisibleDuration = 0.2f;
        [SerializeField] private float shopExitVisibleDuration = 0.2f;

        private int traversedNodeCount;
        private int requiredLoopNodeCount;
        private bool isRunning;
        private bool useSequentialMovement;
        private bool disableRandomAttractionChecks;
        private int fixedMoveSteps = 1;
        private int playerShopSpendTotal;
        private System.Action<CustomerAgent> completedCallback;
        private Renderer[] cachedRenderers;
        private GameObject spawnedCustomerModelInstance;

        public CustomerData Data => data;
        public int PlayerShopSpendTotal => playerShopSpendTotal;

        private void Awake()
        {
            RefreshVisualOverride();
        }

        public void Initialize(BoardManager manager, CustomerDecisionHelper helper, CustomerData customerData, PathNode startNode)
        {
            boardManager = manager;
            decisionHelper = helper;
            data = customerData;
            currentNode = startNode;
            this.startNode = startNode;
            traversedNodeCount = 0;
            requiredLoopNodeCount = boardManager != null && boardManager.MainPathCount > 0 ? boardManager.MainPathCount : 1;
            useSequentialMovement = false;
            disableRandomAttractionChecks = false;
            fixedMoveSteps = 1;
            playerShopSpendTotal = 0;
            completedCallback = null;
            RefreshVisualOverride();
            CacheRenderers();
            SetCustomerVisible(true);

            if (currentNode != null)
            {
                transform.position = currentNode.StandPoint.position;
                TryFaceDirection(Vector3.forward, true);
            }
        }

        public void ConfigureEvaluationRun(System.Action<CustomerAgent> onCompleted)
        {
            useSequentialMovement = true;
            disableRandomAttractionChecks = true;
            fixedMoveSteps = 1;
            playerShopSpendTotal = 0;
            completedCallback = onCompleted;
        }

        public void StartCustomerLife()
        {
            if (!isRunning)
            {
                StartCoroutine(CustomerLoopRoutine());
            }
        }

        private IEnumerator CustomerLoopRoutine()
        {
            isRunning = true;

            while (!HasCompletedLoop())
            {
                int steps = useSequentialMovement
                    ? fixedMoveSteps
                    : (decisionHelper != null ? decisionHelper.RollMoveStep(data) : 1);
                yield return MoveStepsRoutine(steps);
                float stopDuration = ResolveStopDurationOnCurrentTile();
                yield return HandleStopPresentation(stopDuration);

                if (HasCompletedLoop())
                {
                    break;
                }

                yield return new WaitForSeconds(Mathf.Max(minimumIdleInterval, moveInterval));
            }

            completedCallback?.Invoke(this);
            LeaveMap();
        }

        private IEnumerator MoveStepsRoutine(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                PathNode nextNode = boardManager != null ? boardManager.GetNextNode(currentNode) : null;
                if (nextNode == null)
                {
                    break;
                }

                currentNode = nextNode;
                yield return MoveToPosition(currentNode.StandPoint.position);
                traversedNodeCount++;

                if (!disableRandomAttractionChecks && TryInterruptMoveByAttraction())
                {
                    break;
                }

                if (HasCompletedLoop())
                {
                    break;
                }
            }

            currentNode.Tile?.OnCustomerLanded(this);
        }

        private bool TryInterruptMoveByAttraction()
        {
            if (decisionHelper == null || currentNode == null)
            {
                return false;
            }

            ShopTile shopTile = currentNode.Tile as ShopTile;
            if (shopTile == null)
            {
                return false;
            }

            if (!decisionHelper.TryAttractToShop(data, shopTile))
            {
                return false;
            }

            ShowStatusPopup("被吸引", attractedTextColor);
            return true;
        }

        private float ResolveStopDurationOnCurrentTile()
        {
            if (currentNode == null || currentNode.Tile == null)
            {
                return 0f;
            }

            if (!(currentNode.Tile is ShopTile))
            {
                return 0f;
            }

            return Mathf.Max(0f, currentNode.Tile.GetCustomerStopDuration(this));
        }

        private IEnumerator HandleStopPresentation(float stopDuration)
        {
            if (stopDuration <= 0f)
            {
                yield break;
            }

            bool shouldHideDuringStay = currentNode != null && currentNode.Tile is ShopTile;
            if (!shouldHideDuringStay)
            {
                yield return new WaitForSeconds(stopDuration);
                yield break;
            }

            float enterDuration = Mathf.Min(shopEnterVisibleDuration, stopDuration);
            if (enterDuration > 0f)
            {
                SetCustomerVisible(true);
                yield return new WaitForSeconds(enterDuration);
            }

            float remaining = Mathf.Max(0f, stopDuration - enterDuration);
            float exitDuration = Mathf.Min(shopExitVisibleDuration, remaining);
            float hiddenDuration = Mathf.Max(0f, remaining - exitDuration);

            if (hiddenDuration > 0f)
            {
                SetCustomerVisible(false);
                yield return new WaitForSeconds(hiddenDuration);
            }

            SetCustomerVisible(true);
            if (exitDuration > 0f)
            {
                yield return new WaitForSeconds(exitDuration);
            }
        }

        private IEnumerator MoveToPosition(Vector3 targetPosition)
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
            {
                Vector3 moveDirection = targetPosition - transform.position;
                moveDirection.y = 0f;
                TryFaceDirection(moveDirection, false);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPosition;
        }

        private bool HasCompletedLoop()
        {
            return traversedNodeCount >= Mathf.Max(1, requiredLoopNodeCount);
        }

        private void TryFaceDirection(Vector3 direction, bool instant)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = instant
                ? targetRotation
                : Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        public void LeaveMap()
        {
            Destroy(gameObject);
        }

        public void ShowIncomePopup(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            StartCoroutine(ShowIncomePopupRoutine(amount));
        }

        public void RegisterShopSpend(int amount, bool isPlayerOwnedShop)
        {
            if (amount <= 0 || !isPlayerOwnedShop)
            {
                return;
            }

            playerShopSpendTotal += amount;
        }

        public bool ShouldConsumeAtShop(ShopTile shopTile)
        {
            if (shopTile == null)
            {
                return false;
            }

            if (!useSequentialMovement)
            {
                return true;
            }

            if (decisionHelper == null || data == null)
            {
                return false;
            }

            bool attracted = decisionHelper.TryAttractToShop(data, shopTile);
            if (attracted)
            {
                ShowStatusPopup("Attracted", attractedTextColor);
            }

            return attracted;
        }

        public void ShowStatusPopup(string text, Color color)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            StartCoroutine(ShowFloatingTextRoutine(text, color, statusTextOffset, statusFloatDuration, statusFloatDistance, 34, 0.13f));
        }

        private IEnumerator ShowIncomePopupRoutine(int amount)
        {
            yield return ShowFloatingTextRoutine($"+{amount}", incomeTextColor, incomeTextOffset, incomeFloatDuration, incomeFloatDistance, 40, 0.15f);
        }

        private IEnumerator ShowFloatingTextRoutine(string text, Color color, Vector3 localOffset, float duration, float distance, int fontSize, float characterSize)
        {
            GameObject popupObject = new GameObject("FloatingText");
            popupObject.transform.SetParent(transform);
            popupObject.transform.localPosition = localOffset;
            popupObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            TextMesh popupText = popupObject.AddComponent<TextMesh>();
            popupText.text = text;
            popupText.anchor = TextAnchor.MiddleCenter;
            popupText.alignment = TextAlignment.Center;
            popupText.characterSize = characterSize;
            popupText.fontSize = fontSize;
            popupText.color = color;

            Vector3 startPosition = popupObject.transform.localPosition;
            Vector3 endPosition = startPosition + Vector3.up * distance;
            Color startColor = color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                popupObject.transform.localPosition = Vector3.Lerp(startPosition, endPosition, progress);

                Color fadedColor = startColor;
                fadedColor.a = Mathf.Lerp(1f, 0f, progress);
                popupText.color = fadedColor;
                yield return null;
            }

            Destroy(popupObject);
        }

        private void CacheRenderers()
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                cachedRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void RefreshVisualOverride()
        {
            EnsureVisualReferences();

            if (spawnedCustomerModelInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(spawnedCustomerModelInstance);
                }
                else
                {
                    DestroyImmediate(spawnedCustomerModelInstance);
                }
            }

            spawnedCustomerModelInstance = null;
            GameObject selectedModelPrefab = ResolveCustomerModelPrefab();
            bool hasModelPrefab = selectedModelPrefab != null;
            if (hasModelPrefab)
            {
                Transform anchor = modelAnchor != null ? modelAnchor : transform;
                spawnedCustomerModelInstance = Instantiate(selectedModelPrefab, anchor);
                spawnedCustomerModelInstance.name = "CustomerModel";

                Transform modelTransform = spawnedCustomerModelInstance.transform;
                Transform prefabTransform = selectedModelPrefab.transform;
                modelTransform.localPosition = prefabTransform.localPosition + customerModelLocalPosition;
                modelTransform.localRotation = prefabTransform.localRotation * Quaternion.Euler(customerModelLocalEulerAngles);
                modelTransform.localScale = new Vector3(
                    prefabTransform.localScale.x * customerModelLocalScale.x,
                    prefabTransform.localScale.y * customerModelLocalScale.y,
                    prefabTransform.localScale.z * customerModelLocalScale.z);
            }

            if (placeholderRenderer != null)
            {
                placeholderRenderer.enabled = !hasModelPrefab || !hidePlaceholderWhenModelLoaded;
            }

            cachedRenderers = null;
        }

        private void EnsureVisualReferences()
        {
            if (placeholderRenderer == null)
            {
                placeholderRenderer = GetComponentInChildren<Renderer>();
            }

            if (modelAnchor == null)
            {
                Transform existingAnchor = transform.Find("ModelAnchor");
                if (existingAnchor != null)
                {
                    modelAnchor = existingAnchor;
                }
            }
        }

        private GameObject ResolveCustomerModelPrefab()
        {
            if (data != null && customerModelEntries != null)
            {
                for (int i = 0; i < customerModelEntries.Length; i++)
                {
                    CustomerModelEntry entry = customerModelEntries[i];
                    if (entry == null || entry.modelPrefab == null)
                    {
                        continue;
                    }

                    if (entry.customerType == data.customerType)
                    {
                        return entry.modelPrefab;
                    }
                }
            }

            return customerModelPrefab;
        }

        private void SetCustomerVisible(bool visible)
        {
            CacheRenderers();
            if (cachedRenderers == null)
            {
                return;
            }

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = visible;
            }
        }
    }
}
