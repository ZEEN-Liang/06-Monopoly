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
                            ? $"Bought {shopTile.OriginalShopData.shopName}"
                            : "Not enough money to buy this shop");
                    },
                    () => uiManager?.ShowTransientMessage("Skipped this shop"));
                return;
            }

            if (shopTile.CurrentShop != null && shopTile.CurrentShop.Owner == playerData)
            {
                if (shopUpgradeSystem != null && !shopUpgradeSystem.CanUpgrade(shopTile))
                {
                    uiManager?.ShowTransientMessage($"This shop reached the level cap ({shopUpgradeSystem.GetCurrentLevelCap()}).");
                    return;
                }

                if (shopUpgradeSystem != null && !shopUpgradeSystem.ShouldOfferSpecialUpgrade(shopTile))
                {
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
            }
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
    }
}
