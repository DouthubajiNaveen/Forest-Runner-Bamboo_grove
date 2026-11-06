using UnityEngine;

namespace ForestRunner
{
    public enum PowerUpType { Magnet = 0, Shield = 1, SpeedBoost = 2, DoubleCoins = 3, Hover = 4 }

    public enum ObstacleType
    {
        Log = 0, Rock = 1, Barricade = 2, Hanging = 3, Pit = 4,
        River = 5, Spike = 6, Boulder = 7, TempleWall = 8, TempleGate = 9
    }

    public enum GameState { Ready, Playing, Paused, Dying, GameOver }

    public enum DeathCause { Obstacle, Fall, Drown }
}
