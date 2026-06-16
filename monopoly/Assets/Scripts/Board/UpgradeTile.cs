using Monopoly.Player;
using Monopoly.Utils;
using UnityEngine;

namespace Monopoly.Board
{
    public class UpgradeTile : BoardTile
    {
        [Header("Visual")]
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private Color upgradeColor = new Color(0.4f, 1f, 0.5f);

        private void Reset()
        {
            tileType = TileType.Upgrade;
        }

        private void Start()
        {
            ApplyColor();
        }

        public void Configure(PathNode node = null)
        {
            if (node != null)
            {
                bindNode = node;
            }
            tileType = TileType.Upgrade;
            ApplyColor();
            RefreshDebugLabel("Upgrade Tile");
        }

        public override void OnPlayerLanded(PlayerPawn player)
        {
            if (player == null)
            {
                return;
            }

            player.DecisionController?.HandleUpgradeTile(this);
        }

        public override string GetInspectTitle()
        {
            return "Upgrade Tile";
        }

        public override string GetInspectBody()
        {
            return "Pick 1 global upgrade";
        }

        private void ApplyColor()
        {
            if (tileRenderer == null)
            {
                tileRenderer = GetComponentInChildren<Renderer>();
            }

            if (tileRenderer != null)
            {
                tileRenderer.material.color = upgradeColor;
            }

            RefreshDebugLabel("Upgrade Tile");
        }
    }
}
