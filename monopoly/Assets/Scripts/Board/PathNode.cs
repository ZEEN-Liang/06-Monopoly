using System.Collections.Generic;
using UnityEngine;

namespace Monopoly.Board
{
    public class PathNode : MonoBehaviour
    {
        [SerializeField] private string nodeId;
        [SerializeField] private Transform standPoint;
        [SerializeField] private List<PathNode> nextNodes = new List<PathNode>();
        [SerializeField] private BoardTile tile;

        public string NodeId => nodeId;
        public BoardTile Tile => tile;
        public Transform StandPoint => standPoint != null ? standPoint : transform;
        public IReadOnlyList<PathNode> NextNodes => nextNodes;

        public void Configure(string id, Transform point, BoardTile bindTile)
        {
            nodeId = id;
            standPoint = point;
            tile = bindTile;
        }

        public void SetNextNodes(List<PathNode> nodes)
        {
            nextNodes = nodes ?? new List<PathNode>();
        }

        public PathNode GetNextNode(int routeIndex = 0)
        {
            if (nextNodes.Count == 0)
            {
                return null;
            }

            routeIndex = Mathf.Clamp(routeIndex, 0, nextNodes.Count - 1);
            return nextNodes[routeIndex];
        }
    }
}
