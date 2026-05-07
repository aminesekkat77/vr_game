using UnityEngine;

namespace MartialArtsGame
{
    // Drives the presentation camera. Uses hardcoded world-space positions so the
    // framing is identical no matter what state the runtime is in. Demo-grade,
    // not gameplay-grade — the positions are tuned for fighters at:
    //   Player   = (10, 0.9, -2)  (faces +Z)
    //   Opponent = (10, 0,    2)  (faces -Z)
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
        public float followLerp = 8f;
        public float menuSideDistance = 14.0f;
        public float fightSideDistance = 10.5f;
        public float menuCameraHeight = 3.8f;
        public float fightCameraHeight = 3.0f;
        public float lookHeight = 2.4f;
        public float menuDrift = 0.0f;
        public float maxShakeAmplitude = 0.015f;
        public Mode CurrentMode = Mode.Menu;
        public int trailerWaypoint = 0;

        // Stable fallback framing around the dojo center.
        Vector3 menuPos     = new Vector3(1f,  2.8f, 0f);
        Vector3 menuLook    = new Vector3(10f, 1.8f, 0f);
        Vector3 fightPos    = new Vector3(1f,  2.4f, 0f);
        Vector3 fightLook   = new Vector3(10f, 1.8f, 0f);
        Vector3 replayPos   = new Vector3(13f, 1.6f, 0f);
        Vector3 replayLook  = new Vector3(10f, 1.0f, 0f);

        Vector3 shakeOffset;
        float shakeIntensity;
        float shakeUntil;
        float menuOrbitT;
        bool snapNext = true;

        public void UseMenuView() { CurrentMode = Mode.Menu; menuOrbitT = 0f; snapNext = true; }
        public void UseFightView() { CurrentMode = Mode.Fight; snapNext = true; }
        public void UseReplayView() { CurrentMode = Mode.Replay; snapNext = true; }
        public void UseTrailerView(int waypoint) { CurrentMode = Mode.Trailer; trailerWaypoint = waypoint; snapNext = true; }

        void Awake()
        {
            // Snap to menu pose immediately so the first frame is correct.
            transform.position = menuPos;
            transform.rotation = Quaternion.LookRotation(menuLook - menuPos, Vector3.up);
        }

        public void Shake(float intensity, float duration)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, Mathf.Min(maxShakeAmplitude, intensity * 0.22f));
            shakeUntil = Time.unscaledTime + duration;
        }

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

            switch (CurrentMode)
            {
                case Mode.Menu:
                {
                    // Keep menu centered on the fight area but with subtle drift.
                    menuOrbitT += Time.unscaledDeltaTime * 0.18f;
                    float drift = Mathf.Sin(menuOrbitT) * menuDrift;
                    Vector3 center = new Vector3(10f, 0f, 0f);
                    desiredPos = center + Vector3.left * menuSideDistance + Vector3.up * menuCameraHeight + Vector3.forward * drift;
                    desiredRot = Quaternion.LookRotation((center + Vector3.up * lookHeight) - desiredPos, Vector3.up);
                    break;
                }
                case Mode.Fight:
                {
                    Vector3 center = GetFightCenter();
                    desiredPos = center + Vector3.left * fightSideDistance + Vector3.up * fightCameraHeight;
                    desiredRot = Quaternion.LookRotation((center + Vector3.up * lookHeight) - desiredPos, Vector3.up);
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
                        new Vector3(0f, 3.5f, -6f),         // 0 wide opening
                        new Vector3(4f, 1.8f, -3f),         // 1 orbit right
                        new Vector3(-3.5f, 1.4f, -2f),      // 2 close left
                        new Vector3(0f, 2.2f, -3.5f),       // 3 hero
                        new Vector3(2.5f, 0.9f, -1.5f),     // 4 slow-mo close
                        new Vector3(0f, 4.5f, -7f)          // 5 closing wide
                    };
                    int idx = Mathf.Clamp(trailerWaypoint, 0, offsets.Length - 1);
                    desiredPos = Vector3.Lerp(transform.position, pivot + offsets[idx], 1.5f * Time.unscaledDeltaTime);
                    desiredRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation((pivot + Vector3.up * 0.7f) - desiredPos), 2f * Time.unscaledDeltaTime);
                    break;
                }
            }

            // Snap on first frame so we don't show a long lerp from the initial pose.
            if (snapNext)
            {
                transform.position = desiredPos;
                transform.rotation = desiredRot;
                snapNext = false;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPos, followLerp * Time.unscaledDeltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, followLerp * Time.unscaledDeltaTime);
            }

            // Shake additive
            if (CurrentMode == Mode.Fight && Time.unscaledTime < shakeUntil)
            {
                shakeOffset = Random.insideUnitSphere * shakeIntensity;
                transform.position += shakeOffset;
            }
            else
            {
                shakeIntensity = 0f;
            }
        }
    }
}
