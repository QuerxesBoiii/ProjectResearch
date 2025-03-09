using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// DO NOT REMOVE OR EDIT THIS COMMENT: Code is using Unity 6 (6000.0.37f1)
// DO NOT REMOVE OR EDIT THIS COMMENT: Make sure the code is easy to understand, and isn't inefficient. Make the code slightly more efficient.
// DO NOT REMOVE OR EDIT THIS COMMENT: This script controls the behavior of a creature in a simulation, including movement, eating, reproduction, and social/stranger mentality.
// DO NOT REMOVE OR EDIT THIS COMMENT: Can you add any additional comments to clarify the functionality of certain parts of the code? There should be a comment above every function.
// DO NOT REMOVE OR EDIT THIS COMMENT: Code should work with multiplayer netcode for gameobjects
// DO NOT REMOVE OR EDIT THIS COMMENT: Always provide full code whenever changes have occurred, don't make unnecessary changes.

public class CreatureBehavior : MonoBehaviour
{
    // ---- Core Components ----
    private NavMeshAgent agent;
    private Renderer creatureRenderer;
    public CreatureCombat creatureCombat { get; private set; }

    // ---- Inspector Groups ----

    public enum State { Idle, SearchingForFood, Panic, Eating, SeekingMate, Attacking, Fleeing, Dead }
    [Header("Creature States")]
    [SerializeField] public State currentState = State.Idle; // Made public for CreatureCombat access

    public enum SocialMentalityType { Herd, Ignores, Isolation }
    [Header("Social Mentality")]
    [SerializeField] private SocialMentalityType socialMentality = SocialMentalityType.Ignores;

    public enum StrangerMentalityType { Ignores, Avoids }
    [Header("Stranger Mentality")]
    [SerializeField] private StrangerMentalityType strangerMentality = StrangerMentalityType.Ignores;

    public enum CombatBehavior { Friendly, Neutral, Hunter }
    [Header("Combat Behavior")]
    [SerializeField] public CombatBehavior combatBehavior = CombatBehavior.Neutral; // Made public for CreatureCombat access

    [Header("Creature Type")]
    [SerializeField] private int creatureTypeId = -1;

    [Header("Physical Attributes")]
    [SerializeField] public float size = 1f;
    [SerializeField] private float walkingSpeed = 4f;
    [SerializeField] private bool canClimb = false;
    private float newWalkingSpeed => Mathf.Round(walkingSpeed * (size < 1f ? (2f - size) : Mathf.Pow(size, -0.252f)) * 100f) / 100f;
    private float sprintSpeed => Mathf.Round(newWalkingSpeed * 2.0f * 100f) / 100f;

    [Header("Health and Hunger")]
    [SerializeField] public float health; // Made public for CreatureCombat access
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

    [Header("Mentality Timing")]
    private float mentalityCheckTimer = 0f;
    private const float mentalityCheckInterval = 30f;

    [Header("Visual Feedback")]
    private readonly Color fullColor = Color.green;
    private readonly Color emptyColor = Color.red;

    // ---- Static Counter for Unique IDs ----
    private static int nextAvailableId = 0;

    // ---- NavMesh Area Masks ----
    private const int WalkableArea = 1 << 0;
    private const int NotWalkableArea = 1 << 1;
    private const int JumpArea = 1 << 2;
    private const int ClimbArea = 1 << 3;

    // ---- Food Preferences ----
    public enum FoodType { Apple, Berry, Meat }
    [Header("Food Preferences")]
    [SerializeField] private List<FoodType> foodPreferences = new List<FoodType> { FoodType.Apple };

    // ---- Combat ----
    [SerializeField] public Transform combatTarget; // Made public for CreatureCombat access
    private float attackTimer = 0f;
    private const float attackInterval = 1f;

    // ---- Initialization ----
    // Called when the script instance is being loaded
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent) { Debug.LogError($"{name}: No NavMeshAgent! Disabling."); enabled = false; return; }
        creatureRenderer = GetComponent<Renderer>();
        if (!creatureRenderer) Debug.LogWarning($"{name}: No Renderer! Color won’t update.");
        creatureCombat = GetComponent<CreatureCombat>();
        if (!creatureCombat) { Debug.LogError($"{name}: No CreatureCombat!"); }

        size = Mathf.Round(size * 100f) / 100f;
        walkingSpeed = Mathf.Round(walkingSpeed * 100f) / 100f;

        int areaMask = WalkableArea | JumpArea;
        if (canClimb) areaMask |= ClimbArea;
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

    // ---- Main Update Loop ----
    // Updates the creature’s behavior each frame
    void Update()
    {
        if (currentState == State.Dead) return;

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
                if (health <= 0) { Die(); }
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

        if (foodLevel <= maxFoodLevel * 0.4f && currentState == State.Idle)
        {
            currentState = State.SearchingForFood;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Hungry, searching");
        }

        if (socialMentality != SocialMentalityType.Ignores || strangerMentality != StrangerMentalityType.Ignores)
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

        switch (currentState)
        {
            case State.Idle: Wander(); break;
            case State.SearchingForFood: SearchForFood(); break;
            case State.Panic: HandlePanic(); break;
            case State.Eating: Eat(); break;
            case State.SeekingMate: SeekMate(); break;
            case State.Attacking:
                if (combatTarget == null || combatTarget.GetComponent<CreatureBehavior>()?.currentState == State.Dead)
                {
                    currentState = State.SearchingForFood;
                    break;
                }
                agent.SetDestination(combatTarget.position);
                if (Vector3.Distance(transform.position, combatTarget.position) <= creatureCombat.AttackRange)
                {
                    attackTimer += Time.deltaTime;
                    if (attackTimer >= attackInterval)
                    {
                        creatureCombat.Attack(combatTarget.GetComponent<CreatureBehavior>());
                        attackTimer = 0f;
                    }
                }
                break;
            case State.Fleeing:
                if (combatTarget == null)
                {
                    currentState = State.Idle;
                    break;
                }
                Vector3 directionAway = (transform.position - combatTarget.position).normalized;
                Vector3 fleePosition = transform.position + directionAway * 10f;
                if (NavMesh.SamplePosition(fleePosition, out NavMeshHit hit, 10f, agent.areaMask))
                {
                    agent.SetDestination(hit.position);
                }
                if (Vector3.Distance(transform.position, combatTarget.position) > detectionRadius)
                {
                    currentState = State.Idle;
                }
                break;
        }
    }

    // ---- Death Handling ----
    // Kills the creature, turning it into a food source
    public void Die()
    {
        currentState = State.Dead;
        agent.enabled = false;
        gameObject.tag = "Meat";
        FoodSource foodSource = GetComponent<FoodSource>();
        if (foodSource)
        {
            foodSource.enabled = true;
        }
        Debug.Log($"{name}: Died and turned into meat");
    }

    // ---- Detection ----
    // Updates the list of visible objects within detection radius
    private void UpdateDiscoverablesDetection()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, discoverableLayer);
        visibleDiscoverables.Clear();
        foreach (var hit in hits)
            if (hit.transform != transform)
                visibleDiscoverables.Add(hit.transform);
    }

    // ---- Stats Management ----
    // Updates size, health, and hunger stats based on age and physical attributes
    private void UpdateSizeAndStats()
    {
        transform.localScale = Vector3.one * size * ageSize;
        health = Mathf.Ceil(size * 10f);
        maxFoodLevel = Mathf.Ceil(6f + (size * 4f));
        hungerDecreaseInterval = Mathf.Round(
            30f * (size < 1f ? (1f + (2f / 3f) * (1f - size)) : (1f / (1f + 0.2f * (size - 1f)))) * 100f
        ) / 100f;
    }

    // ---- Navigation ----
    // Checks if a target position is reachable via NavMesh
    private bool IsNavigable(Vector3 targetPosition)
    {
        NavMeshPath path = new NavMeshPath();
        bool pathValid = agent.CalculatePath(targetPosition, path);
        return pathValid && path.status == NavMeshPathStatus.PathComplete;
    }

    // ---- Movement Behaviors ----
    // Makes the creature wander randomly within a radius
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

    // Searches for preferred food or a hunting target
    private void SearchForFood()
    {
        if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;
        if (visibleDiscoverables.Count == 0 || foodPreferences.Count == 0)
        {
            if (combatBehavior == CombatBehavior.Hunter && foodLevel < maxFoodLevel * 0.25f)
            {
                combatTarget = FindHuntingTarget();
                if (combatTarget != null)
                {
                    currentState = State.Attacking;
                    agent.SetDestination(combatTarget.position);
                    return;
                }
            }
            PanicWander();
            return;
        }

        foreach (var preferredType in foodPreferences)
        {
            string tag = GetTagFromFoodType(preferredType);
            if (string.IsNullOrEmpty(tag)) continue;

            Transform closest = null;
            float minDist = float.MaxValue;

            foreach (var obj in visibleDiscoverables)
            {
                if (obj.CompareTag(tag))
                {
                    float dist = Vector3.Distance(transform.position, obj.position);
                    if (dist < minDist && IsNavigable(obj.position))
                    {
                        minDist = dist;
                        closest = obj;
                    }
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

        if (combatBehavior == CombatBehavior.Hunter && foodLevel < maxFoodLevel * 0.25f)
        {
            combatTarget = FindHuntingTarget();
            if (combatTarget != null)
            {
                currentState = State.Attacking;
                agent.SetDestination(combatTarget.position);
                return;
            }
        }

        PanicWander();
    }

    // Finds the nearest valid hunting target of a different type
    private Transform FindHuntingTarget()
    {
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var obj in visibleDiscoverables)
        {
            if (obj.CompareTag("Creature"))
            {
                CreatureBehavior other = obj.GetComponent<CreatureBehavior>();
                if (other != null && other.creatureTypeId != creatureTypeId && other.currentState != State.Dead)
                {
                    float dist = Vector3.Distance(transform.position, obj.position);
                    if (dist < minDist && IsNavigable(obj.position))
                    {
                        minDist = dist;
                        nearest = obj;
                    }
                }
            }
        }
        return nearest;
    }

    // Handles panic behavior based on mentality
    private void HandlePanic()
    {
        if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;
        if (socialMentality == SocialMentalityType.Herd && HasCreaturesOfSameTypeInRange())
        {
            currentState = State.Idle;
            Debug.Log($"{name}: Herd calmed (type {creatureTypeId})");
        }
        else if (socialMentality == SocialMentalityType.Isolation) AvoidCreaturesOfSameType();
        else if (strangerMentality == StrangerMentalityType.Avoids) AvoidStrangerCreatures();
        else PanicWander();
    }

    // Moves the creature towards its mate for reproduction
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

    // Wanders randomly in a larger radius during panic
    private void PanicWander()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * panicWanderRadius;
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, panicWanderRadius, agent.areaMask) && IsNavigable(hit.position))
                agent.SetDestination(hit.position);
        }
    }

    // Avoids creatures of the same type (Isolation mentality)
    private void AvoidCreaturesOfSameType()
    {
        float isolationRadius = detectionRadius / 1.5f;
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

    // Avoids creatures of different types (Stranger Avoids mentality)
    private void AvoidStrangerCreatures()
    {
        float avoidanceRadius = detectionRadius / 1.5f;
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var obj in visibleDiscoverables)
        {
            if (obj.CompareTag("Creature"))
            {
                CreatureBehavior other = obj.GetComponent<CreatureBehavior>();
                if (other != null && other.creatureTypeId != creatureTypeId)
                {
                    float dist = Vector3.Distance(transform.position, obj.position);
                    if (dist < minDist) { minDist = dist; nearest = obj; }
                }
            }
        }
        if (nearest && minDist <= avoidanceRadius)
        {
            Vector3 away = (transform.position - nearest.position).normalized * (avoidanceRadius + 1f);
            if (NavMesh.SamplePosition(transform.position + away, out NavMeshHit hit, panicWanderRadius, agent.areaMask) && IsNavigable(hit.position))
                agent.SetDestination(hit.position);
        }
        else currentState = State.Idle;
    }

    // Checks social and stranger mentality, triggering panic if conditions are met
    private void CheckMentality()
    {
        bool sameTypeNearby = HasCreaturesOfSameTypeInRange();
        bool strangersNearby = HasStrangerCreaturesInRange();

        if (socialMentality == SocialMentalityType.Herd && !sameTypeNearby && currentState != State.Eating && currentState != State.SearchingForFood)
        {
            currentState = State.Panic;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Herd alone, panicking");
        }
        else if (socialMentality == SocialMentalityType.Isolation && sameTypeNearby && currentState != State.Eating && currentState != State.SearchingForFood)
        {
            currentState = State.Panic;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Isolation crowded, panicking");
        }

        if (strangerMentality == StrangerMentalityType.Avoids && strangersNearby && currentState != State.Eating && currentState != State.SearchingForFood)
        {
            currentState = State.Panic;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Avoiding strangers, panicking");
        }
    }

    // Checks if creatures of the same type are within detection range
    private bool HasCreaturesOfSameTypeInRange()
    {
        foreach (var obj in visibleDiscoverables)
            if (obj.CompareTag("Creature") && obj.GetComponent<CreatureBehavior>()?.creatureTypeId == creatureTypeId)
                return true;
        return false;
    }

    // Checks if creatures of a different type are within detection range
    private bool HasStrangerCreaturesInRange()
    {
        foreach (var obj in visibleDiscoverables)
            if (obj.CompareTag("Creature") && obj.GetComponent<CreatureBehavior>()?.creatureTypeId != creatureTypeId)
                return true;
        return false;
    }

    // ---- Reproduction ----
    // Checks if the creature should seek a mate
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

    // Finds the nearest creature of the same type for reproduction
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

    // Attempts to reproduce with the target mate
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

    // Checks if the creature can reproduce based on food, time, and age
    private bool CanReproduce() => foodLevel >= maxFoodLevel * 0.8f && (Time.time - lastReproductionTime) >= reproductionCooldown && age >= adultAge;

    // Spawns a child with inherited traits from both parents
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

    // Spawns a child asexually with slight mutations
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

    // ---- Eating ----
    // Consumes food from the target food source
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

    // ---- Visuals ----
    // Updates the creature’s color based on food level
    private void UpdateColor()
    {
        if (creatureRenderer)
        {
            float t = foodLevel / maxFoodLevel;
            creatureRenderer.material.color = Color.Lerp(emptyColor, fullColor, t);
        }
    }

    // Draws detection radius gizmos in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        if (socialMentality == SocialMentalityType.Isolation || strangerMentality == StrangerMentalityType.Avoids)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius / 1.5f);
        }
    }

    // ---- Utility ----
    // Converts food type enum to corresponding tag
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