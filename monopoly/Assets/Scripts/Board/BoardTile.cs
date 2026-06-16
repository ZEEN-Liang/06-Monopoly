using Monopoly.Player;
using Monopoly.UI;
using Monopoly.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Monopoly.Board
{
    public class BoardTile : MonoBehaviour
    {
        [SerializeField] protected string tileId;
        [SerializeField] protected TileType tileType;
        [SerializeField] protected PathNode bindNode;
        [Header("Debug Label")]
        [SerializeField] protected bool showDebugLabel = true;
        [SerializeField] protected Vector3 debugLabelOffset = new Vector3(0f, 1.2f, 0f);
        [Header("Selection")]
        [SerializeField] protected Color selectionOutlineColor = new Color(1f, 0.9f, 0.2f, 1f);
        [SerializeField] protected float selectionOutlineThickness = 0.08f;
        [SerializeField] protected float selectionOutlineHeight = 0.18f;
        [SerializeField] protected float selectionOutlinePadding = 0.08f;

        private TextMesh debugTextMesh;
        private GameObject selectionOutlineRoot;
        private bool isSelected;

        public string TileId => tileId;
        public TileType TileType => tileType;
        public PathNode BindNode => bindNode;

        public virtual void Configure(string id, TileType type, PathNode node)
        {
            tileId = id;
            tileType = type;
            bindNode = node;
            RefreshDebugLabel();
            UpdateDebugLabelVisibility();
        }

        public virtual void OnPlayerLanded(PlayerPawn player)
        {
        }

        public virtual void OnCustomerLanded(Customer.CustomerAgent customer)
        {
        }

        public virtual float GetCustomerStopDuration(Customer.CustomerAgent customer)
        {
            return 0f;
        }

        public virtual string GetInspectTitle()
        {
            return tileType == TileType.None ? "Tile" : tileType.ToString();
        }

        public virtual string GetInspectBody()
        {
            string nodeId = bindNode != null ? bindNode.NodeId : "Unbound";
            return $"[{tileType}]  {nodeId}";
        }

        public virtual void SetSelected(bool selected)
        {
            isSelected = selected;
            EnsureSelectionOutline();
            if (selectionOutlineRoot != null)
            {
                selectionOutlineRoot.SetActive(selected);
            }

            UpdateDebugLabelVisibility();
        }

        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowTileInspectPanel(this);
            }
        }

        private void EnsureSelectionOutline()
        {
            if (selectionOutlineRoot != null)
            {
                return;
            }

            Renderer targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer == null)
            {
                return;
            }

            Bounds bounds = targetRenderer.bounds;
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = transform.InverseTransformVector(bounds.size);

            selectionOutlineRoot = new GameObject("SelectionOutline");
            selectionOutlineRoot.transform.SetParent(transform, false);
            selectionOutlineRoot.transform.localPosition = new Vector3(localCenter.x, localCenter.y + localSize.y * 0.5f + 0.02f, localCenter.z);
            selectionOutlineRoot.transform.localRotation = Quaternion.identity;
            selectionOutlineRoot.SetActive(false);

            float outerWidth = localSize.x + selectionOutlinePadding * 2f;
            float outerDepth = localSize.z + selectionOutlinePadding * 2f;
            float height = selectionOutlineHeight;
            float thickness = selectionOutlineThickness;

            CreateOutlineSegment("Top", new Vector3(0f, 0f, outerDepth * 0.5f), new Vector3(outerWidth, height, thickness));
            CreateOutlineSegment("Bottom", new Vector3(0f, 0f, -outerDepth * 0.5f), new Vector3(outerWidth, height, thickness));
            CreateOutlineSegment("Left", new Vector3(-outerWidth * 0.5f, 0f, 0f), new Vector3(thickness, height, outerDepth));
            CreateOutlineSegment("Right", new Vector3(outerWidth * 0.5f, 0f, 0f), new Vector3(thickness, height, outerDepth));
        }

        private void CreateOutlineSegment(string segmentName, Vector3 localPosition, Vector3 localScale)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = segmentName;
            segment.transform.SetParent(selectionOutlineRoot.transform, false);
            segment.transform.localPosition = localPosition;
            segment.transform.localScale = localScale;

            Collider collider = segment.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = segment.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = selectionOutlineColor;
            }
        }

        protected void RefreshDebugLabel(string extraText = "")
        {
            if (!showDebugLabel)
            {
                return;
            }

            if (debugTextMesh == null)
            {
                Transform existing = transform.Find("DebugLabel");
                if (existing != null)
                {
                    debugTextMesh = existing.GetComponent<TextMesh>();
                }

                if (debugTextMesh == null)
                {
                    GameObject textObject = new GameObject("DebugLabel");
                    textObject.transform.SetParent(transform);
                    textObject.transform.localPosition = debugLabelOffset;
                    textObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    debugTextMesh = textObject.AddComponent<TextMesh>();
                    debugTextMesh.anchor = TextAnchor.MiddleCenter;
                    debugTextMesh.alignment = TextAlignment.Center;
                    debugTextMesh.characterSize = 0.15f;
                    debugTextMesh.fontSize = 36;
                    debugTextMesh.color = Color.black;
                }
            }

            debugTextMesh.transform.localPosition = debugLabelOffset;
            debugTextMesh.text = string.IsNullOrWhiteSpace(extraText)
                ? $"{tileType}\n{tileId}"
                : $"{tileType}\n{extraText}";
            UpdateDebugLabelVisibility();
        }

        protected void UpdateDebugLabelVisibility()
        {
            if (debugTextMesh == null)
            {
                return;
            }

            debugTextMesh.gameObject.SetActive(showDebugLabel && isSelected);
        }
    }
}
