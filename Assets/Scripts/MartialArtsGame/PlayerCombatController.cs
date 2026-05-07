using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

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

        // XR controller edge tracking — UnityEngine.XR.InputDevices reports
        // current state, so we cache the previous frame's value to detect a
        // press-down. Quest 3 triggers come in via OpenXR / Oculus runtime
        // without any extra InputAction wiring needed.
        bool prevXrLeftTrigger;
        bool prevXrRightTrigger;
        bool prevXrLeftPrimary;
        bool prevXrRightPrimary;
        bool prevXrLeftSecondary;
        bool prevXrRightSecondary;
        bool prevXrLeftStickClick;
        bool prevXrRightStickClick;
        bool xrLeftTriggerDown;
        bool xrRightTriggerDown;
        bool xrSpecialDown;
        bool xrKickDown;
        bool xrDodgeDown;
        bool xrAnyGripHeld;
        static readonly List<InputDevice> s_XrDeviceBuf = new List<InputDevice>(2);

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

            PollXrButtons();

            if (!combatActive) return;
            HandleMovement();
            HandleCombatInput();
            FaceOpponent();
        }

        void PollXrButtons()
        {
            // Read raw current-frame state.
            bool lTrig  = ReadXrButton(false, CommonUsages.triggerButton);
            bool rTrig  = ReadXrButton(true,  CommonUsages.triggerButton);
            bool lGrip  = ReadXrButton(false, CommonUsages.gripButton);
            bool rGrip  = ReadXrButton(true,  CommonUsages.gripButton);
            bool lPrim  = ReadXrButton(false, CommonUsages.primaryButton);    // X
            bool rPrim  = ReadXrButton(true,  CommonUsages.primaryButton);    // A
            bool lSec   = ReadXrButton(false, CommonUsages.secondaryButton);  // Y
            bool rSec   = ReadXrButton(true,  CommonUsages.secondaryButton);  // B
            bool lStick = ReadXrButton(false, CommonUsages.primary2DAxisClick);
            bool rStick = ReadXrButton(true,  CommonUsages.primary2DAxisClick);

            // Convert to edges. Punches map directly to triggers; special is
            // either primary button down; kick is either secondary button
            // down; dodge is either thumbstick click down; block is grip held.
            xrLeftTriggerDown  = lTrig && !prevXrLeftTrigger;
            xrRightTriggerDown = rTrig && !prevXrRightTrigger;
            xrSpecialDown      = (lPrim && !prevXrLeftPrimary)   || (rPrim && !prevXrRightPrimary);
            xrKickDown         = (lSec  && !prevXrLeftSecondary) || (rSec  && !prevXrRightSecondary);
            xrDodgeDown        = (lStick && !prevXrLeftStickClick) || (rStick && !prevXrRightStickClick);
            xrAnyGripHeld      = lGrip || rGrip;

            prevXrLeftTrigger     = lTrig;
            prevXrRightTrigger    = rTrig;
            prevXrLeftPrimary     = lPrim;
            prevXrRightPrimary    = rPrim;
            prevXrLeftSecondary   = lSec;
            prevXrRightSecondary  = rSec;
            prevXrLeftStickClick  = lStick;
            prevXrRightStickClick = rStick;
        }

        static bool ReadXrButton(bool right, InputFeatureUsage<bool> usage)
        {
            var side = right ? InputDeviceCharacteristics.Right : InputDeviceCharacteristics.Left;
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | side, s_XrDeviceBuf);
            for (int i = 0; i < s_XrDeviceBuf.Count; i++)
            {
                if (s_XrDeviceBuf[i].TryGetFeatureValue(usage, out bool pressed) && pressed) return true;
            }
            return false;
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
            // Block: LeftShift on keyboard OR either grip held on Quest.
            isBlocking = (Input.GetKey(KeyCode.LeftShift) || xrAnyGripHeld) && stamina > 1f;
            if (animatorRig != null) animatorRig.SetGuard(isBlocking);

            // Dodge: Space on keyboard OR thumbstick click on Quest.
            if ((Input.GetKeyDown(KeyCode.Space) || xrDodgeDown) && !isDodging && stamina >= dodgeStaminaCost)
            {
                isDodging = true;
                dodgeTimer = 0.35f;
                dodgeVelocity = -transform.forward * dodgeImpulse;
                stamina -= dodgeStaminaCost;
                if (animatorRig != null) animatorRig.PlayDodge();
                ScoreAndStatsManager.Notify("dodge");
            }

            if (Time.time < attackLockUntil) return;

            // Quest mapping (matches the user's chosen scheme):
            //   Left trigger  -> Left punch
            //   Right trigger -> Right punch
            //   Secondary (B/Y) -> Kick
            //   Primary  (A/X) -> Special
            if (Input.GetMouseButtonDown(0) || xrLeftTriggerDown)       TryAttack(MoveType.LeftPunch);
            else if (Input.GetMouseButtonDown(1) || xrRightTriggerDown) TryAttack(MoveType.RightPunch);
            else if (Input.GetKeyDown(KeyCode.E) || xrKickDown)         TryAttack(MoveType.Kick);
            else if (Input.GetKeyDown(KeyCode.Q) || xrSpecialDown)      TryAttack(MoveType.Special);
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
