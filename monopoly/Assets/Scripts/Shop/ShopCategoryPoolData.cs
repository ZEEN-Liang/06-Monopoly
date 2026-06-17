using System.Collections.Generic;
using Monopoly.Utils;
using UnityEngine;

namespace Monopoly.Shop
{
    [System.Serializable]
    public class ShopCategoryPoolEntry
    {
        public ShopCategory category;
        public int weight = 1;
        public bool enabled = true;
        public ShopPoolData shopPool;
    }

    [CreateAssetMenu(menuName = "Monopoly/Shop Category Pool")]
    public class ShopCategoryPoolData : ScriptableObject
    {
        public List<ShopCategoryPoolEntry> entries = new List<ShopCategoryPoolEntry>();

        public ShopData GetRandomShopData()
        {
            ShopCategoryPoolEntry categoryEntry = GetRandomCategoryEntry();
            if (categoryEntry == null || categoryEntry.shopPool == null)
            {
                return null;
            }

            return categoryEntry.shopPool.GetRandomShopData();
        }

        private ShopCategoryPoolEntry GetRandomCategoryEntry()
        {
            int totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                ShopCategoryPoolEntry entry = entries[i];
                if (entry == null || !entry.enabled || entry.shopPool == null)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0, entry.weight);
            }

            if (totalWeight <= 0)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null && entries[i].enabled && entries[i].shopPool != null)
                    {
                        return entries[i];
                    }
                }

                return null;
            }

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                ShopCategoryPoolEntry entry = entries[i];
                if (entry == null || !entry.enabled || entry.shopPool == null)
                {
                    continue;
                }

                cumulative += Mathf.Max(0, entry.weight);
                if (roll < cumulative)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
