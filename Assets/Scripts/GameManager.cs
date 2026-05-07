using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float roundTime = 60f;
    public int maxHealth = 100;

    [Header("State")]
    public int playerHealth;
    public int aiHealth;
    public float currentRoundTime;
    public bool isRoundActive = false;

    [Header("Events")]
    public UnityEvent OnRoundStart;
    public UnityEvent OnRoundEnd;
    public UnityEvent<int> OnPlayerHealthChanged;
    public UnityEvent<int> OnAIHealthChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        ResetRound();
    }

    private void Update()
    {
        if (isRoundActive)
        {
            currentRoundTime -= Time.deltaTime;
            if (currentRoundTime <= 0)
            {
                EndRound();
            }
        }
    }

    public void StartRound()
    {
        ResetRound();
        isRoundActive = true;
        OnRoundStart?.Invoke();
        Debug.Log("Round Started!");
    }

    public void EndRound()
    {
        isRoundActive = false;
        OnRoundEnd?.Invoke();
        Debug.Log("Round Ended. Triggering slow motion / replay...");
    }

    public void ResetRound()
    {
        playerHealth = maxHealth;
        aiHealth = maxHealth;
        currentRoundTime = roundTime;
        isRoundActive = false;
        
        OnPlayerHealthChanged?.Invoke(playerHealth);
        OnAIHealthChanged?.Invoke(aiHealth);
        
        // Reset positions could go here
    }

    public void DamageAI(int amount)
    {
        if (!isRoundActive) return;

        aiHealth = Mathf.Max(0, aiHealth - amount);
        OnAIHealthChanged?.Invoke(aiHealth);
        
        if (aiHealth <= 0)
            EndRound();
    }

    public void DamagePlayer(int amount)
    {
        if (!isRoundActive) return;

        playerHealth = Mathf.Max(0, playerHealth - amount);
        OnPlayerHealthChanged?.Invoke(playerHealth);
        
        if (playerHealth <= 0)
            EndRound();
    }
}
