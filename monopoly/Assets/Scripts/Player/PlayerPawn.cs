using System;
using System.Collections;
using System.Linq;
using Monopoly.Board;
using Monopoly.Core;
using UnityEngine;

namespace Monopoly.Player
{
    public class PlayerPawn : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private PlayerDecisionController decisionController;
        [SerializeField] private PlayerData playerData;
        [SerializeField] private PathNode currentNode;

        private bool isMoving;

        public PlayerDecisionController DecisionController => decisionController;
        public PlayerData PlayerData => playerData;
        public BoardTile CurrentTile => currentNode != null ? currentNode.Tile : null;

        private void Awake()
        {
            TryRecoverCurrentNode();
        }

        private void Start()
        {
            TryRecoverCurrentNode();
        }

        public void Initialize(BoardManager manager, PlayerDecisionController controller, PlayerData data, PathNode startNode)
        {
            boardManager = manager;
            decisionController = controller;
            playerData = data;
            currentNode = startNode;

            SnapToCurrentNodeIfPossible();
        }

        public void BeginMove(int steps, Action onMoveFinished)
        {
            TryRecoverCurrentNode();
            Debug.Log($"Player BeginMove called. Steps={steps}, IsMoving={isMoving}, CurrentNode={(currentNode != null ? currentNode.NodeId : "null")}, BoardManager={(boardManager != null)}");

            if (!isMoving)
            {
                StartCoroutine(MoveStepsRoutine(steps, onMoveFinished));
            }
        }

        private IEnumerator MoveStepsRoutine(int steps, Action onMoveFinished)
        {
            isMoving = true;

            for (int i = 0; i < steps; i++)
            {
                PathNode nextNode = boardManager != null ? boardManager.GetNextNode(currentNode) : null;
                if (nextNode == null)
                {
                    Debug.LogWarning($"Player movement stopped early at step {i + 1}/{steps}: next node is null.");
                    break;
                }

                Debug.Log($"Player moving from {(currentNode != null ? currentNode.NodeId : "null")} to {nextNode.NodeId}");
                currentNode = nextNode;
                yield return MoveToPosition(currentNode.StandPoint.position);
            }

            isMoving = false;
            onMoveFinished?.Invoke();
        }

        private IEnumerator MoveToPosition(Vector3 targetPosition)
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPosition;
        }

        private void TryRecoverCurrentNode()
        {
            if (boardManager == null)
            {
                boardManager = FindObjectOfType<BoardManager>();
            }

            if (currentNode != null)
            {
                return;
            }

            if (boardManager != null && boardManager.StartNode != null)
            {
                currentNode = boardManager.StartNode;
                SnapToCurrentNodeIfPossible();
                Debug.Log($"PlayerPawn recovered start node: {currentNode.NodeId}");
                return;
            }

            PathNode[] allNodes = FindObjectsOfType<PathNode>();
            if (allNodes == null || allNodes.Length == 0)
            {
                return;
            }

            PathNode startTileNode = allNodes.FirstOrDefault(node => node != null && node.Tile is StartTile);
            if (startTileNode != null)
            {
                currentNode = startTileNode;
                SnapToCurrentNodeIfPossible();
                Debug.Log($"PlayerPawn recovered start node from StartTile: {currentNode.NodeId}");
                return;
            }

            PathNode namedStartNode = allNodes.FirstOrDefault(node =>
                node != null &&
                !string.IsNullOrEmpty(node.NodeId) &&
                node.NodeId.IndexOf("00", StringComparison.OrdinalIgnoreCase) >= 0);

            if (namedStartNode != null)
            {
                currentNode = namedStartNode;
                SnapToCurrentNodeIfPossible();
                Debug.Log($"PlayerPawn recovered start node from node id guess: {currentNode.NodeId}");
                return;
            }

            currentNode = allNodes
                .Where(node => node != null)
                .OrderBy(node => Vector3.Distance(transform.position, node.StandPoint.position))
                .FirstOrDefault();

            if (currentNode != null)
            {
                SnapToCurrentNodeIfPossible();
                Debug.Log($"PlayerPawn recovered nearest node: {currentNode.NodeId}");
            }
        }

        private void SnapToCurrentNodeIfPossible()
        {
            if (currentNode != null)
            {
                transform.position = currentNode.StandPoint.position;
            }
        }
    }
}
