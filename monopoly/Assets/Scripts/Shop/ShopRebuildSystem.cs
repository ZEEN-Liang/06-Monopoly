using Monopoly.Board;
using Monopoly.Player;
using UnityEngine;

namespace Monopoly.Shop
{
    public class ShopRebuildSystem : MonoBehaviour
    {
        [SerializeField] private int rebuildSatisfactionPenalty = 10;

        public bool TryStartRebuild(PlayerData playerData, ShopTile shopTile, ShopData rebuildTarget)
        {
            if (playerData == null || shopTile == null || shopTile.CurrentShop == null || rebuildTarget == null)
            {
                return false;
            }

            if (!playerData.OwnedShops.Contains(shopTile) || !playerData.SpendMoney(rebuildTarget.rebuildCost))
            {
                return false;
            }

            playerData.ChangeSatisfaction(-rebuildSatisfactionPenalty);
            shopTile.StartRebuild();
            shopTile.FinishRebuild(rebuildTarget);
            return true;
        }
    }
}
