using System;
using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Loads the ScriptableObject configs from Resources. If the assets are missing for any
    /// reason, generated defaults (DefaultData) are used so the game always runs.
    /// </summary>
    public static class ConfigDatabase
    {
        static LevelConfig[] levels;
        static PowerUpConfig[] powerUps;
        static SkinConfig[] skins;
        static GameConfig root;

        static GameConfig GetRoot()
        {
            if (root == null) root = Resources.Load<GameConfig>("GameConfig");
            return root;
        }

        public static LevelConfig[] Levels
        {
            get
            {
                if (levels == null || levels.Length == 0 || levels[0] == null)
                {
                    LevelConfig[] loaded = GetRoot() != null ? GetRoot().levels : null;
                    if (loaded != null && loaded.Length >= 5)
                    {
                        levels = (LevelConfig[])loaded.Clone();
                        Array.Sort(levels, (a, b) => a.levelIndex.CompareTo(b.levelIndex));
                    }
                    else levels = DefaultData.CreateLevels();
                }
                return levels;
            }
        }

        public static PowerUpConfig[] PowerUps
        {
            get
            {
                if (powerUps == null || powerUps.Length == 0 || powerUps[0] == null)
                {
                    PowerUpConfig[] loaded = GetRoot() != null ? GetRoot().powerUps : null;
                    if (loaded != null && loaded.Length >= 5)
                    {
                        powerUps = (PowerUpConfig[])loaded.Clone();
                        Array.Sort(powerUps, (a, b) => ((int)a.type).CompareTo((int)b.type));
                    }
                    else powerUps = DefaultData.CreatePowerUps();
                }
                return powerUps;
            }
        }

        public static SkinConfig[] Skins
        {
            get
            {
                if (skins == null || skins.Length == 0 || skins[0] == null)
                {
                    SkinConfig[] loaded = GetRoot() != null ? GetRoot().skins : null;
                    if (loaded != null && loaded.Length >= 1)
                    {
                        skins = (SkinConfig[])loaded.Clone();
                        Array.Sort(skins, (a, b) => a.skinIndex.CompareTo(b.skinIndex));
                    }
                    else skins = DefaultData.CreateSkins();
                }
                return skins;
            }
        }

        public static LevelConfig GetLevel(int index)
        {
            LevelConfig[] l = Levels;
            return l[Mathf.Clamp(index, 0, l.Length - 1)];
        }

        public static PowerUpConfig GetPowerUp(PowerUpType type)
        {
            PowerUpConfig[] p = PowerUps;
            for (int i = 0; i < p.Length; i++)
                if (p[i].type == type) return p[i];
            return p[0];
        }

        public static SkinConfig GetSkin(int index)
        {
            SkinConfig[] s = Skins;
            return s[Mathf.Clamp(index, 0, s.Length - 1)];
        }
    }
}
