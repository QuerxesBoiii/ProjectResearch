using UnityEngine;
using System.Collections.Generic;

// Code is using Unity 6 (6000.0.37f1)

public class FoodSource : MonoBehaviour
{
    [SerializeField] public int maxFood = 10;
    [SerializeField] public int currentFood = 10;
    [SerializeField] private float replenishInterval = 30f;
    private float replenishTimer = 0f;
    [SerializeField] private bool hasFood = true;
    [SerializeField] private float foodSatiety = 1f;
    [SerializeField] public bool isNotReplenishable = false;

    [SerializeField] private List<GameObject> foodObjects = new List<GameObject>();

    void Start()
    {
        if (foodObjects.Count == 0)
        {
            PopulateFromChildren();
        }

        if (foodObjects.Count != maxFood)
        {
            Debug.LogWarning($"FoodSource '{name}' has {foodObjects.Count} food objects but maxFood is {maxFood}. Adjusting maxFood.");
            maxFood = foodObjects.Count;
        }

        currentFood = Mathf.Clamp(currentFood, 0, maxFood);
        UpdateFoodState();
        UpdateFoodVisibility();
    }

    void Update()
    {
        if (!isNotReplenishable)
        {
            replenishTimer += Time.deltaTime;
            if (replenishTimer >= replenishInterval && currentFood < maxFood)
            {
                currentFood++;
                UpdateFoodState();
                UpdateFoodVisibility();
                replenishTimer = 0f;
            }
        }
        else if (currentFood <= 0)
        {
            Destroy(gameObject);
            Debug.Log($"{name}: Depleted and destroyed");
        }
    }

    private void PopulateFromChildren()
    {
        foodObjects.Clear();
        foreach (Transform child in transform)
        {
            foodObjects.Add(child.gameObject);
            Debug.Log($"{name}: Added child {child.name} to foodObjects list");
        }
    }

    private void UpdateFoodVisibility()
    {
        int objectsToDisable = maxFood - currentFood;
        for (int i = 0; i < foodObjects.Count; i++)
        {
            if (foodObjects[i] != null)
            {
                bool shouldBeActive = i < currentFood;
                bool isActive = foodObjects[i].activeSelf;
                if (isActive != shouldBeActive)
                {
                    foodObjects[i].SetActive(shouldBeActive);
                }
            }
            else
            {
                Debug.LogWarning($"{name}: Food object at index {i} is null");
            }
        }
    }

    private void UpdateFoodState()
    {
        if (currentFood == 0)
        {
            hasFood = false;
        }
        else if (currentFood >= maxFood * 0.3f)
        {
            hasFood = true;
        }
    }

    public int CurrentFood
    {
        get => currentFood;
        set
        {
            currentFood = Mathf.Clamp(value, 0, maxFood);
            UpdateFoodState();
            UpdateFoodVisibility();
        }
    }

    public bool HasFood => hasFood;
    public float FoodSatiety => foodSatiety;
}