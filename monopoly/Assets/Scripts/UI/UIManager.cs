using System.Collections.Generic;
using Monopoly.Board;
using Monopoly.Core;
using Monopoly.Events;
using Monopoly.Player;
using Monopoly.Shop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace Monopoly.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Runtime Binding")]
        [SerializeField] private PlayerData playerData;
        [SerializeField] private TurnManager turnManager;

        [Header("HUD")]
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private Text moneyText;
        [SerializeField] private Text satisfactionText;
        [SerializeField] private Text turnText;
        [SerializeField] private Text shopCountText;
        [SerializeField] private Text rentText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject inspectPanel;
        [SerializeField] private Text inspectTitleText;
        [SerializeField] private Text inspectBodyText;
        [SerializeField] private Vector3 inspectWorldOffset = new Vector3(1.3f, 1.4f, 0f);
        [Header("Choice UI")]
        [SerializeField] private GameObject modalPanel;
        [SerializeField] private Text modalTitleText;
        [SerializeField] private Text modalBodyText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text confirmButtonText;
        [SerializeField] private Text cancelButtonText;
        [SerializeField] private GameObject tripleChoicePanel;
        [SerializeField] private Text tripleChoiceTitleText;
        [SerializeField] private Text tripleChoiceBodyText;
        [SerializeField] private Button choiceOneButton;
        [SerializeField] private Button choiceTwoButton;
        [SerializeField] private Button choiceThreeButton;
        [SerializeField] private Text choiceOneText;
        [SerializeField] private Text choiceTwoText;
        [SerializeField] private Text choiceThreeText;

        private Action pendingConfirmAction;
        private Action pendingCancelAction;
        private Action pendingChoiceOneAction;
        private Action pendingChoiceTwoAction;
        private Action pendingChoiceThreeAction;
        private BoardTile selectedInspectTile;

        public bool IsBlockingChoiceActive =>
            (modalPanel != null && modalPanel.activeSelf) ||
            (tripleChoicePanel != null && tripleChoicePanel.activeSelf);
        public PlayerData BoundPlayerData => playerData;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            EnsureHud();
            RefreshAll();
        }

        private void Update()
        {
            if (playerData == null && GameManager.Instance != null)
            {
                // Keep polling lightly so manually assembled scenes can still pick up bindings later.
            }

            RefreshAll();
            HandleTileInspectInput();
            UpdateInspectPanelPosition();
        }

        public void Bind(PlayerData data, TurnManager manager)
        {
            playerData = data;
            turnManager = manager;
            EnsureHud();
            RefreshAll();
        }

        public void RefreshMoney(int value)
        {
            EnsureHud();
            if (moneyText != null)
            {
                moneyText.text = $"Money: {value}";
            }
        }

        public void RefreshSatisfaction(int value)
        {
            EnsureHud();
            if (satisfactionText != null)
            {
                string rollInfo = playerData != null ? $" | Rolls: {playerData.TotalDiceRollCount}" : string.Empty;
                satisfactionText.text = $"Satisfaction: {value}{rollInfo}";
            }
        }

        public void ShowTurnStart(int turn)
        {
            EnsureHud();
            if (turnText != null)
            {
                turnText.text = $"Turn: {turn}";
            }

            SetMessage($"Turn {turn} start. Press Space to roll.");
            Debug.Log($"Turn {turn} start. Press Space to roll.");
        }

        public void ShowDiceResult(int value)
        {
            SetMessage($"Dice roll: {value}");
            Debug.Log($"Dice roll: {value}");
        }

        public void ShowShopAcquirePanel(ShopTile shopTile, Action onConfirm, Action onCancel)
        {
            if (shopTile == null)
            {
                return;
            }

            string shopName = shopTile.OriginalShopData != null ? shopTile.OriginalShopData.shopName : shopTile.name;
            int cost = shopTile.OriginalShopData != null ? shopTile.OriginalShopData.acquireCost : 0;

            ShowChoiceModal(
                "Buy Shop?",
                $"{shopName}\nCost: {cost}\nBuy this shop?",
                "Buy",
                "Skip",
                onConfirm,
                onCancel);

            Debug.Log($"Acquire shop: {shopTile.name}. Waiting for player choice.");
        }

        public void ShowOwnedShopPanel(ShopTile shopTile)
        {
            SetMessage($"Owned shop: {shopTile.name}");
            Debug.Log($"Owned shop menu: {shopTile.name}. Auto-upgrade is enabled in the demo scaffold.");
        }

        public void ShowNormalShopUpgradePanel(ShopTile shopTile, int cost, Action onConfirm, Action onCancel)
        {
            if (shopTile == null || shopTile.CurrentShop == null || shopTile.CurrentShop.Data == null)
            {
                return;
            }

            ShowChoiceModal(
                $"Upgrade {shopTile.CurrentShop.Data.shopName}?",
                $"Current Lv.{shopTile.CurrentShop.Level}\nUpgrade Cost: {cost}\nSpend money to upgrade by 1 level?",
                "Upgrade",
                "Skip",
                onConfirm,
                onCancel);
        }

        public void ShowOwnedShopUpgradePanel(ShopTile shopTile, List<ShopUpgradeOptionData> options, Action<ShopUpgradeOptionData> onOptionSelected)
        {
            if (shopTile == null || shopTile.CurrentShop == null || shopTile.CurrentShop.Data == null)
            {
                return;
            }

            if (options == null || options.Count == 0)
            {
                ShowTransientMessage("No upgrade options available.");
                return;
            }

            int currentCost = shopTile.CurrentShop.Data.baseUpgradeCost + (shopTile.CurrentShop.Level - 1) * 25 - shopTile.CurrentShop.UpgradeDiscount;
            currentCost = Mathf.Max(0, currentCost);

            string choiceOneLabel = options.Count > 0 ? BuildUpgradeChoiceText(options[0]) : "N/A";
            string choiceTwoLabel = options.Count > 1 ? BuildUpgradeChoiceText(options[1]) : "N/A";
            string choiceThreeLabel = options.Count > 2 ? BuildUpgradeChoiceText(options[2]) : "N/A";
            Action choiceOneAction = options.Count > 0 ? (Action)(() => onOptionSelected?.Invoke(options[0])) : null;
            Action choiceTwoAction = options.Count > 1 ? (Action)(() => onOptionSelected?.Invoke(options[1])) : null;
            Action choiceThreeAction = options.Count > 2 ? (Action)(() => onOptionSelected?.Invoke(options[2])) : null;

            ShowTripleChoiceModal(
                $"Upgrade {shopTile.CurrentShop.Data.shopName}",
                $"Current Lv.{shopTile.CurrentShop.Level}\nUpgrade Cost: {currentCost}\nChoose one random effect:",
                choiceOneLabel,
                choiceTwoLabel,
                choiceThreeLabel,
                choiceOneAction,
                choiceTwoAction,
                choiceThreeAction);

            Debug.Log($"Upgrade choice opened for {shopTile.CurrentShop.Data.shopName}");
        }

        public void ShowUpgradePanel(List<ShopTile> ownedShops)
        {
            SetMessage($"Upgrade chance. Owned shops: {ownedShops.Count}");
            Debug.Log($"Show upgrade panel. Owned shops: {ownedShops.Count}. Auto-upgrade first owned shop in the demo scaffold.");
        }

        public void ShowGlobalUpgradePanel(List<PlayerUpgradeOptionData> options, Action<PlayerUpgradeOptionData> onOptionSelected)
        {
            if (options == null || options.Count == 0)
            {
                ShowTransientMessage("No global upgrades available.");
                return;
            }

            string choiceOneLabel = options.Count > 0 ? BuildPlayerUpgradeChoiceText(options[0]) : "N/A";
            string choiceTwoLabel = options.Count > 1 ? BuildPlayerUpgradeChoiceText(options[1]) : "N/A";
            string choiceThreeLabel = options.Count > 2 ? BuildPlayerUpgradeChoiceText(options[2]) : "N/A";
            Action choiceOneAction = options.Count > 0 ? (Action)(() => onOptionSelected?.Invoke(options[0])) : null;
            Action choiceTwoAction = options.Count > 1 ? (Action)(() => onOptionSelected?.Invoke(options[1])) : null;
            Action choiceThreeAction = options.Count > 2 ? (Action)(() => onOptionSelected?.Invoke(options[2])) : null;

            ShowTripleChoiceModal(
                "Global Upgrade",
                "Choose one player-wide or strategic bonus:",
                choiceOneLabel,
                choiceTwoLabel,
                choiceThreeLabel,
                choiceOneAction,
                choiceTwoAction,
                choiceThreeAction);
        }

        public void ShowEventPanel(EventData eventData, PlayerData currentPlayerData)
        {
            SetMessage($"Event: {eventData.title}");
            Debug.Log($"Event: {eventData.title}");
        }

        public void ShowEventPanel(EventData eventData, PlayerData currentPlayerData, Action<EventChoiceData> onChoiceSelected)
        {
            if (eventData == null)
            {
                return;
            }

            if (eventData.choices == null || eventData.choices.Count == 0)
            {
                ShowTransientMessage($"Event: {eventData.title}");
                return;
            }

            string choiceOneLabel = eventData.choices.Count > 0 ? BuildEventChoiceText(eventData.choices[0]) : "N/A";
            string choiceTwoLabel = eventData.choices.Count > 1 ? BuildEventChoiceText(eventData.choices[1]) : "N/A";
            string choiceThreeLabel = eventData.choices.Count > 2 ? BuildEventChoiceText(eventData.choices[2]) : "N/A";

            Action choiceOneAction = eventData.choices.Count > 0 ? (Action)(() => onChoiceSelected?.Invoke(eventData.choices[0])) : null;
            Action choiceTwoAction = eventData.choices.Count > 1 ? (Action)(() => onChoiceSelected?.Invoke(eventData.choices[1])) : null;
            Action choiceThreeAction = eventData.choices.Count > 2 ? (Action)(() => onChoiceSelected?.Invoke(eventData.choices[2])) : null;

            ShowTripleChoiceModal(
                eventData.title,
                eventData.description,
                choiceOneLabel,
                choiceTwoLabel,
                choiceThreeLabel,
                choiceOneAction,
                choiceTwoAction,
                choiceThreeAction);

            Debug.Log($"Event choice opened: {eventData.title}");
        }

        public void ShowGameResult(bool success)
        {
            SetMessage(success ? "Game Success" : "Game Failed");
            Debug.Log(success ? "Game Success" : "Game Failed");
        }

        public void ShowTransientMessage(string message)
        {
            SetMessage(message);
            Debug.Log(message);
        }

        public void ShowTileInspectPanel(BoardTile tile)
        {
            EnsureHud();
            EnsureInspectPanel();

            if (selectedInspectTile != null && selectedInspectTile != tile)
            {
                selectedInspectTile.SetSelected(false);
            }

            if (tile == null)
            {
                HideTileInspectPanel();
                return;
            }

            selectedInspectTile = tile;
            selectedInspectTile.SetSelected(true);
            inspectTitleText.text = tile.GetInspectTitle();
            inspectBodyText.text = tile.GetInspectBody();
            inspectPanel.SetActive(true);
            UpdateInspectPanelPosition();
        }

        public void HideTileInspectPanel()
        {
            if (selectedInspectTile != null)
            {
                selectedInspectTile.SetSelected(false);
                selectedInspectTile = null;
            }

            if (inspectPanel != null)
            {
                inspectPanel.SetActive(false);
            }
        }

        private void RefreshAll()
        {
            if (playerData == null)
            {
                return;
            }

            RefreshMoney(playerData.Money);
            RefreshSatisfaction(playerData.Satisfaction);

            if (shopCountText != null)
            {
                shopCountText.text = $"Owned Shops: {playerData.OwnedShops.Count}";
            }

            if (rentText != null)
            {
                GameManager gameManager = GameManager.Instance;
                if (gameManager != null && gameManager.EnablePeriodicRent)
                {
                    int seconds = Mathf.CeilToInt(gameManager.RemainingRentTime);
                    rentText.text = $"Rent In: {seconds}s  Fee: {gameManager.PendingRentAmount}";
                }
                else
                {
                    rentText.text = "Rent: Off";
                }
            }

            if (turnManager != null && turnText != null && !string.IsNullOrEmpty(turnText.text) == false)
            {
                turnText.text = $"Turn: {turnManager.CurrentTurn}";
            }
            else if (turnManager != null && turnText != null)
            {
                turnText.text = $"Turn: {turnManager.CurrentTurn}";
            }
        }

        private void EnsureHud()
        {
            if (hudCanvas != null && moneyText != null && satisfactionText != null && turnText != null && shopCountText != null && rentText != null && messageText != null)
            {
                EnsureEventSystem();
                EnsureChoicePanel();
                EnsureTripleChoicePanel();
                EnsureInspectPanel();
                return;
            }

            GameObject canvasObject = new GameObject("HUDCanvas");
            canvasObject.transform.SetParent(transform);

            hudCanvas = canvasObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panelObject = new GameObject("HUDPanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(20f, -20f);
            panelRect.sizeDelta = new Vector2(360f, 180f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(1f, 0.97f, 0.9f, 0.78f);

            moneyText = CreateText(panelObject.transform, "MoneyText", new Vector2(16f, -16f));
            satisfactionText = CreateText(panelObject.transform, "SatisfactionText", new Vector2(16f, -46f));
            turnText = CreateText(panelObject.transform, "TurnText", new Vector2(16f, -76f));
            shopCountText = CreateText(panelObject.transform, "ShopCountText", new Vector2(16f, -106f));
            rentText = CreateText(panelObject.transform, "RentText", new Vector2(16f, -136f), 20);
            messageText = CreateText(panelObject.transform, "MessageText", new Vector2(16f, -166f), 18);

            EnsureEventSystem();
            EnsureChoicePanel();
            EnsureTripleChoicePanel();
            EnsureInspectPanel();
        }

        private Text CreateText(Transform parent, string objectName, Vector2 anchoredPosition, int fontSize = 22)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(320f, 28f);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.color = new Color(0.16f, 0.14f, 0.1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void SetMessage(string message)
        {
            EnsureHud();
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        private void HandleTileInspectInput()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Camera.main == null)
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hitInfo, 500f))
            {
                HideTileInspectPanel();
                return;
            }

            BoardTile tile = hitInfo.collider.GetComponentInParent<BoardTile>();
            if (tile != null)
            {
                ShowTileInspectPanel(tile);
            }
            else
            {
                HideTileInspectPanel();
            }
        }

        private void UpdateInspectPanelPosition()
        {
            if (inspectPanel == null || !inspectPanel.activeSelf || selectedInspectTile == null || Camera.main == null)
            {
                return;
            }

            Vector3 worldPosition = selectedInspectTile.transform.position + inspectWorldOffset;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            RectTransform panelRect = inspectPanel.GetComponent<RectTransform>();
            if (panelRect == null)
            {
                return;
            }

            inspectPanel.SetActive(screenPosition.z > 0f);
            if (!inspectPanel.activeSelf)
            {
                return;
            }

            panelRect.position = screenPosition;
        }

        private string BuildUpgradeChoiceText(ShopUpgradeOptionData option)
        {
            if (option == null)
            {
                return "N/A";
            }

            return $"[{option.tier}]\n{option.title}\n{option.description}";
        }

        private string BuildPlayerUpgradeChoiceText(PlayerUpgradeOptionData option)
        {
            if (option == null)
            {
                return "N/A";
            }

            return $"[{option.tier}]\n{option.title}\n{option.description}";
        }

        private string BuildEventChoiceText(EventChoiceData choiceData)
        {
            if (choiceData == null)
            {
                return "N/A";
            }

            string moneyPart = choiceData.moneyDelta == 0
                ? "Money 0"
                : choiceData.moneyDelta > 0
                    ? $"Money +{choiceData.moneyDelta}"
                    : $"Money {choiceData.moneyDelta}";

            string satisfactionPart = choiceData.satisfactionDelta == 0
                ? "Sat 0"
                : choiceData.satisfactionDelta > 0
                    ? $"Sat +{choiceData.satisfactionDelta}"
                    : $"Sat {choiceData.satisfactionDelta}";

            return $"{choiceData.choiceText}\n{moneyPart}  {satisfactionPart}";
        }

        private void EnsureChoicePanel()
        {
            if (modalPanel != null && modalTitleText != null && modalBodyText != null && confirmButton != null && cancelButton != null)
            {
                return;
            }

            GameObject panelObject = new GameObject("ChoicePanel");
            panelObject.transform.SetParent(hudCanvas.transform, false);
            modalPanel = panelObject;

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(420f, 260f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.98f, 0.95f, 0.88f, 0.97f);

            modalTitleText = CreateText(panelObject.transform, "ModalTitle", new Vector2(24f, -20f), 26);
            modalBodyText = CreateText(panelObject.transform, "ModalBody", new Vector2(24f, -70f), 22);
            modalBodyText.rectTransform.sizeDelta = new Vector2(360f, 100f);
            modalBodyText.alignment = TextAnchor.UpperLeft;

            confirmButton = CreateButton(panelObject.transform, "ConfirmButton", new Vector2(40f, -190f), new Vector2(140f, 42f));
            confirmButtonText = confirmButton.GetComponentInChildren<Text>();
            confirmButtonText.text = "Confirm";
            confirmButton.onClick.AddListener(HandleConfirmClicked);

            cancelButton = CreateButton(panelObject.transform, "CancelButton", new Vector2(230f, -190f), new Vector2(140f, 42f));
            cancelButtonText = cancelButton.GetComponentInChildren<Text>();
            cancelButtonText.text = "Cancel";
            cancelButton.onClick.AddListener(HandleCancelClicked);

            modalPanel.SetActive(false);
        }

        private void EnsureInspectPanel()
        {
            if (inspectPanel != null && inspectTitleText != null && inspectBodyText != null)
            {
                return;
            }

            GameObject panelObject = new GameObject("InspectPanel");
            panelObject.transform.SetParent(hudCanvas.transform, false);
            inspectPanel = panelObject;

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, 0f);
            panelRect.sizeDelta = new Vector2(220f, 120f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.98f, 0.95f, 0.88f, 0.95f);

            inspectTitleText = CreateText(panelObject.transform, "InspectTitle", new Vector2(12f, -12f), 22);
            inspectTitleText.rectTransform.sizeDelta = new Vector2(190f, 28f);

            inspectBodyText = CreateText(panelObject.transform, "InspectBody", new Vector2(12f, -42f), 18);
            inspectBodyText.rectTransform.sizeDelta = new Vector2(190f, 70f);
            inspectBodyText.alignment = TextAnchor.UpperLeft;

            inspectTitleText.text = string.Empty;
            inspectBodyText.text = string.Empty;
            inspectPanel.SetActive(false);
        }

        private void EnsureTripleChoicePanel()
        {
            if (tripleChoicePanel != null && tripleChoiceTitleText != null && tripleChoiceBodyText != null)
            {
                return;
            }

            GameObject panelObject = new GameObject("TripleChoicePanel");
            panelObject.transform.SetParent(hudCanvas.transform, false);
            tripleChoicePanel = panelObject;

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(560f, 300f);

            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.98f, 0.95f, 0.88f, 0.97f);

            tripleChoiceTitleText = CreateText(panelObject.transform, "TripleChoiceTitle", new Vector2(24f, -20f), 26);
            tripleChoiceBodyText = CreateText(panelObject.transform, "TripleChoiceBody", new Vector2(24f, -60f), 20);
            tripleChoiceBodyText.rectTransform.sizeDelta = new Vector2(500f, 70f);
            tripleChoiceBodyText.alignment = TextAnchor.UpperLeft;

            choiceOneButton = CreateButton(panelObject.transform, "ChoiceOneButton", new Vector2(20f, -150f), new Vector2(160f, 90f));
            choiceOneText = choiceOneButton.GetComponentInChildren<Text>();
            choiceOneButton.onClick.AddListener(HandleChoiceOneClicked);

            choiceTwoButton = CreateButton(panelObject.transform, "ChoiceTwoButton", new Vector2(200f, -150f), new Vector2(160f, 90f));
            choiceTwoText = choiceTwoButton.GetComponentInChildren<Text>();
            choiceTwoButton.onClick.AddListener(HandleChoiceTwoClicked);

            choiceThreeButton = CreateButton(panelObject.transform, "ChoiceThreeButton", new Vector2(380f, -150f), new Vector2(160f, 90f));
            choiceThreeText = choiceThreeButton.GetComponentInChildren<Text>();
            choiceThreeButton.onClick.AddListener(HandleChoiceThreeClicked);

            tripleChoicePanel.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private Button CreateButton(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.95f, 0.72f, 0.38f, 1f);

            Button button = buttonObject.AddComponent<Button>();

            Text label = CreateText(buttonObject.transform, "Label", new Vector2(0f, 0f), 20);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            label.alignment = TextAnchor.MiddleCenter;

            return button;
        }

        private void ShowChoiceModal(
            string title,
            string body,
            string confirmText,
            string cancelText,
            Action onConfirm,
            Action onCancel)
        {
            EnsureHud();
            EnsureChoicePanel();

            pendingConfirmAction = onConfirm;
            pendingCancelAction = onCancel;

            modalTitleText.text = title;
            modalBodyText.text = body;
            confirmButtonText.text = confirmText;
            cancelButtonText.text = cancelText;
            modalPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void ShowTripleChoiceModal(
            string title,
            string body,
            string choiceOneLabel,
            string choiceTwoLabel,
            string choiceThreeLabel,
            Action onChoiceOne,
            Action onChoiceTwo,
            Action onChoiceThree)
        {
            EnsureHud();
            EnsureTripleChoicePanel();

            pendingChoiceOneAction = onChoiceOne;
            pendingChoiceTwoAction = onChoiceTwo;
            pendingChoiceThreeAction = onChoiceThree;

            tripleChoiceTitleText.text = title;
            tripleChoiceBodyText.text = body;
            choiceOneText.text = choiceOneLabel;
            choiceTwoText.text = choiceTwoLabel;
            choiceThreeText.text = choiceThreeLabel;
            tripleChoicePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void HandleConfirmClicked()
        {
            pendingConfirmAction?.Invoke();
            CloseChoiceModal();
        }

        private void HandleCancelClicked()
        {
            pendingCancelAction?.Invoke();
            CloseChoiceModal();
        }

        private void HandleChoiceOneClicked()
        {
            pendingChoiceOneAction?.Invoke();
            CloseChoiceModal();
        }

        private void HandleChoiceTwoClicked()
        {
            pendingChoiceTwoAction?.Invoke();
            CloseChoiceModal();
        }

        private void HandleChoiceThreeClicked()
        {
            pendingChoiceThreeAction?.Invoke();
            CloseChoiceModal();
        }

        private void CloseChoiceModal()
        {
            pendingConfirmAction = null;
            pendingCancelAction = null;
            pendingChoiceOneAction = null;
            pendingChoiceTwoAction = null;
            pendingChoiceThreeAction = null;

            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }

            if (tripleChoicePanel != null)
            {
                tripleChoicePanel.SetActive(false);
            }

            Time.timeScale = 1f;
        }
    }
}
