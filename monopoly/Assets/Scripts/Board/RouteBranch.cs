using UnityEngine;

namespace Monopoly.Board
{
    public class RouteBranch : MonoBehaviour
    {
        [SerializeField] private string branchId;
        [SerializeField] private PathNode entryNode;
        [SerializeField] private PathNode exitNode;
        [SerializeField] private bool isUnlocked;

        public string BranchId => branchId;
        public PathNode EntryNode => entryNode;
        public PathNode ExitNode => exitNode;
        public bool IsUnlocked => isUnlocked;

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
        }
    }
}
