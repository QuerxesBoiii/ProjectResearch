using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CreatureBehavior : MonoBehaviour
{
    public enum State { Idle, SearchingForFood, Panic, Eating, SeekingMate }
    public State currentState = State.Idle;

    public enum HerdMentalityType { Herd, Ignores, Isolation }
    [SerializeField] private HerdMentalityType herdMentality = HerdMentalityType.Ignores;

    private NavMeshAgent agent;
    private Renderer creatureRenderer;

    [SerializeField] private float size = 1.00f; // 2 decimals
    [SerializeField] private float walkingSpeed = 4.00f; // 2 decimals
    private float newWalkingSpeed;
    private float sprintSpeed => Mathf.Round(newWalkingSpeed * 1.50f * 100f) / 100f; // 2 decimals

    [SerializeField] private int foodLevel; // Integer
    private float maxFoodLevel; // Will be rounded up to an integer
    [SerializeField] private float health; // 2 decimals
    private float hungerTimer = 0.00f; // 2 decimals
    private float hungerDecreaseInterval; // 2 decimals
    private float damageTimer = 0.00f; // 2 decimals
    private float healTimer = 0.00f; // 2 decimals
    private bool isHealing = false;

    private float mentalityCheckTimer = 0.00f; // 2 decimals
    private const float mentalityCheckInterval = 60.00f; // 2 decimals

    private float reproductionTimer = 0.00f; // 2 decimals
    private const float reproductionCheckInterval = 40.00f; // 2 decimals
    [SerializeField] private float lastReproductionTime = 0.00f; // 2 decimals
    private const float reproductionCooldown = 120.00f; // 2 decimals
    private Transform reproductionTarget;

    [SerializeField] private int age = 0; // Integer
    private float ageTimer = 0.00f; // 2 decimals
    private const float ageIncreaseInterval = 30.00f; // 2 decimals
    private float ageSize => Mathf.Round(Mathf.Lerp(0.30f, 1.00f, Mathf.Min(age / adultAge, 1.00f)) * 100f) / 100f; // 2 decimals
    private const float adultAge = 10.00f; // 2 decimals

    public List<Transform> visibleDiscoverables = new List<Transform>();
    private FoodSource targetFoodSource;
    [SerializeField] private float eatingDistance = 1.00f; // 2 decimals
    [SerializeField] private float detectionRadius = 5.00f; // 2 decimals
    [SerializeField] private LayerMask discoverableLayer;

    private float wanderInterval = 5.00f; // 2 decimals
    private float wanderTimer = 0.00f; // 2 decimals
    [SerializeField] private float wanderRadius = 20.00f; // 2 decimals
    private float panicWanderRadius => Mathf.Round(wanderRadius * 1.50f * 100f) / 100f; // 2 decimals

    private float eatTimer = 0.00f; // 2 decimals
    private float navigationTimer = 0.00f; // 2 decimals
    private const float navigationTimeout = 10.00f; // 2 decimals
    private const float reproductionDistance = 5.00f; // 2 decimals

    private readonly Color fullColor = Color.green;
    private readonly Color emptyColor = Color.red;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        creatureRenderer = GetComponent<Renderer>();
        if (creatureRenderer == null)
        {
            Debug.LogError($"{name}: No Renderer found! Color changes won’t work.");
        }

        size = Mathf.Round(size * 100f) / 100f; // Ensure 2 decimals
        walkingSpeed = Mathf.Round(walkingSpeed * 100f) / 100f; // Ensure 2 decimals
        lastReproductionTime = Time.time; // Start with current time to enforce initial cooldown
        UpdateSizeAndStats();
        foodLevel = (int)maxFoodLevel; // Set initial foodLevel to maxFoodLevel (now rounded up)
        agent.speed = newWalkingSpeed;

        UpdateColor();
        Debug.Log($"{name}: Initialized with size {size}, age {age}, ageSize {ageSize}, speed {newWalkingSpeed}, health {health}, maxFood {maxFoodLevel}, hunger interval {hungerDecreaseInterval}");

        // Check if this is the only creature and trigger asexual reproduction with age adjustments
        if (IsOnlyCreature())
        {
            age = 10; // Set original creature to adult age (10 years)
            UpdateSizeAndStats(); // Update size to reflect adult age
            int cloneAge = 8; // Starting age for first clone
            for (int i = 0; i < 3; i++)
            {
                AsexualReproduction(cloneAge);
                cloneAge -= 2; // Decrease age by 2 for each subsequent clone
            }
            Debug.Log($"{name}: Only creature detected, asexually reproduced 3 times with ages 10 (self), 8, 6, 4");
        }
    }

    void Update()
    {
        UpdateDiscoverablesDetection();

        ageTimer += Time.deltaTime;
        if (ageTimer >= ageIncreaseInterval)
        {
            age += 1; // Integer increment
            ageTimer = 0.00f;
            UpdateSizeAndStats();
            if (age == adultAge)
            {
                Debug.Log($"{name}: Age increased to {age}, creature is now an adult");
            }
        }

        float currentHungerInterval = isHealing ? hungerDecreaseInterval / 2.00f : hungerDecreaseInterval;
        hungerTimer += Time.deltaTime;
        if (hungerTimer >= currentHungerInterval)
        {
            foodLevel -= 1; // Integer decrement
            if (foodLevel < 0) foodLevel = 0;
            hungerTimer = 0.00f;
            UpdateColor();
        }

        if (foodLevel <= 0)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= 5.00f)
            {
                health -= 1.00f;
                damageTimer = 0.00f;
                Debug.Log($"{name}: No food, health decreased to {health}");
                if (health <= 0.00f)
                {
                    Debug.Log($"{name}: Health reached 0, creature destroyed");
                    Destroy(gameObject);
                    return;
                }
            }
        }
        else
        {
            damageTimer = 0.00f;
        }

        if (foodLevel > 0 && health < size * 10.00f)
        {
            isHealing = true;
            healTimer += Time.deltaTime;
            if (healTimer >= 10.00f)
            {
                health += 1.00f;
                healTimer = 0.00f;
                if (health >= size * 10.00f) isHealing = false;
            }
        }
        else if (health >= size * 10.00f)
        {
            isHealing = false;
            healTimer = 0.00f;
        }

        if (foodLevel <= Mathf.Round(maxFoodLevel * 0.50f * 100f) / 100f && currentState == State.Idle)
        {
            currentState = State.SearchingForFood;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Hunger triggered SearchingForFood");
        }

        if (herdMentality != HerdMentalityType.Ignores)
        {
            mentalityCheckTimer += Time.deltaTime;
            if (mentalityCheckTimer >= mentalityCheckInterval)
            {
                CheckMentality();
                mentalityCheckTimer = 0.00f;
            }
        }

        if (age >= adultAge)
        {
            reproductionTimer += Time.deltaTime;
            if (reproductionTimer >= reproductionCheckInterval && currentState != State.Eating && currentState != State.SeekingMate)
            {
                CheckReproduction();
                reproductionTimer = 0.00f;
            }
        }

        if (agent.hasPath)
        {
            navigationTimer += Time.deltaTime;
            if (navigationTimer >= navigationTimeout)
            {
                agent.ResetPath();
                navigationTimer = 0.00f;
                Debug.Log($"{name}: Navigation timeout, resetting path");
            }
        }
        else
        {
            navigationTimer = 0.00f;
        }

        switch (currentState)
        {
            case State.Idle:
                if (agent.speed != newWalkingSpeed) agent.speed = newWalkingSpeed;
                Wander();
                break;

            case State.SearchingForFood:
                if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;
                SearchForFood();
                break;

            case State.Panic:
                if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;
                HandlePanic();
                break;

            case State.Eating:
                if (agent.speed != newWalkingSpeed) agent.speed = newWalkingSpeed;
                Eat();
                break;

            case State.SeekingMate:
                if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;
                SeekMate();
                break;
        }
    }

    void UpdateDiscoverablesDetection()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, discoverableLayer);
        List<Transform> currentDiscoverables = new List<Transform>();

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform) continue;

            Transform discoverableTransform = hit.transform;
            currentDiscoverables.Add(discoverableTransform);
            if (!visibleDiscoverables.Contains(discoverableTransform))
            {
                visibleDiscoverables.Add(discoverableTransform);
                string tag = hit.CompareTag("Apple") ? "Apple" :
                            hit.CompareTag("Creature") ? "Creature" :
                            hit.CompareTag("Player") ? "Player" : "Unknown";
            }
        }

        for (int i = visibleDiscoverables.Count - 1; i >= 0; i--)
        {
            Transform discoverable = visibleDiscoverables[i];
            if (!currentDiscoverables.Contains(discoverable))
            {
                visibleDiscoverables.Remove(discoverable);
                if (discoverable == targetFoodSource?.transform) targetFoodSource = null;
                if (discoverable == reproductionTarget) reproductionTarget = null;
            }
        }
    }

    void UpdateSizeAndStats()
    {
        transform.localScale = Vector3.one * size * ageSize;
        newWalkingSpeed = Mathf.Round((walkingSpeed / size) * 100f) / 100f;
        health = Mathf.Round(size * 10.00f * 100f) / 100f;
        maxFoodLevel = Mathf.Ceil(size * 10.00f); // Round up to next whole number
        hungerDecreaseInterval = Mathf.Round((20.00f / size) * 100f) / 100f;
    }

    void Wander()
    {
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            float currentWanderRadius = wanderRadius;
            Vector3 randomDirection = Random.insideUnitSphere * currentWanderRadius;
            randomDirection += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, currentWanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            wanderTimer = 0.00f;
        }
    }

    void SearchForFood()
    {
        if (visibleDiscoverables.Count > 0)
        {
            Transform closestFoodSource = null;
            float minDist = float.MaxValue;
            foreach (var discoverableTransform in visibleDiscoverables)
            {
                if (discoverableTransform.CompareTag("Apple"))
                {
                    float dist = Vector3.Distance(transform.position, discoverableTransform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestFoodSource = discoverableTransform;
                    }
                }
            }

            if (closestFoodSource != null)
            {
                targetFoodSource = closestFoodSource.GetComponent<FoodSource>();
                if (targetFoodSource != null && targetFoodSource.HasFood)
                {
                    agent.SetDestination(closestFoodSource.position);
                    if (Vector3.Distance(transform.position, targetFoodSource.transform.position) <= eatingDistance)
                    {
                        currentState = State.Eating;
                        agent.isStopped = true;
                        eatTimer = 0.00f;
                    }
                }
                else
                {
                    targetFoodSource = null;
                    PanicWander();
                }
            }
            else
            {
                PanicWander();
            }
        }
        else
        {
            PanicWander();
        }
    }

    void HandlePanic()
    {
        if (herdMentality == HerdMentalityType.Herd)
        {
            bool hasCreaturesNearby = HasCreaturesInRange();
            if (hasCreaturesNearby)
            {
                currentState = State.Idle;
                Debug.Log($"{name}: Herd creature found others, reverting to Idle");
            }
            else
            {
                PanicWander();
            }
        }
        else if (herdMentality == HerdMentalityType.Isolation)
        {
            AvoidCreatures();
        }
    }

    void SeekMate()
    {
        if (reproductionTarget != null)
        {
            agent.SetDestination(reproductionTarget.position);
            float distanceToTarget = Vector3.Distance(transform.position, reproductionTarget.position);
            if (distanceToTarget <= reproductionDistance)
            {
                AttemptReproduction();
            }
        }
        else
        {
            currentState = State.Idle;
            Debug.Log($"{name}: Reproduction target lost, reverting to Idle");
        }
    }

    private void PanicWander()
    {
        float currentWanderRadius = panicWanderRadius;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 randomDirection = Random.insideUnitSphere * currentWanderRadius;
            randomDirection += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, currentWanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void AvoidCreatures()
    {
        float isolationRadius = Mathf.Round(detectionRadius / 2.00f * 100f) / 100f;
        Transform nearestCreature = null;
        float minDist = float.MaxValue;

        foreach (var discoverable in visibleDiscoverables)
        {
            if (discoverable.CompareTag("Creature"))
            {
                float dist = Vector3.Distance(transform.position, discoverable.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestCreature = discoverable;
                }
            }
        }

        if (nearestCreature != null && minDist <= isolationRadius)
        {
            Vector3 directionAway = (transform.position - nearestCreature.position).normalized;
            Vector3 targetPosition = transform.position + directionAway * (isolationRadius + 1.00f);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, panicWanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                Debug.Log($"{name}: Isolation creature moving away from {nearestCreature.name}");
            }
        }
        else
        {
            currentState = State.Idle;
            Debug.Log($"{name}: Isolation creature alone, reverting to Idle");
        }
    }

    private void CheckMentality()
    {
        bool hasCreaturesNearby = HasCreaturesInRange();
        if (herdMentality == HerdMentalityType.Herd)
        {
            if (!hasCreaturesNearby && currentState != State.Eating && currentState != State.SearchingForFood)
            {
                currentState = State.Panic;
                agent.speed = sprintSpeed;
                Debug.Log($"{name}: Herd creature alone, entering Panic mode");
            }
        }
        else if (herdMentality == HerdMentalityType.Isolation)
        {
            if (hasCreaturesNearby && currentState != State.Eating && currentState != State.SearchingForFood)
            {
                currentState = State.Panic;
                agent.speed = sprintSpeed;
                Debug.Log($"{name}: Isolation creature near others, entering Panic mode");
            }
        }
    }

    private bool HasCreaturesInRange()
    {
        foreach (var discoverable in visibleDiscoverables)
        {
            if (discoverable.CompareTag("Creature"))
            {
                return true;
            }
        }
        return false;
    }

    private void CheckReproduction()
    {
        if (CanReproduce() && Random.value < 1.00f / 3.00f)
        {
            Transform nearestCreature = FindNearestCreatureInReproductionRange();
            if (nearestCreature != null)
            {
                reproductionTarget = nearestCreature;
                currentState = State.SeekingMate;
                agent.SetDestination(reproductionTarget.position);
                Debug.Log($"{name}: Reproduction triggered, seeking {reproductionTarget.name}");
            }
            else
            {
                Debug.Log($"{name}: Reproduction triggered, but no creature within range");
            }
        }
    }

    private Transform FindNearestCreatureInReproductionRange()
    {
        float reproductionRange = Mathf.Round(detectionRadius * 1.50f * 100f) / 100f;
        Transform nearest = null;
        float minDist = float.MaxValue;

        Collider[] hits = Physics.OverlapSphere(transform.position, reproductionRange, discoverableLayer);
        foreach (Collider hit in hits)
        {
            if (hit.transform == transform) continue;
            if (hit.CompareTag("Creature"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = hit.transform;
                }
            }
        }
        return nearest;
    }

    private void AttemptReproduction()
    {
        CreatureBehavior partner = reproductionTarget.GetComponent<CreatureBehavior>();
        if (partner != null && partner.CanReproduce())
        {
            SpawnChild(partner);
            lastReproductionTime = Time.time;
            partner.lastReproductionTime = Time.time;
            reproductionTarget = null;
            currentState = State.Idle;
            Debug.Log($"{name}: Successfully reproduced with {partner.name}");
        }
        else
        {
            reproductionTarget = null;
            currentState = State.Idle;
            Debug.Log($"{name}: Partner declined reproduction");
        }
    }

    private bool CanReproduce()
    {
        return foodLevel >= Mathf.Round(maxFoodLevel * 0.80f * 100f) / 100f && (Time.time - lastReproductionTime) >= reproductionCooldown && age >= adultAge;
    }

    private void SpawnChild(CreatureBehavior partner)
    {
        GameObject child = Instantiate(gameObject, transform.position + Vector3.right * 2.00f, Quaternion.identity);
        CreatureBehavior childBehavior = child.GetComponent<CreatureBehavior>();

        float avgSize = Mathf.Round(((size + partner.size) / 2.00f) * 100f) / 100f;
        float avgWalkingSpeed = Mathf.Round(((walkingSpeed + partner.walkingSpeed) / 2.00f) * 100f) / 100f;

        float sizeVariation = Mathf.Round(Random.Range(-0.10f, 0.10f) * avgSize * 100f) / 100f;
        float speedVariation = Mathf.Round(Random.Range(-0.10f, 0.10f) * avgWalkingSpeed * 100f) / 100f;

        childBehavior.size = Mathf.Round((avgSize + sizeVariation) * 100f) / 100f;
        childBehavior.walkingSpeed = Mathf.Round((avgWalkingSpeed + speedVariation) * 100f) / 100f;

        childBehavior.age = 0;
        childBehavior.foodLevel = (int)childBehavior.maxFoodLevel;
        childBehavior.reproductionTimer = 0.00f;
        childBehavior.lastReproductionTime = Time.time;

        Debug.Log($"{name}: Spawned child with size {childBehavior.size}, walkingSpeed {childBehavior.walkingSpeed}, age {childBehavior.age}");
    }

    private void AsexualReproduction(int startingAge)
    {
        GameObject child = Instantiate(gameObject, transform.position + Vector3.right * 2.00f, Quaternion.identity);
        CreatureBehavior childBehavior = child.GetComponent<CreatureBehavior>();

        float avgSize = size; // Use own size as base
        float avgWalkingSpeed = walkingSpeed; // Use own walking speed as base

        float sizeVariation = Mathf.Round(Random.Range(-0.10f, 0.10f) * avgSize * 100f) / 100f;
        float speedVariation = Mathf.Round(Random.Range(-0.10f, 0.10f) * avgWalkingSpeed * 100f) / 100f;

        childBehavior.size = Mathf.Round((avgSize + sizeVariation) * 100f) / 100f;
        childBehavior.walkingSpeed = Mathf.Round((avgWalkingSpeed + speedVariation) * 100f) / 100f;

        childBehavior.age = startingAge; // Set the specified starting age
        childBehavior.foodLevel = (int)childBehavior.maxFoodLevel;
        childBehavior.reproductionTimer = 0.00f;
        childBehavior.lastReproductionTime = Time.time;

        Debug.Log($"{name}: Asexually spawned child with size {childBehavior.size}, walkingSpeed {childBehavior.walkingSpeed}, age {childBehavior.age}");
    }

    private bool IsOnlyCreature()
    {
        GameObject[] creatures = GameObject.FindGameObjectsWithTag("Creature");
        return creatures.Length == 1 && creatures[0] == gameObject; // Only this creature exists
    }

    void Eat()
    {
        eatTimer += Time.deltaTime;
        if (eatTimer >= 1.00f)
        {
            if (targetFoodSource != null && targetFoodSource.HasFood && targetFoodSource.CurrentFood > 0 && foodLevel < maxFoodLevel)
            {
                targetFoodSource.CurrentFood--;
                foodLevel += (int)targetFoodSource.FoodSatiety;
                if (foodLevel > (int)maxFoodLevel) foodLevel = (int)maxFoodLevel;
                UpdateColor();
            }
            else
            {
                if (foodLevel >= (int)maxFoodLevel)
                {
                    currentState = State.Idle;
                    Debug.Log($"{name}: Full, reverting to Idle");
                }
                else
                {
                    currentState = State.SearchingForFood;
                    Debug.Log($"{name}: Food source empty or invalid, searching for food");
                }
                agent.isStopped = false;
            }
            eatTimer = 0.00f;
        }
    }

    private void UpdateColor()
    {
        if (creatureRenderer != null)
        {
            float t = foodLevel / maxFoodLevel;
            Color newColor = Color.Lerp(emptyColor, fullColor, t);
            creatureRenderer.material.color = newColor;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (herdMentality == HerdMentalityType.Isolation)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius / 2.00f);
        }
    }
}