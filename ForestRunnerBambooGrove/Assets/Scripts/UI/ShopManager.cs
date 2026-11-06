using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForestRunner
{
    /// <summary>Coin shop: buy / equip character skins and upgrade power-up durations.</summary>
    public class ShopManager : MonoBehaviour
    {
        public GameObject Root { get; private set; }
        public event Action OnChanged;

        Transform skinList, upgradeList;
        Text coinLabel, messageLabel;

        public static ShopManager Create(Transform parent, Action onBack)
        {
            GameObject go = new GameObject("ShopManager");
            go.transform.SetParent(parent, false);
            ShopManager s = go.AddComponent<ShopManager>();
            s.Build(parent, onBack);
            return s;
        }

        void Build(Transform parent, Action onBack)
        {
            RectTransform root = UIKit.Stretch(parent, "ShopPanel");
            Root = root.gameObject;
            Image dim = Root.AddComponent<Image>();
            dim.color = new Color(0f, 0.04f, 0.02f, 0.82f);

            UIKit.Label(root, "SHOP", 90, UIKit.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 110f));
            coinLabel = UIKit.Label(root, "", 48, UIKit.TextColor, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(-50f, -50f), new Vector2(500f, 70f));
            messageLabel = UIKit.Label(root, "", 34, new Color(1f, 0.6f, 0.4f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 50f));

            Image left = UIKit.Panel(root, "SkinsPanel", new Vector2(0.5f, 0.5f), new Vector2(-480f, -20f), new Vector2(900f, 760f), UIKit.PanelColor);
            UIKit.Label(left.transform, "CHARACTER SKINS", 44, UIKit.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(860f, 60f));
            skinList = UIKit.Rect(left.transform, "List", new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(860f, 660f));

            Image right = UIKit.Panel(root, "UpgradePanel", new Vector2(0.5f, 0.5f), new Vector2(480f, -20f), new Vector2(900f, 760f), UIKit.PanelColor);
            UIKit.Label(right.transform, "POWER-UP UPGRADES", 44, UIKit.Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(860f, 60f));
            upgradeList = UIKit.Rect(right.transform, "List", new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(860f, 660f));

            UIKit.Btn(root, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 84f), onBack);
            Refresh();
        }

        void Say(string msg)
        {
            if (messageLabel != null) messageLabel.text = msg;
        }

        public void Refresh()
        {
            coinLabel.text = "Coins: " + SaveSystem.Coins;
            Clear(skinList);
            Clear(upgradeList);

            SkinConfig[] skins = ConfigDatabase.Skins;
            for (int i = 0; i < skins.Length; i++) BuildSkinRow(skins[i], i);

            PowerUpConfig[] pus = ConfigDatabase.PowerUps;
            for (int i = 0; i < pus.Length; i++) BuildUpgradeRow(pus[i], i);
        }

        static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                GameObject c = t.GetChild(i).gameObject;
                c.SetActive(false);
                Destroy(c);
            }
        }

        void BuildSkinRow(SkinConfig s, int row)
        {
            Vector2 top = new Vector2(0.5f, 1f);
            Image bg = UIKit.Panel(skinList, "Row", top, new Vector2(0f, -row * 128f), new Vector2(860f, 116f), UIKit.PanelLight);
            Transform t = bg.transform;
            Image sw = UIKit.Panel(t, "Swatch", new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(84f, 84f), s.shirt);
            UIKit.Panel(sw.transform, "Hat", new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(56f, 22f), s.hat);
            UIKit.Panel(sw.transform, "Pants", new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(56f, 24f), s.pants);
            UIKit.Label(t, s.skinName, 38, UIKit.TextColor, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(120f, 18f), new Vector2(420f, 50f));

            bool owned = SaveSystem.IsSkinOwned(s.skinIndex);
            bool equipped = SaveSystem.SelectedSkin == s.skinIndex;
            string sub = owned ? (equipped ? "Equipped" : "Owned") : (s.price + " coins");
            UIKit.Label(t, sub, 30, owned ? new Color(0.6f, 1f, 0.7f, 1f) : UIKit.Accent, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(120f, -24f), new Vector2(420f, 40f));

            int idx = s.skinIndex;
            Vector2 right = new Vector2(1f, 0.5f);
            if (equipped)
            {
                Button b = UIKit.Btn(t, "EQUIPPED", right, new Vector2(-16f, 0f), new Vector2(230f, 76f), null, 30, UIKit.Locked);
                b.interactable = false;
            }
            else if (owned)
            {
                UIKit.Btn(t, "EQUIP", right, new Vector2(-16f, 0f), new Vector2(230f, 76f), delegate { Equip(idx); }, 34, UIKit.ButtonColor);
            }
            else
            {
                UIKit.Btn(t, "BUY", right, new Vector2(-16f, 0f), new Vector2(230f, 76f), delegate { BuySkin(idx); }, 34, new Color(0.7f, 0.5f, 0.1f, 1f));
            }
        }

        void BuildUpgradeRow(PowerUpConfig p, int row)
        {
            Vector2 top = new Vector2(0.5f, 1f);
            Image bg = UIKit.Panel(upgradeList, "Row", top, new Vector2(0f, -row * 128f), new Vector2(860f, 116f), UIKit.PanelLight);
            Transform t = bg.transform;
            Image icon = UIKit.Panel(t, "Icon", new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(84f, 84f), p.color);
            UIKit.Label(icon.transform, p.shortLabel, 44, Color.black, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));
            int lvl = SaveSystem.GetUpgradeLevel(p.type);
            UIKit.Label(t, p.displayName, 36, UIKit.TextColor, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(116f, 24f), new Vector2(420f, 46f));
            UIKit.Label(t, "Lv " + lvl + "/" + p.maxUpgradeLevel + "  -  " + p.GetDuration(lvl).ToString("0.#") + "s", 28, UIKit.Accent, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(116f, -22f), new Vector2(420f, 40f));

            PowerUpType type = p.type;
            Vector2 right = new Vector2(1f, 0.5f);
            if (lvl >= p.maxUpgradeLevel)
            {
                Button b = UIKit.Btn(t, "MAX", right, new Vector2(-16f, 0f), new Vector2(230f, 76f), null, 34, UIKit.Locked);
                b.interactable = false;
            }
            else
            {
                int price = p.GetUpgradePrice(lvl);
                UIKit.Btn(t, "UPGRADE " + price, right, new Vector2(-16f, 0f), new Vector2(230f, 76f), delegate { Upgrade(type); }, 28, new Color(0.7f, 0.5f, 0.1f, 1f));
            }
        }

        void Changed()
        {
            Refresh();
            if (OnChanged != null) OnChanged();
        }

        public void BuySkin(int index)
        {
            SkinConfig s = ConfigDatabase.GetSkin(index);
            if (SaveSystem.IsSkinOwned(index)) return;
            if (SaveSystem.SpendCoins(s.price))
            {
                SaveSystem.OwnSkin(index);
                SaveSystem.SelectedSkin = index;
                SaveSystem.Save();
                if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Buy, 1f, 1f);
                Changed();
                Say("");
            }
            else
            {
                if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Denied, 1f, 1f);
                Say("Not enough coins for " + s.skinName + "!");
            }
        }

        public void Equip(int index)
        {
            if (!SaveSystem.IsSkinOwned(index)) return;
            SaveSystem.SelectedSkin = index;
            SaveSystem.Save();
            Changed();
            Say("");
        }

        public void Upgrade(PowerUpType type)
        {
            PowerUpConfig p = ConfigDatabase.GetPowerUp(type);
            int lvl = SaveSystem.GetUpgradeLevel(type);
            if (lvl >= p.maxUpgradeLevel) return;
            if (SaveSystem.SpendCoins(p.GetUpgradePrice(lvl)))
            {
                SaveSystem.SetUpgradeLevel(type, lvl + 1);
                SaveSystem.Save();
                if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Buy, 1f, 1f);
                Changed();
                Say("");
            }
            else
            {
                if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Denied, 1f, 1f);
                Say("Not enough coins to upgrade " + p.displayName + "!");
            }
        }
    }
}
