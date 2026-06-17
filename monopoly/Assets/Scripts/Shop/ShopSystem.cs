using Monopoly.Board;
using Monopoly.Player;
using UnityEngine;

namespace Monopoly.Shop
{
    public class ShopSystem : MonoBehaviour
    {
        public bool TryAcquireShop(PlayerData playerData, ShopTile shopTile, ShopData selectedData = null)
        {
            if (playerData == null || shopTile == null || shopTile.IsOwned)
            {
                return false;
            }

            ShopData targetData = selectedData != null ? selectedData : shopTile.OriginalShopData;
            int finalCost = targetData != null ? Mathf.Max(0, shopTile.GetCurrentAcquireCost() - playerData.GlobalAcquireDiscount) : 0;
            if (targetData == null || !playerData.TrySpendMoneyWithDebt(finalCost))
            {
                return false;
            }

            shopTile.Acquire(playerData, targetData);
            playerData.AddOwnedShop(shopTile);
            return true;
        }
    }
}
