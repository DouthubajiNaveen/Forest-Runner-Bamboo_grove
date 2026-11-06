using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ForestRunner
{
    /// <summary>Main menu: Play, level selection, shop, settings, quit. Also animates the backdrop camera.</summary>
    public class MainMenuManager : MonoBehaviour
    {
        GameObject mainPanel, levelPanel, settingsPanel;
        ShopManager shop;
        Text coinText, statsText;
        Transform camT;
        EnvironmentSpawner spawner;
        CharacterVisual preview;
        bool confirmReset;
        Button resetBtn;

        public static MainMenuManager Create(Camera cam, EnvironmentSpawner env, CharacterVisual runner)
        {
            GameObject go = new GameObject("MainMenuManager");
            MainMenuManager m = go.AddComponent<MainMenuManager>();
            m.camT = cam.transform;
            m.spawner = env;
            m.preview = runner;
            m.Build();
            return m;
        }

        void Build()
        {
            UIKit.EnsureEventSystem();
            Canvas canvas = UIKit.CreateCanvas("MenuCanvas", 10);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            BuildMain(root);
            BuildLevels(root);
            shop = ShopManager.Create(root, delegate { Show(mainPanel); });
            shop.OnChanged += delegate
            {
                preview.ApplySkin(ConfigDatabase.GetSkin(SaveSystem.SelectedSkin));
                RefreshHeader();
            };
            shop.Root.SetActive(false);
            BuildSettings(root);
            Show(mainPanel);
        }

        void Show(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            levelPanel.SetActive(panel == levelPanel);
            settingsPanel.SetActive(panel == settingsPanel);
            shop.Root.SetActive(panel == shop.gameObject || panel == shop.Root);
            if (panel == levelPanel) RebuildLevels();
            if (panel == shop.Root) shop.Refresh();
            RefreshHeader();
            confirmReset = false;
            if (resetBtn != null) UIKit.SetLabel(resetBtn, "RESET PROGRESS");
        }

        void RefreshHeader()
        {
            coinText.text = SaveSystem.Coins.ToString();
            statsText.text = "BEST SCORE  " + SaveSystem.HighScore + "      BEST DISTANCE  " + SaveSystem.BestDistance + " m";
        }

        // ---------------------------------------------------------------- main

        void BuildMain(Transform root)
        {
            RectTransform r = UIKit.Stretch(root, "MainPanel");
            mainPanel = r.gameObject;
            Transform p = r;

            UIKit.Label(p, "FOREST RUNNER", 130, UIKit.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1400f, 150f));
            UIKit.Label(p, "BAMBOO GROVE", 64, UIKit.TextColor, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1000f, 80f));

            Image coinPanel = UIKit.Panel(p, "CoinPanel", new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(320f, 74f), new Color(0f, 0f, 0f, 0.45f));
            UIKit.Panel(coinPanel.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(46f, 46f), new Color(1f, 0.8f, 0.15f, 1f));
            coinText = UIKit.Label(coinPanel.transform, "0", 48, UIKit.TextColor, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(210f, 70f));

            Vector2 L = new Vector2(0f, 0.5f);
            Vector2 size = new Vector2(480f, 92f);
            UIKit.Btn(p, "PLAY", L, new Vector2(70f, 150f), size, delegate { StartGame(LevelManager.SafeSelectedLevel()); }, 46, new Color(0.2f, 0.58f, 0.28f, 1f));
            UIKit.Btn(p, "LEVELS", L, new Vector2(70f, 40f), size, delegate { Show(levelPanel); });
            UIKit.Btn(p, "SHOP", L, new Vector2(70f, -70f), size, delegate { Show(shop.Root); });
            UIKit.Btn(p, "SETTINGS", L, new Vector2(70f, -180f), size, delegate { Show(settingsPanel); });
            UIKit.Btn(p, "QUIT", L, new Vector2(70f, -290f), size, QuitGame, 38, UIKit.DangerColor);

            Image stats = UIKit.Panel(p, "Stats", new Vector2(0f, 0f), new Vector2(40f, 90f), new Vector2(1100f, 64f), new Color(0f, 0f, 0f, 0.45f));
            statsText = UIKit.Label(stats.transform, "", 32, UIKit.TextColor, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(1060f, 60f));
            UIKit.Label(p, "A / D  Move     Space  Jump     S  Slide     Esc  Pause", 30, UIKit.TextColor, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(44f, 30f), new Vector2(1200f, 44f));
        }

        void StartGame(int level)
        {
            SaveSystem.SelectedLevel = level;
            SaveSystem.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameConstants.SceneGameplay);
        }

        void QuitGame()
        {
            SaveSystem.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- level select

        void BuildLevels(Transform root)
        {
            RectTransform r = UIKit.Stretch(root, "LevelPanel");
            levelPanel = r.gameObject;
            Image dim = levelPanel.AddComponent<Image>();
            dim.color = new Color(0f, 0.04f, 0.02f, 0.78f);
            UIKit.Label(r, "SELECT LEVEL", 90, UIKit.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1000f, 110f));
            UIKit.Btn(r, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 84f), delegate { Show(mainPanel); });
        }

        void RebuildLevels()
        {
            Transform r = levelPanel.transform;
            for (int i = r.childCount - 1; i >= 0; i--)
            {
                if (r.GetChild(i).name == "Card") { r.GetChild(i).gameObject.SetActive(false); Destroy(r.GetChild(i).gameObject); }
            }
            LevelConfig[] levels = ConfigDatabase.Levels;
            for (int i = 0; i < levels.Length; i++) BuildCard(r, levels[i], i);
        }

        void BuildCard(Transform parent, LevelConfig lv, int i)
        {
            bool unlocked = SaveSystem.IsLevelUnlocked(i);
            Image card = UIKit.Panel(parent, "Card", new Vector2(0.5f, 0.5f), new Vector2((i - 2) * 370f, -10f), new Vector2(350f, 640f), unlocked ? UIKit.PanelColor : new Color(0.1f, 0.1f, 0.12f, 0.92f));
            Transform c = card.transform;
            Vector2 top = new Vector2(0.5f, 1f);
            UIKit.Label(c, "LEVEL " + (i + 1), 40, unlocked ? UIKit.Accent : Color.gray, TextAnchor.MiddleCenter, top, new Vector2(0f, -18f), new Vector2(330f, 50f));
            UIKit.Label(c, lv.levelName, 34, unlocked ? UIKit.TextColor : Color.gray, TextAnchor.MiddleCenter, top, new Vector2(0f, -72f), new Vector2(320f, 90f));

            // colour swatch showing the level's sky
            Image sky = UIKit.Panel(c, "Sky", top, new Vector2(0f, -170f), new Vector2(300f, 70f), unlocked ? lv.skyHorizon : Color.black);
            UIKit.Panel(sky.transform, "Top", top, new Vector2(0f, 0f), new Vector2(300f, 30f), unlocked ? lv.skyTop : new Color(0.1f, 0.1f, 0.1f, 1f));
            UIKit.Panel(sky.transform, "Grass", new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(300f, 18f), unlocked ? lv.grassColor : new Color(0.12f, 0.12f, 0.12f, 1f));

            // difficulty pips
            for (int d = 0; d < 5; d++)
            {
                Color pc = d < lv.difficulty ? (unlocked ? new Color(1f, 0.45f - d * 0.06f, 0.2f, 1f) : Color.gray) : new Color(0.2f, 0.2f, 0.2f, 1f);
                UIKit.Panel(c, "Pip", top, new Vector2(-96f + d * 48f, -262f), new Vector2(36f, 20f), pc);
            }
            UIKit.Label(c, "DIFFICULTY", 22, UIKit.TextColor, TextAnchor.MiddleCenter, top, new Vector2(0f, -290f), new Vector2(300f, 30f));

            UIKit.Label(c, unlocked ? lv.description : LevelManager.UnlockHint(i), 26, unlocked ? UIKit.TextColor : new Color(1f, 0.55f, 0.4f, 1f),
                TextAnchor.UpperCenter, top, new Vector2(0f, -330f), new Vector2(310f, 150f));
            if (unlocked)
                UIKit.Label(c, "Best: " + SaveSystem.GetLevelBest(i), 30, UIKit.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(310f, 40f));

            int idx = i;
            if (unlocked)
                UIKit.Btn(c, "PLAY", new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(280f, 76f), delegate { StartGame(idx); }, 40, new Color(0.2f, 0.58f, 0.28f, 1f));
            else
            {
                Button b = UIKit.Btn(c, "LOCKED", new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(280f, 76f), null, 36, UIKit.Locked);
                b.interactable = false;
            }
        }

        // ---------------------------------------------------------------- settings

        void BuildSettings(Transform root)
        {
            RectTransform r = UIKit.Stretch(root, "SettingsPanel");
            settingsPanel = r.gameObject;
            Image dim = settingsPanel.AddComponent<Image>();
            dim.color = new Color(0f, 0.04f, 0.02f, 0.82f);
            UIKit.Label(r, "SETTINGS", 90, UIKit.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1000f, 110f));

            Image box = UIKit.Panel(r, "Box", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1300f, 760f), UIKit.PanelColor);
            Transform b = box.transform;
            Vector2 TL = new Vector2(0f, 1f);

            UIKit.Label(b, "Music volume", 40, UIKit.TextColor, TextAnchor.MiddleLeft, TL, new Vector2(60f, -40f), new Vector2(480f, 50f));
            UIKit.MakeSlider(b, TL, new Vector2(560f, -42f), new Vector2(640f, 50f), SaveSystem.MusicVolume, delegate (float v)
            {
                SaveSystem.MusicVolume = v;
                if (AudioManager.Instance != null) AudioManager.Instance.ApplyVolumes();
            });
            UIKit.Label(b, "Sound effects volume", 40, UIKit.TextColor, TextAnchor.MiddleLeft, TL, new Vector2(60f, -120f), new Vector2(480f, 50f));
            UIKit.MakeSlider(b, TL, new Vector2(560f, -122f), new Vector2(640f, 50f), SaveSystem.SfxVolume, delegate (float v)
            {
                SaveSystem.SfxVolume = v;
                if (AudioManager.Instance != null) AudioManager.Instance.ApplyVolumes();
            });

            UIKit.Label(b, "Graphics quality", 40, UIKit.TextColor, TextAnchor.MiddleLeft, TL, new Vector2(60f, -200f), new Vector2(480f, 50f));
            Button q = UIKit.Btn(b, "", TL, new Vector2(560f, -190f), new Vector2(400f, 70f), null, 36, UIKit.ButtonColor);
            UIKit.SetLabel(q, QualitySettings.names[QualitySettings.GetQualityLevel()]);
            q.onClick.AddListener(delegate
            {
                int n = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
                QualitySettings.SetQualityLevel(n, true);
                SaveSystem.QualityLevel = n;
                UIKit.SetLabel(q, QualitySettings.names[n]);
            });
            Button fs = UIKit.Btn(b, "", TL, new Vector2(990f, -190f), new Vector2(260f, 70f), null, 32, UIKit.ButtonColor);
            UIKit.SetLabel(fs, Screen.fullScreen ? "FULLSCREEN" : "WINDOWED");
            fs.onClick.AddListener(delegate
            {
                Screen.fullScreen = !Screen.fullScreen;
                UIKit.SetLabel(fs, Screen.fullScreen ? "FULLSCREEN" : "WINDOWED");
            });

            UIKit.Label(b, "CONTROLS", 44, UIKit.Accent, TextAnchor.MiddleLeft, TL, new Vector2(60f, -290f), new Vector2(600f, 56f));
            UIKit.Label(b,
                "A / Left Arrow      Move left\n" +
                "D / Right Arrow    Move right\n" +
                "Space                    Jump\n" +
                "S / Down Arrow     Slide (in the air: drop fast)\n" +
                "Esc                        Pause / resume\n" +
                "Mouse                   Menu buttons",
                34, UIKit.TextColor, TextAnchor.UpperLeft, TL, new Vector2(60f, -350f), new Vector2(1180f, 280f));

            resetBtn = UIKit.Btn(b, "RESET PROGRESS", new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(420f, 70f), delegate
            {
                if (!confirmReset)
                {
                    confirmReset = true;
                    UIKit.SetLabel(resetBtn, "CLICK AGAIN TO CONFIRM");
                }
                else
                {
                    SaveSystem.ResetProgress();
                    confirmReset = false;
                    UIKit.SetLabel(resetBtn, "PROGRESS RESET");
                    preview.ApplySkin(ConfigDatabase.GetSkin(0));
                    RefreshHeader();
                }
            }, 28, UIKit.DangerColor);

            UIKit.Btn(r, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 84f), delegate { Show(mainPanel); });
        }

        // ---------------------------------------------------------------- backdrop animation

        void Update()
        {
            float dt = Time.deltaTime;
            if (spawner != null) spawner.ShiftOrigin(8f * dt);
            if (camT != null)
            {
                float t = Time.time;
                Vector3 pos = new Vector3(-0.1f + Mathf.Sin(t * 0.35f) * 0.45f, 2.2f + Mathf.Sin(t * 0.5f) * 0.12f, -6f);
                camT.position = pos;
                camT.rotation = Quaternion.LookRotation(new Vector3(-1.9f, 1.3f, 2f) - pos);
            }
        }
    }
}
