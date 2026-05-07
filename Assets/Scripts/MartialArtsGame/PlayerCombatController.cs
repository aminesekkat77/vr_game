using UnityEngine;

namespace MartialArtsGame
{
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("Stats")]
        public float maxHealth = 100f;
        public float health = 100f;
        public float maxStamina = 100f;
        public float stamina = 100f;
        public float staminaRegenPerSec = 14f;

        [Header("Movement (keyboard fallback)")]
        public float moveSpeed = 3.2f;
        public float lookSensitivity = 2.4f;
        public float dodgeImpulse = 5.5f;
        public float dodgeStaminaCost = 14f;
        public float blockStaminaDrainPerSec = 9f;

        [Header("References")]
        public Transform aim;                  // Forward used for hit angle checks (camera transform)
        public Transform opponentTransform;    // Set by GameManager when fight starts
        public AIOpponentController opponent;
        public ProceduralMartialArtsAnimator animatorRig;
        public CharacterController vrCharacter; // Optional: XR Origin's CharacterController
        public AudioSource audioSource;
        public AudioClip swingClip;
        public AudioClip blockClip;

        [Header("Style")]
        public MartialArtStyle style = MartialArtStyle.Karate;

        // Runtime state
        public bool combatActive = false;
        public bool isBlocking = false;
        public bool isDodging = false;
        public float dodgeTimer = 0f;
        public float attackLockUntil = 0f;
        public float pitch = 0f;
        Vector3 dodgeVelocity;

        // For adaptive AI
        public int attacksThrown = 0;
        public int attacksLanded = 0;
        public float lastAttackTime = 0f;

        public void PrepareForFight(MartialArtStyle newStyle)
        {
            style = newStyle;
            health = maxHealth;
            stamina = maxStamina;
            isBlocking = false;
            isDodging = false;
            attackLockUntil = 0f;
            attacksThrown = 0;
            attacksLanded = 0;
        }

        public void SetCombatActive(bool active) { combatActive = active; }

        void Update()
        {
            // Stamina regen always.
            stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSec * Time.deltaTime);
            if (isBlocking) stamina = Mathf.Max(0f, stamina - blockStaminaDrainPerSec * Time.deltaTime);

            if (!combatActive) return;
            HandleLook();
            HandleMovement();
            HandleCombatInput();
            FaceOpponent();
        }

        void HandleLook()
        {
            // Mouse look with keyboard fallback (Left/Right arrows).
            float yaw = Input.GetAxis("Mouse X") * lookSensitivity;
            if (Mathf.Abs(yaw) < 0.0001f)
            {
                if (Input.GetKey(KeyCode.LeftArrow)) yaw = -90f * Time.deltaTime;
                else if (Input.GetKey(KeyCode.RightArrow)) yaw = 90f * Time.deltaTime;
            }
            transform.Rotate(0f, yaw, 0f, Space.World);
            if (aim != null)
            {
                float mousePitch = Input.GetAxis("Mouse Y") * lookSensitivity;
                if (Mathf.Abs(mousePitch) < 0.0001f)
                {
                    if (Input.GetKey(KeyCode.UpArrow)) mousePitch = -75f * Time.deltaTime;
                    else if (Input.GetKey(KeyCode.DownArrow)) mousePitch = 75f * Time.deltaTime;
                }
                pitch = Mathf.Clamp(pitch - mousePitch, -35f, 35f);
                aim.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        void HandleMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Hard keyboard fallback (works even if Horizontal/Vertical axes are not configured).
            if (Mathf.Abs(h) < 0.0001f)
            {
                bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q);
                bool right = Input.GetKey(KeyCode.D);
                h = (right ? 1f : 0f) - (left ? 1f : 0f);
            }
            if (Mathf.Abs(v) < 0.0001f)
            {
                bool forward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z);
                bool backward = Input.GetKey(KeyCode.S);
                v = (forward ? 1f : 0f) - (backward ? 1f : 0f);
            }

            Vector3 dir = (transform.right * h + transform.forward * v).normalized;
            Vector3 step = dir * moveSpeed * Time.deltaTime;

            if (isDodging)
            {
                dodgeTimer -= Time.deltaTime;
                step += dodgeVelocity * Time.deltaTime;
                dodgeVelocity = Vector3.Lerp(dodgeVelocity, Vector3.zero, 8f * Time.deltaTime);
                if (dodgeTimer <= 0f) isDodging = false;
            }

            if (vrCharacter != null && vrCharacter.enabled)
            {
                vrCharacter.Move(step + Vector3.down * 0.1f);
            }
            else
            {
                transform.position += step;
            }
        }

        void FaceOpponent()
        {
            if (opponentTransform == null) return;
            Vector3 to = opponentTransform.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 4f * Time.deltaTime);
        }

        void HandleCombatInput()
        {
            isBlocking = Input.GetKey(KeyCode.LeftShift) && stamina > 1f;
            if (animatorRig != null) animatorRig.SetGuard(isBlocking);

            if (Input.GetKeyDown(KeyCode.Space) && !isDodging && stamina >= dodgeStaminaCost)
            {
                isDodging = true;
                dodgeTimer = 0.35f;
                dodgeVelocity = -transform.forward * dodgeImpulse;
                stamina -= dodgeStaminaCost;
                if (animatorRig != null) animatorRig.PlayDodge();
                ScoreAndStatsManager.Notify("dodge");
            }

            if (Time.time < attackLockUntil) return;

            if (Input.GetMouseButtonDown(0)) TryAttack(MoveType.LeftPunch);
            else if (Input.GetMouseButtonDown(1)) TryAttack(MoveType.RightPunch);
            else if (Input.GetKeyDown(KeyCode.E)) TryAttack(MoveType.Kick);
            else if (Input.GetKeyDown(KeyCode.Q)) TryAttack(MoveType.Special);
        }

        void TryAttack(MoveType slot)
        {
            var move = MartialArtsMoveSet.GetPlayerMove(style, slot);
            if (stamina < move.staminaCost * 0.5f) return; // too tired to even start
            stamina -= move.staminaCost;
            float speedMul = (stamina < 20f) ? 0.7f : 1f;
            attackLockUntil = Time.time + (move.startupTime + move.activeTime + move.recoveryTime) / speedMul;
            attacksThrown++;
            lastAttackTime = Time.time;
            if (animatorRig != null) animatorRig.PlayAttack(slot);
            if (audioSource != null && swingClip != null) audioSource.PlayOneShot(swingClip, 0.6f);

            // Hit window resolved at activeTime midpoint.
            StartCoroutine(ResolveAttack(move, speedMul));
        }

        System.Collections.IEnumerator ResolveAttack(MartialArtsMove move, float speedMul)
        {
            yield return new WaitForSeconds(move.startupTime / speedMul);
            float damageMul = (stamina < 20f) ? 0.6f : 1f;
            bool landed = CombatHitDetector.TryLandHit(transform, opponent, move, damageMul);
            if (landed) attacksLanded++;
        }

        public void TakeDamage(float amount, Vector3 from, bool fromBack = false)
        {
            if (isDodging)
            {
                ScoreAndStatsManager.Notify("dodge_into_attack");
                return;
            }
            // Block check: facing attacker and shift held.
            if (isBlocking && Vector3.Dot(transform.forward, (from - transform.position).normalized) > 0.2f)
            {
                amount *= 0.18f;
                if (audioSource != null && blockClip != null) audioSource.PlayOneShot(blockClip, 0.7f);
                ScoreAndStatsManager.Notify("block");
                if (animatorRig != null) animatorRig.PlayHitFlash(true);
            }
            else
            {
                if (animatorRig != null) animatorRig.PlayHitFlash(false);
            }
            health = Mathf.Max(0f, health - amount);
            ScoreAndStatsManager.AddDamageTaken(amount);
            if (Camera.main != null)
            {
                var shaker = Camera.main.GetComponent<SimpleCameraController>();
                if (shaker != null) shaker.Shake(0.18f, 0.07f);
            }
        }
    }
}
