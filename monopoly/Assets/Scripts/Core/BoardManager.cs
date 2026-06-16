using System.Collections.Generic;
using Monopoly.Board;
using UnityEngine;

namespace Monopoly.Core
{
    public class BoardManager : MonoBehaviour
    {
        [SerializeField] private List<PathNode> mainPath = new List<PathNode>();
        [SerializeField] private List<RouteBranch> routeBranches = new List<RouteBranch>();
        [SerializeField] private List<BoardTile> allTiles = new List<BoardTile>();

        public PathNode GetNextNode(PathNode currentNode, int routeIndex = 0)
        {
            if (currentNode == null)
            {
                return mainPath.Count > 0 ? mainPath[0] : null;
            }

            return currentNode.GetNextNode(routeIndex);
        }

        public BoardTile GetTileByNode(PathNode node)
        {
            return node != null ? node.Tile : null;
        }

        public void Configure(List<PathNode> pathNodes, List<RouteBranch> branches, List<BoardTile> tiles)
        {
            mainPath = pathNodes ?? new List<PathNode>();
            routeBranches = branches ?? new List<RouteBranch>();
            allTiles = tiles ?? new List<BoardTile>();
        }

        public void UnlockBranch(string branchId)
        {
            RouteBranch branch = routeBranches.Find(item => item.BranchId == branchId);
            if (branch != null)
            {
                branch.SetUnlocked(true);
            }
        }
    }
}
