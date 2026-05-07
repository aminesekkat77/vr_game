using UnityEngine;

namespace MartialArtsGame
{
    public class AIOpponentController : MonoBehaviour
    {
        public enum AIState { IdleGuard, Approach, Circle, Attack, Combo, Block, Dodge, HitReaction, Retreat, Knockdown, RoundEnd }

        [Header("Stats")]
        public float maxHealth = 100f;
        public float health = 100f;
        public float maxStamina = 100f;
        public float stamina = 100f;

        [Header("Behaviour")]
        public float preferredDistance = 2.0f;
        public float moveSpeed = 1.9f;
        public float strafeSpeed = 1.3f;
        public float reactionTime = 0.45f;
        public float aggression = 0.5f;       // 0..1 chance influence on attacking
        public float defenseSkill = 0.5f;     // 0..1 block/dodge chance
        public float damageMultiplier = 1f;
        public float attackCooldown = 1.6f;

        [Header("References")]
        public Transform playerTransform;
        public PlayerCombatController player;
        public ProceduralMartialArtsAnimator animatorRig;
        public AudioSource audioSource;
        public AudioClip swingClip;
        public AudioClip blockClip;

        [Header("Style / Difficulty")]
        public MartialArtStyle style = MartialArtStyle.Karate;
        public DifficultyLevel difficulty = DifficultyLevel.Normal;

        public AIState CurrentState = AIState.IdleGuard;
        bool combatActive = false;
        float stateTimer = 0f;
        float nextDecisionAt = 0f;
        float attackLockUntil = 0f;
        float circleSign = 1f;
        float adaptiveTimer = 0f;
        Vector3 startPos;
        Quaternion startRot;

        public void PrepareForFight(MartialArtStyle newStyle, DifficultyLevel diff)
        {
            style = newStyle;
            difficulty = diff;
            health = maxHealth;
            stamina = maxStamina;
            CurrentState = AIState.IdleGuard;
            ApplyDifficulty();
        }

        void Awake()
        {
            startPos = transform.position;
            startRot = transform.rotation;
        }

        void OnEnable()
        {
            // Reset to standing if we got knocked over previously.
            transform.rotation = startRot;
        }

        void ApplyDifficulty()
        {
            switch (difficulty)
            {
                case DifficultyLevel.Easy:
                    reactionTime = 0.65f; aggression = 0.30f; defenseSkill = 0.25f;
                    damageMultiplier = 0.7f; attackCooldown = 2.1f; moveSpeed = 1.6f; break;
                case DifficultyLevel.Normal:
                    reactionTime = 0.40f; aggression = 0.55f; defenseSkill = 0.45f;
                    damageMultiplier = 1.0f; attackCooldown = 1.5f; moveSpeed = 2.0f; break;
                case DifficultyLevel.Hard:
                    reactionTime = 0.22f; aggression = 0.80f; defenseSkill = 0.70f;
                    damageMultiplier = 1.25f; attackCooldown = 1.05f; moveSpeed = 2.4f; break;
                case DifficultyLevel.Adaptive:
                    reactionTime = 0.40f; aggression = 0.50f; defenseSkill = 0.50f;
                    damageMultiplier = 1.0f; attackCooldown = 1.5f; moveSpeed = 2.0f; break;
            }
        }

        public void SetCombatActive(bool active)
        {
            combatActive = active;
            if (!active) CurrentState = AIState.IdleGuard;
        }

        void Update()
        {
            // Slight stamina regen.
            stamina = Mathf.Min(maxStamina, stamina + 12f * Time.deltaTime);
            if (animatorRig != null) animatorRig.SetGuard(CurrentState == AIState.Block || CurrentState == AIState.IdleGuard);

            if (!combatActive) return;

            FacePlayer();
            stateTimer += Time.deltaTime;
            if (difficulty == DifficultyLevel.Adaptive)
            {
                adaptiveTimer += Time.deltaTime;
                if (adaptiveTimer >= 6f)
                {
                    adaptiveTimer = 0f;
                    AdaptDifficulty();
                }
            }

            switch (CurrentState)
            {
                case AIState.IdleGuard:  TickIdle(); break;
                case AIState.Approach:   TickApproach(); break;
                case AIState.Circle:     TickCircle(); break;
                case AIState.Attack:     TickAttack(); break;
                case AIState.Combo:      TickAttack(); break;
                case AIState.Block:      TickBlock(); break;
                case AIState.Dodge:      TickDodge(); break;
                case AIState.HitReaction:TickHitReaction(); break;
                case AIState.Retreat:    TickRetreat(); break;
                case AIState.Knockdown:  break;
            }
        }

        void FacePlayer()
        {
            if (playerTransform == null) return;
            Vector3 to = playerTransform.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 6f * Time.deltaTime);
        }

        float DistanceToPlayer()
        {
            if (playerTransform == null) return 99f;
            return Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                    new Vector3(playerTransform.position.x, 0, playerTransform.position.z));
        }

        void TickIdle()
        {
            if (Time.time < nextDecisionAt) return;
            nextDecisionAt = Time.time + reactionTime + Random.Range(0f, 0.3f);
            float d = DistanceToPlayer();
            float roll = Random.value;
            if (d > preferredDistance + 0.3f) ChangeState(AIState.Approach);
            else if (d < preferredDistance - 0.4f) ChangeState(AIState.Retreat);
            else if (roll < aggression && Time.time > attackLockUntil) ChangeState(AIState.Attack);
            else if (roll < aggression + 0.2f) { circleSign = Random.value < 0.5f ? -1f : 1f; ChangeState(AIState.Circle); }
        }

        void TickApproach()
        {
            float d = DistanceToPlayer();
            if (d <= preferredDistance) { ChangeState(AIState.IdleGuard); return; }
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            if (animatorRig != null) animatorRig.SetMovement(true, 0f);
            if (stateTimer > 1.5f) ChangeState(AIState.IdleGuard);
        }

        void TickRetreat()
        {
            float d = DistanceToPlayer();
            if (d >= preferredDistance) { ChangeState(AIState.IdleGuard); return; }
            transform.position -= transform.forward * moveSpeed * 0.7f * Time.deltaTime;
            if (animatorRig != null) animatorRig.SetMovement(true, 0f);
            if (stateTimer > 0.6f) ChangeState(AIState.IdleGuard);
        }

        void TickCircle()
        {
            transform.position += transform.right * circleSign * strafeSpeed * Time.deltaTime;
            if (animatorRig != null) animatorRig.SetMovement(true, circleSign);
            if (stateTimer > 0.9f) ChangeState(AIState.IdleGuard);
        }

        void TickAttack()
        {
            if (Time.time < attackLockUntil) return;
            // Pick a move from style.
            var moves = MartialArtsMoveSet.GetMoves(style);
            var move = moves[Random.Range(0, moves.Count)];
            stamina -= move.staminaCost;
            attackLockUntil = Time.time + move.startupTime + move.activeTime + move.recoveryTime + attackCooldown * (0.7f + 0.4f * Random.value);
            if (animatorRig != null) animatorRig.PlayAttack(move.moveType);
            if (audioSource != null && swingClip != null) audioSource.PlayOneShot(swingClip, 0.6f);

            StartCoroutine(ResolveAttack(move));

            // Combo chance based on aggression.
            if (Random.value < aggression * 0.6f) StartCoroutine(QueueCombo());
            else ChangeState(AIState.IdleGuard);
        }

        System.Collections.IEnumerator QueueCombo()
        {
            yield return new WaitForSeconds(0.6f);
            CurrentState = AIState.Combo;
            stateTimer = 0f;
        }

        System.Collections.IEnumerator ResolveAttack(MartialArtsMove move)
        {
            yield return new WaitForSeconds(move.startupTime);
            // Fire hit window
            CombatHitDetector.TryLandHitOnPlayer(transform, player, move, damageMultiplier);
        }

        void TickBlock()
        {
            if (stateTimer > 0.6f) ChangeState(AIState.IdleGuard);
        }

        void TickDodge()
        {
            transform.position += transform.right * (Random.value < 0.5f ? -1f : 1f) * 4f * Time.deltaTime;
            if (animatorRig != null) animatorRig.PlayDodge();
            if (stateTimer > 0.35f) ChangeState(AIState.IdleGuard);
        }

        void TickHitReaction()
        {
            if (stateTimer > 0.35f) ChangeState(Random.value < 0.5f ? AIState.IdleGuard : AIState.Retreat);
        }

        void ChangeState(AIState s) { CurrentState = s; stateTimer = 0f; }

        // Called by CombatHitDetector when player tries to hit AI.
        // Returns whether the AI defended (block/dodge) so detector can report.
        public bool TryDefend()
        {
            float roll = Random.value;
            if (roll < defenseSkill * 0.55f)
            {
                ChangeState(AIState.Block);
                if (audioSource != null && blockClip != null) audioSource.PlayOneShot(blockClip, 0.5f);
                if (animatorRig != null) animatorRig.PlayHitFlash(true);
                return true;
            }
            if (roll < defenseSkill * 0.8f)
            {
                ChangeState(AIState.Dodge);
                return true;
            }
            return false;
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            health = Mathf.Max(0f, health - amount);
            ChangeState(AIState.HitReaction);
            if (animatorRig != null) animatorRig.PlayHitFlash(false);
            ScoreAndStatsManager.AddDamageDealt(amount);
            if (Camera.main != null)
            {
                var shaker = Camera.main.GetComponent<SimpleCameraController>();
                if (shaker != null) shaker.Shake(0.22f, 0.08f);
            }
            if (health <= 0f) ChangeState(AIState.Knockdown);
        }

        void AdaptDifficulty()
        {
            if (player == null || ScoreAndStatsManager.Instance == null) return;
            var s = ScoreAndStatsManager.Instance;
            float accuracy = (s.attacksThrown == 0) ? 0f : (float)s.attacksLanded / s.attacksThrown;
            bool playerWinning = (player.health > 70f && accuracy > 0.4f);
            bool playerStruggling = (player.health < 35f);

            if (playerWinning)
            {
                aggression = Mathf.Min(1f, aggression + 0.12f);
                damageMultiplier = Mathf.Min(1.5f, damageMultiplier + 0.08f);
                reactionTime = Mathf.Max(0.18f, reactionTime - 0.04f);
                if (MartialArtsHUD.Instance != null) MartialArtsHUD.Instance.ShowFeedback("AI adapting… pressure up");
            }
            else if (playerStruggling)
            {
                aggression = Mathf.Max(0.2f, aggression - 0.10f);
                damageMultiplier = Mathf.Max(0.6f, damageMultiplier - 0.08f);
                reactionTime = Mathf.Min(0.7f, reactionTime + 0.05f);
                if (MartialArtsHUD.Instance != null) MartialArtsHUD.Instance.ShowFeedback("AI adapting… easing off");
            }
        }
    }
}
