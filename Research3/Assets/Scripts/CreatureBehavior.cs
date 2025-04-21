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

    public enum State { Idle, SearchingForFood, Eating, SeekingMate, Attacking, Fleeing, Dead, Burrowing, Work }
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
    [SerializeField] public float baseHealth = 20f;
    [SerializeField] public float baseMaxFoodLevel = 10f;
    [SerializeField] public float baseHungerDecreaseInterval = 40f;
    [SerializeField] public float baseDefense = 0f;
    [SerializeField] public bool canClimb = false;
    public float sizeMultiplier = 1f;
    public float walkingSpeedMultiplier = 1f;
    public float healthMultiplier = 1f;
    public float maxFoodLevelMultiplier = 1f;
    public float hungerDecreaseIntervalMultiplier = 1f;
    public float reproductionIntervalMultiplier = 1f;
    public float reproductionChanceMultiplier = 1f;
    public float mateDetectionRadiusMultiplier = 1.5f;
    public float reproductionCostMultiplier = 1f;
    public float defenseMultiplier = 1f;

    public float Size => baseSize * sizeMultiplier;
    public float Health
    {
        get => baseHealth * healthMultiplier;
        set => baseHealth = value / healthMultiplier;
    }
    public float Defense
    {
        get => baseDefense * defenseMultiplier;
        set => baseDefense = value / defenseMultiplier;
    }
    private float walkingSpeed => baseWalkingSpeed * walkingSpeedMultiplier;
    public float MaxFoodLevel => Mathf.Ceil(baseMaxFoodLevel * maxFoodLevelMultiplier);
    private float hungerDecreaseInterval => baseHungerDecreaseInterval * hungerDecreaseIntervalMultiplier;
    private float reproductionInterval => 40f * reproductionIntervalMultiplier;
    private float reproductionChance => 25f * reproductionChanceMultiplier;
    private float baseReproductionCost => 10f;

    private float newWalkingSpeed => walkingSpeed;
    private float sprintSpeed
    {
        get
        {
            float speed = walkingSpeed * 2.0f;
            if (traitIds.Contains(8)) speed *= 1.5f;
            if (traitIds.Contains(19) && Health < baseHealth * healthMultiplier * 0.3f) speed *= 1.5f;
            return speed;
        }
    }

    [Header("Health and Hunger")]
    [SerializeField] public float foodLevel;
    [SerializeField] private float hungerTimer = 0f;
    [SerializeField] private bool isDyingOfOldAge = false;

    [Header("Stamina")]
    [SerializeField] public float maxStamina = 20f;
    [SerializeField] public float stamina = 20f;
    [SerializeField] public float staminaRegenRate = 2f;
    [SerializeField] public float staminaDrainRate = 1f;
    [SerializeField] public float lastStaminaUseTime = 0f;
    [SerializeField] private bool canSprint = true;

    [Header("Age and Lifespan")]
    [SerializeField] public float maxAge = 30f;
    [SerializeField] private float _adultAge = 10f;
    public float adultAge { get => _adultAge; set => _adultAge = value; }
    [SerializeField] public float currentAge = 0f;
    public float Age { get => currentAge; set => currentAge = value; }

    [Header("Reproduction")]
    [SerializeField] public bool isPregnant = false;
    [SerializeField] public int totalFoodLostSincePregnant = 0;
    [SerializeField] public float lastImpregnationTime = -1000f;
    [SerializeField] public float lastBirthTime = -1000f;
    [SerializeField] public CreatureBehavior pregnantWith;
    [SerializeField] public CreatureBehavior mother;
    [SerializeField] public CreatureBehavior father;
    [SerializeField] private CreatureBehavior firstMate;
    [SerializeField] private float maleReproductionCooldown = 160f;
    [SerializeField] private float femaleReproductionCooldown = 120f;
    [SerializeField] private float twinChance = 0.05f;
    [SerializeField] private float tripletChance = 0.005f;
    public float TwinChance { get => twinChance; set => twinChance = value; }
    public float TripletChance { get => tripletChance; set => tripletChance = value; }
    private Transform reproductionTarget;
    [SerializeField] private float reproductionDistance = 3f;
    [SerializeField] private float reproductionTimer = 0f;

    public bool IsPregnant => isPregnant;
    public int TotalFoodLostSincePregnant => totalFoodLostSincePregnant;
    public int ReproductionCost => Mathf.RoundToInt(baseReproductionCost * reproductionCostMultiplier);

    [Header("Detection and Interaction")]
    [SerializeField] public float baseDetectionRadius = 50f;
    private float detectionRadius => baseDetectionRadius + (Mathf.Floor(Size - 1f) * (baseDetectionRadius / 5f));
    public float DetectionRadius => detectionRadius;
    [SerializeField] private LayerMask discoverableLayer;
    [SerializeField] private List<Transform> visibleDiscoverables = new();
    [SerializeField] private float eatingDistance = 2f;

    [Header("Movement and Wandering")]
    [SerializeField] public float wanderRadius = 20f;
    [SerializeField] private float wanderTimer = 0f;
    [SerializeField] private float wanderInterval = 5f;
    [SerializeField] private float migrationTimer = 0f;

    [Header("Eating Mechanics")]
    [SerializeField] private float eatTimer = 0f;
    [SerializeField] private float noFoodTimer = 0f;
    private FoodSource reservedFoodSource;
    private float reservedFoodAmount = 0f;

    [Header("Work Mechanics")]
    [SerializeField] private float currentCarriedFood = 0f;
    private float maxCarryLimit => Mathf.Ceil(MaxFoodLevel / 2f);
    private bool isMovingBase = false;
    private float depositFailTimer = 0f;
    private const float depositTimeout = 30f;

    [Header("Combat")]
    [SerializeField] public Transform combatTarget;
    [SerializeField] private float attackTimer = 0f;
    private float attackInterval => traitIds.Contains(23) ? 1f * 1.2f : 1f;

    [Header("Burrowing")]
    [SerializeField] public bool isBurrowing = false;
    [SerializeField] public float burrowTimer = 0f;
    [SerializeField] public float burrowCooldown = 0f;

    [Header("Immobilization")]
    [SerializeField] public bool immobilized = false;
    [SerializeField] public float immobilizedTimer = 0f;
    [SerializeField] public CreatureBehavior lastAttackedBy;

    [Header("Base Reference")]
    [SerializeField] public CreatureBase owningBase;

    [Header("Visuals")]
    [SerializeField] public GameObject meshRendererObject;
    [SerializeField] public Color typeColor = Color.white;
    [SerializeField] public Light bioluminescentLight;

    public enum FoodType { Apple, Berry, Meat }
    [Header("Food Preferences")]
    [SerializeField] public List<FoodType> foodPreferences = new List<FoodType> { FoodType.Apple };

    private HashSet<CreatureBehavior> overlappingCreatures = new HashSet<CreatureBehavior>();
    private float unstickCooldown = 0f;

    [Header("Status Effect Icons")]
    [SerializeField] private GameObject poisonIcon;
    [SerializeField] private GameObject pregnantIcon;
    [SerializeField] private GameObject hungryIcon;
    [SerializeField] private GameObject panickingIcon;

    [Header("Status Effects")]
    [SerializeField] private bool poisoned = false;
    public bool Poisoned { get => poisoned; set => poisoned = value; }

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
    public float BaseDefense
    {
        get => baseDefense;
        set
        {
            baseDefense = value;
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

        maxAge = 3 * adultAge;

        foreach (int traitId in traitIds.ToList())
        {
            Type traitType = TraitManager.GetTraitType(traitId);
            if (traitType != null)
            {
                Trait trait = (Trait)gameObject.AddComponent(traitType);
                trait.ApplyTrait(this);
            }
        }

        maxAge += UnityEngine.Random.Range(-adultAge, adultAge);

        if (meshRendererObject != null)
        {
            SkinnedMeshRenderer renderer = meshRendererObject.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                renderer.material.SetColor("_BaseColor", typeColor);
                Debug.Log($"{name}: Applied color {typeColor} to SkinnedMeshRenderer");
            }
            else
            {
                Debug.LogWarning($"{name}: No SkinnedMeshRenderer found on meshRendererObject!");
            }
        }
        else
        {
            Debug.LogWarning($"{name}: meshRendererObject not assigned!");
        }

        if (bioluminescentLight != null)
        {
            bioluminescentLight.enabled = false;
        }

        int areaMask = WalkableArea | JumpArea;
        if (canClimb) areaMask |= ClimbArea;
        agent.areaMask = areaMask;

        UpdateSizeAndStats();
        foodLevel = Mathf.Ceil(MaxFoodLevel);
        stamina = maxStamina;
        Health = baseHealth * healthMultiplier;
        Defense = baseDefense * defenseMultiplier;
        reservedFoodSource = null;

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

        wanderInterval = 5f;
        agent.speed = newWalkingSpeed;

        if (owningBase != null)
        {
            owningBase.AddCreature(this);
        }
        else
        {
            Debug.LogWarning($"{name}: No owning CreatureBase assigned!");
        }

        UpdateTextDisplay();
        Debug.Log($"{name}: Initialized - Gender: {gender}, Size: {Size}, Speed: {walkingSpeed}, Food: {foodLevel}/{MaxFoodLevel}, Stamina: {stamina}/{maxStamina}, Defense: {Defense}, Color: {typeColor}");
    }

    void Update()
    {
        if (currentState == State.Dead || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        float deltaTime = Time.deltaTime;
        UpdateDiscoverablesDetection();
        UpdateSizeAndStats();

        if (currentState == State.Attacking && combatBehavior != CombatBehavior.Hunter && lastAttackedBy == null)
        {
            ChangeState(State.SearchingForFood);
            combatTarget = null;
            Debug.Log($"{name}: Non-hunter stopped attacking (not retaliating)");
        }

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

        if (isBurrowing)
        {
            burrowTimer -= deltaTime;
            agent.isStopped = true;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            if (burrowTimer <= 0)
            {
                isBurrowing = false;
                agent.isStopped = false;
                gameObject.layer = LayerMask.NameToLayer("Default");
                Debug.Log($"{name}: Emerged from burrowing");
            }
            UpdateTextDisplay();
            return;
        }
        else if (traitIds.Contains(25))
        {
            burrowCooldown -= deltaTime;
        }

        bool isSprinting = (currentState == State.Fleeing || 
                           currentState == State.Attacking || 
                           currentState == State.SearchingForFood || 
                           (currentState == State.Eating && agent.hasPath) ||
                           (currentState == State.Work && agent.hasPath));

        if (isSprinting && stamina > 0 && canSprint)
        {
            stamina = Mathf.Max(stamina - staminaDrainRate * deltaTime, 0);
            lastStaminaUseTime = Time.time;
            agent.speed = sprintSpeed;
        }
        else
        {
            agent.speed = newWalkingSpeed;
        }

        if (Time.time - lastStaminaUseTime >= 2f)
        {
            stamina = Mathf.Min(stamina + staminaRegenRate * deltaTime, maxStamina);
        }

        if (stamina <= 0) canSprint = false;
        if (stamina >= maxStamina * 0.25f) canSprint = true;

        if (traitIds.Contains(17) && foodLevel > MaxFoodLevel * 0.4f && UnityEngine.Random.value < 0.1f * deltaTime)
        {
            TryShareFood();
        }

        currentAge += deltaTime / 60f;

        if (currentAge >= maxAge)
        {
            isDyingOfOldAge = true;
        }

        if (isDyingOfOldAge)
        {
            Health = Mathf.Max(Health - deltaTime * 0.5f, 0);
            if (Health <= 0)
            {
                StartCoroutine(DieWithRotation());
                return;
            }
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
                    lastBirthTime = Time.time;
                }
            }
            hungerTimer -= hungerDecreaseInterval;
            UpdateTextDisplay();
        }

        if (isPregnant && Health < (baseHealth * healthMultiplier) * 0.5f)
        {
            isPregnant = false;
            totalFoodLostSincePregnant = 0;
            pregnantWith = null;
            Debug.Log($"{name}: Lost pregnancy due to low health");
        }

        if (foodLevel <= 0)
        {
            Health = Mathf.Max(Health - deltaTime * 0.5f, 0);
            if (Health <= 0) StartCoroutine(DieWithRotation());
        }

        if (foodLevel > 0 && Health < (baseHealth * healthMultiplier))
        {
            if (hungerTimer >= 10f)
            {
                Health = Mathf.Min(Health + 1, (baseHealth * healthMultiplier));
                hungerTimer = 0f;
                UpdateTextDisplay();
            }
        }

        // Check for hunger first (priority over work)
        if (foodLevel <= MaxFoodLevel * 0.4f && currentState == State.Idle)
        {
            bool foodFoundNearby = visibleDiscoverables.Any(obj => 
                (obj.CompareTag("Apple") || obj.CompareTag("Berry") || obj.CompareTag("Meat")) && 
                foodPreferences.Contains(GetFoodTypeFromTag(obj.tag)) && 
                obj.GetComponent<FoodSource>()?.HasFood == true);

            if (foodFoundNearby)
            {
                ChangeState(State.SearchingForFood);
                noFoodTimer = 0f;
            }
            else if (owningBase != null && owningBase.CurrentFood > 0)
            {
                ChangeState(State.Eating);
                agent.SetDestination(owningBase.transform.position);
                Debug.Log($"{name}: Heading to base to eat");
            }
            else
            {
                ChangeState(State.SearchingForFood);
                noFoodTimer = 0f;
            }
        }
        // Check for work state entry (after hunger check)
        else if (currentState == State.Idle && owningBase != null && owningBase.NeedsFood && !traitIds.Contains(100) && foodLevel > MaxFoodLevel * 0.4f)
        {
            // Only enter Work state if not hungry and base needs food
            if (visibleDiscoverables.Any(obj => 
                (obj.CompareTag("Apple") || obj.CompareTag("Berry") || obj.CompareTag("Meat")) && 
                foodPreferences.Contains(GetFoodTypeFromTag(obj.tag)) && 
                obj.GetComponent<FoodSource>()?.HasFood == true))
            {
                ChangeState(State.Work);
                Debug.Log($"{name}: Starting work to gather food for base");
            }
            else
            {
                ChangeState(State.SearchingForFood);
                Debug.Log($"{name}: Searching for food to work");
            }
        }
        // Existing trait-based work logic (for trait 100)
        else if (traitIds.Contains(100) && 
                 currentState == State.Idle && 
                 owningBase != null && 
                 owningBase.Leader != this && 
                 owningBase.NeedsFood)
        {
            if (owningBase.TryAssignBaseMoveJob(this))
            {
                isMovingBase = true;
                ChangeState(State.Work);
                Debug.Log($"{name}: Assigned to move the base");
            }
            else if (visibleDiscoverables.Any(obj => 
                (obj.CompareTag("Apple") || obj.CompareTag("Berry") || obj.CompareTag("Meat")) && 
                foodPreferences.Contains(GetFoodTypeFromTag(obj.tag)) && 
                obj.GetComponent<FoodSource>()?.HasFood == true))
            {
                ChangeState(State.Work);
                Debug.Log($"{name}: Starting work to gather food");
            }
            else
            {
                ChangeState(State.SearchingForFood);
                Debug.Log($"{name}: Searching for food to work");
            }
        }

        if (currentAge >= adultAge && currentState != State.Eating && currentState != State.SeekingMate && currentState != State.Work)
        {
            reproductionTimer += deltaTime;
            if (reproductionTimer >= reproductionInterval)
            {
                if (CanSeekReproduction() && UnityEngine.Random.Range(0f, 100f) <= reproductionChance)
                {
                    reproductionTarget = FindMateForReproduction();
                    if (reproductionTarget != null)
                    {
                        ChangeState(State.SeekingMate);
                        agent.SetDestination(reproductionTarget.position);
                        Debug.Log($"{name}: Seeking mate {reproductionTarget.name}");
                    }
                }
                reproductionTimer = 0f;
            }
        }

        if (traitIds.Contains(21))
        {
            migrationTimer += deltaTime;
            if (migrationTimer >= 120f)
            {
                WanderMigratory();
                migrationTimer = 0f;
            }
        }

        // Improved unsticking logic
        if (overlappingCreatures.Count > 0 && unstickCooldown <= 0)
        {
            foreach (var other in overlappingCreatures)
            {
                if (other.currentState == State.Dead) continue;

                float dist = Vector3.Distance(transform.position, other.transform.position);
                float minDist = (Size + other.Size) * 0.75f; // Increased separation distance
                if (dist < minDist)
                {
                    Vector3 pushDir = (transform.position - other.transform.position).normalized;
                    Vector3 newPos = transform.position + pushDir * (Size + other.Size) * 0.5f; // Push farther
                    if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, 2f, agent.areaMask))
                    {
                        transform.position = hit.position;
                        agent.Warp(hit.position);
                        agent.isStopped = true;
                        StartCoroutine(ResumeAgent());
                        unstickCooldown = 0.5f; // Reduced cooldown
                        Debug.Log($"{name}: Unstuck from {other.name}");
                    }
                }
            }
        }

        unstickCooldown -= deltaTime;

        reservedFoodSource?.UpdateReservations();

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
                    ChangeState(State.SearchingForFood);
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
                    ChangeState(State.Idle);
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
                    ChangeState(State.Idle);
                }
                break;
            case State.Work:
                PerformWork();
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
                    other.foodLevel = Mathf.Ceil(Mathf.Min(other.foodLevel + 1, other.MaxFoodLevel));
                    Debug.Log($"{name} (Altruistic) shared 1 food with {other.name}");
                    UpdateTextDisplay();
                    other.UpdateTextDisplay();
                    break;
                }
            }
        }
    }

    public void UpdateSizeAndStats() // Changed from private to public
    {
        float ageSizeMultiplier = currentAge <= adultAge ? (0.25f + (currentAge / adultAge) * (1f - 0.25f)) : 1f;
        transform.localScale = Vector3.one * Size * ageSizeMultiplier;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.radius = Size * ageSizeMultiplier * 0.5f;
            agent.height = Size * ageSizeMultiplier;
            agent.speed = newWalkingSpeed;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = Mathf.Clamp(50 - (int)(Size * 10f), 0, 99);
        }
    }

    private void Wander()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            Vector3 destination = transform.position + UnityEngine.Random.insideUnitSphere * wanderRadius;

            if (traitIds.Contains(100) && owningBase != null && owningBase.Leader != null && owningBase.Leader != this)
            {
                float leaderRadius = owningBase.Leader.DetectionRadius * 2f;
                float distanceToLeader = Vector3.Distance(destination, owningBase.Leader.transform.position);
                if (distanceToLeader > leaderRadius)
                {
                    destination = owningBase.Leader.transform.position + (destination - owningBase.Leader.transform.position).normalized * leaderRadius * 0.9f;
                }
            }

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, wanderRadius, agent.areaMask) && IsNavigable(hit.position))
            {
                agent.SetDestination(hit.position);
            }
            wanderTimer = 0f;
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

        if (reservedFoodSource != null)
        {
            reservedFoodSource.ReleaseReservation(this);
            reservedFoodSource = null;
            reservedFoodAmount = 0f;
        }

        if (visibleDiscoverables.Count == 0 || foodPreferences.Count == 0)
        {
            HandleNoFood();
            return;
        }

        Transform closest = null;
        float minDist = float.MaxValue;
        FoodSource closestFoodSource = null;
        float foodToReserve = 0f;

        foreach (var preferredType in foodPreferences)
        {
            string tag = GetTagFromFoodType(preferredType);
            if (string.IsNullOrEmpty(tag)) continue;

            foreach (var obj in visibleDiscoverables)
            {
                if (obj.CompareTag(tag) || obj.CompareTag("Meat"))
                {
                    float dist = Vector3.Distance(transform.position, obj.position);
                    FoodSource foodSource = obj.GetComponent<FoodSource>();
                    if (foodSource != null)
                    {
                        int availableFood = foodSource.CurrentFood - foodSource.ReservedFood;
                        if (dist < minDist && IsNavigable(obj.position) && availableFood > 0)
                        {
                            minDist = dist;
                            closest = obj;
                            closestFoodSource = foodSource;
                            float remainingCapacity = MaxFoodLevel - foodLevel;
                            foodToReserve = Mathf.Min(availableFood, remainingCapacity);
                        }
                    }
                }
            }

            if (closest != null) break;
        }

        if (closest != null && foodToReserve > 0)
        {
            int foodToReserveInt = Mathf.CeilToInt(foodToReserve);
            closestFoodSource.ReserveFood(this, foodToReserveInt, 30f);
            reservedFoodSource = closestFoodSource;
            reservedFoodAmount = foodToReserveInt;

            // Use offset position to avoid clustering
            Vector3 offsetPosition = closestFoodSource.GetOffsetPosition(this);
            if (NavMesh.SamplePosition(offsetPosition, out NavMeshHit hit, 5f, agent.areaMask))
            {
                agent.SetDestination(hit.position);
                if (Vector3.Distance(transform.position, hit.position) <= eatingDistance)
                {
                    ChangeState(State.Eating);
                    agent.isStopped = true;
                    this.targetFoodSource = closestFoodSource;
                    noFoodTimer = 0f;
                }
            }
            else
            {
                // Fallback to original position if offset is invalid
                agent.SetDestination(closest.position);
                if (Vector3.Distance(transform.position, closest.position) <= eatingDistance)
                {
                    ChangeState(State.Eating);
                    agent.isStopped = true;
                    this.targetFoodSource = closestFoodSource;
                    noFoodTimer = 0f;
                }
            }
        }
        else
        {
            HandleNoFood();
        }
    }

    private void HandleNoFood()
    {
        if (combatBehavior == CombatBehavior.Hunter && foodLevel < MaxFoodLevel * 0.2f)
        {
            bool nonLethalFoodAvailable = visibleDiscoverables.Any(obj => 
                (obj.CompareTag("Apple") || obj.CompareTag("Berry") || obj.CompareTag("Meat")) && 
                obj.GetComponent<FoodSource>()?.HasFood == true);

            if (!nonLethalFoodAvailable)
            {
                combatTarget = FindHuntingTarget();
                if (combatTarget != null)
                {
                    ChangeState(State.Attacking);
                    agent.SetDestination(combatTarget.position);
                    Debug.Log($"{name}: Hunting {combatTarget.name} (no non-lethal food available)");
                    return;
                }
            }
        }

        PanicWander();
    }

    private void Eat()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        // Check if we're eating from the base
        if (agent.destination == owningBase.transform.position && Vector3.Distance(transform.position, owningBase.transform.position) <= eatingDistance)
        {
            float neededFood = MaxFoodLevel - foodLevel;
            if (owningBase.EatFromBase(neededFood, out float amountEaten))
            {
                foodLevel += amountEaten;
                UpdateTextDisplay();
                Debug.Log($"{name}: Ate {amountEaten} food from base");
            }
            ChangeState(foodLevel >= MaxFoodLevel ? State.Idle : State.SearchingForFood);
            agent.isStopped = false;
            return;
        }

        // Instantly collect the reserved food from the food source
        if (targetFoodSource != null && targetFoodSource.HasFood && reservedFoodAmount > 0)
        {
            int foodToCollect = Mathf.Min(Mathf.CeilToInt(reservedFoodAmount), targetFoodSource.CurrentFood);
            targetFoodSource.CurrentFood -= foodToCollect;
            foodLevel += foodToCollect;
            Debug.Log($"{name}: Instantly collected {foodToCollect} food from {targetFoodSource.name}. Food level: {foodLevel}/{MaxFoodLevel}, Source food remaining: {targetFoodSource.CurrentFood}");
            UpdateTextDisplay();
        }

        // Clean up and transition state
        reservedFoodSource?.ReleaseReservation(this);
        reservedFoodSource = null;
        targetFoodSource = null;
        reservedFoodAmount = 0f;
        agent.isStopped = false;
        ChangeState(foodLevel >= MaxFoodLevel ? State.Idle : State.SearchingForFood);
        noFoodTimer = 0f;
        Debug.Log($"{name}: Finished eating. New state: {currentState}");
    }

    private void PerformWork()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (isMovingBase)
        {
            if (owningBase.Leader == null)
            {
                isMovingBase = false;
                ChangeState(State.Idle);
                return;
            }

            if (Vector3.Distance(transform.position, owningBase.transform.position) <= eatingDistance && owningBase.transform.parent != transform)
            {
                owningBase.transform.SetParent(transform);
                Debug.Log($"{name}: Picked up base");
            }

            agent.SetDestination(owningBase.Leader.transform.position);
            if (Vector3.Distance(transform.position, owningBase.Leader.transform.position) <= eatingDistance)
            {
                if (NavMesh.SamplePosition(owningBase.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    owningBase.transform.position = hit.position;
                    owningBase.transform.SetParent(null);
                    owningBase.CompleteBaseMove();
                    isMovingBase = false;
                    ChangeState(State.Idle);
                    Debug.Log($"{name}: Placed base at leader's location");
                }
            }
            return;
        }

        if (currentCarriedFood == 0)
        {
            Transform closest = null;
            float minDist = float.MaxValue;
            FoodSource closestFoodSource = null;
            float foodToReserve = 0f;

            foreach (var preferredType in foodPreferences)
            {
                string tag = GetTagFromFoodType(preferredType);
                if (string.IsNullOrEmpty(tag)) continue;

                foreach (var obj in visibleDiscoverables)
                {
                    if (obj.CompareTag(tag) || obj.CompareTag("Meat"))
                    {
                        float dist = Vector3.Distance(transform.position, obj.position);
                        FoodSource foodSource = obj.GetComponent<FoodSource>();
                        if (foodSource != null)
                        {
                            int availableFood = foodSource.CurrentFood - foodSource.ReservedFood;
                            if (dist < minDist && IsNavigable(obj.position) && availableFood > 0)
                            {
                                minDist = dist;
                                closest = obj;
                                closestFoodSource = foodSource;
                                // Reserve up to maxCarryLimit
                                foodToReserve = Mathf.Min(availableFood, maxCarryLimit - currentCarriedFood);
                            }
                        }
                    }
                }

                if (closest != null) break;
            }

            if (closest != null && foodToReserve > 0)
            {
                int foodToReserveInt = Mathf.CeilToInt(foodToReserve);
                closestFoodSource.ReserveFood(this, foodToReserveInt, 30f);
                reservedFoodSource = closestFoodSource;
                reservedFoodAmount = foodToReserveInt;

                // Use offset position to avoid clustering
                Vector3 offsetPosition = closestFoodSource.GetOffsetPosition(this);
                if (NavMesh.SamplePosition(offsetPosition, out NavMeshHit hit, 5f, agent.areaMask))
                {
                    agent.SetDestination(hit.position);
                    if (Vector3.Distance(transform.position, hit.position) <= eatingDistance)
                    {
                        // Instantly collect the reserved food
                        if (closestFoodSource.HasFood && reservedFoodAmount > 0)
                        {
                            int foodToCollect = Mathf.Min(Mathf.CeilToInt(reservedFoodAmount), closestFoodSource.CurrentFood);
                            closestFoodSource.CurrentFood -= foodToCollect;
                            currentCarriedFood += foodToCollect;
                            Debug.Log($"{name}: Instantly picked up {foodToCollect} food. Carrying: {currentCarriedFood}/{maxCarryLimit}, Source food remaining: {closestFoodSource.CurrentFood}");
                            UpdateTextDisplay();
                        }

                        reservedFoodSource?.ReleaseReservation(this);
                        reservedFoodSource = null;
                        reservedFoodAmount = 0f;
                        agent.isStopped = false;

                        // Head back to base immediately
                        agent.SetDestination(owningBase.transform.position);
                        depositFailTimer = 0f;
                    }
                }
                else
                {
                    // Fallback to original position if offset is invalid
                    agent.SetDestination(closest.position);
                    if (Vector3.Distance(transform.position, closest.position) <= eatingDistance)
                    {
                        if (closestFoodSource.HasFood && reservedFoodAmount > 0)
                        {
                            int foodToCollect = Mathf.Min(Mathf.CeilToInt(reservedFoodAmount), closestFoodSource.CurrentFood);
                            closestFoodSource.CurrentFood -= foodToCollect;
                            currentCarriedFood += foodToCollect;
                            Debug.Log($"{name}: Instantly picked up {foodToCollect} food. Carrying: {currentCarriedFood}/{maxCarryLimit}, Source food remaining: {closestFoodSource.CurrentFood}");
                            UpdateTextDisplay();
                        }

                        reservedFoodSource?.ReleaseReservation(this);
                        reservedFoodSource = null;
                        reservedFoodAmount = 0f;
                        agent.isStopped = false;

                        agent.SetDestination(owningBase.transform.position);
                        depositFailTimer = 0f;
                    }
                }
            }
            else
            {
                ChangeState(State.SearchingForFood);
            }
        }
        else
        {
            agent.SetDestination(owningBase.transform.position);
            if (Vector3.Distance(transform.position, owningBase.transform.position) <= eatingDistance)
            {
                if (owningBase.DepositFood(currentCarriedFood))
                {
                    currentCarriedFood = 0f;
                    // Check if base still needs food before continuing work
                    ChangeState(owningBase.NeedsFood ? State.Work : State.Idle);
                    depositFailTimer = 0f;
                }
                else
                {
                    depositFailTimer += Time.deltaTime;
                    Debug.Log($"{name}: Waiting to deposit food at base. Timer: {depositFailTimer}/{depositTimeout}");
                    if (depositFailTimer >= depositTimeout)
                    {
                        Debug.Log($"{name}: Timed out waiting to deposit food. Dropping food and searching again.");
                        currentCarriedFood = 0f;
                        ChangeState(State.SearchingForFood);
                        depositFailTimer = 0f;
                    }
                }
            }
            else
            {
                depositFailTimer = 0f;
            }
        }
    }

    private FoodSource targetFoodSource;

    private bool CanSeekReproduction()
    {
        if (currentAge < adultAge || foodLevel < MaxFoodLevel * 0.75f)
            return false;

        if (gender == Gender.Female)
        {
            return !isPregnant && (Time.time - lastBirthTime) >= femaleReproductionCooldown;
        }
        else
        {
            return (Time.time - lastImpregnationTime) >= maleReproductionCooldown;
        }
    }

    private Transform FindMateForReproduction()
    {
        float range = detectionRadius * mateDetectionRadiusMultiplier;
        Transform nearest = null;
        float minDist = float.MaxValue;
        Collider[] hits = Physics.OverlapSphere(transform.position, range, discoverableLayer);

        if (traitIds.Contains(33) && firstMate != null)
        {
            if (firstMate.currentState == State.Dead)
            {
                return null;
            }

            if (Vector3.Distance(transform.position, firstMate.transform.position) <= range &&
                IsNavigable(firstMate.transform.position))
            {
                CreatureBehavior other = firstMate;
                if (other.gender != gender && 
                    other.currentAge >= other.adultAge && 
                    other.foodLevel >= other.MaxFoodLevel * 0.75f &&
                    (other.gender == Gender.Female ? (!other.isPregnant && (Time.time - other.lastBirthTime) >= other.femaleReproductionCooldown) : 
                                                    (Time.time - other.lastImpregnationTime) >= other.maleReproductionCooldown))
                {
                    nearest = firstMate.transform;
                    Debug.Log($"{name}: Found mate {nearest.name} (Loyal trait)");
                }
            }
            return nearest;
        }

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Creature")) continue;

            CreatureBehavior other = hit.GetComponent<CreatureBehavior>();
            if (other == null || other == this || other.currentState == State.Dead) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (other.gender != gender && 
                other.currentAge >= other.adultAge && 
                other.foodLevel >= other.MaxFoodLevel * 0.75f && 
                dist < minDist && 
                IsNavigable(other.transform.position) &&
                (other.gender == Gender.Female ? (!other.isPregnant && (Time.time - other.lastBirthTime) >= other.femaleReproductionCooldown) : 
                                                (Time.time - other.lastImpregnationTime) >= other.maleReproductionCooldown))
            {
                nearest = other.transform;
                minDist = dist;
            }
        }

        if (nearest != null)
        {
            Debug.Log($"{name}: Found mate {nearest.name}");
        }

        return nearest;
    }

    private void SeekMate()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        if (reproductionTarget == null || reproductionTarget.GetComponent<CreatureBehavior>()?.currentState == State.Dead)
        {
            ChangeState(State.Idle);
            reproductionTarget = null;
            return;
        }

        agent.SetDestination(reproductionTarget.position);
        float distanceToMate = Vector3.Distance(transform.position, reproductionTarget.position);

        if (distanceToMate <= reproductionDistance)
        {
            CreatureBehavior mate = reproductionTarget.GetComponent<CreatureBehavior>();
            if (mate != null)
            {
                if (traitIds.Contains(33) && firstMate == null)
                {
                    firstMate = mate;
                    Debug.Log($"{name}: Set {mate.name} as first mate (Loyal trait)");
                }
                if (mate.traitIds.Contains(33) && mate.firstMate == null)
                {
                    mate.firstMate = this;
                    Debug.Log($"{mate.name}: Set {name} as first mate (Loyal trait)");
                }

                if (gender == Gender.Female)
                {
                    isPregnant = true;
                    pregnantWith = mate;
                    lastImpregnationTime = Time.time;
                    mate.lastImpregnationTime = Time.time;
                    Debug.Log($"{name} is now pregnant with {mate.name}'s child");
                }
                else
                {
                    mate.isPregnant = true;
                    mate.pregnantWith = this;
                    lastImpregnationTime = Time.time;
                    mate.lastImpregnationTime = Time.time;
                    Debug.Log($"{mate.name} is now pregnant with {name}'s child");
                }

                ChangeState(State.Idle);
                reproductionTarget = null;
            }
        }
    }

    private Transform FindHuntingTarget()
    {
        float range = detectionRadius;
        Transform nearest = null;
        float minDist = float.MaxValue;
        Collider[] hits = Physics.OverlapSphere(transform.position, range, discoverableLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Creature")) continue;

            CreatureBehavior other = hit.GetComponent<CreatureBehavior>();
            if (other == null || other == this || other.currentState == State.Dead) continue;

            if (other.typeColor == typeColor) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < minDist && IsNavigable(other.transform.position))
            {
                nearest = other.transform;
                minDist = dist;
            }
        }

        return nearest;
    }

    private void PanicWander()
    {
        noFoodTimer += Time.deltaTime;
        if (noFoodTimer >= 10f)
        {
            Wander();
            noFoodTimer = 0f;
        }
    }

    private void UpdateDiscoverablesDetection()
    {
        visibleDiscoverables.Clear();
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, discoverableLayer);
        foreach (var hit in hits)
        {
            if (hit.transform != transform)
            {
                visibleDiscoverables.Add(hit.transform);
            }
        }
    }

    private bool IsNavigable(Vector3 position)
    {
        return NavMesh.CalculatePath(transform.position, position, agent.areaMask, new NavMeshPath());
    }

    private FoodType GetFoodTypeFromTag(string tag)
    {
        switch (tag)
        {
            case "Apple": return FoodType.Apple;
            case "Berry": return FoodType.Berry;
            case "Meat": return FoodType.Meat;
            default: return FoodType.Apple;
        }
    }

    private string GetTagFromFoodType(FoodType type)
    {
        switch (type)
        {
            case FoodType.Apple: return "Apple";
            case FoodType.Berry: return "Berry";
            case FoodType.Meat: return "Meat";
            default: return null;
        }
    }

    private void BirthBaby()
    {
        if (!isPregnant || pregnantWith == null) return;

        Vector3 spawnPosition = transform.position + UnityEngine.Random.insideUnitSphere * 2f;
        spawnPosition.y = transform.position.y;

        owningBase.SpawnChild(this, pregnantWith, spawnPosition);

        float randomValue = UnityEngine.Random.value;
        if (randomValue < tripletChance)
        {
            for (int i = 0; i < 2; i++)
            {
                spawnPosition = transform.position + UnityEngine.Random.insideUnitSphere * 2f;
                spawnPosition.y = transform.position.y;
                owningBase.SpawnChild(this, pregnantWith, spawnPosition);
            }
            Debug.Log($"{name}: Gave birth to triplets!");
        }
        else if (randomValue < twinChance)
        {
            spawnPosition = transform.position + UnityEngine.Random.insideUnitSphere * 2f;
            spawnPosition.y = transform.position.y;
            owningBase.SpawnChild(this, pregnantWith, spawnPosition);
            Debug.Log($"{name}: Gave birth to twins!");
        }
        else
        {
            Debug.Log($"{name}: Gave birth to a single child");
        }

        isPregnant = false;
        pregnantWith = null;
        totalFoodLostSincePregnant = 0;
    }

    public IEnumerator DieWithRotation()
    {
        currentState = State.Dead;
        agent.isStopped = true;
        agent.enabled = false;

        FoodSource foodSource = GetComponent<FoodSource>();
        if (foodSource != null)
        {
            foodSource.enabled = true;
            foodSource.currentFood = Mathf.Max(1, (int)MaxFoodLevel / 2);
            foodSource.maxFood = foodSource.currentFood;
        }

        float rotation = 0f;
        while (rotation < 90f)
        {
            rotation += Time.deltaTime * 90f;
            transform.rotation = Quaternion.Euler(rotation, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
            yield return null;
        }

        if (owningBase != null)
        {
            owningBase.RemoveCreature(this);
        }

        yield return new WaitForSeconds(30f);
        Destroy(gameObject);
    }

    private void SetupTextDisplay()
    {
        textDisplay = GetComponentInChildren<TextMeshPro>();
        if (textDisplay == null)
        {
            Debug.LogWarning($"{name}: No TextMeshPro component found!");
            return;
        }
        textDisplay.text = name;
    }

    public void UpdateTextDisplay()
    {
        if (textDisplay == null) return;

        string displayText = $"{name}\n" +
                             $"State: {currentState}\n" +
                             $"Health: {Mathf.Round(Health)}/{Mathf.Round(baseHealth * healthMultiplier)}\n" +
                             $"Food: {foodLevel}/{MaxFoodLevel}\n" +
                             $"Age: {Mathf.Round(currentAge)}/{Mathf.Round(maxAge)}\n" +
                             $"Defense: {Mathf.Round(Defense)}";

        if (currentCarriedFood > 0)
        {
            displayText += $"\nCarrying: {currentCarriedFood}/{maxCarryLimit}";
        }

        textDisplay.text = displayText;

        if (poisonIcon != null) poisonIcon.SetActive(poisoned);
        if (pregnantIcon != null) pregnantIcon.SetActive(isPregnant);
        if (hungryIcon != null) hungryIcon.SetActive(foodLevel <= MaxFoodLevel * 0.4f);
        if (panickingIcon != null) panickingIcon.SetActive(noFoodTimer > 0);
    }

    public void ChangeState(State newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            Debug.Log($"{name}: State changed to {currentState}");
        }
    }

    private IEnumerator ResumeAgent()
    {
        yield return new WaitForSeconds(0.1f);
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        CreatureBehavior otherCreature = other.GetComponent<CreatureBehavior>();
        if (otherCreature != null)
        {
            overlappingCreatures.Add(otherCreature);
        }
    }

    void OnTriggerExit(Collider other)
    {
        CreatureBehavior otherCreature = other.GetComponent<CreatureBehavior>();
        if (otherCreature != null)
        {
            overlappingCreatures.Remove(otherCreature);
        }
    }
}