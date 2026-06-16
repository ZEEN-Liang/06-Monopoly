using System.Collections.Generic;
using Monopoly.Player;
using Monopoly.UI;
using System.Linq;
using UnityEngine;

namespace Monopoly.Events
{
    public class EventManager : MonoBehaviour
    {
        [Header("Assign event assets here or use Resources/Events")]
        [SerializeField] private List<EventData> eventPool = new List<EventData>();
        [SerializeField] private UIManager uiManager;

        private void Awake()
        {
            EnsureEventPool();
        }

        public void Configure(List<EventData> events, UIManager manager)
        {
            if (events != null && events.Count > 0)
            {
                eventPool = events;
            }

            uiManager = manager;
            EnsureEventPool();
        }

        public void TriggerRandomEvent(PlayerData playerData)
        {
            EnsureEventPool();

            if (eventPool.Count == 0 || playerData == null)
            {
                return;
            }

            EventData eventData = eventPool[Random.Range(0, eventPool.Count)];
            uiManager?.ShowEventPanel(
                eventData,
                playerData,
                choiceData =>
                {
                    ApplyChoice(playerData, choiceData);
                    uiManager?.ShowTransientMessage(BuildResultMessage(choiceData));
                });
        }

        public void ApplyChoice(PlayerData playerData, EventChoiceData choiceData)
        {
            if (playerData == null || choiceData == null)
            {
                return;
            }

            playerData.AddMoney(choiceData.moneyDelta);
            playerData.ChangeSatisfaction(choiceData.satisfactionDelta);
        }

        private string BuildResultMessage(EventChoiceData choiceData)
        {
            if (choiceData == null)
            {
                return "Event resolved.";
            }

            string moneyPart = choiceData.moneyDelta >= 0 ? $"+{choiceData.moneyDelta}" : choiceData.moneyDelta.ToString();
            string satisfactionPart = choiceData.satisfactionDelta >= 0 ? $"+{choiceData.satisfactionDelta}" : choiceData.satisfactionDelta.ToString();
            return $"Event result  Money {moneyPart}  Satisfaction {satisfactionPart}";
        }

        private void EnsureEventPool()
        {
            if (eventPool != null && eventPool.Count > 0)
            {
                return;
            }

            EventData[] loadedAssets = Resources.LoadAll<EventData>("Events");
            if (loadedAssets != null && loadedAssets.Length > 0)
            {
                eventPool = loadedAssets.Where(eventData => eventData != null).ToList();
                Debug.Log($"EventManager loaded {eventPool.Count} event assets from Resources/Events.");
            }
        }
    }
}
