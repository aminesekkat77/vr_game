using UnityEngine;
using UnityEngine.UI;

namespace MartialArtsGame
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Root")]
        public GameObject menuRoot;

        [Header("Style Buttons (Karate, MuayThai, MMA, BJJ)")]
        public Button[] styleButtons = new Button[4];
        public Text selectedStyleLabel;

        [Header("Difficulty Buttons (Easy, Normal, Hard, Adaptive)")]
        public Button[] difficultyButtons = new Button[4];
        public Text selectedDifficultyLabel;

        [Header("Action Buttons")]
        public Button startButton;
        public Button trailerButton;
        public Button quitButton;

        readonly string[] styleNames = { "Karate", "Muay Thai", "MMA", "Brazilian Jiu-Jitsu" };
        readonly string[] difficultyNames = { "Easy", "Normal", "Hard", "Adaptive AI" };

        void Start()
        {
            for (int i = 0; i < styleButtons.Length; i++)
            {
                int idx = i;
                if (styleButtons[i] != null) styleButtons[i].onClick.AddListener(() => SelectStyle(idx));
            }
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                int idx = i;
                if (difficultyButtons[i] != null) difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(idx));
            }
            if (startButton != null) startButton.onClick.AddListener(StartFightPressed);
            if (trailerButton != null) trailerButton.onClick.AddListener(TrailerPressed);
            if (quitButton != null) quitButton.onClick.AddListener(QuitPressed);

            SelectStyle(0);
            SelectDifficulty(1);
            isShowing = true;
        }

        public void Show(bool visible)
        {
            if (menuRoot != null) menuRoot.SetActive(visible);
            isShowing = visible;
        }

        bool isShowing;
        int currentStyleIdx = 0;
        int currentDifficultyIdx = 1;

        // Keyboard fallback: 1/2/3/4 select style, F1/F2/F3/F4 select difficulty,
        // Enter starts the fight, T launches trailer, Esc/Q quits to menu.
        // Useful when running with the XR Device Simulator where controller rays
        // don't always hit overlay UI.
        void Update()
        {
            if (!isShowing) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectStyle(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectStyle(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectStyle(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectStyle(3);
            if (Input.GetKeyDown(KeyCode.F1)) SelectDifficulty(0);
            else if (Input.GetKeyDown(KeyCode.F2)) SelectDifficulty(1);
            else if (Input.GetKeyDown(KeyCode.F3)) SelectDifficulty(2);
            else if (Input.GetKeyDown(KeyCode.F4)) SelectDifficulty(3);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                StartFightPressed();
            if (Input.GetKeyDown(KeyCode.T))
                TrailerPressed();
            if (Input.GetKeyDown(KeyCode.Escape))
                QuitPressed();
        }

        void SelectStyle(int idx)
        {
            currentStyleIdx = idx;
            if (MartialArtsGameManager.Instance != null) MartialArtsGameManager.Instance.OnSelectStyle(idx);
            if (selectedStyleLabel != null) selectedStyleLabel.text = "Style: " + styleNames[idx];
            HighlightSelected(styleButtons, idx);
        }

        void SelectDifficulty(int idx)
        {
            currentDifficultyIdx = idx;
            if (MartialArtsGameManager.Instance != null) MartialArtsGameManager.Instance.OnSelectDifficulty(idx);
            if (selectedDifficultyLabel != null) selectedDifficultyLabel.text = "Difficulty: " + difficultyNames[idx];
            HighlightSelected(difficultyButtons, idx);
        }

        void HighlightSelected(Button[] buttons, int idx)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var img = buttons[i].GetComponent<Image>();
                if (img != null) img.color = (i == idx) ? new Color(0.95f, 0.45f, 0.10f, 1f) : new Color(0.18f, 0.18f, 0.22f, 0.9f);
            }
        }

        void StartFightPressed()
        {
            if (MartialArtsGameManager.Instance != null) MartialArtsGameManager.Instance.StartFight();
        }

        void TrailerPressed()
        {
            if (MartialArtsGameManager.Instance != null) MartialArtsGameManager.Instance.StartTrailer();
        }

        void QuitPressed()
        {
            if (MartialArtsGameManager.Instance != null) MartialArtsGameManager.Instance.OnQuit();
        }
    }
}
