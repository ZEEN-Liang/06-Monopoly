using System.Collections.Generic;
using Monopoly.Board;
using Monopoly.Shop;
using Monopoly.UI;
using UnityEngine;

namespace Monopoly.Player
{
    public class PlayerDecisionController : MonoBehaviour
    {
        [SerializeField] private PlayerData playerData;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private ShopSystem shopSystem;
        [SerializeField] private ShopUpgradeSystem shopUpgradeSystem;
        [SerializeField] private ShopRebuildSystem shopRebuildSystem;
        [SerializeField] private PlayerUpgradeSystem playerUpgradeSystem;

        public void Configure(
            PlayerData data,
            UIManager manager,
            ShopSystem system,
            ShopUpgradeSystem upgradeSystem,
            ShopRebuildSystem rebuildSystem,
            PlayerUpgradeSystem globalUpgradeSystem)
        {
            playerData = data;
            uiManager = manager;
            shopSystem = system;
            shopUpgradeSystem = upgradeSystem;
            shopRebuildSystem = rebuildSystem;
            playerUpgradeSystem = globalUpgradeSystem;
        }

        public void HandleShopTile(ShopTile shopTile)
        {
            SyncRuntimeReferences();
            Debug.Log($"HandleShopTile called. Tile={(shopTile != null ? shopTile.name : "null")}, IsOwned={(shopTile != null && shopTile.IsOwned)}, PlayerData={(playerData != null ? playerData.name : "null")}, UI={(uiManager != null)}, UpgradeSystem={(shopUpgradeSystem != null)}");

            if (shopTile == null)
            {
                return;
            }

            if (!shopTile.IsOwned)
            {
                uiManager?.ShowShopAcquirePanel(
                    shopTile,
                    () =>
                    {
                        bool success = TryAcquireShop(shopTile);
                        uiManager?.ShowTransientMessage(success
                            ? BuildAcquireShopSuccessMessage(shopTile)
                            : "Not enough money to buy this shop");
                    },
                    () => uiManager?.ShowTransientMessage("Skipped this shop"));
                return;
            }

            if (IsOwnedByCurrentPlayer(shopTile))
            {
                Debug.Log($"Owned shop detected. Level={(shopTile.CurrentShop != null ? shopTile.CurrentShop.Level : -1)}");

                if (shopUpgradeSystem != null && !shopUpgradeSystem.CanUpgrade(shopTile))
                {
                    uiManager?.ShowTransientMessage($"This shop reached the level cap ({shopUpgradeSystem.GetCurrentLevelCap()}).");
                    return;
                }

                if (shopUpgradeSystem != null && !shopUpgradeSystem.ShouldOfferSpecialUpgrade(shopTile))
                {
                    Debug.Log("Opening normal upgrade panel.");
                    int cost = shopUpgradeSystem.GetUpgradeCost(shopTile.CurrentShop);
                    uiManager?.ShowNormalShopUpgradePanel(
                        shopTile,
                        cost,
                        () =>
                        {
                            bool success = TryUpgradeShop(shopTile);
                            uiManager?.ShowTransientMessage(success
                                ? $"Normal upgrade applied to {shopTile.CurrentShop.Data.shopName}"
                                : "Not enough money to upgrade this shop");
                        },
                        () => uiManager?.ShowTransientMessage("Skipped normal upgrade"));
                    return;
                }

                List<ShopUpgradeOptionData> options = shopUpgradeSystem != null
                    ? shopUpgradeSystem.GetRandomUpgradeOptions(shopTile, 3)
                    : new List<ShopUpgradeOptionData>();

                if (options == null || options.Count == 0)
                {
                    Debug.LogWarning("Special upgrade options are empty. Falling back to normal upgrade panel.");
                    int fallbackCost = shopUpgradeSystem != null ? shopUpgradeSystem.GetUpgradeCost(shopTile.CurrentShop) : 0;
                    uiManager?.ShowNormalShopUpgradePanel(
                        shopTile,
                        fallbackCost,
                        () =>
                        {
                            bool success = TryUpgradeShop(shopTile);
                            uiManager?.ShowTransientMessage(success
                                ? $"Normal upgrade applied to {shopTile.CurrentShop.Data.shopName}"
                                : "Not enough money to upgrade this shop");
                        },
                        () => uiManager?.ShowTransientMessage("Skipped normal upgrade"));
                    return;
                }

                Debug.Log($"Opening special upgrade panel with {options.Count} options.");
                uiManager?.ShowOwnedShopUpgradePanel(
                    shopTile,
                    options,
                    option =>
                    {
                        bool success = TryUpgradeShop(shopTile, option);
                        uiManager?.ShowTransientMessage(success
                            ? $"Applied {option.title}"
                            : "Not enough money to upgrade this shop");
                    });
                return;
            }

            uiManager?.ShowTransientMessage("This shop is not controlled by the current player.");
        }

        public void HandleUpgradeTile(UpgradeTile upgradeTile)
        {
            List<PlayerUpgradeOptionData> options = playerUpgradeSystem != null
                ? playerUpgradeSystem.GetRandomOptions(playerData, 3)
                : new List<PlayerUpgradeOptionData>();

            uiManager?.ShowGlobalUpgradePanel(
                options,
                option =>
                {
                    playerUpgradeSystem?.ApplyOption(playerData, option);
                    uiManager?.ShowTransientMessage($"Applied global upgrade: {option.title}");
                });
        }

        public bool TryAcquireShop(ShopTile shopTile, ShopData selectedData = null)
        {
            return shopSystem != null && shopSystem.TryAcquireShop(playerData, shopTile, selectedData);
        }

        public bool TryUpgradeShop(ShopTile shopTile, Monopoly.Utils.ShopBranchType branchType = Monopoly.Utils.ShopBranchType.Default)
        {
            return shopUpgradeSystem != null && shopUpgradeSystem.TryUpgradeShop(playerData, shopTile, branchType);
        }

        public bool TryUpgradeShop(ShopTile shopTile, ShopUpgradeOptionData option)
        {
            return shopUpgradeSystem != null && shopUpgradeSystem.TryUpgradeShop(playerData, shopTile, option);
        }

        public bool TryRebuildShop(ShopTile shopTile, ShopData rebuildTarget)
        {
            return shopRebuildSystem != null && shopRebuildSystem.TryStartRebuild(playerData, shopTile, rebuildTarget);
        }

        private string BuildAcquireShopSuccessMessage(ShopTile shopTile)
        {
            string shopName = shopTile != null && shopTile.OriginalShopData != null
                ? shopTile.OriginalShopData.shopName
                : "shop";

            if (playerData != null && playerData.HasDebt)
            {
                return $"Bought {shopName} on credit. Debt: {playerData.CurrentDebtAmount}";
            }

            return $"Bought {shopName}";
        }

        private void SyncRuntimeReferences()
        {
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            if (playerData == null && uiManager != null)
            {
                playerData = uiManager.BoundPlayerData;
            }

            if (playerData == null)
            {
                playerData = FindObjectOfType<PlayerData>();
            }

            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }

            if (shopSystem == null)
            {
                shopSystem = FindObjectOfType<ShopSystem>();
            }

            if (shopUpgradeSystem == null)
            {
                shopUpgradeSystem = FindObjectOfType<ShopUpgradeSystem>();
            }

            if (shopRebuildSystem == null)
            {
                shopRebuildSystem = FindObjectOfType<ShopRebuildSystem>();
            }

            if (playerUpgradeSystem == null)
            {
                playerUpgradeSystem = FindObjectOfType<PlayerUpgradeSystem>();
            }
        }

        private bool IsOwnedByCurrentPlayer(ShopTile shopTile)
        {
            if (shopTile == null || shopTile.CurrentShop == null)
            {
                return false;
            }

            if (shopTile.CurrentShop.Owner == playerData)
            {
                return true;
            }

            return playerData != null && playerData.OwnedShops.Contains(shopTile);
        }
    }
}
