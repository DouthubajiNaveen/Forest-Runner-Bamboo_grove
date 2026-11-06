using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForestRunner
{
    /// <summary>
    /// Central run controller: state machine, speed ramp, distance / score, coin tally,
    /// level unlocking, floating-origin shifting, pause / restart / game-over flow.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }
        public LevelConfig Level { get; private set; }
        public int LevelIndex { get; private set; }

        public float BaseSpeed { get; private set; }
        public float Distance { get; private set; }
        public int RunCoins { get; private set; }
        public bool NewHighScore { get; private set; }
        public bool NextLevelUnlocked { get; private set; }
        public DeathCause LastDeath { get; private set; }

        int scoreBonus;
        bool unlockTriggered;

        public event Action<GameState> OnStateChanged;
        public event Action<string, float> OnBanner;

        public const float BoostMultiplier = 1.6f;

        public float CurrentSpeed
        {
            get
            {
                if (State != GameState.Playing) return 0f;
                float m = 1f;
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.SpeedBoost)) m = BoostMultiplier;
                return BaseSpeed * m;
            }
        }

        public int Score { get { return Mathf.FloorToInt(Distance) + scoreBonus; } }
        public float Difficulty01 { get { return Mathf.Clamp01(Distance / 4000f); } }

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            State = GameState.Ready;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Begin(LevelConfig lv, int index)
        {
            Level = lv;
            LevelIndex = index;
            BaseSpeed = lv.baseSpeed;
            Distance = 0f;
            scoreBonus = 0;
            RunCoins = 0;
            NewHighScore = false;
            NextLevelUnlocked = false;
            unlockTriggered = false;
            SetState(GameState.Playing);
            if (OnBanner != null) OnBanner("LEVEL " + (index + 1) + "  -  " + lv.levelName.ToUpper(), 3.2f);
        }

        void SetState(GameState s)
        {
            State = s;
            if (OnStateChanged != null) OnStateChanged(s);
        }

        public void AddCoins(int amount)
        {
            RunCoins += amount;
            scoreBonus += amount * 10;
            SaveSystem.AddCoins(amount);
        }

        void Update()
        {
            if (State != GameState.Playing) return;
            float dt = Time.deltaTime;

            BaseSpeed = Mathf.Min(Level.maxSpeed, BaseSpeed + Level.acceleration * dt);
            Distance += CurrentSpeed * dt;

            PlayerController p = PlayerController.Instance;
            if (p != null && p.transform.position.z > GameConstants.OriginShiftThreshold)
                ShiftOrigin(p.transform.position.z);

            if (!unlockTriggered && Level.distanceToUnlockNext > 0 && Distance >= Level.distanceToUnlockNext)
            {
                unlockTriggered = true;
                int next = LevelIndex + 1;
                if (next < ConfigDatabase.Levels.Length && next > SaveSystem.UnlockedLevel)
                {
                    SaveSystem.UnlockedLevel = next;
                    NextLevelUnlocked = true;
                    SaveSystem.Save();
                    if (OnBanner != null) OnBanner("LEVEL " + (next + 1) + " UNLOCKED!", 3f);
                    if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Unlock, 1f, 1f);
                }
            }
        }

        /// <summary>Floating origin: keeps coordinates small so physics / rendering stay precise.</summary>
        void ShiftOrigin(float dz)
        {
            if (PlayerController.Instance != null) PlayerController.Instance.ShiftZ(dz);
            if (CameraController.Instance != null) CameraController.Instance.ShiftZ(dz);
            if (EnvironmentSpawner.Instance != null) EnvironmentSpawner.Instance.ShiftOrigin(dz);
        }

        public void PlayerDied(DeathCause cause)
        {
            if (State != GameState.Playing) return;
            LastDeath = cause;
            SetState(GameState.Dying);
            StartCoroutine(EndRunRoutine());
        }

        IEnumerator EndRunRoutine()
        {
            yield return new WaitForSeconds(1.4f);
            EndRun();
        }

        void EndRun()
        {
            SetState(GameState.GameOver);
            int score = Score;
            if (score > SaveSystem.HighScore)
            {
                SaveSystem.HighScore = score;
                NewHighScore = true;
            }
            int dist = Mathf.FloorToInt(Distance);
            if (dist > SaveSystem.BestDistance) SaveSystem.BestDistance = dist;
            if (score > SaveSystem.GetLevelBest(LevelIndex)) SaveSystem.SetLevelBest(LevelIndex, score);
            SaveSystem.Save();

            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.GameOver, 1f, 1f);
            if (GameOverManager.Instance != null) GameOverManager.Instance.Show();
        }

        public void SetPaused(bool paused)
        {
            if (paused && State == GameState.Playing)
            {
                SetState(GameState.Paused);
                Time.timeScale = 0f;
            }
            else if (!paused && State == GameState.Paused)
            {
                Time.timeScale = 1f;
                SetState(GameState.Playing);
            }
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SaveSystem.Save();
            SceneManager.LoadScene(GameConstants.SceneGameplay);
        }

        public void LoadMenu()
        {
            Time.timeScale = 1f;
            SaveSystem.Save();
            SceneManager.LoadScene(GameConstants.SceneMenu);
        }

        public void PlayLevel(int index)
        {
            SaveSystem.SelectedLevel = index;
            Restart();
        }
    }
}
