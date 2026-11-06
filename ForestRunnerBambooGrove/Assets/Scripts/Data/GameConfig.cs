using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Root ScriptableObject (Assets/Resources/GameConfig.asset) that references the level,
    /// power-up and skin assets stored in Assets/ScriptableObjects/.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Forest Runner/Game Config")]
    public class GameConfig : ScriptableObject
    {
        public LevelConfig[] levels;
        public PowerUpConfig[] powerUps;
        public SkinConfig[] skins;
    }
}
