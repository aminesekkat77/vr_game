using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MartialArtsGame
{
    // After a round, runs a slow-mo cinematic moment then shows an end card with stats and coach feedback.
    public class CoachReplayController : MonoBehaviour
    {
        [Header("Root")]
        public GameObject endScreenRoot;

        [Header("End screen texts")]
        public Text outcomeText;
        public Text scoreText;
        public Text accuracyText;
        public Text damageDealtText;
        public Text damageTakenText;
        public Text coachText;
        public Text instructionsText;

        [Header("Camera")]
        public SimpleCameraController fightCamera;
        public Transform replayPivot;     // Aim camera at this point during slow-mo

        public bool IsActive { get; private set; }

        public void HideAll()
        {
            if (endScreenRoot != null) endScreenRoot.SetActive(false);
            Time.timeScale = 1f;
            IsActive = false;
        }

        public void PlayReplay(string outcome, ScoreAndStatsManager stats)
        {
            StartCoroutine(ReplayRoutine(outcome, stats));
        }

        IEnumerator ReplayRoutine(string outcome, ScoreAndStatsManager stats)
        {
            IsActive = true;
            // Slow-mo phase
            Time.timeScale = 0.35f;
            if (fightCamera != null) fightCamera.UseReplayView();

            yield return new WaitForSecondsRealtime(2.4f);
            Time.timeScale = 1f;

            // Show end screen
            if (endScreenRoot != null) endScreenRoot.SetActive(true);
            if (outcomeText != null) outcomeText.text = outcome;
            if (scoreText != null && stats != null) scoreText.text = "Score: " + stats.score;
            if (accuracyText != null && stats != null)
            {
                float acc = (stats.attacksThrown == 0) ? 0f : (float)stats.attacksLanded / stats.attacksThrown;
                accuracyText.text = "Accuracy: " + Mathf.RoundToInt(acc * 100f) + "%  (" + stats.attacksLanded + "/" + stats.attacksThrown + ")";
            }
            if (damageDealtText != null && stats != null) damageDealtText.text = "Damage dealt: " + Mathf.RoundToInt(stats.damageDealt);
            if (damageTakenText != null && stats != null) damageTakenText.text = "Damage taken: " + Mathf.RoundToInt(stats.damageTaken);
            if (coachText != null && stats != null)
            {
                var msgs = stats.BuildCoachFeedback();
                coachText.text = "Coach:\n  - " + string.Join("\n  - ", msgs);
            }
            if (instructionsText != null) instructionsText.text = "Press R to fight again   |   Esc to return to menu";
        }
    }
}
