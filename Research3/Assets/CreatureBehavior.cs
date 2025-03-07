using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// DO NOT REMOVE THIS COMMENT: Code is using Unity 6 (6000.0.37f1)
// DO NOT REMOVE THIS COMMENT: Make sure the code is easy to understand, and isn't inefficient. Make the code slightly more efficient.
// DO NOT REMOVE THIS COMMENT: This script controls the behavior of a creature in a simulation, including movement, eating, reproduction, and herd mentality.
// DO NOT REMOVE THIS COMMENT: Can you add any additional comments to clarify the functionality of certain parts of the code? - Added below.
// DO NOT REMOVE THIS COMMENT: Code should work with multiplayer netcode for gameobjects (currently simplified, Netcode can be re-added).

public class CreatureBehavior : MonoBehaviour
{
    // ---- Core Components ----
    private NavMeshAgent agent;
    private Renderer creatureRenderer;

    // ---- Inspector Groups ----

    // Creature states enum (no header here, as it's invalid on enum)
    public enum State { Idle, SearchingForFood, Panic, Eating, SeekingMate }
    [Header("Creature States")]
    [SerializeField] private State currentState = State.Idle;

    // Herd mentality enum (no header here)
    public enum HerdMentalityType { Herd, Ignores, Isolation }
    [Header("Herd Mentality")]
    [SerializeField] private HerdMentalityType herdMentality = HerdMentalityType.Ignores;

    [Header("Creature Type")]
    [SerializeField] private int creatureTypeId = -1; // Unique ID (0-9999), assigned at spawn

    [Header("Physical Attributes")]
    [SerializeField] private float size = 1f;
    [SerializeField] private float walkingSpeed = 4f;
    private float newWalkingSpeed => 
        Mathf.Round(walkingSpeed * 
        (size < 1f 
            ? (2f - size) 
            : Mathf.Pow(size, -0.252f)
        ) * 100f) / 100f;

    private float sprintSpeed => Mathf.Round(newWalkingSpeed * 2.0f * 100f) / 100f;

    [Header("Health and Hunger")]
    [SerializeField] private float health; // Now rounded up to whole numbers
    [SerializeField] private int foodLevel;
    private float maxFoodLevel;
    private float hungerTimer = 0f;
    private float hungerDecreaseInterval;
    private float damageTimer = 0f;
    private float healTimer = 0f;
    private bool isHealing = false;

    [Header("Age and Growth")]
    [SerializeField] private int age = 0;
    private float ageTimer = 0f;
    private const float ageIncreaseInterval = 30f; // Seconds per age increment
    private float ageSize => Mathf.Round(Mathf.Lerp(0.3f, 1f, Mathf.Min(age / adultAge, 1f)) * 100f) / 100f;
    private const float adultAge = 10f;

    [Header("Reproduction")]
    [SerializeField] private float lastReproductionTime = 0f;
    private float reproductionTimer = 0f;
    private const float reproductionCheckInterval = 40f; // How often to check for reproduction
    private const float reproductionCooldown = 120f; // Cooldown between reproductions
    private Transform reproductionTarget;
    private const float reproductionDistance = 5f; // Distance to initiate reproduction
    [SerializeField] private float mutationRate = 0.1f;

    [Header("Detection and Interaction")]
    [SerializeField] private List<Transform> visibleDiscoverables = new(); // Vision list in Inspector
    private FoodSource targetFoodSource;
    [SerializeField] private float baseDetectionRadius = 50f; // Default detection radius at size 1.0, now adjustable
    private float detectionRadius => baseDetectionRadius + (Mathf.Floor(size - 1f) * (baseDetectionRadius / 5f)); // Scales with size
    [SerializeField] private LayerMask discoverableLayer;
    [SerializeField] private float eatingDistance = 1f;

    [Header("Movement and Wandering")]
    [SerializeField] private float wanderRadius = 20f;
    private float wanderInterval = 5f; // Initial value, will be randomized in Wander()
    private float wanderTimer = 0f;
    private float panicWanderRadius => Mathf.Round(wanderRadius * 2.0f * 100f) / 100f;
    private float navigationTimer = 0f;
    private const float navigationTimeout = 10f; // Timeout for path reset

    [Header("Eating Mechanics")]
    private float eatTimer = 0f;

    [Header("Herd Mentality Timing")]
    private float mentalityCheckTimer = 0f;
    private const float mentalityCheckInterval = 60f; // How often to check herd mentality

    [Header("Visual Feedback")]
    private readonly Color fullColor = Color.green;
    private readonly Color emptyColor = Color.red;

    [Header("Info (Read-Only Debug Stats)")]
    [SerializeField] [Tooltip("Current walking speed adjusted by size")] private float _newWalkingSpeedDisplay => newWalkingSpeed;
    [SerializeField] [Tooltip("Current sprint speed")] private float _sprintSpeedDisplay => sprintSpeed;
    [SerializeField] [Tooltip("Maximum food capacity")] private float _maxFoodLevelDisplay => maxFoodLevel;
    [SerializeField] [Tooltip("Time since last hunger decrease")] private float _hungerTimerDisplay => hungerTimer;
    [SerializeField] [Tooltip("Interval between hunger decreases")] private float _hungerDecreaseIntervalDisplay => hungerDecreaseInterval;
    [SerializeField] [Tooltip("Current detection radius based on size")] private float _detectionRadiusDisplay => detectionRadius;
    [SerializeField] [Tooltip("Current physical size with age scaling")] private float _currentSizeDisplay => size * ageSize;
    [SerializeField] [Tooltip("Time since last reproduction")] private float _timeSinceLastReproduction => Time.time - lastReproductionTime;

    // ---- Static Counter for Unique IDs ----
    private static int nextAvailableId = 0; // Increments for each new type

    void Start()
    {
        // Initialize components
        agent = GetComponent<NavMeshAgent>();
        if (!agent) { Debug.LogError($"{name}: No NavMeshAgent! Disabling."); enabled = false; return; }
        creatureRenderer = GetComponent<Renderer>();
        if (!creatureRenderer) Debug.LogWarning($"{name}: No Renderer! Color won’t update.");

        // Round attributes for consistency
        size = Mathf.Round(size * 100f) / 100f;
        walkingSpeed = Mathf.Round(walkingSpeed * 100f) / 100f;

        // Set initial stats
        lastReproductionTime = Time.time;
        UpdateSizeAndStats();
        foodLevel = (int)maxFoodLevel;
        agent.speed = newWalkingSpeed;

        // Assign RANDOM wander interval
        wanderInterval = Random.Range(2.5f, 12.5f);

        // Assign ID if not set (e.g., manually placed creatures)
        if (creatureTypeId == -1)
        {
            creatureTypeId = nextAvailableId++;
            age = 10; // Start as adult
            UpdateSizeAndStats();
            int cloneAge = 8;
            for (int i = 0; i < 3; i++)
            {
                AsexualReproduction(cloneAge);
                cloneAge -= 2;
            }
            Debug.Log($"{name}: Assigned type {creatureTypeId}, spawned clones (ages 10, 8, 6, 4)");
        }

        UpdateColor();
        Debug.Log($"{name}: Initialized - Type: {creatureTypeId}, Size: {size}, Age: {age}, Detection Radius: {detectionRadius}");
    }

    void Update()
    {
        // Cache Time.deltaTime for efficiency (reduces multiple property accesses)
        float deltaTime = Time.deltaTime;

        UpdateDiscoverablesDetection();

        // Age progression
        ageTimer += deltaTime;
        if (ageTimer >= ageIncreaseInterval)
        {
            age++;
            ageTimer = 0f;
            UpdateSizeAndStats();
            if (age == adultAge) Debug.Log($"{name}: Now adult at age {age}");
        }

        // Hunger management
        float hungerInterval = isHealing ? hungerDecreaseInterval / 2f : hungerDecreaseInterval;
        hungerTimer += deltaTime;
        if (hungerTimer >= hungerInterval)
        {
            foodLevel = Mathf.Max(foodLevel - 1, 0);
            hungerTimer = 0f;
            UpdateColor();
        }

        // Starvation damage
        if (foodLevel <= 0)
        {
            damageTimer += deltaTime;
            if (damageTimer >= 5f)
            {
                health = Mathf.Ceil(health - 1); // Round up after decrement
                damageTimer = 0f;
                Debug.Log($"{name}: Starving, health: {health}");
                if (health <= 0) { Debug.Log($"{name}: Died"); Destroy(gameObject); }
            }
        }
        else damageTimer = 0f;

        // Healing
        if (foodLevel > 0 && health < Mathf.Ceil(size * 10f))
        {
            isHealing = true;
            healTimer += deltaTime;
            if (healTimer >= 10f)
            {
                health = Mathf.Ceil(health + 1); // Round up after increment
                healTimer = 0f;
                if (health >= Mathf.Ceil(size * 10f)) isHealing = false;
            }
        }
        else if (health >= Mathf.Ceil(size * 10f)) { isHealing = false; healTimer = 0f; }

        // Trigger food search at 50% hunger
        if (foodLevel <= maxFoodLevel * 0.5f && currentState == State.Idle)
        {
            currentState = State.SearchingForFood;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Hungry, searching");
        }

        // Herd mentality check
        if (herdMentality != HerdMentalityType.Ignores)
        {
            mentalityCheckTimer += deltaTime;
            if (mentalityCheckTimer >= mentalityCheckInterval)
            {
                CheckMentality();
                mentalityCheckTimer = 0f;
            }
        }

        // Reproduction check
        if (age >= adultAge)
        {
            reproductionTimer += deltaTime;
            if (reproductionTimer >= reproductionCheckInterval && currentState != State.Eating && currentState != State.SeekingMate)
            {
                CheckReproduction();
                reproductionTimer = 0f;
            }
        }

        // Navigation timeout to prevent getting stuck
        if (agent.hasPath)
        {
            navigationTimer += deltaTime;
            if (navigationTimer >= navigationTimeout)
            {
                agent.ResetPath();
                navigationTimer = 0f;
                Debug.Log($"{name}: Navigation timeout");
            }
        }
        else navigationTimer = 0f;

        // State machine
        switch (currentState)
        {
            case State.Idle: Wander(); break;
            case State.SearchingForFood: SearchForFood(); break;
            case State.Panic: HandlePanic(); break;
            case State.Eating: Eat(); break;
            case State.SeekingMate: SeekMate(); break;
        }
    }

    // Updates the list of visible objects within detection radius
    private void UpdateDiscoverablesDetection()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, discoverableLayer);
        visibleDiscoverables.Clear();
        foreach (var hit in hits)
            if (hit.transform != transform) // Exclude self
                visibleDiscoverables.Add(hit.transform);
    }

    // Updates stats based on size and age, ensuring health is a whole number
    private void UpdateSizeAndStats()
    {
        transform.localScale = Vector3.one * size * ageSize;
        health = Mathf.Ceil(size * 10f); // Health rounded up to nearest whole number
        maxFoodLevel = Mathf.Ceil(size * 10f);
        
        hungerDecreaseInterval = Mathf.Round(
            60f *
            (size < 1f 
                ? (1f + (2f / 3f) * (1f - size))    // For sizes below 1: 0.75 → ~70, 0.5 → ~80
                : (1f / (1f + 0.2f * (size - 1f)))   // For sizes 1 or above: 2 → ~50, 3 → ~42.5, 4 → ~37.5
            ) * 100f
        ) / 100f;
    }

    private void Wander()
    {
        if (agent.speed != newWalkingSpeed) agent.speed = newWalkingSpeed;
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * wanderRadius;
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            wanderTimer = 0f;
            wanderInterval = Random.Range(2.5f, 10f); // Randomize interval between 2.5 and 10 seconds
        }
    }

    private void SearchForFood()
    {
        if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;
        if (visibleDiscoverables.Count == 0) { PanicWander(); return; }
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var obj in visibleDiscoverables)
        {
            if (obj.CompareTag("Apple"))
            {
                float dist = Vector3.Distance(transform.position, obj.position);
                if (dist < minDist) { minDist = dist; closest = obj; }
            }
        }
        if (closest)
        {
            targetFoodSource = closest.GetComponent<FoodSource>();
            if (targetFoodSource?.HasFood ?? false)
            {
                agent.SetDestination(closest.position);
                if (minDist <= eatingDistance)
                {
                    currentState = State.Eating;
                    agent.isStopped = true;
                    eatTimer = 0f;
                }
            }
            else { targetFoodSource = null; PanicWander(); }
        }
        else PanicWander();
    }

    private void HandlePanic()
    {
        if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;
        if (herdMentality == HerdMentalityType.Herd && HasCreaturesOfSameTypeInRange())
        {
            currentState = State.Idle;
            Debug.Log($"{name}: Herd calmed (type {creatureTypeId})");
        }
        else if (herdMentality == HerdMentalityType.Isolation) AvoidCreaturesOfSameType();
        else PanicWander();
    }

    private void SeekMate()
    {
        if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;
        if (reproductionTarget)
        {
            agent.SetDestination(reproductionTarget.position);
            if (Vector3.Distance(transform.position, reproductionTarget.position) <= reproductionDistance)
                AttemptReproduction();
        }
        else
        {
            currentState = State.Idle;
            Debug.Log($"{name}: Mate lost");
        }
    }

    private void PanicWander()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * panicWanderRadius;
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, panicWanderRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
    }

    private void AvoidCreaturesOfSameType()
    {
        float isolationRadius = detectionRadius / 1.7f;
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var obj in visibleDiscoverables)
        {
            if (obj.CompareTag("Creature"))
            {
                CreatureBehavior other = obj.GetComponent<CreatureBehavior>();
                if (other?.creatureTypeId == creatureTypeId)
                {
                    float dist = Vector3.Distance(transform.position, obj.position);
                    if (dist < minDist) { minDist = dist; nearest = obj; }
                }
            }
        }
        if (nearest && minDist <= isolationRadius)
        {
            Vector3 away = (transform.position - nearest.position).normalized * (isolationRadius + 1f);
            if (NavMesh.SamplePosition(transform.position + away, out NavMeshHit hit, panicWanderRadius, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
        else currentState = State.Idle;
    }

    private void CheckMentality()
    {
        bool nearby = HasCreaturesOfSameTypeInRange();
        if (herdMentality == HerdMentalityType.Herd && !nearby && currentState != State.Eating && currentState != State.SearchingForFood)
        {
            currentState = State.Panic;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Herd alone, panicking");
        }
        else if (herdMentality == HerdMentalityType.Isolation && nearby && currentState != State.Eating && currentState != State.SearchingForFood)
        {
            currentState = State.Panic;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Isolation crowded, panicking");
        }
    }

    private bool HasCreaturesOfSameTypeInRange()
    {
        foreach (var obj in visibleDiscoverables)
            if (obj.CompareTag("Creature") && obj.GetComponent<CreatureBehavior>()?.creatureTypeId == creatureTypeId)
                return true;
        return false;
    }

    private void CheckReproduction()
    {
        if (CanReproduce() && Random.value < 0.333f)
        {
            reproductionTarget = FindNearestCreatureOfSameTypeInReproductionRange();
            if (reproductionTarget)
            {
                currentState = State.SeekingMate;
                agent.SetDestination(reproductionTarget.position);
                Debug.Log($"{name}: Seeking mate {reproductionTarget.name}");
            }
            else Debug.Log($"{name}: No mate found");
        }
    }

    private Transform FindNearestCreatureOfSameTypeInReproductionRange()
    {
        float range = detectionRadius * 1.5f;
        Transform nearest = null;
        float minDist = float.MaxValue;
        Collider[] hits = Physics.OverlapSphere(transform.position, range, discoverableLayer);
        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            if (hit.CompareTag("Creature"))
            {
                CreatureBehavior other = hit.GetComponent<CreatureBehavior>();
                if (other?.creatureTypeId == creatureTypeId)
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < minDist) { minDist = dist; nearest = hit.transform; }
                }
            }
        }
        return nearest;
    }

    private void AttemptReproduction()
    {
        CreatureBehavior partner = reproductionTarget?.GetComponent<CreatureBehavior>();
        if (partner?.CanReproduce() ?? false && partner.creatureTypeId == creatureTypeId)
        {
            SpawnChild(partner);
            lastReproductionTime = Time.time;
            partner.lastReproductionTime = Time.time;
            reproductionTarget = null;
            currentState = State.Idle;
            Debug.Log($"{name}: Reproduced with {partner.name}");
        }
        else
        {
            reproductionTarget = null;
            currentState = State.Idle;
            Debug.Log($"{name}: Reproduction failed");
        }
    }

    private bool CanReproduce() => foodLevel >= maxFoodLevel * 0.8f && (Time.time - lastReproductionTime) >= reproductionCooldown && age >= adultAge;

    private void SpawnChild(CreatureBehavior partner)
    {
        GameObject child = Instantiate(gameObject, transform.position + Vector3.right * 2f, Quaternion.identity);
        CreatureBehavior childBehavior = child.GetComponent<CreatureBehavior>();

        float avgSize = Mathf.Round((size + partner.size) / 2f * 100f) / 100f;
        float avgSpeed = Mathf.Round((walkingSpeed + partner.walkingSpeed) / 2f * 100f) / 100f;
        childBehavior.size = Mathf.Round((avgSize + Random.Range(-mutationRate, mutationRate) * avgSize) * 100f) / 100f;
        childBehavior.walkingSpeed = Mathf.Round((avgSpeed + Random.Range(-mutationRate, mutationRate) * avgSpeed) * 100f) / 100f;

        childBehavior.age = 0;
        childBehavior.foodLevel = (int)childBehavior.maxFoodLevel;
        childBehavior.lastReproductionTime = Time.time;
        childBehavior.creatureTypeId = creatureTypeId;
    }

    private void AsexualReproduction(int startingAge)
    {
        GameObject child = Instantiate(gameObject, transform.position + Vector3.right * 2f, Quaternion.identity);
        CreatureBehavior childBehavior = child.GetComponent<CreatureBehavior>();

        float sizeVar = Random.Range(-mutationRate, mutationRate) * size;
        childBehavior.size = Mathf.Round((size + sizeVar) * 100f) / 100f;
        float speedVar = Random.Range(-mutationRate, mutationRate) * walkingSpeed;
        childBehavior.walkingSpeed = Mathf.Round((walkingSpeed + speedVar) * 100f) / 100f;

        childBehavior.age = startingAge;
        childBehavior.foodLevel = (int)childBehavior.maxFoodLevel;
        childBehavior.lastReproductionTime = Time.time;
        childBehavior.creatureTypeId = creatureTypeId;
    }

    private void Eat()
    {
        if (agent.speed != newWalkingSpeed) agent.speed = newWalkingSpeed;
        eatTimer += Time.deltaTime;
        if (eatTimer >= 1f)
        {
            if (targetFoodSource?.HasFood ?? false && targetFoodSource.CurrentFood > 0 && foodLevel < maxFoodLevel)
            {
                targetFoodSource.CurrentFood--;
                foodLevel = Mathf.Min(foodLevel + (int)targetFoodSource.FoodSatiety, (int)maxFoodLevel);
                UpdateColor();
            }
            else
            {
                currentState = foodLevel >= maxFoodLevel ? State.Idle : State.SearchingForFood;
                agent.isStopped = false;
                Debug.Log($"{name}: {(foodLevel >= maxFoodLevel ? "Full" : "Food gone")}");
            }
            eatTimer = 0f;
        }
    }

    private void UpdateColor()
    {
        if (creatureRenderer)
        {
            float t = foodLevel / maxFoodLevel;
            creatureRenderer.material.color = Color.Lerp(emptyColor, fullColor, t);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        if (herdMentality == HerdMentalityType.Isolation)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius / 1.7f);
        }
    }
}