using Monopoly.Board;
using Monopoly.Player;
using Monopoly.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Monopoly.Shop
{
    public class ShopUpgradeSystem : MonoBehaviour
    {
        [Header("Assign upgrade option assets here")]
        [SerializeField] private List<ShopUpgradeOptionData> upgradeEffectPool = new List<ShopUpgradeOptionData>();
        [Header("Stage Rules")]
        [SerializeField] private int currentShopLevelCap = 5;

        private void Awake()
        {
            EnsureDefaultUpgradePool();
        }

        public bool TryUpgradeShop(PlayerData playerData, ShopTile shopTile, ShopBranchType branchType = ShopBranchType.Default)
        {
            if (playerData == null || shopTile == null || shopTile.CurrentShop == null || !CanUpgrade(shopTile))
            {
                return false;
            }

            int cost = GetUpgradeCost(shopTile.CurrentShop);
            if (!playerData.SpendMoney(cost))
            {
                return false;
            }

            if (branchType == ShopBranchType.Default)
            {
                shopTile.CurrentShop.Upgrade();
            }
            else
            {
                shopTile.CurrentShop.ApplyUpgradeChoice(branchType);
            }

            shopTile.CurrentShop.ClampLevel(currentShopLevelCap);
            shopTile.RefreshVisualState();
            return true;
        }

        public bool TryUpgradeShop(PlayerData playerData, ShopTile shopTile, ShopUpgradeOptionData option)
        {
            if (playerData == null || shopTile == null || shopTile.CurrentShop == null || option == null || !CanUpgrade(shopTile))
            {
                return false;
            }

            int cost = GetUpgradeCost(shopTile.CurrentShop);
            if (!playerData.SpendMoney(cost))
            {
                return false;
            }

            shopTile.CurrentShop.ApplyUpgradeOption(option);
            shopTile.CurrentShop.ClampLevel(currentShopLevelCap);
            shopTile.RefreshVisualState();
            return true;
        }

        public bool CanUpgrade(ShopTile shopTile)
        {
            return shopTile != null &&
                   shopTile.CurrentShop != null &&
                   shopTile.CurrentShop.Level < currentShopLevelCap;
        }

        public int GetCurrentLevelCap()
        {
            return currentShopLevelCap;
        }

        public bool ShouldOfferSpecialUpgrade(ShopTile shopTile)
        {
            if (shopTile == null || shopTile.CurrentShop == null || !CanUpgrade(shopTile))
            {
                return false;
            }

            int targetLevel = shopTile.CurrentShop.Level + 1;
            return IsSpecialUpgradeLevel(targetLevel);
        }

        public List<ShopUpgradeOptionData> GetRandomUpgradeOptions(ShopTile shopTile, int optionCount = 3)
        {
            EnsureDefaultUpgradePool();

            int shopLevel = shopTile != null && shopTile.CurrentShop != null ? shopTile.CurrentShop.Level : 1;
            List<ShopUpgradeOptionData> eligibleOptions = upgradeEffectPool
                .Where(option => option != null && IsOptionEligibleForLevel(option, shopLevel))
                .ToList();

            if (eligibleOptions.Count == 0)
            {
                eligibleOptions = new List<ShopUpgradeOptionData>(upgradeEffectPool);
            }

            int resultCount = Mathf.Min(optionCount, eligibleOptions.Count);
            List<ShopUpgradeOptionData> result = new List<ShopUpgradeOptionData>();
            for (int i = 0; i < resultCount; i++)
            {
                ShopUpgradeOptionData selectedOption = PickWeightedOption(eligibleOptions, shopLevel);
                if (selectedOption == null)
                {
                    break;
                }

                result.Add(selectedOption);
                eligibleOptions.Remove(selectedOption);
            }

            return result;
        }

        public int GetUpgradeCost(ShopInstance shop)
        {
            if (shop == null || shop.Data == null)
            {
                return 0;
            }

            int rawCost = shop.Data.baseUpgradeCost + (shop.Level - 1) * 25;
            if (shop.Data != null && shop.Data.growthProfile != null)
            {
                rawCost = shop.Data.baseUpgradeCost + (shop.Level - 1) * shop.Data.growthProfile.ownedUpgradeCostStepPerLevel;
            }
            return Mathf.Max(0, rawCost - shop.UpgradeDiscount);
        }

        private void EnsureDefaultUpgradePool()
        {
            if (upgradeEffectPool.Count > 0)
            {
                return;
            }

            ShopUpgradeOptionData[] loadedAssets = Resources.LoadAll<ShopUpgradeOptionData>("ShopUpgrades");
            if (loadedAssets != null && loadedAssets.Length > 0)
            {
                upgradeEffectPool.AddRange(loadedAssets.Where(option => option != null));
                Debug.Log($"ShopUpgradeSystem loaded {upgradeEffectPool.Count} shop upgrade assets from Resources/ShopUpgrades.");
                return;
            }

            Debug.LogWarning("ShopUpgradeSystem has no upgrade option assets assigned. Using runtime fallback options. Create assets via Create -> Monopoly -> Shop Upgrade Option and assign them in the inspector.");

            upgradeEffectPool.Add(CreateFallbackOption(
                "income_boost_small",
                "Income Engine",
                "+15 base income each visit",
                ShopUpgradeTier.Common,
                ShopUpgradeEffectType.FlatIncomeBoost,
                ShopBranchType.Income,
                15,
                0,
                1,
                99,
                12,
                1));

            upgradeEffectPool.Add(CreateFallbackOption(
                "preferred_bonus",
                "Taste Match",
                "+15 bonus from preferred customers",
                ShopUpgradeTier.Common,
                ShopUpgradeEffectType.PreferredIncomeBoost,
                ShopBranchType.Control,
                15,
                0,
                1,
                99,
                12,
                1));

            upgradeEffectPool.Add(CreateFallbackOption(
                "discount_boost",
                "Chain Supply",
                "Future upgrades cost 20 less",
                ShopUpgradeTier.Rare,
                ShopUpgradeEffectType.UpgradeDiscountBoost,
                ShopBranchType.Support,
                20,
                0,
                2,
                99,
                8,
                1));

            upgradeEffectPool.Add(CreateFallbackOption(
                "mixed_growth",
                "Steady Expansion",
                "+8 base income, future upgrades cost 10 less",
                ShopUpgradeTier.Rare,
                ShopUpgradeEffectType.MixedGrowth,
                ShopBranchType.Support,
                8,
                10,
                2,
                99,
                7,
                1));

            upgradeEffectPool.Add(CreateFallbackOption(
                "power_spike",
                "Power Spike",
                "+12 base income and +1 extra level",
                ShopUpgradeTier.Epic,
                ShopUpgradeEffectType.DoubleLevelGain,
                ShopBranchType.Income,
                12,
                1,
                3,
                99,
                4,
                1));
        }

        private ShopUpgradeOptionData CreateFallbackOption(
            string optionId,
            string title,
            string description,
            ShopUpgradeTier tier,
            ShopUpgradeEffectType effectType,
            ShopBranchType branchTag,
            int primaryValue,
            int secondaryValue = 0,
            int minShopLevel = 1,
            int maxShopLevel = 99,
            int baseWeight = 10,
            int extraWeightPerLevel = 0)
        {
            ShopUpgradeOptionData option = ScriptableObject.CreateInstance<ShopUpgradeOptionData>();
            option.hideFlags = HideFlags.DontSave;
            option.optionId = optionId;
            option.title = title;
            option.description = description;
            option.tier = tier;
            option.effectType = effectType;
            option.branchTag = branchTag;
            option.minShopLevel = minShopLevel;
            option.maxShopLevel = maxShopLevel;
            option.baseWeight = baseWeight;
            option.extraWeightPerLevel = extraWeightPerLevel;
            option.primaryValue = primaryValue;
            option.secondaryValue = secondaryValue;
            return option;
        }

        private bool IsOptionEligibleForLevel(ShopUpgradeOptionData option, int shopLevel)
        {
            return option != null && shopLevel >= option.minShopLevel && shopLevel <= option.maxShopLevel;
        }

        private ShopUpgradeOptionData PickWeightedOption(List<ShopUpgradeOptionData> options, int shopLevel)
        {
            if (options == null || options.Count == 0)
            {
                return null;
            }

            int totalWeight = 0;
            List<int> weights = new List<int>(options.Count);

            for (int i = 0; i < options.Count; i++)
            {
                int weight = CalculateOptionWeight(options[i], shopLevel);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0)
            {
                return options[Random.Range(0, options.Count)];
            }

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < options.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                {
                    return options[i];
                }
            }

            return options[options.Count - 1];
        }

        private int CalculateOptionWeight(ShopUpgradeOptionData option, int shopLevel)
        {
            if (option == null)
            {
                return 0;
            }

            int levelDelta = Mathf.Max(0, shopLevel - option.minShopLevel);
            return Mathf.Max(0, option.baseWeight + levelDelta * option.extraWeightPerLevel);
        }

        private bool IsSpecialUpgradeLevel(int targetLevel)
        {
            if (targetLevel <= 0)
            {
                return false;
            }

            int interval = (targetLevel / 10) + 2;
            return targetLevel % interval == 0;
        }
    }
}
