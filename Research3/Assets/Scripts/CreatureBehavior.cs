using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CreatureBehavior : MonoBehaviour
{
    // ---- Core Components ----
    private NavMeshAgent agent;
    public CreatureCombat creatureCombat { get; private set; }
    private TextMeshPro textDisplay;

    // ---- Inspector Groups ----
    public enum State { Idle, SearchingForFood, Panic, Eating, SeekingMate, Attacking, Fleeing, Dead }
    [Header("Creature States")]
    [SerializeField] public State currentState = State.Idle;

    public enum SocialMentalityType { Herd, Ignores, Isolation }
    [Header("Social Mentality")]
    [SerializeField] private SocialMentalityType socialMentality = SocialMentalityType.Ignores;

    public enum StrangerMentalityType { Ignores, Avoids }
    [Header("Stranger Mentality")]
    [SerializeField] private StrangerMentalityType strangerMentality = StrangerMentalityType.Ignores;

    public enum CombatBehavior { Friendly, Neutral, Hunter }
    [Header("Combat Behavior")]
    [SerializeField] public CombatBehavior combatBehavior = CombatBehavior.Neutral;

    [Header("Creature Type")]
    [SerializeField] private int creatureTypeId = -1;

    [Header("Physical Attributes")]
    [SerializeField] private float size = 1f;
    [SerializeField] private float walkingSpeed = 4f;
    [SerializeField] private bool canClimb = false;
    private float newWalkingSpeed => Mathf.Round(walkingSpeed * (size < 1f ? (2f - size) : Mathf.Pow(size, -0.252f)) * 100f) / 100f;
    private float sprintSpeed => Mathf.Round(newWalkingSpeed * 2.0f * 100f) / 100f;

    [Header("Health and Hunger")]
    [SerializeField] public float health;
    [SerializeField] private int foodLevel;
    [SerializeField] private float maxFoodLevel;
    private float hungerTimer = 0f;
    [SerializeField] private float hungerDecreaseInterval;
    private float damageTimer = 0f;
    private float healTimer = 0f;
    private bool isHealing = false;

    [Header("Age and Growth")]
    [SerializeField] private int age = 0;
    private float ageTimer = 0f;
    private const float ageIncreaseInterval = 30f;
    private float ageSize => Mathf.Round(Mathf.Lerp(0.2f, 1f, Mathf.Min(age / adultAge, 1f)) * 100f) / 100f;
    private const float adultAge = 10f;

    [Header("Reproduction")]
    [SerializeField] private Gender gender;
    [SerializeField] private bool isPregnant = false;
    [SerializeField] private int totalFoodLostSincePregnant = 0;
    [SerializeField] private float lastImpregnationTime = -1000f;
    private CreatureBehavior pregnantWith;
    private float reproductionTimer = 0f;
    private const float reproductionCheckInterval = 20f;
    private const float reproductionCooldown = 60f;
    private Transform reproductionTarget;
    [SerializeField] private const float reproductionDistance = 8f;
    [SerializeField] private float mutationRate = 0.1f;
    [SerializeField] private List<CreatureBehavior> children = new List<CreatureBehavior>();
    [SerializeField] private float attachmentLevel;

    [Header("Asexual Reproduction")]
    [SerializeField] private int originalSpawnAmount = 3; // New field for initial spawn count

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
    private const float navigationTimeout = 12f;

    [Header("Eating Mechanics")]
    private float eatTimer = 0f;

    [Header("Mentality Timing")]
    private float mentalityCheckTimer = 0f;
    private const float mentalityCheckInterval = 30f;

    // ---- Collision Avoidance ----
    private float avoidanceTimer = 0f;
    private const float avoidanceCheckInterval = 0.5f;
    private const float avoidanceDistance = 2f;

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
    [SerializeField] public Transform combatTarget;
    private float attackTimer = 0f;
    private const float attackInterval = 1f;

    // ---- Reproduction Cost ----
    private int ReproductionCost => Mathf.RoundToInt(size * 8) + Mathf.FloorToInt(walkingSpeed / 3);

    // ---- Gender Enum ----
    public enum Gender { Male, Female }

    // ---- Parents ----
    [SerializeField] private CreatureBehavior mother;
    [SerializeField] private CreatureBehavior father;

    // ---- Herd Mentality Variables ----
    [Header("Herd Mentality")]
    [SerializeField] private int preferredGroupSize = 2; // Number of same-type creatures it prefers to be near (excluding itself)
    private float panicDelayTimer = 0f; // Tracks time elapsed during delay
    private float panicDelayTime = -1f; // Delay before reacting, -1 means not active

    // ---- Initialization ----
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent) { Debug.LogError($"{name}: No NavMeshAgent! Disabling."); enabled = false; return; }
        creatureCombat = GetComponent<CreatureCombat>();
        if (!creatureCombat) { Debug.LogError($"{name}: No CreatureCombat!"); }

        SetupTextDisplay();

        size = Mathf.Round(size * 100f) / 100f;
        walkingSpeed = Mathf.Round(walkingSpeed * 100f) / 100f;

        int areaMask = WalkableArea | JumpArea;
        if (canClimb) areaMask |= ClimbArea;
        agent.areaMask = areaMask;

        agent.radius = size * 0.5f;
        agent.height = size;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        UpdateSizeAndStats();
        foodLevel = (int)maxFoodLevel;
        agent.speed = newWalkingSpeed;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, agent.areaMask))
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
                Debug.Log($"{name}: Repositioned onto NavMesh at startup");
            }
            else
            {
                Debug.LogError($"{name}: Failed to place on NavMesh at startup!");
                enabled = false;
                return;
            }
        }

        FoodSource foodSource = GetComponent<FoodSource>();
        if (foodSource)
        {
            foodSource.maxFood = (int)maxFoodLevel;
            foodSource.currentFood = (int)maxFoodLevel;
            foodSource.enabled = false;
        }

        wanderInterval = Random.Range(2.5f, 10f);
        children = new List<CreatureBehavior>();

        if (creatureTypeId == -1)
        {
            creatureTypeId = nextAvailableId++;
            age = 10;
            UpdateSizeAndStats();
            gender = (Random.value < 0.5f) ? Gender.Male : Gender.Female;
            SpawnInitialCreatures(originalSpawnAmount);
            Debug.Log($"{name}: Assigned type {creatureTypeId}, spawned {originalSpawnAmount} initial creatures");
        }
        else
        {
            gender = (Random.value < 0.5f) ? Gender.Male : Gender.Female;
        }

        isPregnant = false;
        totalFoodLostSincePregnant = 0;
        lastImpregnationTime = -1000f;
        pregnantWith = null;

        UpdateTextDisplay();
        Debug.Log($"{name}: Initialized - Type: {creatureTypeId}, Gender: {gender}, Size: {size}, Age: {age}");
    }

    // ---- Main Update Loop ----
    void Update()
    {
        if (currentState == State.Dead || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float deltaTime = Time.deltaTime;
        UpdateDiscoverablesDetection();

        avoidanceTimer += deltaTime;
        if (avoidanceTimer >= avoidanceCheckInterval)
        {
            AvoidCollisions();
            avoidanceTimer = 0f;
        }

        ageTimer += deltaTime;
        if (ageTimer >= ageIncreaseInterval)
        {
            age++;
            ageTimer = 0f;
            UpdateSizeAndStats();
            if (age == adultAge) Debug.Log($"{name}: Now adult at age {age}");
        }

        float hungerMultiplier = 1f;
        if (isPregnant) hungerMultiplier *= 2f;
        if (isHealing) hungerMultiplier *= 2f;
        if (currentState == State.SearchingForFood || currentState == State.Attacking || currentState == State.Fleeing)
            hungerMultiplier *= 1.5f;

        hungerTimer += deltaTime * hungerMultiplier;
        if (hungerTimer >= hungerDecreaseInterval)
        {
            foodLevel = Mathf.Max(foodLevel - 1, 0);
            if (isPregnant)
            {
                totalFoodLostSincePregnant++;
                int reproductionCostCounter = totalFoodLostSincePregnant / 2;
                if (reproductionCostCounter >= ReproductionCost)
                {
                    BirthBaby();
                    isPregnant = false;
                    totalFoodLostSincePregnant = 0;
                }
            }
            hungerTimer = 0f;
            UpdateTextDisplay();
        }

        if (foodLevel <= 0)
        {
            damageTimer += deltaTime;
            if (damageTimer >= 5f)
            {
                health = Mathf.Ceil(health - 1);
                damageTimer = 0f;
                UpdateTextDisplay();
                if (health <= 0) { StartCoroutine(DieWithRotation()); }
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
                UpdateTextDisplay();
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

        if (age >= adultAge && currentState != State.Eating && currentState != State.SeekingMate)
        {
            reproductionTimer += deltaTime;
            if (reproductionTimer >= reproductionCheckInterval)
            {
                if (CanSeekReproduction() && Random.value < 1f / 3f)
                {
                    reproductionTarget = FindMateForReproduction();
                    if (reproductionTarget != null)
                    {
                        currentState = State.SeekingMate;
                        agent.SetDestination(reproductionTarget.position);
                        Debug.Log($"{name}: Seeking mate {reproductionTarget.name}");
                    }
                }
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

        if (socialMentality == SocialMentalityType.Herd && currentState != State.Eating && currentState != State.SearchingForFood && attachmentLevel > 0)
        {
            int count = CountSameTypeCreaturesInRange();
            if (count < preferredGroupSize)
            {
                if (panicDelayTime == -1f) // Delay not yet started
                {
                    panicDelayTime = CalculatePanicDelay();
                    panicDelayTimer = 0f;
                    Debug.Log($"{name}: Herd too small ({count}/{preferredGroupSize}), starting delay of {panicDelayTime}s");
                }
                else // Delay in progress
                {
                    panicDelayTimer += deltaTime;
                    if (panicDelayTimer >= panicDelayTime)
                    {
                        if (count < preferredGroupSize) // Still too few after delay
                        {
                            currentState = State.Panic;
                            agent.speed = sprintSpeed;
                            Debug.Log($"{name}: Herd still too small after delay, panicking");
                            FindHerdSpot();
                        }
                        panicDelayTime = -1f; // Reset delay
                    }
                }
            }
            else
            {
                panicDelayTime = -1f; // Reset if group size is sufficient
            }
        }

        switch (currentState)
        {
            case State.Idle:
                Wander();
                break;
            case State.SearchingForFood:
                SearchForFood();
                break;
            case State.Panic:
                HandlePanic();
                break;
            case State.Eating:
                Eat();
                break;
            case State.SeekingMate:
                SeekMate();
                break;
            case State.Attacking:
                if (combatTarget == null || combatTarget.GetComponent<CreatureBehavior>()?.currentState == State.Dead)
                {
                    currentState = State.SearchingForFood;
                    break;
                }
                agent.SetDestination(combatTarget.position);
                if (Vector3.Distance(transform.position, combatTarget.position) <= creatureCombat.AttackRange)
                {
                    attackTimer += deltaTime;
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

    // ---- Stats Management ----
    private void UpdateSizeAndStats()
    {
        transform.localScale = Vector3.one * size * ageSize;
        health = Mathf.Ceil(size * 10f);
        maxFoodLevel = Mathf.Ceil(6f + (size * 6f));
        hungerDecreaseInterval = Mathf.Round(
            30f * (size < 1f ? (1f + (2f / 3f) * (1f - size)) : (1f / (1f + 0.2f * (size - 1f)))) * 100f
        ) / 100f;

        if (age < adultAge)
        {
            attachmentLevel = 1f - (age / (float)adultAge); // 1.0 at birth, 0.0 at adult age
        }
        else
        {
            attachmentLevel = 0f; // Adults don’t panic about herd size
        }

        if (agent.isActiveAndEnabled)
        {
            agent.radius = size * 0.5f;
            agent.height = size;
        }
    }

    // ---- Movement Behaviors ----
    private void Wander()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (agent.speed != newWalkingSpeed) agent.speed = newWalkingSpeed;
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            if (age < adultAge && attachmentLevel > 0)
            {
                CreatureBehavior closestParent = GetClosestParent();
                if (closestParent != null)
                {
                    float followRadius = detectionRadius * (0.1f + 0.4f * (1 - attachmentLevel));
                    Vector3 parentPos = closestParent.transform.position;
                    Vector3 destination = parentPos + Random.insideUnitSphere * followRadius;
                    if (NavMesh.SamplePosition(destination, out NavMeshHit hit, followRadius, agent.areaMask) && IsNavigable(hit.position))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
                else
                {
                    WanderIndependently();
                }
            }
            else
            {
                WanderIndependently();
            }

            wanderTimer = 0f;
            wanderInterval = Random.Range(2.5f, 10f);
        }
    }

    private void WanderIndependently()
    {
        Vector3 destination = transform.position + Random.insideUnitSphere * wanderRadius;
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, wanderRadius, agent.areaMask) && IsNavigable(hit.position))
        {
            agent.SetDestination(hit.position);
        }
    }

    private CreatureBehavior GetClosestParent()
    {
        CreatureBehavior closestParent = null;
        float minDist = float.MaxValue;

        if (mother != null && mother.currentState != State.Dead)
        {
            float distToMother = Vector3.Distance(transform.position, mother.transform.position);
            if (distToMother < minDist)
            {
                minDist = distToMother;
                closestParent = mother;
            }
        }

        if (father != null && father.currentState != State.Dead)
        {
            float distToFather = Vector3.Distance(transform.position, father.transform.position);
            if (distToFather < minDist)
            {
                minDist = distToFather;
                closestParent = father;
            }
        }

        return closestParent;
    }

    // ---- Herd Mentality Methods ----
    private int CountSameTypeCreaturesInRange()
    {
        int count = 0;
        foreach (var obj in visibleDiscoverables)
        {
            if (obj.CompareTag("Creature"))
            {
                CreatureBehavior other = obj.GetComponent<CreatureBehavior>();
                if (other != null && other != this && other.creatureTypeId == creatureTypeId)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private float CalculatePanicDelay()
    {
        float minDelay, maxDelay;
        if (attachmentLevel >= 0.75f) // Newborn to young
        {
            minDelay = 1f;
            maxDelay = 4f;
        }
        else if (attachmentLevel >= 0.25f) // Mid-age
        {
            minDelay = 4f;
            maxDelay = 12f;
        }
        else // Near adult
        {
            minDelay = 12f;
            maxDelay = 24f;
        }
        return Random.Range(minDelay, maxDelay);
    }

    private void FindHerdSpot()
    {
        float extendedRadius = detectionRadius * 1.5f;
        Transform nearest = null;
        float minDist = float.MaxValue;

        Collider[] hits = Physics.OverlapSphere(transform.position, extendedRadius, discoverableLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Creature"))
            {
                CreatureBehavior other = hit.GetComponent<CreatureBehavior>();
                if (other != null && other != this && other.creatureTypeId == creatureTypeId)
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = hit.transform;
                    }
                }
            }
        }

        if (nearest != null)
        {
            Vector3 direction = (transform.position - nearest.position).normalized;
            Vector3 targetPos = nearest.position + direction * (detectionRadius * 0.5f);
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, detectionRadius, agent.areaMask) && IsNavigable(hit.position))
            {
                agent.SetDestination(hit.position);
                Debug.Log($"{name}: Moving toward {nearest.name} to join herd");
            }
            else
            {
                agent.SetDestination(nearest.position);
            }
        }
        else
        {
            PanicWander();
            Debug.Log($"{name}: No herd members in 1.5x range, panic wandering");
        }
    }

    // ---- Panic Handling ----
    private void HandlePanic()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (agent.speed != sprintSpeed) agent.speed = sprintSpeed;

        if (socialMentality == SocialMentalityType.Herd)
        {
            int count = CountSameTypeCreaturesInRange();
            if (count >= preferredGroupSize)
            {
                currentState = State.Idle;
                Debug.Log($"{name}: Herd size sufficient ({count}/{preferredGroupSize}), calming down");
            }
            else
            {
                FindHerdSpot();
            }
        }
        else if (socialMentality == SocialMentalityType.Isolation)
        {
            AvoidCreaturesOfSameType();
        }
        else if (strangerMentality == StrangerMentalityType.Avoids)
        {
            AvoidStrangerCreatures();
        }
        else
        {
            PanicWander();
        }
    }

    private void PanicWander()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance))
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * panicWanderRadius;
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, panicWanderRadius, agent.areaMask) && IsNavigable(hit.position))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    // ---- Mentality Checks ----
    private void CheckMentality()
    {
        bool sameTypeNearby = HasCreaturesOfSameTypeInRange();
        bool strangersNearby = HasStrangerCreaturesInRange();

        if (socialMentality == SocialMentalityType.Isolation && sameTypeNearby && currentState != State.Eating && currentState != State.SearchingForFood)
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

    private bool HasCreaturesOfSameTypeInRange()
    {
        foreach (var obj in visibleDiscoverables)
            if (obj.CompareTag("Creature") && obj.GetComponent<CreatureBehavior>()?.creatureTypeId == creatureTypeId)
                return true;
        return false;
    }

    private bool HasStrangerCreaturesInRange()
    {
        foreach (var obj in visibleDiscoverables)
            if (obj.CompareTag("Creature") && obj.GetComponent<CreatureBehavior>()?.creatureTypeId != creatureTypeId)
                return true;
        return false;
    }

    // ---- Remaining Methods ----
    private void AvoidCollisions()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, avoidanceDistance, discoverableLayer);
        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            if (hit.CompareTag("Creature") && hit.GetComponent<CreatureBehavior>()?.currentState != State.Dead)
            {
                Vector3 directionAway = (transform.position - hit.transform.position).normalized;
                Vector3 newPosition = transform.position + directionAway * (avoidanceDistance * 0.5f);
                if (NavMesh.SamplePosition(newPosition, out NavMeshHit navHit, avoidanceDistance, agent.areaMask) && IsNavigable(navHit.position))
                {
                    agent.SetDestination(navHit.position);
                    break;
                }
            }
        }
    }

    private bool CanSeekReproduction()
    {
        if (gender == Gender.Female)
        {
            return !isPregnant && foodLevel >= maxFoodLevel * 0.7f;
        }
        else
        {
            return (Time.time - lastImpregnationTime) >= reproductionCooldown && foodLevel >= maxFoodLevel * 0.7f;
        }
    }

    private Transform FindMateForReproduction()
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
                if (other != null && other.creatureTypeId == creatureTypeId && other.gender != gender)
                {
                    if (gender == Gender.Female && other.gender == Gender.Male)
                    {
                        if ((Time.time - other.lastImpregnationTime) >= reproductionCooldown && other.foodLevel >= other.maxFoodLevel * 0.8f)
                        {
                            float dist = Vector3.Distance(transform.position, hit.transform.position);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                nearest = hit.transform;
                            }
                        }
                    }
                    else if (gender == Gender.Male && other.gender == Gender.Female)
                    {
                        if (!other.isPregnant && other.foodLevel >= other.maxFoodLevel * 0.7f)
                        {
                            float dist = Vector3.Distance(transform.position, hit.transform.position);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                nearest = hit.transform;
                            }
                        }
                    }
                }
            }
        }
        return nearest;
    }

    private void SeekMate()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

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

    private void AttemptReproduction()
    {
        if (reproductionTarget == null) return;
        CreatureBehavior partner = reproductionTarget.GetComponent<CreatureBehavior>();
        if (partner == null || partner.creatureTypeId != creatureTypeId || partner.gender == gender) return;

        if (gender == Gender.Female && partner.gender == Gender.Male)
        {
            if (!isPregnant && foodLevel >= maxFoodLevel * 0.7f && (Time.time - partner.lastImpregnationTime) >= reproductionCooldown && partner.foodLevel >= partner.maxFoodLevel * 0.8f)
            {
                isPregnant = true;
                totalFoodLostSincePregnant = 0;
                partner.lastImpregnationTime = Time.time;
                pregnantWith = partner;
                Debug.Log($"{name} (Female) is now pregnant with {partner.name}'s child");
            }
        }
        else if (gender == Gender.Male && partner.gender == Gender.Female)
        {
            if (!partner.isPregnant && partner.foodLevel >= partner.maxFoodLevel * 0.7f && (Time.time - lastImpregnationTime) >= reproductionCooldown && foodLevel >= maxFoodLevel * 0.8f)
            {
                partner.isPregnant = true;
                partner.totalFoodLostSincePregnant = 0;
                lastImpregnationTime = Time.time;
                partner.pregnantWith = this;
                Debug.Log($"{partner.name} (Female) is now pregnant with {name}'s child");
            }
        }
        reproductionTarget = null;
        currentState = State.Idle;
    }

    private void SpawnChild(CreatureBehavior mother, CreatureBehavior father)
    {
        GameObject child = Instantiate(gameObject, mother.transform.position + Vector3.right * 2f, Quaternion.identity);
        CreatureBehavior childBehavior = child.GetComponent<CreatureBehavior>();

        float avgSize = (mother.size + father.size) / 2f;
        float avgSpeed = (mother.walkingSpeed + father.walkingSpeed) / 2f;
        childBehavior.size = Mathf.Round((avgSize + Random.Range(-mutationRate, mutationRate) * avgSize) * 100f) / 100f;
        childBehavior.walkingSpeed = Mathf.Round((avgSpeed + Random.Range(-mutationRate, mutationRate) * avgSpeed) * 100f) / 100f;
        childBehavior.canClimb = mother.canClimb || father.canClimb;

        childBehavior.age = 0;
        childBehavior.foodLevel = (int)childBehavior.maxFoodLevel;
        childBehavior.creatureTypeId = mother.creatureTypeId;
        childBehavior.gender = (Random.value < 0.5f) ? Gender.Male : Gender.Female;
        childBehavior.isPregnant = false;
        childBehavior.totalFoodLostSincePregnant = 0;
        childBehavior.lastImpregnationTime = -1000f;
        childBehavior.pregnantWith = null;
        childBehavior.mother = mother;
        childBehavior.father = father;

        if (NavMesh.SamplePosition(child.transform.position, out NavMeshHit hit, 5f, childBehavior.agent.areaMask))
        {
            child.transform.position = hit.position;
            childBehavior.agent.Warp(hit.position);
        }

        mother.children.Add(childBehavior);
        father.children.Add(childBehavior);

        Debug.Log($"Child born: {childBehavior.name}, Gender: {childBehavior.gender}, Size: {childBehavior.size}, Speed: {childBehavior.walkingSpeed}, CanClimb: {childBehavior.canClimb}");
    }

    private void BirthBaby()
    {
        if (pregnantWith == null) return;
        SpawnChild(this, pregnantWith);
        isPregnant = false;
        totalFoodLostSincePregnant = 0;
        pregnantWith = null;
    }

    public IEnumerator DieWithRotation()
    {
        currentState = State.Dead;
        agent.enabled = false;

        float duration = 0.5f;
        float elapsed = 0f;
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.Euler(180f, transform.eulerAngles.y, 0f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            yield return null;
        }
        transform.rotation = endRotation;

        gameObject.tag = "Meat";
        FoodSource foodSource = GetComponent<FoodSource>();
        if (foodSource)
        {
            foodSource.enabled = true;
            foodSource.isNotReplenishable = true;
            foodSource.maxFood = (int)maxFoodLevel;
            foodSource.CurrentFood = (int)maxFoodLevel;
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers) renderer.enabled = true;
        transform.localScale = Vector3.one * size * ageSize;
        foreach (Transform child in transform) child.gameObject.SetActive(true);
        UpdateTextDisplay();
    }

    private void UpdateDiscoverablesDetection()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, discoverableLayer);
        visibleDiscoverables.Clear();
        foreach (var hit in hits)
            if (hit.transform != transform)
                visibleDiscoverables.Add(hit.transform);
    }

    private bool IsNavigable(Vector3 targetPosition)
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return false;

        NavMeshPath path = new NavMeshPath();
        bool pathValid = agent.CalculatePath(targetPosition, path);
        return pathValid && path.status == NavMeshPathStatus.PathComplete;
    }

    private void SearchForFood()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

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
                if (targetFoodSource != null && targetFoodSource.HasFood)
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

    private void AvoidCreaturesOfSameType()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

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

    private void AvoidStrangerCreatures()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

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

    private void Eat()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (agent.speed != newWalkingSpeed) agent.speed = newWalkingSpeed;
        eatTimer += Time.deltaTime;
        if (eatTimer >= 1f)
        {
            if (targetFoodSource != null && targetFoodSource.HasFood && targetFoodSource.CurrentFood > 0 && foodLevel < maxFoodLevel)
            {
                targetFoodSource.CurrentFood--;
                foodLevel = Mathf.Min(foodLevel + (int)targetFoodSource.FoodSatiety, (int)maxFoodLevel);
                UpdateTextDisplay();
            }
            else
            {
                currentState = foodLevel >= maxFoodLevel ? State.Idle : State.SearchingForFood;
                agent.isStopped = false;
                targetFoodSource = null;
                Debug.Log($"{name}: {(foodLevel >= maxFoodLevel ? "Full" : "Food gone")}");
            }
            eatTimer = 0f;
        }
    }

    private void SetupTextDisplay()
    {
        GameObject textObj = new GameObject($"{name}_StatusText");
        textObj.transform.SetParent(transform);
        textDisplay = textObj.AddComponent<TextMeshPro>();

        textDisplay.transform.localPosition = new Vector3(0, size * 2.5f, 0);
        textDisplay.transform.localRotation = Quaternion.identity;

        textDisplay.fontSize = 3f;
        textDisplay.alignment = TextAlignmentOptions.Center;
        textDisplay.textWrappingMode = TextWrappingModes.NoWrap;
        textDisplay.color = Color.white;
        textDisplay.outlineWidth = 0.1f;
        textDisplay.outlineColor = Color.black;
    }

    private void UpdateTextDisplay()
    {
        if (textDisplay)
        {
            int maxHealth = Mathf.CeilToInt(size * 10f);
            string text = $"ID {creatureTypeId}\n{foodLevel}/{maxFoodLevel} FOOD\n{(currentState == State.Dead ? 0 : Mathf.CeilToInt(health))}/{maxHealth} HP";
            if (gender == Gender.Female && isPregnant)
            {
                int reproductionCostCounter = totalFoodLostSincePregnant / 2;
                text += $"\nPregnant: {reproductionCostCounter}/{ReproductionCost}";
            }
            text += $"\n{gender}";
            textDisplay.text = text;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        if (socialMentality == SocialMentalityType.Isolation || strangerMentality == StrangerMentalityType.Avoids)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius / 1.5f);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidanceDistance);
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

    private void SpawnInitialCreatures(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            int startingAge = Random.Range(4, 11); // Random age between 4 and 10
            Vector3 spawnOffset = Vector3.right * 2f * (i + 1); // Space out each spawn

            GameObject child = Instantiate(gameObject, transform.position + spawnOffset, Quaternion.identity);
            CreatureBehavior childBehavior = child.GetComponent<CreatureBehavior>();

            childBehavior.creatureTypeId = creatureTypeId;
            childBehavior.age = startingAge;
            childBehavior.size = size;
            childBehavior.walkingSpeed = walkingSpeed;
            childBehavior.canClimb = canClimb;
            childBehavior.gender = (Random.value < 0.5f) ? Gender.Male : Gender.Female;
            childBehavior.isPregnant = false;
            childBehavior.totalFoodLostSincePregnant = 0;
            childBehavior.lastImpregnationTime = -1000f;
            childBehavior.pregnantWith = null;
            childBehavior.mother = this;

            if (NavMesh.SamplePosition(child.transform.position, out NavMeshHit hit, 5f, childBehavior.agent.areaMask))
            {
                child.transform.position = hit.position;
                childBehavior.agent.Warp(hit.position);
            }

            Debug.Log($"Initial creature spawned: {childBehavior.name}, Age: {startingAge}, Gender: {childBehavior.gender}");
        }
    }
}