using UnityEngine;

namespace Monopoly.Player
{
    public class DiceController : MonoBehaviour
    {
        [SerializeField] private int minValue = 1;
        [SerializeField] private int maxValue = 6;

        public int Roll()
        {
            return Random.Range(minValue, maxValue + 1);
        }
    }
}
