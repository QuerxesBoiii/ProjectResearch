using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// DO NOT REMOVE OR EDIT THIS COMMENT: Code is using Unity 6 (6000.0.37f1)
// DO NOT REMOVE OR EDIT THIS COMMENT: Make sure the code is easy to understand, and isn't inefficient. Make the code slightly more efficient.
// DO NOT REMOVE OR EDIT THIS COMMENT: This script manages food sources in the simulation, including replenishment and animated scaling for apples, berries, meat, etc.
// DO NOT REMOVE OR EDIT THIS COMMENT: The script supports both replenishable (e.g., trees) and non-replenishable (e.g., dead creatures) food sources.

public class FoodSource : MonoBehaviour
{
    [SerializeField] private int maxFood = 10; // Maximum amount of food the source can hold
    [SerializeField] public int currentFood = 10; // Current amount of food available
    [SerializeField] private float replenishInterval = 15f; // Time (seconds) between food replenishment
    private float replenishTimer = 0f; // Timer for replenishment
    [SerializeField] private bool hasFood = true; // Indicates if the source has any food left
    [SerializeField] private float foodSatiety = 1f; // How much hunger each food unit restores
    [SerializeField] public bool isNotReplenishable = false; // If true, food doesn’t replenish (e.g., meat from dead creatures)

    [SerializeField] private List<GameObject> foodObjects = new List<GameObject>(); // Child objects representing individual food items
    private List<Coroutine> activeCoroutines; // Tracks animations for each food object

    // Animation durations
    [SerializeField] private float shrinkDuration = 1f; // Time to shrink from scale 1 to 0 when eaten
    [SerializeField] private float growDuration = 15f; // Time to grow from 0 to 0.75 (matches replenishInterval)
    [SerializeField] private float bubbleDuration = 0.5f; // Time for the bubble effect (0.75 -> 1.5 -> 1)

    // Called when the script instance is being loaded
    void Start()
    {
        // Populate foodObjects from child objects if not set in Inspector, excluding the creature's main body
        if (foodObjects.Count == 0)
        {
            PopulateFromChildren();
        }

        // Warn and adjust maxFood if it doesn’t match the number of food objects
        if (foodObjects.Count != maxFood && foodObjects.Count > 0)
        {
            Debug.LogWarning($"FoodSource '{name}' has {foodObjects.Count} food objects but maxFood is {maxFood}. Adjusting maxFood.");
            maxFood = foodObjects.Count;
        }
        else if (foodObjects.Count == 0 && GetComponent<CreatureBehavior>())
        {
            // For dead creatures, set maxFood based on size if no food objects are defined
            CreatureBehavior creature = GetComponent<CreatureBehavior>();
            maxFood = Mathf.CeilToInt(creature.size * 5f); // E.g., 5 food units per size unit
            currentFood = maxFood;
            Debug.Log($"{name}: No food objects defined for creature, set maxFood to {maxFood} based on size {creature.size}");
        }

        // Clamp initial currentFood to valid range
        currentFood = Mathf.Clamp(currentFood, 0, maxFood);
        activeCoroutines = new List<Coroutine>(new Coroutine[maxFood]); // Pre-allocate coroutine list
        UpdateFoodState();

        // Set initial scale based on currentFood (all enabled at start)
        for (int i = 0; i < foodObjects.Count; i++)
        {
            if (foodObjects[i] != null)
            {
                foodObjects[i].transform.localScale = (i < currentFood) ? Vector3.one : Vector3.zero;
                foodObjects[i].SetActive(true); // Start with all objects enabled
            }
        }
    }

    // Updates the food source state each frame
    void Update()
    {
        // Only replenish if the source is replenishable (e.g., trees, not meat)
        if (!isNotReplenishable)
        {
            replenishTimer += Time.deltaTime;
            if (replenishTimer >= replenishInterval && currentFood < maxFood)
            {
                currentFood++;
                UpdateFoodState();
                StartGrowAnimation(currentFood - 1); // Animate the newly added food
                replenishTimer = 0f;
            }
        }
    }

    // Populates foodObjects list from child GameObjects, excluding the creature’s main body
    private void PopulateFromChildren()
    {
        foodObjects.Clear();
        Renderer creatureRenderer = GetComponentInChildren<Renderer>();
        foreach (Transform child in transform)
        {
            // Skip the child if it’s the creature’s main renderer
            if (creatureRenderer != null && child.GetComponent<Renderer>() == creatureRenderer)
            {
                continue;
            }
            foodObjects.Add(child.gameObject);
            Debug.Log($"{name}: Added child {child.name} to foodObjects list");
        }
    }

    // Updates the hasFood flag based on currentFood level
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

    // Triggers shrink animation for a food object at the specified index
    private void StartShrinkAnimation(int index)
    {
        if (index >= 0 && index < foodObjects.Count && foodObjects[index] != null)
        {
            if (activeCoroutines[index] != null)
                StopCoroutine(activeCoroutines[index]);
            activeCoroutines[index] = StartCoroutine(ShrinkFood(foodObjects[index]));
        }
    }

    // Triggers grow animation for a food object at the specified index
    private void StartGrowAnimation(int index)
    {
        if (index >= 0 && index < foodObjects.Count && foodObjects[index] != null)
        {
            if (activeCoroutines[index] != null)
                StopCoroutine(activeCoroutines[index]);
            foodObjects[index].SetActive(true); // Enable before growing
            activeCoroutines[index] = StartCoroutine(GrowFood(foodObjects[index]));
        }
    }

    // Coroutine to animate food shrinking from current scale to 0
    private IEnumerator ShrinkFood(GameObject food)
    {
        float elapsed = 0f;
        Vector3 startScale = food.transform.localScale;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            food.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        food.transform.localScale = Vector3.zero;
        food.SetActive(false); // Disable after shrinking
    }

    // Coroutine to animate food growing from 0 to 0.75, then bubbling to 1.5 and back to 1
    private IEnumerator GrowFood(GameObject food)
    {
        // Phase 1: Grow from 0 to 0.75
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 midScale = Vector3.one * 0.75f;
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / growDuration;
            food.transform.localScale = Vector3.Lerp(startScale, midScale, t);
            yield return null;
        }
        food.transform.localScale = midScale;

        // Phase 2: Bubble effect from 0.75 to 1.5 and back to 1
        elapsed = 0f;
        Vector3 peakScale = Vector3.one * 1.5f;
        Vector3 endScale = Vector3.one;
        while (elapsed < bubbleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bubbleDuration;
            if (t < 0.5f) // First half: 0.75 to 1.5
                food.transform.localScale = Vector3.Lerp(midScale, peakScale, t * 2f);
            else // Second half: 1.5 to 1
                food.transform.localScale = Vector3.Lerp(peakScale, endScale, (t - 0.5f) * 2f);
            yield return null;
        }
        food.transform.localScale = endScale;
    }

    // Property to get/set currentFood, handling animations and destruction
    public int CurrentFood
    {
        get { return currentFood; }
        set
        {
            int oldValue = currentFood;
            currentFood = Mathf.Clamp(value, 0, maxFood);
            UpdateFoodState();

            // Handle food being eaten (shrinking animation)
            if (currentFood < oldValue)
            {
                int foodLost = oldValue - currentFood;
                for (int i = 0; i < foodLost; i++)
                {
                    int index = currentFood + i;
                    StartShrinkAnimation(index);
                }
            }
            // Handle food being added (growing animation)
            else if (currentFood > oldValue)
            {
                int foodGained = currentFood - oldValue;
                for (int i = 0; i < foodGained; i++)
                {
                    int index = oldValue + i;
                    StartGrowAnimation(index);
                }
            }

            // Destroy the object if non-replenishable and fully consumed
            if (isNotReplenishable && currentFood <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public bool HasFood => hasFood;
    public float FoodSatiety => foodSatiety;
}