using UnityEngine;
using TMPro;

public class ScanToolScript : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TextMeshProUGUI displayText; // Optional: UI text for displaying stats
    [SerializeField] private float scanRange = 10f;
    [SerializeField] private LayerMask creatureLayer;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError($"{name}: No main camera assigned or found!");
                enabled = false;
            }
        }

        if (displayText == null)
        {
            Debug.LogWarning($"{name}: No TextMeshProUGUI assigned; using Debug.Log for output.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left-click to scan
        {
            ScanCreature();
        }
    }

    private void ScanCreature()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, scanRange, creatureLayer))
        {
            CreatureBehavior creature = hit.collider.GetComponent<CreatureBehavior>();
            if (creature != null)
            {
                DisplayCreatureInfo(creature);
            }
            else
            {
                ClearDisplay("No creature detected.");
            }
        }
        else
        {
            ClearDisplay("No creature in range.");
        }
    }

    private void DisplayCreatureInfo(CreatureBehavior creature)
    {
        string info = $"Creature: {creature.gameObject.name}\n";
        info += $"Gender: {creature.gender}\n";
        info += $"State: {creature.currentState}\n";
        info += $"Health: {Mathf.CeilToInt(creature.Health)}/{Mathf.CeilToInt(creature.Size * 10f)}\n";
        info += $"Food: {creature.FoodLevel:F0}/{creature.MaxFoodLevel:F0}\n";
        info += $"Stamina: {creature.Stamina:F0}/{creature.MaxStamina:F0}\n";
        info += $"Age: {creature.Age:F1}/{creature.MaxAge:F1}\n";

        // Display traits
        info += "Traits:\n";
        if (creature.traitIds.Count > 0)
        {
            foreach (int traitId in creature.traitIds)
            {
                string traitName = TraitManager.GetTraitName(traitId) ?? "Unknown";
                info += $"- {traitName}\n";
            }
        }
        else
        {
            info += "- None\n";
        }

        // Pregnancy status
        if (creature.gender == CreatureBehavior.Gender.Female && creature.IsPregnant)
        {
            info += $"Pregnant: {creature.TotalFoodLostSincePregnant / 2}/{creature.ReproductionCost}\n";
        }

        // Burrowing status
        if (creature.isBurrowing)
        {
            info += $"Burrowing: {creature.burrowTimer:F1}s remaining\n";
        }

        // Output to UI or Debug.Log
        if (displayText != null)
        {
            displayText.text = info;
        }
        Debug.Log(info);
    }

    private void ClearDisplay(string message)
    {
        if (displayText != null)
        {
            displayText.text = message;
        }
        Debug.Log(message);
    }
}