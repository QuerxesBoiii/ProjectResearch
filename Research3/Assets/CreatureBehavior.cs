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

    // Hunger system (foodLevel: 10 is full, 0 is empty)
    [SerializeField] private float foodLevel = 10f; // 0 to 10 scale
    private float hungerTimer = 0f;
    private float hungerDecreaseInterval = 10f; // Decrease by 1 every 10 seconds

    // Vision and targeting
    public List<Transform> visibleBushes = new List<Transform>(); // List of bushes in range, visible in Inspector
    private BerryBush targetBush;
    [SerializeField] private float eatingDistance = 1f; // Distance to start eating
    [SerializeField] private float detectionRadius = 5f; // Detection range for bushes
    [SerializeField] private LayerMask foodLayer; // Layer mask for berry bushes

    // Movement speeds
    [SerializeField] private float walkingSpeed = 4f; // Base speed for Idle and Eating
    private float sprintSpeed => walkingSpeed * 1.5f; // 50% more than walking speed for SearchingForFood

    // Wandering in Idle
    private float wanderInterval = 5f; // Move every 5 seconds
    private float wanderTimer = 0f;
    private float wanderRadius = 20f; // Max distance for random points

    // Eating timing
    private float eatTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkingSpeed; // Set initial speed to walking
        currentState = State.Idle;
    }

    void Update()
    {
        // Update bush detection
        UpdateBushDetection();

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
            agent.speed = sprintSpeed; // Switch to sprint speed
        }

        // State machine
        switch (currentState)
        {
            case State.Idle:
                if (agent.speed != walkingSpeed) agent.speed = walkingSpeed; // Ensure walking speed
                Wander();
                break;

            case State.SearchingForFood:
                if (agent.speed != sprintSpeed) agent.speed = sprintSpeed; // Ensure sprint speed
                SearchForFood();
                break;

            case State.Eating:
                if (agent.speed != walkingSpeed) agent.speed = walkingSpeed; // Revert to walking speed
                Eat();
                break;
        }
    }

    void UpdateBushDetection()
    {
        // Check for berry bushes within the detection radius
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, foodLayer);
        List<Transform> currentBushes = new List<Transform>();

        // Process all detected colliders
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("BerryBush"))
            {
                Transform bushTransform = hit.transform;
                currentBushes.Add(bushTransform);

                // Add new bushes to the list
                if (!visibleBushes.Contains(bushTransform))
                {
                    visibleBushes.Add(bushTransform);
                    Debug.Log("BerryBush entered range: " + bushTransform.name);
                }
            }
        }

        // Remove bushes that are no longer in range
        for (int i = visibleBushes.Count - 1; i >= 0; i--)
        {
            Transform bush = visibleBushes[i];
            if (!currentBushes.Contains(bush))
            {
                visibleBushes.Remove(bush);
                Debug.Log("BerryBush exited range: " + bush.name);
            }
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
            Transform closestTransform = null;
            float minDist = float.MaxValue;
            foreach (var bushTransform in visibleBushes)
            {
                float dist = Vector3.Distance(transform.position, bushTransform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestTransform = bushTransform;
                }
            }
            targetBush = closestTransform.GetComponent<BerryBush>(); // Get BerryBush component
            if (targetBush != null)
            {
                agent.SetDestination(closestTransform.position);

                // Check if close enough to eat
                if (Vector3.Distance(transform.position, targetBush.transform.position) <= eatingDistance)
                {
                    currentState = State.Eating;
                    agent.isStopped = true;
                    eatTimer = 0f;
                }
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
            if (targetBush != null && targetBush.currentFood > 0 && foodLevel < 10)
            {
                targetBush.currentFood--;
                foodLevel += 1;
                if (foodLevel > 10) foodLevel = 10;
            }
            else
            {
                // Bush empty, missing component, or food level full
                if (foodLevel >= 10)
                {
                    currentState = State.Idle;
                }
                else // Bush has no food or is invalid
                {
                    currentState = State.SearchingForFood;
                }
                agent.isStopped = false;
            }
            eatTimer = 0f;
        }
    }

    void OnDrawGizmos()
    {
        // Visualize the detection sphere in the editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}