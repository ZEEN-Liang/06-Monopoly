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
        public int PreferredIncomeBonus;
        public int UpgradeDiscount;
        public float CustomerStopFlatBonus;
        public float CustomerStopMultiplierBonus;

        public ShopInstance(ShopData data, PlayerData owner)
        {
            Data = data;
            Owner = owner;
            Level = 1;
            IsOwned = true;
            CurrentBranch = ShopBranchType.Default;
            FlatIncomeBonus = 0;
            PreferredIncomeBonus = 10;
            UpgradeDiscount = 0;
            CustomerStopFlatBonus = 0f;
            CustomerStopMultiplierBonus = 0f;
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
                    PreferredIncomeBonus += 15;
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
                    PreferredIncomeBonus += option.primaryValue;
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
            Level = 1;
            CurrentBranch = ShopBranchType.Default;
            FlatIncomeBonus = 0;
            PreferredIncomeBonus = 10;
            UpgradeDiscount = 0;
            CustomerStopFlatBonus = 0f;
            CustomerStopMultiplierBonus = 0f;
        }

        public int CalculateIncome(CustomerData customerData)
        {
            int income = Data != null ? Data.baseIncome : 0;
            income += (Level - 1) * 10;
            income += FlatIncomeBonus;

            if (customerData != null && Data != null && customerData.preferredCategories.Contains(Data.category))
            {
                income += PreferredIncomeBonus;
            }

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

            float dataFlatModifier = Data != null ? Data.customerStopFlatModifier : 0f;
            float dataMultiplier = Data != null ? Data.customerStopMultiplier : 1f;
            float runtimeMultiplier = 1f + CustomerStopMultiplierBonus;

            float duration = (baseDuration + dataFlatModifier + CustomerStopFlatBonus) * dataMultiplier * runtimeMultiplier;
            return UnityEngine.Mathf.Max(minDuration, duration);
        }
    }
}
