using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestRunner
{
    /// <summary>In-game HUD: score, distance, coins, level name, power-up indicators, banners, damage flash.</summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        public Canvas Canvas { get; private set; }

        Text scoreText, distText, coinText, levelText, bannerText;
        Image flashImage, bannerBg;
        int lastScore = -1, lastDist = -1, lastCoins = -1;
        float flashAlpha;
        Color flashColor;
        Coroutine bannerRoutine;
        GameObject hudRoot;

        class PUEntry { public GameObject root; public Image fill; public Text time; }
        readonly PUEntry[] entries = new PUEntry[PowerUpManager.TypeCount];

        public static UIManager Create()
        {
            GameObject go = new GameObject("UIManager");
            UIManager m = go.AddComponent<UIManager>();
            m.Build();
            return m;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (GameManager.Instance != null) GameManager.Instance.OnBanner -= ShowBanner;
        }

        void Build()
        {
            Instance = this;
            UIKit.EnsureEventSystem();
            Canvas = UIKit.CreateCanvas("GameCanvas", 10);
            Canvas.transform.SetParent(transform, false);
            hudRoot = UIKit.Stretch(Canvas.transform, "HUD").gameObject;
            Transform h = hudRoot.transform;

            Vector2 TL = new Vector2(0f, 1f), TR = new Vector2(1f, 1f), TC = new Vector2(0.5f, 1f), ML = new Vector2(0f, 0.5f);

            scoreText = UIKit.Label(h, "0", 70, UIKit.TextColor, TextAnchor.UpperLeft, TL, new Vector2(40f, -20f), new Vector2(700f, 90f));
            distText = UIKit.Label(h, "0 m", 40, UIKit.Accent, TextAnchor.UpperLeft, TL, new Vector2(42f, -105f), new Vector2(600f, 50f));
            levelText = UIKit.Label(h, "", 34, UIKit.TextColor, TextAnchor.UpperCenter, TC, new Vector2(0f, -22f), new Vector2(900f, 50f));

            Image coinPanel = UIKit.Panel(h, "CoinPanel", TR, new Vector2(-40f, -24f), new Vector2(300f, 72f), new Color(0f, 0f, 0f, 0.4f));
            Image coinIcon = UIKit.Panel(coinPanel.transform, "CoinIcon", ML, new Vector2(14f, 0f), new Vector2(46f, 46f), new Color(1f, 0.8f, 0.15f, 1f));
            coinIcon.sprite = UIKit.Round;
            coinText = UIKit.Label(coinPanel.transform, "0", 48, UIKit.TextColor, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(200f, 70f));

            UIKit.Btn(h, "PAUSE", TR, new Vector2(-40f, -110f), new Vector2(200f, 64f), delegate
            {
                if (PauseManager.Instance != null) PauseManager.Instance.Toggle();
            }, 32, new Color(0.1f, 0.3f, 0.18f, 0.95f));

            // power-up indicators
            for (int i = 0; i < PowerUpManager.TypeCount; i++)
            {
                PowerUpConfig cfg = ConfigDatabase.GetPowerUp((PowerUpType)i);
                PUEntry e = new PUEntry();
                Image bg = UIKit.Panel(h, "PU_" + cfg.displayName, ML, new Vector2(40f, 160f - i * 88f), new Vector2(380f, 76f), new Color(0f, 0f, 0f, 0.5f));
                e.root = bg.gameObject;
                Image icon = UIKit.Panel(bg.transform, "Icon", ML, new Vector2(10f, 0f), new Vector2(56f, 56f), cfg.color);
                UIKit.Label(icon.transform, cfg.shortLabel, 34, Color.black, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
                UIKit.Label(bg.transform, cfg.displayName, 26, UIKit.TextColor, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(78f, -8f), new Vector2(230f, 30f));
                e.time = UIKit.Label(bg.transform, "", 26, UIKit.Accent, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-12f, -8f), new Vector2(100f, 30f));
                UIKit.Panel(bg.transform, "BarBg", new Vector2(0f, 0f), new Vector2(78f, 12f), new Vector2(290f, 16f), new Color(0f, 0f, 0f, 0.6f));
                Image fill = UIKit.Panel(bg.transform, "BarFill", new Vector2(0f, 0f), new Vector2(78f, 12f), new Vector2(290f, 16f), cfg.color);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = 0;
                e.fill = fill;
                e.root.SetActive(false);
                entries[i] = e;
            }

            // banner
            bannerBg = UIKit.Panel(h, "BannerBg", new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1100f, 110f), new Color(0f, 0f, 0f, 0.45f));
            bannerText = UIKit.Label(bannerBg.transform, "", 54, UIKit.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 110f));
            bannerBg.gameObject.SetActive(false);

            // flash overlay
            RectTransform fr = UIKit.Stretch(h, "Flash");
            flashImage = fr.gameObject.AddComponent<Image>();
            flashImage.color = new Color(1f, 0f, 0f, 0f);
            flashImage.raycastTarget = false;

            if (GameManager.Instance != null) GameManager.Instance.OnBanner += ShowBanner;
        }

        void Start()
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.Level != null) levelText.text = "LEVEL " + (GameManager.Instance.LevelIndex + 1) + "  -  " + GameManager.Instance.Level.levelName;
            }
        }

        public void SetHudVisible(bool v)
        {
            if (hudRoot != null) hudRoot.SetActive(v);
        }

        public void Flash(Color c)
        {
            flashColor = c;
            flashAlpha = c.a;
        }

        public void ShowBanner(string text, float seconds)
        {
            if (levelText != null && GameManager.Instance != null && GameManager.Instance.Level != null)
                levelText.text = "LEVEL " + (GameManager.Instance.LevelIndex + 1) + "  -  " + GameManager.Instance.Level.levelName;
            if (bannerRoutine != null) StopCoroutine(bannerRoutine);
            bannerRoutine = StartCoroutine(BannerRoutine(text, seconds));
        }

        IEnumerator BannerRoutine(string text, float seconds)
        {
            bannerText.text = text;
            bannerBg.gameObject.SetActive(true);
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(Mathf.Min(t / 0.3f, (seconds - t) / 0.5f));
                Color c = UIKit.Accent; c.a = a;
                bannerText.color = c;
                Color b = bannerBg.color; b.a = 0.45f * a;
                bannerBg.color = b;
                yield return null;
            }
            bannerBg.gameObject.SetActive(false);
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            int s = gm.Score;
            if (s != lastScore) { lastScore = s; scoreText.text = s.ToString(); }
            int d = Mathf.FloorToInt(gm.Distance);
            if (d != lastDist) { lastDist = d; distText.text = d + " m"; }
            int c = SaveSystem.Coins;
            if (c != lastCoins) { lastCoins = c; coinText.text = c.ToString(); }

            PowerUpManager pm = PowerUpManager.Instance;
            if (pm != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    PowerUpType t = (PowerUpType)i;
                    bool on = pm.IsActive(t);
                    if (entries[i].root.activeSelf != on) entries[i].root.SetActive(on);
                    if (on)
                    {
                        entries[i].fill.fillAmount = pm.Fraction(t);
                        entries[i].time.text = Mathf.CeilToInt(pm.Remaining(t)) + "s";
                    }
                }
            }

            if (flashAlpha > 0f)
            {
                flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 1.2f);
                Color fc = flashColor; fc.a = flashAlpha;
                flashImage.color = fc;
            }
        }
    }
}
