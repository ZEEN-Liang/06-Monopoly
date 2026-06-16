using Monopoly.Player;
using Monopoly.Shop;
using Monopoly.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Monopoly.Board
{
    public class ShopTile : BoardTile
    {
        [SerializeField] private ShopData originalShopData;
        [SerializeField] private ShopData[] possibleShopTemplates;
        [SerializeField] private ShopInstance currentShop;
        [SerializeField] private bool isUnderConstruction;
        [Header("Visual")]
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private Color defaultShopColor = new Color(1f, 0.4f, 0.4f);
        [SerializeField] private Color ownedShopColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color underConstructionColor = Color.gray;

        public ShopData OriginalShopData => originalShopData;
        public ShopInstance CurrentShop => currentShop;
        public bool IsOwned => currentShop != null && currentShop.IsOwned;
        public bool IsUnderConstruction => isUnderConstruction;

        private void Reset()
        {
            tileType = TileType.Shop;
        }

        private void Start()
        {
            CacheRenderer();
            RefreshColor();
        }

        public void Configure(ShopData sourceShopData, PathNode node = null)
        {
            originalShopData = sourceShopData;
            if (node != null)
            {
                bindNode = node;
            }
            tileType = TileType.Shop;
            CacheRenderer();
            RefreshColor();
            RefreshDebugLabel(GetDebugLabelText());
        }

        public void ConfigureRandom(List<ShopData> candidateShopData, PathNode node = null)
        {
            ShopData selectedData = GetRandomShopTemplate(candidateShopData);
            Configure(selectedData, node);
        }

        public override void OnPlayerLanded(PlayerPawn player)
        {
            if (player == null)
            {
                return;
            }

            player.DecisionController?.HandleShopTile(this);
        }

        public override void OnCustomerLanded(Customer.CustomerAgent customer)
        {
            if (customer == null || currentShop == null || !currentShop.IsOwned || isUnderConstruction)
            {
                return;
            }

            int income = currentShop.CalculateIncome(customer.Data);
            income += currentShop.Owner != null ? currentShop.Owner.GlobalShopIncomeBonus : 0;
            income += currentShop.Owner != null && currentShop.Data != null && customer.Data != null &&
                      customer.Data.preferredCategories.Contains(currentShop.Data.category)
                ? currentShop.Owner.GlobalPreferredIncomeBonus
                : 0;
            currentShop.Owner?.AddMoney(income);
            customer.ShowIncomePopup(income);
        }

        public override float GetCustomerStopDuration(Customer.CustomerAgent customer)
        {
            ShopData targetData = currentShop != null ? currentShop.Data : originalShopData;
            if (targetData == null)
            {
                return 0f;
            }

            if (currentShop != null)
            {
                return currentShop.CalculateCustomerStopDuration(customer != null ? customer.Data : null);
            }

            float baseDuration = customer != null && customer.Data != null ? customer.Data.baseStopDuration : 2.2f;
            float minDuration = customer != null && customer.Data != null ? customer.Data.minStopDuration : 0.8f;
            return Mathf.Max(minDuration, (baseDuration + targetData.customerStopFlatModifier) * targetData.customerStopMultiplier);
        }

        public override string GetInspectTitle()
        {
            ShopData targetData = currentShop != null ? currentShop.Data : originalShopData;
            return targetData != null ? targetData.shopName : "Shop Tile";
        }

        public override string GetInspectBody()
        {
            ShopData targetData = currentShop != null ? currentShop.Data : originalShopData;
            string category = targetData != null ? targetData.category.ToString() : "Unknown";
            string baseIncome = targetData != null ? targetData.baseIncome.ToString() : "-";
            string ownerState = IsOwned ? "Owned" : "Unowned";
            string levelText = currentShop != null ? currentShop.Level.ToString() : "-";
            string stayText = GetCustomerStopDuration(null).ToString("0.0");
            string upgradeCost = currentShop != null && currentShop.Data != null
                ? Mathf.Max(0, currentShop.Data.baseUpgradeCost + (currentShop.Level - 1) * 25 - currentShop.UpgradeDiscount).ToString()
                : "-";

            return
                $"{ownerState}  Lv.{levelText}\n" +
                $"{category}  Income {baseIncome}\n" +
                $"Stay {stayText}s  Next {upgradeCost}" +
                (isUnderConstruction ? "\nRebuilding" : string.Empty);
        }

        public void Acquire(PlayerData owner, ShopData shopDataOverride = null)
        {
            ShopData selectedData = shopDataOverride != null ? shopDataOverride : originalShopData;
            currentShop = new ShopInstance(selectedData, owner);
            RefreshColor();
            RefreshDebugLabel(GetDebugLabelText());
        }

        public void StartRebuild()
        {
            isUnderConstruction = true;
            RefreshColor();
            RefreshDebugLabel(GetDebugLabelText());
        }

        public void FinishRebuild(ShopData newShopData)
        {
            if (currentShop != null)
            {
                currentShop.ReplaceData(newShopData);
            }

            isUnderConstruction = false;
            RefreshColor();
            RefreshDebugLabel(GetDebugLabelText());
        }

        public void RefreshVisualState()
        {
            CacheRenderer();
            RefreshColor();
            RefreshDebugLabel(GetDebugLabelText());
        }

        private void CacheRenderer()
        {
            if (tileRenderer == null)
            {
                tileRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private ShopData GetRandomShopTemplate(List<ShopData> candidateShopData)
        {
            if (possibleShopTemplates != null && possibleShopTemplates.Length > 0)
            {
                return possibleShopTemplates[Random.Range(0, possibleShopTemplates.Length)];
            }

            if (candidateShopData != null && candidateShopData.Count > 0)
            {
                return candidateShopData[Random.Range(0, candidateShopData.Count)];
            }

            return originalShopData;
        }

        private void RefreshColor()
        {
            if (tileRenderer == null)
            {
                return;
            }

            if (isUnderConstruction)
            {
                tileRenderer.material.color = underConstructionColor;
                return;
            }

            tileRenderer.material.color = IsOwned ? GetCategoryColor() : defaultShopColor;
        }

        private Color GetCategoryColor()
        {
            ShopData targetData = currentShop != null ? currentShop.Data : originalShopData;
            if (targetData == null)
            {
                return ownedShopColor;
            }

            switch (targetData.category)
            {
                case ShopCategory.Drink:
                    return new Color(0.4f, 0.8f, 1f);
                case ShopCategory.Snack:
                    return new Color(1f, 0.55f, 0.3f);
                case ShopCategory.Dessert:
                    return new Color(1f, 0.6f, 0.85f);
                case ShopCategory.Seafood:
                    return new Color(0.35f, 0.85f, 0.9f);
                case ShopCategory.MainDish:
                    return new Color(0.8f, 0.45f, 0.25f);
                case ShopCategory.Support:
                    return new Color(0.7f, 0.7f, 1f);
                default:
                    return ownedShopColor;
            }
        }

        private string GetDebugLabelText()
        {
            ShopData targetData = currentShop != null ? currentShop.Data : originalShopData;
            string shopName = targetData != null ? targetData.shopName : "Unknown Shop";

            if (isUnderConstruction)
            {
                return $"Shop\n{shopName}\nRebuilding";
            }

            if (IsOwned)
            {
                return $"Shop\n{shopName}\nOwned Lv.{currentShop.Level}";
            }

            return $"Shop\n{shopName}\nUnowned";
        }
    }
}
