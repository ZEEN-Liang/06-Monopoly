using Monopoly.Player;
using Monopoly.UI;
using Monopoly.Board;
using System.Collections.Generic;
using UnityEngine;
using Monopoly.Customer;

namespace Monopoly.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Core References")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Customer.CustomerFlowManager customerFlowManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private PlayerData playerData;
        [Header("Periodic Rent")]
        [SerializeField] private bool enablePeriodicRent = true;
        [SerializeField] private float rentIntervalSeconds = 60f;
        [SerializeField] private bool rentAlsoAppliesToRebuildingShops = true;
        [Header("Evaluation Customer")]
        [SerializeField] private bool enableEvaluationCustomer = true;
        [SerializeField] private float evaluationTriggerSeconds = 600f;
        [SerializeField] private CustomerData evaluationCustomerData;
        [SerializeField] private int evaluationVictorySpendTarget = 300;
        [SerializeField] private int evaluationGradeSThreshold = 260;
        [SerializeField] private int evaluationGradeAThreshold = 180;
        [SerializeField] private int evaluationGradeBThreshold = 100;

        private float rentTimer;
        private float gameElapsedSeconds;
        private bool evaluationTriggered;
        private bool evaluationCompleted;

        public bool EnablePeriodicRent => enablePeriodicRent;
        public float RentIntervalSeconds => rentIntervalSeconds;
        public float RemainingRentTime => enablePeriodicRent ? Mathf.Max(0f, rentIntervalSeconds - rentTimer) : 0f;
        public int PendingRentAmount => CalculatePendingRentAmount();
        public bool EnableEvaluationCustomer => enableEvaluationCustomer;
        public bool EvaluationTriggered => evaluationTriggered;
        public float EvaluationTriggerSeconds => evaluationTriggerSeconds;
        public float RemainingEvaluationTime => enableEvaluationCustomer && !evaluationTriggered
            ? Mathf.Max(0f, evaluationTriggerSeconds - gameElapsedSeconds)
            : 0f;

        public bool IsGameRunning { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SyncPlayerDataReference();

            if (turnManager != null && playerData != null)
            {
                StartGame();
            }
        }

        private void Update()
        {
            SyncPlayerDataReference();

            if (!IsGameRunning || playerData == null)
            {
                return;
            }

            gameElapsedSeconds += Time.deltaTime;
            playerData.TickDebtTimer(Time.deltaTime);
            if (playerData.IsDebtOverdue)
            {
                uiManager?.ShowTransientMessage("Debt deadline missed. Game Over.");
                EndGame(false);
                return;
            }

            if (enableEvaluationCustomer && !evaluationTriggered && gameElapsedSeconds >= evaluationTriggerSeconds)
            {
                StartEvaluationCustomerRun();
                return;
            }

            if (!enablePeriodicRent)
            {
                return;
            }

            rentTimer += Time.deltaTime;
            if (rentTimer >= rentIntervalSeconds)
            {
                while (rentTimer >= rentIntervalSeconds)
                {
                    rentTimer -= rentIntervalSeconds;
                    ApplyPeriodicRent();

                    if (!IsGameRunning)
                    {
                        break;
                    }
                }
            }
        }

        public void StartGame()
        {
            if (IsGameRunning)
            {
                return;
            }

            IsGameRunning = true;
            Time.timeScale = 1f;
            rentTimer = 0f;
            gameElapsedSeconds = 0f;
            evaluationTriggered = false;
            evaluationCompleted = false;

            if (customerFlowManager != null)
            {
                customerFlowManager.StartSpawning();
            }

            if (turnManager != null)
            {
                turnManager.StartPlayerTurn();
            }
        }

        public void Configure(
            TurnManager manager,
            Customer.CustomerFlowManager flowManager,
            UIManager managerUI,
            PlayerData data)
        {
            turnManager = manager;
            customerFlowManager = flowManager;
            uiManager = managerUI;
            playerData = data;

            if (uiManager != null)
            {
                uiManager.Bind(playerData, turnManager);
            }
        }

        public void EndGame(bool success)
        {
            IsGameRunning = false;
            rentTimer = 0f;

            if (customerFlowManager != null)
            {
                customerFlowManager.StopSpawning();
            }

            if (uiManager != null)
            {
                uiManager.ShowGameResult(success);
            }
        }

        public void EndGame(bool success, string title, string body)
        {
            IsGameRunning = false;
            rentTimer = 0f;

            if (customerFlowManager != null)
            {
                customerFlowManager.StopSpawning();
            }

            if (uiManager != null)
            {
                uiManager.ShowGameResult(title, body);
            }
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
        }

        public void CheckGameState()
        {
            if (playerData == null)
            {
                return;
            }

            if (playerData.IsDebtOverdue)
            {
                EndGame(false);
            }
        }

        private void ApplyPeriodicRent()
        {
            PlayerData activePlayerData = ResolveActivePlayerData();
            if (activePlayerData == null)
            {
                return;
            }

            int ownedShopCount = GetRentChargedShopCount();
            if (ownedShopCount <= 0)
            {
                return;
            }

            int totalRent = CalculatePendingRentAmount();
            if (totalRent <= 0)
            {
                return;
            }

            int debtBefore = activePlayerData.CurrentDebtAmount;
            int moneyBefore = activePlayerData.Money;

            if (!activePlayerData.TrySpendMoneyWithDebt(totalRent))
            {
                if (uiManager != null)
                {
                    uiManager.ShowTransientMessage($"Rent due: {totalRent}. Debt is disabled.");
                }
                return;
            }

            if (uiManager != null)
            {
                int debtAdded = Mathf.Max(0, activePlayerData.CurrentDebtAmount - debtBefore);
                int cashPaid = Mathf.Min(totalRent, moneyBefore);
                if (debtAdded > 0)
                {
                    uiManager.ShowTransientMessage($"Paid rent: -{cashPaid}, Debt +{debtAdded} ({ownedShopCount} shops)");
                }
                else
                {
                    uiManager.ShowTransientMessage($"Paid rent: -{totalRent} ({ownedShopCount} shops)");
                }
            }

            Debug.Log($"Periodic rent paid: {totalRent} for {ownedShopCount} owned shops.");
            CheckGameState();
        }

        private void StartEvaluationCustomerRun()
        {
            evaluationTriggered = true;

            if (customerFlowManager != null)
            {
                customerFlowManager.StopSpawning();
            }

            uiManager?.ShowTransientMessage("VIP customer arrived. Final evaluation has started.");

            CustomerAgent evaluationCustomer = customerFlowManager != null
                ? customerFlowManager.SpawnEvaluationCustomer(evaluationCustomerData, OnEvaluationCustomerCompleted)
                : null;

            if (evaluationCustomer == null)
            {
                EndGame(false, "Evaluation Failed", "Unable to spawn the evaluation customer.");
            }
        }

        private void OnEvaluationCustomerCompleted(CustomerAgent customer)
        {
            if (evaluationCompleted)
            {
                return;
            }

            evaluationCompleted = true;
            int spendTotal = customer != null ? customer.PlayerShopSpendTotal : 0;

            if (spendTotal >= evaluationVictorySpendTarget)
            {
                EndGame(true, "Victory", $"VIP spend: {spendTotal}\nTarget: {evaluationVictorySpendTarget}\nLevel cleared.");
                return;
            }

            string grade = ResolveEvaluationGrade(spendTotal);
            EndGame(
                false,
                "Evaluation Complete",
                $"VIP spend: {spendTotal}\nTarget: {evaluationVictorySpendTarget}\nGrade: {grade}");
        }

        private string ResolveEvaluationGrade(int spendTotal)
        {
            if (spendTotal >= evaluationGradeSThreshold)
            {
                return "S";
            }

            if (spendTotal >= evaluationGradeAThreshold)
            {
                return "A";
            }

            if (spendTotal >= evaluationGradeBThreshold)
            {
                return "B";
            }

            return "C";
        }

        private int GetRentChargedShopCount()
        {
            List<ShopTile> ownedShops = GetRuntimeOwnedShops();
            int count = 0;
            for (int i = 0; i < ownedShops.Count; i++)
            {
                ShopTile shopTile = ownedShops[i];
                if (shopTile == null || !shopTile.IsOwned)
                {
                    continue;
                }

                if (!rentAlsoAppliesToRebuildingShops && shopTile.IsUnderConstruction)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private int CalculatePendingRentAmount()
        {
            List<ShopTile> ownedShops = GetRuntimeOwnedShops();
            int totalRent = 0;

            for (int i = 0; i < ownedShops.Count; i++)
            {
                ShopTile shopTile = ownedShops[i];
                if (shopTile == null || !shopTile.IsOwned)
                {
                    continue;
                }

                if (!rentAlsoAppliesToRebuildingShops && shopTile.IsUnderConstruction)
                {
                    continue;
                }

                totalRent += Mathf.Max(0, shopTile.GetCurrentRent());
            }

            return totalRent;
        }

        private List<ShopTile> GetRuntimeOwnedShops()
        {
            List<ShopTile> result = new List<ShopTile>();
            HashSet<ShopTile> seen = new HashSet<ShopTile>();
            PlayerData activePlayerData = ResolveActivePlayerData();

            if (activePlayerData != null && activePlayerData.OwnedShops != null)
            {
                for (int i = 0; i < activePlayerData.OwnedShops.Count; i++)
                {
                    ShopTile shopTile = activePlayerData.OwnedShops[i];
                    if (shopTile == null || seen.Contains(shopTile))
                    {
                        continue;
                    }

                    if (shopTile.IsOwned)
                    {
                        result.Add(shopTile);
                        seen.Add(shopTile);
                    }
                }
            }

            ShopTile[] allShops = FindObjectsOfType<ShopTile>();
            for (int i = 0; i < allShops.Length; i++)
            {
                ShopTile shopTile = allShops[i];
                if (shopTile == null || seen.Contains(shopTile))
                {
                    continue;
                }

                if (shopTile.IsOwned)
                {
                    result.Add(shopTile);
                    seen.Add(shopTile);
                    activePlayerData?.AddOwnedShop(shopTile);
                }
            }

            return result;
        }

        private void SyncPlayerDataReference()
        {
            PlayerData bestData = ResolveActivePlayerData();

            if (bestData != null && bestData != playerData)
            {
                playerData = bestData;
            }
        }

        private PlayerData ResolveActivePlayerData()
        {
            PlayerData bestData = uiManager != null ? uiManager.BoundPlayerData : null;
            bestData = ChooseBetterPlayerData(bestData, turnManager != null ? turnManager.BoundPlayerData : null);
            bestData = ChooseBetterPlayerData(bestData, playerData);
            return bestData;
        }

        private PlayerData ChooseBetterPlayerData(PlayerData current, PlayerData candidate)
        {
            if (candidate == null)
            {
                return current;
            }

            if (current == null)
            {
                return candidate;
            }

            if (candidate.OwnedShopCount > current.OwnedShopCount)
            {
                return candidate;
            }

            if (candidate.TotalDiceRollCount > current.TotalDiceRollCount)
            {
                return candidate;
            }

            return current;
        }
    }
}
