using UnityEngine;
using TMPro;
using System.Linq; // Added for Select

public class ScanToolScript : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scanText;
    [SerializeField] private float scanRange = 10f;
    [SerializeField] private LayerMask scanLayer;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Scan();
        }
    }

    private void Scan()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, scanRange, scanLayer))
        {
            CreatureBehavior creature = hit.collider.GetComponent<CreatureBehavior>();
            FoodSource foodSource = hit.collider.GetComponent<FoodSource>();
            CreatureBase creatureBase = hit.collider.GetComponent<CreatureBase>();

            if (creature != null)
            {
                DisplayCreatureInfo(creature);
            }
            else if (foodSource != null)
            {
                DisplayFoodSourceInfo(foodSource);
            }
            else if (creatureBase != null)
            {
                DisplayCreatureBaseInfo(creatureBase);
            }
            else
            {
                scanText.text = "No scannable object detected.";
            }
        }
        else
        {
            scanText.text = "Nothing in range to scan.";
        }
    }

    private void DisplayCreatureInfo(CreatureBehavior creature)
    {
        string traits = string.Join(", ", creature.traitIds.Select(id => TraitManager.GetTraitName(id)));
        string state = creature.currentState.ToString();
        string health = $"HP: {(creature.currentState == CreatureBehavior.State.Dead ? 0 : Mathf.CeilToInt(creature.Health))}/{creature.baseHealth * creature.healthMultiplier:F0}";
        string food = $"Food: {creature.foodLevel:F0}/{creature.MaxFoodLevel:F0}";
        string stamina = $"Stamina: {creature.stamina:F0}/{creature.maxStamina:F0}";
        string age = $"Age: {creature.currentAge:F1}/{creature.maxAge:F1}";
        string gender = $"Gender: {creature.gender}";
        string additionalInfo = "";

        if (creature.gender == CreatureBehavior.Gender.Female && creature.isPregnant)
        {
            additionalInfo += $"\nPregnant: {creature.totalFoodLostSincePregnant / 2}/{creature.ReproductionCost}";
        }
        if (creature.FoodGrabbed > 0) // Updated to use public property
        {
            additionalInfo += $"\nCarrying Food: {creature.FoodGrabbed}/{Mathf.Ceil(creature.MaxFoodLevel / 2)}";
        }
        if (creature.Poisoned)
        {
            additionalInfo += "\nPoisoned";
        }
        if (creature.immobilized)
        {
            additionalInfo += $"\nImmobilized: {creature.immobilizedTimer:F1}s remaining";
        }

        scanText.text = $"Creature: {creature.name}\nTraits: {traits}\nState: {state}\n{health}\n{food}\n{stamina}\n{age}\n{gender}{additionalInfo}";
    }

    private void DisplayFoodSourceInfo(FoodSource foodSource)
    {
        string foodAmount = $"Food: {foodSource.CurrentFood}/{foodSource.maxFood}";
        string status = foodSource.HasFood ? "Has Food" : "Depleted";
        scanText.text = $"Food Source: {foodSource.name}\n{foodAmount}\nStatus: {status}";
    }

    private void DisplayCreatureBaseInfo(CreatureBase creatureBase)
    {
        string foodAmount = $"Food: {creatureBase.CurrentFood}/{creatureBase.MaxFoodCapacity}";
        string creatureCount = $"Creatures: {creatureBase.Creatures.Count}";
        string leader = creatureBase.Leader != null ? $"Leader: {creatureBase.Leader.name}" : "No Leader";
        scanText.text = $"Creature Base: {creatureBase.name}\n{foodAmount}\n{creatureCount}\n{leader}";
    }
}