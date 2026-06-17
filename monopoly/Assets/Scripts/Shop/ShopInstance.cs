using Monopoly.Customer;
using Monopoly.Player;
using Monopoly.Utils;

namespace Monopoly.Shop
{
    [System.Serializable]
    public class ShopInstance
    {
        public ShopData Data;
        public PlayerData Owner;
        public int Level;
        public bool IsOwned;
        public ShopBranchType CurrentBranch;
        public int FlatIncomeBonus;
        public int UpgradeDiscount;
        public float AttractionFlatBonus;
        public float AttractionMultiplierBonus;
        public float CustomerStopFlatBonus;
        public float CustomerStopMultiplierBonus;

        public ShopInstance(ShopData data, PlayerData owner)
        {
            Data = data;
            Owner = owner;
            Level = data != null && data.growthProfile != null
                ? UnityEngine.Mathf.Max(1, data.growthProfile.ownedStartingLevel)
                : 1;
            IsOwned = true;
            CurrentBranch = ShopBranchType.Default;
            FlatIncomeBonus = 0;
            UpgradeDiscount = 0;
            AttractionFlatBonus = 0f;
            AttractionMultiplierBonus = 0f;
            CustomerStopFlatBonus = 0f;
            CustomerStopMultiplierBonus = 0f;
        }

        public void ApplyInheritedMarketState(
            int inheritedLevel,
            int inheritedIncomeBonus,
            float inheritedAttractionBonus,
            float inheritedStopBonus)
        {
            Level = UnityEngine.Mathf.Max(1, inheritedLevel);
            FlatIncomeBonus += inheritedIncomeBonus;
            AttractionFlatBonus += inheritedAttractionBonus;
            CustomerStopFlatBonus += inheritedStopBonus;
        }

        public void Upgrade()
        {
            Level++;
        }

        public void ClampLevel(int maxLevel)
        {
            Level = UnityEngine.Mathf.Min(Level, maxLevel);
        }

        public void ApplyUpgradeChoice(ShopBranchType branchType)
        {
            Level++;
            CurrentBranch = branchType;

            switch (branchType)
            {
                case ShopBranchType.Income:
                    FlatIncomeBonus += 15;
                    break;
                case ShopBranchType.Control:
                    AttractionFlatBonus += 6f;
                    break;
                case ShopBranchType.Support:
                    FlatIncomeBonus += 5;
                    UpgradeDiscount += 20;
                    break;
            }
        }

        public void ApplyUpgradeOption(ShopUpgradeOptionData option)
        {
            if (option == null)
            {
                return;
            }

            Level++;
            CurrentBranch = option.branchTag;

            switch (option.effectType)
            {
                case ShopUpgradeEffectType.FlatIncomeBoost:
                    FlatIncomeBonus += option.primaryValue;
                    break;
                case ShopUpgradeEffectType.PreferredIncomeBoost:
                    AttractionFlatBonus += option.primaryValue;
                    break;
                case ShopUpgradeEffectType.UpgradeDiscountBoost:
                    UpgradeDiscount += option.primaryValue;
                    break;
                case ShopUpgradeEffectType.MixedGrowth:
                    FlatIncomeBonus += option.primaryValue;
                    UpgradeDiscount += option.secondaryValue;
                    break;
                case ShopUpgradeEffectType.DoubleLevelGain:
                    FlatIncomeBonus += option.primaryValue;
                    Level += option.secondaryValue;
                    break;
            }
        }

        public void ReplaceData(ShopData newData)
        {
            Data = newData;
            Level = newData != null && newData.growthProfile != null
                ? UnityEngine.Mathf.Max(1, newData.growthProfile.ownedStartingLevel)
                : 1;
            CurrentBranch = ShopBranchType.Default;
            FlatIncomeBonus = 0;
            UpgradeDiscount = 0;
            AttractionFlatBonus = 0f;
            AttractionMultiplierBonus = 0f;
            CustomerStopFlatBonus = 0f;
            CustomerStopMultiplierBonus = 0f;
        }

        public int CalculateIncome(CustomerData customerData)
        {
            int income = Data != null ? Data.baseCustomerProfit : 0;
            int incomePerLevel = Data != null && Data.growthProfile != null
                ? Data.growthProfile.ownedCustomerProfitGrowthPerLevel
                : 10;
            income += (Level - 1) * incomePerLevel;
            income += FlatIncomeBonus;

            return income;
        }

        public float CalculateCustomerStopDuration(CustomerData customerData)
        {
            float baseDuration = customerData != null ? customerData.baseStopDuration : 2.2f;
            float variance = customerData != null ? customerData.stopDurationVariance : 0f;
            float minDuration = customerData != null ? customerData.minStopDuration : 0.8f;

            if (variance > 0f)
            {
                baseDuration += UnityEngine.Random.Range(-variance, variance);
            }

            float shopBaseDuration = Data != null ? Data.baseCustomerStayDuration : 0.8f;
            float shopReduction = Data != null && Data.growthProfile != null
                ? UnityEngine.Mathf.Max(0f, Level - 1) * Data.growthProfile.ownedStayDurationReductionPerLevel
                : 0f;
            float dataMultiplier = Data != null ? Data.customerStayDurationMultiplier : 1f;
            float runtimeMultiplier = 1f + CustomerStopMultiplierBonus;

            float duration = (baseDuration + shopBaseDuration - shopReduction + CustomerStopFlatBonus) * dataMultiplier * runtimeMultiplier;
            return UnityEngine.Mathf.Max(minDuration, duration);
        }

        public float CalculateAttractionScore(CustomerData customerData)
        {
            float attraction = Data != null
                ? Data.baseAttractionRate + UnityEngine.Mathf.Max(0, Level - 1) * GetOwnedAttractionGrowthPerLevel()
                : 0f;
            attraction += AttractionFlatBonus;
            attraction *= 1f + AttractionMultiplierBonus;

            return UnityEngine.Mathf.Max(0f, attraction);
        }

        private float GetOwnedAttractionGrowthPerLevel()
        {
            return Data != null && Data.growthProfile != null
                ? Data.growthProfile.ownedAttractionGrowthPerLevel
                : 0.15f;
        }
    }
}
