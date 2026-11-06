using UnityEngine;

namespace ForestRunner
{
    /// <summary>ScriptableObject describing one power-up (duration, upgrades, colour).</summary>
    [CreateAssetMenu(fileName = "PowerUp", menuName = "Forest Runner/Power-Up Config")]
    public class PowerUpConfig : ScriptableObject
    {
        public PowerUpType type;
        public string displayName = "Power-Up";
        public string shortLabel = "P";
        [TextArea] public string description = "";
        public float baseDuration = 10f;
        public float durationPerUpgrade = 2f;
        public int maxUpgradeLevel = 5;
        public int basePrice = 100;
        public float spawnWeight = 1f;
        public Color color = Color.white;

        public float GetDuration(int upgradeLevel)
        {
            return baseDuration + durationPerUpgrade * Mathf.Clamp(upgradeLevel, 0, maxUpgradeLevel);
        }

        public int GetUpgradePrice(int currentLevel)
        {
            return basePrice * (currentLevel + 1);
        }
    }
}
