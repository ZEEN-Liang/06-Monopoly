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
        [SerializeField] private int diceRollCost = 10;
        private bool waitingForDecision;

        public bool IsPlayerTurn { get; private set; }
        public int CurrentTurn { get; private set; }
        public PlayerData BoundPlayerData => playerData;
        public int DiceRollCost => diceRollCost;

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
            }
        }

        public void RollPlayerDice()
        {
            if (!IsPlayerTurn || playerDice == null || playerPawn == null)
            {
                Debug.LogWarning(
                    $"RollPlayerDice blocked. IsPlayerTurn={IsPlayerTurn}, Dice={(playerDice != null)}, Pawn={(playerPawn != null)}");
                return;
            }

            if (playerDice.IsRolling)
            {
                Debug.Log("RollPlayerDice ignored: physical dice is already rolling.");
                return;
            }

            if (playerData != null && !playerData.SpendMoney(diceRollCost))
            {
                uiManager?.ShowTransientMessage($"Not enough money to roll. Cost: {diceRollCost}");
                return;
            }

            IsPlayerTurn = false;
            playerDice.Roll(OnDiceRollFinished);
        }

        private void OnDiceRollFinished(int steps)
        {
            playerData?.RecordDiceRoll(steps);
            Debug.Log($"TurnManager rolling dice. Steps={steps}, Pawn={playerPawn.name}");

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
