using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class CreatureBase : MonoBehaviour
{
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private int initialPopulation = 4;
    [SerializeField] public string creatureTypeName = "Unnamed Creature";
    [SerializeField] public string creatureTraits = "None";
    public List<CreatureBehavior> livingCreatures = new List<CreatureBehavior>();

    void Start()
    {
        SpawnInitialCreatures();
    }

    void SpawnInitialCreatures()
    {
        Debug.Log("Starting to spawn creatures");

        List<int> selectedTraitIds = SelectRandomTraits();
        creatureTraits = string.Join(", ", selectedTraitIds.Select(id => TraitManager.GetTraitName(id)));

        if (selectedTraitIds.Count > 0)
        {
            int randomTraitId = selectedTraitIds[UnityEngine.Random.Range(0, selectedTraitIds.Count)];
            creatureTypeName = TraitManager.GetTraitName(randomTraitId) + " Creature";
        }
        else
        {
            creatureTypeName = "Basic Creature";
        }

        if (creaturePrefab == null)
        {
            Debug.LogError("Creature prefab is not assigned in the Inspector!");
            return;
        }

        for (int i = 0; i < initialPopulation; i++)
        {
            Debug.Log($"Attempting to spawn creature {i + 1}");

            // Calculate a random offset in the XZ plane, keeping Y consistent
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 3f;
            randomOffset.y = 0; // Ensure spiders stay at the same height
            Vector3 basePos = transform.position + Vector3.up * 2f;

            // Find a valid NavMesh position
            Vector3 spawnPos;
            if (NavMesh.SamplePosition(basePos + randomOffset, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }
            else
            {
                spawnPos = basePos; // Fallback to base position
                Debug.LogWarning($"Creature {i + 1} could not find a NavMesh position, using fallback at {spawnPos}");
            }

            GameObject creature = Instantiate(creaturePrefab, spawnPos, Quaternion.identity);
            CreatureBehavior behavior = creature.GetComponent<CreatureBehavior>();

            if (behavior != null)
            {
                behavior.Age = 10f;
                behavior.traitIds = new List<int>(selectedTraitIds);
                behavior.gender = (i < 2) ? CreatureBehavior.Gender.Male : CreatureBehavior.Gender.Female;
                behavior.name = creatureTypeName;
                livingCreatures.Add(behavior);
                AssignTraitsToCreature(behavior);
                Debug.Log($"Spawned {creatureTypeName} with traits: {creatureTraits}, Gender: {behavior.gender} at {spawnPos}");
            }
            else
            {
                Debug.LogError($"Creature {i + 1} at {spawnPos} is missing CreatureBehavior component!");
                Destroy(creature);
            }
        }

        Debug.Log($"Finished spawning {livingCreatures.Count} creatures");
    }

    // Placeholder methods (ensure these are implemented elsewhere)
    private List<int> SelectRandomTraits()
    {
        // Implement your trait selection logic here
        return new List<int>(); // Example
    }

    private void AssignTraitsToCreature(CreatureBehavior creature)
    {
        // Implement your trait assignment logic here
    }
}