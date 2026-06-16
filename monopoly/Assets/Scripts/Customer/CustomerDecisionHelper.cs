using Monopoly.Shop;
using UnityEngine;

namespace Monopoly.Customer
{
    public class CustomerDecisionHelper : MonoBehaviour
    {
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

            int spend = customerData.baseSpend + shop.Data.baseIncome;
            if (customerData.preferredCategories.Contains(shop.Data.category))
            {
                spend += 10;
            }

            return spend;
        }
    }
}
