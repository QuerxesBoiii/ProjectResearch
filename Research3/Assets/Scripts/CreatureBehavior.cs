using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System;

public class CreatureBehavior : MonoBehaviour
{
    // ---- Core Components ----
    private NavMeshAgent agent;
    public CreatureCombat creatureCombat { get; private set; }
    private TextMeshPro textDisplay;

    // ---- Inspector Groups ----
    public enum State { Idle, SearchingForFood, Eating, SeekingMate, Attacking, Fleeing, Dead }
    [Header("Creature States")]
    [SerializeField] public State currentState = State.Idle;

    public enum CombatBehavior { Friendly, Neutral, Hunter }
    [Header("Combat Behavior")]
    [SerializeField] public CombatBehavior combatBehavior = CombatBehavior.Neutral;

    public enum Gender { Male, Female }
    [Header("Gender")]
    [SerializeField] public Gender gender;

    [Header("Creature Type")]
    [SerializeField] private int creatureTypeId = -1;

    [Header("Traits")]
    [SerializeField] public List<int> traitIds = new List<int>();

    [Header("Physical Attributes")]
    [SerializeField] private float size = 1f;
    [SerializeField] private float walkingSpeed = 4f;
    [SerializeField] private bool canClimb = false;
    private float newWalkingSpeed => Mathf.Round(walkingSpeed * (size < 1f ? (2f - size) : Mathf.Pow(size, -0.252f)) * 100f) / 100f;
    private float sprintSpeed => Mathf.Round(newWalkingSpeed * 2.0f * 100f) / 100f;

    [Header("Health and Hunger")]
    [SerializeField] private float health = 10f;
    [SerializeField] private float maxFoodLevel = 10f;
    [SerializeField] private float foodLevel;
    [SerializeField] private float hungerDecreaseInterval = 30f;
    private float hungerTimer = 0f;
    private float healTimer = 0f;
    private bool isHealing = false;

    [Header("Age and Lifespan")]
    [SerializeField] private float maxAge = 100f;
    [SerializeField] public float currentAge = 0f;
    public float Age { get => currentAge; set => currentAge = value; }

    [Header("Reproduction")]
    [SerializeField] private bool isPregnant = false;
    [SerializeField] private int totalFoodLostSincePregnant = 0;
    [SerializeField] private float lastImpregnationTime = -1000f;
    private CreatureBehavior pregnantWith;
    [SerializeField] private float reproductionCheckInterval = 20f;
    [SerializeField] private float reproductionCooldown = 60f;
    private Transform reproductionTarget;
    [SerializeField] private float reproductionDistance = 8f;
    [SerializeField] private float mutationRate = 0.1f;
    [SerializeField] private List<CreatureBehavior> children = new List<CreatureBehavior>();
    private float reproductionTimer = 0f;

    [Header("Detection and Interaction")]
    [SerializeField] private float baseDetectionRadius = 50f;
    [SerializeField] private float detectionRadius => baseDetectionRadius + (Mathf.Floor(size - 1f) * (baseDetectionRadius / 5f));
    [SerializeField] private LayerMask discoverableLayer;
    [SerializeField] private List<Transform> visibleDiscoverables = new();
    [SerializeField] private float eatingDistance = 1f;

    [Header("Movement and Wandering")]
    [SerializeField] private float wanderRadius = 20f;
    private float wanderTimer = 0f;
    private float wanderInterval = 5f;
    private float navigationTimer = 0f;
    private const float navigationTimeout = 12f;

    [Header("Eating Mechanics")]
    private float eatTimer = 0f;

    // ---- Combat ----
    [SerializeField] public Transform combatTarget;
    private float attackTimer = 0f;
    private const float attackInterval = 1f;

    // ---- Reproduction Cost ----
    private int ReproductionCost => Mathf.RoundToInt(size * 8) + Mathf.FloorToInt(walkingSpeed / 3);

    // ---- Food Preferences ----
    public enum FoodType { Apple, Berry, Meat }
    [Header("Food Preferences")]
    [SerializeField] public List<FoodType> foodPreferences = new List<FoodType> { FoodType.Apple };

    // ---- NavMesh Area Masks ----
    private const int WalkableArea = 1 << 0;
    private const int JumpArea = 1 << 2;
    private const int ClimbArea = 1 << 3;

    // ---- Collision Avoidance ----
    private float avoidanceTimer = 0f;
    private const float avoidanceCheckInterval = 0.5f;
    private const float avoidanceDistance = 2f;

    // ---- Properties for Trait Access and External Scripts ----
    public float Size { get => size; set { size = value; UpdateSizeAndStats(); } }
    public float Health { get => health; set { health = value; UpdateTextDisplay(); } }
    public float WalkingSpeed { get => walkingSpeed; set { walkingSpeed = value; UpdateSizeAndStats(); } }
    public float ReproductionCheckInterval { get => reproductionCheckInterval; set => reproductionCheckInterval = value; }
    public float MaxAge { get => maxAge; set => maxAge = value; }
    public float HungerDecreaseInterval { get => hungerDecreaseInterval; set => hungerDecreaseInterval = value; }
    public int CreatureTypeId => creatureTypeId;
    public float FoodLevel => foodLevel;
    public float MaxFoodLevel => maxFoodLevel;
    public bool IsPregnant => isPregnant;
    public int TotalFoodLostSincePregnant => totalFoodLostSincePregnant;
    public int ReproductionCostProperty => ReproductionCost;

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

        foodLevel = maxFoodLevel;
        gender = (UnityEngine.Random.value < 0.5f) ? Gender.Male : Gender.Female; // Fixed: Line 145
        children = new List<CreatureBehavior>();

        UpdateSizeAndStats();

        // Apply all Trait components attached to this GameObject
        foreach (var trait in GetComponents<Trait>())
        {
            if (trait != null)
            {
                trait.ApplyTrait(this);
                if (!traitIds.Contains(trait.Id))
                    traitIds.Add(trait.Id);
            }
        }

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

        wanderInterval = UnityEngine.Random.Range(2.5f, 10f); // Fixed: Line 185
        agent.speed = newWalkingSpeed;

        UpdateTextDisplay();
        Debug.Log($"{name}: Initialized - Gender: {gender}, Size: {size}, Age: {currentAge}");
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

        currentAge += deltaTime;
        if (currentAge >= maxAge)
        {
            StartCoroutine(DieWithRotation());
            return;
        }

        float hungerMultiplier = isPregnant ? 2f : 1f;
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
            health = Mathf.Ceil(health - deltaTime);
            if (health <= 0) StartCoroutine(DieWithRotation());
        }

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
        else if (health >= Mathf.Ceil(size * 10f))
        {
            isHealing = false;
            healTimer = 0f;
        }

        if (foodLevel <= maxFoodLevel * 0.4f && currentState == State.Idle)
        {
            currentState = State.SearchingForFood;
            agent.speed = sprintSpeed;
            Debug.Log($"{name}: Hungry, searching");
        }

        if (currentAge >= maxAge * 0.1f && currentState != State.Eating && currentState != State.SeekingMate)
        {
            reproductionTimer += deltaTime;
            if (reproductionTimer >= reproductionCheckInterval)
            {
                if (CanSeekReproduction() && UnityEngine.Random.value < 1f / 3f) // Fixed: Line 273
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
        else
        {
            navigationTimer = 0f;
        }

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

        UpdateTextDisplay();
    }

    // ---- Stats Management ----
    private void UpdateSizeAndStats()
    {
        transform.localScale = Vector3.one * size;
        health = Mathf.Max(health, Mathf.Ceil(size * 10f));
        maxFoodLevel = Mathf.Ceil(6f + (size * 6f));
        hungerDecreaseInterval = Mathf.Round(
            30f * (size < 1f ? (1f + (2f / 3f) * (1f - size)) : (1f / (1f + 0.2f * (size - 1f)))) * 100f
        ) / 100f;

        if (agent.isActiveAndEnabled)
        {
            agent.radius = size * 0.5f;
            agent.height = size;
            agent.speed = newWalkingSpeed;
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
            Vector3 destination = transform.position + UnityEngine.Random.insideUnitSphere * wanderRadius; // Fixed: Line 382
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, wanderRadius, agent.areaMask) && IsNavigable(hit.position))
            {
                agent.SetDestination(hit.position);
            }
            wanderTimer = 0f;
            wanderInterval = UnityEngine.Random.Range(2.5f, 10f); // Fixed: Line 388
        }
    }

    // ---- Food and Eating ----
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
                FoodSource targetFoodSource = closest.GetComponent<FoodSource>();
                if (targetFoodSource != null && targetFoodSource.HasFood)
                {
                    agent.SetDestination(closest.position);
                    if (minDist <= eatingDistance)
                    {
                        currentState = State.Eating;
                        agent.isStopped = true;
                        eatTimer = 0f;
                        this.targetFoodSource = targetFoodSource;
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

    private FoodSource targetFoodSource;

    // ---- Reproduction ----
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
                if (other != null && other.gender != gender)
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
        if (partner == null || partner.gender == gender) return;

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
            if (!partner.isPregnant && partner.foodLevel >= partner.maxFoodLevel * 0.7f && (Time.time - lastImpregnationTime) >= reproductionCooldown && foodLevel >= partner.maxFoodLevel * 0.8f)
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
        childBehavior.size = Mathf.Round((avgSize + UnityEngine.Random.Range(-mutationRate, mutationRate) * avgSize) * 100f) / 100f; // Fixed: Line 617
        childBehavior.walkingSpeed = Mathf.Round((avgSpeed + UnityEngine.Random.Range(-mutationRate, mutationRate) * avgSpeed) * 100f) / 100f; // Fixed: Line 618
        childBehavior.canClimb = mother.canClimb || father.canClimb;

        childBehavior.currentAge = 0f;
        childBehavior.foodLevel = (int)childBehavior.maxFoodLevel;
        childBehavior.gender = (UnityEngine.Random.value < 0.5f) ? Gender.Male : Gender.Female; // Fixed: Line 623
        childBehavior.isPregnant = false;
        childBehavior.totalFoodLostSincePregnant = 0;
        childBehavior.lastImpregnationTime = -1000f;
        childBehavior.pregnantWith = null;

        // Inherit traits using traitIds
        List<int> parentTraitIds = new List<int>(mother.traitIds);
        parentTraitIds.AddRange(father.traitIds);
        parentTraitIds = parentTraitIds.Distinct().ToList();

        if (parentTraitIds.Count > 0)
        {
            int numTraitsToInherit = UnityEngine.Random.Range(0, parentTraitIds.Count + 1); // Fixed: Line 636
            for (int i = 0; i < numTraitsToInherit; i++)
            {
                int traitId = parentTraitIds[UnityEngine.Random.Range(0, parentTraitIds.Count)]; // Fixed: Line 639
                Type traitType = TraitManager.GetTraitType(traitId);
                if (traitType != null)
                {
                    childBehavior.gameObject.AddComponent(traitType);
                    childBehavior.traitIds.Add(traitId);
                }
            }
        }

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

    // ---- Utility Methods ----
    private void PanicWander()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance))
        {
            Vector3 pos = transform.position + UnityEngine.Random.insideUnitSphere * (wanderRadius * 2f); // Fixed: Line 677
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, wanderRadius * 2f, agent.areaMask) && IsNavigable(hit.position))
            {
                agent.SetDestination(hit.position);
            }
        }
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
                if (other != null && other.currentState != State.Dead)
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

    private bool IsNavigable(Vector3 targetPosition)
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;

        NavMeshPath path = new NavMeshPath();
        bool pathValid = agent.CalculatePath(targetPosition, path);
        return pathValid && path.status == NavMeshPathStatus.PathComplete;
    }

    private void UpdateDiscoverablesDetection()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, discoverableLayer);
        visibleDiscoverables.Clear();
        foreach (var hit in hits)
            if (hit.transform != transform)
                visibleDiscoverables.Add(hit.transform);
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
        transform.localScale = Vector3.one * size;
        foreach (Transform child in transform) child.gameObject.SetActive(true);
        UpdateTextDisplay();
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
            string text = $"Food: {foodLevel}/{maxFoodLevel}\nHP: {(currentState == State.Dead ? 0 : Mathf.CeilToInt(health))}/{maxHealth}";
            if (gender == Gender.Female && isPregnant)
            {
                int reproductionCostCounter = totalFoodLostSincePregnant / 2;
                text += $"\nPregnant: {reproductionCostCounter}/{ReproductionCost}";
            }
            text += $"\n{gender}";
            textDisplay.text = text;
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, avoidanceDistance);
    }
}