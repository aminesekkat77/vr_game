using System.Collections.Generic;
using UnityEngine;

namespace MartialArtsGame
{
    // Tracks score and statistics for the round, drives coach feedback.
    public class ScoreAndStatsManager : MonoBehaviour
    {
        public static ScoreAndStatsManager Instance;

        public int score = 0;
        public int attacksThrown = 0;
        public int attacksLanded = 0;
        public int blocks = 0;
        public int dodges = 0;
        public int dodgesIntoAttack = 0;
        public int aiDefended = 0;
        public float damageDealt = 0f;
        public float damageTaken = 0f;
        public float lastHitTime = -1f;
        public int comboCount = 0;
        public float comboWindow = 1.6f;
        public List<string> coachMessages = new List<string>();

        void Awake() { if (Instance == null) Instance = this; }

        public void ResetRound()
        {
            score = 0; attacksThrown = 0; attacksLanded = 0; blocks = 0; dodges = 0;
            dodgesIntoAttack = 0; aiDefended = 0; damageDealt = 0; damageTaken = 0;
            comboCount = 0; lastHitTime = -1f; coachMessages.Clear();
        }

        public static void Notify(string evt)
        {
            if (Instance == null) return;
            switch (evt)
            {
                case "block": Instance.blocks++; Instance.score += 5; break;
                case "dodge": Instance.dodges++; break;
                case "dodge_into_attack": Instance.dodgesIntoAttack++; Instance.score += 25;
                    if (MartialArtsHUD.Instance != null) MartialArtsHUD.Instance.ShowFeedback("Perfect dodge!"); break;
                case "ai_defended": Instance.aiDefended++; break;
            }
            if (MartialArtsHUD.Instance != null) MartialArtsHUD.Instance.SetScore(Instance.score);
        }

        public static void Hit(MartialArtsMove move, float dmg)
        {
            if (Instance == null) return;
            int gain = Mathf.RoundToInt(move.scoreValue);
            // Combo bonus.
            if (Time.time - Instance.lastHitTime < Instance.comboWindow) { Instance.comboCount++; gain += 5 * Instance.comboCount; }
            else Instance.comboCount = 1;
            Instance.lastHitTime = Time.time;
            // Counter bonus: if player just got hit < 0.6s ago and lands a hit.
            if (move.moveType == MoveType.Special) gain += 10;
            Instance.score += gain;
            if (MartialArtsHUD.Instance != null)
            {
                MartialArtsHUD.Instance.SetScore(Instance.score);
                MartialArtsHUD.Instance.ShowFeedback("+" + gain + " " + move.displayName);
            }
        }

        public static void AddDamageDealt(float amount)
        {
            if (Instance == null) return;
            Instance.damageDealt += amount;
        }

        public static void AddDamageTaken(float amount)
        {
            if (Instance == null) return;
            Instance.damageTaken += amount;
        }

        public List<string> BuildCoachFeedback()
        {
            var msgs = new List<string>();
            float accuracy = (attacksThrown == 0) ? 0f : (float)attacksLanded / attacksThrown;
            if (accuracy > 0.55f) msgs.Add("Great accuracy — you read the AI well.");
            else if (accuracy > 0.30f) msgs.Add("Solid accuracy. Mix up your strikes more.");
            else msgs.Add("Be patient — wait for openings before attacking.");
            if (blocks >= 3) msgs.Add("Good defense. Counter after a block to score more.");
            else msgs.Add("Try blocking with Left Shift when the AI swings.");
            if (dodgesIntoAttack >= 1) msgs.Add("Perfect dodge timing — keep that up!");
            if (damageTaken > damageDealt + 20f) msgs.Add("You dropped your guard after attacking.");
            if (comboCount >= 3) msgs.Add("Nice combo chain.");
            if (msgs.Count < 3) msgs.Add("Try stepping out after your combo.");
            return msgs;
        }
    }
}
