using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public float minPunchVelocity = 0.5f;

    private Vector3 previousPosition;
    private float currentVelocity;

    private void Start()
    {
        previousPosition = transform.position;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void FixedUpdate()
    {
        Vector3 distanceVect = transform.position - previousPosition;
        currentVelocity = distanceVect.magnitude / Time.fixedDeltaTime;
        previousPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentVelocity >= minPunchVelocity)
        {
            Debug.Log("💥 HIT !");
        }
    }
}