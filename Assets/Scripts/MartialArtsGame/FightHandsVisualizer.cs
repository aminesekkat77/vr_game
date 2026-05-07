using UnityEngine;

namespace MartialArtsGame
{
    // Spawns two visible "controller hand" widgets parented to the active camera
    // so the moment the user clicks Start, they see hand-shaped objects in the
    // foreground (mimicking VR controllers) regardless of XR runtime presence.
    // The hands react to LMB / RMB / E / Q / LeftShift inputs with a quick
    // forward jab / guard, giving immediate visual feedback.
    public class FightHandsVisualizer : MonoBehaviour
    {
        [Header("References")]
        public Camera targetCamera;

        [Header("Tuning")]
        public Vector3 leftRest  = new Vector3(-0.22f, -0.28f, 0.55f);
        public Vector3 rightRest = new Vector3( 0.22f, -0.28f, 0.55f);
        public Vector3 jabOffset = new Vector3(0f, 0.08f, 0.55f);
        public Vector3 guardOffset = new Vector3(0f, 0.18f, 0.10f);
        public float restLerp = 18f;
        public float jabDuration = 0.22f;
        public Color leftColor = new Color(0.95f, 0.45f, 0.10f, 1f);
        public Color rightColor = new Color(0.20f, 0.65f, 0.95f, 1f);
        public float handSize = 0.075f;

        Transform leftHand;
        Transform rightHand;
        Vector3 leftTarget;
        Vector3 rightTarget;
        float leftJabUntil;
        float rightJabUntil;
        bool guarding;
        bool active;
        bool built;
        bool pendingActiveOnBuild;

        void Start()
        {
            EnsureBuilt();
            ApplyActive();
        }

        public void SetActive(bool on)
        {
            active = on;
            pendingActiveOnBuild = on;
            EnsureBuilt();
            ApplyActive();
        }

        void ApplyActive()
        {
            if (!built) return;
            if (leftHand != null)  leftHand.gameObject.SetActive(active);
            if (rightHand != null) rightHand.gameObject.SetActive(active);
        }

        void EnsureBuilt()
        {
            if (built) return;
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return; // try again later

            leftHand  = MakeHand("VR_LeftController",  leftColor,  leftRest);
            rightHand = MakeHand("VR_RightController", rightColor, rightRest);
            leftTarget = leftRest;
            rightTarget = rightRest;
            built = true;
            // honor any SetActive that arrived before the camera was ready
            active = pendingActiveOnBuild || active;
        }

        Transform MakeHand(string n, Color color, Vector3 localPos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = n;
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            var t = go.transform;
            t.SetParent(targetCamera.transform, false);
            t.localPosition = localPos;
            t.localRotation = Quaternion.Euler(20f, 0f, 0f);
            t.localScale = new Vector3(handSize * 1.4f, handSize, handSize * 1.6f);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                r.material = new Material(sh);
                if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
                if (r.material.HasProperty("_Color"))     r.material.color = color;
                if (r.material.HasProperty("_Smoothness")) r.material.SetFloat("_Smoothness", 0.7f);
            }
            // Accent ring on top of the controller body for readability.
            var accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            accent.name = n + "_Accent";
            var ac = accent.GetComponent<Collider>(); if (ac != null) Destroy(ac);
            accent.transform.SetParent(t, false);
            accent.transform.localPosition = new Vector3(0f, 0.55f, -0.25f);
            accent.transform.localScale = new Vector3(0.85f, 0.18f, 0.18f);
            var ar = accent.GetComponent<Renderer>();
            if (ar != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                ar.material = new Material(sh);
                Color tint = Color.Lerp(color, Color.white, 0.55f);
                if (ar.material.HasProperty("_BaseColor")) ar.material.SetColor("_BaseColor", tint);
                if (ar.material.HasProperty("_Color"))     ar.material.color = tint;
            }
            return t;
        }

        void Update()
        {
            if (!built) { EnsureBuilt(); ApplyActive(); }
            if (!active || leftHand == null || rightHand == null) return;

            guarding = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetMouseButtonDown(0)) leftJabUntil  = Time.time + jabDuration;
            if (Input.GetMouseButtonDown(1)) rightJabUntil = Time.time + jabDuration;
            if (Input.GetKeyDown(KeyCode.E)) rightJabUntil = Time.time + jabDuration * 1.4f;
            if (Input.GetKeyDown(KeyCode.Q)) leftJabUntil  = Time.time + jabDuration * 1.4f;

            leftTarget  = ResolveTarget(leftRest,  jabOffset, guardOffset, leftJabUntil,  +1);
            rightTarget = ResolveTarget(rightRest, jabOffset, guardOffset, rightJabUntil, -1);

            float k = 1f - Mathf.Exp(-restLerp * Time.deltaTime);
            leftHand.localPosition  = Vector3.Lerp(leftHand.localPosition,  leftTarget,  k);
            rightHand.localPosition = Vector3.Lerp(rightHand.localPosition, rightTarget, k);
        }

        Vector3 ResolveTarget(Vector3 rest, Vector3 jab, Vector3 guard, float jabUntil, int sideSign)
        {
            if (Time.time < jabUntil)
            {
                float t = 1f - Mathf.Clamp01((jabUntil - Time.time) / jabDuration);
                float arc = Mathf.Sin(t * Mathf.PI);
                return new Vector3(rest.x + sideSign * 0.05f, rest.y + jab.y * arc, rest.z + jab.z * arc);
            }
            if (guarding)
            {
                return new Vector3(rest.x * 0.6f, rest.y + guard.y, rest.z + guard.z);
            }
            return rest;
        }
    }
}
