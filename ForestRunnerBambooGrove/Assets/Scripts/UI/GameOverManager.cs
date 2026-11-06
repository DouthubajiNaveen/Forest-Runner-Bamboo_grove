using UnityEngine;
using UnityEngine.UI;

namespace ForestRunner
{
    /// <summary>Game-over screen: final stats, retry, next level, main menu.</summary>
    public class GameOverManager : MonoBehaviour
    {
        public static GameOverManager Instance { get; private set; }
        GameObject panel;
        Text scoreT, distT, coinsT, bestT, headline, unlockT;
        Button nextBtn;

        public static GameOverManager Create(Canvas canvas)
        {
            GameObject go = new GameObject("GameOverManager");
            GameOverManager g = go.AddComponent<GameOverManager>();
            g.Build(canvas);
            return g;
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Build(Canvas canvas)
        {
            Instance = this;
            RectTransform root = UIKit.Stretch(canvas.transform, "GameOverPanel");
            panel = root.gameObject;
            Image dim = panel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.7f);
            Image box = UIKit.Panel(root, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 880f), UIKit.PanelColor);
            Transform b = box.transform;
            Vector2 C = new Vector2(0.5f, 1f);
            UIKit.Label(b, "GAME OVER", 90, new Color(1f, 0.4f, 0.3f, 1f), TextAnchor.MiddleCenter, C, new Vector2(0f, -30f), new Vector2(800f, 110f));
            headline = UIKit.Label(b, "", 40, UIKit.Accent, TextAnchor.MiddleCenter, C, new Vector2(0f, -140f), new Vector2(800f, 50f));
            scoreT = UIKit.Label(b, "", 52, UIKit.TextColor, TextAnchor.MiddleCenter, C, new Vector2(0f, -205f), new Vector2(800f, 70f));
            distT = UIKit.Label(b, "", 44, UIKit.TextColor, TextAnchor.MiddleCenter, C, new Vector2(0f, -280f), new Vector2(800f, 60f));
            coinsT = UIKit.Label(b, "", 44, UIKit.Accent, TextAnchor.MiddleCenter, C, new Vector2(0f, -345f), new Vector2(800f, 60f));
            bestT = UIKit.Label(b, "", 40, UIKit.TextColor, TextAnchor.MiddleCenter, C, new Vector2(0f, -410f), new Vector2(800f, 60f));
            unlockT = UIKit.Label(b, "", 40, new Color(0.5f, 1f, 0.6f, 1f), TextAnchor.MiddleCenter, C, new Vector2(0f, -475f), new Vector2(800f, 60f));

            Vector2 B = new Vector2(0.5f, 0f);
            UIKit.Btn(b, "RETRY", B, new Vector2(0f, 270f), new Vector2(520f, 92f), delegate { GameManager.Instance.Restart(); });
            nextBtn = UIKit.Btn(b, "NEXT LEVEL", B, new Vector2(0f, 160f), new Vector2(520f, 92f), delegate
            {
                GameManager.Instance.PlayLevel(GameManager.Instance.LevelIndex + 1);
            }, 38, new Color(0.7f, 0.5f, 0.1f, 1f));
            UIKit.Btn(b, "MAIN MENU", B, new Vector2(0f, 50f), new Vector2(520f, 92f), delegate { GameManager.Instance.LoadMenu(); }, 38, UIKit.DangerColor);
            panel.SetActive(false);
        }

        public void Show()
        {
            GameManager gm = GameManager.Instance;
            if (UIManager.Instance != null) UIManager.Instance.SetHudVisible(false);
            scoreT.text = "Score:  " + gm.Score;
            distT.text = "Distance:  " + Mathf.FloorToInt(gm.Distance) + " m";
            coinsT.text = "Coins collected:  " + gm.RunCoins;
            bestT.text = "Best score:  " + SaveSystem.HighScore;
            headline.text = gm.NewHighScore ? "NEW HIGH SCORE!" : (gm.LastDeath == DeathCause.Obstacle ? "You hit an obstacle" : (gm.LastDeath == DeathCause.Drown ? "You fell in the river" : "You fell into a pit"));
            int next = gm.LevelIndex + 1;
            bool hasNext = next < ConfigDatabase.Levels.Length && SaveSystem.IsLevelUnlocked(next);
            nextBtn.gameObject.SetActive(hasNext);
            unlockT.text = gm.NextLevelUnlocked ? "Level " + (next + 1) + " unlocked!" : "";
            panel.SetActive(true);
        }
    }
}
