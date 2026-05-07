using System.Collections;
using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Walk,
        Punch
    }

    [Header("References")]
    public Transform playerTransform;
    public Animator animator;

    [Header("Combat Stats")]
    public float attackRange = 2.8f;
    public float stopDistance = 2.5f;
    public float retreatDistance = 1.8f;
    public float moveSpeed = 1.5f;
    public float actionCooldown = 1.2f;

    [Header("Behaviour")]
    [Range(0f, 1f)] public float aggroLevel = 0.5f;
    public float animationSpeedMultiplier = 1.0f;
    public float rotationSpeed = 5f;

    private AIState currentState = AIState.Idle;
    private float nextActionTime;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
       /* if (GameManager.Instance != null && !GameManager.Instance.isRoundActive)
        {
            SetIdle();
            return;
        } */

        if (playerTransform == null || animator == null)
        {
            return;
        }

        UpdateFacing();

        float distanceToPlayer = GetFlatDistanceToPlayer();

        if (distanceToPlayer > stopDistance)
        {
            MoveTowardsPlayer();
            SetWalking(true);
            currentState = AIState.Walk;
            return;
        }

        if (distanceToPlayer < retreatDistance)
        {
            MoveBackward();
            SetWalking(true);
            currentState = AIState.Walk;
            return;
        }

        SetWalking(false);
        currentState = AIState.Idle;

        if (distanceToPlayer <= attackRange && Time.time >= nextActionTime)
        {
            DecideNextAction();
        }
    }

    private void UpdateFacing()
    {
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private float GetFlatDistanceToPlayer()
    {
        Vector3 aiPos = transform.position;
        Vector3 playerPos = playerTransform.position;

        aiPos.y = 0f;
        playerPos.y = 0f;

        return Vector3.Distance(aiPos, playerPos);
    }

    private void MoveTowardsPlayer()
    {
        Vector3 targetPos = playerTransform.position;
        targetPos.y = transform.position.y;

        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 desiredPosition = targetPos - direction * stopDistance;

        transform.position = Vector3.MoveTowards(
            transform.position,
            desiredPosition,
            moveSpeed * Time.deltaTime
        );
    }

    private void MoveBackward()
    {
        Vector3 backward = -transform.forward;
        backward.y = 0f;

        transform.position += backward.normalized * moveSpeed * Time.deltaTime;
    }

    private void DecideNextAction()
    {
        float rand = Random.value;

        if (rand < aggroLevel)
        {
            DoPunch();
        }
        else
        {
            SetIdle();
            nextActionTime = Time.time + 0.5f;
        }
    }

    private void DoPunch()
    {
        currentState = AIState.Punch;
        animator.SetTrigger("punch");
        nextActionTime = Time.time + actionCooldown;
        StartCoroutine(ReturnToIdleAfterPunch(0.8f / Mathf.Max(animationSpeedMultiplier, 0.01f)));
    }

    private IEnumerator ReturnToIdleAfterPunch(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetIdle();
    }

    private void SetWalking(bool walking)
    {
        animator.SetBool("isWalking", walking);
    }

    private void SetIdle()
    {
        currentState = AIState.Idle;
        SetWalking(false);
    }

    public void OnPlayerAttacks()
    {
        // Réservé pour plus tard : block / dodge
    }

    public void OnHit()
    {
        Debug.Log("AI got hit!");
        transform.position += -transform.forward * 0.15f;
    }
}