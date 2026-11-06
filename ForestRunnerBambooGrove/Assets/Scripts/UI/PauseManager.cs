using UnityEngine;
using UnityEngine.UI;

namespace ForestRunner
{
    /// <summary>Pause menu (Escape key or HUD button): Resume / Restart / Main Menu.</summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }
        GameObject panel;

        public static PauseManager Create(Canvas canvas)
        {
            GameObject go = new GameObject("PauseManager");
            PauseManager p = go.AddComponent<PauseManager>();
            p.Build(canvas);
            return p;
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Build(Canvas canvas)
        {
            Instance = this;
            RectTransform root = UIKit.Stretch(canvas.transform, "PausePanel");
            panel = root.gameObject;
            Image dim = panel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            Image box = UIKit.Panel(root, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 640f), UIKit.PanelColor);
            Transform b = box.transform;
            UIKit.Label(b, "PAUSED", 80, UIKit.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 100f));
            UIKit.Btn(b, "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(460f, 96f), Toggle);
            UIKit.Btn(b, "RESTART", new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(460f, 96f), delegate { GameManager.Instance.Restart(); });
            UIKit.Btn(b, "MAIN MENU", new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(460f, 96f), delegate { GameManager.Instance.LoadMenu(); }, 38, UIKit.DangerColor);
            UIKit.Label(b, "Esc - resume", 28, UIKit.TextColor, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(560f, 40f));
            panel.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
        }

        public void Toggle()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.Playing) { gm.SetPaused(true); panel.SetActive(true); }
            else if (gm.State == GameState.Paused) { gm.SetPaused(false); panel.SetActive(false); }
        }
    }
}
