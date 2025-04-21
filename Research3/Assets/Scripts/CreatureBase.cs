using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class CreatureBase : MonoBehaviour
{
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private int maxTraitPoints = 8;
    [SerializeField] private int initialCreatureCount = 5; // Exactly 5 creatures at the start
    [SerializeField] private float spawnRadius = 20f;

    [SerializeField] private List<CreatureBehavior> spawnedCreatures = new List<CreatureBehavior>(); // Visible in Inspector
    [SerializeField] private int population = 0; // Tracks current living creatures
    private List<int> selectedTraitIds = new List<int>();
    private Color creatureTypeColor = Color.white;
    private int maleSpawnCount = 0; // Tracks total males spawned
    private int femaleSpawnCount = 0; // Tracks total females spawned
    private bool lastSpawnedWasMale = false; // Tracks the last gender for alternating initial spawns

    // List of 30 unique secondary names (no overlap with trait names like Loyal, Giant, Gluttonous, etc.)
    private static readonly string[] secondaryNames = new string[]
    {
        "Rascal", "Arachnid", "Boy", "Viper", "Wanderer", "Scout", "Drifter", "Nomad", "Prowler", "Stalker",
        "Gazer", "Sentry", "Rogue", "Bandit", "Maverick", "Outlaw", "Rebel", "Voyager", "Seeker", "Explorer",
        "Strider", "Runner", "Chaser", "Dasher", "Sprinter", "Hopper", "Leaper", "Glider", "Skimmer", "Diver",
        "Lurker", "Warden", "Trickster", "Sniper", "Fixer", "Watcher", "Tamer", "Slinger", "Vulture", "Brawler",
        "Judge", "Agent", "Maven", "Seeker", "Operator", "Rebel", "Planner", "Charmer", "Deceiver", "Pretender"
    };

    private string secondaryName; // The chosen secondary name for this CreatureBase's creatures

    void Start()
    {
        // Select a secondary name for all creatures of this type
        secondaryName = secondaryNames[Random.Range(0, secondaryNames.Length)];

        SelectCreatureTraits();
        SetCreatureTypeColor();
        SpawnInitialCreatures();
    }

    private void SelectCreatureTraits()
    {
        int[] allTraitIds = TraitManager.GetAllTraitIds();
        int pointsUsed = 0;

        while (pointsUsed < maxTraitPoints)
        {
            int remainingPoints = maxTraitPoints - pointsUsed;
            var affordableTraits = allTraitIds
                .Where(id => TraitManager.GetTraitCost(id) <= remainingPoints && !selectedTraitIds.Contains(id))
                .ToArray();

            if (affordableTraits.Length == 0) break;

            int randomTraitId = affordableTraits[Random.Range(0, affordableTraits.Length)];
            selectedTraitIds.Add(randomTraitId);
            pointsUsed += TraitManager.GetTraitCost(randomTraitId);
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
        int creatureCount = initialCreatureCount; // Always spawn exactly 5 creatures
        for (int i = 0; i < creatureCount; i++)
        {
            // Alternate genders: start with male, then female, then male, etc.
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
            creatureBehavior.baseHungerDecreaseInterval = 40f; // 40 seconds as per previous update

            creatureBehavior.currentAge = 10f; // Start as adult
            creatureBehavior.traitIds = new List<int>(selectedTraitIds);
            creatureBehavior.typeColor = creatureTypeColor;
            creatureBehavior.owningBase = this;

            creatureBehavior.gender = gender;
            int spawnNumber = IncrementSpawnCounter(creatureBehavior.gender);

            string creatureName = GenerateCreatureName(spawnNumber, creatureBehavior.gender);
            creatureObj.name = creatureName;

            spawnedCreatures.Add(creatureBehavior);
            population = spawnedCreatures.Count; // Update population counter

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

            childBehavior.currentAge = 0f; // Start as baby
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
            population = spawnedCreatures.Count; // Update population counter

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
        // Get the prefix from TraitManager (e.g., "Loyal", "Giant", "Gluttonous")
        string prefix = "";
        foreach (int traitId in selectedTraitIds)
        {
            prefix = TraitManager.GetTraitPrefix(traitId);
            if (!string.IsNullOrEmpty(prefix))
            {
                break;
            }
        }

        // Use the secondary name chosen for this CreatureBase
        string baseName = prefix.Length > 0 ? $"{prefix} {secondaryName}" : $"Creature {secondaryName}";

        // Add the gender marker and spawn number (e.g., "M2" or "F1")
        string genderMarker = gender == CreatureBehavior.Gender.Male ? "M" : "F";
        return $"{baseName} ({genderMarker}{spawnNumber})";
    }

    public void AddCreature(CreatureBehavior creature)
    {
        if (!spawnedCreatures.Contains(creature))
        {
            spawnedCreatures.Add(creature);
            population = spawnedCreatures.Count; // Update population counter
        }
    }

    public void RemoveCreature(CreatureBehavior creature)
    {
        spawnedCreatures.Remove(creature);
        population = spawnedCreatures.Count; // Update population counter
    }
}