using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Code is using Unity 6 (6000.0.37f1)
// This script controls the behavior of a creature in a simulation, including movement, eating, reproduction, and herd mentality.
// Code should work with multiplayer netcode for gameobjects

public class CreatureBehavior : MonoBehaviour
{
    // ---- Core Components ----
    private NavMeshAgent agent;
    private Renderer creatureRenderer;
    private CreatureCombat combat;

    // ---- Creature States ----
    public enum State { Idle, SearchingForFood, Panic, Eating, SeekingMate, Attacking, Fleeing, Dead }
    [Header("Creature States")]
    [SerializeField] private State currentState = State.Idle;

    // Public properties for accessibility by CreatureCombat
    public State CurrentState
    {
        get => currentState;
        set => currentState = value;
    }
    public Transform AttackTarget
    {
        get => attackTarget;
        set => attackTarget = value;
    }
    public Transform FleeFrom
    {
        get => fleeFrom;
        set => fleeFrom = value;
    }
    public NavMeshAgent Agent => agent;

    // ---- Herd Mentality ----
    public enum HerdMentalityType { Herd, Ignores, Isolation }
    [Header("Herd Mentality")]
    [SerializeField] private HerdMentalityType herdMentality = HerdMentalityType.Ignores;

    [Header("Creature Type")]
    [SerializeField] private int creatureTypeId = -1;

    [Header("Physical Attributes")]
    [SerializeField] private float size = 1f;
    [SerializeField] private float walkingSpeed = 4f;
    [SerializeField] private bool canClimb = false;
    private float newWalkingSpeed => Mathf.Round(walkingSpeed * (size < 1f ? (2f - size) : Mathf.Pow(size, -0.252f)) * 100f) / 100f;
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

    [Header("Combat Targets")]
    [SerializeField] private Transform attackTarget;
    [SerializeField] private Transform fleeFrom;

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

    // ---- Static Counter for Unique IDs ----
    private static int nextAvailableId = 0;

    // ---- NavMesh Area Masks ----
    private const int WalkableArea = 1 << 0;
    private const int NotWalkableArea = 1 << 1;
    private const int JumpArea = 1 << 2;
    private const int ClimbArea = 1 << 3;

    // ---- Food Preferences with Weights ----
    public enum FoodType { Apple, Berry, Meat }
    [System.Serializable]
    public struct FoodPreference
    {
        public FoodType Type;
        public float Weight; // Preference weight (higher = more preferred)
    }
    [Header("Food Preferences")]
    [SerializeField] private List<FoodPreference> foodPreferences = new List<FoodPreference> { new FoodPreference { Type = FoodType.Apple, Weight = 1f } };
    private Dictionary<string, FoodType> tagToFoodTypeCache = new Dictionary<string, FoodType>();

    // ---- Cached Discoverables for Performance ----
    private Dictionary<string, List<Transform>> cachedDiscoverablesByTag = new Dictionary<string, List<Transform>>();

    void Start()
    {
        // Component initialization
        agent = GetComponent<NavMeshAgent>();
        if (!agent) { Debug.LogError($"{name}: No NavMeshAgent! Disabling."); enabled = false; return; }
        creatureRenderer = GetComponent<Renderer>();
        if (!creatureRenderer) Debug.LogWarning($"{name}: No Renderer! Color won’t update.");
        combat = GetComponent<CreatureCombat>();
        if (!combat) Debug.LogError($"{name}: No CreatureCombat component!");

        // NavMesh configuration
        int areaMask = WalkableArea | JumpArea;
        if (canClimb) areaMask |= ClimbArea;
        agent.areaMask = areaMask;

        // Initial setup
        size = Mathf.Round(size * 100f) / 100f;
        walkingSpeed = Mathf.Round(walkingSpeed * 100f) / 100f;
        lastReproductionTime = Time.time;
        RandomizeFoodPreferences(); // Randomize food preferences at start
        InitializeFoodTagCache();
        UpdateSizeAndStats();
        foodLevel = (int)maxFoodLevel;
        agent.speed = newWalkingSpeed;
        wanderInterval = Random.Range(2.5f, 10f);

        // Initial spawn logic
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
        if (currentState == State.Dead) return;

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
                health = Mathf.Ceil(health - 1);
                damageTimer = 0f;
                Debug.Log($"{name}: Starving, health: {health}");
                if (health <= 0) Die();
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
                health = Mathf.Ceil(health + 1);
                healTimer = 0f;
                if (health >= Mathf.Ceil(size * 10f)) isHealing = false;
            }
        }
        else if (health >= Mathf.Ceil(size * 10f)) { isHealing = false; healTimer = 0f; }

        // Trigger food search
        if (foodLevel <= maxFoodLevel * 0.4f && currentState == State.Idle)
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

        // Navigation timeout
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
            case State.Attacking: combat.AttackUpdate(); break;
            case State.Fleeing: combat.FleeUpdate(); break;
            case State.Dead: break;
        }
    }

    // ---- Helper Methods ----

    // Randomizes food preferences based on creature type for variety
    private void RandomizeFoodPreferences()
    {
        if (Random.value < 0.3f) // 30% chance to randomize
        {
            foodPreferences.Clear();
            int prefCount = Random.Range(1, 3); // 1-2 preferences
            FoodType[] types = { FoodType.Apple, FoodType.Berry, FoodType.Meat };
            for (int i = 0; i < prefCount; i++)
            {
                FoodType type = types[Random.Range(0, types.Length)];
                if (!foodPreferences.Exists(p => p.Type == type))
                    foodPreferences.Add(new FoodPreference { Type = type, Weight = Random.Range(0.5f, 1.5f) });
            }
            Debug.Log($"{name}: Randomized food preferences: {string.Join(", ", foodPreferences.ConvertAll(p => $"{p.Type} ({p.Weight})"))}");
        }
    }

    // Initializes the tag-to-food-type cache for efficiency
    private void InitializeFoodTagCache()
    {
        tagToFoodTypeCache["Apple"] = FoodType.Apple;
        tagToFoodTypeCache["Berry"] = FoodType.Berry;
        tagToFoodTypeCache["Meat"] = FoodType.Meat;
    }

    // Updates and caches visible discoverables
    private void UpdateDiscoverablesDetection()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, discoverableLayer);
        visibleDiscoverables.Clear();
        cachedDiscoverablesByTag.Clear();
        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            visibleDiscoverables.Add(hit.transform);
            string tag = hit.tag;
            if (!cachedDiscoverablesByTag.ContainsKey(tag))
                cachedDiscoverablesByTag[tag] = new List<Transform>();
            cachedDiscoverablesByTag[tag].Add(hit.transform);
        }
    }

    private void UpdateSizeAndStats()
    {
        transform.localScale = Vector3.one * size * ageSize;
        health = Mathf.Ceil(size * 10f);
        maxFoodLevel = Mathf.Ceil(6f + (size * 4f));
        hungerDecreaseInterval = Mathf.Round(
            30f * (size < 1f ? (1f + (2f / 3f) * (1f - size)) : (1f / (1f + 0.2f * (size - 1f)))) * 100f
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
        if (visibleDiscoverables.Count == 0 || foodPreferences.Count == 0)
        {
            PanicWander();
            return;
        }

        // Sort preferences by weight (descending)
        foodPreferences.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        foreach (var pref in foodPreferences)
        {
            string tag = GetTagFromFoodType(pref.Type);
            if (!cachedDiscoverablesByTag.ContainsKey(tag)) continue;

            Transform closest = null;
            float minDist = float.MaxValue;
            foreach (var obj in cachedDiscoverablesByTag[tag])
            {
                float dist = Vector3.Distance(transform.position, obj.position);
                if (dist < minDist && IsNavigable(obj.position))
                {
                    minDist = dist;
                    closest = obj;
                }
            }

            if (closest != null)
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
                    return;
                }
            }
        }

        // Hunter logic
        if (combat.CombatBehavior == CreatureCombat.CombatBehavior.Hunter && foodLevel < maxFoodLevel * 0.25f)
        {
            Transform huntTarget = FindHuntableCreature();
            if (huntTarget != null)
            {
                attackTarget = huntTarget;
                currentState = State.Attacking;
                agent.SetDestination(huntTarget.position);
                Debug.Log($"{name}: Hunting {huntTarget.name}");
                return;
            }
        }

        PanicWander();
    }

    private Transform FindHuntableCreature()
    {
        if (!cachedDiscoverablesByTag.ContainsKey("Creature")) return null;
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var obj in cachedDiscoverablesByTag["Creature"])
        {
            CreatureBehavior other = obj.GetComponent<CreatureBehavior>();
            if (other != null && other.creatureTypeId != creatureTypeId && other.CurrentState != State.Dead)
            {
                float dist = Vector3.Distance(transform.position, obj.position);
                if (dist < minDist && IsNavigable(obj.position))
                {
                    minDist = dist;
                    closest = obj;
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
        if (cachedDiscoverablesByTag.ContainsKey("Creature"))
        {
            foreach (var obj in cachedDiscoverablesByTag["Creature"])
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
        if (!cachedDiscoverablesByTag.ContainsKey("Creature")) return false;
        foreach (var obj in cachedDiscoverablesByTag["Creature"])
            if (obj.GetComponent<CreatureBehavior>()?.creatureTypeId == creatureTypeId)
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

    public void TakeDamage(float damage, CreatureBehavior attacker)
    {
        if (currentState == State.Dead) return;
        health = Mathf.Ceil(health - damage);
        Debug.Log($"{name}: Took {damage} damage, health: {health}");
        if (health <= 0)
        {
            Die();
        }
        else
        {
            combat.OnAttacked(attacker);
        }
    }

    private void Die()
    {
        currentState = State.Dead;
        agent.enabled = false;
        transform.Rotate(0, 180, 0);
        gameObject.tag = "Meat";
        FoodSource foodSource = GetComponent<FoodSource>();
        if (foodSource != null)
        {
            foodSource.enabled = true;
            foodSource.isNotReplenishable = true;
            foodSource.maxFood = (int)(size * 5);
            foodSource.CurrentFood = foodSource.maxFood;
        }
        Debug.Log($"{name}: Died and became meat");
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