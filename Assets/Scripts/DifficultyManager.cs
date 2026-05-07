using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public AIOpponent aiOpponent;

    [Header("Difficulty Bounds")]
    public float minActionCooldown = 0.3f;
    public float maxActionCooldown = 1.5f;
    public float minAnimSpeed = 0.8f;
    public float maxAnimSpeed = 1.5f;
    public float minAggro = 0.2f;
    public float maxAggro = 0.8f;

    [Header("Current Difficulty (0 to 1)")]
    [Range(0f, 1f)]
    public float currentDifficulty = 0.5f;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerHealthChanged.AddListener(ScaleDownDifficulty);
            GameManager.Instance.OnAIHealthChanged.AddListener(ScaleUpDifficulty);
            GameManager.Instance.OnRoundStart.AddListener(ApplyDifficulty);
        }
    }

    private void ScaleUpDifficulty(int _)
    {
        // Player hit AI - increase difficulty slightly
        currentDifficulty = Mathf.Clamp01(currentDifficulty + 0.1f);
        ApplyDifficulty();
    }

    private void ScaleDownDifficulty(int _)
    {
        // AI hit player - decrease difficulty slightly
        currentDifficulty = Mathf.Clamp01(currentDifficulty - 0.15f);
        ApplyDifficulty();
    }

    private void ApplyDifficulty()
    {
        if (aiOpponent == null) return;

        // Inverse lerp for cooldown: higher difficulty = lower cooldown
        aiOpponent.actionCooldown = Mathf.Lerp(maxActionCooldown, minActionCooldown, currentDifficulty);
        
        // Lerp for speed/aggro: higher difficulty = higher speed and aggro
        aiOpponent.animationSpeedMultiplier = Mathf.Lerp(minAnimSpeed, maxAnimSpeed, currentDifficulty);
        aiOpponent.aggroLevel = Mathf.Lerp(minAggro, maxAggro, currentDifficulty);

        Debug.Log($"Difficulty adjusted to {currentDifficulty * 100}%. AI Speed: {aiOpponent.animationSpeedMultiplier}");
    }
}
