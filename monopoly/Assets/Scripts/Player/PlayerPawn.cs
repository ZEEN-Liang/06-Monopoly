using System;
using System.Collections;
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

        public void Initialize(BoardManager manager, PlayerDecisionController controller, PlayerData data, PathNode startNode)
        {
            boardManager = manager;
            decisionController = controller;
            playerData = data;
            currentNode = startNode;

            if (currentNode != null)
            {
                transform.position = currentNode.StandPoint.position;
            }
        }

        public void BeginMove(int steps, Action onMoveFinished)
        {
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
                    break;
                }

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
    }
}
