using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System;

public class CreatureBehavior : MonoBehaviour
{
    private NavMeshAgent agent;
    public CreatureCombat creatureCombat { get; private set; }
    private TextMeshPro textDisplay;

    public enum State { Idle, SearchingForFood, Eating, SeekingMate, Attacking, Fleeing, Dead, Burrowing }
    [Header("Creature States")]
    [SerializeField] public State currentState = State.Idle;

    public enum CombatBehavior { Friendly, Neutral, Hunter }
    [Header("Combat Behavior")]
    [SerializeField] public CombatBehavior combatBehavior = CombatBehavior.Neutral;

    public enum Gender { Male, Female }
    [Header("Gender")]
    [SerializeField] public Gender gender;

    [Header("Traits")]
    [SerializeField] public List<int> traitIds = new List<int>();

    [Header("Physical Attributes")]
    [SerializeField] public float baseSize = 1f;
    [SerializeField] public float baseWalkingSpeed = 4f;
    [SerializeField] public float baseHealth = 10f;
    [SerializeField] public float baseMaxFoodLevel = 10f;
    [SerializeField] public float baseHungerDecreaseInterval = 30f;
    [SerializeField] public float baseReproductionCheckInterval = 20f;
    [SerializeField] public bool canClimb = false;
    public float sizeMultiplier = 1f;
    public float walkingSpeedMultiplier = 1f;
    public float healthMultiplier = 1f;
    public float maxFoodLevelMultiplier = 1f;
    public float hungerDecreaseIntervalMultiplier = 1f;
    public float reproductionCheckIntervalMultiplier = 1f;

    public float Size => baseSize * sizeMultiplier;
    public float Health
    {
        get => baseHealth * healthMultiplier;
        set => baseHealth = value / healthMultiplier;
    }
    private float walkingSpeed => baseWalkingSpeed * walkingSpeedMultiplier;
    public float MaxFoodLevel => baseMaxFoodLevel * maxFoodLevelMultiplier;
    private float hungerDecreaseInterval => baseHungerDecreaseInterval * hungerDecreaseIntervalMultiplier;
    private float reproductionCheckInterval => baseReproductionCheckInterval * reproductionCheckIntervalMultiplier;

    private float newWalkingSpeed => walkingSpeed;
    private float sprintSpeed
    {
        get
        {
            float speed = walkingSpeed * (traitIds.Contains(8) ? 3.0f : 2.0f); // Hunter: 3x, others: 2x
            if (traitIds.Contains(28) && Health < Size * 10f * 0.3f) speed *= 1.5f; // Adrenaline
            return speed;
        }
    }

    [Header("Health and Hunger")]
    [SerializeField] public float foodLevel;
    [SerializeField] private float hungerTimer = 0f;

    [Header("Stamina")]
    [SerializeField] public float maxStamina = 10f;
    [SerializeField] public float stamina = 10f;
    [SerializeField] public float staminaRegenRate = 1f;
    [SerializeField] public float staminaDrainRate = 1f;

    [Header("Age and Lifespan")]
    [SerializeField] public float maxAge = 100f;
    [SerializeField] public float currentAge = 0f;
    public float Age { get => currentAge; set => currentAge = value; }

    [Header("Reproduction")]
    [SerializeField] private bool isPregnant = false;
    [SerializeField] private int totalFoodLostSincePregnant = 0;
    [SerializeField] private float lastImpregnationTime = -1000f;
    private CreatureBehavior pregnantWith;
    [SerializeField] private float reproductionCooldown = 60f;
    private Transform reproductionTarget;
    [SerializeField] private float reproductionDistance = 3f;
    [SerializeField] private float reproductionTimer = 0f;

    public bool IsPregnant => isPregnant;
    public int TotalFoodLostSincePregnant => totalFoodLostSincePregnant;
    public int ReproductionCost => Mathf.RoundToInt(Size * (traitIds.Contains(41) ? 6.4f : 8f)) + Mathf.FloorToInt(walkingSpeed / 3); // SwiftBreeder: -20%

    [Header("Detection and Interaction")]
    [SerializeField] public float baseDetectionRadius = 50f;
    private float detectionRadius => baseDetectionRadius + (Mathf.Floor(Size - 1f) * (baseDetectionRadius / 5f));
    [SerializeField] private LayerMask discoverableLayer;
    [SerializeField] private List<Transform> visibleDiscoverables = new();
    [SerializeField] private float eatingDistance = 2f;

    [Header("Movement and Wandering")]
    [SerializeField] public float wanderRadius = 20f;
    [SerializeField] private float wanderTimer = 0f;
    [SerializeField] private float wanderInterval = 5f;
    [SerializeField] private float migrationTimer = 0f; // For Migratory

    [Header("Eating Mechanics")]
    [SerializeField] private float eatTimer = 0f;
    [SerializeField] private float noFoodTimer = 0f;

    [Header("Combat")]
    [SerializeField] public Transform combatTarget;
    [SerializeField] private float attackTimer = 0f;
    private float attackInterval => traitIds.Contains(36) ? 1f * 1.2f : 1f; // Ambusher: -20% attack speed

    [Header("Burrowing")]
    [SerializeField] public bool isBurrowing = false;
    [SerializeField] public float burrowTimer = 0f;
    [SerializeField] public float burrowCooldown = 0f;

    [Header("Immobilization")]
    [SerializeField] public bool immobilized = false;
    [SerializeField] public float immobilizedTimer = 0f;
    [SerializeField] public CreatureBehavior lastAttackedBy;

    [Header("Base Reference")]
    [SerializeField] public CreatureBase owningBase; // Reference to the spawning base

    public enum FoodType { Apple, Berry, Meat }
    [Header("Food Preferences")]
    [SerializeField] public List<FoodType> foodPreferences = new List<FoodType> { FoodType.Apple };

    private const int WalkableArea = 1 << 0;
    private const int JumpArea = 1 << 2;
    private const int ClimbArea = 1 << 3;

    public float BaseSize
    {
        get => baseSize;
        set
        {
            baseSize = value;
            UpdateSizeAndStats();
        }
    }
    public float BaseHealth
    {
        get => baseHealth;
        set
        {
            baseHealth = value;
            UpdateSizeAndStats();
        }
    }
    public float BaseWalkingSpeed
    {
        get => baseWalkingSpeed;
        set
        {
            baseWalkingSpeed = value;
            UpdateSizeAndStats();
        }
    }
    public float BaseMaxFoodLevel
    {
        get => baseMaxFoodLevel;
        set
        {
            baseMaxFoodLevel = value;
            UpdateSizeAndStats();
        }
    }
    public float BaseHungerDecreaseInterval
    {
        get => baseHungerDecreaseInterval;
        set
        {
            baseHungerDecreaseInterval = value;
            UpdateSizeAndStats();
        }
    }
    public float BaseReproductionCheckInterval
    {
        get => baseReproductionCheckInterval;
        set
        {
            baseReproductionCheckInterval = value;
            UpdateSizeAndStats();
        }
    }
    public float MaxAge { get => maxAge; set => maxAge = value; }
    public float FoodLevel => foodLevel;
    public float MaxStamina => maxStamina;
    public float Stamina => stamina;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent) { Debug.LogError($"{name}: No NavMeshAgent! Disabling."); enabled = false; return; }
        creatureCombat = GetComponent<CreatureCombat>();
        if (!creatureCombat) { Debug.LogError($"{name}: No CreatureCombat!"); }

        SetupTextDisplay();

        baseSize = Mathf.Round(baseSize * 100f) / 100f;
        baseWalkingSpeed = Mathf.Round(baseWalkingSpeed * 100f) / 100f;
        baseHealth = Mathf.Max(baseHealth, Mathf.Ceil(Size * 10f));

        // Apply trait effects
        foreach (int traitId in traitIds)
        {
            Type traitType = TraitManager.GetTraitType(traitId);
            if (traitType != null)
            {
                Trait trait = (Trait)gameObject.AddComponent(traitType);
                trait.ApplyTrait(this);
            }
        }

        int areaMask = WalkableArea | JumpArea;
        if (canClimb) areaMask |= ClimbArea;
        agent.areaMask = areaMask;

        UpdateSizeAndStats();
        foodLevel = MaxFoodLevel;
        stamina = maxStamina;

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
        if (foodSource != null)
        {
            foodSource.maxFood = (int)MaxFoodLevel;
            foodSource.currentFood = (int)MaxFoodLevel;
            foodSource.enabled = false;
        }

        wanderInterval = UnityEngine.Random.Range(2.5f, 10f);
        agent.speed = newWalkingSpeed;

        // Register with CreatureBase if not already done
        if (owningBase != null)
        {
            owningBase.AddCreature(this);
        }
        else
        {
            Debug.LogWarning($"{name}: No owning CreatureBase assigned!");
        }

        UpdateTextDisplay();
        Debug.Log($"{name}: Initialized - Gender: {gender}, Size: {Size}, Speed: {walkingSpeed}, Food: {foodLevel}/{MaxFoodLevel}, Stamina: {stamina}/{maxStamina}");
    }

    void Update()
    {
        if (currentState == State.Dead || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float deltaTime = Time.deltaTime;
        UpdateDiscoverablesDetection();

        // Immobilization
        if (immobilized)
        {
            immobilizedTimer -= deltaTime;
            agent.isStopped = true;
            if (immobilizedTimer <= 0)
            {
                immobilized = false;
                agent.isStopped = false;
                Debug.Log($"{name}: No longer immobilized");
            }
        }

        // Burrowing
        if (isBurrowing)
        {
            burrowTimer -= deltaTime;
            agent.isStopped = true;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // Undetectable
            if (burrowTimer <= 0)
            {
                isBurrowing = false;
                agent.isStopped = false;
                gameObject.layer = LayerMask.NameToLayer("Default");
                Debug.Log($"{name}: Emerged from burrowing");
            }
            UpdateTextDisplay();
            return; // Skip other updates while burrowing
        }
        else if (traitIds.Contains(43))
        {
            burrowCooldown -= deltaTime;
        }

        // Stamina management
        bool isSprinting = (currentState == State.Fleeing || currentState == State.Attacking || (currentState == State.SearchingForFood && visibleDiscoverables.Count == 0));
        if (isSprinting && stamina > maxStamina * 0.25f)
        {
            stamina = Mathf.Max(stamina - staminaDrainRate * deltaTime, 0);
            agent.speed = sprintSpeed;
        }
        else
        {
            stamina = Mathf.Min(stamina + staminaRegenRate * deltaTime, maxStamina);
            agent.speed = newWalkingSpeed;
        }

        // Altruist food sharing (10% chance per second)
        if (traitIds.Contains(17) && foodLevel > MaxFoodLevel * 0.4f && UnityEngine.Random.value < 0.1f * deltaTime)
        {
            TryShareFood();
        }

        currentAge += deltaTime / 60f;
        if (currentAge >= maxAge && !traitIds.Contains(6)) // Immortal prevents age death
        {
            StartCoroutine(DieWithRotation());
            return;
        }

        float hungerMultiplier = isPregnant ? 2f : 1f;
        if (currentState == State.SearchingForFood || currentState == State.Attacking || currentState == State.Fleeing)
            hungerMultiplier *= 1.5f;

        hungerTimer += deltaTime * hungerMultiplier;
        while (hungerTimer >= hungerDecreaseInterval)
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
            hungerTimer -= hungerDecreaseInterval;
            UpdateTextDisplay();
        }

        if (foodLevel <= 0)
        {
            Health = Mathf.Max(Health - deltaTime * 0.5f, 0);
            if (Health <= 0) StartCoroutine(DieWithRotation());
        }

        if (foodLevel > 0 && Health < Mathf.Ceil(Size * 10f))
        {
            if (hungerTimer >= 10f)
            {
                Health = Mathf.Min(Health + 1, Mathf.Ceil(Size * 10f));
                hungerTimer = 0f;
                UpdateTextDisplay();
            }
        }

        if (foodLevel <= MaxFoodLevel * 0.4f && currentState == State.Idle)
        {
            currentState = State.SearchingForFood;
            noFoodTimer = 0f;
            Debug.Log($"{name}: Hungry, searching");
        }

        if (currentAge >= maxAge * 0.1f && currentState != State.Eating && currentState != State.SeekingMate)
        {
            reproductionTimer += deltaTime;
            if (reproductionTimer >= reproductionCheckInterval)
            {
                if (CanSeekReproduction() && UnityEngine.Random.value < 1f / 3f)
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

        // Migratory wandering
        if (traitIds.Contains(31))
        {
            migrationTimer += deltaTime;
            if (migrationTimer >= 120f) // 2 minutes
            {
                WanderMigratory();
                migrationTimer = 0f;
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
                    noFoodTimer = 0f;
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

    private void TryShareFood()
    {
        foreach (var obj in visibleDiscoverables)
        {
            if (obj.CompareTag("Creature"))
            {
                CreatureBehavior other = obj.GetComponent<CreatureBehavior>();
                if (other != null && other != this && other.foodLevel <= other.MaxFoodLevel * 0.4f)
                {
                    foodLevel = Mathf.Max(foodLevel - 1, 0);
                    other.foodLevel = Mathf.Min(other.foodLevel + 1, other.MaxFoodLevel);
                    Debug.Log($"{name} (Altruist) shared 1 food with {other.name}");
                    UpdateTextDisplay();
                    other.UpdateTextDisplay();
                    break;
                }
            }
        }
    }

    private void UpdateSizeAndStats()
    {
        transform.localScale = Vector3.one * Size;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.radius = Size * 0.5f;
            agent.height = Size;
            agent.speed = newWalkingSpeed;
        }
    }

    private void Wander()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            Vector3 destination = transform.position + UnityEngine.Random.insideUnitSphere * wanderRadius;
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, wanderRadius, agent.areaMask) && IsNavigable(hit.position))
            {
                agent.SetDestination(hit.position);
            }
            wanderTimer = 0f;
            wanderInterval = UnityEngine.Random.Range(2.5f, 10f);
        }
    }

    private void WanderMigratory()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        Vector3 destination = transform.position + UnityEngine.Random.insideUnitSphere * (wanderRadius * 4f);
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, wanderRadius * 4f, agent.areaMask) && IsNavigable(hit.position))
        {
            agent.SetDestination(hit.position);
            Debug.Log($"{name} (Migratory) moving to new location");
        }
    }

    private void SearchForFood()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (visibleDiscoverables.Count == 0 || foodPreferences.Count == 0)
        {
            HandleNoFood();
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
                if (obj.CompareTag(tag) || (traitIds.Contains(45) && obj.CompareTag("Meat"))) // Cannibal includes same-type Meat
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
                        noFoodTimer = 0f;
                    }
                    return;
                }
            }
        }

        HandleNoFood();
    }

    private void HandleNoFood()
    {
        noFoodTimer += Time.deltaTime;

        if (combatBehavior == CombatBehavior.Hunter && noFoodTimer >= 10f)
        {
            combatTarget = FindHuntingTarget();
            if (combatTarget != null)
            {
                currentState = State.Attacking;
                agent.SetDestination(combatTarget.position);
                noFoodTimer = 0f;
                return;
            }
        }

        PanicWander();
    }

    private void Eat()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        eatTimer += Time.deltaTime;
        if (eatTimer >= 1f)
        {
            if (targetFoodSource != null && targetFoodSource.HasFood && targetFoodSource.CurrentFood > 0 && foodLevel < MaxFoodLevel)
            {
                targetFoodSource.CurrentFood--;
                foodLevel = Mathf.Min(foodLevel + (int)targetFoodSource.FoodSatiety, (int)MaxFoodLevel);
                UpdateTextDisplay();
            }
            else
            {
                currentState = foodLevel >= MaxFoodLevel ? State.Idle : State.SearchingForFood;
                agent.isStopped = false;
                targetFoodSource = null;
                noFoodTimer = 0f;
                Debug.Log($"{name}: {(foodLevel >= MaxFoodLevel ? "Full" : "Food gone")}");
            }
            eatTimer = 0f;
        }
    }

    private FoodSource targetFoodSource;

    private bool CanSeekReproduction()
    {
        if (gender == Gender.Female)
        {
            return !isPregnant && foodLevel >= MaxFoodLevel * 0.7f;
        }
        else
        {
            return (Time.time - lastImpregnationTime) >= reproductionCooldown && foodLevel >= MaxFoodLevel * 0.7f;
        }
    }

    private Transform FindMateForReproduction()
    {
        float range = detectionRadius * (traitIds.Contains(54) ? 2f : 1.5f); // Pheromonal: +50% range
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
                        if ((Time.time - other.lastImpregnationTime) >= reproductionCooldown && other.foodLevel >= other.MaxFoodLevel * 0.8f)
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
                        if (!other.isPregnant && other.foodLevel >= other.MaxFoodLevel * 0.7f)
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
            if (!isPregnant && foodLevel >= MaxFoodLevel * 0.7f && (Time.time - partner.lastImpregnationTime) >= reproductionCooldown && partner.foodLevel >= partner.MaxFoodLevel * 0.8f)
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
            if (!partner.isPregnant && partner.foodLevel >= partner.MaxFoodLevel * 0.7f && (Time.time - lastImpregnationTime) >= reproductionCooldown && foodLevel >= partner.MaxFoodLevel * 0.8f)
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

        childBehavior.baseSize = mother.baseSize;
        childBehavior.baseWalkingSpeed = mother.baseWalkingSpeed;
        childBehavior.baseHealth = mother.baseHealth;
        childBehavior.baseMaxFoodLevel = mother.baseMaxFoodLevel;
        childBehavior.baseHungerDecreaseInterval = mother.baseHungerDecreaseInterval;
        childBehavior.baseReproductionCheckInterval = mother.baseReproductionCheckInterval;
        childBehavior.canClimb = mother.canClimb;
        childBehavior.traitIds = new List<int>(mother.traitIds);

        childBehavior.currentAge = 0f;
        childBehavior.foodLevel = (int)childBehavior.MaxFoodLevel;
        childBehavior.stamina = childBehavior.maxStamina;
        childBehavior.gender = (UnityEngine.Random.value < 0.5f) ? Gender.Male : Gender.Female;
        childBehavior.isPregnant = false;
        childBehavior.totalFoodLostSincePregnant = 0;
        childBehavior.lastImpregnationTime = -1000f;
        childBehavior.pregnantWith = null;
        childBehavior.owningBase = mother.owningBase; // Inherit mother's base

        if (NavMesh.SamplePosition(child.transform.position, out NavMeshHit hit, 5f, childBehavior.agent.areaMask))
        {
            child.transform.position = hit.position;
            childBehavior.agent.Warp(hit.position);
        }

        if (childBehavior.owningBase != null)
        {
            // Assign name before adding to CreatureBase
            childBehavior.gameObject.name = childBehavior.owningBase.GenerateChildName(childBehavior);
            childBehavior.owningBase.AddCreature(childBehavior);
        }
        else
        {
            Debug.LogWarning($"Child {childBehavior.gameObject.name}: No CreatureBase found to register with!");
        }

        Debug.Log($"Child born: {childBehavior.name}, Gender: {childBehavior.gender}, Size: {childBehavior.Size}, Speed: {childBehavior.walkingSpeed}, CanClimb: {childBehavior.canClimb}");
    }

    private void BirthBaby()
    {
        if (pregnantWith == null) return;
        SpawnChild(this, pregnantWith);
        float twinChance = traitIds.Contains(16) ? 0.1f : 0.05f; // Prolific: 10%, others: 5%
        if (UnityEngine.Random.value < twinChance)
        {
            SpawnChild(this, pregnantWith);
            Debug.Log($"{name} gave birth to twins!");
        }
        isPregnant = false;
        totalFoodLostSincePregnant = 0;
        pregnantWith = null;
    }

    private void PanicWander()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance))
        {
            Vector3 pos = transform.position + UnityEngine.Random.insideUnitSphere * (wanderRadius * 2f);
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
                if (other != null && other.currentState != State.Dead && other != this)
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

    private bool IsNavigable(Vector3 targetPosition)
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;

        NavMeshPath path = new NavMeshPath();
        bool pathValid = agent.CalculatePath(targetPosition, path);
        return pathValid && path.status == NavMeshPathStatus.PathComplete;
    }

    private void UpdateDiscoverablesDetection()
    {
        if (isBurrowing) return; // Undetectable while burrowing

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        visibleDiscoverables.Clear();
        foreach (var hit in hits)
            if (hit.transform != transform)
                visibleDiscoverables.Add(hit.transform);
    }

    public float GetFoodDetectionRadius()
    {
        return detectionRadius * (traitIds.Contains(59) ? 1.25f : 1f); // Keen: +25%
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
        if (foodSource != null)
        {
            foodSource.enabled = true;
            foodSource.isNotReplenishable = true;
            foodSource.maxFood = (int)MaxFoodLevel;
            foodSource.CurrentFood = (int)MaxFoodLevel;
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers) renderer.enabled = true;
        transform.localScale = Vector3.one * Size;
        foreach (Transform child in transform) child.gameObject.SetActive(true);
        UpdateTextDisplay();

        if (owningBase != null)
            owningBase.RemoveCreature(this);
    }

    private void SetupTextDisplay()
    {
        GameObject textObj = new GameObject($"{name}_StatusText");
        textObj.transform.SetParent(transform);
        textDisplay = textObj.AddComponent<TextMeshPro>();

        textDisplay.transform.localPosition = new Vector3(0, Size * 2.5f, 0);
        textDisplay.transform.localRotation = Quaternion.identity;

        textDisplay.fontSize = 3f;
        textDisplay.alignment = TextAlignmentOptions.Center;
        textDisplay.textWrappingMode = TextWrappingModes.NoWrap;
        textDisplay.color = Color.white;
        textDisplay.outlineWidth = 0.1f;
        textDisplay.outlineColor = Color.black;
    }

    public void UpdateTextDisplay()
    {
        if (!textDisplay) return;

        int maxHealth = Mathf.CeilToInt(Size * 10f);
        string text = $"Food: {foodLevel:F0}/{MaxFoodLevel:F0}\nHP: {(currentState == State.Dead ? 0 : Mathf.CeilToInt(Health))}/{maxHealth}\nStamina: {stamina:F0}/{maxStamina:F0}";
        if (gender == Gender.Female && isPregnant)
            text += $"\nPregnant: {totalFoodLostSincePregnant / 2}/{ReproductionCost}";
        if (isBurrowing)
            text += "\nBurrowing";
        text += $"\n{gender}";
        textDisplay.text = text;
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
    }
}