using UnityEngine;
using System.Collections.Generic;

public class BerryBush : MonoBehaviour
{
    [SerializeField] private int maxFood = 10; // Maximum food capacity
    [SerializeField] public int currentFood = 10; // Current food available
    [SerializeField] private float replenishInterval = 30f; // Time to replenish 1 food unit
    private float replenishTimer = 0f;
    [SerializeField] private bool hasFood = true; // Tracks if bush has usable food

    // List of child GameObjects (e.g., apples) to toggle visibility
    [SerializeField] private List<GameObject> foodObjects = new List<GameObject>();

    void Start()
    {
        // Optional: Auto-populate from children if list is empty
        if (foodObjects.Count == 0)
        {
            PopulateFromChildren();
        }

        // Ensure maxFood matches the number of food objects
        if (foodObjects.Count != maxFood)
        {
            Debug.LogWarning($"BerryBush '{name}' has {foodObjects.Count} food objects but maxFood is {maxFood}. Adjusting maxFood.");
            maxFood = foodObjects.Count;
        }

        // Clamp currentFood and update state
        currentFood = Mathf.Clamp(currentFood, 0, maxFood);
        UpdateFoodState();
        UpdateFoodVisibility();
    }

    void Update()
    {
        // Replenish food over time
        replenishTimer += Time.deltaTime;
        if (replenishTimer >= replenishInterval && currentFood < maxFood)
        {
            currentFood++;
            UpdateFoodState();
            UpdateFoodVisibility();
            replenishTimer = 0f;
            Debug.Log($"{name}: Food replenished to {currentFood}/{maxFood}, hasFood: {hasFood}");
        }
    }

    // Populate foodObjects from child GameObjects if not set manually
    private void PopulateFromChildren()
    {
        foodObjects.Clear();
        foreach (Transform child in transform)
        {
            foodObjects.Add(child.gameObject);
            Debug.Log($"{name}: Added child {child.name} to foodObjects list");
        }
    }

    // Update visibility based on currentFood
    private void UpdateFoodVisibility()
    {
        int objectsToDisable = maxFood - currentFood; // Number of objects to turn off
        for (int i = 0; i < foodObjects.Count; i++)
        {
            if (foodObjects[i] != null)
            {
                bool shouldBeActive = i < currentFood; // Active if index < currentFood
                bool isActive = foodObjects[i].activeSelf;
                if (isActive != shouldBeActive)
                {
                    foodObjects[i].SetActive(shouldBeActive);
                    Debug.Log($"{name}: Set {foodObjects[i].name} (index {i}) to active: {shouldBeActive} (currentFood: {currentFood})");
                }
            }
            else
            {
                Debug.LogWarning($"{name}: Food object at index {i} is null");
            }
        }
    }

    // Update hasFood based on currentFood
    private void UpdateFoodState()
    {
        if (currentFood == 0)
        {
            hasFood = false;
        }
        else if (currentFood >= maxFood * 0.3f) // 30% of maxFood
        {
            hasFood = true;
        }
        // If 0 < currentFood < 30%, hasFood remains false until it reaches 30%
    }

    // Public property for currentFood (used by CreatureBehavior)
    public int CurrentFood
    {
        get { return currentFood; }
        set
        {
            currentFood = Mathf.Clamp(value, 0, maxFood);
            UpdateFoodState();
            UpdateFoodVisibility();
            Debug.Log($"{name}: CurrentFood set to {currentFood}/{maxFood}, hasFood: {hasFood}");
        }
    }

    // Public getter for hasFood (used by CreatureBehavior)
    public bool HasFood => hasFood;
}