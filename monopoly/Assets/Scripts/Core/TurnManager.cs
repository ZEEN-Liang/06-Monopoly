using Monopoly.Board;
using Monopoly.Player;
using Monopoly.UI;
using UnityEngine;

namespace Monopoly.Core
{
    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private PlayerPawn playerPawn;
        [SerializeField] private DiceController playerDice;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private PlayerData playerData;
        private bool waitingForDecision;

        public bool IsPlayerTurn { get; private set; }
        public int CurrentTurn { get; private set; }
        public PlayerData BoundPlayerData => playerData;

        public void Configure(PlayerPawn pawn, DiceController dice, UIManager manager, PlayerData data)
        {
            playerPawn = pawn;
            playerDice = dice;
            uiManager = manager;
            playerData = data;
        }

        public void StartPlayerTurn()
        {
            IsPlayerTurn = true;
            CurrentTurn++;

            if (uiManager != null)
            {
                uiManager.ShowTurnStart(CurrentTurn);
            }
        }

        private void Update()
        {
            if (waitingForDecision && uiManager != null && !uiManager.IsBlockingChoiceActive)
            {
                waitingForDecision = false;
                EndPlayerTurn();
                return;
            }

            if (IsPlayerTurn && Input.GetKeyDown(KeyCode.Space))
            {
                RollPlayerDice();
            }
        }

        public void RollPlayerDice()
        {
            if (!IsPlayerTurn || playerDice == null || playerPawn == null)
            {
                return;
            }

            IsPlayerTurn = false;

            int steps = playerDice.Roll();
            playerData?.RecordDiceRoll(steps);

            if (uiManager != null)
            {
                uiManager.ShowDiceResult(steps);
            }

            playerPawn.BeginMove(steps, OnPlayerMoveFinished);
        }

        private void OnPlayerMoveFinished()
        {
            BoardTile tile = playerPawn.CurrentTile;

            if (tile != null)
            {
                tile.OnPlayerLanded(playerPawn);
            }

            if (uiManager != null && uiManager.IsBlockingChoiceActive)
            {
                waitingForDecision = true;
                return;
            }

            EndPlayerTurn();
        }

        public void EndPlayerTurn()
        {
            waitingForDecision = false;
            IsPlayerTurn = false;
            StartPlayerTurn();
        }
    }
}
