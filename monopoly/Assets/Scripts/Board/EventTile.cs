using Monopoly.Events;
using Monopoly.Player;
using Monopoly.Utils;
using UnityEngine;

namespace Monopoly.Board
{
    public class EventTile : BoardTile
    {
        [SerializeField] private EventManager eventManager;
        [Header("Visual")]
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private Color eventColor = new Color(1f, 0.85f, 0.3f);

        private void Reset()
        {
            tileType = TileType.Event;
        }

        private void Start()
        {
            ApplyColor();
        }

        public void Configure(EventManager manager, PathNode node = null)
        {
            eventManager = manager;
            if (node != null)
            {
                bindNode = node;
            }
            tileType = TileType.Event;
            ApplyColor();
            RefreshDebugLabel("Event Tile");
        }

        public override void OnPlayerLanded(PlayerPawn player)
        {
            if (player == null || eventManager == null)
            {
                return;
            }

            eventManager.TriggerRandomEvent(player.PlayerData);
        }

        public override string GetInspectTitle()
        {
            return "Event Tile";
        }

        public override string GetInspectBody()
        {
            return "Trigger a random event";
        }

        private void ApplyColor()
        {
            if (tileRenderer == null)
            {
                tileRenderer = GetComponentInChildren<Renderer>();
            }

            if (tileRenderer != null)
            {
                tileRenderer.material.color = eventColor;
            }

            RefreshDebugLabel("Event Tile");
        }
    }
}
