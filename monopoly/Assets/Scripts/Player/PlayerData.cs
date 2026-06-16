using System.Collections.Generic;
using Monopoly.Board;
using UnityEngine;

namespace Monopoly.Player
{
    public class PlayerData : MonoBehaviour
    {
        [SerializeField] private int startingMoney = 500;
        [SerializeField] private int startingSatisfaction = 100;

        [field: SerializeField] public int Money { get; private set; }
        [field: SerializeField] public int Satisfaction { get; private set; }
        [field: SerializeField] public int GlobalShopIncomeBonus { get; private set; }
        [field: SerializeField] public int GlobalAcquireDiscount { get; private set; }
        [field: SerializeField] public int GlobalPreferredIncomeBonus { get; private set; }
        [field: SerializeField] public int TotalDiceRollCount { get; private set; }
        [field: SerializeField] public int TotalDiceStepSum { get; private set; }

        public List<ShopTile> OwnedShops { get; } = new List<ShopTile>();
        public int OwnedShopCount => OwnedShops.Count;

        private void Awake()
        {
            Money = startingMoney;
            Satisfaction = startingSatisfaction;
            GlobalShopIncomeBonus = 0;
            GlobalAcquireDiscount = 0;
            GlobalPreferredIncomeBonus = 0;
            TotalDiceRollCount = 0;
            TotalDiceStepSum = 0;
        }

        public bool CanAfford(int cost)
        {
            return Money >= cost;
        }

        public void AddMoney(int value)
        {
            Money += value;
        }

        public bool SpendMoney(int value)
        {
            if (!CanAfford(value))
            {
                return false;
            }

            Money -= value;
            return true;
        }

        public void ChangeSatisfaction(int delta)
        {
            Satisfaction += delta;
        }

        public void AddOwnedShop(ShopTile shopTile)
        {
            if (shopTile != null && !OwnedShops.Contains(shopTile))
            {
                OwnedShops.Add(shopTile);
            }
        }

        public void AddGlobalShopIncomeBonus(int value)
        {
            GlobalShopIncomeBonus += value;
        }

        public void AddGlobalAcquireDiscount(int value)
        {
            GlobalAcquireDiscount += value;
        }

        public void AddGlobalPreferredIncomeBonus(int value)
        {
            GlobalPreferredIncomeBonus += value;
        }

        public void RecordDiceRoll(int steps)
        {
            TotalDiceRollCount++;
            TotalDiceStepSum += Mathf.Max(0, steps);
        }
    }
}
