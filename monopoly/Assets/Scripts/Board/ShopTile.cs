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
        [Header("Autonomous Growth")]
        [SerializeField] private int marketLevel = 1;
        [SerializeField] private int autonomousCustomerStayCount;
        [Header("Visual")]
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private Color defaultShopColor = new Color(1f, 0.4f, 0.4f);
        [SerializeField] private Color ownedShopColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color underConstructionColor = Color.gray;

        public ShopData OriginalShopData => originalShopData;
        public ShopInstance CurrentShop => currentShop;
        public bool IsOwned => currentShop != null && currentShop.IsOwned;
        public bool IsUnderConstruction => isUnderConstruction;
        public ShopCategory GeneratedCategory => (currentShop != null ? currentShop.Data : originalShopData) != null
            ? (currentShop != null ? currentShop.Data : originalShopData).category
            : ShopCategory.FastFood;

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
            currentShop = null;
            isUnderConstruction = false;
            marketLevel = GetWildStartingLevel();
            autonomousCustomerStayCount = 0;
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

        public void ConfigureRandom(ShopPoolData shopPoolData, PathNode node = null)
        {
            ShopData selectedData = shopPoolData != null ? shopPoolData.GetRandomShopData() : null;
            if (selectedData == null)
            {
                selectedData = GetRandomShopTemplate(null);
            }

            Configure(selectedData, node);
        }

        public void ConfigureRandom(ShopCategoryPoolData categoryPoolData, PathNode node = null)
        {
            ShopData selectedData = categoryPoolData != null ? categoryPoolData.GetRandomShopData() : null;
            if (selectedData == null)
            {
                selectedData = GetRandomShopTemplate(null);
            }

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
            if (customer == null || isUnderConstruction)
            {
                return;
            }

            if (!customer.ShouldConsumeAtShop(this))
            {
                return;
            }

            if (!IsOwned)
            {
                RegisterAutonomousCustomerStay();
                return;
            }

            if (currentShop == null || !currentShop.IsOwned)
            {
                return;
            }

            int income = currentShop.CalculateIncome(customer.Data);
            income += currentShop.Owner != null ? currentShop.Owner.GlobalShopIncomeBonus : 0;
            currentShop.Owner?.AddMoney(income);
            customer.RegisterShopSpend(income, true);
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
            float marketStopBonus = GetAutonomousStopBonus();
            float levelReduction = Mathf.Max(0, GetEffectiveLevel() - 1) * GetOwnedStayDurationReductionPerLevel();
            return Mathf.Max(
                minDuration,
                (baseDuration + targetData.baseCustomerStayDuration - levelReduction + marketStopBonus) * targetData.customerStayDurationMultiplier);
        }

        public float GetAttractionScore(Customer.CustomerAgent customer)
        {
            ShopData targetData = currentShop != null ? currentShop.Data : originalShopData;
            if (targetData == null)
            {
                return 0f;
            }

            if (currentShop != null)
            {
                return currentShop.CalculateAttractionScore(customer != null ? customer.Data : null);
            }

            float attraction = Mathf.Max(0f, targetData.baseAttractionRate) + GetAutonomousAttractionBonus();
            attraction += Mathf.Max(0, GetEffectiveLevel() - 1) * GetOwnedAttractionGrowthPerLevel();

            return Mathf.Max(0f, attraction);
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
            string profitText = targetData != null ? GetDisplayedProfitBase(targetData).ToString() : "-";
            string ownerState = IsOwned ? "Owned" : "Unowned";
            string levelText = GetEffectiveLevel().ToString();
            string stayText = GetCustomerStopDuration(null).ToString("0.0");
            string attractionText = GetAttractionScore(null).ToString("0.0");
            string rentText = GetCurrentRent().ToString();
            string nextCostText = currentShop != null && currentShop.Data != null
                ? Mathf.Max(0, currentShop.Data.baseUpgradeCost + (currentShop.Level - 1) * GetUpgradeCostStep() - currentShop.UpgradeDiscount).ToString()
                : GetCurrentAcquireCost().ToString();

            return
                $"{ownerState}  Lv.{levelText}\n" +
                $"{category}  Profit {profitText}  Rent {rentText}\n" +
                $"Stay {stayText}s  Attract {attractionText}\n" +
                $"{(IsOwned ? "Next" : "Buy")} {nextCostText}" +
                (isUnderConstruction ? "\nRebuilding" : string.Empty);
        }

        public void Acquire(PlayerData owner, ShopData shopDataOverride = null)
        {
            ShopData selectedData = shopDataOverride != null ? shopDataOverride : originalShopData;
            currentShop = new ShopInstance(selectedData, owner);
            currentShop.ApplyInheritedMarketState(
                marketLevel,
                GetAutonomousIncomeBonus(),
                GetAutonomousAttractionBonus(),
                GetAutonomousStopBonus());
            RefreshColor();
            RefreshDebugLabel(GetDebugLabelText());
        }

        public int GetCurrentAcquireCost()
        {
            if (originalShopData == null)
            {
                return 0;
            }

            return Mathf.Max(0, originalShopData.baseAcquireCost + (GetEffectiveLevel() - 1) * GetWildAcquireCostBonusPerLevel());
        }

        public int GetCurrentRent()
        {
            ShopData targetData = currentShop != null ? currentShop.Data : originalShopData;
            if (targetData == null)
            {
                return 0;
            }

            return Mathf.Max(0, targetData.baseRent + Mathf.Max(0, GetEffectiveLevel() - 1) * GetOwnedRentGrowthPerLevel());
        }

        public ShopGrowthProfile GetGrowthProfile()
        {
            if (originalShopData != null && originalShopData.growthProfile != null)
            {
                return originalShopData.growthProfile;
            }

            if (currentShop != null && currentShop.Data != null)
            {
                return currentShop.Data.growthProfile;
            }

            return null;
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
                tileRenderer = GetPlaceholderRenderer();
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
                case ShopCategory.Exotic:
                    return new Color(0.5f, 0.82f, 1f);
                case ShopCategory.Snack:
                    return new Color(1f, 0.72f, 0.36f);
                case ShopCategory.Chinese:
                    return new Color(0.9f, 0.34f, 0.28f);
                case ShopCategory.FastFood:
                    return new Color(1f, 0.9f, 0.35f);
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

            return $"Shop\n{shopName}\nWild Lv.{marketLevel}";
        }

        private void RegisterAutonomousCustomerStay()
        {
            if (marketLevel >= GetWildMaxLevel())
            {
                return;
            }

            autonomousCustomerStayCount++;
            int threshold = GetCustomersNeededForNextAutoUpgrade();
            if (autonomousCustomerStayCount < threshold)
            {
                return;
            }

            autonomousCustomerStayCount = 0;
            marketLevel = Mathf.Min(GetWildMaxLevel(), marketLevel + 1);
            RefreshDebugLabel(GetDebugLabelText());
            Debug.Log($"{name} auto-upgraded to market level {marketLevel} after customer traffic.");
        }

        private int GetCustomersNeededForNextAutoUpgrade()
        {
            return Mathf.Max(1, GetBaseCustomersNeededForAutoUpgrade() + Mathf.Max(0, marketLevel - 1) * GetExtraCustomersNeededPerLevel());
        }

        private int GetEffectiveLevel()
        {
            return currentShop != null ? currentShop.Level : marketLevel;
        }

        private int GetAutonomousIncomeBonus()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            int bonusPerLevel = profile != null ? profile.wildIncomeBonusPerLevel : 8;
            return Mathf.Max(0, marketLevel - 1) * bonusPerLevel;
        }

        private float GetAutonomousAttractionBonus()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            float bonusPerLevel = profile != null ? profile.wildAttractionBonusPerLevel : 0.25f;
            return Mathf.Max(0, marketLevel - 1) * bonusPerLevel;
        }

        private float GetAutonomousStopBonus()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            float bonusPerLevel = profile != null ? profile.wildStopDurationBonusPerLevel : 0.15f;
            return Mathf.Max(0, marketLevel - 1) * bonusPerLevel;
        }

        private int GetDisplayedProfitBase(ShopData targetData)
        {
            if (targetData == null)
            {
                return 0;
            }

            return currentShop != null
                ? currentShop.CalculateIncome(null)
                : targetData.baseCustomerProfit + Mathf.Max(0, GetEffectiveLevel() - 1) * GetOwnedCustomerProfitGrowthPerLevel() + GetAutonomousIncomeBonus();
        }

        private int GetUpgradeCostStep()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? profile.ownedUpgradeCostStepPerLevel : 25;
        }

        private int GetOwnedRentGrowthPerLevel()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? profile.ownedRentGrowthPerLevel : 10;
        }

        private int GetOwnedCustomerProfitGrowthPerLevel()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? profile.ownedCustomerProfitGrowthPerLevel : 10;
        }

        private float GetOwnedAttractionGrowthPerLevel()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? profile.ownedAttractionGrowthPerLevel : 0.15f;
        }

        private float GetOwnedStayDurationReductionPerLevel()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? profile.ownedStayDurationReductionPerLevel : 0.1f;
        }

        private int GetWildStartingLevel()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? Mathf.Max(1, profile.wildStartingLevel) : 1;
        }

        private int GetWildMaxLevel()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? Mathf.Max(1, profile.wildMaxLevel) : 5;
        }

        private int GetWildAcquireCostBonusPerLevel()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? Mathf.Max(0, profile.wildAcquireCostBonusPerLevel) : 40;
        }

        private int GetBaseCustomersNeededForAutoUpgrade()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? Mathf.Max(1, profile.baseCustomersNeededForAutoUpgrade) : 3;
        }

        private int GetExtraCustomersNeededPerLevel()
        {
            ShopGrowthProfile profile = GetGrowthProfile();
            return profile != null ? Mathf.Max(0, profile.extraCustomersNeededPerLevel) : 1;
        }
    }
}
