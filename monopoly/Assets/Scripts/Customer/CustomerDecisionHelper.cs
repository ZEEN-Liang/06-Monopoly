using Monopoly.Board;
using Monopoly.Shop;
using UnityEngine;

namespace Monopoly.Customer
{
    public class CustomerDecisionHelper : MonoBehaviour
    {
        [Header("Attraction")]
        [SerializeField] private float minimumAttractionChance = 0f;
        [SerializeField] private float maximumAttractionChance = 100f;

        public int RollMoveStep(CustomerData data)
        {
            if (data == null)
            {
                return 1;
            }

            return Random.Range(data.minMoveStep, data.maxMoveStep + 1);
        }

        public int CalculateSpend(CustomerData customerData, ShopInstance shop)
        {
            if (customerData == null || shop == null || shop.Data == null)
            {
                return 0;
            }

            int spend = customerData.baseSpend + shop.Data.baseCustomerProfit;
            if (customerData.preferredCategories.Contains(shop.Data.category))
            {
                spend += 10;
            }

            return spend;
        }

        public bool TryAttractToShop(CustomerData customerData, ShopTile shopTile)
        {
            if (customerData == null || shopTile == null)
            {
                return false;
            }

            float shopAttractionChance = shopTile.GetAttractionScore(null);
            if (shopAttractionChance <= 0f)
            {
                return false;
            }

            ShopData targetData = shopTile.CurrentShop != null ? shopTile.CurrentShop.Data : shopTile.OriginalShopData;
            float customerAttractionChance = customerData.attractionSensitivity;
            if (targetData != null && customerData.preferredCategories.Contains(targetData.category))
            {
                customerAttractionChance += customerData.preferredAttractionBonus;
            }

            float chance = Mathf.Clamp(
                shopAttractionChance + customerAttractionChance,
                minimumAttractionChance,
                maximumAttractionChance);

            return Random.Range(0f, 100f) < chance;
        }
    }
}
