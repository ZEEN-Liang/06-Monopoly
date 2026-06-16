using System.Collections;
using Monopoly.Board;
using Monopoly.Core;
using UnityEngine;

namespace Monopoly.Customer
{
    public class CustomerAgent : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private CustomerDecisionHelper decisionHelper;
        [SerializeField] private CustomerData data;
        [SerializeField] private PathNode currentNode;
        [SerializeField] private PathNode startNode;
        [SerializeField] private float moveInterval = 1.5f;
        [SerializeField] private float minimumIdleInterval = 0.2f;
        [Header("Floating Income Text")]
        [SerializeField] private Vector3 incomeTextOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private Color incomeTextColor = new Color(0.15f, 0.9f, 0.35f);
        [SerializeField] private float incomeFloatDuration = 0.9f;
        [SerializeField] private float incomeFloatDistance = 0.8f;

        private int completedLoops;
        private bool isRunning;

        public CustomerData Data => data;

        public void Initialize(BoardManager manager, CustomerDecisionHelper helper, CustomerData customerData, PathNode startNode)
        {
            boardManager = manager;
            decisionHelper = helper;
            data = customerData;
            currentNode = startNode;
            this.startNode = startNode;

            if (currentNode != null)
            {
                transform.position = currentNode.StandPoint.position;
            }
        }

        public void StartCustomerLife()
        {
            if (!isRunning)
            {
                StartCoroutine(CustomerLoopRoutine());
            }
        }

        private IEnumerator CustomerLoopRoutine()
        {
            isRunning = true;

            while (completedLoops < 1)
            {
                int steps = decisionHelper != null ? decisionHelper.RollMoveStep(data) : 1;
                yield return MoveStepsRoutine(steps);
                float stopDuration = ResolveStopDurationOnCurrentTile();
                yield return new WaitForSeconds(Mathf.Max(minimumIdleInterval, moveInterval + stopDuration));
            }

            LeaveMap();
        }

        private IEnumerator MoveStepsRoutine(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                PathNode nextNode = boardManager != null ? boardManager.GetNextNode(currentNode) : null;
                if (nextNode == null)
                {
                    break;
                }

                currentNode = nextNode;
                yield return MoveToPosition(currentNode.StandPoint.position);

                if (startNode != null && currentNode == startNode)
                {
                    completedLoops++;
                }
            }

            currentNode.Tile?.OnCustomerLanded(this);
        }

        private float ResolveStopDurationOnCurrentTile()
        {
            if (currentNode == null || currentNode.Tile == null)
            {
                return 0f;
            }

            if (!(currentNode.Tile is ShopTile))
            {
                return 0f;
            }

            return Mathf.Max(0f, currentNode.Tile.GetCustomerStopDuration(this));
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

        public void LeaveMap()
        {
            Destroy(gameObject);
        }

        public void ShowIncomePopup(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            StartCoroutine(ShowIncomePopupRoutine(amount));
        }

        private IEnumerator ShowIncomePopupRoutine(int amount)
        {
            GameObject popupObject = new GameObject("IncomePopup");
            popupObject.transform.SetParent(transform);
            popupObject.transform.localPosition = incomeTextOffset;
            popupObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            TextMesh popupText = popupObject.AddComponent<TextMesh>();
            popupText.text = $"+{amount}";
            popupText.anchor = TextAnchor.MiddleCenter;
            popupText.alignment = TextAlignment.Center;
            popupText.characterSize = 0.15f;
            popupText.fontSize = 40;
            popupText.color = incomeTextColor;

            Vector3 startPosition = popupObject.transform.localPosition;
            Vector3 endPosition = startPosition + Vector3.up * incomeFloatDistance;
            Color startColor = incomeTextColor;

            float elapsed = 0f;
            while (elapsed < incomeFloatDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / incomeFloatDuration);

                popupObject.transform.localPosition = Vector3.Lerp(startPosition, endPosition, progress);

                Color fadedColor = startColor;
                fadedColor.a = Mathf.Lerp(1f, 0f, progress);
                popupText.color = fadedColor;

                yield return null;
            }

            Destroy(popupObject);
        }
    }
}
