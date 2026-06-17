using System.Collections.Generic;
using Monopoly.Utils;
using UnityEngine;

namespace Monopoly.Shop
{
    [System.Serializable]
    public class ShopPoolEntry
    {
        public ShopData shopData;
        public int weight = 1;
        public bool enabled = true;
    }

    [CreateAssetMenu(menuName = "Monopoly/Shop Pool")]
    public class ShopPoolData : ScriptableObject
    {
        public List<ShopPoolEntry> entries = new List<ShopPoolEntry>();

        public ShopData GetRandomShopData()
        {
            int totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                ShopPoolEntry entry = entries[i];
                if (entry == null || !entry.enabled || entry.shopData == null)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0, entry.weight);
            }

            if (totalWeight <= 0)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null && entries[i].enabled && entries[i].shopData != null)
                    {
                        return entries[i].shopData;
                    }
                }

                return null;
            }

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                ShopPoolEntry entry = entries[i];
                if (entry == null || !entry.enabled || entry.shopData == null)
                {
                    continue;
                }

                cumulative += Mathf.Max(0, entry.weight);
                if (roll < cumulative)
                {
                    return entry.shopData;
                }
            }

            return null;
        }
    }
}
