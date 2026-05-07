using UnityEngine;
#if XR_CORE_UTILS_PRESENT || true
using Unity.XR.CoreUtils;
#endif

namespace MartialArtsGame
{
    // Keeps the XR Origin's ground footprint aligned with the PlayerCombat
    // controller's transform. The Player object owns gameplay logic (movement,
    // health, faceOpponent), the XR Origin owns headset tracking. By syncing
    // XR Origin's XZ to the player's XZ each frame, the HMD-tracked head pose
    // ends up at the right world position above the player's feet — and WASD /
    // FaceOpponent / dodge all transparently move the user too.
    //
    // Also forces the tracking origin mode to Floor on Start so the Quest /
    // headset reports a real eye height (otherwise the camera ends up at y=0
    // because the rig has no manual eye offset applied).
    [DefaultExecutionOrder(450)]
    public class VrPlayerSync : MonoBehaviour
    {
        [Header("References")]
        public Transform xrOriginRoot;
        public Transform player;

        [Header("Tuning")]
        public float floorY = 0f;
        public bool syncRotation = true;
        public bool forceFloorTrackingMode = true;

        void Start()
        {
            EnsureRefs();
#if XR_CORE_UTILS_PRESENT || true
            if (forceFloorTrackingMode && xrOriginRoot != null)
            {
                var origin = xrOriginRoot.GetComponent<XROrigin>();
                if (origin != null)
                {
                    origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
                }
            }
#endif
            // Snap once so the user spawns at the player's feet on the first frame.
            ApplySync(snap: true);
        }

        void LateUpdate()
        {
            EnsureRefs();
            ApplySync(snap: false);
        }

        void EnsureRefs()
        {
            if (xrOriginRoot == null)
            {
                var go = GameObject.Find("XR Origin (XR Rig)");
                if (go == null) go = GameObject.Find("XR Origin");
                if (go != null) xrOriginRoot = go.transform;
            }
            if (player == null && MartialArtsGameManager.Instance != null && MartialArtsGameManager.Instance.player != null)
            {
                player = MartialArtsGameManager.Instance.player.transform;
            }
        }

        void ApplySync(bool snap)
        {
            if (xrOriginRoot == null || player == null) return;
            if (!xrOriginRoot.gameObject.activeInHierarchy && !snap) return;

            var p = player.position;
            xrOriginRoot.position = new Vector3(p.x, floorY, p.z);
            if (syncRotation)
            {
                xrOriginRoot.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
            }
        }
    }
}
