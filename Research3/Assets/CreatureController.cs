using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CreatureController : MonoBehaviour
{
    private enum CreatureState { Idle, Hungry, SearchingForFood, Eating }

    [SerializeField]
    private CreatureState currentState = CreatureState.Idle;

    private NavMeshAgent agent;

    private const int maxHunger = 10;
    [SerializeField, Range(0, maxHunger)]
    private int hungerLevel = maxHunger;
    [SerializeField]
    private float hungerDecreaseInterval = 10f;
    private float hungerTimer = 0f;
    [SerializeField]
    private int hungerThreshold = 5;

    [SerializeField]
    private float wanderRadius = 5f;
    [SerializeField]
    private float wanderInterval = 5f;
    private float wanderTimer = 0f;

    [SerializeField]
    private GameObject visionObject;
    private SphereCollider visionCollider;

    private List<BerryBush> nearbyBushes = new List<BerryBush>();

    private BerryBush targetBush;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (visionObject != null)
        {
            visionCollider = visionObject.GetComponent<SphereCollider>();
            if (visionCollider == null)
            {
                Debug.LogError("Vision object does not have a SphereCollider component.");
            }
        }
        else
        {
            Debug.LogError("Vision object is not assigned.");
        }
    }

    private void Update()
    {
        hungerTimer += Time.deltaTime;
        if (hungerTimer >= hungerDecreaseInterval)
        {
            hungerLevel = Mathf.Max(0, hungerLevel - 1);
            hungerTimer = 0f;
        }

        switch (currentState)
        {
            case CreatureState.Idle:
                Wander();
                if (hungerLevel <= hungerThreshold)
                {
                    currentState = CreatureState.Hungry;
                }
                break;

            case CreatureState.Hungry:
                if (nearbyBushes.Count > 0)
                {
                    targetBush = FindNearestBush();
                    if (targetBush != null)
                    {
                        agent.SetDestination(targetBush.transform.position);
                        currentState = CreatureState.SearchingForFood;
                    }
                }
                else
                {
                    Wander();
                }
                break;

            case CreatureState.SearchingForFood:
                if (targetBush == null)
                {
                    currentState = CreatureState.Hungry;
                }
                break;

            case CreatureState.Eating:
                if (targetBush != null)
                {
                    if (targetBush.TakeFood(1))
                    {
                        hungerLevel = Mathf.Min(maxHunger, hungerLevel + 1);
                    }
                }
                targetBush = null;
                currentState = CreatureState.Idle;
                break;
        }
    }

    private void Wander()
    {
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            wanderTimer = 0f;
        }
    }

    private Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    private BerryBush FindNearestBush()
    {
        BerryBush nearestBush = null;
        float shortestDistance = Mathf.Infinity;

        foreach (BerryBush bush in nearbyBushes)
        {
            float distance = Vector3.Distance(transform.position, bush.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestBush = bush;
            }
        }

        return nearestBush;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BerryBush"))
        {
            BerryBush bush = other.GetComponent<BerryBush>();
            if (bush != null && !nearbyBushes.Contains(bush))
            {
                nearbyBushes.Add(bush);
            }
        }
        else if (other.CompareTag("EatingRange") && targetBush != null && other.gameObject == targetBush.gameObject)
        {
            currentState = CreatureState.Eating;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("BerryBush"))
        {
            BerryBush bush = other.GetComponent<BerryBush>();
            if (bush != null && nearbyBushes.Contains(bush))
            {
                nearbyBushes.Remove(bush);
            }
        }
    }
}
