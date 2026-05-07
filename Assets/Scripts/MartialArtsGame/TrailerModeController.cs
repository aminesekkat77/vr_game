using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MartialArtsGame
{
    // Scripted ~50s trailer with camera moves, text overlays and a slow-mo impact shot.
    public class TrailerModeController : MonoBehaviour
    {
        [Header("Root")]
        public GameObject trailerRoot;       // Canvas that holds trailer overlays
        public CanvasGroup fadeOverlay;      // Black fade overlay
        public Text titleText;
        public Text subtitleText;
        public Text featureText;
        public Text endTagText;

        [Header("References")]
        public SimpleCameraController fightCamera;
        public Transform player;
        public Transform opponent;
        public ProceduralMartialArtsAnimator opponentRig;
        public ProceduralMartialArtsAnimator playerRig;

        bool running;
        Coroutine routine;

        public void StartTrailer()
        {
            if (running) return;
            if (trailerRoot != null) trailerRoot.SetActive(true);
            running = true;
            routine = StartCoroutine(Run());
        }

        public void StopTrailer()
        {
            if (routine != null) StopCoroutine(routine);
            if (trailerRoot != null) trailerRoot.SetActive(false);
            Time.timeScale = 1f;
            running = false;
        }

        IEnumerator Run()
        {
            ClearTexts();
            yield return Fade(1f, 0f, 0.6f);

            // Beat 1: title
            if (fightCamera != null) fightCamera.UseTrailerView(0);
            yield return ShowTitle("PROJECT 05", "MARTIAL ARTS SPARRING PARTNER", 3.0f);
            // Beat 2: tagline
            yield return ShowFeature("Always ready. Never tired.", 3.0f);
            // Beat 3: orbit + opponent stance
            if (fightCamera != null) fightCamera.UseTrailerView(1);
            if (opponentRig != null) opponentRig.SetGuard(true);
            yield return new WaitForSeconds(0.4f);
            if (opponentRig != null) opponentRig.PlayAttack(MoveType.LeftPunch);
            yield return new WaitForSeconds(0.6f);
            if (opponentRig != null) opponentRig.PlayAttack(MoveType.RightPunch);
            yield return new WaitForSeconds(0.6f);
            if (opponentRig != null) opponentRig.PlayAttack(MoveType.RoundKick);
            yield return ShowFeature("Multi-discipline Combat", 2.5f);

            // Beat 4: exchange
            if (fightCamera != null) fightCamera.UseTrailerView(2);
            for (int i = 0; i < 3; i++)
            {
                if (playerRig != null) playerRig.PlayAttack(MoveType.LeftPunch);
                yield return new WaitForSeconds(0.4f);
                if (opponentRig != null) opponentRig.PlayAttack(MoveType.Kick);
                yield return new WaitForSeconds(0.4f);
            }
            yield return ShowFeature("Adaptive AI Opponent", 2.5f);

            // Beat 5: progressive difficulty
            if (fightCamera != null) fightCamera.UseTrailerView(3);
            yield return ShowFeature("Progressive Difficulty", 2.0f);

            // Beat 6: slow-mo impact
            Time.timeScale = 0.35f;
            if (fightCamera != null) fightCamera.UseTrailerView(4);
            if (opponentRig != null) opponentRig.PlayAttack(MoveType.RoundKick);
            if (playerRig != null) playerRig.PlayHitFlash(false);
            yield return new WaitForSecondsRealtime(2.0f);
            Time.timeScale = 1f;
            yield return ShowFeature("Slow-motion Coach Replay", 2.5f);

            // Beat 7: closing
            if (fightCamera != null) fightCamera.UseTrailerView(5);
            yield return ShowFeature("Train anywhere. Your dojo is everywhere.", 3.0f);
            yield return ShowFinal("MARTIAL ARTS SPARRING PARTNER", "Meta Quest / Apple Vision Pro Concept", 3.5f);

            yield return Fade(0f, 1f, 0.8f);
            if (MartialArtsGameManager.Instance != null) MartialArtsGameManager.Instance.ReturnToMenuFromTrailer();
            running = false;
        }

        IEnumerator Fade(float from, float to, float dur)
        {
            if (fadeOverlay == null) yield break;
            float t = 0f;
            fadeOverlay.alpha = from;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            fadeOverlay.alpha = to;
        }

        IEnumerator ShowTitle(string main, string sub, float dur)
        {
            ClearTexts();
            if (titleText != null) { titleText.text = main; titleText.gameObject.SetActive(true); }
            if (subtitleText != null) { subtitleText.text = sub; subtitleText.gameObject.SetActive(true); }
            yield return new WaitForSeconds(dur);
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (subtitleText != null) subtitleText.gameObject.SetActive(false);
        }

        IEnumerator ShowFeature(string text, float dur)
        {
            if (featureText != null) { featureText.text = text; featureText.gameObject.SetActive(true); }
            yield return new WaitForSeconds(dur);
            if (featureText != null) featureText.gameObject.SetActive(false);
        }

        IEnumerator ShowFinal(string main, string tag, float dur)
        {
            if (titleText != null) { titleText.text = main; titleText.gameObject.SetActive(true); }
            if (endTagText != null) { endTagText.text = tag; endTagText.gameObject.SetActive(true); }
            yield return new WaitForSeconds(dur);
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (endTagText != null) endTagText.gameObject.SetActive(false);
        }

        void ClearTexts()
        {
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (subtitleText != null) subtitleText.gameObject.SetActive(false);
            if (featureText != null) featureText.gameObject.SetActive(false);
            if (endTagText != null) endTagText.gameObject.SetActive(false);
        }
    }
}
