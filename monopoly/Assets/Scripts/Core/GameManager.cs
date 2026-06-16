using Monopoly.Player;
using Monopoly.UI;
using Monopoly.Board;
using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private int rentPerOwnedShop = 15;
        [SerializeField] private int extraRentPerShopLevel = 10;
        [SerializeField] private bool rentAlsoAppliesToRebuildingShops = true;

        private float rentTimer;

        public bool EnablePeriodicRent => enablePeriodicRent;
        public float RentIntervalSeconds => rentIntervalSeconds;
        public int RentPerOwnedShop => rentPerOwnedShop;
        public int ExtraRentPerShopLevel => extraRentPerShopLevel;
        public float RemainingRentTime => enablePeriodicRent ? Mathf.Max(0f, rentIntervalSeconds - rentTimer) : 0f;
        public int PendingRentAmount => CalculatePendingRentAmount();

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

            if (!IsGameRunning || !enablePeriodicRent || playerData == null)
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

            if (playerData.Money < 0 || playerData.Satisfaction <= 0)
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

            if (!activePlayerData.SpendMoney(totalRent))
            {
                if (uiManager != null)
                {
                    uiManager.ShowTransientMessage($"Rent due: {totalRent}. Unable to pay.");
                }

                EndGame(false);
                return;
            }

            if (uiManager != null)
            {
                uiManager.ShowTransientMessage($"Paid rent: -{totalRent} ({ownedShopCount} shops)");
            }

            Debug.Log($"Periodic rent paid: {totalRent} for {ownedShopCount} owned shops.");
            CheckGameState();
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

                int shopLevel = shopTile.CurrentShop != null ? Mathf.Max(1, shopTile.CurrentShop.Level) : 1;
                totalRent += Mathf.Max(0, rentPerOwnedShop);
                totalRent += Mathf.Max(0, shopLevel - 1) * Mathf.Max(0, extraRentPerShopLevel);
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
