using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForestRunner
{
    /// <summary>Code-driven uGUI helpers (canvas, panels, labels, buttons, sliders) with a bamboo-forest theme.</summary>
    public static class UIKit
    {
        public static readonly Color PanelColor = new Color(0.04f, 0.13f, 0.08f, 0.9f);
        public static readonly Color PanelLight = new Color(0.08f, 0.22f, 0.13f, 0.92f);
        public static readonly Color Accent = new Color(1f, 0.83f, 0.25f, 1f);
        public static readonly Color TextColor = new Color(0.96f, 0.99f, 0.92f, 1f);
        public static readonly Color ButtonColor = new Color(0.17f, 0.45f, 0.24f, 1f);
        public static readonly Color DangerColor = new Color(0.62f, 0.2f, 0.15f, 1f);
        public static readonly Color Locked = new Color(0.25f, 0.25f, 0.27f, 1f);

        static Font font;
        static Sprite round;

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                }
                return font;
            }
        }

        public static Sprite Round
        {
            get
            {
                if (round != null) return round;
                int s = 64; float r = 20f;
                Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                Color32[] px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, r, s - r);
                        float cy = Mathf.Clamp(y + 0.5f, r, s - r);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        float a = Mathf.Clamp01(r - d + 0.5f);
                        px[y * s + x] = new Color(1f, 1f, 1f, a);
                    }
                tex.SetPixels32(px);
                tex.Apply(false);
                round = Sprite.Create(tex, new UnityEngine.Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                return round;
            }
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            GameObject go = new GameObject(name);
            Canvas c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            CanvasScaler cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        /// <summary>Rect anchored at a point; pivot equals anchor so 'pos' places that corner/edge/centre.</summary>
        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color c)
        {
            RectTransform rt = Rect(parent, name, anchor, pos, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Round;
            img.type = Image.Type.Sliced;
            img.color = c;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 sz)
        {
            RectTransform rt = Rect(parent, "Label", anchor, pos, sz);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            Shadow sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static Button Btn(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick, int fontSize, Color color)
        {
            RectTransform rt = Rect(parent, "Button_" + label, anchor, pos, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = Round;
            img.type = Image.Type.Sliced;
            img.color = color;
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.1f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            Label(rt, label, fontSize, TextColor, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            b.onClick.AddListener(delegate
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                if (onClick != null) onClick();
            });
            return b;
        }

        public static Button Btn(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
        {
            return Btn(parent, label, anchor, pos, size, onClick, 38, ButtonColor);
        }

        public static void SetLabel(Button b, string text)
        {
            Text t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static Slider MakeSlider(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float value, Action<float> onChange)
        {
            RectTransform rt = Rect(parent, "Slider", anchor, pos, size);
            Slider s = rt.gameObject.AddComponent<Slider>();

            Image bg = Panel(rt, "Background", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x, 16f), new Color(0.02f, 0.06f, 0.04f, 1f));

            RectTransform fillArea = Stretch(rt, "FillArea");
            fillArea.offsetMin = new Vector2(8f, size.y * 0.5f - 8f);
            fillArea.offsetMax = new Vector2(-8f, -(size.y * 0.5f - 8f));
            GameObject fillGo = new GameObject("Fill", typeof(RectTransform));
            RectTransform fill = fillGo.GetComponent<RectTransform>();
            fill.SetParent(fillArea, false);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.sizeDelta = new Vector2(10f, 0f);
            Image fi = fillGo.AddComponent<Image>();
            fi.sprite = Round; fi.type = Image.Type.Sliced; fi.color = Accent;

            RectTransform handleArea = Stretch(rt, "HandleArea");
            handleArea.offsetMin = new Vector2(16f, 0f);
            handleArea.offsetMax = new Vector2(-16f, 0f);
            GameObject hGo = new GameObject("Handle", typeof(RectTransform));
            RectTransform h = hGo.GetComponent<RectTransform>();
            h.SetParent(handleArea, false);
            h.anchorMin = new Vector2(0f, 0.5f);
            h.anchorMax = new Vector2(0f, 0.5f);
            h.sizeDelta = new Vector2(32f, 32f);
            Image hi = hGo.AddComponent<Image>();
            hi.sprite = Round; hi.type = Image.Type.Sliced; hi.color = Color.white;

            s.fillRect = fill;
            s.handleRect = h;
            s.targetGraphic = hi;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f;
            s.value = value;
            s.onValueChanged.AddListener(delegate (float v) { if (onChange != null) onChange(v); });
            return s;
        }
    }
}
