using UnityEngine;

namespace ForestRunner
{
    /// <summary>ScriptableObject describing one level: look, difficulty and spawn weights.</summary>
    [CreateAssetMenu(fileName = "Level", menuName = "Forest Runner/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        public int levelIndex;
        public string levelName = "Level";
        [TextArea] public string description = "";
        [Range(1, 5)] public int difficulty = 1;
        public int distanceToUnlockNext = 1000;

        [Header("Speed")]
        public float baseSpeed = 16f;
        public float maxSpeed = 28f;
        public float acceleration = 0.12f;

        [Header("Spawning")]
        [Range(0f, 1f)] public float obstacleChance = 0.45f;
        [Range(0f, 1f)] public float coinChance = 0.7f;
        public float wLog = 3f;
        public float wRock = 2f;
        public float wBarricade = 2f;
        public float wHanging = 0f;
        public float wPit = 0f;
        public float wRiver = 0f;
        public float wSpike = 0f;
        public float wBoulder = 0f;
        public float wTempleWall = 0f;
        public float wTempleGate = 0f;

        [Header("Environment")]
        public int bambooPerSide = 9;
        [Range(0f, 1f)] public float templeChance = 0.05f;
        [Range(0f, 1f)] public float vineArchChance = 0.3f;
        public bool hasWaterfall = false;
        public bool night = false;
        public bool glowPlants = false;

        [Header("Colours & Light")]
        public Color skyTop = new Color(0.25f, 0.55f, 0.9f, 1f);
        public Color skyHorizon = new Color(0.78f, 0.9f, 0.85f, 1f);
        public Color fogColor = new Color(0.72f, 0.86f, 0.8f, 1f);
        public float fogDensity = 0.012f;
        public Color ambientColor = new Color(0.5f, 0.6f, 0.55f, 1f);
        public Color lightColor = new Color(1f, 0.96f, 0.85f, 1f);
        public float lightIntensity = 1.15f;
        public float sunPitch = 45f;
        public float sunYaw = 150f;
        public Color pathColor = new Color(0.62f, 0.5f, 0.34f, 1f);
        public Color grassColor = new Color(0.28f, 0.5f, 0.2f, 1f);
        public Color bambooColor = new Color(0.42f, 0.7f, 0.24f, 1f);
        public Color leafColor = new Color(0.32f, 0.62f, 0.2f, 1f);
        public Color stoneColor = new Color(0.5f, 0.5f, 0.48f, 1f);
        public Color waterColor = new Color(0.2f, 0.5f, 0.7f, 0.75f);
        public Color glowColor = new Color(0.3f, 1f, 0.8f, 1f);
    }
}
