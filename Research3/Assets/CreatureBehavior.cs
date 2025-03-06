using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CreatureBehavior : MonoBehaviour
{
    public enum State { Idle, SearchingForFood, Eating }
    public State currentState = State.Idle;

    private NavMeshAgent agent;
    private Renderer creatureRenderer; // For changing color

    [SerializeField] private float foodLevel = 10f; // 0 to 10 scale
    private float hungerTimer = 0f;
    private float hungerDecreaseInterval = 10f;

    public List<Transform> visibleBushes = new List<Transform>();
    private BerryBush targetBush;
    [SerializeField] private float eatingDistance = 1f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private LayerMask foodLayer;

    [SerializeField] private float walkingSpeed = 4f;
    private float sprintSpeed => walkingSpeed * 1.5f;

    private float wanderInterval = 5f;
    private float wanderTimer = 0f;
    private float wanderRadius = 20f;

    private float eatTimer = 0f;

    // Colors for food level visualization
    private readonly Color fullColor = Color.green; // 10/10 food
    private readonly Color emptyColor = Color.red;  // 0/10 food

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkingSpeed;
        currentState = State.Idle;

        // Get the renderer for color changes
        creatureRenderer = GetComponent<Renderer>();
        if (creatureRenderer == null)
        {
            Debug.LogError($"{name}: No Renderer found! Color changes won’t work.");
        }
        UpdateColor(); // Set initial color
    }

    void Update()
    {
        UpdateBushDetection();

        hungerTimer += Time.deltaTime;
        if (hungerTimer >= hungerDecreaseInterval)
        {
            foodLevel -= 1;
            if (foodLevel < 0) foodLevel = 0;
            hungerTimer = 0f;
            UpdateColor(); // Update color when food decreases
        }

        if (foodLevel <= 5 && currentState == State.Idle)
        {
            currentState = State.SearchingForFood;
            agent.speed = sprintSpeed;
        }

        switch (currentState)
        {
            case State.Idle:
                if (agent.speed != walkingSpeed) agent.speed = walkingSpeed;
                Wander();
                break;

            case State.SearchingForFood:
                if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;
                SearchForFood();
                break;

            case State.Eating:
                if (agent.speed != walkingSpeed) agent.speed = walkingSpeed;
                Eat();
                break;
        }
    }

    void UpdateBushDetection()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, foodLayer);
        List<Transform> currentBushes = new List<Transform>();

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("BerryBush"))
            {
                BerryBush bush = hit.GetComponent<BerryBush>();
                if (bush != null && bush.HasFood)
                {
                    Transform bushTransform = hit.transform;
                    currentBushes.Add(bushTransform);
                    if (!visibleBushes.Contains(bushTransform))
                    {
                        visibleBushes.Add(bushTransform);
                        Debug.Log("BerryBush entered range: " + bushTransform.name);
                    }
                }
            }
        }

        for (int i = visibleBushes.Count - 1; i >= 0; i--)
        {
            Transform bush = visibleBushes[i];
            BerryBush bushComponent = bush.GetComponent<BerryBush>();
            if (!currentBushes.Contains(bush) || (bushComponent != null && !bushComponent.HasFood))
            {
                visibleBushes.Remove(bush);
                if (bush == targetBush?.transform) targetBush = null;
                Debug.Log("BerryBush exited range or has no food: " + bush.name);
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
            targetBush = closestTransform.GetComponent<BerryBush>();
            if (targetBush != null && targetBush.HasFood)
            {
                agent.SetDestination(closestTransform.position);
                if (Vector3.Distance(transform.position, targetBush.transform.position) <= eatingDistance)
                {
                    currentState = State.Eating;
                    agent.isStopped = true;
                    eatTimer = 0f;
                }
            }
            else
            {
                targetBush = null;
            }
        }
        else
        {
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
        if (eatTimer >= 1f)
        {
            if (targetBush != null && targetBush.HasFood && targetBush.CurrentFood > 0 && foodLevel < 10)
            {
                targetBush.CurrentFood--;
                foodLevel += 1;
                if (foodLevel > 10) foodLevel = 10;
                UpdateColor(); // Update color when food increases
                Debug.Log($"{name}: Ate from {targetBush.name}, foodLevel now {foodLevel}");
            }
            else
            {
                if (foodLevel >= 10)
                {
                    currentState = State.Idle;
                    Debug.Log($"{name}: Full, reverting to Idle");
                }
                else
                {
                    currentState = State.SearchingForFood;
                    Debug.Log($"{name}: Bush empty or invalid, searching for another");
                }
                agent.isStopped = false;
            }
            eatTimer = 0f;
        }
    }

    // Update creature color based on foodLevel
    private void UpdateColor()
    {
        if (creatureRenderer != null)
        {
            float t = foodLevel / 10f; // Normalize foodLevel to 0-1 (0 = empty, 1 = full)
            Color newColor = Color.Lerp(emptyColor, fullColor, t);
            creatureRenderer.material.color = newColor;
            Debug.Log($"{name}: Updated color to {newColor} (foodLevel: {foodLevel}/10)");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}