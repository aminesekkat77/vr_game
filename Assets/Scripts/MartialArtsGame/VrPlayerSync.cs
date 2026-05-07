using UnityEngine;

namespace MartialArtsGame
{
    // Keeps the XR Origin's XZ footprint aligned with the PlayerCombat
    // controller's transform so WASD / FaceOpponent / dodge transparently
    // move the headset in world space too.
    //
    // We deliberately do NOT touch the XR Origin's Y position or its tracking
    // origin mode — leave those as configured in the scene's XR Origin
    // component. Forcing Floor mode at runtime caused Quest 3 to lose the
    // Camera Offset's Y bias and drop the camera to the ground.
    [DefaultExecutionOrder(450)]
    public class VrPlayerSync : MonoBehaviour
    {
        [Header("References")]
        public Transform xrOriginRoot;
        public Transform player;

        [Header("Tuning")]
        public bool syncRotation = true;
        // If your XR Origin's Y is non-zero in the scene (e.g. because the
        // dojo floor is above world Y=0), set this so the rig stays at the
        // correct elevation. Default: keep whatever the XR Origin already had.
        public bool overrideY = false;
        public float overrideYValue = 0f;

        void Start()
        {
            EnsureRefs();
            // Snap once so the user spawns above the player's feet on frame 1.
            ApplySync();
        }

        void LateUpdate()
        {
            EnsureRefs();
            ApplySync();
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

        void ApplySync()
        {
            if (xrOriginRoot == null || player == null) return;
            if (!xrOriginRoot.gameObject.activeInHierarchy) return;

            var p = player.position;
            float y = overrideY ? overrideYValue : xrOriginRoot.position.y;
            xrOriginRoot.position = new Vector3(p.x, y, p.z);
            if (syncRotation)
            {
                xrOriginRoot.rotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);
            }
        }
    }
}
