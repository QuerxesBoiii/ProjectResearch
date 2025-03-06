using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CreatureBehavior : MonoBehaviour
{
    // State enum visible in Inspector
    public enum State { Idle, SearchingForFood, Eating }
    public State currentState = State.Idle;

    // Components
    private NavMeshAgent agent;

    // Hunger system (renamed for clarity: foodLevel, where 10 is full, 0 is empty)
    [SerializeField] private float foodLevel = 10f; // 0 to 10 scale
    private float hungerTimer = 0f;
    private float hungerDecreaseInterval = 10f; // Decrease by 1 every 10 seconds

    // Vision and targeting
    public List<BerryBush> visibleBushes = new List<BerryBush>(); // Public for Inspector visibility
    private BerryBush targetBush;
    [SerializeField] private float eatingDistance = 1f; // Distance to start eating

    // Wandering in Idle
    private float wanderInterval = 5f; // Move every 5 seconds
    private float wanderTimer = 0f;
    private float wanderRadius = 10f; // Max distance for random points

    // Eating timing
    private float eatTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = State.Idle;
    }

    void Update()
    {
        // Decrease food level over time
        hungerTimer += Time.deltaTime;
        if (hungerTimer >= hungerDecreaseInterval)
        {
            foodLevel -= 1;
            if (foodLevel < 0) foodLevel = 0;
            hungerTimer = 0f;
        }

        // Switch to SearchingForFood when food level is 5 or less
        if (foodLevel <= 5 && currentState == State.Idle)
        {
            currentState = State.SearchingForFood;
        }

        // State machine
        switch (currentState)
        {
            case State.Idle:
                Wander();
                break;

            case State.SearchingForFood:
                SearchForFood();
                break;

            case State.Eating:
                Eat();
                break;
        }
    }

    void Wander()
    {
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            wanderTimer = 0f;
        }
    }

    void SearchForFood()
    {
        if (visibleBushes.Count > 0)
        {
            // Find closest bush
            BerryBush closest = null;
            float minDist = float.MaxValue;
            foreach (var bush in visibleBushes)
            {
                float dist = Vector3.Distance(transform.position, bush.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = bush;
                }
            }
            targetBush = closest;
            agent.SetDestination(closest.transform.position);

            // Check if close enough to eat
            if (Vector3.Distance(transform.position, targetBush.transform.position) <= eatingDistance)
            {
                currentState = State.Eating;
                agent.isStopped = true;
                eatTimer = 0f;
            }
        }
        else
        {
            // No bushes visible, wander randomly
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                randomDirection += transform.position;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
        }
    }

    void Eat()
    {
        eatTimer += Time.deltaTime;
        if (eatTimer >= 1f) // Eat every second
        {
            if (targetBush.currentFood > 0 && foodLevel < 10)
            {
                targetBush.currentFood--;
                foodLevel += 1;
                if (foodLevel > 10) foodLevel = 10;
            }
            else
            {
                // Bush empty or food level full
                if (foodLevel >= 10)
                {
                    currentState = State.Idle;
                }
                else // Bush has no food
                {
                    currentState = State.SearchingForFood;
                }
                agent.isStopped = false;
            }
            eatTimer = 0f;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "BerryBush")
        {
            BerryBush bush = other.GetComponent<BerryBush>();
            if (bush != null)
            {
                visibleBushes.Add(bush);
                Debug.Log("Added bush: " + bush.name); // For debugging
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "BerryBush")
        {
            BerryBush bush = other.GetComponent<BerryBush>();
            if (bush != null)
            {
                visibleBushes.Remove(bush);
                Debug.Log("Removed bush: " + bush.name); // For debugging
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GetComponent<SphereCollider>().radius);
    }
}