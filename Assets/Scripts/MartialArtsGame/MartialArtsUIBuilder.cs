using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace MartialArtsGame
{
    // Builds all UI canvases (Menu / HUD / EndScreen / Trailer) at runtime and
    // wires references to the GameManager components. Saves us from manually
    // authoring dozens of UI GameObjects in the scene.
    [DefaultExecutionOrder(-200)]
    public class MartialArtsUIBuilder : MonoBehaviour
    {
        [Header("References")]
        public MartialArtsGameManager game;
        public PlayerCombatController player;
        public AIOpponentController opponent;
        public ScoreAndStatsManager stats;
        public CoachReplayController replay;
        public TrailerModeController trailer;
        public SimpleCameraController fightCamera;
        public Transform playerTransform;
        public Transform opponentTransform;
        public ProceduralMartialArtsAnimator playerRig;
        public ProceduralMartialArtsAnimator opponentRig;

        Font defaultFont;

        void Awake()
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            EnsureEventSystem();
            BuildAll();
        }

        void EnsureEventSystem()
        {
            // XR-first path for Quest / Vision Pro compatibility.
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                go.transform.SetParent(transform.parent != null ? transform.parent.parent : null, false);
                es = go.GetComponent<EventSystem>();
            }
            // Strip any other input modules and ensure XRUIInputModule is present.
            foreach (var mod in es.GetComponents<BaseInputModule>())
                if (!(mod is XRUIInputModule)) Destroy(mod);
            if (es.GetComponent<XRUIInputModule>() == null) es.gameObject.AddComponent<XRUIInputModule>();
        }

        void BuildAll()
        {
            var menu = BuildMenu();
            var hud = BuildHUD();
            var endScreen = BuildEndScreen();
            var trailerCanvas = BuildTrailerCanvas();

            // Wire HUD
            if (hud != null) { hud.player = player; hud.opponent = opponent; hud.game = game; }
            // Wire menu via game
            if (game != null) game.menu = menu;
            if (game != null) game.hud = hud;
            if (game != null) game.replay = replay;
            if (game != null) game.trailer = trailer;
            if (game != null) game.stats = stats;
            if (game != null) game.player = player;
            if (game != null) game.opponent = opponent;
            if (game != null) game.fightCamera = fightCamera;

            // Wire replay
            if (replay != null) { replay.fightCamera = fightCamera; replay.endScreenRoot = endScreen; }
            // Wire trailer
            if (trailer != null) { trailer.fightCamera = fightCamera; trailer.player = playerTransform; trailer.opponent = opponentTransform; trailer.playerRig = playerRig; trailer.opponentRig = opponentRig; trailer.trailerRoot = trailerCanvas; }
            // Wire fight camera
            if (fightCamera != null) { fightCamera.player = playerTransform; fightCamera.opponent = opponentTransform; }

            // Wire player connections
            if (player != null)
            {
                player.opponent = opponent;
                player.opponentTransform = opponentTransform;
                player.aim = Camera.main != null ? Camera.main.transform : null;
                player.animatorRig = playerRig;
                player.audioSource = player.GetComponent<AudioSource>();
            }
            // Wire AI connections
            if (opponent != null)
            {
                opponent.player = player;
                opponent.playerTransform = playerTransform;
                opponent.animatorRig = opponentRig;
                opponent.audioSource = opponent.GetComponent<AudioSource>();
            }

            // Generate placeholder audio clips so events fire even without imported assets.
            var swing = MakeToneClip("swing", 0.07f, 220f);
            var hit   = MakeToneClip("hit",   0.10f, 90f);
            var block = MakeToneClip("block", 0.08f, 140f);
            if (player != null) { player.swingClip = swing; player.blockClip = block; }
            if (opponent != null) { opponent.swingClip = swing; opponent.blockClip = block; }
        }

        // ---------- Builders ----------

        MainMenuController BuildMenu()
        {
            // Keep tracked device raycaster so XR rays can drive UI.
            var canvasGO = NewCanvas("MenuCanvas", 5);
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            var menu = canvasGO.AddComponent<MainMenuController>();

            // Outer dim (slight world tint)
            AddPanel(canvasGO.transform, "MenuDim", new Color(0f, 0f, 0f, 0.45f), Vector2.zero, Vector2.one);
            // Centered glass panel — Apple Vision Pro inspired translucency.
            var bg = AddPanel(canvasGO.transform, "MenuBg", new Color(0.07f, 0.08f, 0.12f, 0.78f),
                new Vector2(0.18f, 0.05f), new Vector2(0.82f, 0.95f));
            menu.menuRoot = bg.gameObject;
            // Subtle inner highlight (lighter strip at top for glass feel).
            AddPanel(bg.transform, "MenuHighlight", new Color(1f, 1f, 1f, 0.06f),
                new Vector2(0f, 0.94f), new Vector2(1f, 1f));

            // Title
            AddText(bg.transform, "Title", "MARTIAL ARTS  SPARRING  PARTNER", 48, FontStyle.Bold,
                new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.94f), TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.35f));
            AddText(bg.transform, "Subtitle", "Always ready.   Never tired.", 22, FontStyle.Italic,
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.81f), TextAnchor.MiddleCenter, new Color(0.85f, 0.90f, 1f));

            // Style row
            AddText(bg.transform, "StyleHeader", "Choose Martial Art Style", 22, FontStyle.Bold,
                new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.66f), TextAnchor.MiddleCenter, Color.white);
            string[] styles = { "Karate", "Muay Thai", "MMA", "BJJ" };
            menu.styleButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.10f + i * 0.20f;
                menu.styleButtons[i] = AddButton(bg.transform, "Style_" + styles[i], styles[i],
                    new Vector2(x0, 0.52f), new Vector2(x0 + 0.18f, 0.59f));
            }
            menu.selectedStyleLabel = AddText(bg.transform, "StyleSelected", "Style: Karate", 18, FontStyle.Normal,
                new Vector2(0.1f, 0.47f), new Vector2(0.9f, 0.51f), TextAnchor.MiddleCenter, new Color(1f, 0.75f, 0.2f));

            // Difficulty row
            AddText(bg.transform, "DiffHeader", "Choose Difficulty", 22, FontStyle.Bold,
                new Vector2(0.1f, 0.39f), new Vector2(0.9f, 0.45f), TextAnchor.MiddleCenter, Color.white);
            string[] diffs = { "Easy", "Normal", "Hard", "Adaptive" };
            menu.difficultyButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.10f + i * 0.20f;
                menu.difficultyButtons[i] = AddButton(bg.transform, "Diff_" + diffs[i], diffs[i],
                    new Vector2(x0, 0.31f), new Vector2(x0 + 0.18f, 0.38f));
            }
            menu.selectedDifficultyLabel = AddText(bg.transform, "DiffSelected", "Difficulty: Normal", 18, FontStyle.Normal,
                new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.30f), TextAnchor.MiddleCenter, new Color(1f, 0.75f, 0.2f));

            // Action buttons
            menu.startButton = AddButton(bg.transform, "StartBtn", "START FIGHT",
                new Vector2(0.32f, 0.14f), new Vector2(0.68f, 0.22f));
            ColorButton(menu.startButton, new Color(0.20f, 0.65f, 0.25f));
            menu.trailerButton = AddButton(bg.transform, "TrailerBtn", "TRAILER MODE (T)",
                new Vector2(0.32f, 0.07f), new Vector2(0.68f, 0.13f));
            ColorButton(menu.trailerButton, new Color(0.30f, 0.30f, 0.65f));
            menu.quitButton = AddButton(bg.transform, "QuitBtn", "QUIT",
                new Vector2(0.32f, 0.01f), new Vector2(0.68f, 0.06f));
            ColorButton(menu.quitButton, new Color(0.55f, 0.20f, 0.20f));

            // Footer
            AddText(bg.transform, "Footer", "PROJECT 05  |  Meta Quest / Apple Vision Pro Concept", 14, FontStyle.Italic,
                new Vector2(0.0f, 0.0f), new Vector2(1f, 0.03f), TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));
            // Keyboard hint (useful for VR/XR Device Simulator where ray clicks may miss)
            AddText(bg.transform, "KeyboardHint", "Keyboard:  1-4 = Style    F1-F4 = Difficulty    ENTER = Start Fight    T = Trailer", 13, FontStyle.Bold,
                new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.06f), TextAnchor.MiddleCenter, new Color(0.55f, 0.85f, 1f));
            return menu;
        }

        MartialArtsHUD BuildHUD()
        {
            var canvasGO = NewCanvas("HUDCanvas", 1);
            var hud = canvasGO.AddComponent<MartialArtsHUD>();
            var root = AddPanel(canvasGO.transform, "HUDRoot", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);
            hud.hudRoot = root.gameObject;

            // Top bars
            var pHpBg = AddPanel(root, "PlayerHPBg", new Color(0,0,0,0.5f), new Vector2(0.02f, 0.92f), new Vector2(0.30f, 0.97f));
            hud.playerHealthBar = AddSimpleBar(pHpBg, "PlayerHP", new Color(0.85f, 0.10f, 0.10f));
            AddText(pHpBg, "PlayerLabel", "PLAYER", 14, FontStyle.Bold,
                new Vector2(0,0), new Vector2(1,1), TextAnchor.MiddleLeft, Color.white).rectTransform.offsetMin = new Vector2(8, 0);

            var pStBg = AddPanel(root, "PlayerStBg", new Color(0,0,0,0.5f), new Vector2(0.02f, 0.88f), new Vector2(0.30f, 0.91f));
            hud.playerStaminaBar = AddSimpleBar(pStBg, "PlayerST", new Color(0.20f, 0.80f, 0.30f));

            var oHpBg = AddPanel(root, "OppHPBg", new Color(0,0,0,0.5f), new Vector2(0.70f, 0.92f), new Vector2(0.98f, 0.97f));
            hud.opponentHealthBar = AddSimpleBar(oHpBg, "OppHP", new Color(0.85f, 0.10f, 0.10f));
            AddText(oHpBg, "OppLabel", "OPPONENT", 14, FontStyle.Bold,
                new Vector2(0,0), new Vector2(1,1), TextAnchor.MiddleRight, Color.white).rectTransform.offsetMax = new Vector2(-8, 0);

            var oStBg = AddPanel(root, "OppStBg", new Color(0,0,0,0.5f), new Vector2(0.70f, 0.88f), new Vector2(0.98f, 0.91f));
            hud.opponentStaminaBar = AddSimpleBar(oStBg, "OppST", new Color(0.20f, 0.80f, 0.30f));

            // Timer (top center)
            hud.timerText = AddText(root, "Timer", "60", 60, FontStyle.Bold,
                new Vector2(0.40f, 0.86f), new Vector2(0.60f, 0.99f), TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.3f));

            // Score (bottom right)
            hud.scoreText = AddText(root, "Score", "Score  0", 28, FontStyle.Bold,
                new Vector2(0.78f, 0.04f), new Vector2(0.99f, 0.10f), TextAnchor.MiddleRight, Color.white);

            // Style/Difficulty (bottom left)
            hud.styleText = AddText(root, "Style", "Style: Karate", 16, FontStyle.Normal,
                new Vector2(0.02f, 0.06f), new Vector2(0.30f, 0.10f), TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.25f));
            hud.difficultyText = AddText(root, "Difficulty", "Difficulty: Normal", 16, FontStyle.Normal,
                new Vector2(0.02f, 0.02f), new Vector2(0.30f, 0.06f), TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.95f));

            // Big center text
            hud.bigText = AddText(root, "BigText", "", 90, FontStyle.Bold,
                new Vector2(0.20f, 0.40f), new Vector2(0.80f, 0.65f), TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.3f));
            hud.bigText.gameObject.SetActive(false);

            // Feedback toast
            hud.feedbackText = AddText(root, "Feedback", "", 22, FontStyle.Bold,
                new Vector2(0.20f, 0.18f), new Vector2(0.80f, 0.24f), TextAnchor.MiddleCenter, Color.white);
            hud.feedbackText.gameObject.SetActive(false);

            // Combo text
            hud.comboText = AddText(root, "Combo", "", 32, FontStyle.Bold,
                new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.36f), TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.15f));
            hud.comboText.gameObject.SetActive(false);

            // Controls hint
            AddText(root, "Hints", "WASD move  |  Mouse look  |  LMB/RMB punches  |  E kick  |  Q special  |  Space dodge  |  LShift block  |  R restart  |  Esc menu", 12, FontStyle.Normal,
                new Vector2(0.05f, 0.0f), new Vector2(0.95f, 0.025f), TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));

            root.gameObject.SetActive(false);
            return hud;
        }

        GameObject BuildEndScreen()
        {
            var canvasGO = NewCanvas("EndScreenCanvas", 2);
            var bg = AddPanel(canvasGO.transform, "EndBg", new Color(0.04f, 0.04f, 0.06f, 0.92f), Vector2.zero, Vector2.one);

            replay.outcomeText = AddText(bg, "Outcome", "ROUND END", 70, FontStyle.Bold,
                new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.92f), TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.3f));
            replay.scoreText = AddText(bg, "Score", "Score: 0", 32, FontStyle.Bold,
                new Vector2(0.20f, 0.66f), new Vector2(0.80f, 0.74f), TextAnchor.MiddleCenter, Color.white);
            replay.accuracyText = AddText(bg, "Accuracy", "Accuracy: 0%", 24, FontStyle.Normal,
                new Vector2(0.20f, 0.58f), new Vector2(0.80f, 0.65f), TextAnchor.MiddleCenter, new Color(0.85f, 0.95f, 0.85f));
            replay.damageDealtText = AddText(bg, "DmgDealt", "Damage dealt: 0", 22, FontStyle.Normal,
                new Vector2(0.20f, 0.50f), new Vector2(0.80f, 0.57f), TextAnchor.MiddleCenter, Color.white);
            replay.damageTakenText = AddText(bg, "DmgTaken", "Damage taken: 0", 22, FontStyle.Normal,
                new Vector2(0.20f, 0.43f), new Vector2(0.80f, 0.50f), TextAnchor.MiddleCenter, Color.white);
            replay.coachText = AddText(bg, "Coach", "Coach:", 20, FontStyle.Normal,
                new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.40f), TextAnchor.UpperLeft, new Color(1f, 0.85f, 0.4f));
            replay.coachText.alignment = TextAnchor.UpperLeft;
            replay.instructionsText = AddText(bg, "Instructions", "Press R to fight again   |   Esc to return to menu", 18, FontStyle.Italic,
                new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.10f), TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));

            bg.gameObject.SetActive(false);
            return bg.gameObject;
        }

        GameObject BuildTrailerCanvas()
        {
            var canvasGO = NewCanvas("TrailerCanvas", 3);
            var root = AddPanel(canvasGO.transform, "TrailerRoot", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one);

            // Fade overlay
            var fadeGO = new GameObject("FadeOverlay", typeof(Image), typeof(CanvasGroup));
            fadeGO.transform.SetParent(root, false);
            var fr = fadeGO.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
            fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
            fadeGO.GetComponent<Image>().color = Color.black;
            trailer.fadeOverlay = fadeGO.GetComponent<CanvasGroup>();
            trailer.fadeOverlay.alpha = 0f;

            trailer.titleText = AddText(root, "TrailerTitle", "PROJECT 05", 100, FontStyle.Bold,
                new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.78f), TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.25f));
            trailer.subtitleText = AddText(root, "TrailerSubtitle", "MARTIAL ARTS SPARRING PARTNER", 36, FontStyle.Bold,
                new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.55f), TextAnchor.MiddleCenter, Color.white);
            trailer.featureText = AddText(root, "FeatureText", "", 56, FontStyle.Bold,
                new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.62f), TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.3f));
            trailer.endTagText = AddText(root, "EndTag", "", 24, FontStyle.Italic,
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.42f), TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.95f));

            trailer.titleText.gameObject.SetActive(false);
            trailer.subtitleText.gameObject.SetActive(false);
            trailer.featureText.gameObject.SetActive(false);
            trailer.endTagText.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ---------- Helpers ----------

        GameObject NewCanvas(string n, int order)
        {
            var go = new GameObject(n, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            return go;
        }

        // World-space canvas variant. Sized so 1920x1080 design at scale 0.0014
        // produces a ~2.7m x 1.5m readable panel in front of the player. Adds
        // TrackedDeviceGraphicRaycaster so XR controller rays can click buttons.
        GameObject NewWorldCanvas(string n, Vector3 worldPos, Vector3 worldEuler, float scale = 0.0014f)
        {
            var go = new GameObject(n, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 0;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1920, 1080);
            rt.position = worldPos;
            rt.rotation = Quaternion.Euler(worldEuler);
            rt.localScale = new Vector3(scale, scale, scale);
            // Keep XR raycaster for world-space XR UI interaction.
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            return go;
        }

        RectTransform AddPanel(Transform parent, string n, Color c, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(n, typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = c;
            return rt;
        }

        Text AddText(Transform parent, string n, string content, int size, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, TextAnchor align, Color c)
        {
            var go = new GameObject(n, typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.text = content;
            t.font = defaultFont;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = c;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var ol = go.GetComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.85f);
            ol.effectDistance = new Vector2(2, -2);
            return t;
        }

        Button AddButton(Transform parent, string n, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(n, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.22f, 0.9f);
            var btn = go.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.95f, 0.55f, 0.10f, 1f);
            cb.pressedColor = new Color(0.65f, 0.25f, 0.05f, 1f);
            cb.selectedColor = new Color(0.95f, 0.45f, 0.10f, 1f);
            btn.colors = cb;
            AddText(go.transform, "Label", label, 20, FontStyle.Bold,
                Vector2.zero, Vector2.one, TextAnchor.MiddleCenter, Color.white);
            return btn;
        }

        void ColorButton(Button b, Color c)
        {
            var img = b.GetComponent<Image>();
            if (img != null) img.color = c;
        }

        Slider AddSimpleBar(RectTransform parent, string n, Color fillColor)
        {
            // Background already exists (parent). Add a Slider with Fill.
            var go = new GameObject(n, typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);

            // Fill area
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero; fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = Vector2.zero; fillAreaRT.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = fillColor;
            fillImg.raycastTarget = false;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = null;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            return slider;
        }

        AudioClip MakeToneClip(string n, float duration, float frequency)
        {
            int sr = 22050;
            int samples = Mathf.RoundToInt(sr * duration);
            var clip = AudioClip.Create(n, samples, 1, sr, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 14f);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.45f * env;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
