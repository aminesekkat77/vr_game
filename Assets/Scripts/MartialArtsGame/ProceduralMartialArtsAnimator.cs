using System.Collections;
using UnityEngine;

namespace MartialArtsGame
{
    // Procedural animation rig: drives synthetic "fist" and "foot" anchor cubes
    // that are spawned as children of a fighter root. Adds body sway, stepping
    // motion, hit reactions and dodge animations without relying on any Animator
    // controller or AnimationClip.
    public class ProceduralMartialArtsAnimator : MonoBehaviour
    {
        [Header("Mode")]
        public bool autoBuildLimbs = true;
        public Color limbColor = new Color(0.85f, 0.20f, 0.20f, 1f);
        public Vector3 leftHandRest = new Vector3(-0.30f, 1.15f, 0.30f);
        public Vector3 rightHandRest = new Vector3( 0.30f, 1.15f, 0.30f);
        public Vector3 leftFootRest  = new Vector3(-0.18f, 0.05f, 0.10f);
        public Vector3 rightFootRest = new Vector3( 0.18f, 0.05f, 0.10f);
        public Vector3 bodyPivotRest = new Vector3(0f, 1.0f, 0f);

        [Header("Body sway")]
        public Transform bodyPivot;     // Set to fighter root if null
        public float swayAmount = 0.03f;
        public float swayFrequency = 1.6f;

        [Header("Foot stepping")]
        public bool useStepping = true;
        public float stepFrequency = 2.5f;
        public float stepHeight = 0.07f;

        // Anchor children created at runtime.
        Transform leftHand, rightHand, leftFoot, rightFoot;
        Renderer[] flashRenderers;
        bool guard;
        bool moving;
        float strafe; // -1 left, 1 right, 0 forward
        Coroutine attackRoutine;
        Coroutine dodgeRoutine;
        Coroutine flashRoutine;
        Vector3 bodyPivotInitialLocal;
        bool ownsBodyPivot;

        void Awake()
        {
            if (autoBuildLimbs) BuildLimbs();
            // If no explicit pivot, only modulate a zero-amplitude sway via a virtual pivot.
            // Don't override the fighter root's transform — that would teleport us to bodyPivotRest.
            ownsBodyPivot = (bodyPivot != null && bodyPivot != transform);
            if (ownsBodyPivot) bodyPivotInitialLocal = bodyPivot.localPosition;
            CacheRenderers();
        }

        void BuildLimbs()
        {
            leftHand  = MakeLimb("L_Hand",  leftHandRest,  new Vector3(0.10f, 0.10f, 0.10f));
            rightHand = MakeLimb("R_Hand",  rightHandRest, new Vector3(0.10f, 0.10f, 0.10f));
            leftFoot  = MakeLimb("L_Foot",  leftFootRest,  new Vector3(0.13f, 0.06f, 0.18f));
            rightFoot = MakeLimb("R_Foot",  rightFootRest, new Vector3(0.13f, 0.06f, 0.18f));
        }

        Transform MakeLimb(string n, Vector3 localPos, Vector3 size)
        {
            // Avoid duplicates if rebuilt.
            var existing = transform.Find(n);
            if (existing != null) return existing;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = n;
            // Strip collider so limbs don't push physics around.
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            var t = go.transform;
            t.SetParent(transform, false);
            t.localPosition = localPos;
            t.localScale = size;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                r.material.color = limbColor;
            }
            return t;
        }

        void CacheRenderers()
        {
            flashRenderers = GetComponentsInChildren<Renderer>(true);
        }

        void Update()
        {
            // Body sway — only if a separate pivot is assigned (won't teleport the fighter root).
            if (ownsBodyPivot && bodyPivot != null)
            {
                float t = Time.time * swayFrequency * (moving ? 1.6f : 1f);
                Vector3 sway = new Vector3(Mathf.Sin(t) * swayAmount, Mathf.Cos(t * 0.7f) * swayAmount * 0.4f, 0f);
                bodyPivot.localPosition = bodyPivotInitialLocal + sway;
            }
            // Foot stepping
            if (useStepping && leftFoot != null && rightFoot != null)
            {
                float ph = Time.time * stepFrequency * (moving ? 1f : 0.25f);
                float lY = Mathf.Max(0f, Mathf.Sin(ph)) * stepHeight;
                float rY = Mathf.Max(0f, Mathf.Sin(ph + Mathf.PI)) * stepHeight;
                leftFoot.localPosition  = new Vector3(leftFootRest.x,  leftFootRest.y + lY,  leftFootRest.z + (moving ? 0.05f * Mathf.Sin(ph) : 0f) + strafe * 0.04f);
                rightFoot.localPosition = new Vector3(rightFootRest.x, rightFootRest.y + rY, rightFootRest.z + (moving ? 0.05f * Mathf.Sin(ph + Mathf.PI) : 0f) + strafe * 0.04f);
            }
            // Hands stay near rest unless actively animating; guard pulls them up.
            if (leftHand != null && attackRoutine == null)
            {
                Vector3 rest = guard ? new Vector3(leftHandRest.x + 0.05f, leftHandRest.y + 0.10f, leftHandRest.z + 0.10f) : leftHandRest;
                leftHand.localPosition = Vector3.Lerp(leftHand.localPosition, rest, 8f * Time.deltaTime);
            }
            if (rightHand != null && attackRoutine == null)
            {
                Vector3 rest = guard ? new Vector3(rightHandRest.x - 0.05f, rightHandRest.y + 0.10f, rightHandRest.z + 0.10f) : rightHandRest;
                rightHand.localPosition = Vector3.Lerp(rightHand.localPosition, rest, 8f * Time.deltaTime);
            }
        }

        public void SetGuard(bool g) { guard = g; }
        public void SetMovement(bool isMoving, float strafeDir) { moving = isMoving; strafe = strafeDir; }

        public void PlayAttack(MoveType type)
        {
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = StartCoroutine(AttackRoutine(type));
        }

        IEnumerator AttackRoutine(MoveType type)
        {
            Transform limb = null;
            Vector3 rest = Vector3.zero, target = Vector3.zero;
            float duration = 0.30f;
            switch (type)
            {
                case MoveType.LeftPunch:
                    limb = leftHand; rest = leftHandRest;
                    target = new Vector3(leftHandRest.x + 0.05f, leftHandRest.y + 0.05f, leftHandRest.z + 0.85f);
                    duration = 0.28f; break;
                case MoveType.RightPunch:
                    limb = rightHand; rest = rightHandRest;
                    target = new Vector3(rightHandRest.x - 0.05f, rightHandRest.y + 0.05f, rightHandRest.z + 0.85f);
                    duration = 0.30f; break;
                case MoveType.Elbow:
                    limb = rightHand; rest = rightHandRest;
                    target = new Vector3(rightHandRest.x - 0.15f, rightHandRest.y, rightHandRest.z + 0.55f);
                    duration = 0.26f; break;
                case MoveType.Knee:
                    limb = rightFoot; rest = rightFootRest;
                    target = new Vector3(rightFootRest.x, rightFootRest.y + 0.6f, rightFootRest.z + 0.55f);
                    duration = 0.34f; break;
                case MoveType.FrontKick:
                case MoveType.Kick:
                    limb = rightFoot; rest = rightFootRest;
                    target = new Vector3(rightFootRest.x, rightFootRest.y + 0.45f, rightFootRest.z + 0.95f);
                    duration = 0.36f; break;
                case MoveType.RoundKick:
                    limb = rightFoot; rest = rightFootRest;
                    target = new Vector3(rightFootRest.x + 0.45f, rightFootRest.y + 0.55f, rightFootRest.z + 0.75f);
                    duration = 0.40f; break;
                case MoveType.Special:
                    limb = leftHand; rest = leftHandRest;
                    target = new Vector3(leftHandRest.x + 0.10f, leftHandRest.y + 0.20f, leftHandRest.z + 1.05f);
                    duration = 0.35f; break;
                case MoveType.Grapple:
                    limb = rightHand; rest = rightHandRest;
                    target = new Vector3(0f, rightHandRest.y - 0.20f, rightHandRest.z + 0.65f);
                    duration = 0.38f; break;
            }
            if (limb == null) { attackRoutine = null; yield break; }
            float t = 0f;
            float half = duration * 0.5f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = (t < half) ? (t / half) : (1f - (t - half) / half);
                limb.localPosition = Vector3.Lerp(rest, target, Mathf.SmoothStep(0f, 1f, k));
                yield return null;
            }
            limb.localPosition = rest;
            attackRoutine = null;
        }

        public void PlayDodge()
        {
            if (dodgeRoutine != null) StopCoroutine(dodgeRoutine);
            dodgeRoutine = StartCoroutine(DodgeRoutine());
        }

        IEnumerator DodgeRoutine()
        {
            float t = 0f;
            Quaternion start = transform.localRotation;
            Quaternion lean = Quaternion.Euler(0f, 0f, 12f) * start;
            while (t < 0.18f) { t += Time.deltaTime; transform.localRotation = Quaternion.Slerp(start, lean, t / 0.18f); yield return null; }
            t = 0f;
            while (t < 0.18f) { t += Time.deltaTime; transform.localRotation = Quaternion.Slerp(lean, start, t / 0.18f); yield return null; }
            transform.localRotation = start;
            dodgeRoutine = null;
        }

        public void PlayHitFlash(bool blocked)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(blocked ? Color.cyan : Color.red));
        }

        IEnumerator FlashRoutine(Color c)
        {
            // Tint all child renderer materials briefly.
            if (flashRenderers == null) CacheRenderers();
            Color[] originals = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                if (flashRenderers[i] != null && flashRenderers[i].material != null && flashRenderers[i].material.HasProperty("_BaseColor"))
                {
                    originals[i] = flashRenderers[i].material.GetColor("_BaseColor");
                    flashRenderers[i].material.SetColor("_BaseColor", c);
                }
                else if (flashRenderers[i] != null && flashRenderers[i].material != null && flashRenderers[i].material.HasProperty("_Color"))
                {
                    originals[i] = flashRenderers[i].material.color;
                    flashRenderers[i].material.color = c;
                }
            }
            yield return new WaitForSeconds(0.10f);
            for (int i = 0; i < flashRenderers.Length; i++)
            {
                if (flashRenderers[i] != null && flashRenderers[i].material != null && flashRenderers[i].material.HasProperty("_BaseColor"))
                    flashRenderers[i].material.SetColor("_BaseColor", originals[i]);
                else if (flashRenderers[i] != null && flashRenderers[i].material != null && flashRenderers[i].material.HasProperty("_Color"))
                    flashRenderers[i].material.color = originals[i];
            }
            flashRoutine = null;
        }
    }
}
