using System.Collections.Generic;
using UnityEngine;

namespace Monopoly.Events
{
    [CreateAssetMenu(menuName = "Monopoly/Event Data")]
    public class EventData : ScriptableObject
    {
        public string eventId;
        public string title;
        [TextArea] public string description;
        public List<EventChoiceData> choices = new List<EventChoiceData>();
    }
}
