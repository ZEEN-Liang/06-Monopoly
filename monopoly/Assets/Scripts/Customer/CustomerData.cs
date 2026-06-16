using System.Collections.Generic;
using Monopoly.Utils;
using UnityEngine;

namespace Monopoly.Customer
{
    [CreateAssetMenu(menuName = "Monopoly/Customer Data")]
    public class CustomerData : ScriptableObject
    {
        public string customerName;
        public CustomerType customerType;
        public int minMoveStep = 1;
        public int maxMoveStep = 3;
        public int baseSpend = 20;
        [Header("Stay")]
        public float baseStopDuration = 2.2f;
        public float stopDurationVariance = 0.35f;
        public float minStopDuration = 0.8f;
        public List<ShopCategory> preferredCategories = new List<ShopCategory>();
    }
}
