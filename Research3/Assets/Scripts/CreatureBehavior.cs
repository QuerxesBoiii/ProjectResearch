using UnityEngine; using UnityEngine.AI; using System.Collections; using System.Collections.Generic; using TMPro; using System.Linq; using System;

public class CreatureBehavior : MonoBehaviour { private NavMeshAgent agent; public CreatureCombat creatureCombat { get; private set; } private TextMeshPro textDisplay;

public enum State { Idle, SearchingForFood, Eating, SeekingMate, Attacking, Fleeing, Dead, Work }
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

public float Size => baseSize * sizeMultiplier;
public float Health
{
    get => baseHealth * healthMultiplier;
    set => baseHealth = value / healthMultiplier;
}
private float walkingSpeed => baseWalkingSpeed * walkingSpeedMultiplier * (isPregnant && gender == Gender.Female ? 0.75f : 1f);
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
        if (traitIds.Contains(8)) speed *= 1.5f; // Hunting
        if (traitIds.Contains(19) && Health < baseHealth * healthMultiplier * 0.3f) speed *= 1.5f; // Adrenaline
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
public float DetectionRadius => baseDetectionRadius + (Mathf.Floor(Size - 1f) * (baseDetectionRadius / 5f));
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

[Header("Work Mechanics")]
[SerializeField] private float foodGrabbed = 0f;
[SerializeField] private float foodGrabbedMax;
private FoodSource targetFoodSource;
private bool isMovingBaseJob = false;
[SerializeField] private int workCycles = 0; // Track completed work cycles
[SerializeField] private float workAttemptTimer = 0f; // Timer for work attempts
[SerializeField] private float workCooldownTimer = 0f; // Timer for work cooldown
private const float workAttemptInterval = 20f; // Attempt every 20 seconds
private const float workChance = 0.4f; // 40% chance to attempt work
private const int maxWorkCycles = 3; // Stop after 3 cycles
private const float workCooldown = 30f; // 30-second cooldown after work shift

public float FoodGrabbed => foodGrabbed;

[Header("Combat")]
[SerializeField] public Transform combatTarget;
[SerializeField] private float attackTimer = 0f;
private float attackInterval => traitIds.Contains(23) ? 1f * 1.2f : 1f; // Ambush

[Header("Immobilization")]
[SerializeField] public bool immobilized = false;
[SerializeField] public float immobilizedTimer = 0f;
[SerializeField] public CreatureBehavior lastAttackedBy;

[Header("Base Reference")]
[SerializeField] public CreatureBase owningBase;

[Header("Visuals")]
[SerializeField] public GameObject meshRendererObject;
[SerializeField] public Color typeColor = Color.white;

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
    foodGrabbedMax = Mathf.Ceil(MaxFoodLevel / 2);

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

    int areaMask = WalkableArea | JumpArea;
    if (canClimb) areaMask |= ClimbArea;
    agent.areaMask = areaMask;

    UpdateSizeAndStats();
    foodLevel = Mathf.Ceil(MaxFoodLevel);
    stamina = maxStamina;
    Health = baseHealth * healthMultiplier;
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
    Debug.Log($"{name}: Initialized - Gender: {gender}, Size: {Size}, Speed: {walkingSpeed}, Food: {foodLevel}/{MaxFoodLevel}, Stamina: {stamina}/{maxStamina}, Color: {typeColor}");
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

    if (foodLevel <= MaxFoodLevel * 0.4f && (currentState == State.Idle || currentState == State.Work))
    {
        ChangeState(State.SearchingForFood);
        noFoodTimer = 0f;
        isMovingBaseJob = false;
        workCycles = 0; // Reset work cycles when hungry
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

    if (overlappingCreatures.Count > 0 && Time.time % 1f < 0.02f && unstickCooldown <= 0)
    {
        foreach (var other in overlappingCreatures)
        {
            if (other.currentState == State.Dead) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            float minDist = (Size + other.Size) * 0.5f;
            if (dist < minDist)
            {
                Vector3 pushDir = (transform.position - other.transform.position).normalized;
                Vector3 newPos = transform.position + pushDir * (Size + other.Size) * 0.1f * Time.deltaTime;
                if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, 1f, agent.areaMask))
                {
                    transform.position = hit.position;
                    agent.Warp(hit.position);
                    agent.isStopped = true;
                    StartCoroutine(ResumeAgent());
                    unstickCooldown = 1f;
                }
            }
        }
    }

    unstickCooldown -= deltaTime;

    reservedFoodSource?.UpdateReservations();

    if (currentState == State.Idle && 
        owningBase != null && owningBase.Leader != this && // Leader check
        !(gender == Gender.Female && isPregnant) && // Pregnant female check
        workCooldownTimer <= 0) // Cooldown check
    {
        workAttemptTimer += deltaTime;
        if (workAttemptTimer >= workAttemptInterval)
        {
            workAttemptTimer = 0f;
            if (UnityEngine.Random.value < workChance && 
                owningBase.CurrentFood < owningBase.MaxFoodCapacity * 0.8f && 
                IsFoodSourceInRange())
            {
                ChangeState(State.Work);
                Debug.Log($"{name}: Attempted work and started working (food in range, base food < 80%)");
            }
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
            if (Vector3.Distance(transform.position, combatTarget.position) > DetectionRadius)
            {
                ChangeState(State.Idle);
            }
            break;
        case State.Work:
            Work();
            break;
    }

    UpdateTextDisplay();
}

private bool IsFoodSourceInRange()
{
    foreach (var preferredType in foodPreferences)
    {
        string tag = GetTagFromFoodType(preferredType);
        if (string.IsNullOrEmpty(tag)) continue;

        foreach (var obj in visibleDiscoverables)
        {
            if (obj.CompareTag(tag) || obj.CompareTag("Meat"))
            {
                FoodSource foodSource = obj.GetComponent<FoodSource>();
                if (foodSource != null)
                {
                    int availableFood = foodSource.CurrentFood - foodSource.ReservedFood;
                    if (availableFood >= 1 && IsNavigable(obj.position))
                    {
                        return true;
                    }
                }
            }
        }
    }
    return false;
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

private void UpdateSizeAndStats()
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

    if (owningBase != null && owningBase.Leader != null && this != owningBase.Leader)
    {
        float distanceToLeader = Vector3.Distance(transform.position, owningBase.Leader.transform.position);
        if (distanceToLeader > 2 * owningBase.Leader.DetectionRadius)
        {
            agent.SetDestination(owningBase.Leader.transform.position);
            return;
        }
    }

    wanderTimer += Time.deltaTime;
    if (wanderTimer >= wanderInterval)
    {
        Vector3 destination = transform.position + UnityEngine.Random.insideUnitSphere * wanderRadius;
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
    }

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
        FoodSource closestFoodSource = null;

        foreach (var obj in visibleDiscoverables)
        {
            if (obj.CompareTag(tag) || obj.CompareTag("Meat"))
            {
                float dist = Vector3.Distance(transform.position, obj.position);
                FoodSource foodSource = obj.GetComponent<FoodSource>();
                if (foodSource != null)
                {
                    int availableFood = foodSource.CurrentFood - foodSource.ReservedFood;
                    if (dist < minDist && IsNavigable(obj.position) && availableFood >= 1)
                    {
                        minDist = dist;
                        closest = obj;
                        closestFoodSource = foodSource;
                    }
                }
            }
        }

        if (closest != null)
        {
            closestFoodSource.ReserveFood(this, 30f);
            reservedFoodSource = closestFoodSource;
            agent.SetDestination(closest.position);
            if (minDist <= eatingDistance)
            {
                ChangeState(State.Eating);
                agent.isStopped = true;
                eatTimer = 0f;
                targetFoodSource = closestFoodSource;
                noFoodTimer = 0f;
            }
            return;
        }
    }

    HandleNoFood();
}

private void HandleNoFood()
{
    if (owningBase != null && owningBase.CurrentFood > 0)
    {
        agent.SetDestination(owningBase.GetRandomDepositionPoint());
        if (owningBase.IsCreatureInTrigger(this) || Vector3.Distance(transform.position, owningBase.transform.position) <= eatingDistance)
        {
            if (owningBase.EatFromBase(1f, this))
            {
                foodLevel = Mathf.Ceil(Mathf.Min(foodLevel + 1, MaxFoodLevel));
                ChangeState(State.Idle);
                noFoodTimer = 0f;
            }
        }
        return;
    }

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

    eatTimer += Time.deltaTime;
    if (eatTimer >= 1f)
    {
        if (targetFoodSource != null && targetFoodSource.HasFood && targetFoodSource.CurrentFood > 0 && foodLevel < MaxFoodLevel)
        {
            targetFoodSource.CurrentFood--;
            foodLevel = Mathf.Ceil(Mathf.Min(foodLevel + (int)targetFoodSource.FoodSatiety, MaxFoodLevel));
            reservedFoodSource?.ReleaseReservation(this);
            reservedFoodSource = null;
            UpdateTextDisplay();
        }
        else
        {
            ChangeState(foodLevel >= MaxFoodLevel ? State.Idle : State.SearchingForFood);
            agent.isStopped = false;
            reservedFoodSource?.ReleaseReservation(this);
            reservedFoodSource = null;
            targetFoodSource = null;
            noFoodTimer = 0f;
        }
        eatTimer = 0f;
    }
}

private void Work()
{
    if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

    if (isMovingBaseJob)
    {
        if (owningBase != null && owningBase.Leader != null)
        {
            owningBase.transform.SetParent(transform);
            agent.SetDestination(owningBase.Leader.transform.position);
            if (Vector3.Distance(transform.position, owningBase.Leader.transform.position) <= eatingDistance)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    owningBase.transform.position = transform.position;
                    owningBase.transform.SetParent(null);
                    owningBase.CompleteBaseMove(this);
                    isMovingBaseJob = false;
                    workCooldownTimer = workCooldown; // Start cooldown after moving base
                    ChangeState(State.Idle);
                }
            }
        }
        return;
    }

    if (foodGrabbed > 0 && owningBase != null)
    {
        agent.SetDestination(owningBase.GetRandomDepositionPoint());
        if (owningBase.IsCreatureInTrigger(this) || Vector3.Distance(transform.position, owningBase.transform.position) <= eatingDistance)
        {
            bool depositedAll = owningBase.DepositFood(foodGrabbed, this);
            foodGrabbed = depositedAll ? 0 : foodGrabbed - (owningBase.MaxFoodCapacity - owningBase.CurrentFood);
            workCycles++;
            Debug.Log($"{name}: Completed work cycle {workCycles}/{maxWorkCycles}");
            if (workCycles >= maxWorkCycles)
            {
                workCycles = 0;
                workCooldownTimer = workCooldown;
                ChangeState(State.Idle);
                Debug.Log($"{name}: Completed {maxWorkCycles} work cycles, returning to Idle with {workCooldown}s cooldown");
            }
            else if (owningBase.CurrentFood >= owningBase.MaxFoodCapacity * 0.8f)
            {
                workCycles = 0;
                workCooldownTimer = workCooldown;
                ChangeState(State.Idle);
                Debug.Log($"{name}: Base food >= 80%, returning to Idle with {workCooldown}s cooldown");
            }
            UpdateTextDisplay();
        }
        return;
    }

    if (targetFoodSource == null && foodGrabbed < foodGrabbedMax)
    {
        if (reservedFoodSource != null)
        {
            reservedFoodSource.ReleaseReservation(this);
            reservedFoodSource = null;
        }

        Transform closest = null;
        float minDist = float.MaxValue;
        FoodSource closestFoodSource = null;

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
                        if (dist < minDist && IsNavigable(obj.position) && availableFood >= 1)
                        {
                            minDist = dist;
                            closest = obj;
                            closestFoodSource = foodSource;
                        }
                    }
                }
            }
        }

        if (closest != null)
        {
            closestFoodSource.ReserveFood(this, 30f);
            reservedFoodSource = closestFoodSource;
            targetFoodSource = closestFoodSource;
            agent.SetDestination(closest.position);
        }
        else
        {
            if (owningBase != null && owningBase.CurrentFood > 0)
            {
                agent.SetDestination(owningBase.GetRandomDepositionPoint());
                if (owningBase.IsCreatureInTrigger(this) || Vector3.Distance(transform.position, owningBase.transform.position) <= eatingDistance)
                {
                    if (owningBase.EatFromBase(1f, this))
                    {
                        foodLevel = Mathf.Ceil(Mathf.Min(foodLevel + 1, MaxFoodLevel));
                        workCooldownTimer = workCooldown;
                        ChangeState(State.Idle);
                    }
                }
            }
            else
            {
                workCooldownTimer = workCooldown;
                ChangeState(State.Idle);
            }
            return;
        }
    }

    if (targetFoodSource != null && foodGrabbed < foodGrabbedMax)
    {
        agent.SetDestination(targetFoodSource.transform.position);
        if (Vector3.Distance(transform.position, targetFoodSource.transform.position) <= eatingDistance)
        {
            eatTimer += Time.deltaTime;
            if (eatTimer >= 1f)
            {
                if (targetFoodSource.HasFood && targetFoodSource.CurrentFood > 0)
                {
                    float potentialFood = foodGrabbed + targetFoodSource.FoodSatiety;
                    if (potentialFood <= foodGrabbedMax)
                    {
                        targetFoodSource.CurrentFood--;
                        foodGrabbed += targetFoodSource.FoodSatiety;
                        Debug.Log($"{name}: Grabbed {targetFoodSource.FoodSatiety} food. Total grabbed: {foodGrabbed}/{foodGrabbedMax}");
                    }
                    else
                    {
                        float remainingCapacity = foodGrabbedMax - foodGrabbed;
                        if (remainingCapacity > 0)
                        {
                            targetFoodSource.CurrentFood--;
                            foodGrabbed += remainingCapacity;
                            Debug.Log($"{name}: Grabbed {remainingCapacity} food (partial). Total grabbed: {foodGrabbed}/{foodGrabbedMax}");
                        }
                    }
                    UpdateTextDisplay();
                }
                else
                {
                    reservedFoodSource?.ReleaseReservation(this);
                    reservedFoodSource = null;
                    targetFoodSource = null;
                }
                eatTimer = 0f;
            }
        }
    }
}

public void AssignMoveBaseJob()
{
    isMovingBaseJob = true;
    ChangeState(State.Work);
    if (reservedFoodSource != null)
    {
        reservedFoodSource.ReleaseReservation(this);
        reservedFoodSource = null;
        targetFoodSource = null;
    }
}

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
    float range = DetectionRadius * mateDetectionRadiusMultiplier;
    Transform nearest = null;
    float minDist = float.MaxValue;
    Collider[] hits = Physics.OverlapSphere(transform.position, range, discoverableLayer);

    foreach (var hit in hits)
    {
        if (hit.transform == transform) continue;
        if (hit.CompareTag("Creature"))
        {
            CreatureBehavior other = hit.GetComponent<CreatureBehavior>();
            if (other != null && other.gender != gender && other.currentAge >= other.adultAge && other.foodLevel >= other.MaxFoodLevel * 0.75f)
            {
                if (gender == Gender.Female && other.gender == Gender.Male)
                {
                    if ((Time.time - other.lastImpregnationTime) >= other.maleReproductionCooldown)
                    {
                        float dist = Vector3.Distance(transform.position, hit.transform.position);
                        if (dist < minDist && IsNavigable(hit.transform.position))
                        {
                            minDist = dist;
                            nearest = hit.transform;
                        }
                    }
                }
                else if (gender == Gender.Male && other.gender == Gender.Female)
                {
                    if (!other.isPregnant && (Time.time - other.lastBirthTime) >= other.femaleReproductionCooldown)
                    {
                        float dist = Vector3.Distance(transform.position, hit.transform.position);
                        if (dist < minDist && IsNavigable(hit.transform.position))
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
            ChangeState(State.Idle);
        }
    }
    else
    {
        ChangeState(State.Idle);
    }
}

private void AttemptReproduction()
{
    if (reproductionTarget == null) return;
    CreatureBehavior partner = reproductionTarget.GetComponent<CreatureBehavior>();
    if (partner == null || partner.gender == gender) return;

    if (Health < (baseHealth * healthMultiplier) * 0.5f || 
        partner.Health < (partner.baseHealth * partner.healthMultiplier) * 0.5f)
    {
        reproductionTarget = null;
        ChangeState(State.Idle);
        return;
    }

    if (gender == Gender.Female && partner.gender == Gender.Male)
    {
        if (!isPregnant && foodLevel >= MaxFoodLevel * 0.75f && (Time.time - lastBirthTime) >= femaleReproductionCooldown &&
            (Time.time - partner.lastImpregnationTime) >= partner.maleReproductionCooldown && partner.foodLevel >= partner.MaxFoodLevel * 0.75f)
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
        if (!partner.isPregnant && partner.foodLevel >= partner.MaxFoodLevel * 0.75f && (Time.time - partner.lastBirthTime) >= partner.femaleReproductionCooldown &&
            (Time.time - lastImpregnationTime) >= maleReproductionCooldown && foodLevel >= MaxFoodLevel * 0.75f)
        {
            partner.isPregnant = true;
            partner.totalFoodLostSincePregnant = 0;
            lastImpregnationTime = Time.time;
            partner.pregnantWith = this;
            Debug.Log($"{partner.name} (Female) is now pregnant with {name}'s child");
        }
    }
    reproductionTarget = null;
    ChangeState(State.Idle);
}

private void BirthBaby()
{
    if (pregnantWith == null) return;
    owningBase.SpawnChild(this, pregnantWith, transform.position + Vector3.right * 2f);
    bool hasTwins = UnityEngine.Random.value < twinChance;
    bool hasTriplets = UnityEngine.Random.value < tripletChance;

    if (hasTriplets)
    {
        owningBase.SpawnChild(this, pregnantWith, transform.position + Vector3.right * 2f);
        owningBase.SpawnChild(this, pregnantWith, transform.position + Vector3.right * 2f);
        Debug.Log($"{name} gave birth to triplets!");
    }
    else if (hasTwins)
    {
        owningBase.SpawnChild(this, pregnantWith, transform.position + Vector3.right * 2f);
        Debug.Log($"{name} gave birth to twins!");
    }

    isPregnant = false;
    totalFoodLostSincePregnant = 0;
    pregnantWith = null;
    lastBirthTime = Time.time;
    Debug.Log($"{name} gave birth");
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
                if (other.traitIds.SequenceEqual(traitIds)) continue;
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
    Collider[] hits = Physics.OverlapSphere(transform.position, DetectionRadius, discoverableLayer);
    visibleDiscoverables.Clear();
    foreach (var hit in hits)
    {
        if (hit.transform == transform) continue;

        string tag = hit.transform.tag;
        if (tag == "Creature" || tag == "Apple" || tag == "Berry" || tag == "Meat")
        {
            visibleDiscoverables.Add(hit.transform);
        }
    }
}

public float GetFoodDetectionRadius()
{
    return DetectionRadius * (traitIds.Contains(28) ? 1.25f : 1f); // Keen
}

public IEnumerator DieWithRotation()
{
    reservedFoodSource?.ReleaseReservation(this);
    reservedFoodSource = null;
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

    SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
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

    float maxHealth = baseHealth * healthMultiplier;
    string text = $"Food: {foodLevel:F0}/{MaxFoodLevel:F0}\nHP: {(currentState == State.Dead ? 0 : Mathf.CeilToInt(Health))}/{maxHealth:F0}\nStamina: {stamina:F0}/{maxStamina:F0}";
    if (gender == Gender.Female && isPregnant)
        text += $"\nPregnant: {totalFoodLostSincePregnant / 2}/{ReproductionCost}";
    if (foodGrabbed > 0)
        text += $"\nCarrying Food: {foodGrabbed}/{foodGrabbedMax}";
    if (currentState == State.Work)
        text += $"\nWork Cycles: {workCycles}/{maxWorkCycles}";
    text += $"\n{gender}";
    textDisplay.text = text;

    if (poisonIcon != null)
        poisonIcon.SetActive(poisoned);
    if (pregnantIcon != null)
        pregnantIcon.SetActive(isPregnant);
    if (hungryIcon != null)
        hungryIcon.SetActive(foodLevel <= MaxFoodLevel * 0.4f);
    if (panickingIcon != null)
        panickingIcon.SetActive(currentState == State.Fleeing);
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
    Gizmos.DrawWireSphere(transform.position, DetectionRadius);
}

void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Creature"))
    {
        CreatureBehavior otherCreature = other.GetComponent<CreatureBehavior>();
        if (otherCreature != null && otherCreature != this)
        {
            overlappingCreatures.Add(otherCreature);
        }
    }
}

void OnTriggerExit(Collider other)
{
    if (other.CompareTag("Creature"))
    {
        CreatureBehavior otherCreature = other.GetComponent<CreatureBehavior>();
        if (otherCreature != null)
        {
            overlappingCreatures.Remove(otherCreature);
        }
    }
}

private IEnumerator ResumeAgent()
{
    yield return new WaitForSeconds(0.5f);
    if (agent.isActiveAndEnabled && agent.isOnNavMesh)
    {
        agent.isStopped = false;
    }
}

private void ChangeState(State newState)
{
    if (currentState == newState) return;

    if ((currentState == State.SearchingForFood || currentState == State.Eating || currentState == State.Work) &&
        (newState != State.SearchingForFood && newState != State.Eating && newState != State.Work))
    {
        reservedFoodSource?.ReleaseReservation(this);
        reservedFoodSource = null;
        targetFoodSource = null;
    }

    if (newState != State.Work)
    {
        isMovingBaseJob = false;
    }

    currentState = newState;
}


}