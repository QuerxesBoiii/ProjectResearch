using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class CreatureBase : MonoBehaviour
{
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private int maxTraitPoints = 8;
    [SerializeField] private int initialCreatureCount = 5;
    [SerializeField] private float spawnRadius = 20f;

    [SerializeField] private List<CreatureBehavior> spawnedCreatures = new List<CreatureBehavior>();
    [SerializeField] private int population = 0;
    private List<int> selectedTraitIds = new List<int>();
    private Color creatureTypeColor = Color.white;
    private int maleSpawnCount = 0;
    private int femaleSpawnCount = 0;
    private bool lastSpawnedWasMale = false;

    private static readonly string[] secondaryNames = new string[]
    {
        "Rascal", "Arachnid", "Boy", "Viper", "Wanderer", "Scout", "Drifter", "Nomad", "Prowler", "Stalker",
        "Gazer", "Sentry", "Rogue", "Bandit", "Maverick", "Outlaw", "Rebel", "Voyager", "Seeker", "Explorer",
        "Strider", "Runner", "Chaser", "Dasher", "Sprinter", "Hopper", "Leaper", "Glider", "Skimmer", "Diver",
        "Lurker", "Warden", "Trickster", "Sniper", "Fixer", "Watcher", "Tamer", "Slinger", "Vulture", "Brawler",
        "Judge", "Agent", "Maven", "Seeker", "Operator", "Rebel", "Planner", "Charmer", "Deceiver", "Pretender"
    };

    private string secondaryName;
    private CreatureBehavior leader;
    [SerializeField] private float currentFood = 0f;
    [SerializeField] private float maxFoodCapacity = 0f;
    private bool isDepositing = false;
    private bool isEatingFromBase = false;
    private bool isMovingBase = false;
    private CreatureBehavior creatureMovingBase;
    private Vector3 defaultScale;

    public CreatureBehavior Leader => leader;
    public float CurrentFood => currentFood;
    public float MaxFoodCapacity => maxFoodCapacity;
    public List<CreatureBehavior> Creatures => spawnedCreatures;

    private BoxCollider triggerCollider; // Reference to the trigger collider

    void Start()
    {
        defaultScale = transform.localScale;
        secondaryName = secondaryNames[Random.Range(0, secondaryNames.Length)];
        triggerCollider = GetComponent<BoxCollider>(); // Get the trigger collider
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: No trigger BoxCollider found or it's not set as trigger!");
        }
        SelectCreatureTraits();
        SetCreatureTypeColor();
        SpawnInitialCreatures();
        SelectLeader();
        UpdateFoodCapacity();
    }

    void Update()
    {
        UpdateBaseSize();
        if (leader != null && !isMovingBase)
        {
            float distanceToLeader = Vector3.Distance(transform.position, leader.transform.position);
            if (distanceToLeader > 2 * spawnRadius)
            {
                AssignBaseMoveJob();
            }
        }
    }

    private void SelectCreatureTraits()
    {
        // Select General Traits
        int[] allGeneralTraitIds = TraitManager.GetAllGeneralTraitIds();
        int pointsUsed = 0;

        while (pointsUsed < maxTraitPoints)
        {
            int remainingPoints = maxTraitPoints - pointsUsed;
            var affordableTraits = allGeneralTraitIds
                .Where(id => TraitManager.GetTraitCost(id) <= remainingPoints && !selectedTraitIds.Contains(id))
                .ToArray();

            if (affordableTraits.Length == 0) break;

            int randomTraitId = affordableTraits[Random.Range(0, affordableTraits.Length)];
            selectedTraitIds.Add(randomTraitId);
            pointsUsed += TraitManager.GetTraitCost(randomTraitId);
        }

        // Select exactly one Social Trait
        int[] allSocialTraitIds = TraitManager.GetAllSocialTraitIds();
        if (allSocialTraitIds.Length > 0)
        {
            int randomSocialTraitId = allSocialTraitIds[Random.Range(0, allSocialTraitIds.Length)];
            selectedTraitIds.Add(randomSocialTraitId);
        }

        if (selectedTraitIds.Count == 0)
        {
            Debug.LogWarning($"{name}: No traits selected, using default configuration.");
        }
        else
        {
            string traitNames = string.Join(", ", selectedTraitIds.Select(id => TraitManager.GetTraitName(id)));
            Debug.Log($"{name}: Creature type defined with traits: {traitNames} (Total cost: {pointsUsed})");
        }
    }

    private void SetCreatureTypeColor()
    {
        if (selectedTraitIds.Count == 0)
        {
            creatureTypeColor = Color.white;
            Debug.Log($"{name}: No traits selected, using default white color.");
            return;
        }

        float r = 0f, g = 0f, b = 0f;
        int count = selectedTraitIds.Count;

        foreach (int traitId in selectedTraitIds)
        {
            string hex = TraitManager.GetTraitHexColor(traitId);
            if (ColorUtility.TryParseHtmlString(hex, out Color traitColor))
            {
                r += traitColor.r;
                g += traitColor.g;
                b += traitColor.b;
            }
            else
            {
                Debug.LogWarning($"{name}: Invalid hex color '{hex}' for trait ID {traitId}. Using white.");
            }
        }

        creatureTypeColor = new Color(r / count, g / count, b / count);
        Debug.Log($"{name}: Creature type color set to {creatureTypeColor}");
    }

    private void SpawnInitialCreatures()
    {
        int creatureCount = initialCreatureCount;
        for (int i = 0; i < creatureCount; i++)
        {
            CreatureBehavior.Gender gender = lastSpawnedWasMale ? CreatureBehavior.Gender.Female : CreatureBehavior.Gender.Male;
            SpawnCreature(gender);
            lastSpawnedWasMale = !lastSpawnedWasMale;
        }
        Debug.Log($"{name}: Spawned {creatureCount} initial creatures.");
    }

    public void SpawnCreature(CreatureBehavior.Gender gender)
    {
        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * spawnRadius;
        spawnPosition.y = transform.position.y;

        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out UnityEngine.AI.NavMeshHit hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            GameObject creatureObj = Instantiate(creaturePrefab, hit.position, Quaternion.identity);
            CreatureBehavior creatureBehavior = creatureObj.GetComponent<CreatureBehavior>();

            creatureBehavior.baseSize = 1f;
            creatureBehavior.baseWalkingSpeed = 4f;
            creatureBehavior.baseHealth = 20f;
            creatureBehavior.baseMaxFoodLevel = 10f;
            creatureBehavior.baseHungerDecreaseInterval = 40f;

            creatureBehavior.currentAge = 10f;
            creatureBehavior.traitIds = new List<int>(selectedTraitIds);
            creatureBehavior.typeColor = creatureTypeColor;
            creatureBehavior.owningBase = this;

            creatureBehavior.gender = gender;
            int spawnNumber = IncrementSpawnCounter(creatureBehavior.gender);

            string creatureName = GenerateCreatureName(spawnNumber, creatureBehavior.gender);
            creatureObj.name = creatureName;

            spawnedCreatures.Add(creatureBehavior);
            population = spawnedCreatures.Count;
            UpdateFoodCapacity();

            Debug.Log($"{name}: Spawned creature '{creatureObj.name}' at {hit.position} with traits: {string.Join(", ", creatureBehavior.traitIds.Select(id => TraitManager.GetTraitName(id)))}");
        }
        else
        {
            Debug.LogWarning($"{name}: Failed to find valid NavMesh position for creature spawn.");
        }
    }

    public void SpawnChild(CreatureBehavior mother, CreatureBehavior father, Vector3 spawnPosition)
    {
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            GameObject childObj = Instantiate(creaturePrefab, hit.position, Quaternion.identity);
            CreatureBehavior childBehavior = childObj.GetComponent<CreatureBehavior>();

            childBehavior.baseSize = mother.baseSize;
            childBehavior.baseWalkingSpeed = mother.baseWalkingSpeed;
            childBehavior.baseHealth = mother.baseHealth;
            childBehavior.baseMaxFoodLevel = mother.baseMaxFoodLevel;
            childBehavior.baseHungerDecreaseInterval = mother.baseHungerDecreaseInterval;

            childBehavior.currentAge = 0f;
            childBehavior.traitIds = new List<int>(mother.traitIds);
            childBehavior.typeColor = mother.typeColor;
            childBehavior.owningBase = this;
            childBehavior.mother = mother;
            childBehavior.father = father;

            childBehavior.gender = Random.value < 0.5f ? CreatureBehavior.Gender.Male : CreatureBehavior.Gender.Female;
            int spawnNumber = IncrementSpawnCounter(childBehavior.gender);

            childBehavior.isPregnant = false;
            childBehavior.totalFoodLostSincePregnant = 0;
            childBehavior.lastImpregnationTime = -1000f;
            childBehavior.lastBirthTime = -1000f;
            childBehavior.pregnantWith = null;

            string childName = GenerateCreatureName(spawnNumber, childBehavior.gender);
            childObj.name = childName;

            spawnedCreatures.Add(childBehavior);
            population = spawnedCreatures.Count;
            UpdateFoodCapacity();

            Debug.Log($"Child born: {childObj.name}, Gender: {childBehavior.gender}, Size: {childBehavior.Size}, Color: {childBehavior.typeColor}, Mother: {mother.name}, Father: {father.name}");
        }
        else
        {
            Debug.LogWarning($"{name}: Failed to find valid NavMesh position for child spawn at {spawnPosition}.");
        }
    }

    private int IncrementSpawnCounter(CreatureBehavior.Gender gender)
    {
        if (gender == CreatureBehavior.Gender.Male)
        {
            maleSpawnCount++;
            return maleSpawnCount;
        }
        else
        {
            femaleSpawnCount++;
            return femaleSpawnCount;
        }
    }

    private string GenerateCreatureName(int spawnNumber, CreatureBehavior.Gender gender)
    {
        string prefix = "";
        foreach (int traitId in selectedTraitIds)
        {
            prefix = TraitManager.GetTraitPrefix(traitId);
            if (!string.IsNullOrEmpty(prefix))
            {
                break;
            }
        }

        string baseName = prefix.Length > 0 ? $"{prefix} {secondaryName}" : $"Creature {secondaryName}";
        string genderMarker = gender == CreatureBehavior.Gender.Male ? "M" : "F";
        return $"{baseName} ({genderMarker}{spawnNumber})";
    }

    public void AddCreature(CreatureBehavior creature)
    {
        if (!spawnedCreatures.Contains(creature))
        {
            spawnedCreatures.Add(creature);
            population = spawnedCreatures.Count;
            UpdateFoodCapacity();
            if (leader == null || creature.currentAge > leader.currentAge)
            {
                SelectLeader();
            }
        }
    }

    public void RemoveCreature(CreatureBehavior creature)
    {
        spawnedCreatures.Remove(creature);
        population = spawnedCreatures.Count;
        UpdateFoodCapacity();
        if (creature == leader)
        {
            SelectLeader();
        }
        if (creature == creatureMovingBase)
        {
            isMovingBase = false;
            creatureMovingBase = null;
        }
    }

    private void SelectLeader()
    {
        if (spawnedCreatures.Count == 0)
        {
            leader = null;
            return;
        }

        leader = spawnedCreatures.OrderByDescending(c => c.currentAge).First();
        Debug.Log($"{name}: New leader selected: {leader.name} (Age: {leader.currentAge})");
    }

    private void UpdateFoodCapacity()
    {
        maxFoodCapacity = population * 5f;
    }

    public bool DepositFood(float amount, CreatureBehavior creature)
    {
        if (isDepositing || currentFood >= maxFoodCapacity) return false;

        isDepositing = true;
        float spaceAvailable = maxFoodCapacity - currentFood;
        float amountToDeposit = Mathf.Min(amount, spaceAvailable);
        currentFood += amountToDeposit;
        Debug.Log($"{creature.name}: Deposited {amountToDeposit} food at {name}. Total food: {currentFood}/{maxFoodCapacity}");
        isDepositing = false;
        return amountToDeposit == amount;
    }

    public bool EatFromBase(float amount, CreatureBehavior creature)
    {
        if (isEatingFromBase || currentFood < amount) return false;

        isEatingFromBase = true;
        currentFood -= amount;
        Debug.Log($"{creature.name}: Ate {amount} food from {name}. Total food: {currentFood}/{maxFoodCapacity}");
        isEatingFromBase = false;
        return true;
    }

    private void UpdateBaseSize()
    {
        if (maxFoodCapacity == 0)
        {
            transform.localScale = defaultScale * 0.5f;
            return;
        }

        float foodRatio = currentFood / maxFoodCapacity;
        float scaleMultiplier = Mathf.Lerp(0.3f, 2f, foodRatio);
        transform.localScale = defaultScale * scaleMultiplier + Vector3.one * (population * 0.1f);

        if (transform.localScale.magnitude >= 8f)
        {
            transform.localScale = Vector3.one * 8f;
        }
    }

    private void AssignBaseMoveJob()
    {
        if (isMovingBase || leader == null) return;

        CreatureBehavior worker = spawnedCreatures.FirstOrDefault(c => c != leader && c.currentState != CreatureBehavior.State.Dead);
        if (worker != null)
        {
            isMovingBase = true;
            creatureMovingBase = worker;
            worker.AssignMoveBaseJob();
            Debug.Log($"{worker.name}: Assigned to move {name} to leader's position.");
        }
    }

    public void CompleteBaseMove(CreatureBehavior creature)
    {
        if (creature == creatureMovingBase)
        {
            isMovingBase = false;
            creatureMovingBase = null;
            Debug.Log($"{creature.name}: Completed moving {name} to leader's position.");
        }
    }

    // Check if a creature is within the trigger collider
    public bool IsCreatureInTrigger(CreatureBehavior creature)
    {
        if (triggerCollider == null || !triggerCollider.isTrigger) return false;

        Vector3 creaturePos = creature.transform.position;
        Vector3 closestPoint = triggerCollider.ClosestPoint(creaturePos);
        return Vector3.Distance(creaturePos, closestPoint) < 0.01f; // Creature is inside or on the collider
    }

    // Get a random deposition point within the trigger collider
    public Vector3 GetRandomDepositionPoint()
    {
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            return transform.position; // Fallback to center if no collider
        }

        Bounds bounds = triggerCollider.bounds;
        Vector3 randomPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            transform.position.y, // Keep y at base level
            Random.Range(bounds.min.z, bounds.max.z)
        );

        // Ensure the point is on the NavMesh
        if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out UnityEngine.AI.NavMeshHit hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position; // Fallback to center if no valid NavMesh point
    }
}