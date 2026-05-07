using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplaySystem : MonoBehaviour
{
    [System.Serializable]
    public class ReplayTarget
    {
        public Transform targetTransform;
        public Animator optionalAnimator;
        [HideInInspector] public Queue<PosRotState> recordedStates = new Queue<PosRotState>();
    }

    public struct PosRotState
    {
        public Vector3 position;
        public Quaternion rotation;
        // Optional: you could store Animator state parameters here if needed
    }

    [Header("Replay Settings")]
    public float maxRecordTime = 10f; // Seconds to keep in memory
    public float playbackSpeedTimeScale = 0.3f; // Slow motion speed
    
    [Header("Targets (e.g. Player Hands, AI)")]
    public List<ReplayTarget> targetsToRecord;

    private bool isRecording = false;
    private bool isPlayingBack = false;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRoundStart.AddListener(StartRecording);
            GameManager.Instance.OnRoundEnd.AddListener(StartReplay);
        }
    }

    private void StartRecording()
    {
        foreach (var target in targetsToRecord)
        {
            target.recordedStates.Clear();
        }
        isRecording = true;
        isPlayingBack = false;
    }

    private void FixedUpdate()
    {
        if (isRecording)
        {
            int maxFrames = Mathf.RoundToInt(maxRecordTime / Time.fixedDeltaTime);
            
            foreach (var target in targetsToRecord)
            {
                if (target.targetTransform == null) continue;

                target.recordedStates.Enqueue(new PosRotState
                {
                    position = target.targetTransform.position,
                    rotation = target.targetTransform.rotation
                });

                if (target.recordedStates.Count > maxFrames)
                {
                    target.recordedStates.Dequeue(); // remove oldest frame
                }
            }
        }
    }

    public void StartReplay()
    {
        if (!isRecording) return;
        
        isRecording = false;
        isPlayingBack = true;
        StartCoroutine(ReplayRoutine());
    }

    private IEnumerator ReplayRoutine()
    {
        // 1. Lower time scale for slow motion visual effect across everything
        Time.timeScale = playbackSpeedTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // Keep physics smooth

        // We will manually move the objects through their queue
        // First, pause any AI scripts from controlling the transforms
        AIOpponent ai = FindObjectOfType<AIOpponent>();
        if (ai != null) ai.enabled = false;

        // Clone the queues so we don't modify the originals during playback
        Dictionary<ReplayTarget, Queue<PosRotState>> playbackQueues = new Dictionary<ReplayTarget, Queue<PosRotState>>();
        int maxFrames = 0;
        
        foreach (var target in targetsToRecord)
        {
            playbackQueues.Add(target, new Queue<PosRotState>(target.recordedStates));
            if (target.recordedStates.Count > maxFrames) maxFrames = target.recordedStates.Count;
            
            // disable animator if we just want raw transform replay
            if (target.optionalAnimator != null) target.optionalAnimator.enabled = false; 
        }

        while (isPlayingBack)
        {
            bool allQueuesEmpty = true;
            foreach (var kvp in playbackQueues)
            {
                if (kvp.Value.Count > 0)
                {
                    allQueuesEmpty = false;
                    PosRotState state = kvp.Value.Dequeue();
                    kvp.Key.targetTransform.position = state.position;
                    kvp.Key.targetTransform.rotation = state.rotation;
                }
            }

            if (allQueuesEmpty) break; // Finished replay

            yield return new WaitForFixedUpdate();
        }

        // Replay Finished
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        
        if (ai != null) ai.enabled = true;
        
        foreach (var target in targetsToRecord)
        {
            if (target.optionalAnimator != null) target.optionalAnimator.enabled = true;
        }

        isPlayingBack = false;
        Debug.Log("Replay finished.");
        
        // Wait another bit then reset round
        yield return new WaitForSeconds(2.0f);
        if (GameManager.Instance != null && !GameManager.Instance.isRoundActive)
        {
           GameManager.Instance.StartRound();
        }
    }
}
