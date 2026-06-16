using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Monopoly.Player
{
    public class PlayerUpgradeSystem : MonoBehaviour
    {
        [Header("Assign player upgrade assets here")]
        [SerializeField] private List<PlayerUpgradeOptionData> globalUpgradePool = new List<PlayerUpgradeOptionData>();
        [Header("Rarity Unlock By Total Dice Rolls")]
        [SerializeField] private int uncommonUnlockRollCount = 2;
        [SerializeField] private int rareUnlockRollCount = 5;
        [SerializeField] private int epicUnlockRollCount = 8;
        [SerializeField] private int legendaryUnlockRollCount = 12;

        private void Awake()
        {
            EnsureFallbackPool();
        }

        public List<PlayerUpgradeOptionData> GetRandomOptions(PlayerData playerData, int count = 3)
        {
            EnsureFallbackPool();

            int totalRollCount = playerData != null ? playerData.TotalDiceRollCount : 0;
            PlayerUpgradeTier unlockedTier = GetUnlockedTier(totalRollCount);

            List<PlayerUpgradeOptionData> eligibleOptions = globalUpgradePool
                .Where(option => option != null && IsOptionEligible(option, totalRollCount, unlockedTier))
                .ToList();

            if (eligibleOptions.Count == 0)
            {
                eligibleOptions = globalUpgradePool.Where(option => option != null).ToList();
            }

            int resultCount = Mathf.Min(count, eligibleOptions.Count);
            List<PlayerUpgradeOptionData> result = new List<PlayerUpgradeOptionData>();
            for (int i = 0; i < resultCount; i++)
            {
                PlayerUpgradeOptionData selectedOption = PickWeightedOption(eligibleOptions, totalRollCount);
                if (selectedOption == null)
                {
                    break;
                }

                result.Add(selectedOption);
                eligibleOptions.Remove(selectedOption);
            }

            return result;
        }

        public void ApplyOption(PlayerData playerData, PlayerUpgradeOptionData option)
        {
            if (playerData == null || option == null)
            {
                return;
            }

            switch (option.effectType)
            {
                case PlayerUpgradeEffectType.MoneyGain:
                    playerData.AddMoney(option.primaryValue);
                    break;
                case PlayerUpgradeEffectType.SatisfactionGain:
                    playerData.ChangeSatisfaction(option.primaryValue);
                    break;
                case PlayerUpgradeEffectType.GlobalShopIncomeBonus:
                    playerData.AddGlobalShopIncomeBonus(option.primaryValue);
                    break;
                case PlayerUpgradeEffectType.GlobalAcquireDiscount:
                    playerData.AddGlobalAcquireDiscount(option.primaryValue);
                    break;
                case PlayerUpgradeEffectType.GlobalPreferredIncomeBonus:
                    playerData.AddGlobalPreferredIncomeBonus(option.primaryValue);
                    break;
            }
        }

        public PlayerUpgradeTier GetUnlockedTier(int totalRollCount)
        {
            if (totalRollCount >= legendaryUnlockRollCount)
            {
                return PlayerUpgradeTier.Legendary;
            }

            if (totalRollCount >= epicUnlockRollCount)
            {
                return PlayerUpgradeTier.Epic;
            }

            if (totalRollCount >= rareUnlockRollCount)
            {
                return PlayerUpgradeTier.Rare;
            }

            if (totalRollCount >= uncommonUnlockRollCount)
            {
                return PlayerUpgradeTier.Uncommon;
            }

            return PlayerUpgradeTier.Common;
        }

        private void EnsureFallbackPool()
        {
            if (globalUpgradePool.Count > 0)
            {
                return;
            }

            PlayerUpgradeOptionData[] loadedAssets = Resources.LoadAll<PlayerUpgradeOptionData>("PlayerUpgrades");
            if (loadedAssets != null && loadedAssets.Length > 0)
            {
                globalUpgradePool.AddRange(loadedAssets.Where(option => option != null));
                Debug.Log($"PlayerUpgradeSystem loaded {globalUpgradePool.Count} player upgrade assets from Resources/PlayerUpgrades.");
                return;
            }

            globalUpgradePool.Add(CreateFallbackOption("capital_injection", "Capital Injection", "+120 money immediately", PlayerUpgradeTier.Common, PlayerUpgradeEffectType.MoneyGain, 120, 0, 0, 999, 16, 0));
            globalUpgradePool.Add(CreateFallbackOption("street_promo", "Street Promotion", "+10 satisfaction", PlayerUpgradeTier.Common, PlayerUpgradeEffectType.SatisfactionGain, 10, 0, 0, 999, 14, 0));
            globalUpgradePool.Add(CreateFallbackOption("service_training", "Service Training", "+5 income for all owned shops", PlayerUpgradeTier.Uncommon, PlayerUpgradeEffectType.GlobalShopIncomeBonus, 5, 0, 2, 999, 10, 1));
            globalUpgradePool.Add(CreateFallbackOption("franchise_coupon", "Franchise Coupon", "Future shop purchases cost 20 less", PlayerUpgradeTier.Rare, PlayerUpgradeEffectType.GlobalAcquireDiscount, 20, 0, 5, 999, 7, 1));
            globalUpgradePool.Add(CreateFallbackOption("taste_festival", "Taste Festival", "+8 preferred-customer income globally", PlayerUpgradeTier.Epic, PlayerUpgradeEffectType.GlobalPreferredIncomeBonus, 8, 0, 8, 999, 4, 1));
        }

        private PlayerUpgradeOptionData CreateFallbackOption(
            string id,
            string title,
            string description,
            PlayerUpgradeTier tier,
            PlayerUpgradeEffectType effectType,
            int primaryValue,
            int secondaryValue,
            int minRollCount,
            int maxRollCount,
            int baseWeight,
            int extraWeightPerRollWindow)
        {
            PlayerUpgradeOptionData option = ScriptableObject.CreateInstance<PlayerUpgradeOptionData>();
            option.hideFlags = HideFlags.DontSave;
            option.optionId = id;
            option.title = title;
            option.description = description;
            option.tier = tier;
            option.effectType = effectType;
            option.minRollCount = minRollCount;
            option.maxRollCount = maxRollCount;
            option.baseWeight = baseWeight;
            option.extraWeightPerRollWindow = extraWeightPerRollWindow;
            option.primaryValue = primaryValue;
            option.secondaryValue = secondaryValue;
            return option;
        }

        private bool IsOptionEligible(PlayerUpgradeOptionData option, int totalRollCount, PlayerUpgradeTier unlockedTier)
        {
            return option != null &&
                   option.tier <= unlockedTier &&
                   totalRollCount >= option.minRollCount &&
                   totalRollCount <= option.maxRollCount;
        }

        private PlayerUpgradeOptionData PickWeightedOption(List<PlayerUpgradeOptionData> options, int totalRollCount)
        {
            if (options == null || options.Count == 0)
            {
                return null;
            }

            int totalWeight = 0;
            List<int> weights = new List<int>(options.Count);

            for (int i = 0; i < options.Count; i++)
            {
                int weight = CalculateOptionWeight(options[i], totalRollCount);
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

        private int CalculateOptionWeight(PlayerUpgradeOptionData option, int totalRollCount)
        {
            if (option == null)
            {
                return 0;
            }

            int rollsPerStep = Mathf.Max(1, option.rollsPerWeightStep);
            int growthSteps = Mathf.Max(0, totalRollCount - option.minRollCount) / rollsPerStep;
            return Mathf.Max(0, option.baseWeight + growthSteps * option.extraWeightPerRollWindow);
        }
    }
}
