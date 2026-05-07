using UnityEngine;

namespace MartialArtsGame
{
    // Drives the cinematic camera. Menu mode shows a wide third-person framing
    // of the dojo. Fight mode pins the camera to the player's eyes (first-person)
    // so the AI is visible directly in front and the procedural-rig hands act as
    // the user's "controllers" in the foreground. Replay/Trailer modes orbit the
    // fight area cinematically.
    [DefaultExecutionOrder(500)]
    public class SimpleCameraController : MonoBehaviour
    {
        public enum Mode { Menu, Fight, Replay, Trailer }

        [Header("References")]
        public Transform player;
        public Transform opponent;
        public Transform menuPivot;

        [Header("Tuning")]
        public Vector3 menuOffset = new Vector3(-3.0f, 2.2f, -4.5f);
        public Vector3 fightOffset = new Vector3(0f, 1.6f, -3.2f);
        public float followLerp = 10f;
        public float menuSideDistance = 14.0f;
        public float fightSideDistance = 10.5f;
        public float menuCameraHeight = 3.8f;
        public float fightCameraHeight = 3.0f;
        public float lookHeight = 2.4f;
        public float menuDrift = 0.0f;
        public float maxShakeAmplitude = 0.0f;

        [Header("Fight (first-person)")]
        public float fightEyeOffset = 1.25f;        // height above the player root
        public float fightForwardOffset = 0.05f;    // small push past the head so we don't clip it
        public float fightLookAtHeight = 0.95f;     // where on the opponent the camera aims
        public float fightRotationLerp = 14f;

        public Mode CurrentMode = Mode.Menu;
        public int trailerWaypoint = 0;

        // Stable fallback framing around the dojo center.
        Vector3 menuPos     = new Vector3(1f,  2.8f, 0f);
        Vector3 menuLook    = new Vector3(10f, 1.8f, 0f);
        Vector3 fightPos    = new Vector3(10f, 1.65f, -2f);
        Vector3 fightLook   = new Vector3(10f, 1.65f, 2f);
        Vector3 replayPos   = new Vector3(13f, 1.6f, 0f);
        Vector3 replayLook  = new Vector3(10f, 1.0f, 0f);

        float menuOrbitT;
        bool snapNext = true;
        Camera cam;

        public void UseMenuView() { CurrentMode = Mode.Menu; menuOrbitT = 0f; snapNext = true; }
        public void UseFightView() { CurrentMode = Mode.Fight; snapNext = true; }
        public void UseReplayView() { CurrentMode = Mode.Replay; snapNext = true; }
        public void UseTrailerView(int waypoint) { CurrentMode = Mode.Trailer; trailerWaypoint = waypoint; snapNext = true; }

        void Awake()
        {
            cam = GetComponent<Camera>();
            // Defensive: keep VR / XR pose drivers from fighting our writes — even
            // when no XR runtime is loaded, leaving target eye on Both has bitten us
            // before with various plugin combinations.
            if (cam != null) cam.stereoTargetEye = StereoTargetEyeMask.None;

            transform.position = menuPos;
            transform.rotation = Quaternion.LookRotation(menuLook - menuPos, Vector3.up);
        }

        // Kept for API compatibility — old code calls Shake() on damage, but the
        // additive random jitter caused visible shake-in-place when followLerp was
        // low, so it's a no-op now.
        public void Shake(float intensity, float duration) { }

        Vector3 GetFightCenter()
        {
            if (player != null && opponent != null) return (player.position + opponent.position) * 0.5f;
            if (player != null) return player.position + player.forward * 1.8f;
            if (opponent != null) return opponent.position - opponent.forward * 1.8f;
            return new Vector3(10f, 0f, 0f);
        }

        void LateUpdate()
        {
            Vector3 desiredPos = transform.position;
            Quaternion desiredRot = transform.rotation;
            float rotLerp = followLerp;

            switch (CurrentMode)
            {
                case Mode.Menu:
                {
                    // World-space menu sits at (10, 1.65, 0.5) facing -Z.
                    // Park the cinematic camera in front of it (same axis the
                    // VR user uses) so non-VR builds and the editor preview
                    // see the menu head-on.
                    menuOrbitT += Time.unscaledDeltaTime * 0.18f;
                    Vector3 menuPanel = new Vector3(10f, 1.65f, 0.5f);
                    desiredPos = menuPanel + new Vector3(0f, 0.05f, -3.6f + Mathf.Sin(menuOrbitT) * menuDrift);
                    desiredRot = Quaternion.LookRotation(menuPanel - desiredPos, Vector3.up);
                    break;
                }
                case Mode.Fight:
                {
                    Vector3 playerPos = (player != null) ? player.position : new Vector3(10f, 0.9f, -2f);
                    Vector3 oppPos    = (opponent != null) ? opponent.position : new Vector3(10f, 0f, 2f);
                    Vector3 horiz = oppPos - playerPos; horiz.y = 0f;
                    if (horiz.sqrMagnitude < 0.0001f) horiz = (player != null) ? player.forward : Vector3.forward;
                    Vector3 fwd = horiz.normalized;
                    desiredPos = playerPos + Vector3.up * fightEyeOffset + fwd * fightForwardOffset;
                    Vector3 lookTarget = oppPos + Vector3.up * fightLookAtHeight;
                    Vector3 lookDir = lookTarget - desiredPos;
                    if (lookDir.sqrMagnitude < 0.0001f) lookDir = fwd;
                    desiredRot = Quaternion.LookRotation(lookDir, Vector3.up);
                    rotLerp = fightRotationLerp;
                    break;
                }
                case Mode.Replay:
                {
                    menuOrbitT += Time.unscaledDeltaTime * 0.4f;
                    Vector3 pivot = replayLook;
                    desiredPos = pivot + new Vector3(Mathf.Cos(menuOrbitT) * 3.0f, 1.4f, Mathf.Sin(menuOrbitT) * 3.0f - 2f);
                    desiredRot = Quaternion.LookRotation(pivot - desiredPos, Vector3.up);
                    break;
                }
                case Mode.Trailer:
                {
                    Vector3 pivot = (player != null && opponent != null) ? (player.position + opponent.position) * 0.5f : Vector3.zero;
                    Vector3[] offsets = {
                        new Vector3(0f, 3.5f, -6f),
                        new Vector3(4f, 1.8f, -3f),
                        new Vector3(-3.5f, 1.4f, -2f),
                        new Vector3(0f, 2.2f, -3.5f),
                        new Vector3(2.5f, 0.9f, -1.5f),
                        new Vector3(0f, 4.5f, -7f)
                    };
                    int idx = Mathf.Clamp(trailerWaypoint, 0, offsets.Length - 1);
                    desiredPos = Vector3.Lerp(transform.position, pivot + offsets[idx], 1.5f * Time.unscaledDeltaTime);
                    desiredRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation((pivot + Vector3.up * 0.7f) - desiredPos), 2f * Time.unscaledDeltaTime);
                    break;
                }
            }

            if (snapNext)
            {
                transform.position = desiredPos;
                transform.rotation = desiredRot;
                snapNext = false;
            }
            else
            {
                float kPos = 1f - Mathf.Exp(-followLerp * Time.unscaledDeltaTime);
                float kRot = 1f - Mathf.Exp(-rotLerp * Time.unscaledDeltaTime);
                transform.position = Vector3.Lerp(transform.position, desiredPos, kPos);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, kRot);
            }
        }
    }
}
