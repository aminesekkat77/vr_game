using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MartialArtsGame
{
    public class MartialArtsHUD : MonoBehaviour
    {
        public static MartialArtsHUD Instance;

        [Header("Root")]
        public GameObject hudRoot;

        [Header("Bars")]
        public Slider playerHealthBar;
        public Slider opponentHealthBar;
        public Slider playerStaminaBar;
        public Slider opponentStaminaBar;

        [Header("Texts")]
        public Text scoreText;
        public Text timerText;
        public Text styleText;
        public Text difficultyText;
        public Text bigText;
        public Text feedbackText;
        public Text comboText;

        [Header("References")]
        public PlayerCombatController player;
        public AIOpponentController opponent;
        public MartialArtsGameManager game;

        Coroutine bigTextRoutine;
        Coroutine feedbackRoutine;
        Coroutine comboRoutine;

        readonly string[] styleNames = { "Karate", "Muay Thai", "MMA", "Brazilian Jiu-Jitsu" };
        readonly string[] difficultyNames = { "Easy", "Normal", "Hard", "Adaptive AI" };

        void Awake() { if (Instance == null) Instance = this; }

        public void SetVisible(bool v) { if (hudRoot != null) hudRoot.SetActive(v); }

        public void SetStyleAndDifficulty(MartialArtStyle style, DifficultyLevel diff)
        {
            if (styleText != null) styleText.text = "Style: " + styleNames[(int)style];
            if (difficultyText != null) difficultyText.text = "Difficulty: " + difficultyNames[(int)diff];
        }

        void Update()
        {
            if (player != null)
            {
                if (playerHealthBar != null) playerHealthBar.value = player.health / player.maxHealth;
                if (playerStaminaBar != null) playerStaminaBar.value = player.stamina / player.maxStamina;
            }
            if (opponent != null)
            {
                if (opponentHealthBar != null) opponentHealthBar.value = opponent.health / opponent.maxHealth;
                if (opponentStaminaBar != null) opponentStaminaBar.value = opponent.stamina / opponent.maxStamina;
            }
            if (game != null && timerText != null)
                timerText.text = Mathf.CeilToInt(game.roundTimer).ToString();
        }

        public void SetScore(int s) { if (scoreText != null) scoreText.text = "Score  " + s; }

        public void ShowBigText(string msg, float duration)
        {
            if (bigText == null) return;
            if (bigTextRoutine != null) StopCoroutine(bigTextRoutine);
            bigTextRoutine = StartCoroutine(BigTextRoutine(msg, duration));
        }

        IEnumerator BigTextRoutine(string msg, float duration)
        {
            bigText.text = msg;
            bigText.gameObject.SetActive(true);
            float t = 0f;
            Vector3 baseScale = Vector3.one * 1.2f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                bigText.transform.localScale = Vector3.Lerp(baseScale * 0.8f, baseScale, k);
                bigText.color = new Color(1f, 0.9f, 0.3f, 1f - (k * 0.4f));
                yield return null;
            }
            bigText.gameObject.SetActive(false);
        }

        public void ShowFeedback(string msg)
        {
            if (feedbackText == null) return;
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(FeedbackRoutine(msg));
        }

        IEnumerator FeedbackRoutine(string msg)
        {
            feedbackText.text = msg;
            feedbackText.gameObject.SetActive(true);
            float t = 0f;
            while (t < 1.6f)
            {
                t += Time.unscaledDeltaTime;
                feedbackText.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1.6f - t));
                yield return null;
            }
            feedbackText.gameObject.SetActive(false);
        }

        public void ShowComboText(string msg)
        {
            if (comboText == null) return;
            if (comboRoutine != null) StopCoroutine(comboRoutine);
            comboRoutine = StartCoroutine(ComboRoutine(msg));
        }

        IEnumerator ComboRoutine(string msg)
        {
            comboText.text = msg;
            comboText.gameObject.SetActive(true);
            float t = 0f;
            while (t < 0.9f)
            {
                t += Time.unscaledDeltaTime;
                comboText.color = new Color(1f, 0.55f, 0.15f, Mathf.Clamp01(0.9f - t));
                yield return null;
            }
            comboText.gameObject.SetActive(false);
        }
    }
}
