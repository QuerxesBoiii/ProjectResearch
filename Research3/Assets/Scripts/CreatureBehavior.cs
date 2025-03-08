using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CreatureBehavior : MonoBehaviour
{
    // ---- Core Components ----
    private NavMeshAgent agent;
    private Renderer creatureRenderer;
    private CreatureCombat creatureCombat;

    // Public accessor for agent to allow CreatureCombat to use it
    public NavMeshAgent Agent => agent;

    // ---- Inspector Groups ----

    public enum State { Idle, SearchingForFood, Panic, Eating, SeekingMate, Attacking, Fleeing }
    [Header("Creature States")]
    [SerializeField] public State currentState = State.Idle;

    public enum HerdMentalityType { Herd, Ignores, Isolation }
    [Header("Herd Mentality")]
    [SerializeField] private HerdMentalityType herdMentality = HerdMentalityType.Ignores;

    [Header("Creature Type")]
    [SerializeField] private int creatureTypeId = -1;

    [Header("Physical Attributes")]
    [SerializeField] private float size = 1f;
    [SerializeField] private float walkingSpeed = 4f;
    [SerializeField] private bool canClimb = false;
    [SerializeField] private GameObject meatPrefab; // Prefab with fixed food values
    private float newWalkingSpeed => 
        Mathf.Round(walkingSpeed * 
        (size < 1f 
            ? (2f - size) 
            : Mathf.Pow(size, -0.252f)
        ) * 100f) / 100f;

    private float sprintSpeed => Mathf.Round(newWalkingSpeed * 2.0f * 100f) / 100f;

    [Header("Health and Hunger")]
    [SerializeField] private float health;
    [SerializeField] private int foodLevel;
    private float maxFoodLevel;
    private float hungerTimer = 0f;
    [SerializeField] private float hungerDecreaseInterval;
    private float damageTimer = 0f;
    private float healTimer = 0f;
    private bool isHealing = false;

    [Header("Age and Growth")]
    [SerializeField] private int age = 0;
    private float ageTimer = 0f;
    private const float ageIncreaseInterval = 30f;
    private float ageSize => Mathf.Round(Mathf.Lerp(0.3f, 1f, Mathf.Min(age / adultAge, 1f)) * 100f) / 100f;
    private const float adultAge = 10f;

    [Header("Reproduction")]
    [SerializeField] private float lastReproductionTime = 0f;
    private float reproductionTimer = 0f;
    private const float reproductionCheckInterval = 40f;
    private const float reproductionCooldown = 120f;
    private Transform reproductionTarget;
    [SerializeField] private const float reproductionDistance = 8f;
    [SerializeField] private float mutationRate = 0.1f;

    [Header("Detection and Interaction")]
    [SerializeField] private List<Transform> visibleDiscoverables = new();
    private FoodSource targetFoodSource;
    [SerializeField] private float baseDetectionRadius = 50f;
    [SerializeField] private float detectionRadius => baseDetectionRadius + (Mathf.Floor(size - 1f) * (baseDetectionRadius / 5f));
    [SerializeField] private LayerMask discoverableLayer;
    [SerializeField] private float eatingDistance = 1f;

    [Header("Movement and Wandering")]
    [SerializeField] private float wanderRadius = 20f;
    private float wanderInterval = 5f;
    private float wanderTimer = 0f;
    private float panicWanderRadius => Mathf.Round(wanderRadius * 2.0f * 100f) / 100f;
    private float navigationTimer = 0f;
    private const float navigationTimeout = 10f;

    [Header("Eating Mechanics")]
    private float eatTimer = 0f;

    [Header("Herd Mentality Timing")]
    private float mentalityCheckTimer = 0f;
    private const float mentalityCheckInterval = 60f;

    [Header("Visual Feedback")]
    private readonly Color fullColor = Color.green;
    private readonly Color emptyColor = Color.red;

    [Header("Info (Read-Only Debug Stats)")]
    [SerializeField] private float _newWalkingSpeedDisplay => newWalkingSpeed;
    [SerializeField] private float _sprintSpeedDisplay => sprintSpeed;
    [SerializeField] private float _maxFoodLevelDisplay => maxFoodLevel;
    [SerializeField] private float _hungerTimerDisplay => hungerTimer;
    [SerializeField] private float _hungerDecreaseIntervalDisplay => hungerDecreaseInterval;
    [SerializeField] private float _detectionRadiusDisplay => detectionRadius;
    [SerializeField] private float _currentSizeDisplay => size * ageSize;
    [SerializeField] private float _timeSinceLastReproduction => Time.time - lastReproductionTime;

    private static int nextAvailableId = 0;

    private const int WalkableArea = 1 << 0;
    private const int NotWalkableArea = 1 << 1;
    private const int JumpArea = 1 << 2;
    private const int ClimbArea = 1 << 3;

    public enum FoodType { Apple, Berry, Meat }
    [Header("Food Preferences")]
    [SerializeField] private List<FoodType> foodPreferences = new List<FoodType> { FoodType.Apple };

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent) { Debug.LogError($"{name}: No NavMeshAgent! Disabling."); enabled = false; return; }
        creatureRenderer = GetComponent<Renderer>();
        if (!creatureRenderer) Debug.LogWarning($"{name}: No Renderer! Color won’t update.");
        creatureCombat = GetComponent<CreatureCombat>();
        if (!creatureCombat) Debug.LogWarning($"{name}: No CreatureCombat component found!");
        if (!meatPrefab) Debug.LogWarning($"{name}: No meatPrefab assigned!");

        size = Mathf.Round(size * 100f) / 100f;
        walkingSpeed = Mathf.Round(walkingSpeed * 100f) / 100f;

        int areaMask = WalkableArea | JumpArea;
        if (canClimb)
        {
            areaMask |= ClimbArea;
        }
        agent.areaMask = areaMask;

        lastReproductionTime = Time.time;
        UpdateSizeAndStats();
        foodLevel = (int)maxFoodLevel;
        agent.speed = newWalkingSpeed;

        wanderInterval = Random.Range(2.5f, 10f);

        if (creatureTypeId == -1)
        {
            creatureTypeId = nextAvailableId++;
            age = 10;
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
        Debug.Log($"{name}: Initialized - Type: {creatureTypeId}, Size: {size}, Age: {age}, Detection Radius: {detectionRadius}, Can Climb: {canClimb}");
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateDiscoverablesDetection();

        ageTimer += deltaTime;
        if (ageTimer >= ageIncreaseInterval)
        {
            age++;
            ageTimer = 0f;
            UpdateSizeAndStats();
            if (age == adultAge) Debug.Log($"{name}: Now adult at age {age}");
        }

        float hungerInterval = isHealing ? hungerDecreaseInterval / 2f : hungerDecreaseInterval;
        hungerTimer += deltaTime;
        if (hungerTimer >= hungerInterval)
        {
            foodLevel = Mathf.Max(foodLevel - 1, 0);
            hungerTimer = 0f;
            UpdateColor();
        }

        if (foodLevel <= 0)
        {
            damageTimer += deltaTime;
            if (damageTimer >= 5f)
            {
                health = Mathf.Ceil(health - 1);
                damageTimer = 0f;
                Debug.Log($"{name}: Starving, health: {health}");
                if (health <= 0) Die();
            }
        }
        else damageTimer = 0f;

        if (foodLevel > 0 && health < Mathf.Ceil(size * 10f))
        {
            isHealing = true;
            healTimer += deltaTime;
            if (healTimer >= 10f)
            {
                health = Mathf.Ceil(health + 1);
                healTimer = 0f;
                if (health >= Mathf.Ceil(size * 10f)) isHealing = false;
            }
        }
        else if (health >= Mathf.Ceil(size * 10f)) { isHealing = false; healTimer = 0f; }

        // Changed from 0.5f to 0.4f
        if (foodLevel <= maxFoodLevel * 0.4f && currentState == State.Idle)
        {
            currentState = State.SearchingForFood;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Hungry, searching");
        }

        if (herdMentality != HerdMentalityType.Ignores)
        {
            mentalityCheckTimer += deltaTime;
            if (mentalityCheckTimer >= mentalityCheckInterval)
            {
                CheckMentality();
                mentalityCheckTimer = 0f;
            }
        }

        if (age >= adultAge)
        {
            reproductionTimer += deltaTime;
            if (reproductionTimer >= reproductionCheckInterval && currentState != State.Eating && currentState != State.SeekingMate)
            {
                CheckReproduction();
                reproductionTimer = 0f;
            }
        }

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

        creatureCombat?.HandleCombatReactions();

        switch (currentState)
        {
            case State.Idle: Wander(); break;
            case State.SearchingForFood: SearchForFood(); break;
            case State.Panic: HandlePanic(); break;
            case State.Eating: Eat(); break;
            case State.SeekingMate: SeekMate(); break;
            case State.Attacking: creatureCombat.PerformAttack(); break;
            case State.Fleeing: creatureCombat.PerformFlee(); break;
        }
    }

    private void UpdateDiscoverablesDetection()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, discoverableLayer);
        visibleDiscoverables.Clear();
        foreach (var hit in hits)
            if (hit.transform != transform)
                visibleDiscoverables.Add(hit.transform);
    }

    private void UpdateSizeAndStats()
    {
        transform.localScale = Vector3.one * size * ageSize;
        health = Mathf.Ceil(size * 10f);
        maxFoodLevel = Mathf.Ceil(6f + (size * 4f));
        hungerDecreaseInterval = Mathf.Round(
            30f *
            (size < 1f 
                ? (1f + (2f / 3f) * (1f - size))
                : (1f / (1f + 0.2f * (size - 1f)))
            ) * 100f
        ) / 100f;
    }

    private bool IsNavigable(Vector3 targetPosition)
    {
        NavMeshPath path = new NavMeshPath();
        bool pathValid = agent.CalculatePath(targetPosition, path);
        return pathValid && path.status == NavMeshPathStatus.PathComplete;
    }

    private void Wander()
    {
        if (agent.speed != newWalkingSpeed) agent.speed = newWalkingSpeed;
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * wanderRadius;
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, wanderRadius, agent.areaMask) && IsNavigable(hit.position))
            {
                agent.SetDestination(hit.position);
            }
            wanderTimer = 0f;
            wanderInterval = Random.Range(2.5f, 10f);
        }
    }

    private void SearchForFood()
    {
        if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;

        // Step 1: Check for meat first with extended radius
        if (foodPreferences.Contains(FoodType.Meat))
        {
            Transform closestMeat = FindClosestFood("Meat", detectionRadius * 1.5f);
            if (closestMeat != null)
            {
                targetFoodSource = closestMeat.GetComponent<FoodSource>();
                if (targetFoodSource?.HasFood ?? false)
                {
                    agent.SetDestination(closestMeat.position);
                    if (Vector3.Distance(transform.position, closestMeat.position) <= eatingDistance)
                    {
                        currentState = State.Eating;
                        agent.isStopped = true;
                        eatTimer = 0f;
                    }
                    return; // Exit early to prioritize meat
                }
            }
        }

        // Step 2: Check other preferred food types
        foreach (var preferredType in foodPreferences)
        {
            if (preferredType == FoodType.Meat) continue; // Already checked
            string tag = GetTagFromFoodType(preferredType);
            Transform closest = FindClosestFood(tag, detectionRadius);
            if (closest != null)
            {
                targetFoodSource = closest.GetComponent<FoodSource>();
                if (targetFoodSource?.HasFood ?? false)
                {
                    agent.SetDestination(closest.position);
                    if (Vector3.Distance(transform.position, closest.position) <= eatingDistance)
                    {
                        currentState = State.Eating;
                        agent.isStopped = true;
                        eatTimer = 0f;
                    }
                    return;
                }
            }
        }

        // Step 3: Hunt only if no food is found, creature is a hunter, and hunger is critical (0.3f)
        if (creatureCombat?.behaviorStyle == CreatureCombat.BehaviorStyle.Hunter && foodLevel <= maxFoodLevel * 0.3f)
        {
            Transform huntTarget = FindHuntTarget();
            if (huntTarget != null)
            {
                creatureCombat.SetAttackTarget(huntTarget);
                return;
            }
        }

        // Step 4: If no food or prey, wander
        PanicWander();
    }

    private Transform FindClosestFood(string tag, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, discoverableLayer);
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var hit in hits)
        {
            if (hit.CompareTag(tag))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = hit.transform;
                }
            }
        }
        return closest;
    }

    private Transform FindHuntTarget()
    {
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var obj in visibleDiscoverables)
        {
            if (obj.CompareTag("Creature"))
            {
                CreatureBehavior other = obj.GetComponent<CreatureBehavior>();
                if (other != null && other.creatureTypeId != creatureTypeId)
                {
                    float dist = Vector3.Distance(transform.position, obj.position);
                    if (dist < minDist && IsNavigable(obj.position))
                    {
                        minDist = dist;
                        closest = obj;
                    }
                }
            }
        }
        return closest;
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
            if (IsNavigable(reproductionTarget.position))
            {
                agent.SetDestination(reproductionTarget.position);
                if (Vector3.Distance(transform.position, reproductionTarget.position) <= reproductionDistance)
                    AttemptReproduction();
            }
            else
            {
                reproductionTarget = null;
                currentState = State.Idle;
                Debug.Log($"{name}: Mate unreachable");
            }
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
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, panicWanderRadius, agent.areaMask) && IsNavigable(hit.position))
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
            if (NavMesh.SamplePosition(transform.position + away, out NavMeshHit hit, panicWanderRadius, agent.areaMask) && IsNavigable(hit.position))
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
                if (IsNavigable(reproductionTarget.position))
                {
                    currentState = State.SeekingMate;
                    agent.SetDestination(reproductionTarget.position);
                    Debug.Log($"{name}: Seeking mate {reproductionTarget.name}");
                }
                else
                {
                    reproductionTarget = null;
                    Debug.Log($"{name}: Mate unreachable, skipping reproduction");
                }
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
        childBehavior.canClimb = canClimb || partner.canClimb;

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
        childBehavior.canClimb = canClimb;

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

    public void TakeDamage(float amount, Transform attacker)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
        else
        {
            creatureCombat?.OnAttacked(attacker);
            Debug.Log($"{name}: Took {amount} damage from {attacker.name}, health: {health}");
        }
    }

    private void Die()
    {
        if (meatPrefab != null)
        {
            int meatCount = Mathf.CeilToInt(maxFoodLevel / 6f);
            Debug.Log($"{name}: Died, spawning {meatCount} meat prefabs (maxFoodLevel = {maxFoodLevel})");

            for (int i = 0; i < meatCount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
                Vector3 spawnPosition = transform.position + offset;
                if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    Instantiate(meatPrefab, hit.position, transform.rotation);
                }
                else
                {
                    Instantiate(meatPrefab, transform.position, transform.rotation);
                }
            }
        }
        else
        {
            Debug.LogWarning($"{name}: No meatPrefab assigned, no meat spawned!");
        }

        Destroy(gameObject);
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

    private string GetTagFromFoodType(FoodType type)
    {
        switch (type)
        {
            case FoodType.Apple: return "Apple";
            case FoodType.Berry: return "Berry";
            case FoodType.Meat: return "Meat";
            default: return "";
        }
    }
}