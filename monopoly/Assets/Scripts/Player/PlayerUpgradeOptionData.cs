using UnityEngine;

namespace Monopoly.Player
{
    [CreateAssetMenu(menuName = "Monopoly/Player Upgrade Option", fileName = "PlayerUpgradeOption_")]
    public class PlayerUpgradeOptionData : ScriptableObject
    {
        public string optionId;
        public string title;
        public string description;
        public PlayerUpgradeTier tier = PlayerUpgradeTier.Common;
        public PlayerUpgradeEffectType effectType;
        public int minRollCount;
        public int maxRollCount = 999;
        public int baseWeight = 10;
        public int extraWeightPerRollWindow;
        public int rollsPerWeightStep = 2;
        public int primaryValue;
        public int secondaryValue;
    }
}
