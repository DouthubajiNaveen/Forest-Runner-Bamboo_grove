using UnityEngine;

namespace ForestRunner
{
    /// <summary>PlayerPrefs wrapper: high scores, coin wallet, unlocks, shop and settings.</summary>
    public static class SaveSystem
    {
        const string KHighScore = "fr_highscore";
        const string KBestDistance = "fr_bestdistance";
        const string KCoins = "fr_coins";
        const string KUnlocked = "fr_unlockedlevel";
        const string KSelectedLevel = "fr_selectedlevel";
        const string KMusic = "fr_music";
        const string KSfx = "fr_sfx";
        const string KQuality = "fr_quality";
        const string KSkin = "fr_skin";
        const string KOwnedSkins = "fr_ownedskins";
        const string KUpgrade = "fr_upgrade_";
        const string KLevelBest = "fr_levelbest_";

        public static int HighScore
        {
            get { return PlayerPrefs.GetInt(KHighScore, 0); }
            set { PlayerPrefs.SetInt(KHighScore, value); }
        }

        public static int BestDistance
        {
            get { return PlayerPrefs.GetInt(KBestDistance, 0); }
            set { PlayerPrefs.SetInt(KBestDistance, value); }
        }

        public static int Coins
        {
            get { return PlayerPrefs.GetInt(KCoins, 0); }
            set { PlayerPrefs.SetInt(KCoins, Mathf.Max(0, value)); }
        }

        /// <summary>Highest unlocked level index (0 = only level 1).</summary>
        public static int UnlockedLevel
        {
            get { return PlayerPrefs.GetInt(KUnlocked, 0); }
            set { PlayerPrefs.SetInt(KUnlocked, value); }
        }

        public static int SelectedLevel
        {
            get { return PlayerPrefs.GetInt(KSelectedLevel, 0); }
            set { PlayerPrefs.SetInt(KSelectedLevel, value); }
        }

        public static float MusicVolume
        {
            get { return PlayerPrefs.GetFloat(KMusic, 0.7f); }
            set { PlayerPrefs.SetFloat(KMusic, Mathf.Clamp01(value)); }
        }

        public static float SfxVolume
        {
            get { return PlayerPrefs.GetFloat(KSfx, 0.8f); }
            set { PlayerPrefs.SetFloat(KSfx, Mathf.Clamp01(value)); }
        }

        public static int QualityLevel
        {
            get { return PlayerPrefs.GetInt(KQuality, -1); }
            set { PlayerPrefs.SetInt(KQuality, value); }
        }

        public static int SelectedSkin
        {
            get { return PlayerPrefs.GetInt(KSkin, 0); }
            set { PlayerPrefs.SetInt(KSkin, value); }
        }

        public static bool IsSkinOwned(int index)
        {
            if (index == 0) return true;
            int mask = PlayerPrefs.GetInt(KOwnedSkins, 1);
            return (mask & (1 << index)) != 0;
        }

        public static void OwnSkin(int index)
        {
            int mask = PlayerPrefs.GetInt(KOwnedSkins, 1);
            PlayerPrefs.SetInt(KOwnedSkins, mask | (1 << index));
        }

        public static int GetUpgradeLevel(PowerUpType t)
        {
            return PlayerPrefs.GetInt(KUpgrade + (int)t, 0);
        }

        public static void SetUpgradeLevel(PowerUpType t, int level)
        {
            PlayerPrefs.SetInt(KUpgrade + (int)t, level);
        }

        public static int GetLevelBest(int level)
        {
            return PlayerPrefs.GetInt(KLevelBest + level, 0);
        }

        public static void SetLevelBest(int level, int score)
        {
            PlayerPrefs.SetInt(KLevelBest + level, score);
        }

        public static bool IsLevelUnlocked(int level)
        {
            return level <= UnlockedLevel;
        }

        public static bool SpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins = Coins - amount;
            return true;
        }

        public static void AddCoins(int amount)
        {
            Coins = Coins + amount;
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        /// <summary>Wipes all progress (used by the Settings screen).</summary>
        public static void ResetProgress()
        {
            float music = MusicVolume;
            float sfx = SfxVolume;
            int quality = QualityLevel;
            PlayerPrefs.DeleteAll();
            MusicVolume = music;
            SfxVolume = sfx;
            QualityLevel = quality;
            PlayerPrefs.Save();
        }
    }
}
