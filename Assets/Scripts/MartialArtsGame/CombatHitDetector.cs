using UnityEngine;

namespace MartialArtsGame
{
    // Pure utility: range + angle checks, applies damage, raises score events.
    public static class CombatHitDetector
    {
        const float HitArcDeg = 75f; // half-arc around forward considered "in front"

        public static bool TryLandHit(Transform attacker, AIOpponentController target, MartialArtsMove move, float damageMul)
        {
            if (attacker == null || target == null) return false;
            Vector3 to = target.transform.position - attacker.position; to.y = 0f;
            float dist = to.magnitude;
            if (dist > move.range) return false;
            float ang = Vector3.Angle(attacker.forward, to);
            if (ang > HitArcDeg) return false;
            // AI may block/dodge.
            if (target.TryDefend())
            {
                ScoreAndStatsManager.Notify("ai_defended");
                if (MartialArtsHUD.Instance != null) MartialArtsHUD.Instance.ShowFeedback("AI blocked!");
                return false;
            }
            float dmg = move.damage * damageMul;
            target.TakeDamage(dmg, attacker.position);
            ScoreAndStatsManager.Hit(move, dmg);
            if (MartialArtsHUD.Instance != null) MartialArtsHUD.Instance.ShowComboText(move.displayName + "!");
            return true;
        }

        public static void TryLandHitOnPlayer(Transform attacker, PlayerCombatController target, MartialArtsMove move, float damageMul)
        {
            if (attacker == null || target == null) return;
            Vector3 to = target.transform.position - attacker.position; to.y = 0f;
            float dist = to.magnitude;
            if (dist > move.range) return;
            float ang = Vector3.Angle(attacker.forward, to);
            if (ang > HitArcDeg) return;
            float dmg = move.damage * damageMul;
            target.TakeDamage(dmg, attacker.position);
            if (MartialArtsHUD.Instance != null) MartialArtsHUD.Instance.ShowFeedback("Keep your guard up!");
        }
    }
}
