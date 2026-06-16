using Monopoly.Utils;
using UnityEngine;

namespace Monopoly.Shop
{
    [CreateAssetMenu(menuName = "Monopoly/Shop Data")]
    public class ShopData : ScriptableObject
    {
        public string shopId;
        public string shopName;
        public ShopCategory category;
        public ShopRole role;
        public int acquireCost = 100;
        public int rebuildCost = 150;
        public int baseIncome = 30;
        public int baseUpgradeCost = 50;
        [Header("Customer Stay")]
        public float customerStopFlatModifier = 0.8f;
        public float customerStopMultiplier = 1f;
    }
}
