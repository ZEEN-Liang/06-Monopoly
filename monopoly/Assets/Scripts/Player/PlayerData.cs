using System.Collections.Generic;
using Monopoly.Board;
using UnityEngine;

namespace Monopoly.Player
{
    public class PlayerData : MonoBehaviour
    {
        [SerializeField] private int startingMoney = 500;
        [SerializeField] private int startingSatisfaction = 100;
        [Header("Debt")]
        [SerializeField] private bool allowPropertyDebt = true;
        [SerializeField] private float debtRepayDurationSeconds = 90f;
        [SerializeField] private bool enableDebtInterest = true;
        [SerializeField] private float debtInterestIntervalSeconds = 15f;
        [SerializeField] private float debtInterestRate = 0.1f;
        [SerializeField] private int minimumDebtInterest = 1;

        [field: SerializeField] public int Money { get; private set; }
        [field: SerializeField] public int Satisfaction { get; private set; }
        [field: SerializeField] public int GlobalShopIncomeBonus { get; private set; }
        [field: SerializeField] public int GlobalAcquireDiscount { get; private set; }
        [field: SerializeField] public int TotalDiceRollCount { get; private set; }
        [field: SerializeField] public int TotalDiceStepSum { get; private set; }
        [field: SerializeField] public int CurrentDebtAmount { get; private set; }
        [field: SerializeField] public float DebtRemainingSeconds { get; private set; }
        [field: SerializeField] public float DebtInterestProgressSeconds { get; private set; }

        public List<ShopTile> OwnedShops { get; } = new List<ShopTile>();
        public int OwnedShopCount => OwnedShops.Count;
        public bool HasDebt => CurrentDebtAmount > 0;
        public bool AllowPropertyDebt => allowPropertyDebt;
        public float DebtRepayDurationSeconds => debtRepayDurationSeconds;
        public bool IsDebtOverdue => HasDebt && DebtRemainingSeconds <= 0f;
        public bool EnableDebtInterest => enableDebtInterest;
        public float DebtInterestIntervalSeconds => debtInterestIntervalSeconds;
        public float DebtInterestRate => debtInterestRate;

        private void Awake()
        {
            Money = startingMoney;
            Satisfaction = startingSatisfaction;
            GlobalShopIncomeBonus = 0;
            GlobalAcquireDiscount = 0;
            TotalDiceRollCount = 0;
            TotalDiceStepSum = 0;
            CurrentDebtAmount = 0;
            DebtRemainingSeconds = 0f;
            DebtInterestProgressSeconds = 0f;
        }

        public bool CanAfford(int cost)
        {
            return Money >= cost;
        }

        public void AddMoney(int value)
        {
            if (value <= 0)
            {
                return;
            }

            if (CurrentDebtAmount > 0)
            {
                int repaidDebt = Mathf.Min(CurrentDebtAmount, value);
                CurrentDebtAmount -= repaidDebt;
                value -= repaidDebt;
            }

            if (value > 0)
            {
                Money += value;
            }

            if (CurrentDebtAmount <= 0)
            {
                CurrentDebtAmount = 0;
                DebtRemainingSeconds = 0f;
                DebtInterestProgressSeconds = 0f;
            }
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

        public bool TrySpendMoneyWithDebt(int value)
        {
            if (value <= 0)
            {
                return true;
            }

            if (SpendMoney(value))
            {
                return true;
            }

            if (!allowPropertyDebt)
            {
                return false;
            }

            int remainingCost = value;
            if (Money > 0)
            {
                int spentCash = Mathf.Min(Money, remainingCost);
                Money -= spentCash;
                remainingCost -= spentCash;
            }

            if (remainingCost > 0)
            {
                CurrentDebtAmount += remainingCost;
            }

            DebtRemainingSeconds = debtRepayDurationSeconds;
            return true;
        }

        public void TickDebtTimer(float deltaTime)
        {
            if (!HasDebt)
            {
                DebtRemainingSeconds = 0f;
                DebtInterestProgressSeconds = 0f;
                return;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            DebtRemainingSeconds = Mathf.Max(0f, DebtRemainingSeconds - safeDeltaTime);

            if (!enableDebtInterest || debtInterestIntervalSeconds <= 0f)
            {
                return;
            }

            DebtInterestProgressSeconds += safeDeltaTime;
            while (DebtInterestProgressSeconds >= debtInterestIntervalSeconds)
            {
                DebtInterestProgressSeconds -= debtInterestIntervalSeconds;
                ApplyDebtInterestTick();
            }
        }

        private void ApplyDebtInterestTick()
        {
            if (!HasDebt)
            {
                return;
            }

            int interest = Mathf.Max(minimumDebtInterest, Mathf.CeilToInt(CurrentDebtAmount * debtInterestRate));
            CurrentDebtAmount += Mathf.Max(0, interest);
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

        public void RecordDiceRoll(int steps)
        {
            TotalDiceRollCount++;
            TotalDiceStepSum += Mathf.Max(0, steps);
        }
    }
}
