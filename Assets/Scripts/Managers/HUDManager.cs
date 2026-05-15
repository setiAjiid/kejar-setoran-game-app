using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using KejarSetoran.Visual;

namespace KejarSetoran.Managers
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        public Canvas canvas;

        // Game HUD
        private GameObject gameHudRoot;
        private Text moneyText;
        private Text targetText;
        private Text timerText;
        private Image timerFill;
        private Text pathInfoText;
        private Text customerInfoText;
        private Text statusText;
        private Text overlayText;

        // Main Menu
        private GameObject mainMenuRoot;

        // Game Over
        private GameObject gameOverRoot;
        private Text gameOverTitle;
        private Text gameOverStats;

        private Font defaultFont;

        // === Design tokens ===
        private static readonly Color BgDeep = new Color(0.055f, 0.075f, 0.125f, 1f);
        private static readonly Color BgPanel = new Color(0.105f, 0.135f, 0.20f, 0.96f);
        private static readonly Color BgPanelDark = new Color(0.08f, 0.10f, 0.16f, 0.95f);
        private static readonly Color BgPanelMid = new Color(0.14f, 0.18f, 0.26f, 0.95f);
        private static readonly Color Accent = new Color(0.98f, 0.78f, 0.20f, 1f);   // warm gold
        private static readonly Color AccentSoft = new Color(1f, 0.85f, 0.30f, 1f);
        private static readonly Color Primary = new Color(0.27f, 0.58f, 0.97f, 1f);  // blue
        private static readonly Color Success = new Color(0.20f, 0.83f, 0.45f, 1f);
        private static readonly Color Danger = new Color(0.93f, 0.32f, 0.30f, 1f);
        private static readonly Color Muted = new Color(0.62f, 0.68f, 0.78f, 1f);
        private static readonly Color TextMain = new Color(0.95f, 0.96f, 0.99f, 1f);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildCanvas();
            BuildMainMenu();
            BuildGameHud();
            BuildGameOver();
            ShowMainMenu();
        }

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            var es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esGO.AddComponent<StandaloneInputModule>();
#endif
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                var oldLegacy = es.GetComponent<StandaloneInputModule>();
                if (oldLegacy != null) Destroy(oldLegacy);
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                if (es.GetComponent<StandaloneInputModule>() == null)
                    es.gameObject.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        // ===== Main Menu =====
        private void BuildMainMenu()
        {
            mainMenuRoot = new GameObject("MainMenu");
            mainMenuRoot.transform.SetParent(canvas.transform, false);
            var rt = mainMenuRoot.AddComponent<RectTransform>();
            FullScreen(rt);

            // Background gradient
            var grad = AddImage(mainMenuRoot.transform, "BgGradient", FullScreenAnchors());
            grad.sprite = SpriteFactory.VerticalGradient(720, new Color(0.10f, 0.14f, 0.22f), new Color(0.04f, 0.05f, 0.10f));
            grad.color = Color.white;
            grad.raycastTarget = true;

            // Decorative top stripe
            var stripe = AddRounded(mainMenuRoot.transform, "Stripe", new Color(1f, 0.85f, 0.20f, 0.18f), 6);
            var srt = stripe.rectTransform;
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0, 220);
            srt.sizeDelta = new Vector2(360, 6);

            // Title
            var title = MakeText(mainMenuRoot.transform, "KEJAR SETORAN", 72, new Vector2(0, 150), new Vector2(900, 90), TextMain, FontStyle.Bold);
            AddShadow(title, new Color(0, 0, 0, 0.65f), new Vector2(3, -3));

            // Subtitle
            MakeText(mainMenuRoot.transform, "Ojek Delivery  -  Dijkstra Shortest Path", 22, new Vector2(0, 90), new Vector2(900, 30), Accent, FontStyle.Normal);

            // Tagline card
            var tagCard = AddRounded(mainMenuRoot.transform, "Tag", new Color(0f, 0f, 0f, 0.35f), 14);
            var tagRt = tagCard.rectTransform;
            tagRt.anchorMin = new Vector2(0.5f, 0.5f); tagRt.anchorMax = new Vector2(0.5f, 0.5f);
            tagRt.anchoredPosition = new Vector2(0, 25); tagRt.sizeDelta = new Vector2(640, 90);
            MakeText(tagCard.transform,
                "Antar penumpang lewat rute tercepat sebelum waktu habis.\n" +
                "WASD = jalan   SPACE = jemput/antar   H = HINT path (Rp 200)   TAB = overlay   ESC = pause",
                17, Vector2.zero, new Vector2(620, 80), new Color(0.85f, 0.88f, 0.95f), FontStyle.Italic);

            // Buttons
            var play = MakeButton(mainMenuRoot.transform, "PLAY", new Vector2(0, -85), new Vector2(260, 64), Primary);
            play.onClick.AddListener(() => GameManager.Instance.StartGame());

            var quit = MakeButton(mainMenuRoot.transform, "QUIT", new Vector2(0, -165), new Vector2(260, 50), new Color(0.32f, 0.22f, 0.28f));
            quit.onClick.AddListener(() => {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            // Footer
            MakeText(mainMenuRoot.transform, "EF234405 DAA - Quiz 2 Group Project", 13,
                new Vector2(0, -320), new Vector2(800, 20), Muted, FontStyle.Normal);
        }

        // ===== Game HUD =====
        private void BuildGameHud()
        {
            gameHudRoot = new GameObject("GameHud");
            gameHudRoot.transform.SetParent(canvas.transform, false);
            var rt = gameHudRoot.AddComponent<RectTransform>();
            FullScreen(rt);

            // ---- Top bar (rounded) ----
            var topBar = AddRounded(gameHudRoot.transform, "TopBar", new Color(0.06f, 0.08f, 0.13f, 0.92f), 14);
            var tbr = topBar.rectTransform;
            tbr.anchorMin = new Vector2(0.5f, 1f); tbr.anchorMax = new Vector2(0.5f, 1f);
            tbr.pivot = new Vector2(0.5f, 1f);
            tbr.anchoredPosition = new Vector2(0, -16);
            tbr.sizeDelta = new Vector2(1180, 76);

            // Money badge (left)
            var moneyBadge = AddRounded(topBar.transform, "MoneyBadge", new Color(1f, 0.85f, 0.20f, 0.18f), 10);
            var mbr = moneyBadge.rectTransform;
            mbr.anchorMin = new Vector2(0, 0.5f); mbr.anchorMax = new Vector2(0, 0.5f);
            mbr.pivot = new Vector2(0, 0.5f);
            mbr.anchoredPosition = new Vector2(20, 0);
            mbr.sizeDelta = new Vector2(280, 52);

            MakeText(moneyBadge.transform, "MONEY", 12, new Vector2(0, 14), new Vector2(260, 16), Muted, FontStyle.Bold);
            moneyText = MakeText(moneyBadge.transform, "Rp 0", 26, new Vector2(0, -7), new Vector2(260, 32), Accent, FontStyle.Bold);

            // Target badge (mid-left)
            var tgtBadge = AddRounded(topBar.transform, "TgtBadge", new Color(0.27f, 0.58f, 0.97f, 0.16f), 10);
            var tbr2 = tgtBadge.rectTransform;
            tbr2.anchorMin = new Vector2(0, 0.5f); tbr2.anchorMax = new Vector2(0, 0.5f);
            tbr2.pivot = new Vector2(0, 0.5f);
            tbr2.anchoredPosition = new Vector2(320, 0);
            tbr2.sizeDelta = new Vector2(220, 52);

            MakeText(tgtBadge.transform, "TARGET", 12, new Vector2(0, 14), new Vector2(200, 16), Muted, FontStyle.Bold);
            targetText = MakeText(tgtBadge.transform, "Rp 50.000", 20, new Vector2(0, -7), new Vector2(200, 28), TextMain, FontStyle.Bold);

            // Timer (right side)
            var timerCard = AddRounded(topBar.transform, "TimerCard", new Color(0f, 0f, 0f, 0.25f), 10);
            var tcr = timerCard.rectTransform;
            tcr.anchorMin = new Vector2(1, 0.5f); tcr.anchorMax = new Vector2(1, 0.5f);
            tcr.pivot = new Vector2(1, 0.5f);
            tcr.anchoredPosition = new Vector2(-20, 0);
            tcr.sizeDelta = new Vector2(380, 52);

            MakeText(timerCard.transform, "TIME LEFT", 12, new Vector2(-145, 14), new Vector2(120, 16), Muted, FontStyle.Bold);
            timerText = MakeText(timerCard.transform, "5:00", 22, new Vector2(-145, -7), new Vector2(120, 28), TextMain, FontStyle.Bold);

            var timerBg = AddRounded(timerCard.transform, "TimerBg", new Color(0.05f, 0.06f, 0.10f, 0.85f), 8);
            var tbg = timerBg.rectTransform;
            tbg.anchorMin = new Vector2(1, 0.5f); tbg.anchorMax = new Vector2(1, 0.5f);
            tbg.pivot = new Vector2(1, 0.5f);
            tbg.anchoredPosition = new Vector2(-15, 0);
            tbg.sizeDelta = new Vector2(220, 18);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(timerBg.transform, false);
            timerFill = fillGO.AddComponent<Image>();
            timerFill.sprite = SpriteFactory.MakeRoundedRect(220, 14, 6, Color.white);
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            timerFill.fillAmount = 1f;
            timerFill.color = Success;
            timerFill.raycastTarget = false;
            var frt = timerFill.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(-2, -2);

            // ---- Bottom-left info card ----
            var infoCard = AddRounded(gameHudRoot.transform, "InfoCard", new Color(0.06f, 0.08f, 0.13f, 0.92f), 14);
            var ir = infoCard.rectTransform;
            ir.anchorMin = new Vector2(0, 0); ir.anchorMax = new Vector2(0, 0);
            ir.pivot = new Vector2(0, 0);
            ir.anchoredPosition = new Vector2(20, 20);
            ir.sizeDelta = new Vector2(440, 160);

            // accent stripe at top
            var accent = AddRounded(infoCard.transform, "Accent", new Color(1f, 0.85f, 0.20f, 0.9f), 3);
            var ar = accent.rectTransform;
            ar.anchorMin = new Vector2(0, 1); ar.anchorMax = new Vector2(1, 1);
            ar.pivot = new Vector2(0.5f, 1);
            ar.anchoredPosition = new Vector2(0, -10); ar.sizeDelta = new Vector2(-24, 4);

            MakeText(infoCard.transform, "DIJKSTRA  -  SHORTEST PATH", 11, new Vector2(14, -22),
                new Vector2(400, 14), Muted, FontStyle.Bold).alignment = TextAnchor.UpperLeft;

            pathInfoText = MakeText(infoCard.transform, "Path: -", 16, new Vector2(14, -44),
                new Vector2(410, 28), Accent, FontStyle.Bold);
            pathInfoText.alignment = TextAnchor.UpperLeft;

            var divider = AddImage(infoCard.transform, "Divider", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), new Vector2(-32, 1));
            divider.color = new Color(1f, 1f, 1f, 0.10f);

            MakeText(infoCard.transform, "PASSENGER", 11, new Vector2(14, -82),
                new Vector2(400, 14), Muted, FontStyle.Bold).alignment = TextAnchor.UpperLeft;

            customerInfoText = MakeText(infoCard.transform, "Waiting...", 14, new Vector2(14, -100),
                new Vector2(410, 60), TextMain, FontStyle.Normal);
            customerInfoText.alignment = TextAnchor.UpperLeft;

            // ---- Top-center status (floating toast) ----
            statusText = MakeText(gameHudRoot.transform, "", 28, new Vector2(0, -120),
                new Vector2(900, 50), Accent, FontStyle.Bold);
            var srt = statusText.rectTransform;
            srt.anchorMin = new Vector2(0.5f, 1f); srt.anchorMax = new Vector2(0.5f, 1f);
            AddShadow(statusText, new Color(0, 0, 0, 0.7f), new Vector2(2, -2));

            // ---- Hint chip (bottom-right) ----
            var hint = AddRounded(gameHudRoot.transform, "Hint", new Color(0f, 0f, 0f, 0.55f), 10);
            var hr = hint.rectTransform;
            hr.anchorMin = new Vector2(1, 0); hr.anchorMax = new Vector2(1, 0);
            hr.pivot = new Vector2(1, 0);
            hr.anchoredPosition = new Vector2(-20, 20); hr.sizeDelta = new Vector2(340, 78);

            MakeText(hint.transform, "WASD  move   SPACE  pickup/drop", 13, new Vector2(0, 22),
                new Vector2(320, 20), TextMain, FontStyle.Normal);
            MakeText(hint.transform, "H  HINT path (-Rp 200)   TAB  overlay", 13, new Vector2(0, 0),
                new Vector2(320, 20), Accent, FontStyle.Bold);
            MakeText(hint.transform, "ESC  pause", 13, new Vector2(0, -22),
                new Vector2(320, 20), Muted, FontStyle.Normal);

            // ---- Pause overlay (last, so it renders on top) ----
            var pauseBg = AddImage(gameHudRoot.transform, "PauseOverlay", FullScreenAnchors());
            pauseBg.color = new Color(0, 0, 0, 0.55f);
            pauseBg.raycastTarget = true;

            overlayText = MakeText(pauseBg.transform, "", 56, Vector2.zero, new Vector2(900, 200), TextMain, FontStyle.Bold);
            AddShadow(overlayText, new Color(0, 0, 0, 0.7f), new Vector2(3, -3));
            pauseBg.gameObject.SetActive(false);

            gameHudRoot.SetActive(false);
        }

        // ===== Game Over =====
        private void BuildGameOver()
        {
            gameOverRoot = new GameObject("GameOver");
            gameOverRoot.transform.SetParent(canvas.transform, false);
            var rt = gameOverRoot.AddComponent<RectTransform>();
            FullScreen(rt);

            var grad = AddImage(gameOverRoot.transform, "Bg", FullScreenAnchors());
            grad.sprite = SpriteFactory.VerticalGradient(720, new Color(0.10f, 0.14f, 0.22f), new Color(0.04f, 0.05f, 0.10f));
            grad.color = Color.white;
            grad.raycastTarget = true;

            var card = AddRounded(gameOverRoot.transform, "Card", BgPanel, 24);
            var cr = card.rectTransform;
            cr.anchorMin = new Vector2(0.5f, 0.5f); cr.anchorMax = new Vector2(0.5f, 0.5f);
            cr.anchoredPosition = Vector2.zero; cr.sizeDelta = new Vector2(620, 460);

            gameOverTitle = MakeText(card.transform, "Game Over", 56, new Vector2(0, 150),
                new Vector2(580, 70), TextMain, FontStyle.Bold);
            AddShadow(gameOverTitle, new Color(0, 0, 0, 0.6f), new Vector2(2, -2));

            var divider = AddImage(card.transform, "Divider", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 95), new Vector2(380, 2));
            divider.color = new Color(1f, 1f, 1f, 0.15f);

            gameOverStats = MakeText(card.transform, "", 20, new Vector2(0, 15),
                new Vector2(540, 180), new Color(0.92f, 0.94f, 0.98f), FontStyle.Normal);
            gameOverStats.alignment = TextAnchor.MiddleCenter;

            var retry = MakeButton(card.transform, "RETRY", new Vector2(-130, -150), new Vector2(220, 56), Primary);
            retry.onClick.AddListener(() => GameManager.Instance.StartGame());

            var menu = MakeButton(card.transform, "MAIN MENU", new Vector2(130, -150), new Vector2(220, 56), new Color(0.38f, 0.32f, 0.55f));
            menu.onClick.AddListener(() => GameManager.Instance.GoToMainMenu());

            gameOverRoot.SetActive(false);
        }

        // ===== State controls =====
        public void ShowMainMenu()
        {
            mainMenuRoot.SetActive(true);
            gameHudRoot.SetActive(false);
            gameOverRoot.SetActive(false);
        }

        public void ShowGameHud()
        {
            mainMenuRoot.SetActive(false);
            gameHudRoot.SetActive(true);
            gameOverRoot.SetActive(false);
            var pause = gameHudRoot.transform.Find("PauseOverlay");
            if (pause != null) pause.gameObject.SetActive(false);
        }

        public void ShowGameOver(bool won, int money, int target, int delivered)
        {
            mainMenuRoot.SetActive(false);
            gameHudRoot.SetActive(false);
            gameOverRoot.SetActive(true);
            gameOverTitle.text = won ? "Target Tercapai!" : "Bangkrut!";
            gameOverTitle.color = won ? Success : Danger;
            gameOverStats.text = $"Penghasilan : Rp {money:N0}\nTarget         : Rp {target:N0}\nPenumpang  : {delivered} diantar";
        }

        public void SetPaused(bool paused)
        {
            var pause = gameHudRoot.transform.Find("PauseOverlay");
            if (pause != null) pause.gameObject.SetActive(paused);
            overlayText.text = paused ? "PAUSED\n<size=20>tekan ESC untuk lanjut</size>" : "";
        }

        // ===== Updates =====
        public void UpdateMoney(int money) => moneyText.text = $"Rp {money:N0}";
        public void UpdateTarget(int target) => targetText.text = $"Rp {target:N0}";

        public void UpdateTimer(float seconds, float totalSeconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            timerText.text = $"{m}:{s:00}";
            timerFill.fillAmount = totalSeconds > 0 ? Mathf.Clamp01(seconds / totalSeconds) : 0;
            if (seconds <= 60f)
            {
                float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                timerFill.color = Color.Lerp(Danger, new Color(1f, 0.7f, 0.2f), pulse);
            }
            else if (seconds <= totalSeconds * 0.5f)
                timerFill.color = new Color(0.97f, 0.72f, 0.25f);
            else
                timerFill.color = Success;
        }

        public void UpdatePathInfo(string text) => pathInfoText.text = text;
        public void UpdateCustomerInfo(string text) => customerInfoText.text = text;

        public void FlashStatus(string text, float seconds = 1.6f)
        {
            statusText.text = text;
            CancelInvoke(nameof(ClearStatus));
            Invoke(nameof(ClearStatus), seconds);
        }
        private void ClearStatus() => statusText.text = "";

        // ===== UI helpers =====
        private static (Vector2 min, Vector2 max) FullScreenAnchors()
            => (Vector2.zero, Vector2.one);

        private static void FullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private Image AddImage(Transform parent, string name, (Vector2 min, Vector2 max) anchors)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            var rt = img.rectTransform;
            rt.anchorMin = anchors.min; rt.anchorMax = anchors.max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return img;
        }

        private Image AddImage(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            var rt = img.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return img;
        }

        private Image AddRounded(Transform parent, string name, Color color, int radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.RoundedRect(radius);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = color;
            return img;
        }

        private void AddShadow(Graphic g, Color color, Vector2 offset)
        {
            var shadow = g.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = offset;
        }

        private Text MakeText(Transform parent, string text, int size, Vector2 pos, Vector2 sizeDelta, Color color, FontStyle style)
        {
            var go = new GameObject("Text_" + (text.Length > 0 ? text.Substring(0, Mathf.Min(text.Length, 10)) : "x"));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = defaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            return t;
        }

        private Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.RoundedRect(14);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = color;
            var rt = img.rectTransform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            // soft shadow behind
            var shadowGO = new GameObject("BtnShadow");
            shadowGO.transform.SetParent(go.transform, false);
            shadowGO.transform.SetSiblingIndex(0);
            var sImg = shadowGO.AddComponent<Image>();
            sImg.sprite = SpriteFactory.RoundedRect(14);
            sImg.type = Image.Type.Sliced;
            sImg.pixelsPerUnitMultiplier = 1f;
            sImg.color = new Color(0, 0, 0, 0.35f);
            sImg.raycastTarget = false;
            var srt = sImg.rectTransform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(-2, -6); srt.offsetMax = new Vector2(2, -2);

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            cb.selectedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            var t = MakeText(go.transform, label, 24, Vector2.zero, size, Color.white, FontStyle.Bold);
            t.alignment = TextAnchor.MiddleCenter;

            return btn;
        }
    }
}
