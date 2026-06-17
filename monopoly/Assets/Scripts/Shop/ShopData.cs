using Monopoly.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Monopoly.Shop
{
    [CreateAssetMenu(menuName = "Monopoly/Shop Data")]
    public class ShopData : ScriptableObject
    {
        public string shopId;
        public string shopName;
        public ShopCategory category;
        public ShopRole role;
        [Header("Core Economy")]
        [FormerlySerializedAs("acquireCost")]
        public int baseAcquireCost = 100;
        public int rebuildCost = 150;
        public int baseUpgradeCost = 50;
        public int baseRent = 15;
        [FormerlySerializedAs("baseIncome")]
        public int baseCustomerProfit = 30;
        public ShopGrowthProfile growthProfile;
        [Header("Visual")]
        public GameObject shopModelPrefab;
        public Vector3 shopModelScale = Vector3.one;
        public Vector3 shopModelLocalOffset = Vector3.zero;
        [Header("Attraction")]
        [FormerlySerializedAs("baseAttraction")]
        public float baseAttractionRate = 0f;
        [Header("Customer Stay")]
        [FormerlySerializedAs("customerStopFlatModifier")]
        public float baseCustomerStayDuration = 0.8f;
        [FormerlySerializedAs("customerStopMultiplier")]
        public float customerStayDurationMultiplier = 1f;
    }
}
