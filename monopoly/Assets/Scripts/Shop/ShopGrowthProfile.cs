using UnityEngine;

namespace Monopoly.Shop
{
    [CreateAssetMenu(menuName = "Monopoly/Shop Growth Profile")]
    public class ShopGrowthProfile : ScriptableObject
    {
        [Header("Owned Shop Growth")]
        public int ownedStartingLevel = 1;
        public int ownedRentGrowthPerLevel = 10;
        public int ownedCustomerProfitGrowthPerLevel = 10;
        public float ownedAttractionGrowthPerLevel = 0.15f;
        public float ownedStayDurationReductionPerLevel = 0.1f;
        public int ownedUpgradeCostStepPerLevel = 25;

        [Header("Wild Shop Growth")]
        public int wildStartingLevel = 1;
        public int wildMaxLevel = 5;
        public int baseCustomersNeededForAutoUpgrade = 3;
        public int extraCustomersNeededPerLevel = 1;
        public int wildIncomeBonusPerLevel = 8;
        public int wildAcquireCostBonusPerLevel = 40;
        public float wildAttractionBonusPerLevel = 0.25f;
        public float wildStopDurationBonusPerLevel = 0.15f;
    }
}
