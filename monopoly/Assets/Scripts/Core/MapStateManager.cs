using System.Collections.Generic;
using UnityEngine;

namespace Monopoly.Core
{
    public class MapStateManager : MonoBehaviour
    {
        private readonly Dictionary<string, bool> unlockedBranches = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> areaValues = new Dictionary<string, int>();

        public void SetBranchState(string branchId, bool unlocked)
        {
            unlockedBranches[branchId] = unlocked;
        }

        public bool IsBranchUnlocked(string branchId)
        {
            return unlockedBranches.TryGetValue(branchId, out bool unlocked) && unlocked;
        }

        public void SetAreaValue(string areaId, int value)
        {
            areaValues[areaId] = value;
        }

        public int GetAreaValue(string areaId)
        {
            return areaValues.TryGetValue(areaId, out int value) ? value : 0;
        }
    }
}
