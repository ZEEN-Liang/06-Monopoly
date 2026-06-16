using UnityEngine;
using Monopoly.Utils;

namespace Monopoly.Shop
{
    [CreateAssetMenu(menuName = "Monopoly/Shop Upgrade Option", fileName = "ShopUpgradeOption_")]
    public class ShopUpgradeOptionData : ScriptableObject
    {
        public string optionId;
        public string title;
        public string description;
        public ShopUpgradeTier tier = ShopUpgradeTier.Common;
        public ShopUpgradeEffectType effectType;
        public ShopBranchType branchTag = ShopBranchType.Special;
        public int minShopLevel = 1;
        public int maxShopLevel = 99;
        public int baseWeight = 10;
        public int extraWeightPerLevel = 0;
        public int primaryValue;
        public int secondaryValue;
    }
}
