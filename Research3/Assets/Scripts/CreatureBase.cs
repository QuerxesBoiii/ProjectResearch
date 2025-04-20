using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CreatureBase : MonoBehaviour
{
    public List<CreatureBehavior> livingCreatures = new List<CreatureBehavior>();
    private int typeSpawnCount = 0; // Tracks spawn count for this creature type
    private List<int> creatureTypeTraits = new List<int>(); // Fixed traits for this base's creature type
    private string typeNameBase = ""; // Base name for this type (e.g., "Prolific Rascal")
    private List<string> randomNames = new List<string>
    {
        "Rascal", "Scout", "Wanderer", "Sprout", "Nibbler", "Dash", "Glimmer", "Breeze", "Spark", "Twig",
        "Arachnid", "Wisp", "Drake", "Fern", "Cinder", "Skitter", "Prowler", "Sylph", "Grove", "Mire",
        "Wyrm", "Shade", "Flicker", "Thorn", "Rift", "Boulder", "Zephyr", "Crag", "Moth", "Viper",
        "Talon", "Mist", "Ember", "Ridge", "Chasm", "Lynx", "Specter", "Bramble", "Flux", "Raven",
        "Gale", "Crest", "Dusk", "Fang", "Haze", "Tide", "Blaze", "Stalker", "Vine", "Frost", "Echo"
    };

    [Header("Spawn Settings")]
    [SerializeField] private GameObject creaturePrefab; // Single creature prefab to spawn
    [SerializeField] private int minCreaturesToSpawn = 5; // Minimum number of creatures to spawn
    [SerializeField] private int maxCreaturesToSpawn = 10; // Maximum number of creatures to spawn
    [SerializeField] private float spawnRadius = 20f; // Radius around CreatureBase to spawn creatures
    [SerializeField] private int maxTraitPoints = 8; // Max trait cost for creature type

    void Start()
    {
        // Define creature type (traits and name) once
        DefineCreatureType();

        // Spawn initial creatures
        int creaturesToSpawn = Random.Range(minCreaturesToSpawn, maxCreaturesToSpawn + 1);
        for (int i = 0; i < creaturesToSpawn; i++)
        {
            SpawnCreature();
        }
    }

    private void DefineCreatureType()
    {
        // Select random traits until total cost equals maxTraitPoints
        creatureTypeTraits.Clear();
        List<int> availableTraitIds = TraitManager.GetAllTraitIds().ToList();
        int totalCost = 0;
        int loopLimit = 100; // Safety limit to prevent infinite loops
        int loopCount = 0;

        while (totalCost < maxTraitPoints && loopCount < loopLimit)
        {
            // Filter traits that fit within remaining points
            List<int> validTraitIds = availableTraitIds
                .Where(id => TraitManager.GetTraitCost(id) <= maxTraitPoints - totalCost)
                .ToList();

            if (validTraitIds.Count == 0)
            {
                Debug.LogWarning($"{name}: No traits available to reach exactly {maxTraitPoints} points. Total cost: {totalCost}");
                break;
            }

            // Pick a random valid trait
            int index = Random.Range(0, validTraitIds.Count);
            int traitId = validTraitIds[index];
            int traitCost = TraitManager.GetTraitCost(traitId);

            creatureTypeTraits.Add(traitId);
            totalCost += traitCost;
            loopCount++;
        }

        if (totalCost != maxTraitPoints)
        {
            Debug.LogWarning($"{name}: Failed to reach exactly {maxTraitPoints} points after {loopCount} iterations. Total cost: {totalCost}, Traits: {string.Join(", ", creatureTypeTraits.Select(id => TraitManager.GetTraitName(id)))}");
        }
        else
        {
            Debug.Log($"{name}: Creature type defined with traits: {string.Join(", ", creatureTypeTraits.Select(id => TraitManager.GetTraitName(id)))} (Total cost: {totalCost})");
        }

        // Generate type name
        string traitName = "Unknown";
        if (creatureTypeTraits.Count > 0)
        {
            int randomTraitId = creatureTypeTraits[Random.Range(0, creatureTypeTraits.Count)];
            traitName = TraitManager.GetTraitName(randomTraitId) ?? "Unknown";
        }
        string randomName = randomNames[Random.Range(0, randomNames.Count)];
        typeNameBase = $"{traitName} {randomName}";
    }

    private void SpawnCreature()
    {
        if (creaturePrefab == null)
        {
            Debug.LogWarning($"{name}: No creature prefab assigned to spawn!");
            return;
        }

        Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
        spawnPos.y = transform.position.y; // Keep spawn on same Y level

        // Ensure spawn position is on NavMesh
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            GameObject creatureObj = Instantiate(creaturePrefab, hit.position, Quaternion.identity);
            CreatureBehavior creature = creatureObj.GetComponent<CreatureBehavior>();
            if (creature != null)
            {
                // Randomize creature attributes
                creature.baseSize = Random.Range(0.8f, 1.2f);
                creature.baseWalkingSpeed = Random.Range(3f, 5f);
                creature.baseHealth = Random.Range(8f, 12f);
                creature.baseMaxFoodLevel = Random.Range(8f, 12f);
                creature.gender = Random.value < 0.5f ? CreatureBehavior.Gender.Male : CreatureBehavior.Gender.Female;

                // Assign fixed traits for this creature type
                creature.traitIds.Clear();
                creature.traitIds.AddRange(creatureTypeTraits);

                // Set owning base
                creature.owningBase = this;

                AddCreature(creature);
            }
        }
        else
        {
            Debug.LogWarning($"{name}: Failed to find valid NavMesh position for spawning creature!");
        }
    }

    public void AddCreature(CreatureBehavior creature)
    {
        if (!livingCreatures.Contains(creature))
        {
            livingCreatures.Add(creature);
            AssignCreatureName(creature);
            Debug.Log($"Added creature: {creature.gameObject.name} to base {name}");
        }
    }

    public void RemoveCreature(CreatureBehavior creature)
    {
        if (livingCreatures.Contains(creature))
        {
            livingCreatures.Remove(creature);
            Debug.Log($"Removed creature: {creature.gameObject.name} from base {name}");
        }
    }

    private void AssignCreatureName(CreatureBehavior creature)
    {
        typeSpawnCount++;
        string creatureName = $"{typeNameBase} ({typeSpawnCount})";
        creature.gameObject.name = creatureName;

        // Update text display to reflect new name
        creature.UpdateTextDisplay();
    }

    public string GenerateChildName(CreatureBehavior child)
    {
        // Use current spawn count without incrementing (incremented in AddCreature)
        int currentSpawnCount = typeSpawnCount + 1;
        return $"{typeNameBase} ({currentSpawnCount})";
    }
}