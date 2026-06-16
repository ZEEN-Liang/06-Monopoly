using UnityEngine;

namespace Monopoly.Events
{
    [System.Serializable]
    public class EventChoiceData
    {
        public string choiceText;
        public int moneyDelta;
        public int satisfactionDelta;
    }
}
